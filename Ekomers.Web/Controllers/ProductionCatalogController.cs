using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

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
    public async Task<IActionResult> Index(string? search, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        ViewBag.Modul = "YeniUretim";
        search=search?.Trim();page=Math.Max(1,page);pageSize=new[]{25,50,100}.Contains(pageSize)?pageSize:50;
        var query = from material in _context.PrdMaterials.AsNoTracking()
                           join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                           where material.IsDelete != true && (string.IsNullOrEmpty(search) || EF.Functions.Like(material.Code,"%"+search+"%") || EF.Functions.Like(material.Name,"%"+search+"%"))
                           select new ProductionCatalogItemVM
                           {
                               Id = material.ID, Code = material.Code, Name = material.Name, Source = material.Source,
                               Type = material.Type, Unit = unit.Name, LogoActive = material.LogoActive,
                               LogoLastSyncDate = material.LogoLastSyncDate, IsActive = material.IsActive != false
                           };
        var totalCount=await query.CountAsync(cancellationToken);
        var totalPages=Math.Max(1,(int)Math.Ceiling(totalCount/(double)pageSize));page=Math.Min(page,totalPages);
        var items=await query.OrderBy(x=>x.Code).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var model=new ProductionCatalogIndexVM{Search=search,Page=page,PageSize=pageSize,TotalCount=totalCount,Items=items};
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var model = new ProductionCatalogCreateVM();
        await FillUnits(model, cancellationToken);
        if (model.UnitId <= 0 && model.Units.Count > 0)
            model.UnitId = int.Parse(model.Units[0].Value, CultureInfo.InvariantCulture);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductionCatalogCreateVM model, CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var code = (model.Code ?? string.Empty).Trim().ToUpperInvariant();
        var name = (model.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(code))
            ModelState.AddModelError(nameof(model.Code), "Malzeme kodu zorunludur.");
        else if (await _context.PrdMaterials.AnyAsync(x => x.Code == code, cancellationToken))
            ModelState.AddModelError(nameof(model.Code), "Bu malzeme kodu katalogda zaten kayıtlıdır.");

        if (string.IsNullOrWhiteSpace(name))
            ModelState.AddModelError(nameof(model.Name), "Malzeme adı zorunludur.");
        if (!Enum.IsDefined(model.Type))
            ModelState.AddModelError(nameof(model.Type), "Geçerli bir malzeme türü seçiniz.");
        if (!Enum.IsDefined(model.QualityControlRequirement))
            ModelState.AddModelError(nameof(model.QualityControlRequirement), "Geçerli bir kalite kontrol seçeneği seçiniz.");
        if (!await _context.PrdUnits.AnyAsync(x => x.ID == model.UnitId && x.IsDelete != true && x.IsActive != false, cancellationToken))
            ModelState.AddModelError(nameof(model.UnitId), "Seçilen birim bulunamadı.");

        decimal? criticalQuantity = null;
        if (!string.IsNullOrWhiteSpace(model.CriticalQuantity))
        {
            if (!TryParseDecimal(model.CriticalQuantity, out var parsed) || parsed < 0)
                ModelState.AddModelError(nameof(model.CriticalQuantity), "Kritik stok miktarı sıfır veya daha büyük geçerli bir sayı olmalıdır.");
            else
                criticalQuantity = parsed;
        }

        if (!ModelState.IsValid)
        {
            model.Code = code;
            model.Name = name;
            await FillUnits(model, cancellationToken);
            return View(model);
        }

        var now = DateTime.Now;
        var material = new PrdMaterial
        {
            Code = code,
            Name = name,
            Source = PrdMaterialSource.QuickCode,
            Type = model.Type,
            UnitId = model.UnitId,
            Description = model.Description?.Trim(),
            RequiresLotTracking = model.RequiresLotTracking,
            RequiresExpirationDate = model.RequiresExpirationDate,
            QualityControlRequirement = model.QualityControlRequirement,
            CriticalQuantity = criticalQuantity,
            LogoActive = false,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = User.Identity?.Name
        };
        _context.PrdMaterials.Add(material);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"] = $"{material.Code} kodlu malzeme kataloğa eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var material = await _context.PrdMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true,cancellationToken);
        if (material == null) return NotFound();
        var model = new ProductionCatalogEditVM { Id=material.ID, Code=material.Code, Name=material.Name, Source=material.Source, Type=material.Type, UnitId=material.UnitId, Description=material.Description, RequiresLotTracking=material.RequiresLotTracking, RequiresExpirationDate=material.RequiresExpirationDate, QualityControlRequirement=material.QualityControlRequirement, CriticalQuantity=material.CriticalQuantity?.ToString("0.######",CultureInfo.GetCultureInfo("tr-TR")), IsActive=material.IsActive != false };
        await FillUnits(model, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProductionCatalogEditVM model, CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var material = await _context.PrdMaterials.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, cancellationToken);
        if (material == null) return NotFound();
        if (!await _context.PrdUnits.AnyAsync(x => x.ID == model.UnitId && x.IsDelete != true && x.IsActive != false, cancellationToken))
            ModelState.AddModelError(nameof(model.UnitId), "Seçilen birim bulunamadı.");
        decimal? criticalQuantity=null;
        if(!string.IsNullOrWhiteSpace(model.CriticalQuantity))
        {
            if(!TryParseDecimal(model.CriticalQuantity,out var parsed)||parsed<0)ModelState.AddModelError(nameof(model.CriticalQuantity),"Kritik stok miktarı sıfır veya daha büyük geçerli bir sayı olmalıdır.");
            else criticalQuantity=parsed;
        }
        if (!ModelState.IsValid) { model.Code=material.Code; model.Source=material.Source; model.IsActive=material.IsActive != false; await FillUnits(model,cancellationToken); return View(model); }
        material.Name=model.Name.Trim(); material.Type=model.Type; material.UnitId=model.UnitId; material.Description=model.Description?.Trim();material.RequiresLotTracking=model.RequiresLotTracking;material.RequiresExpirationDate=model.RequiresExpirationDate;material.QualityControlRequirement=model.QualityControlRequirement;material.CriticalQuantity=criticalQuantity;
        material.UpdateDate=DateTime.Now; material.UpdateUserID=User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"]="Malzeme kartı güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(int id, CancellationToken cancellationToken)
    {
        var material=await _context.PrdMaterials.FirstOrDefaultAsync(x=>x.ID==id && x.IsDelete!=true,cancellationToken);
        if(material==null)return NotFound();
        material.IsActive=material.IsActive==false; material.UpdateDate=DateTime.Now; material.UpdateUserID=User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"]=material.IsActive==true ? "Malzeme yeniden aktifleştirildi." : "Malzeme pasife alındı.";
        return RedirectToAction(nameof(Index));
    }

    private async Task FillUnits(ProductionCatalogEditVM model,CancellationToken ct)
    {
        model.Units = await BuildUnitItems(model.UnitId, ct);
    }

    private async Task FillUnits(ProductionCatalogCreateVM model,CancellationToken ct)
    {
        model.Units = await BuildUnitItems(model.UnitId, ct);
    }

    private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> BuildUnitItems(int selectedUnitId, CancellationToken ct)
    {
        var units = await _context.PrdUnits.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false).ToListAsync(ct);
        return units
            .GroupBy(x => ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var unit = group.FirstOrDefault(x => x.ID == selectedUnitId)
                    ?? group.OrderByDescending(x => x.Code.Equals(group.Key, StringComparison.OrdinalIgnoreCase)).ThenBy(x => x.ID).First();
                return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(ProductionUnitNormalizer.DisplayName(group.Key, unit.Name), unit.ID.ToString());
            })
            .OrderBy(x => x.Text)
            .ToList();
    }

    private static bool TryParseDecimal(string? value,out decimal result)
    {
        result=0;if(string.IsNullOrWhiteSpace(value))return false;
        var normalized=value.Trim().Replace(" ",string.Empty);
        if(normalized.Contains(',')&&normalized.Contains('.'))normalized=normalized.LastIndexOf(',')>normalized.LastIndexOf('.')?normalized.Replace(".",string.Empty).Replace(',','.'):normalized.Replace(",",string.Empty);
        else if(normalized.Contains(','))normalized=normalized.Replace(',','.');
        return decimal.TryParse(normalized,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out result);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sync(CancellationToken cancellationToken)
    {
        var result = await _syncService.SyncAsync(User.Identity?.Name, cancellationToken);
        TempData["success"] = $"Logo aktarımı tamamlandı. {result.UnitAdded} birim, {result.MaterialAdded} malzeme eklendi; {result.MaterialUpdated} Logo kartı güncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
