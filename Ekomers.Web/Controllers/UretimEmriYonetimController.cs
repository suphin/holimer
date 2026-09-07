using Ekomers.Data.Services;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class UretimEmriYonetimController : Controller
{
    private readonly IProductionOrderCleanupService _cleanupService;

    public UretimEmriYonetimController(IProductionOrderCleanupService cleanupService) => _cleanupService = cleanupService;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, PrdProductionOrderStatus? status, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        return View(await _cleanupService.GetOrdersAsync(search, status, ct));
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await _cleanupService.GetPreviewAsync(id, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, string? confirmationOrderNumber, string? deleteReason, bool confirmStockRollback, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var result = await _cleanupService.DeleteAsync(id, confirmationOrderNumber, deleteReason, confirmStockRollback, User.Identity?.Name, ct);
        if (result.Succeeded)
        {
            TempData["success"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        var model = await _cleanupService.GetPreviewAsync(id, ct);
        if (model == null)
        {
            TempData["error"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        model.ConfirmationOrderNumber = confirmationOrderNumber;
        model.DeleteReason = deleteReason;
        model.ConfirmStockRollback = confirmStockRollback;
        ModelState.AddModelError(string.Empty, result.Message);
        return View(nameof(Detay), model);
    }
}
