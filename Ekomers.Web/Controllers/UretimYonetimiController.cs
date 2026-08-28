using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class UretimYonetimiController : Controller
{
    private const string PlanningSessionKey="PrdProductionPlanningList";
    private readonly ApplicationDbContext _context;
    public UretimYonetimiController(ApplicationDbContext context)=>_context=context;

    public IActionResult Dashboard() => ModulSayfasi("Üretim Paneli", "Üretim sürecinin genel görünümü bu ekranda yer alacak.");
    [HttpGet]
    public async Task<IActionResult> Planlama(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        return View(await BuildPlanningModel(ReadPlanningList(),ct));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> PlanlamayaEkle(int recipeVersionId,[FromForm(Name="quantity")] string quantityText,CancellationToken ct)
    {
        if(!TryParseProductionDecimal(quantityText,out var quantity)){TempData["error"]="Üretilecek miktar geçerli bir sayı olmalıdır.";return RedirectToAction(nameof(Planlama));}
        if(quantity<=0){TempData["error"]="Üretilecek miktar sıfırdan büyük olmalıdır.";return RedirectToAction(nameof(Planlama));}
        if(!await _context.PrdRecipeVersions.AnyAsync(x=>x.ID==recipeVersionId&&x.Status==PrdRecipeStatus.Active&&x.IsDelete!=true,ct)){TempData["error"]="Aktif reçete bulunamadı.";return RedirectToAction(nameof(Planlama));}
        var list=ReadPlanningList();var existing=list.FirstOrDefault(x=>x.RecipeVersionId==recipeVersionId);
        if(existing==null)list.Add(new ProductionPlanningSessionItem{RecipeVersionId=recipeVersionId,Quantity=quantity});else existing.Quantity+=quantity;
        SavePlanningList(list);return RedirectToAction(nameof(Planlama));
    }

    [HttpPost,ValidateAntiForgeryToken]
    public IActionResult PlanlamadanCikar(int recipeVersionId){var list=ReadPlanningList();list.RemoveAll(x=>x.RecipeVersionId==recipeVersionId);SavePlanningList(list);return RedirectToAction(nameof(Planlama));}

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaniKilitle(DateTime? targetProductionDate,string? notes,CancellationToken ct)
    {
        var list=ReadPlanningList();if(list.Count==0){TempData["error"]="Kilitlenecek üretim planı bulunamadı.";return RedirectToAction(nameof(Planlama));}
        if(!targetProductionDate.HasValue){TempData["error"]="Hedef üretim tarihi seçilmelidir.";return RedirectToAction(nameof(Planlama));}
        var model=await BuildPlanningModel(list,ct);
        if(model.Plans.Count!=list.Count||await _context.PrdRecipeVersions.CountAsync(x=>list.Select(s=>s.RecipeVersionId).Contains(x.ID)&&x.Status==PrdRecipeStatus.Active&&x.IsDelete!=true,ct)!=list.Count){TempData["error"]="Plan içindeki reçetelerden biri artık aktif değil. Planı kontrol edip tekrar deneyiniz.";return RedirectToAction(nameof(Planlama));}
        var now=DateTime.Now;var user=User.Identity?.Name;var prefix=$"UP-{now:yyyyMMddHHmmssfff}";
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        var header=new PrdProductionPlanHeader{PlanNumber=prefix,PlanDate=now,TargetProductionDate=targetProductionDate.Value.Date,Status=PrdProductionPlanHeaderStatus.Locked,CalculatedDate=now,LockedDate=now,LockedUserId=user,Notes=notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
        _context.PrdProductionPlanHeaders.Add(header);await _context.SaveChangesAsync(ct);
        for(var i=0;i<model.Plans.Count;i++){var line=model.Plans[i];_context.PrdProductionPlans.Add(new PrdProductionPlan{ProductionPlanHeaderId=header.ID,PlanNumber=$"{prefix}-{i+1:00}",RecipeVersionId=line.RecipeVersionId,ProductMaterialId=line.ProductMaterialId,PlannedQuantity=line.Quantity,UnitId=line.UnitId,PlannedProductionDate=targetProductionDate.Value.Date,BatchNumber=string.Empty,Status=PrdProductionPlanStatus.Approved,IsConvertedToOrder=false,Notes=notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});}
        _context.PrdProductionPlanRequirements.AddRange(model.Requirements.Select(x=>new PrdProductionPlanRequirement{ProductionPlanHeaderId=header.ID,MaterialId=x.MaterialId,UnitId=x.UnitId,TheoreticalQuantity=x.TheoreticalQuantity,PlannedWasteQuantity=x.PlannedWasteQuantity,TotalRequiredQuantity=x.RequiredQuantity,PhysicalStockQuantity=x.PhysicalStockQuantity,ReservedQuantity=x.ReservedQuantity,AvailableStockQuantity=x.AvailableStockQuantity,ShortageQuantity=x.ShortageQuantity,CalculationDate=now,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user}));
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);HttpContext.Session.Remove(PlanningSessionKey);TempData["success"]=$"{prefix} numaralı plan kilitlendi. {model.Plans.Count} ürün satırı kaydedildi; üretim emri henüz oluşturulmadı.";return RedirectToAction(nameof(Planlama));
    }
    public IActionResult Emirler() => ModulSayfasi("Üretim Emirleri", "Onaylanan üretim emirleri bu ekranda yönetilecek.");
    public IActionResult DepoHazirlama() => ModulSayfasi("Depo Hazırlama Emirleri", "Yeni Prd depo hazırlama görevleri bu ekranda yönetilecek.");
    public IActionResult Gerceklesen() => ModulSayfasi("Gerçekleşen Üretim", "Gerçek tüketim, iade, fire ve çıktı kayıtları bu ekranda yönetilecek.");
    public IActionResult Izlenebilirlik() => ModulSayfasi("Lot ve Parti İzlenebilirliği", "Hammadde lotundan mamul partisine izleme bu ekranda yapılacak.");
    public IActionResult Raporlar() => ModulSayfasi("Üretim Raporları", "Planlanan ve gerçekleşen üretim karşılaştırmaları burada yer alacak.");

    private IActionResult ModulSayfasi(string baslik, string aciklama)
    {
        ViewBag.Modul = "YeniUretim";
        ViewBag.Baslik = baslik;
        ViewBag.Aciklama = aciklama;
        return View("ModulSayfasi");
    }

    private List<ProductionPlanningSessionItem> ReadPlanningList(){var json=HttpContext.Session.GetString(PlanningSessionKey);return string.IsNullOrWhiteSpace(json)?[]:JsonSerializer.Deserialize<List<ProductionPlanningSessionItem>>(json)??[];}
    private void SavePlanningList(List<ProductionPlanningSessionItem> list)=>HttpContext.Session.SetString(PlanningSessionKey,JsonSerializer.Serialize(list));
    private static bool TryParseProductionDecimal(string? value,out decimal result)
    {
        result=0;if(string.IsNullOrWhiteSpace(value))return false;
        var normalized=value.Trim().Replace(" ",string.Empty);
        if(normalized.Contains(',')&&normalized.Contains('.'))normalized=normalized.LastIndexOf(',')>normalized.LastIndexOf('.')?normalized.Replace(".",string.Empty).Replace(',','.'):normalized.Replace(",",string.Empty);
        else if(normalized.Contains(','))normalized=normalized.Replace(',','.');
        return decimal.TryParse(normalized,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out result);
    }

    private async Task<ProductionPlanningVM> BuildPlanningModel(List<ProductionPlanningSessionItem> selected,CancellationToken ct)
    {
        var model=new ProductionPlanningVM();
        model.Recipes=await(from version in _context.PrdRecipeVersions.AsNoTracking() join recipe in _context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID join product in _context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID where version.Status==PrdRecipeStatus.Active&&version.IsDelete!=true&&recipe.IsDelete!=true&&product.IsActive!=false&&product.IsDelete!=true orderby product.Code select new SelectListItem(product.Code+" - "+product.Name+" (v"+version.VersionNumber+")",version.ID.ToString())).ToListAsync(ct);
        if(selected.Count==0)return model;
        var ids=selected.Select(x=>x.RecipeVersionId).ToList();
        var data=await(from version in _context.PrdRecipeVersions.AsNoTracking() join recipe in _context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID join product in _context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on version.UnitId equals unit.ID where ids.Contains(version.ID)&&version.IsDelete!=true select new{Version=version,Recipe=recipe,Product=product,Unit=unit}).ToListAsync(ct);
        model.Plans=data.Select(x=>new ProductionPlanningLineVM{RecipeVersionId=x.Version.ID,ProductMaterialId=x.Product.ID,UnitId=x.Version.UnitId,ProductCode=x.Product.Code,ProductName=x.Product.Name,VersionNumber=x.Version.VersionNumber,Quantity=selected.First(s=>s.RecipeVersionId==x.Version.ID).Quantity,Unit=x.Unit.Name}).ToList();
        var recipeItems=await(from item in _context.PrdRecipeItems.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on item.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID where ids.Contains(item.RecipeVersionId)&&item.IsDelete!=true&&material.IsDelete!=true select new{Item=item,Material=material,Unit=unit}).ToListAsync(ct);
        var requirements=recipeItems.Select(x=>{var theoretical=x.Item.Quantity/data.First(d=>d.Version.ID==x.Item.RecipeVersionId).Version.BaseQuantity*selected.First(s=>s.RecipeVersionId==x.Item.RecipeVersionId).Quantity;return new{x.Material,x.Unit,Theoretical=theoretical,Waste=theoretical*x.Item.PlannedWasteRate/100m};}).GroupBy(x=>new{x.Material.ID,x.Material.Code,MaterialName=x.Material.Name,x.Material.Type,UnitId=x.Unit.ID,UnitName=x.Unit.Name}).Select(g=>new{g.Key,Theoretical=g.Sum(x=>x.Theoretical),Waste=g.Sum(x=>x.Waste),Required=g.Sum(x=>x.Theoretical+x.Waste)}).ToList();
        var materialIds=requirements.Select(x=>x.Key.ID).ToList();
        var warehouseIds=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false&&(x.Type==PrdWarehouseType.Main||x.Type==PrdWarehouseType.Production)).Select(x=>x.ID).ToListAsync(ct);
        var today=DateTime.Today;
        var stocks=await(from movement in _context.PrdStockMovements.AsNoTracking() join lot0 in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot0.ID into lotJoin from lot in lotJoin.DefaultIfEmpty() where materialIds.Contains(movement.MaterialId)&&warehouseIds.Contains(movement.WarehouseId)&&movement.IsDelete!=true&&(movement.StockLotId==null||lot.ExpirationDate==null||lot.ExpirationDate>=today) group movement by new{movement.MaterialId,movement.UnitId} into g select new{g.Key.MaterialId,g.Key.UnitId,Quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity)}).ToListAsync(ct);
        var reservations=await(from reservation in _context.PrdStockReservations.AsNoTracking() join lot in _context.PrdStockLots.AsNoTracking() on reservation.StockLotId equals lot.ID where materialIds.Contains(reservation.MaterialId)&&warehouseIds.Contains(reservation.WarehouseId)&&reservation.IsDelete!=true&&(reservation.Status==PrdReservationStatus.Active||reservation.Status==PrdReservationStatus.PartiallyUsed)&&(lot.ExpirationDate==null||lot.ExpirationDate>=today) group reservation by reservation.MaterialId into g select new{MaterialId=g.Key,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToListAsync(ct);
        model.Requirements=requirements.Select(x=>{var physical=stocks.Where(s=>s.MaterialId==x.Key.ID&&s.UnitId==x.Key.UnitId).Sum(s=>s.Quantity);var reserved=reservations.Where(s=>s.MaterialId==x.Key.ID).Sum(s=>s.Quantity);return new ProductionRequirementLineVM{MaterialId=x.Key.ID,Code=x.Key.Code,Name=x.Key.MaterialName,Type=x.Key.Type,UnitId=x.Key.UnitId,TheoreticalQuantity=x.Theoretical,PlannedWasteQuantity=x.Waste,RequiredQuantity=x.Required,PhysicalStockQuantity=physical,ReservedQuantity=reserved,AvailableStockQuantity=Math.Max(0,physical-reserved),Unit=x.Key.UnitName};}).OrderBy(x=>x.Type).ThenBy(x=>x.Code).ToList();
        return model;
    }
}
