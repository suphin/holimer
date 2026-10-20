using System.Data;
using System.Text.Json;
using Dapper;
using Ekomers.Data;
using Ekomers.Data.Services.Profitability;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Ekomers.Web.Infrastructure.Auth;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = ReportPermissions.MuhasebeGiderleri)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed partial class SatisKarlilikGiderleriController(ApplicationDbContext context, LogoContext logoContext, IAccountingExpenseService expenses) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, CancellationToken ct)
    {
        ViewBag.Modul = "Rapor";
        var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = endDate ?? DateTime.Today;
        var summary = await expenses.GetSummaryAsync(start, end, ct);
        var groups = await context.RptAccountingExpenseGroups.AsNoTracking().Where(x => x.IsDelete != true).OrderBy(x => x.Name).ToListAsync(ct);
        var rules = await (from rule in context.RptAccountingExpenseRules.AsNoTracking()
                           join expenseGroup in context.RptAccountingExpenseGroups.AsNoTracking() on rule.ExpenseGroupId equals expenseGroup.ID
                           where rule.FirmNumber == 100 && rule.IsDelete != true
                           orderby rule.AccountCode, rule.ValidFrom descending
                           select new AccountingExpenseRuleVM { Id = rule.ID, AccountCode = rule.AccountCode, GroupName = expenseGroup.Name,
                               ValidFrom = rule.ValidFrom, ValidTo = rule.ValidTo, ExpenseGroupId = expenseGroup.ID }).ToListAsync(ct);
        return View(new AccountingExpenseManagementVM { Summary = summary, Groups = groups, Rules = rules });
    }

    [HttpGet]
    public async Task<IActionResult> Hareketler(string accountCode, DateTime startDate, DateTime endDate, int page = 1, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(accountCode) || accountCode.Length > 100 || !accountCode.StartsWith('7') || page < 1) return BadRequest();
        ViewBag.Modul = "Rapor";
        return View(await expenses.GetMovementsAsync(accountCode.Trim(), startDate, endDate, page, ct));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ReportPermissions.MuhasebeGiderleriYonet)]
    public async Task<IActionResult> GrupKaydet(AccountingExpenseGroupInputVM input, int? id, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        if (!ModelState.IsValid || !Enum.IsDefined(input.Behavior) || !Enum.IsDefined(input.Stage) || string.IsNullOrWhiteSpace(input.Name))
            return Error("Geçerli grup adı, türü ve aşaması giriniz.", startDate, endDate);
        var name = input.Name.Trim();
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var group = id.HasValue ? await context.RptAccountingExpenseGroups.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct) : null;
        if (id.HasValue && group == null) return NotFound();
        if (await context.RptAccountingExpenseGroups.AnyAsync(x => x.Name == name && x.IsDelete != true && x.ID != (id ?? 0), ct)) return Error("Bu grup adı zaten kullanılıyor.", startDate, endDate);
        if (group != null && (group.Behavior != input.Behavior || group.Stage != input.Stage) && await context.RptAccountingExpenseRules.AnyAsync(x => x.ExpenseGroupId == group.ID && x.IsDelete != true, ct))
            return Error("Kullanılmış grubun türü/aşaması değiştirilemez. Yeni grup açıp yeni tarihli sınıflandırma ekleyiniz.", startDate, endDate);
        var before = group == null ? null : new { group.Name, group.Behavior, group.Stage, group.Notes };
        if (group == null) { group = new() { CreateDate = DateTime.Now, CreateUserID = Actor, IsActive = true, IsDelete = false }; context.Add(group); }
        group.Name = name; group.Behavior = input.Behavior; group.Stage = input.Stage; group.Notes = input.Notes?.Trim();
        group.UpdateDate = DateTime.Now; group.UpdateUserID = Actor;
        if (!await CommitAsync(tx, ct, () => Audit("GiderGrubuKaydet", new { Before = before, After = new { group.ID, group.Name, group.Behavior, group.Stage } })))
            return Error("Grup eşzamanlı değişti veya aynı ad kullanılıyor. Sayfayı yenileyiniz.", startDate, endDate);
        TempData["success"] = "Gider grubu kaydedildi."; return Back(startDate, endDate);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ReportPermissions.MuhasebeGiderleriYonet)]
    public async Task<IActionResult> GrupSil(int id, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var group = await GroupForUpdate(id, ct);
        if (group == null) return NotFound();
        if (await context.RptAccountingExpenseRules.AnyAsync(x => x.ExpenseGroupId == id && x.IsDelete != true, ct)) return Error("Sınıflandırmada kullanılan grup silinemez.", startDate, endDate);
        group.IsDelete = true; group.IsActive = false; group.DeleteDate = DateTime.Now; group.DeleteUserID = Actor;
        Audit("GiderGrubuSil", new { group.ID, group.Name });
        if (!await CommitAsync(tx, ct)) return Error("Grup başka işlemde kullanıldı. Sayfayı yenileyiniz.", startDate, endDate);
        TempData["success"] = "Grup arşivlendi; denetim kaydı korunuyor."; return Back(startDate, endDate);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ReportPermissions.MuhasebeGiderleriYonet)]
    public async Task<IActionResult> HesaplariBagla(AccountingExpenseMappingInputVM input, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var codes = (input.AccountCodes ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (!ModelState.IsValid || codes.Count is 0 or > 500 || codes.Any(x => x.Length > 100 || !AccountingExpenseService.CanMap(x)) || !ValidDates(input.ValidFrom, input.ValidTo))
            return Error("Hesap seçimi veya geçerlilik tarihleri geçersiz. Korunan hesaplar bağlanamaz.", startDate, endDate);
        var connection = logoContext.Database.GetDbConnection();
        var actual = (await connection.QueryAsync<string>(new CommandDefinition("SELECT CODE FROM dbo.LG_100_EMUHACC WHERE CODE IN @Codes", new { Codes = codes }, cancellationToken: ct)))
            .Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codes.Any(x => !actual.Contains(x))) return Error("Seçilen hesaplardan biri Logo'da bulunamadı.", startDate, endDate);
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var group = await GroupForUpdate(input.ExpenseGroupId, ct);
        if (group == null) return Error("Aktif gider grubu seçiniz.", startDate, endDate);
        var existing = await context.RptAccountingExpenseRules.Where(x => x.FirmNumber == 100 && codes.Contains(x.AccountCode) && x.IsDelete != true).ToListAsync(ct);
        foreach (var code in codes)
        {
            var from = input.ValidFrom.Date;
            var old = existing.Where(x => string.Equals(x.AccountCode, code, StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.ValidFrom).ToList();
            if (old.Any(x => x.ValidFrom == from)) return Error($"{code}: Aynı başlangıç tarihli kural zaten var; aşağıdaki kuralı düzenleyiniz.", startDate, endDate);
            var previous = old.LastOrDefault(x => x.ValidFrom < from);
            var next = old.FirstOrDefault(x => x.ValidFrom > from);
            var until = input.ValidTo?.Date ?? next?.ValidFrom.AddDays(-1);
            if (next != null && until >= next.ValidFrom) return Error($"{code}: Sonraki sınıflandırma tarihiyle çakışıyor.", startDate, endDate);
            if (previous?.ValidTo != null && previous.ValidTo >= from) return Error($"{code}: Önceki tarih aralığıyla çakışıyor.", startDate, endDate);
            if (previous != null && previous.ValidTo == null)
            {
                previous.ValidTo = from.AddDays(-1); previous.UpdateDate = DateTime.Now; previous.UpdateUserID = Actor;
                Audit("GiderKuraliDonemKapat", new { previous.ID, previous.AccountCode, previous.ValidTo });
            }
            context.Add(new RptAccountingExpenseRule { FirmNumber = 100, AccountCode = code, ExpenseGroupId = group.ID, ValidFrom = from, ValidTo = until,
                Notes = input.Notes?.Trim(), IsActive = true, IsDelete = false, CreateDate = DateTime.Now, CreateUserID = Actor });
        }
        Audit("GiderHesaplariniBagla", new { Codes = codes, GroupId = group.ID, input.ValidFrom, input.ValidTo });
        if (!await CommitAsync(tx, ct)) return Error("Eşzamanlı sınıflandırma değişikliği algılandı. Sayfayı yenileyip tekrar deneyiniz.", startDate, endDate);
        TempData["success"] = $"{codes.Count} hesap bağlandı. Önceki açık dönemler yeni başlangıçtan önce kapatıldı.";
        return Back(startDate, endDate);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ReportPermissions.MuhasebeGiderleriYonet)]
    public async Task<IActionResult> KuralDuzenle(int id, int expenseGroupId, DateTime validFrom, DateTime? validTo, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        if (!ValidDates(validFrom, validTo)) return Error("Tarih aralığı geçersiz.", startDate, endDate);
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var group = await GroupForUpdate(expenseGroupId, ct);
        var rule = await context.RptAccountingExpenseRules.FirstOrDefaultAsync(x => x.ID == id && x.FirmNumber == 100 && x.IsDelete != true, ct);
        if (rule == null || group == null) return NotFound();
        if (!AccountingExpenseService.CanMap(rule.AccountCode)) return BadRequest();
        if (await context.RptAccountingExpenseRules.AnyAsync(x => x.ID != id && x.FirmNumber == 100 && x.AccountCode == rule.AccountCode && x.IsDelete != true &&
            (validTo == null || x.ValidFrom <= validTo) && (x.ValidTo == null || x.ValidTo >= validFrom.Date), ct)) return Error("Bu hesapta tarih aralığı başka kuralla çakışıyor.", startDate, endDate);
        var before = new { rule.ExpenseGroupId, rule.ValidFrom, rule.ValidTo };
        rule.ExpenseGroupId = expenseGroupId; rule.ValidFrom = validFrom.Date; rule.ValidTo = validTo?.Date; rule.UpdateDate = DateTime.Now; rule.UpdateUserID = Actor;
        Audit("GiderKuraliDuzenle", new { rule.ID, rule.AccountCode, Before = before, After = new { rule.ExpenseGroupId, rule.ValidFrom, rule.ValidTo } });
        if (!await CommitAsync(tx, ct)) return Error("Eşzamanlı kural değişikliği algılandı. Sayfayı yenileyiniz.", startDate, endDate);
        TempData["success"] = "Kural güncellendi. Geçmiş raporlar yeni tarih aralığına göre hesaplanır."; return Back(startDate, endDate);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ReportPermissions.MuhasebeGiderleriYonet)]
    public async Task<IActionResult> KuralSil(int id, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var rule = await context.RptAccountingExpenseRules.FirstOrDefaultAsync(x => x.ID == id && x.FirmNumber == 100 && x.IsDelete != true, ct);
        if (rule == null) return NotFound();
        rule.IsDelete = true; rule.IsActive = false; rule.DeleteDate = DateTime.Now; rule.DeleteUserID = Actor;
        Audit("GiderKuraliSil", new { rule.ID, rule.AccountCode, rule.ExpenseGroupId, rule.ValidFrom, rule.ValidTo });
        if (!await CommitAsync(tx, ct)) return Error("Eşzamanlı kural değişikliği algılandı. Sayfayı yenileyiniz.", startDate, endDate);
        TempData["success"] = "Kural arşivlendi; tutarlar silinmedi. Eşleşmeyen dönemler sınıflandırma bekleyecek."; return Back(startDate, endDate);
    }

    private async Task<RptAccountingExpenseGroup?> GroupForUpdate(int id, CancellationToken ct)
    {
        var query = context.Database.IsSqlServer() ? context.RptAccountingExpenseGroups.FromSqlInterpolated($"SELECT * FROM dbo.RptAccountingExpenseGroup WITH(UPDLOCK,HOLDLOCK) WHERE ID={id}") : context.RptAccountingExpenseGroups.AsQueryable();
        return await query.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true && x.IsActive != false, ct);
    }
    private async Task<bool> CommitAsync(IDbContextTransaction tx, CancellationToken ct, Action? auditAfterSave = null)
    {
        try
        {
            await context.SaveChangesAsync(ct);
            if (auditAfterSave != null) { auditAfterSave(); await context.SaveChangesAsync(ct); }
            await tx.CommitAsync(ct); return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 1205 or 2601 or 2627 or 547 })
        { await tx.RollbackAsync(ct); context.ChangeTracker.Clear(); return false; }
        catch (SqlException ex) when (ex.Number is 1205 or 2601 or 2627 or 547)
        { await tx.RollbackAsync(ct); context.ChangeTracker.Clear(); return false; }
        catch { context.ChangeTracker.Clear(); throw; }
    }
    private string Actor => User.Identity?.Name ?? "unknown";
    private void Audit(string action, object data) { var json = JsonSerializer.Serialize(data); context.UserActivityLog.Add(new UserActivityLog { DateTime = DateTime.Now, UserName = Actor.Length > 100 ? Actor[..100] : Actor, ControllerName = nameof(SatisKarlilikGiderleriController), ActionName = action, Info = "AccountingExpenseConfiguration", Parameters = json.Length > 4096 ? json[..4096] : json }); }
    private static bool ValidDates(DateTime from, DateTime? to) => from.Date >= new DateTime(1900, 1, 1) && from.Date < DateTime.MaxValue.Date && (!to.HasValue || to.Value.Date >= from.Date && to.Value.Date < DateTime.MaxValue.Date);
    private IActionResult Back(DateTime start, DateTime end) => RedirectToAction(nameof(Index), new { startDate = start == default ? (DateTime?)null : start, endDate = end == default ? (DateTime?)null : end });
    private IActionResult Error(string message, DateTime start, DateTime end) { context.ChangeTracker.Clear(); TempData["error"] = message; return Back(start, end); }
}
