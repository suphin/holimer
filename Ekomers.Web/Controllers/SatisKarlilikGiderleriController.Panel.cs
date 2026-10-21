using Ekomers.Data.Services.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

public sealed partial class SatisKarlilikGiderleriController
{
    [HttpGet]
    public async Task<IActionResult> Panel(DateTime? startDate = null, DateTime? endDate = null, string category = "all", string behavior = "all", string? search = null, bool showEmpty = false, CancellationToken ct = default)
    {
        ViewBag.Modul = "Rapor";
        var today = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")).Date;
        ViewBag.PanelToday = today;
        var filter = new AccountingExpenseDashboardFilterVM { StartDate = (startDate ?? new DateTime(today.Year, today.Month, 1)).Date,
            EndDate = (endDate ?? today).Date, Category = string.IsNullOrEmpty(category) ? "all" : category,
            Behavior = string.IsNullOrEmpty(behavior) ? "all" : behavior, Search = search?.Trim() ?? "", ShowEmpty = showEmpty };
        if (!ModelState.IsValid || !AccountingExpenseDashboard.ValidFilter(filter))
            return View(new AccountingExpenseDashboardVM { Filter = filter, Error = "Geçerli tarih/kategori/tür seçiniz. Başlangıç bitişten sonra olamaz; en fazla 36 takvim ayı ve 150 karakter arama kullanılabilir." });
        try
        {
            var summary = await expenses.GetSummaryAsync(filter.StartDate, filter.EndDate, ct);
            var groups = await context.RptAccountingExpenseGroups.AsNoTracking().Where(g => g.IsDelete != true && g.IsActive != false).ToListAsync(ct);
            return View(AccountingExpenseDashboard.Build(summary, groups, filter));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            HttpContext.RequestServices.GetRequiredService<ILogger<SatisKarlilikGiderleriController>>()
                .LogError(ex, "Yönetici gider paneli okunamadı; {Start} – {End}", filter.StartDate, filter.EndDate);
            return View(new AccountingExpenseDashboardVM { Filter = filter, Error = "Muhasebe giderleri okunamadı. Logo bağlantısını ve gider grup tanımlarını kontrol edip tekrar deneyiniz." });
        }
    }
}
