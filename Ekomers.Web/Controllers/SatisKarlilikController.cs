using Ekomers.Data.Services.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin,Yönetici,Rapor")]
public sealed class SatisKarlilikController : Controller
{
    private readonly ISalesProfitabilityReportService _reportService;
    private readonly ICustomerReceivablesService _receivablesService;
    private readonly IChannelBalancingService _channelBalancingService;

    public SatisKarlilikController(
        ISalesProfitabilityReportService reportService,
        ICustomerReceivablesService receivablesService,
        IChannelBalancingService channelBalancingService)
    {
        _reportService = reportService;
        _receivablesService = receivablesService;
        _channelBalancingService = channelBalancingService;
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

    [HttpGet]
    public async Task<IActionResult> Receivables(string? search, CancellationToken ct = default)
    {
        var model = await _receivablesService.GetSummaryAsync(search, ct);
        return PartialView("_Receivables", model);
    }

    [HttpGet]
    public async Task<IActionResult> ReceivableDetail(
        string customerCode,
        CancellationToken ct = default)
    {
        var model = await _receivablesService.GetDetailAsync(customerCode, ct);
        return PartialView("_ReceivableDetail", model);
    }

    [HttpGet]
    public async Task<IActionResult> ChannelBalance(
        DateTime? startDate,
        DateTime? endDate,
        string? search,
        string? priceStatus,
        CancellationToken ct = default)
    {
        var today = DateTime.Today;
        var model = await _channelBalancingService.GetAsync(
            new SalesProfitabilityFilterVM
            {
                StartDate = startDate ?? today.AddDays(-30),
                EndDate = endDate ?? today,
                Search = search,
                PriceStatus = priceStatus
            },
            ct);
        return PartialView("_ChannelBalance", model);
    }
}
