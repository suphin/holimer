using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class ProductionCatalogController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ProductionCatalogSyncService _syncService;

    public ProductionCatalogController(ApplicationDbContext context, ProductionCatalogSyncService syncService)
    {
        _context = context;
        _syncService = syncService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await (from material in _context.PrdMaterials.AsNoTracking()
                           join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                           where material.IsDelete != true
                           orderby material.Code
                           select new ProductionCatalogItemVM
                           {
                               Id = material.ID, Code = material.Code, Name = material.Name, Source = material.Source,
                               Type = material.Type, Unit = unit.Name, LogoActive = material.LogoActive,
                               LogoLastSyncDate = material.LogoLastSyncDate
                           }).Take(2000).ToListAsync(cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var result = await _syncService.SyncAsync(User.Identity?.Name, cancellationToken);
        TempData["success"] = $"Aktarım tamamlandı. {result.UnitAdded} birim, {result.MaterialAdded} malzeme eklendi; {result.MaterialUpdated} Logo kartı güncellendi. Hızlı kod: {result.QuickCodeAdded}.";
        return RedirectToAction(nameof(Index));
    }
}
