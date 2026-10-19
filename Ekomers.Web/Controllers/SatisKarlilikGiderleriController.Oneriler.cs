using System.Data;
using Ekomers.Data.Services.Profitability;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

public sealed partial class SatisKarlilikGiderleriController
{
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Yönetici")]
    public async Task<IActionResult> HazirGruplariOlustur(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        try
        {
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            // Also serializes the first installation, when no preset exists yet.
            var query = context.Database.IsSqlServer()
                ? context.RptAccountingExpenseGroups.FromSqlRaw("SELECT * FROM dbo.RptAccountingExpenseGroup WITH(UPDLOCK,HOLDLOCK)")
                : context.RptAccountingExpenseGroups.AsQueryable();
            var existing = await query.Where(x => x.IsDelete != true).ToListAsync(ct);
            var created = new List<RptAccountingExpenseGroup>();
            var adopted = new List<int>();
            var conflicts = 0;
            foreach (var preset in AccountingExpenseSuggestions.Presets)
            {
                var group = existing.FirstOrDefault(x => x.PresetKey == preset.Key);
                if (group != null)
                {
                    if (group.Behavior != preset.Behavior || group.Stage != preset.Stage || group.IsActive == false) conflicts++;
                    continue; // Preserve renamed/customized existing presets.
                }
                group = existing.FirstOrDefault(x => AccountingExpenseSuggestions.Normalize(x.Name) == AccountingExpenseSuggestions.Normalize(preset.Name));
                if (group != null)
                {
                    if (group.PresetKey != null || group.Behavior != preset.Behavior || group.Stage != preset.Stage || group.IsActive == false) { conflicts++; continue; }
                    group.PresetKey = preset.Key; group.UpdateDate = DateTime.Now; group.UpdateUserID = Actor; adopted.Add(group.ID);
                    continue;
                }
                group = new RptAccountingExpenseGroup { Name = preset.Name, PresetKey = preset.Key, Behavior = preset.Behavior, Stage = preset.Stage,
                    Notes = "Hazır kategori. Hesap eşleştirmesi ve gider tutarı oluşturmaz; kullanım ve sabit/değişken ayrımı kullanıcı onayı gerektirir.",
                    IsActive = true, IsDelete = false, CreateDate = DateTime.Now, CreateUserID = Actor };
                context.Add(group); existing.Add(group); created.Add(group);
            }
            if (!await CommitAsync(tx, ct, () =>
            {
                foreach (var item in created) Audit("HazirGiderGrubuOlustur", new { item.ID, item.PresetKey, item.Name, item.Behavior, item.Stage });
                foreach (var id in adopted) Audit("HazirGiderGrubuBagla", new { ID = id, PresetKey = existing.Single(x => x.ID == id).PresetKey });
                Audit("HazirGiderGruplariOlustur", new { Created = created.Count, Adopted = adopted.Count, Conflicts = conflicts });
            }))
                return Error("Gruplar başka bir işlemde değişti. Sayfayı yenileyip tekrar deneyiniz.", startDate, endDate);
            TempData["success"] = $"{created.Count} hazır grup oluşturuldu; {adopted.Count} mevcut grup şablona bağlandı. Mevcut adlar, türler ve hesap kuralları değiştirilmedi. {conflicts} uyumsuz/pasif grup inceleme bekliyor.";
            return RedirectToAction(nameof(Oneriler), new { startDate, endDate });
        }
        catch (SqlException ex) when (ex.Number is 1205 or 2601 or 2627)
        { return Error("Eşzamanlı kategori değişikliği algılandı. Sayfayı yenileyiniz; mevcut gruplar korunuyor.", startDate, endDate); }
    }

    [HttpGet, Authorize(Roles = "Admin,Yönetici")]
    public async Task<IActionResult> Oneriler(DateTime? startDate, DateTime? endDate, int page = 1, CancellationToken ct = default)
    {
        var start = startDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        var end = endDate ?? DateTime.Today;
        if (!ValidDates(start, end) || end.Date < start.Date) return Error("Geçerli rapor dönemi seçiniz.", default, default);
        ViewBag.Modul = "Rapor";
        var summary = await expenses.GetSummaryAsync(start, end, ct);
        var groups = await context.RptAccountingExpenseGroups.AsNoTracking().Where(x => x.IsDelete != true && x.IsActive != false).OrderBy(x => x.Name).ToListAsync(ct);
        // Any existing rule, including past/future/inactive rules, stays out of this new-account wizard.
        var mapped = (await context.RptAccountingExpenseRules.AsNoTracking().Where(x => x.FirmNumber == 100 && x.IsDelete != true).Select(x => x.AccountCode).ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var candidates = summary.Accounts.Where(x => x.CanMap && !mapped.Contains(x.AccountCode)).OrderBy(x => x.AccountCode).ToList();
        var pages = Math.Max(1, (candidates.Count + 99) / 100);
        page = Math.Clamp(page, 1, pages);
        var rows = candidates.Skip((page - 1) * 100).Take(100).Select(account =>
        {
            var proposal = AccountingExpenseSuggestions.Suggest(account.AccountCode, account.AccountName);
            var preset = AccountingExpenseSuggestions.Presets.FirstOrDefault(x => x.Key == proposal.PresetKey);
            var group = preset == null ? null : groups.FirstOrDefault(x => x.PresetKey == preset.Key && x.Behavior == preset.Behavior && x.Stage == preset.Stage);
            return new AccountingExpenseSuggestionRowVM { AccountCode = account.AccountCode, AccountName = account.AccountName,
                Category = proposal.Category, SuggestedGroupId = group?.ID, NeedsReview = group == null,
                Reason = proposal.Reason + (preset != null && group == null ? " Önerilen hazır grup eksik/pasif/uyumsuz; hazır grupları oluşturunuz veya uygun grubu elle seçiniz." : "") };
        }).ToList();
        return View(new AccountingExpenseSuggestionsVM { Summary = summary, Groups = groups, Rows = rows, Page = page, TotalCandidates = candidates.Count,
            AlreadyMappedCount = summary.Accounts.Count(x => x.CanMap && mapped.Contains(x.AccountCode)), ProtectedCount = summary.Accounts.Count(x => !x.CanMap) });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin,Yönetici")]
    public async Task<IActionResult> OnerileriOnayla(AccountingExpenseSuggestionApprovalVM input, DateTime startDate, DateTime endDate, int page = 1, CancellationToken ct = default)
    {
        var selected = (input.Rows ?? []).Where(x => x.Selected).ToList();
        if (!ModelState.IsValid || input.Rows?.Count > 100 || selected.Count is 0 or > 100 || !ValidDates(input.ValidFrom, input.ValidTo) ||
            !ValidDates(startDate, endDate) || endDate.Date < startDate.Date ||
            selected.Any(x => string.IsNullOrWhiteSpace(x.AccountCode) || x.AccountCode.Length > 100 || !AccountingExpenseService.CanMap(x.AccountCode.Trim()) || x.ExpenseGroupId is null or <= 0))
            return SuggestionError("En az bir hesap seçiniz; seçilen her satır için grup ve geçerli tarihler gereklidir. Korunan hesaplar bağlanamaz.", startDate, endDate, page);
        var codes = selected.Select(x => x.AccountCode.Trim()).ToList();
        if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Count)
            return SuggestionError("Aynı hesap birden fazla satırda gönderilemez.", startDate, endDate, page);
        // Re-read the ERP-derived period scope: hidden fields and browser suggestions are not trusted.
        var summary = await expenses.GetSummaryAsync(startDate, endDate, ct);
        var actual = summary.Accounts.Where(x => x.CanMap).Select(x => x.AccountCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (codes.Any(x => !actual.Contains(x))) return SuggestionError("Hesaplardan biri seçilen dönemde doğrulanamadı. Önizlemeyi yenileyiniz.", startDate, endDate, page);
        try
        {
            await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var groups = new Dictionary<int, RptAccountingExpenseGroup>();
            foreach (var id in selected.Select(x => x.ExpenseGroupId!.Value).Distinct().Order())
            {
                var group = await GroupForUpdate(id, ct);
                if (group == null || !Enum.IsDefined(group.Behavior) || !Enum.IsDefined(group.Stage))
                    return SuggestionError("Seçilen gruplardan biri artık geçerli değil. Önizlemeyi yenileyiniz.", startDate, endDate, page);
                groups.Add(id, group);
            }
            var existing = (await context.RptAccountingExpenseRules.Where(x => x.FirmNumber == 100 && x.IsDelete != true && codes.Contains(x.AccountCode))
                .Select(x => x.AccountCode).ToListAsync(ct)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var added = new List<RptAccountingExpenseRule>();
            foreach (var row in selected.OrderBy(x => x.AccountCode, StringComparer.OrdinalIgnoreCase))
            {
                var code = row.AccountCode.Trim();
                if (existing.Contains(code)) continue; // Stale/repeated submissions never close or overwrite any period.
                var rule = new RptAccountingExpenseRule { FirmNumber = 100, AccountCode = code, ExpenseGroupId = groups[row.ExpenseGroupId!.Value].ID,
                    ValidFrom = input.ValidFrom.Date, ValidTo = input.ValidTo?.Date, Notes = "Hesap önerileri önizlemesinde kullanıcı tarafından seçilerek onaylandı.",
                    IsActive = true, IsDelete = false, CreateDate = DateTime.Now, CreateUserID = Actor };
                context.Add(rule); added.Add(rule);
            }
            if (!await CommitAsync(tx, ct, () =>
            {
                foreach (var rule in added) Audit("GiderOnerisiOnayla", new { rule.ID, rule.AccountCode, rule.ExpenseGroupId, rule.ValidFrom, rule.ValidTo });
                Audit("GiderOnerileriniOnayla", new { Added = added.Count, Skipped = existing.Count });
            }))
                return SuggestionError("Eşzamanlı eşleştirme algılandı. Önizlemeyi yenileyiniz; mevcut kurallar korunuyor.", startDate, endDate, page);
            TempData["success"] = $"{added.Count} hesap onayladığınız gruplara bağlandı. {existing.Count} önceden eşleştirilmiş hesap değiştirilmeden atlandı.";
            return RedirectToAction(nameof(Oneriler), new { startDate, endDate, page });
        }
        catch (SqlException ex) when (ex.Number is 1205 or 2601 or 2627)
        { return SuggestionError("Eşzamanlı işlem algılandı. Önizlemeyi yenileyiniz; mevcut kurallar korunuyor.", startDate, endDate, page); }
    }

    private IActionResult SuggestionError(string message, DateTime startDate, DateTime endDate, int page)
    {
        context.ChangeTracker.Clear(); TempData["error"] = message;
        return RedirectToAction(nameof(Oneriler), new { startDate = startDate == default ? (DateTime?)null : startDate,
            endDate = endDate == default ? (DateTime?)null : endDate, page });
    }
}
