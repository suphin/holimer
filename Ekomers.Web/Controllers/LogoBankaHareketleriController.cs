using System.Globalization;
using System.Security.Claims;
using Ekomers.Data;
using Ekomers.Filters;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin,Rapor")]
[TypeFilter(typeof(ActionFilter))]
[TypeFilter(typeof(ErrorFilter))]
public sealed class LogoBankaHareketleriController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly LogoBankMovementReportService _reportService;
    private readonly LogoBankMovementReportJob _reportJob;

    public LogoBankaHareketleriController(
        ApplicationDbContext context,
        LogoBankMovementReportService reportService,
        LogoBankMovementReportJob reportJob)
    {
        _context = context;
        _reportService = reportService;
        _reportJob = reportJob;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int companyId = 0,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string direction = "all",
        string? clientSearch = null,
        int? editId = null,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "Rapor";
        ViewBag.CanManageSchedules = User.IsInRole("Admin");

        var companies = await GetCompaniesAsync(ct);
        var recipients = await GetRecipientsAsync(ct);
        var start = (startDate ?? DateTime.Today).Date;
        var end = (endDate ?? DateTime.Today).Date;
        if (end < start)
            (start, end) = (end, start);
        if ((end - start).TotalDays > 31)
        {
            end = start.AddDays(31);
            ViewBag.QueryWarning = "Tek sorguda en fazla 32 günlük dönem gösterilir.";
        }

        direction = direction.ToLowerInvariant() is "incoming" or "outgoing" ? direction.ToLowerInvariant() : "all";
        clientSearch = (clientSearch ?? string.Empty).Trim();
        if (clientSearch.Length > 150)
            clientSearch = clientSearch[..150];
        var selectedCompanies = companyId == 0 ? companies : companies.Where(x => x.Id == companyId).ToList();
        var model = new LogoBankMovementReportPageVM
        {
            CompanyId = companyId,
            StartDate = start,
            EndDate = end,
            Direction = direction,
            ClientSearch = clientSearch,
            Companies = companies,
            AvailableRecipients = recipients
        };

        try
        {
            if (selectedCompanies.Count > 0)
                model.Rows = await _reportService.GetMovementsAsync(selectedCompanies, start, end.AddDays(1), direction, clientSearch, ct);
        }
        catch (Exception ex)
        {
            ViewBag.QueryError = "Logo banka hareketleri okunamadı: " + ex.Message;
        }

        if (User.IsInRole("Admin"))
        {
            await FillSchedulesAsync(model, ct);
            if (editId.HasValue)
            {
                var schedule = await _context.LogoBankMovementReportSchedules.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ID == editId.Value && x.IsDelete != true, ct);
                if (schedule != null)
                {
                    model.ScheduleForm = new LogoBankMovementScheduleFormVM
                    {
                        Id = schedule.ID,
                        Name = schedule.Name,
                        CompanyIds = LogoBankMovementReportJob.ParseIntegerIds(schedule.CompanyIds),
                        RecipientUserIds = LogoBankMovementReportJob.ParseStringIds(schedule.RecipientUserIds),
                        SendTime = schedule.SendTime.ToString(@"hh\:mm"),
                        LookbackDays = schedule.LookbackDays,
                        IncludeToday = schedule.IncludeToday,
                        IsEnabled = schedule.IsEnabled
                    };
                }
            }
        }

        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> PlanKaydet(
        [Bind(Prefix = "ScheduleForm")] LogoBankMovementScheduleFormVM model,
        CancellationToken ct)
    {
        if (!TimeSpan.TryParseExact(model.SendTime, @"hh\:mm", CultureInfo.InvariantCulture, out var sendTime))
            ModelState.AddModelError(nameof(model.SendTime), "Gönderim saatini seçiniz.");

        model.CompanyIds = (model.CompanyIds ?? []).Distinct().ToList();
        model.RecipientUserIds = (model.RecipientUserIds ?? []).Distinct(StringComparer.Ordinal).ToList();

        var validCompanyCount = await _context.Sirketler.CountAsync(x =>
            model.CompanyIds.Contains(x.ID) && x.IsDelete != true && x.LogoTigerSirketKodu != null, ct);
        if (validCompanyCount != model.CompanyIds.Count)
            ModelState.AddModelError(nameof(model.CompanyIds), "Seçilen firmalardan birinin Logo firma kodu bulunamadı.");

        var validRecipientCount = await _context.Users.CountAsync(x =>
            model.RecipientUserIds.Contains(x.Id) && x.IsActive && x.Email != null && x.Email != "", ct);
        if (validRecipientCount != model.RecipientUserIds.Count)
            ModelState.AddModelError(nameof(model.RecipientUserIds), "Alıcılar yalnızca e-posta adresi bulunan aktif portal kullanıcıları olabilir.");

        if (!ModelState.IsValid)
        {
            TempData["error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Index), new { editId = model.Id });
        }

        var now = DateTime.Now;
        LogoBankMovementReportSchedule schedule;
        if (model.Id.HasValue)
        {
            schedule = await _context.LogoBankMovementReportSchedules
                .FirstOrDefaultAsync(x => x.ID == model.Id.Value && x.IsDelete != true, ct)
                ?? throw new InvalidOperationException("Düzenlenecek rapor planı bulunamadı.");
            schedule.UpdateDate = now;
            schedule.UpdateUserID = User.Identity?.Name;
        }
        else
        {
            schedule = new LogoBankMovementReportSchedule
            {
                CreateDate = now,
                CreateUserID = User.Identity?.Name,
                IsActive = true,
                IsDelete = false
            };
            _context.LogoBankMovementReportSchedules.Add(schedule);
        }

        schedule.Name = model.Name.Trim();
        schedule.CompanyIds = string.Join(',', model.CompanyIds.OrderBy(x => x));
        schedule.RecipientUserIds = string.Join(',', model.RecipientUserIds.OrderBy(x => x));
        schedule.SendTime = sendTime;
        schedule.LookbackDays = model.LookbackDays;
        schedule.IncludeToday = model.IncludeToday;
        schedule.IsEnabled = model.IsEnabled;
        await _context.SaveChangesAsync(ct);

        TempData["success"] = model.Id.HasValue ? "Rapor planı güncellendi." : "Rapor planı oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> PlanDurum(int id, CancellationToken ct)
    {
        var schedule = await _context.LogoBankMovementReportSchedules
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (schedule == null) return NotFound();
        schedule.IsEnabled = !schedule.IsEnabled;
        schedule.UpdateDate = DateTime.Now;
        schedule.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = schedule.IsEnabled ? "Rapor planı etkinleştirildi." : "Rapor planı durduruldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> PlanSil(int id, CancellationToken ct)
    {
        var schedule = await _context.LogoBankMovementReportSchedules
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (schedule == null) return NotFound();
        schedule.IsDelete = true;
        schedule.IsEnabled = false;
        schedule.DeleteDate = DateTime.Now;
        schedule.DeleteUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Rapor planı silindi. Geçmiş gönderim kayıtları korundu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> SimdiGonder(int id)
    {
        try
        {
            await _reportJob.SendScheduleAsync(id, "Manuel", User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name, updateLastRun: false);
            TempData["success"] = "Rapor seçilen portal kullanıcılarına gönderildi.";
        }
        catch (Exception ex)
        {
            TempData["error"] = "Rapor gönderilemedi: " + ex.Message;
        }
        return RedirectToAction(nameof(Index));
    }

    private async Task<List<LogoBankMovementCompanyVM>> GetCompaniesAsync(CancellationToken ct)
    {
        var raw = await _context.Sirketler.AsNoTracking()
            .Where(x => x.IsDelete != true && x.LogoTigerSirketKodu != null && x.LogoTigerSirketKodu != "")
            .Select(x => new LogoBankMovementCompanyVM
            {
                Id = x.ID,
                Name = x.SirketKisaAdi ?? x.SirketAdi ?? ("Firma " + x.ID),
                FirmNumber = x.LogoTigerSirketKodu!
            })
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        return raw.Where(x => int.TryParse(x.FirmNumber, out var firm) && firm is > 0 and <= 999).ToList();
    }

    private async Task<List<LogoBankMovementRecipientVM>> GetRecipientsAsync(CancellationToken ct) =>
        await _context.Users.AsNoTracking()
            .Where(x => x.IsActive && x.Email != null && x.Email != "")
            .OrderBy(x => x.AdSoyad)
            .Select(x => new LogoBankMovementRecipientVM { Id = x.Id, Name = x.AdSoyad, Email = x.Email! })
            .ToListAsync(ct);

    private async Task FillSchedulesAsync(LogoBankMovementReportPageVM model, CancellationToken ct)
    {
        var companyNames = model.Companies.ToDictionary(x => x.Id, x => x.Name);
        var recipientNames = model.AvailableRecipients.ToDictionary(x => x.Id, x => x.Name);
        var schedules = await _context.LogoBankMovementReportSchedules.AsNoTracking()
            .Where(x => x.IsDelete != true).OrderBy(x => x.SendTime).ThenBy(x => x.Name).ToListAsync(ct);
        model.Schedules = schedules.Select(x => new LogoBankMovementReportScheduleRowVM
        {
            Id = x.ID,
            Name = x.Name,
            Companies = string.Join(", ", LogoBankMovementReportJob.ParseIntegerIds(x.CompanyIds).Select(id => companyNames.GetValueOrDefault(id, $"#{id}"))),
            Recipients = string.Join(", ", LogoBankMovementReportJob.ParseStringIds(x.RecipientUserIds).Select(id => recipientNames.GetValueOrDefault(id, "Silinmiş kullanıcı"))),
            SendTime = x.SendTime,
            LookbackDays = x.LookbackDays,
            IncludeToday = x.IncludeToday,
            IsEnabled = x.IsEnabled,
            LastRunAt = x.LastRunAt,
            LastSuccessAt = x.LastSuccessAt,
            LastRecordCount = x.LastRecordCount,
            LastError = x.LastError
        }).ToList();

        var scheduleNames = schedules.ToDictionary(x => x.ID, x => x.Name);
        var deliveries = await _context.LogoBankMovementReportDeliveries.AsNoTracking()
            .OrderByDescending(x => x.StartedAt).Take(50).ToListAsync(ct);
        model.Deliveries = deliveries.Select(x => new LogoBankMovementReportDeliveryRowVM
            {
                Id = x.Id,
                ScheduleName = x.ScheduleId.HasValue && scheduleNames.ContainsKey(x.ScheduleId.Value)
                    ? scheduleNames[x.ScheduleId.Value] : "Silinmiş plan",
                StartedAt = x.StartedAt,
                CompletedAt = x.CompletedAt,
                CompanySummary = x.CompanySummary,
                Recipients = x.Recipients,
                TriggerType = x.TriggerType,
                RecordCount = x.RecordCount,
                IncomingTotalTry = x.IncomingTotalTry,
                OutgoingTotalTry = x.OutgoingTotalTry,
                IsSuccess = x.IsSuccess,
                ErrorMessage = x.ErrorMessage
            }).ToList();
    }
}
