using Ekomers.Data.Services.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin,Yönetici,Rapor")]
public sealed class SatisKarlilikController : Controller
{
    private readonly ISalesProfitabilityReportService _reportService;

    public SatisKarlilikController(ISalesProfitabilityReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        string? priceStatus,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "Rapor";
        var today = DateTime.Today;
        var filter = new SalesProfitabilityFilterVM
        {
            StartDate = startDate ?? today.AddDays(-30),
            EndDate = endDate ?? today,
            Search = search,
            PriceStatus = priceStatus,
            Page = page,
            PageSize = pageSize
        };

        var model = await _reportService.GetPreviewAsync(filter, ct);
        return View(model);
    }
}
