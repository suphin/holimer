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
    private const string EditingPlanHeaderSessionKey="PrdEditingPlanHeaderId";
    private static readonly bool LegacyStockImportEnabled=true;
    private readonly ApplicationDbContext _context;
    public UretimYonetimiController(ApplicationDbContext context)=>_context=context;

    public IActionResult Dashboard() => ModulSayfasi("Üretim Paneli", "Üretim sürecinin genel görünümü bu ekranda yer alacak.");

    [HttpGet]
    public async Task<IActionResult> Depolar(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true).OrderBy(x=>x.Type).ThenBy(x=>x.Code).Select(x=>new WarehouseListVM{Id=x.ID,Code=x.Code,Name=x.Name,Type=x.Type,Description=x.Description,IsActive=x.IsActive!=false,LotCount=_context.PrdStockLots.Count(l=>l.WarehouseId==x.ID&&l.IsDelete!=true),MovementCount=_context.PrdStockMovements.Count(m=>m.WarehouseId==x.ID&&m.IsDelete!=true)}).ToListAsync(ct);
        return View(model);
    }

    [HttpGet,Authorize(Roles="Admin")]
    public IActionResult DepoYeni()
    {
        ViewBag.Modul="YeniUretim";FillWarehouseTypes();return View("DepoForm",new WarehouseFormVM{IsActive=true});
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public async Task<IActionResult> DepoYeni(WarehouseFormVM model,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";NormalizeWarehouseForm(model);await ValidateWarehouseForm(model,null,ct);
        if(!ModelState.IsValid){FillWarehouseTypes();return View("DepoForm",model);}
        var now=DateTime.Now;_context.PrdWarehouses.Add(new PrdWarehouse{Code=model.Code,Name=model.Name,Type=model.Type,Description=model.Description,IsActive=model.IsActive,IsDelete=false,CreateDate=now,CreateUserID=User.Identity?.Name});
        await _context.SaveChangesAsync(ct);TempData["success"]=$"{model.Code} kodlu depo oluşturuldu.";return RedirectToAction(nameof(Depolar));
    }

    [HttpGet,Authorize(Roles="Admin")]
    public async Task<IActionResult> DepoDuzenle(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";var model=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.ID==id&&x.IsDelete!=true).Select(x=>new WarehouseFormVM{Id=x.ID,Code=x.Code,Name=x.Name,Type=x.Type,Description=x.Description,IsActive=x.IsActive!=false}).FirstOrDefaultAsync(ct);if(model==null)return NotFound();
        FillWarehouseTypes();return View("DepoForm",model);
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public async Task<IActionResult> DepoDuzenle(WarehouseFormVM model,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";var warehouse=await _context.PrdWarehouses.FirstOrDefaultAsync(x=>x.ID==model.Id&&x.IsDelete!=true,ct);if(warehouse==null)return NotFound();
        NormalizeWarehouseForm(model);await ValidateWarehouseForm(model,model.Id,ct);
        if(warehouse.Type!=model.Type)
        {
            var isUsed=await _context.PrdStockMovements.AnyAsync(x=>x.WarehouseId==model.Id&&x.IsDelete!=true,ct)||await _context.PrdProductionOrders.AnyAsync(x=>(x.SourceWarehouseId==model.Id||x.ProductionWarehouseId==model.Id)&&x.IsDelete!=true,ct)||await _context.PrdWarehouseTasks.AnyAsync(x=>(x.SourceWarehouseId==model.Id||x.TargetWarehouseId==model.Id)&&x.IsDelete!=true,ct);
            if(isUsed)ModelState.AddModelError(nameof(model.Type),"Hareket veya üretim kaydı bulunan deponun türü değiştirilemez.");
        }
        if(!ModelState.IsValid){FillWarehouseTypes();return View("DepoForm",model);}
        warehouse.Code=model.Code;warehouse.Name=model.Name;warehouse.Type=model.Type;warehouse.Description=model.Description;warehouse.IsActive=model.IsActive;warehouse.UpdateDate=DateTime.Now;warehouse.UpdateUserID=User.Identity?.Name;
        await _context.SaveChangesAsync(ct);TempData["success"]=$"{model.Code} kodlu depo güncellendi.";return RedirectToAction(nameof(Depolar));
    }

    [HttpGet]
    public async Task<IActionResult> Stoklar(string? code,string? name,int? warehouseId,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        code=code?.Trim();name=name?.Trim();
        var model=new ProductionStockReportVM{Code=code,Name=name,WarehouseId=warehouseId};
        model.Warehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false).OrderBy(x=>x.Type).ThenBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        var rows=await(from movement in _context.PrdStockMovements.AsNoTracking()
                       join material in _context.PrdMaterials.AsNoTracking() on movement.MaterialId equals material.ID
                       join warehouse in _context.PrdWarehouses.AsNoTracking() on movement.WarehouseId equals warehouse.ID
                       join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID
                       join lot0 in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot0.ID into lotJoin
                       from lot in lotJoin.DefaultIfEmpty()
                       where movement.IsDelete!=true&&material.IsDelete!=true&&warehouse.IsDelete!=true
                         &&(!warehouseId.HasValue||movement.WarehouseId==warehouseId.Value)
                         &&(string.IsNullOrEmpty(code)||EF.Functions.Like(material.Code,"%"+code+"%"))
                         &&(string.IsNullOrEmpty(name)||EF.Functions.Like(material.Name,"%"+name+"%"))
                       select new{movement.MaterialId,movement.WarehouseId,MaterialCode=material.Code,MaterialName=material.Name,material.CriticalQuantity,WarehouseCode=warehouse.Code,WarehouseName=warehouse.Name,LotNumber=lot==null?string.Empty:lot.LotNumber,ExpirationDate=lot==null?(DateTime?)null:lot.ExpirationDate,movement.Direction,movement.Quantity,movement.TotalCost,Unit=unit.Name}).ToListAsync(ct);
        model.Items=rows.GroupBy(x=>new{x.MaterialId,x.WarehouseId,x.MaterialCode,x.MaterialName,x.CriticalQuantity,x.WarehouseCode,x.WarehouseName,x.Unit}).Select(g=>
        {
            var incoming=g.Where(x=>x.Direction==PrdStockDirection.In).Sum(x=>x.Quantity);var outgoing=g.Where(x=>x.Direction==PrdStockDirection.Out).Sum(x=>x.Quantity);var remaining=incoming-outgoing;var totalCost=g.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost);
            return new ProductionStockReportItemVM{MaterialId=g.Key.MaterialId,WarehouseId=g.Key.WarehouseId,MaterialCode=g.Key.MaterialCode,MaterialName=g.Key.MaterialName,CriticalQuantity=g.Key.CriticalQuantity,WarehouseCode=g.Key.WarehouseCode,WarehouseName=g.Key.WarehouseName,Unit=g.Key.Unit,IncomingQuantity=incoming,OutgoingQuantity=outgoing,RemainingQuantity=remaining,TotalCost=totalCost,UnitCost=remaining==0?0:totalCost/remaining};
        }).Where(x=>x.RemainingQuantity!=0).OrderBy(x=>x.WarehouseCode).ThenBy(x=>x.MaterialCode).ToList();
        model.UnitSummaries=model.Items.GroupBy(x=>x.Unit).Select(g=>new ProductionStockUnitSummaryVM{Unit=g.Key,Quantity=g.Sum(x=>x.RemainingQuantity)}).OrderBy(x=>x.Unit).ToList();
        model.Lots=rows.GroupBy(x=>new{x.MaterialId,x.MaterialCode,x.MaterialName,x.WarehouseCode,x.WarehouseName,x.LotNumber,x.ExpirationDate,x.Unit}).Select(g=>{var quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity);var totalCost=g.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost);return new ProductionStockBalanceVM{MaterialId=g.Key.MaterialId,MaterialCode=g.Key.MaterialCode,MaterialName=g.Key.MaterialName,WarehouseCode=g.Key.WarehouseCode,WarehouseName=g.Key.WarehouseName,LotNumber=g.Key.LotNumber,ExpirationDate=g.Key.ExpirationDate,Unit=g.Key.Unit,Quantity=quantity,TotalCost=totalCost,UnitCost=quantity==0?0:totalCost/quantity};}).Where(x=>x.Quantity!=0).OrderBy(x=>x.WarehouseCode).ThenBy(x=>x.MaterialCode).ThenBy(x=>x.ExpirationDate).ToList();
        return View(model);
    }

    [HttpGet,Authorize(Roles="Admin")]
    public async Task<IActionResult> MevcutStokAktar(int? legacyWarehouseId,int? targetWarehouseId,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";ViewBag.ImportEnabled=LegacyStockImportEnabled;return View(await BuildLegacyStockImportModel(legacyWarehouseId,targetWarehouseId,ct));
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public async Task<IActionResult> MevcutStokAktar(int legacyWarehouseId,int targetWarehouseId,bool confirmImport,CancellationToken ct)
    {
        if(!LegacyStockImportEnabled){TempData["error"]="Mevcut stok aktarımı şu anda kapalıdır.";return RedirectToAction(nameof(Stoklar));}
        ViewBag.Modul="YeniUretim";ViewBag.ImportEnabled=LegacyStockImportEnabled;var model=await BuildLegacyStockImportModel(legacyWarehouseId,targetWarehouseId,ct);
        if(!confirmImport)ModelState.AddModelError(string.Empty,"Aktarım onay kutusunu işaretleyiniz.");
        if(model.AlreadyImported)ModelState.AddModelError(string.Empty,"Bu eski depo, seçilen yeni depoya daha önce aktarılmış.");
        if(model.ImportableCount==0)ModelState.AddModelError(string.Empty,"Malzeme ve maliyet bilgisi eşleşen, aktarılabilir stok kalemi bulunamadı.");
        if(!ModelState.IsValid)return View(model);
        var target=await _context.PrdWarehouses.FirstOrDefaultAsync(x=>x.ID==targetWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct);if(target==null){ModelState.AddModelError(string.Empty,"Hedef depo bulunamadı.");return View(model);}
        var matched=model.Lines.Where(x=>x.PrdMaterialId.HasValue&&x.UnitCost.HasValue&&x.Quantity>0).ToList();var materialIds=matched.Select(x=>x.PrdMaterialId!.Value).Distinct().ToList();var materials=await _context.PrdMaterials.Where(x=>materialIds.Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        var existingLots=await _context.PrdStockLots.Where(x=>x.WarehouseId==targetWarehouseId&&materialIds.Contains(x.MaterialId)&&x.IsDelete!=true).ToListAsync(ct);var now=DateTime.Now;var user=User.Identity?.Name;var documentNumber=$"DEVIR-{legacyWarehouseId}-{targetWarehouseId}-{now:yyyyMMddHHmmss}";
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        var inventoryDocument=new PrdInventoryDocument{DocumentNumber=documentNumber,Type=PrdInventoryDocumentType.Opening,Status=PrdInventoryDocumentStatus.Posted,DocumentDate=now.Date,PostingDate=now,PostedUserId=user,TargetWarehouseId=targetWarehouseId,CurrencyCode="TRY",ExchangeRate=1,TotalCost=matched.Sum(x=>x.TotalCost??0),SourceDocumentType="LegacyWarehouse",SourceDocumentId=legacyWarehouseId,Notes="Eski portal stoklarından maliyetli başlangıç aktarımı",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
        _context.PrdInventoryDocuments.Add(inventoryDocument);
        var lotMap=new Dictionary<string,PrdStockLot>(StringComparer.OrdinalIgnoreCase);
        foreach(var existing in existingLots)lotMap[$"{existing.MaterialId}|{existing.LotNumber}"]=existing;
        foreach(var line in matched)
        {
            var materialId=line.PrdMaterialId!.Value;var lotNumber=string.IsNullOrWhiteSpace(line.LotNumber)?$"DEVIR-{line.LegacyMaterialId}":line.LotNumber.Trim();if(lotNumber.Length>100)lotNumber=lotNumber[..100];var key=$"{materialId}|{lotNumber}";
            if(!lotMap.ContainsKey(key)){var lot=new PrdStockLot{MaterialId=materialId,WarehouseId=targetWarehouseId,LotNumber=lotNumber,ExpirationDate=line.ExpirationDate,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdStockLots.Add(lot);lotMap[key]=lot;}
        }
        foreach(var materialGroup in matched.GroupBy(x=>x.PrdMaterialId!.Value))
        {
            var material=materials[materialGroup.Key];var critical=materialGroup.Select(x=>x.CriticalQuantity).FirstOrDefault(x=>x.HasValue&&x.Value>0);
            if(!material.CriticalQuantity.HasValue&&critical.HasValue)material.CriticalQuantity=critical.Value;
        }
        await _context.SaveChangesAsync(ct);
        var sequence=0;var documentLines=new List<(LegacyStockImportLineVM Source,PrdInventoryDocumentLine Line)>();
        foreach(var source in matched)
        {
            var materialId=source.PrdMaterialId!.Value;var lotNumber=string.IsNullOrWhiteSpace(source.LotNumber)?$"DEVIR-{source.LegacyMaterialId}":source.LotNumber.Trim();if(lotNumber.Length>100)lotNumber=lotNumber[..100];var lot=lotMap[$"{materialId}|{lotNumber}"];
            var line=new PrdInventoryDocumentLine{InventoryDocumentId=inventoryDocument.ID,Sequence=++sequence,MaterialId=materialId,UnitId=materials[materialId].UnitId,TargetStockLotId=lot.ID,LotNumber=lotNumber,ExpirationDate=source.ExpirationDate,Quantity=source.Quantity,OriginalUnitCost=source.UnitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=source.UnitCost!.Value,TotalCost=source.TotalCost!.Value,CostSource=PrdStockCostSource.LegacyImport,Notes=source.CostSource,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
            _context.PrdInventoryDocumentLines.Add(line);documentLines.Add((source,line));
        }
        await _context.SaveChangesAsync(ct);
        foreach(var entry in documentLines)
        {
            var source=entry.Source;var line=entry.Line;var materialId=source.PrdMaterialId!.Value;
            _context.PrdStockMovements.Add(new PrdStockMovement{InventoryDocumentId=inventoryDocument.ID,InventoryDocumentLineId=line.ID,MaterialId=materialId,WarehouseId=targetWarehouseId,StockLotId=line.TargetStockLotId,Direction=PrdStockDirection.In,MovementType=PrdStockMovementType.Opening,Quantity=source.Quantity,UnitId=materials[materialId].UnitId,OriginalUnitCost=source.UnitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=source.UnitCost!.Value,TotalCost=source.TotalCost!.Value,CostSource=PrdStockCostSource.LegacyImport,MovementDate=now,DocumentNumber=documentNumber,DocumentType=PrdStockDocumentType.InventoryDocument,DocumentId=inventoryDocument.ID,Description="Mevcut portal stoklarından maliyetli başlangıç aktarımı",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
        }
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]=$"{matched.Count} lot/stok bakiyesi {target.Code} deposuna {inventoryDocument.TotalCost:N2} ₺ değerle aktarıldı. Eşleşmeyen {model.UnmatchedCount}, maliyeti bulunamayan {model.MissingCostCount} kalem aktarılmadı.";return RedirectToAction(nameof(Stoklar));
    }
    [HttpGet]
    public async Task<IActionResult> Planlama(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await BuildPlanningModel(ReadPlanningList(),ct);
        if(int.TryParse(HttpContext.Session.GetString(EditingPlanHeaderSessionKey),out var headerId))
        {
            var header=await _context.PrdProductionPlanHeaders.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==headerId&&x.Status==PrdProductionPlanHeaderStatus.Draft&&x.IsDelete!=true,ct);
            if(header!=null){model.EditingPlanHeaderId=header.ID;model.EditingPlanNumber=header.PlanNumber;model.TargetProductionDate=header.TargetProductionDate;model.Notes=header.Notes;}else HttpContext.Session.Remove(EditingPlanHeaderSessionKey);
        }
        return View(model);
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
        PrdProductionPlanHeader header;
        if(int.TryParse(HttpContext.Session.GetString(EditingPlanHeaderSessionKey),out var editingHeaderId))
        {
            header=await _context.PrdProductionPlanHeaders.FirstOrDefaultAsync(x=>x.ID==editingHeaderId&&x.Status==PrdProductionPlanHeaderStatus.Draft&&x.IsDelete!=true,ct)??throw new InvalidOperationException("Düzenlenen taslak plan bulunamadı.");
            prefix=header.PlanNumber;var oldLines=await _context.PrdProductionPlans.Where(x=>x.ProductionPlanHeaderId==header.ID&&x.IsDelete!=true).ToListAsync(ct);var oldRequirements=await _context.PrdProductionPlanRequirements.Where(x=>x.ProductionPlanHeaderId==header.ID).ToListAsync(ct);
            foreach(var old in oldLines){old.IsDelete=true;old.IsActive=false;old.DeleteDate=now;old.DeleteUserID=user;}_context.PrdProductionPlanRequirements.RemoveRange(oldRequirements);
            header.TargetProductionDate=targetProductionDate.Value.Date;header.Status=PrdProductionPlanHeaderStatus.Locked;header.CalculatedDate=now;header.LockedDate=now;header.LockedUserId=user;header.Notes=notes?.Trim();header.UpdateDate=now;header.UpdateUserID=user;
        }
        else
        {
            header=new PrdProductionPlanHeader{PlanNumber=prefix,PlanDate=now,TargetProductionDate=targetProductionDate.Value.Date,Status=PrdProductionPlanHeaderStatus.Locked,CalculatedDate=now,LockedDate=now,LockedUserId=user,Notes=notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
            _context.PrdProductionPlanHeaders.Add(header);await _context.SaveChangesAsync(ct);
        }
        var lineSuffix=now.ToString("HHmmssfff");
        for(var i=0;i<model.Plans.Count;i++){var line=model.Plans[i];_context.PrdProductionPlans.Add(new PrdProductionPlan{ProductionPlanHeaderId=header.ID,PlanNumber=$"{prefix}-{lineSuffix}-{i+1:00}",RecipeVersionId=line.RecipeVersionId,ProductMaterialId=line.ProductMaterialId,PlannedQuantity=line.Quantity,UnitId=line.UnitId,PlannedProductionDate=targetProductionDate.Value.Date,BatchNumber=string.Empty,Status=PrdProductionPlanStatus.Approved,IsConvertedToOrder=false,Notes=notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});}
        _context.PrdProductionPlanRequirements.AddRange(model.Requirements.Select(x=>new PrdProductionPlanRequirement{ProductionPlanHeaderId=header.ID,MaterialId=x.MaterialId,UnitId=x.UnitId,TheoreticalQuantity=x.TheoreticalQuantity,PlannedWasteQuantity=x.PlannedWasteQuantity,TotalRequiredQuantity=x.RequiredQuantity,PhysicalStockQuantity=x.PhysicalStockQuantity,ReservedQuantity=x.ReservedQuantity,AvailableStockQuantity=x.AvailableStockQuantity,ShortageQuantity=x.ShortageQuantity,CalculationDate=now,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user}));
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);HttpContext.Session.Remove(PlanningSessionKey);HttpContext.Session.Remove(EditingPlanHeaderSessionKey);TempData["success"]=$"{prefix} numaralı plan kilitlendi. {model.Plans.Count} ürün satırı kaydedildi; üretim emri henüz oluşturulmadı.";return RedirectToAction(nameof(PlanDetay),new{id=header.ID});
    }

    [HttpGet]
    public async Task<IActionResult> Planlar(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await _context.PrdProductionPlanHeaders.AsNoTracking().Where(x=>x.IsDelete!=true).OrderByDescending(x=>x.PlanDate).Select(x=>new ProductionPlanListVM{Id=x.ID,PlanNumber=x.PlanNumber,PlanDate=x.PlanDate,TargetProductionDate=x.TargetProductionDate,Status=x.Status,ProductCount=_context.PrdProductionPlans.Count(p=>p.ProductionPlanHeaderId==x.ID&&p.IsDelete!=true),RequirementCount=_context.PrdProductionPlanRequirements.Count(r=>r.ProductionPlanHeaderId==x.ID&&r.IsDelete!=true),TotalShortageQuantity=_context.PrdProductionPlanRequirements.Where(r=>r.ProductionPlanHeaderId==x.ID&&r.IsDelete!=true).Sum(r=>(decimal?)r.ShortageQuantity)??0,Notes=x.Notes}).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> PlanDetay(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await _context.PrdProductionPlanHeaders.AsNoTracking().Where(x=>x.ID==id&&x.IsDelete!=true).Select(x=>new ProductionPlanDetailVM{Id=x.ID,PlanNumber=x.PlanNumber,PlanDate=x.PlanDate,TargetProductionDate=x.TargetProductionDate,Status=x.Status,CalculatedDate=x.CalculatedDate,LockedDate=x.LockedDate,LockedUserId=x.LockedUserId,Notes=x.Notes}).FirstOrDefaultAsync(ct);
        if(model==null)return NotFound();
        model.Lines=await(from line in _context.PrdProductionPlans.AsNoTracking() join product in _context.PrdMaterials.AsNoTracking() on line.ProductMaterialId equals product.ID join version in _context.PrdRecipeVersions.AsNoTracking() on line.RecipeVersionId equals version.ID join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID where line.ProductionPlanHeaderId==id&&line.IsDelete!=true orderby line.ID select new ProductionPlanDetailLineVM{Id=line.ID,LineNumber=line.PlanNumber,ProductCode=product.Code,ProductName=product.Name,RecipeVersionId=version.ID,VersionNumber=version.VersionNumber,Quantity=line.PlannedQuantity,Unit=unit.Name,IsConvertedToOrder=line.IsConvertedToOrder}).ToListAsync(ct);
        model.Requirements=await(from requirement in _context.PrdProductionPlanRequirements.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on requirement.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on requirement.UnitId equals unit.ID where requirement.ProductionPlanHeaderId==id&&requirement.IsDelete!=true orderby material.Type,material.Code select new ProductionRequirementLineVM{MaterialId=material.ID,Code=material.Code,Name=material.Name,Type=material.Type,UnitId=unit.ID,Unit=unit.Name,TheoreticalQuantity=requirement.TheoreticalQuantity,PlannedWasteQuantity=requirement.PlannedWasteQuantity,RequiredQuantity=requirement.TotalRequiredQuantity,PhysicalStockQuantity=requirement.PhysicalStockQuantity,ReservedQuantity=requirement.ReservedQuantity,AvailableStockQuantity=requirement.AvailableStockQuantity}).ToListAsync(ct);
        model.CurrentRequirements=model.Requirements.Select(CopyRequirementForLiveStock).ToList();
        var warehouseIds=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false&&(x.Type==PrdWarehouseType.Main||x.Type==PrdWarehouseType.Production)).Select(x=>x.ID).ToListAsync(ct);
        await ApplyCurrentStockAsync(model.CurrentRequirements,warehouseIds,ct);model.CurrentStockCalculationDate=DateTime.Now;
        return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public async Task<IActionResult> PlaniTasligaAl(int id,CancellationToken ct)
    {
        var header=await _context.PrdProductionPlanHeaders.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(header==null)return NotFound();
        if(header.Status!=PrdProductionPlanHeaderStatus.Locked){TempData["error"]="Yalnızca kilitli plan taslağa alınabilir.";return RedirectToAction(nameof(PlanDetay),new{id});}
        var lineIds=await _context.PrdProductionPlans.Where(x=>x.ProductionPlanHeaderId==id&&x.IsDelete!=true).Select(x=>x.ID).ToListAsync(ct);
        if(await _context.PrdProductionOrders.AnyAsync(x=>lineIds.Contains(x.ProductionPlanId)&&x.IsDelete!=true,ct)){TempData["error"]="Üretim emri oluşturulmuş plan tekrar taslağa alınamaz.";return RedirectToAction(nameof(PlanDetay),new{id});}
        header.Status=PrdProductionPlanHeaderStatus.Draft;header.LockedDate=null;header.LockedUserId=null;header.UpdateDate=DateTime.Now;header.UpdateUserID=User.Identity?.Name;
        var lines=await _context.PrdProductionPlans.Where(x=>x.ProductionPlanHeaderId==id&&x.IsDelete!=true).ToListAsync(ct);foreach(var line in lines)line.Status=PrdProductionPlanStatus.Draft;
        await _context.SaveChangesAsync(ct);TempData["success"]="Plan yönetici yetkisiyle taslağa alındı.";return RedirectToAction(nameof(PlanDetay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> TaslagiDuzenle(int id,CancellationToken ct)
    {
        var header=await _context.PrdProductionPlanHeaders.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.Status==PrdProductionPlanHeaderStatus.Draft&&x.IsDelete!=true,ct);if(header==null){TempData["error"]="Düzenlenebilir taslak plan bulunamadı.";return RedirectToAction(nameof(Planlar));}
        var lines=await _context.PrdProductionPlans.AsNoTracking().Where(x=>x.ProductionPlanHeaderId==id&&x.IsDelete!=true).Select(x=>new ProductionPlanningSessionItem{RecipeVersionId=x.RecipeVersionId,Quantity=x.PlannedQuantity}).ToListAsync(ct);
        SavePlanningList(lines);HttpContext.Session.SetString(EditingPlanHeaderSessionKey,id.ToString());return RedirectToAction(nameof(Planlama));
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public async Task<IActionResult> PlaniIptalEt(int id,CancellationToken ct)
    {
        var header=await _context.PrdProductionPlanHeaders.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(header==null)return NotFound();
        var lines=await _context.PrdProductionPlans.Where(x=>x.ProductionPlanHeaderId==id&&x.IsDelete!=true).ToListAsync(ct);
        var lineIds=lines.Select(x=>x.ID).ToList();
        if(lines.Any(x=>x.IsConvertedToOrder)||await _context.PrdProductionOrders.AnyAsync(x=>lineIds.Contains(x.ProductionPlanId)&&x.IsDelete!=true,ct)){TempData["error"]="Üretim emrine dönüşmüş plan iptal edilemez.";return RedirectToAction(nameof(PlanDetay),new{id});}
        header.Status=PrdProductionPlanHeaderStatus.Cancelled;header.IsActive=false;header.UpdateDate=DateTime.Now;header.UpdateUserID=User.Identity?.Name;foreach(var line in lines){line.Status=PrdProductionPlanStatus.Cancelled;line.IsActive=false;}
        await _context.SaveChangesAsync(ct);TempData["success"]="Üretim planı iptal edildi.";return RedirectToAction(nameof(PlanDetay),new{id});
    }
    [HttpGet]
    public async Task<IActionResult> EmirOlustur(int planHeaderId,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await BuildOrderCreateModel(planHeaderId,null,null,null,ct);
        if(model==null){TempData["error"]="Üretim emrine dönüştürülebilecek kilitli plan bulunamadı.";return RedirectToAction(nameof(Planlar));}
        return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> EmirOlustur(ProductionOrderCreateVM model,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var rebuilt=await BuildOrderCreateModel(model.PlanHeaderId,model.Lines,model.SourceWarehouseId,model.ProductionWarehouseId,ct);
        if(rebuilt==null){TempData["error"]="Plan artık üretim emrine dönüştürülebilir durumda değil.";return RedirectToAction(nameof(PlanDetay),new{id=model.PlanHeaderId});}
        rebuilt.SourceWarehouseId=model.SourceWarehouseId;rebuilt.ProductionWarehouseId=model.ProductionWarehouseId;rebuilt.AllowShortage=model.AllowShortage;
        var sourceValid=await _context.PrdWarehouses.AnyAsync(x=>x.ID==model.SourceWarehouseId&&x.Type==PrdWarehouseType.Main&&x.IsActive!=false&&x.IsDelete!=true,ct);
        var productionValid=await _context.PrdWarehouses.AnyAsync(x=>x.ID==model.ProductionWarehouseId&&x.Type==PrdWarehouseType.Production&&x.IsActive!=false&&x.IsDelete!=true,ct);
        if(!sourceValid)ModelState.AddModelError(nameof(model.SourceWarehouseId),"Geçerli bir ana depo seçiniz.");
        if(!productionValid)ModelState.AddModelError(nameof(model.ProductionWarehouseId),"Geçerli bir üretim deposu seçiniz.");
        if(model.SourceWarehouseId==model.ProductionWarehouseId)ModelState.AddModelError(string.Empty,"Kaynak depo ile üretim deposu aynı olamaz.");
        if(rebuilt.TotalShortageQuantity>0&&!model.AllowShortage)ModelState.AddModelError(nameof(model.AllowShortage),"Eksik stok bulundu. Emirleri oluşturmak için uyarıyı onaylamalısınız.");
        if(rebuilt.Lines.Any(x=>string.IsNullOrWhiteSpace(x.BatchNumber)))ModelState.AddModelError(string.Empty,"Her ürün için parti numarası girilmelidir.");
        if(!ModelState.IsValid)return View(rebuilt);

        var planIds=rebuilt.Lines.Select(x=>x.ProductionPlanId).ToList();
        var plans=await _context.PrdProductionPlans.Where(x=>planIds.Contains(x.ID)&&x.ProductionPlanHeaderId==model.PlanHeaderId&&!x.IsConvertedToOrder&&x.IsDelete!=true).OrderBy(x=>x.ID).ToListAsync(ct);
        if(plans.Count!=rebuilt.Lines.Count){TempData["error"]="Plan satırları değişti. Lütfen işlemi yeniden başlatınız.";return RedirectToAction(nameof(PlanDetay),new{id=model.PlanHeaderId});}
        var recipeVersionIds=plans.Select(x=>x.RecipeVersionId).Distinct().ToList();
        var versions=await _context.PrdRecipeVersions.Where(x=>recipeVersionIds.Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        var items=await _context.PrdRecipeItems.Where(x=>recipeVersionIds.Contains(x.RecipeVersionId)&&x.IsDelete!=true).ToListAsync(ct);
        if(plans.Any(x=>!versions.ContainsKey(x.RecipeVersionId))){TempData["error"]="Plan reçetelerinden biri bulunamadı.";return RedirectToAction(nameof(PlanDetay),new{id=model.PlanHeaderId});}

        var now=DateTime.Now;var user=User.Identity?.Name;var orderPrefix=$"UE-{now:yyyyMMddHHmmssfff}";
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        for(var i=0;i<plans.Count;i++)
        {
            var plan=plans[i];var input=rebuilt.Lines.First(x=>x.ProductionPlanId==plan.ID);var version=versions[plan.RecipeVersionId];
            var order=new PrdProductionOrder{OrderNumber=$"{orderPrefix}-{i+1:00}",ProductionPlanId=plan.ID,RecipeVersionId=plan.RecipeVersionId,ProductMaterialId=plan.ProductMaterialId,SourceWarehouseId=model.SourceWarehouseId,ProductionWarehouseId=model.ProductionWarehouseId,PlannedQuantity=plan.PlannedQuantity,ActualQuantity=0,UnitId=plan.UnitId,BatchNumber=input.BatchNumber.Trim(),PlannedProductionDate=plan.PlannedProductionDate,Status=rebuilt.TotalShortageQuantity>0?PrdProductionOrderStatus.MaterialWaiting:PrdProductionOrderStatus.Draft,Notes=input.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
            _context.PrdProductionOrders.Add(order);await _context.SaveChangesAsync(ct);
            var orderRequirements=items.Where(x=>x.RecipeVersionId==plan.RecipeVersionId).Select(x=>
            {
                var theoretical=x.Quantity/version.BaseQuantity*plan.PlannedQuantity;var required=theoretical+(theoretical*x.PlannedWasteRate/100m);
                return new PrdMaterialRequirement{ProductionOrderId=order.ID,RecipeItemId=x.ID,MaterialId=x.MaterialId,UnitId=x.UnitId,TheoreticalQuantity=required,ReservedQuantity=0,IssuedQuantity=0,ConsumedQuantity=0,ReturnedQuantity=0,WasteQuantity=0,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
            });
            _context.PrdMaterialRequirements.AddRange(orderRequirements);plan.BatchNumber=input.BatchNumber.Trim();plan.IsConvertedToOrder=true;plan.Status=PrdProductionPlanStatus.ConvertedToOrder;plan.UpdateDate=now;plan.UpdateUserID=user;
        }
        var header=await _context.PrdProductionPlanHeaders.FirstAsync(x=>x.ID==model.PlanHeaderId,ct);header.Status=PrdProductionPlanHeaderStatus.ConvertedToOrders;header.UpdateDate=now;header.UpdateUserID=user;
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
        TempData["success"]=$"{plans.Count} ayrı üretim emri oluşturuldu.";return RedirectToAction(nameof(Emirler));
    }

    [HttpGet]
    public async Task<IActionResult> Emirler(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await(from order in _context.PrdProductionOrders.AsNoTracking() join plan in _context.PrdProductionPlans.AsNoTracking() on order.ProductionPlanId equals plan.ID join header in _context.PrdProductionPlanHeaders.AsNoTracking() on plan.ProductionPlanHeaderId equals header.ID join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID where order.IsDelete!=true orderby order.ID descending select new ProductionOrderListVM{Id=order.ID,OrderNumber=order.OrderNumber,PlanNumber=header.PlanNumber,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=order.PlannedQuantity,Unit=unit.Name,BatchNumber=order.BatchNumber,PlannedProductionDate=order.PlannedProductionDate,Status=order.Status,RequirementCount=_context.PrdMaterialRequirements.Count(x=>x.ProductionOrderId==order.ID&&x.IsDelete!=true)}).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EmirDetay(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await(from order in _context.PrdProductionOrders.AsNoTracking() join plan in _context.PrdProductionPlans.AsNoTracking() on order.ProductionPlanId equals plan.ID join header in _context.PrdProductionPlanHeaders.AsNoTracking() on plan.ProductionPlanHeaderId equals header.ID join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID join source in _context.PrdWarehouses.AsNoTracking() on order.SourceWarehouseId equals source.ID join target in _context.PrdWarehouses.AsNoTracking() on order.ProductionWarehouseId equals target.ID join version in _context.PrdRecipeVersions.AsNoTracking() on order.RecipeVersionId equals version.ID where order.ID==id&&order.IsDelete!=true select new ProductionOrderDetailVM{Id=order.ID,OrderNumber=order.OrderNumber,PlanNumber=header.PlanNumber,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=order.PlannedQuantity,Unit=unit.Name,BatchNumber=order.BatchNumber,PlannedProductionDate=order.PlannedProductionDate,Status=order.Status,RequirementCount=_context.PrdMaterialRequirements.Count(x=>x.ProductionOrderId==order.ID&&x.IsDelete!=true),SourceWarehouseId=source.ID,ProductionWarehouseId=target.ID,SourceWarehouse=source.Code+" - "+source.Name,ProductionWarehouse=target.Code+" - "+target.Name,RecipeVersionNumber=version.VersionNumber,Notes=order.Notes}).FirstOrDefaultAsync(ct);
        if(model==null)return NotFound();
        model.Requirements=await BuildOrderRequirementsAsync(id,model.SourceWarehouseId,model.ProductionWarehouseId,ct);model.TotalShortageQuantity=model.Requirements.Sum(x=>x.ShortageQuantity);model.StockCalculationDate=DateTime.Now;
        var warehouseTask=await _context.PrdWarehouseTasks.AsNoTracking().Where(x=>x.ProductionOrderId==id&&x.IsDelete!=true&&x.Status!=PrdWarehouseTaskStatus.Cancelled).OrderByDescending(x=>x.ID).Select(x=>new{x.ID,x.TaskNumber,x.Status}).FirstOrDefaultAsync(ct);if(warehouseTask!=null){model.WarehouseTaskId=warehouseTask.ID;model.WarehouseTaskNumber=warehouseTask.TaskNumber;model.WarehouseTaskStatus=warehouseTask.Status;}
        return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> EmirStokDurumunuGuncelle(int id,CancellationToken ct)
    {
        var order=await _context.PrdProductionOrders.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(order==null)return NotFound();
        if(order.Status!=PrdProductionOrderStatus.Draft&&order.Status!=PrdProductionOrderStatus.MaterialWaiting){TempData["error"]="Yalnızca taslak veya malzeme bekleyen emrin stok durumu güncellenebilir.";return RedirectToAction(nameof(EmirDetay),new{id});}
        var requirements=await BuildOrderRequirementsAsync(id,order.SourceWarehouseId,order.ProductionWarehouseId,ct);var shortage=requirements.Sum(x=>x.ShortageQuantity);order.Status=shortage>0?PrdProductionOrderStatus.MaterialWaiting:PrdProductionOrderStatus.Draft;order.UpdateDate=DateTime.Now;order.UpdateUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);
        TempData[shortage>0?"error":"success"]=shortage>0?$"Güncel stok kontrolünde toplam {shortage:0.######} eksik miktar bulunuyor.":"Güncel stok yeterli; emir malzeme toplama aşamasına hazır.";return RedirectToAction(nameof(EmirDetay),new{id});
    }
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DepoGoreviOlustur(int id,CancellationToken ct)
    {
        var order=await _context.PrdProductionOrders.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(order==null)return NotFound();
        if(order.Status!=PrdProductionOrderStatus.Draft&&order.Status!=PrdProductionOrderStatus.MaterialWaiting&&order.Status!=PrdProductionOrderStatus.WarehousePreparing){TempData["error"]="Bu üretim emri için malzeme ayırma işlemi yapılamaz.";return RedirectToAction(nameof(EmirDetay),new{id});}
        var requirements=await _context.PrdMaterialRequirements.Where(x=>x.ProductionOrderId==id&&x.IsDelete!=true).OrderBy(x=>x.ID).ToListAsync(ct);if(requirements.Count==0){TempData["error"]="Üretim emrinde malzeme ihtiyacı bulunmuyor.";return RedirectToAction(nameof(EmirDetay),new{id});}
        var lots=await GetReservableLotsAsync(order.SourceWarehouseId,order.ProductionWarehouseId,requirements.Select(x=>x.MaterialId).Distinct().ToList(),ct);var unitIds=requirements.Select(x=>x.UnitId).Concat(lots.Select(x=>x.UnitId)).Distinct().ToList();var units=await _context.PrdUnits.AsNoTracking().Where(x=>unitIds.Contains(x.ID)).Select(x=>new{x.ID,x.Code,x.Name}).ToDictionaryAsync(x=>x.ID,ct);
        var now=DateTime.Now;var user=User.Identity?.Name;var newReservations=new List<PrdStockReservation>();var sourceAllocations=new List<(int RequirementId,PrdStockReservation Reservation,decimal RequirementQuantity)>();
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        foreach(var requirement in requirements)
        {
            if(!units.TryGetValue(requirement.UnitId,out var requirementUnit))continue;var remaining=Math.Max(0,requirement.TheoreticalQuantity-requirement.ReservedQuantity);if(remaining<=0)continue;
            foreach(var lot in lots.Where(x=>x.MaterialId==requirement.MaterialId&&x.AvailableQuantity>0).OrderBy(x=>x.WarehouseId==order.ProductionWarehouseId?0:1).ThenBy(x=>x.ExpirationDate??DateTime.MaxValue).ThenBy(x=>x.StockLotId))
            {
                if(remaining<=0)break;if(!units.TryGetValue(lot.UnitId,out var lotUnit))continue;var availableInRequirementUnit=ConvertProductionQuantity(lot.AvailableQuantity,lot.UnitId,lotUnit.Code,lotUnit.Name,requirement.UnitId,requirementUnit.Code,requirementUnit.Name);if(availableInRequirementUnit<=0)continue;
                var takeInRequirementUnit=Math.Min(remaining,availableInRequirementUnit);var takeInLotUnit=ConvertProductionQuantity(takeInRequirementUnit,requirement.UnitId,requirementUnit.Code,requirementUnit.Name,lot.UnitId,lotUnit.Code,lotUnit.Name);if(takeInLotUnit<=0)continue;takeInLotUnit=Math.Min(takeInLotUnit,lot.AvailableQuantity);
                var reservation=new PrdStockReservation{MaterialRequirementId=requirement.ID,MaterialId=requirement.MaterialId,WarehouseId=lot.WarehouseId,StockLotId=lot.StockLotId,ReservedQuantity=takeInLotUnit,UsedQuantity=0,ReleasedQuantity=0,Status=PrdReservationStatus.Active,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};newReservations.Add(reservation);if(lot.WarehouseId==order.SourceWarehouseId)sourceAllocations.Add((requirement.ID,reservation,takeInRequirementUnit));
                requirement.ReservedQuantity+=takeInRequirementUnit;remaining-=takeInRequirementUnit;lot.AvailableQuantity-=takeInLotUnit;
            }
        }
        if(newReservations.Count>0)_context.PrdStockReservations.AddRange(newReservations);
        var shortages=requirements.ToDictionary(x=>x.ID,x=>Math.Max(0,x.TheoreticalQuantity-x.ReservedQuantity));var hasShortage=shortages.Values.Any(x=>x>0);
        var task=await _context.PrdWarehouseTasks.FirstOrDefaultAsync(x=>x.ProductionOrderId==id&&x.IsDelete!=true&&x.Status!=PrdWarehouseTaskStatus.Cancelled&&x.Status!=PrdWarehouseTaskStatus.Delivered,ct);
        if(task==null&&(sourceAllocations.Count>0||hasShortage))
        {
            task=new PrdWarehouseTask{TaskNumber=$"DH-{now:yyyyMMddHHmmssfff}-{order.ID}",ProductionOrderId=order.ID,SourceWarehouseId=order.SourceWarehouseId,TargetWarehouseId=order.ProductionWarehouseId,Status=hasShortage?PrdWarehouseTaskStatus.Shortage:PrdWarehouseTaskStatus.Waiting,RequestDate=now,Notes="Üretim emri malzeme hazırlama görevi",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdWarehouseTasks.Add(task);
        }
        await _context.SaveChangesAsync(ct);
        if(task!=null)
        {
            var taskItems=await _context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==task.ID&&x.IsDelete!=true).ToListAsync(ct);var itemByRequirement=taskItems.ToDictionary(x=>x.MaterialRequirementId);
            foreach(var requirement in requirements)
            {
                var addedRequested=sourceAllocations.Where(x=>x.RequirementId==requirement.ID).Sum(x=>x.RequirementQuantity);var shortage=shortages[requirement.ID];if(addedRequested<=0&&shortage<=0&&!itemByRequirement.ContainsKey(requirement.ID))continue;
                if(!itemByRequirement.TryGetValue(requirement.ID,out var item)){item=new PrdWarehouseTaskItem{WarehouseTaskId=task.ID,MaterialRequirementId=requirement.ID,MaterialId=requirement.MaterialId,UnitId=requirement.UnitId,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdWarehouseTaskItems.Add(item);itemByRequirement[requirement.ID]=item;}
                item.RequestedQuantity+=addedRequested;item.ShortageQuantity=shortage;item.UpdateDate=now;item.UpdateUserID=user;
            }
            await _context.SaveChangesAsync(ct);
            foreach(var allocation in sourceAllocations){var item=itemByRequirement[allocation.RequirementId];_context.PrdWarehouseTaskLots.Add(new PrdWarehouseTaskLot{WarehouseTaskItemId=item.ID,StockReservationId=allocation.Reservation.ID,StockLotId=allocation.Reservation.StockLotId,Quantity=allocation.Reservation.ReservedQuantity,PreparedQuantity=0,ShippedQuantity=0,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});}
            task.Status=hasShortage?PrdWarehouseTaskStatus.Shortage:(task.Status==PrdWarehouseTaskStatus.Preparing||task.Status==PrdWarehouseTaskStatus.Ready?task.Status:PrdWarehouseTaskStatus.Waiting);task.UpdateDate=now;task.UpdateUserID=user;
        }
        order.Status=hasShortage?PrdProductionOrderStatus.MaterialWaiting:(task==null?PrdProductionOrderStatus.Ready:PrdProductionOrderStatus.WarehousePreparing);order.UpdateDate=now;order.UpdateUserID=user;await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
        if(task==null){TempData["success"]="Gerekli malzemeler üretim deposunda emre ayrıldı; emir üretime hazır.";return RedirectToAction(nameof(EmirDetay),new{id});}
        TempData[hasShortage?"error":"success"]=hasShortage?"Mevcut stoklar ayrıldı; bulunamayan miktarlar depo görevinde açık bırakıldı.":"Malzemeler ayrıldı ve depo hazırlama görevi oluşturuldu.";return RedirectToAction(nameof(DepoGorevDetay),new{id=task.ID});
    }

    [HttpGet]
    public async Task<IActionResult> DepoHazirlama(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";var model=await(from task in _context.PrdWarehouseTasks.AsNoTracking() join order in _context.PrdProductionOrders.AsNoTracking() on task.ProductionOrderId equals order.ID join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID join source in _context.PrdWarehouses.AsNoTracking() on task.SourceWarehouseId equals source.ID join target in _context.PrdWarehouses.AsNoTracking() on task.TargetWarehouseId equals target.ID where task.IsDelete!=true orderby task.ID descending select new ProductionWarehouseTaskListVM{Id=task.ID,TaskNumber=task.TaskNumber,ProductionOrderId=order.ID,OrderNumber=order.OrderNumber,ProductCode=product.Code,ProductName=product.Name,SourceWarehouse=source.Code+" - "+source.Name,TargetWarehouse=target.Code+" - "+target.Name,Status=task.Status,RequestDate=task.RequestDate,AssignedUserId=task.AssignedUserId,ItemCount=_context.PrdWarehouseTaskItems.Count(x=>x.WarehouseTaskId==task.ID&&x.IsDelete!=true),RequestedQuantity=_context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==task.ID&&x.IsDelete!=true).Sum(x=>(decimal?)x.RequestedQuantity)??0,PreparedQuantity=_context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==task.ID&&x.IsDelete!=true).Sum(x=>(decimal?)x.PreparedQuantity)??0,ShortageQuantity=_context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==task.ID&&x.IsDelete!=true).Sum(x=>(decimal?)x.ShortageQuantity)??0}).ToListAsync(ct);return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> DepoGorevDetay(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await(from task in _context.PrdWarehouseTasks.AsNoTracking()
                        join order in _context.PrdProductionOrders.AsNoTracking() on task.ProductionOrderId equals order.ID
                        join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID
                        join source in _context.PrdWarehouses.AsNoTracking() on task.SourceWarehouseId equals source.ID
                        join target in _context.PrdWarehouses.AsNoTracking() on task.TargetWarehouseId equals target.ID
                        where task.ID==id&&task.IsDelete!=true
                        select new ProductionWarehouseTaskDetailVM{Id=task.ID,TaskNumber=task.TaskNumber,ProductionOrderId=order.ID,OrderNumber=order.OrderNumber,ProductCode=product.Code,ProductName=product.Name,SourceWarehouse=source.Code+" - "+source.Name,TargetWarehouse=target.Code+" - "+target.Name,Status=task.Status,RequestDate=task.RequestDate,AssignedUserId=task.AssignedUserId,PreparedDate=task.PreparedDate,ShippedDate=task.ShippedDate,DeliveredDate=task.DeliveredDate,Notes=task.Notes}).FirstOrDefaultAsync(ct);
        if(model==null)return NotFound();
        model.Items=await(from item in _context.PrdWarehouseTaskItems.AsNoTracking()
                          join material in _context.PrdMaterials.AsNoTracking() on item.MaterialId equals material.ID
                          join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID
                          where item.WarehouseTaskId==id&&item.IsDelete!=true
                          orderby material.Code
                          select new ProductionWarehouseTaskItemVM{Id=item.ID,MaterialCode=material.Code,MaterialName=material.Name,Unit=unit.Name,RequestedQuantity=item.RequestedQuantity,PreparedQuantity=item.PreparedQuantity,ShippedQuantity=item.ShippedQuantity,ShortageQuantity=item.ShortageQuantity}).ToListAsync(ct);
        var itemIds=model.Items.Select(x=>x.Id).ToList();
        var lots=await(from taskLot in _context.PrdWarehouseTaskLots.AsNoTracking()
                       join taskItem in _context.PrdWarehouseTaskItems.AsNoTracking() on taskLot.WarehouseTaskItemId equals taskItem.ID
                       join stockLot in _context.PrdStockLots.AsNoTracking() on taskLot.StockLotId equals stockLot.ID
                       join material in _context.PrdMaterials.AsNoTracking() on stockLot.MaterialId equals material.ID
                       join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                       join taskUnit in _context.PrdUnits.AsNoTracking() on taskItem.UnitId equals taskUnit.ID
                       where itemIds.Contains(taskLot.WarehouseTaskItemId)&&taskLot.IsDelete!=true
                       select new{taskLot.ID,taskLot.WarehouseTaskItemId,stockLot.LotNumber,stockLot.ExpirationDate,taskLot.Quantity,PreparedQuantity=taskLot.PreparedQuantity??(taskItem.PreparedQuantity>0?taskLot.Quantity:0),ShippedQuantity=taskLot.ShippedQuantity??(taskItem.ShippedQuantity>0?taskLot.Quantity:0),SourceUnitId=unit.ID,SourceUnitCode=unit.Code,SourceUnit=unit.Name,TaskUnitId=taskUnit.ID,TaskUnitCode=taskUnit.Code,TaskUnit=taskUnit.Name}).ToListAsync(ct);
        foreach(var item in model.Items)
            item.Lots=lots.Where(x=>x.WarehouseTaskItemId==item.Id).Select(x=>{var factor=ConvertProductionQuantity(1,x.SourceUnitId,x.SourceUnitCode,x.SourceUnit,x.TaskUnitId,x.TaskUnitCode,x.TaskUnit);var canConvert=factor>0;return new ProductionWarehouseTaskLotVM{Id=x.ID,LotNumber=x.LotNumber,ExpirationDate=x.ExpirationDate,Quantity=x.Quantity,PreparedQuantity=x.PreparedQuantity,ShippedQuantity=x.ShippedQuantity,Unit=x.SourceUnit,TaskQuantity=canConvert?x.Quantity*factor:x.Quantity,TaskPreparedQuantity=canConvert?x.PreparedQuantity*factor:x.PreparedQuantity,TaskShippedQuantity=canConvert?x.ShippedQuantity*factor:x.ShippedQuantity,TaskUnitConversionFactor=canConvert?factor:1,TaskUnit=canConvert?x.TaskUnit:x.SourceUnit};}).ToList();
        model.ItemCount=model.Items.Count;model.RequestedQuantity=model.Items.Sum(x=>x.RequestedQuantity);model.PreparedQuantity=model.Items.Sum(x=>x.PreparedQuantity);model.ShortageQuantity=model.Items.Sum(x=>x.ShortageQuantity);return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DepoGoreviHazirlamayaBasla(int id,CancellationToken ct)
    {
        var task=await _context.PrdWarehouseTasks.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(task==null)return NotFound();var hasCollectable=await _context.PrdWarehouseTaskItems.AnyAsync(x=>x.WarehouseTaskId==id&&x.IsDelete!=true&&x.RequestedQuantity>x.PreparedQuantity,ct);if(!hasCollectable){TempData["error"]="Bu görevde şu anda toplanabilecek yeni ayrılmış malzeme bulunmuyor.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}task.Status=PrdWarehouseTaskStatus.Preparing;task.AssignedUserId=User.Identity?.Name;task.UpdateDate=DateTime.Now;task.UpdateUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);TempData["success"]="Depo görevi hazırlanmaya alındı.";return RedirectToAction(nameof(DepoGorevDetay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DepoGoreviHazirliginiKaydet(ProductionWarehousePreparationInputVM input,CancellationToken ct)
    {
        var id=input.Id;
        var task=await _context.PrdWarehouseTasks.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(task==null)return NotFound();
        if(task.Status!=PrdWarehouseTaskStatus.Preparing){TempData["error"]="Miktar girişi için önce depo görevini hazırlamaya başlatınız.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
        var items=await _context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==id&&x.IsDelete!=true).ToListAsync(ct);var itemIds=items.Select(x=>x.ID).ToList();var itemById=items.ToDictionary(x=>x.ID);
        var taskLots=await(from taskLot in _context.PrdWarehouseTaskLots
                           join reservation in _context.PrdStockReservations on taskLot.StockReservationId equals reservation.ID
                           join stockLot in _context.PrdStockLots on taskLot.StockLotId equals stockLot.ID
                           where itemIds.Contains(taskLot.WarehouseTaskItemId)&&taskLot.IsDelete!=true&&reservation.IsDelete!=true
                           select new{TaskLot=taskLot,Reservation=reservation,StockLot=stockLot}).ToListAsync(ct);
        var posted=input.Lots.GroupBy(x=>x.Id).ToDictionary(x=>x.Key,x=>x.First());var parsed=new Dictionary<int,decimal>();var existingShipped=new Dictionary<int,decimal>();
        foreach(var row in taskLots)
        {
            if(!posted.TryGetValue(row.TaskLot.ID,out var entry)||!TryParseProductionDecimal(entry.PreparedQuantityInput,out var quantity)||quantity<0){TempData["error"]=$"{row.StockLot.LotNumber} lotu için hazırlanan miktar geçersiz.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
            var shipped=row.TaskLot.ShippedQuantity??(itemById[row.TaskLot.WarehouseTaskItemId].ShippedQuantity>0?row.TaskLot.Quantity:0);if(quantity+0.000001m<shipped){TempData["error"]=$"{row.StockLot.LotNumber} lotunda hazırlanan miktar daha önce sevk edilen miktardan az olamaz.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
            existingShipped[row.TaskLot.ID]=shipped;
            parsed[row.TaskLot.ID]=quantity;
        }
        var sourceLotIds=taskLots.Select(x=>x.StockLot.ID).Distinct().ToList();var taskReservationIds=taskLots.Select(x=>x.Reservation.ID).Distinct().ToList();
        var physicalBalances=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.StockLotId.HasValue&&sourceLotIds.Contains(x.StockLotId.Value)&&x.IsDelete!=true).GroupBy(x=>x.StockLotId!.Value).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity)}).ToDictionaryAsync(x=>x.StockLotId,x=>x.Quantity,ct);
        var otherReservations=await _context.PrdStockReservations.AsNoTracking().Where(x=>sourceLotIds.Contains(x.StockLotId)&&x.IsDelete!=true&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)&&!taskReservationIds.Contains(x.ID)).GroupBy(x=>x.StockLotId).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToDictionaryAsync(x=>x.StockLotId,x=>x.Quantity,ct);
        foreach(var group in taskLots.GroupBy(x=>x.StockLot.ID))
        {
            var outstandingPrepared=group.Sum(x=>parsed[x.TaskLot.ID]-existingShipped[x.TaskLot.ID]);var physical=physicalBalances.TryGetValue(group.Key,out var balance)?balance:0;var available=Math.Max(0,physical-(otherReservations.TryGetValue(group.Key,out var other)?other:0));
            if(outstandingPrepared>available+0.000001m){TempData["error"]=$"{group.First().StockLot.LotNumber} lotunda hazırlanabilecek en fazla miktar {available:0.######}.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
        }
        var now=DateTime.Now;var user=User.Identity?.Name;
        foreach(var row in taskLots)
        {
            var prepared=parsed[row.TaskLot.ID];row.TaskLot.PreparedQuantity=prepared;row.TaskLot.ShippedQuantity=existingShipped[row.TaskLot.ID];row.TaskLot.UpdateDate=now;row.TaskLot.UpdateUserID=user;
            row.Reservation.ReservedQuantity=Math.Max(Math.Max(row.TaskLot.Quantity,prepared),row.Reservation.UsedQuantity+row.Reservation.ReleasedQuantity);
            row.Reservation.Status=row.Reservation.UsedQuantity+row.Reservation.ReleasedQuantity>=row.Reservation.ReservedQuantity?PrdReservationStatus.Used:(row.Reservation.UsedQuantity>0?PrdReservationStatus.PartiallyUsed:PrdReservationStatus.Active);row.Reservation.UpdateDate=now;row.Reservation.UpdateUserID=user;
        }
        var materialIds=taskLots.Select(x=>x.Reservation.MaterialId).Distinct().ToList();var materialUnitIds=await _context.PrdMaterials.AsNoTracking().Where(x=>materialIds.Contains(x.ID)).Select(x=>new{x.ID,x.UnitId}).ToDictionaryAsync(x=>x.ID,x=>x.UnitId,ct);var unitIds=items.Select(x=>x.UnitId).Concat(materialUnitIds.Values).Distinct().ToList();var units=await _context.PrdUnits.AsNoTracking().Where(x=>unitIds.Contains(x.ID)).Select(x=>new{x.ID,x.Code,x.Name}).ToDictionaryAsync(x=>x.ID,ct);
        foreach(var item in items)
        {
            var prepared=0m;foreach(var row in taskLots.Where(x=>x.TaskLot.WarehouseTaskItemId==item.ID)){var sourceUnitId=materialUnitIds[row.Reservation.MaterialId];var sourceUnit=units[sourceUnitId];var itemUnit=units[item.UnitId];prepared+=ConvertProductionQuantity(row.TaskLot.PreparedQuantity??0,sourceUnitId,sourceUnit.Code,sourceUnit.Name,item.UnitId,itemUnit.Code,itemUnit.Name);}item.PreparedQuantity=prepared;item.UpdateDate=now;item.UpdateUserID=user;
        }
        var order=await _context.PrdProductionOrders.FirstAsync(x=>x.ID==task.ProductionOrderId,ct);var hasStockShortage=items.Any(x=>x.ShortageQuantity>0);var hasPreparationShortage=items.Any(x=>x.PreparedQuantity+0.000001m<x.RequestedQuantity);
        if(input.CompletePreparation)
        {
            if(!taskLots.Any(x=>(x.TaskLot.PreparedQuantity??0)>(x.TaskLot.ShippedQuantity??0))){TempData["error"]="Sevke hazırlanmış en az bir lot miktarı girilmelidir.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
            task.Status=hasStockShortage||hasPreparationShortage?PrdWarehouseTaskStatus.Shortage:PrdWarehouseTaskStatus.Ready;task.PreparedDate=now;order.Status=hasStockShortage||hasPreparationShortage?PrdProductionOrderStatus.MaterialWaiting:PrdProductionOrderStatus.WarehousePreparing;
        }
        else{task.Status=PrdWarehouseTaskStatus.Preparing;order.Status=PrdProductionOrderStatus.WarehousePreparing;}
        task.UpdateDate=now;task.UpdateUserID=user;order.UpdateDate=now;order.UpdateUserID=user;await _context.SaveChangesAsync(ct);
        if(!input.CompletePreparation)TempData["success"]="Girilen hazırlama miktarları kaydedildi.";
        else if(hasStockShortage||hasPreparationShortage)TempData["error"]="Hazırlanan miktarlar kaydedildi; eksikler görev üzerinde açık bırakıldı ve mevcut malzemeler sevk edilebilir.";
        else TempData["success"]="Hazırlama tamamlandı; girilen gerçek miktarlar sevk edilebilir.";
        return RedirectToAction(nameof(DepoGorevDetay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DepoGoreviSevkEt(int id,CancellationToken ct)
    {
        var task=await _context.PrdWarehouseTasks.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(task==null)return NotFound();
        if(task.Status!=PrdWarehouseTaskStatus.Ready&&task.Status!=PrdWarehouseTaskStatus.Shortage){TempData["error"]="Yalnızca hazırlanmış depo görevi sevk edilebilir.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
        var items=await _context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==id&&x.IsDelete!=true).ToListAsync(ct);var itemIds=items.Select(x=>x.ID).ToList();
        var taskLots=await(from taskLot in _context.PrdWarehouseTaskLots
                           join reservation in _context.PrdStockReservations on taskLot.StockReservationId equals reservation.ID
                           join sourceLot in _context.PrdStockLots on taskLot.StockLotId equals sourceLot.ID
                           where itemIds.Contains(taskLot.WarehouseTaskItemId)&&taskLot.IsDelete!=true&&(taskLot.PreparedQuantity??taskLot.Quantity)>(taskLot.ShippedQuantity??0)&&reservation.IsDelete!=true&&reservation.WarehouseId==task.SourceWarehouseId&&(reservation.Status==PrdReservationStatus.Active||reservation.Status==PrdReservationStatus.PartiallyUsed)
                           select new{TaskLot=taskLot,Reservation=reservation,SourceLot=sourceLot}).ToListAsync(ct);
        if(!items.Any(x=>x.PreparedQuantity>x.ShippedQuantity)){TempData["error"]="Sevk öncesinde ayrılan malzemelerin hazırlandığı onaylanmalıdır.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
        if(taskLots.Count==0){TempData["error"]="Sevk edilecek hazırlanmış lot bulunmuyor.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}
        var now=DateTime.Now;var user=User.Identity?.Name;
        try
        {
            await using var transaction=await _context.Database.BeginTransactionAsync(ct);
            var sourceLotIds=taskLots.Select(x=>x.SourceLot.ID).Distinct().ToList();var taskReservationIds=taskLots.Select(x=>x.Reservation.ID).Distinct().ToList();var balanceRows=await _context.PrdStockMovements.Where(x=>x.StockLotId.HasValue&&sourceLotIds.Contains(x.StockLotId.Value)&&x.IsDelete!=true).GroupBy(x=>x.StockLotId!.Value).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity),Value=g.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost)}).ToDictionaryAsync(x=>x.StockLotId,ct);
            var otherReservations=await _context.PrdStockReservations.Where(x=>sourceLotIds.Contains(x.StockLotId)&&x.IsDelete!=true&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)&&!taskReservationIds.Contains(x.ID)).GroupBy(x=>x.StockLotId).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToDictionaryAsync(x=>x.StockLotId,x=>x.Quantity,ct);
            foreach(var group in taskLots.GroupBy(x=>x.SourceLot.ID))
            {
                if(!balanceRows.TryGetValue(group.Key,out var balance))throw new InvalidOperationException("Kaynak lot stok hareketlerinde bulunamadı.");var shipment=group.Sum(x=>(x.TaskLot.PreparedQuantity??x.TaskLot.Quantity)-(x.TaskLot.ShippedQuantity??0));var available=Math.Max(0,balance.Quantity-(otherReservations.TryGetValue(group.Key,out var other)?other:0));if(shipment>available)throw new InvalidOperationException($"{group.First().SourceLot.LotNumber} lotunda sevk için yeterli fiziksel stok kalmadı.");if(balance.Quantity<=0)throw new InvalidOperationException($"{group.First().SourceLot.LotNumber} lotunun bakiyesi bulunmuyor.");
            }
            var document=new PrdInventoryDocument{DocumentNumber=$"USEV-{now:yyyyMMddHHmmssfff}-{task.ID}",Type=PrdInventoryDocumentType.WarehouseTransfer,Status=PrdInventoryDocumentStatus.Posted,DocumentDate=now,PostingDate=now,PostedUserId=user,SourceWarehouseId=task.SourceWarehouseId,TargetWarehouseId=task.TargetWarehouseId,CurrencyCode="TRY",ExchangeRate=1,SourceDocumentType="ProductionOrder",SourceDocumentId=task.ProductionOrderId,Notes=$"{task.TaskNumber} üretim malzemesi sevki",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocuments.Add(document);await _context.SaveChangesAsync(ct);
            var materialIds=taskLots.Select(x=>x.Reservation.MaterialId).Distinct().ToList();var materialUnitIds=await _context.PrdMaterials.AsNoTracking().Where(x=>materialIds.Contains(x.ID)).Select(x=>new{x.ID,x.UnitId}).ToDictionaryAsync(x=>x.ID,x=>x.UnitId,ct);var unitIds=items.Select(x=>x.UnitId).Concat(materialUnitIds.Values).Distinct().ToList();var units=await _context.PrdUnits.AsNoTracking().Where(x=>unitIds.Contains(x.ID)).Select(x=>new{x.ID,x.Code,x.Name}).ToDictionaryAsync(x=>x.ID,ct);
            var sequence=0;var shipmentTotalCost=0m;
            foreach(var allocation in taskLots)
            {
                var sourceLot=allocation.SourceLot;var reservation=allocation.Reservation;var quantity=(allocation.TaskLot.PreparedQuantity??allocation.TaskLot.Quantity)-(allocation.TaskLot.ShippedQuantity??0);var balance=balanceRows[sourceLot.ID];var unitCost=balance.Quantity==0?0:balance.Value/balance.Quantity;var totalCost=quantity*unitCost;shipmentTotalCost+=totalCost;var targetLot=await GetOrCreateProductionTargetLotAsync(sourceLot,task.TargetWarehouseId,now,user,ct);
                var line=new PrdInventoryDocumentLine{InventoryDocumentId=document.ID,Sequence=++sequence,MaterialId=reservation.MaterialId,UnitId=materialUnitIds[reservation.MaterialId],SourceStockLotId=sourceLot.ID,TargetStockLotId=targetLot.ID,LotNumber=sourceLot.LotNumber,ProductionDate=sourceLot.ProductionDate,ExpirationDate=sourceLot.ExpirationDate,Quantity=quantity,OriginalUnitCost=unitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=unitCost,TotalCost=totalCost,CostSource=PrdStockCostSource.Transfer,Notes=task.TaskNumber,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocumentLines.Add(line);await _context.SaveChangesAsync(ct);
                _context.PrdStockMovements.Add(CreateWarehouseTaskMovement(document,line,task.SourceWarehouseId,sourceLot.ID,PrdStockDirection.Out,now,user));_context.PrdStockMovements.Add(CreateWarehouseTaskMovement(document,line,task.TargetWarehouseId,targetLot.ID,PrdStockDirection.In,now,user));
                var remainingReservation=Math.Max(0,reservation.ReservedQuantity-reservation.UsedQuantity-reservation.ReleasedQuantity);var used=Math.Min(quantity,remainingReservation);reservation.UsedQuantity+=used;reservation.Status=reservation.UsedQuantity+reservation.ReleasedQuantity>=reservation.ReservedQuantity?PrdReservationStatus.Used:PrdReservationStatus.PartiallyUsed;reservation.UpdateDate=now;reservation.UpdateUserID=user;
                _context.PrdStockReservations.Add(new PrdStockReservation{MaterialRequirementId=reservation.MaterialRequirementId,MaterialId=reservation.MaterialId,WarehouseId=task.TargetWarehouseId,StockLotId=targetLot.ID,ReservedQuantity=quantity,UsedQuantity=0,ReleasedQuantity=0,Status=PrdReservationStatus.Active,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
                allocation.TaskLot.ShippedQuantity=(allocation.TaskLot.ShippedQuantity??0)+quantity;allocation.TaskLot.UpdateDate=now;allocation.TaskLot.UpdateUserID=user;var item=items.First(x=>x.ID==allocation.TaskLot.WarehouseTaskItemId);if(units.TryGetValue(materialUnitIds[reservation.MaterialId],out var sourceUnit)&&units.TryGetValue(item.UnitId,out var itemUnit))item.ShippedQuantity+=ConvertProductionQuantity(quantity,materialUnitIds[reservation.MaterialId],sourceUnit.Code,sourceUnit.Name,item.UnitId,itemUnit.Code,itemUnit.Name);else item.ShippedQuantity+=quantity;item.UpdateDate=now;item.UpdateUserID=user;
            }
            document.TotalCost=shipmentTotalCost;var hasShortage=items.Any(x=>x.ShortageQuantity>0||x.PreparedQuantity+0.000001m<x.RequestedQuantity);var hasUnshipped=items.Any(x=>x.PreparedQuantity>x.ShippedQuantity);task.Status=hasShortage?PrdWarehouseTaskStatus.Shortage:(hasUnshipped?PrdWarehouseTaskStatus.Ready:PrdWarehouseTaskStatus.Shipped);task.ShippedDate=now;task.UpdateDate=now;task.UpdateUserID=user;var order=await _context.PrdProductionOrders.FirstAsync(x=>x.ID==task.ProductionOrderId,ct);order.Status=hasShortage?PrdProductionOrderStatus.MaterialWaiting:PrdProductionOrderStatus.WarehousePreparing;order.UpdateDate=now;order.UpdateUserID=user;await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
            TempData[hasShortage?"error":"success"]=hasShortage?"Hazırlanan malzemeler üretim deposuna sevk edildi; eksikler görevde açık kalmaya devam ediyor.":"Hazırlanan malzemeler üretim deposuna sevk edildi. Şimdi teslim onayı verilebilir.";
        }
        catch(InvalidOperationException ex){TempData["error"]=ex.Message;}
        return RedirectToAction(nameof(DepoGorevDetay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> DepoGoreviTeslimAl(int id,CancellationToken ct)
    {
        var task=await _context.PrdWarehouseTasks.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(task==null)return NotFound();if(task.Status!=PrdWarehouseTaskStatus.Shipped){TempData["error"]="Yalnızca tamamen sevk edilmiş görev teslim alınabilir.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}var items=await _context.PrdWarehouseTaskItems.Where(x=>x.WarehouseTaskId==id&&x.IsDelete!=true).ToListAsync(ct);if(items.Any(x=>x.ShortageQuantity>0||x.ShippedQuantity<x.RequestedQuantity)){TempData["error"]="Görevde eksik veya sevk edilmemiş malzeme bulunuyor.";return RedirectToAction(nameof(DepoGorevDetay),new{id});}var now=DateTime.Now;var user=User.Identity?.Name;task.Status=PrdWarehouseTaskStatus.Delivered;task.DeliveredDate=now;task.UpdateDate=now;task.UpdateUserID=user;var order=await _context.PrdProductionOrders.FirstAsync(x=>x.ID==task.ProductionOrderId,ct);order.Status=PrdProductionOrderStatus.Ready;order.UpdateDate=now;order.UpdateUserID=user;await _context.SaveChangesAsync(ct);TempData["success"]="Malzemeler üretim deposunda teslim alındı; üretim emri üretime hazır.";return RedirectToAction(nameof(DepoGorevDetay),new{id});
    }
    [HttpGet]
    public async Task<IActionResult> Gerceklesen(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await(from order in _context.PrdProductionOrders.AsNoTracking()
                        join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID
                        join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID
                        where order.IsDelete!=true&&(order.Status==PrdProductionOrderStatus.Ready||order.Status==PrdProductionOrderStatus.InProduction||order.Status==PrdProductionOrderStatus.Completed)
                        orderby order.Status,order.ID descending
                        select new ProductionExecutionListVM{Id=order.ID,OrderNumber=order.OrderNumber,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=order.PlannedQuantity,ActualQuantity=order.ActualQuantity,Unit=unit.Name,BatchNumber=order.BatchNumber,Status=order.Status,StartDate=order.StartDate,CompletionDate=order.CompletionDate}).ToListAsync(ct);
        var completedIds=model.Where(x=>x.Status==PrdProductionOrderStatus.Completed).Select(x=>x.Id).ToList();
        if(completedIds.Count>0)
        {
            var results=await _context.PrdProductionResults.AsNoTracking().Where(x=>completedIds.Contains(x.ProductionOrderId)&&x.IsDelete!=true).OrderByDescending(x=>x.ID).ToListAsync(ct);var resultByOrder=results.GroupBy(x=>x.ProductionOrderId).ToDictionary(x=>x.Key,x=>x.First());
            var receiptMovements=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.DocumentId.HasValue&&completedIds.Contains(x.DocumentId.Value)&&x.DocumentType==PrdStockDocumentType.ProductionOrder&&x.MovementType==PrdStockMovementType.ProductionReceipt&&x.Direction==PrdStockDirection.In&&x.IsDelete!=true).Select(x=>new{OrderId=x.DocumentId!.Value,x.TotalCost}).ToListAsync(ct);var receiptCosts=receiptMovements.GroupBy(x=>x.OrderId).ToDictionary(x=>x.Key,x=>x.Sum(y=>y.TotalCost));
            foreach(var item in model.Where(x=>x.Status==PrdProductionOrderStatus.Completed))
            {
                resultByOrder.TryGetValue(item.Id,out var result);var legacyCost=receiptCosts.TryGetValue(item.Id,out var receiptCost)?receiptCost:0;item.MaterialCost=result?.MaterialCost??legacyCost;item.AdditionalCost=(result?.TransportationCost??0)+(result?.LaborCost??0)+(result?.OtherCost??0);item.TotalProductionCost=result?.TotalProductionCost??legacyCost;item.UnitProductionCost=result?.UnitProductionCost??(item.ActualQuantity>0?item.TotalProductionCost/item.ActualQuantity:0);
            }
        }
        return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> UretimiBaslat(int id,CancellationToken ct)
    {
        var order=await _context.PrdProductionOrders.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(order==null)return NotFound();
        if(order.Status!=PrdProductionOrderStatus.Ready){TempData["error"]="Yalnızca üretime hazır emir başlatılabilir.";return RedirectToAction(nameof(EmirDetay),new{id});}
        if(await _context.PrdProductionMaterialActuals.AnyAsync(x=>x.ProductionOrderId==id&&x.IsDelete!=true,ct)){TempData["error"]="Bu emrin üretime çıkış kayıtları daha önce oluşturulmuş.";return RedirectToAction(nameof(EmirDetay),new{id});}
        var requirements=await _context.PrdMaterialRequirements.Where(x=>x.ProductionOrderId==id&&x.IsDelete!=true).ToListAsync(ct);var requirementIds=requirements.Select(x=>x.ID).ToList();
        var reservations=await(from reservation in _context.PrdStockReservations
                               join lot in _context.PrdStockLots on reservation.StockLotId equals lot.ID
                               where requirementIds.Contains(reservation.MaterialRequirementId)&&reservation.WarehouseId==order.ProductionWarehouseId&&reservation.IsDelete!=true&&(reservation.Status==PrdReservationStatus.Active||reservation.Status==PrdReservationStatus.PartiallyUsed)
                               select new{Reservation=reservation,Lot=lot}).ToListAsync(ct);
        if(reservations.Count==0){TempData["error"]="Üretim deposunda bu emre ayrılmış lot bulunmuyor.";return RedirectToAction(nameof(EmirDetay),new{id});}
        var materialIds=requirements.Select(x=>x.MaterialId).Distinct().ToList();var materialUnitIds=await _context.PrdMaterials.AsNoTracking().Where(x=>materialIds.Contains(x.ID)).Select(x=>new{x.ID,x.UnitId}).ToDictionaryAsync(x=>x.ID,x=>x.UnitId,ct);var unitIds=requirements.Select(x=>x.UnitId).Concat(materialUnitIds.Values).Distinct().ToList();var units=await _context.PrdUnits.AsNoTracking().Where(x=>unitIds.Contains(x.ID)).Select(x=>new{x.ID,x.Code,x.Name}).ToDictionaryAsync(x=>x.ID,ct);
        foreach(var requirement in requirements)
        {
            if(!materialUnitIds.TryGetValue(requirement.MaterialId,out var materialUnitId)||!units.TryGetValue(materialUnitId,out var sourceUnit)||!units.TryGetValue(requirement.UnitId,out var targetUnit)){TempData["error"]="Malzeme birim dönüşümü yapılamadı.";return RedirectToAction(nameof(EmirDetay),new{id});}
            var reserved=reservations.Where(x=>x.Reservation.MaterialRequirementId==requirement.ID).Sum(x=>ConvertProductionQuantity(x.Reservation.ReservedQuantity-x.Reservation.UsedQuantity-x.Reservation.ReleasedQuantity,materialUnitId,sourceUnit.Code,sourceUnit.Name,requirement.UnitId,targetUnit.Code,targetUnit.Name));if(reserved+0.000001m<requirement.TheoreticalQuantity){TempData["error"]="Üretim deposundaki emir rezervasyonları ihtiyacı karşılamıyor.";return RedirectToAction(nameof(EmirDetay),new{id});}
        }
        var lotIds=reservations.Select(x=>x.Lot.ID).Distinct().ToList();var balanceRows=await _context.PrdStockMovements.Where(x=>x.StockLotId.HasValue&&lotIds.Contains(x.StockLotId.Value)&&x.IsDelete!=true).GroupBy(x=>x.StockLotId!.Value).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity),Value=g.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost)}).ToDictionaryAsync(x=>x.StockLotId,ct);var reservationIds=reservations.Select(x=>x.Reservation.ID).ToList();var otherReservations=await _context.PrdStockReservations.Where(x=>lotIds.Contains(x.StockLotId)&&x.IsDelete!=true&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)&&!reservationIds.Contains(x.ID)).GroupBy(x=>x.StockLotId).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToDictionaryAsync(x=>x.StockLotId,x=>x.Quantity,ct);
        foreach(var group in reservations.GroupBy(x=>x.Lot.ID)){if(!balanceRows.TryGetValue(group.Key,out var balance)||balance.Quantity<=0){TempData["error"]=$"{group.First().Lot.LotNumber} lotunun fiziksel bakiyesi bulunmuyor.";return RedirectToAction(nameof(EmirDetay),new{id});}var issue=group.Sum(x=>x.Reservation.ReservedQuantity-x.Reservation.UsedQuantity-x.Reservation.ReleasedQuantity);var available=Math.Max(0,balance.Quantity-(otherReservations.TryGetValue(group.Key,out var other)?other:0));if(issue>available+0.000001m){TempData["error"]=$"{group.First().Lot.LotNumber} lotunun kullanılabilir miktarı üretime çıkış için yetersiz.";return RedirectToAction(nameof(EmirDetay),new{id});}}
        var now=DateTime.Now;var user=User.Identity?.Name;
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        var document=new PrdInventoryDocument{DocumentNumber=$"UCIK-{now:yyyyMMddHHmmssfff}-{order.ID}",Type=PrdInventoryDocumentType.ProductionIssue,Status=PrdInventoryDocumentStatus.Posted,DocumentDate=now,PostingDate=now,PostedUserId=user,SourceWarehouseId=order.ProductionWarehouseId,CurrencyCode="TRY",ExchangeRate=1,SourceDocumentType="ProductionOrder",SourceDocumentId=order.ID,Notes=$"{order.OrderNumber} üretime malzeme çıkışı",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocuments.Add(document);await _context.SaveChangesAsync(ct);
        var sequence=0;var totalCost=0m;
        foreach(var group in reservations.GroupBy(x=>new{x.Reservation.MaterialRequirementId,x.Reservation.MaterialId,x.Lot.ID}))
        {
            var first=group.First();var quantity=group.Sum(x=>x.Reservation.ReservedQuantity-x.Reservation.UsedQuantity-x.Reservation.ReleasedQuantity);if(quantity<=0)continue;var balance=balanceRows[first.Lot.ID];var unitCost=balance.Value/balance.Quantity;var lineTotal=quantity*unitCost;var unitId=materialUnitIds[first.Reservation.MaterialId];
            var line=new PrdInventoryDocumentLine{InventoryDocumentId=document.ID,Sequence=++sequence,MaterialId=first.Reservation.MaterialId,UnitId=unitId,SourceStockLotId=first.Lot.ID,LotNumber=first.Lot.LotNumber,ProductionDate=first.Lot.ProductionDate,ExpirationDate=first.Lot.ExpirationDate,Quantity=quantity,OriginalUnitCost=unitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=unitCost,TotalCost=lineTotal,CostSource=PrdStockCostSource.Production,Notes=order.OrderNumber,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocumentLines.Add(line);await _context.SaveChangesAsync(ct);_context.PrdStockMovements.Add(CreateProductionStockMovement(document,line,order.ProductionWarehouseId,first.Lot.ID,PrdStockDirection.Out,PrdStockMovementType.ProductionIssue,order.ID,now,user));
            _context.PrdProductionMaterialActuals.Add(new PrdProductionMaterialActual{ProductionOrderId=order.ID,MaterialRequirementId=first.Reservation.MaterialRequirementId,MaterialId=first.Reservation.MaterialId,StockLotId=first.Lot.ID,UnitId=unitId,IssuedQuantity=quantity,ConsumedQuantity=0,ReturnedQuantity=0,WasteQuantity=0,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});var requirement=requirements.First(x=>x.ID==first.Reservation.MaterialRequirementId);var sourceUnit=units[unitId];var targetUnit=units[requirement.UnitId];requirement.IssuedQuantity+=ConvertProductionQuantity(quantity,unitId,sourceUnit.Code,sourceUnit.Name,requirement.UnitId,targetUnit.Code,targetUnit.Name);totalCost+=lineTotal;
            foreach(var allocation in group){var reservation=allocation.Reservation;var remaining=reservation.ReservedQuantity-reservation.UsedQuantity-reservation.ReleasedQuantity;reservation.UsedQuantity+=remaining;reservation.Status=reservation.UsedQuantity+reservation.ReleasedQuantity>=reservation.ReservedQuantity?PrdReservationStatus.Used:PrdReservationStatus.PartiallyUsed;reservation.UpdateDate=now;reservation.UpdateUserID=user;}
        }
        document.TotalCost=totalCost;order.Status=PrdProductionOrderStatus.InProduction;order.StartDate=now;order.UpdateDate=now;order.UpdateUserID=user;await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]="Malzemeler lotlarıyla üretime çıkarıldı; gerçekleşen tüketim, iade ve fire girişi yapılabilir.";return RedirectToAction(nameof(GerceklesenDetay),new{id});
    }

    [HttpGet]
    public async Task<IActionResult> GerceklesenDetay(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";var model=await BuildProductionExecutionDetailAsync(id,null,ct);if(model==null)return NotFound();return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> GerceklesenTamamla(ProductionExecutionDetailVM input,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";var order=await _context.PrdProductionOrders.FirstOrDefaultAsync(x=>x.ID==input.OrderId&&x.IsDelete!=true,ct);if(order==null)return NotFound();if(order.Status!=PrdProductionOrderStatus.InProduction){TempData["error"]="Yalnızca üretimdeki emir tamamlanabilir.";return RedirectToAction(nameof(GerceklesenDetay),new{id=input.OrderId});}
        var actuals=await _context.PrdProductionMaterialActuals.Where(x=>x.ProductionOrderId==order.ID&&x.IsDelete!=true).ToListAsync(ct);var posted=input.Materials.GroupBy(x=>x.ActualId).ToDictionary(x=>x.Key,x=>x.First());var parsed=new Dictionary<int,(decimal Consumed,decimal Returned,decimal Waste,string? WasteReason,string? Notes)>();
        if(!TryParseProductionDecimal(input.ActualQuantityInput,out var outputQuantity)||outputQuantity<=0)ModelState.AddModelError(nameof(input.ActualQuantityInput),"Gerçekleşen mamul miktarı sıfırdan büyük olmalıdır.");
        var transportationCost=0m;var laborCost=0m;var otherCost=0m;
        if(!string.IsNullOrWhiteSpace(input.TransportationCostInput)&&(!TryParseProductionDecimal(input.TransportationCostInput,out transportationCost)||transportationCost<0))ModelState.AddModelError(nameof(input.TransportationCostInput),"Nakliye maliyeti sıfır veya daha büyük geçerli bir tutar olmalıdır.");
        if(!string.IsNullOrWhiteSpace(input.LaborCostInput)&&(!TryParseProductionDecimal(input.LaborCostInput,out laborCost)||laborCost<0))ModelState.AddModelError(nameof(input.LaborCostInput),"İşçilik maliyeti sıfır veya daha büyük geçerli bir tutar olmalıdır.");
        if(!string.IsNullOrWhiteSpace(input.OtherCostInput)&&(!TryParseProductionDecimal(input.OtherCostInput,out otherCost)||otherCost<0))ModelState.AddModelError(nameof(input.OtherCostInput),"Diğer gider sıfır veya daha büyük geçerli bir tutar olmalıdır.");
        if(otherCost>0&&string.IsNullOrWhiteSpace(input.OtherCostDescription))ModelState.AddModelError(nameof(input.OtherCostDescription),"Diğer gider girildiğinde açıklama zorunludur.");
        if(input.ProductionDate==default)ModelState.AddModelError(nameof(input.ProductionDate),"Üretim tarihi zorunludur.");var product=await _context.PrdMaterials.AsNoTracking().FirstAsync(x=>x.ID==order.ProductMaterialId,ct);if(product.RequiresExpirationDate&&!input.ExpirationDate.HasValue)ModelState.AddModelError(nameof(input.ExpirationDate),"Mamul için son kullanma tarihi zorunludur.");if(input.ExpirationDate.HasValue&&input.ExpirationDate.Value.Date<input.ProductionDate.Date)ModelState.AddModelError(nameof(input.ExpirationDate),"Son kullanma tarihi üretim tarihinden önce olamaz.");
        var existingResultLot=await _context.PrdStockLots.AsNoTracking().FirstOrDefaultAsync(x=>x.MaterialId==order.ProductMaterialId&&x.WarehouseId==order.ProductionWarehouseId&&x.LotNumber==order.BatchNumber&&x.IsDelete!=true,ct);
        if(existingResultLot!=null&&(existingResultLot.ProductionDate?.Date!=input.ProductionDate.Date||existingResultLot.ExpirationDate?.Date!=input.ExpirationDate?.Date))ModelState.AddModelError(string.Empty,$"{order.BatchNumber} parti numarası bu depoda farklı üretim veya son kullanma tarihiyle kayıtlı.");
        foreach(var actual in actuals)
        {
            if(!posted.TryGetValue(actual.ID,out var row)){ModelState.AddModelError(string.Empty,"Gerçekleşen malzeme satırlarından biri eksik.");continue;}if(!TryParseProductionDecimal(row.ConsumedQuantityInput,out var consumed)||consumed<0||!TryParseProductionDecimal(row.ReturnedQuantityInput,out var returned)||returned<0||!TryParseProductionDecimal(row.WasteQuantityInput,out var waste)||waste<0){ModelState.AddModelError(string.Empty,$"{row.MaterialCode} / {row.LotNumber} miktarlarından biri geçersiz.");continue;}if(Math.Abs(consumed+returned+waste-actual.IssuedQuantity)>0.000001m)ModelState.AddModelError(string.Empty,$"{row.MaterialCode} / {row.LotNumber}: tüketilen + iade + fire, üretime verilen {actual.IssuedQuantity:0.######} miktarına eşit olmalıdır.");if(waste>0&&string.IsNullOrWhiteSpace(row.WasteReason))ModelState.AddModelError(string.Empty,$"{row.MaterialCode} / {row.LotNumber} için fire nedeni girilmelidir.");parsed[actual.ID]=(consumed,returned,waste,row.WasteReason?.Trim(),row.Notes?.Trim());
        }
        if(actuals.Count==0)ModelState.AddModelError(string.Empty,"Üretime çıkmış malzeme kaydı bulunmuyor.");
        if(!ModelState.IsValid){var invalidModel=await BuildProductionExecutionDetailAsync(order.ID,input,ct);return View(nameof(GerceklesenDetay),invalidModel!);}
        var lotIds=actuals.Select(x=>x.StockLotId).Distinct().ToList();var issueCosts=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.DocumentType==PrdStockDocumentType.ProductionOrder&&x.DocumentId==order.ID&&x.MovementType==PrdStockMovementType.ProductionIssue&&x.Direction==PrdStockDirection.Out&&x.StockLotId.HasValue&&lotIds.Contains(x.StockLotId.Value)&&x.IsDelete!=true).GroupBy(x=>x.StockLotId!.Value).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.Quantity),Value=g.Sum(x=>x.TotalCost)}).ToDictionaryAsync(x=>x.StockLotId,ct);if(actuals.Any(x=>!issueCosts.ContainsKey(x.StockLotId)||issueCosts[x.StockLotId].Quantity<=0)){ModelState.AddModelError(string.Empty,"Üretime çıkış maliyetlerinden biri bulunamadı.");var invalidModel=await BuildProductionExecutionDetailAsync(order.ID,input,ct);return View(nameof(GerceklesenDetay),invalidModel!);}
        var requirements=await _context.PrdMaterialRequirements.Where(x=>x.ProductionOrderId==order.ID&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);var unitIds=actuals.Select(x=>x.UnitId).Concat(requirements.Values.Select(x=>x.UnitId)).Append(order.UnitId).Distinct().ToList();var units=await _context.PrdUnits.AsNoTracking().Where(x=>unitIds.Contains(x.ID)).Select(x=>new{x.ID,x.Code,x.Name}).ToDictionaryAsync(x=>x.ID,ct);var lots=await _context.PrdStockLots.Where(x=>lotIds.Contains(x.ID)).ToDictionaryAsync(x=>x.ID,ct);var now=DateTime.Now;var user=User.Identity?.Name;
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);PrdInventoryDocument? returnDocument=null;var returnRows=actuals.Where(x=>parsed[x.ID].Returned>0).ToList();if(returnRows.Count>0){returnDocument=new PrdInventoryDocument{DocumentNumber=$"UIAD-{now:yyyyMMddHHmmssfff}-{order.ID}",Type=PrdInventoryDocumentType.ProductionReturn,Status=PrdInventoryDocumentStatus.Posted,DocumentDate=now,PostingDate=now,PostedUserId=user,TargetWarehouseId=order.ProductionWarehouseId,CurrencyCode="TRY",ExchangeRate=1,SourceDocumentType="ProductionOrder",SourceDocumentId=order.ID,Notes=$"{order.OrderNumber} üretimden artan malzeme iadesi",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocuments.Add(returnDocument);await _context.SaveChangesAsync(ct);}
        var returnSequence=0;var returnTotal=0m;var netMaterialCost=0m;
        foreach(var actual in actuals)
        {
            var values=parsed[actual.ID];var issueCost=issueCosts[actual.StockLotId];var unitCost=issueCost.Value/issueCost.Quantity;netMaterialCost+=(values.Consumed+values.Waste)*unitCost;actual.ConsumedQuantity=values.Consumed;actual.ReturnedQuantity=values.Returned;actual.WasteQuantity=values.Waste;actual.WasteReason=values.WasteReason;actual.Notes=values.Notes;actual.UpdateDate=now;actual.UpdateUserID=user;var requirement=requirements[actual.MaterialRequirementId];var sourceUnit=units[actual.UnitId];var targetUnit=units[requirement.UnitId];requirement.ConsumedQuantity+=ConvertProductionQuantity(values.Consumed,actual.UnitId,sourceUnit.Code,sourceUnit.Name,requirement.UnitId,targetUnit.Code,targetUnit.Name);requirement.ReturnedQuantity+=ConvertProductionQuantity(values.Returned,actual.UnitId,sourceUnit.Code,sourceUnit.Name,requirement.UnitId,targetUnit.Code,targetUnit.Name);requirement.WasteQuantity+=ConvertProductionQuantity(values.Waste,actual.UnitId,sourceUnit.Code,sourceUnit.Name,requirement.UnitId,targetUnit.Code,targetUnit.Name);requirement.UpdateDate=now;requirement.UpdateUserID=user;
            if(values.Returned>0&&returnDocument!=null){var lot=lots[actual.StockLotId];var total=values.Returned*unitCost;var line=new PrdInventoryDocumentLine{InventoryDocumentId=returnDocument.ID,Sequence=++returnSequence,MaterialId=actual.MaterialId,UnitId=actual.UnitId,TargetStockLotId=lot.ID,LotNumber=lot.LotNumber,ProductionDate=lot.ProductionDate,ExpirationDate=lot.ExpirationDate,Quantity=values.Returned,OriginalUnitCost=unitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=unitCost,TotalCost=total,CostSource=PrdStockCostSource.Production,Notes=order.OrderNumber,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocumentLines.Add(line);await _context.SaveChangesAsync(ct);_context.PrdStockMovements.Add(CreateProductionStockMovement(returnDocument,line,order.ProductionWarehouseId,lot.ID,PrdStockDirection.In,PrdStockMovementType.ProductionReturn,order.ID,now,user));returnTotal+=total;}
        }
        if(returnDocument!=null)returnDocument.TotalCost=returnTotal;
        var totalProductionCost=netMaterialCost+transportationCost+laborCost+otherCost;var outputUnitCost=totalProductionCost/outputQuantity;
        var receiptDocument=new PrdInventoryDocument{DocumentNumber=$"UMAM-{now:yyyyMMddHHmmssfff}-{order.ID}",Type=PrdInventoryDocumentType.ProductionReceipt,Status=PrdInventoryDocumentStatus.Posted,DocumentDate=input.ProductionDate,PostingDate=now,PostedUserId=user,TargetWarehouseId=order.ProductionWarehouseId,CurrencyCode="TRY",ExchangeRate=1,TotalCost=totalProductionCost,SourceDocumentType="ProductionOrder",SourceDocumentId=order.ID,Notes=$"{order.OrderNumber} mamul girişi",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocuments.Add(receiptDocument);await _context.SaveChangesAsync(ct);
        var resultLot=await GetOrCreateProductionLotAsync(order.ProductMaterialId,order.ProductionWarehouseId,order.BatchNumber,input.ProductionDate,input.ExpirationDate,now,user,ct);
        var receiptLine=new PrdInventoryDocumentLine{InventoryDocumentId=receiptDocument.ID,Sequence=1,MaterialId=order.ProductMaterialId,UnitId=order.UnitId,TargetStockLotId=resultLot.ID,LotNumber=resultLot.LotNumber,ProductionDate=input.ProductionDate,ExpirationDate=input.ExpirationDate,Quantity=outputQuantity,OriginalUnitCost=outputUnitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=outputUnitCost,TotalCost=totalProductionCost,CostSource=PrdStockCostSource.Production,Notes=input.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdInventoryDocumentLines.Add(receiptLine);await _context.SaveChangesAsync(ct);
        _context.PrdStockMovements.Add(CreateProductionStockMovement(receiptDocument,receiptLine,order.ProductionWarehouseId,resultLot.ID,PrdStockDirection.In,PrdStockMovementType.ProductionReceipt,order.ID,now,user));
        _context.PrdProductionResults.Add(new PrdProductionResult{ProductionOrderId=order.ID,ProductMaterialId=order.ProductMaterialId,WarehouseId=order.ProductionWarehouseId,UnitId=order.UnitId,ActualQuantity=outputQuantity,BatchNumber=order.BatchNumber,ProductionDate=input.ProductionDate,ExpirationDate=input.ExpirationDate,StockLotId=resultLot.ID,MaterialCost=netMaterialCost,TransportationCost=transportationCost,LaborCost=laborCost,OtherCost=otherCost,TotalProductionCost=totalProductionCost,UnitProductionCost=outputUnitCost,OtherCostDescription=input.OtherCostDescription?.Trim(),Notes=input.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
        order.ActualQuantity=outputQuantity;order.Status=PrdProductionOrderStatus.Completed;order.CompletionDate=now;order.UpdateDate=now;order.UpdateUserID=user;await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]=$"Üretim tamamlandı; toplam {totalProductionCost:N2} ₺ maliyetle mamul stoklarına işlendi.";return RedirectToAction(nameof(GerceklesenDetay),new{id=order.ID});
    }
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

    private static bool IsLegacyOpeningMovement(string? code,string? name)
    {
        static string Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?string.Empty:value.Trim().ToUpper(new CultureInfo("tr-TR"));
        return Normalize(code).Contains("DEVİR")||Normalize(code).Contains("DEVIR")||Normalize(name).Contains("DEVİR")||Normalize(name).Contains("DEVIR");
    }

    private void FillWarehouseTypes()=>ViewBag.WarehouseTypes=Enum.GetValues<PrdWarehouseType>().Select(x=>new SelectListItem(x.ToTurkish(),((int)x).ToString())).ToList();
    private static void NormalizeWarehouseForm(WarehouseFormVM model){model.Code=(model.Code??string.Empty).Trim().ToUpperInvariant();model.Name=(model.Name??string.Empty).Trim();model.Description=string.IsNullOrWhiteSpace(model.Description)?null:model.Description.Trim();}
    private async Task ValidateWarehouseForm(WarehouseFormVM model,int? currentId,CancellationToken ct)
    {
        if(!Enum.IsDefined(model.Type)||model.Type==0)ModelState.AddModelError(nameof(model.Type),"Geçerli bir depo türü seçiniz.");
        if(!string.IsNullOrWhiteSpace(model.Code)&&await _context.PrdWarehouses.AnyAsync(x=>x.Code==model.Code&&x.ID!=currentId&&x.IsDelete!=true,ct))ModelState.AddModelError(nameof(model.Code),"Bu depo kodu zaten kullanılıyor.");
    }

    private async Task<LegacyStockImportVM> BuildLegacyStockImportModel(int? legacyWarehouseId,int? targetWarehouseId,CancellationToken ct)
    {
        var model=new LegacyStockImportVM{LegacyWarehouseId=legacyWarehouseId,TargetWarehouseId=targetWarehouseId};
        model.LegacyWarehouses=await _context.MalzemeDepo.AsNoTracking().Where(x=>x.IsActive==true&&x.IsDelete==false).OrderBy(x=>x.Kod).Select(x=>new SelectListItem((x.Kod??"")+" - "+x.Ad,x.ID.ToString())).ToListAsync(ct);
        model.TargetWarehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Type).ThenBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        if(!legacyWarehouseId.HasValue||!targetWarehouseId.HasValue)return model;
        var prefix=$"DEVIR-{legacyWarehouseId.Value}-{targetWarehouseId.Value}-";model.AlreadyImported=await _context.PrdStockMovements.AsNoTracking().AnyAsync(x=>x.DocumentNumber.StartsWith(prefix)&&x.IsDelete!=true,ct);
        var oldRows=await(from stock in _context.MalzemeStok.AsNoTracking() join material in _context.Malzeme.AsNoTracking() on stock.MalzemeID equals material.ID join unit in _context.MalzemeBirim.AsNoTracking() on material.BirimID equals unit.ID where stock.DepoID==legacyWarehouseId.Value&&stock.IsActive==true&&stock.IsDelete==false&&material.IsDelete==false select new{stock.MalzemeID,Code=material.Kod,LogoCode=material.LogoKod,Name=material.Ad,Unit=unit.Ad,MaterialCost=material.Maliyet,CriticalQuantity=material.KritikMiktar,stock.LotNumara,stock.SktTarih,stock.GirisCikis,stock.Miktar}).ToListAsync(ct);
        var legacyMaterialIds=oldRows.Select(x=>x.MalzemeID).Distinct().ToList();
        var costCandidates=await(from stock in _context.MalzemeStok.AsNoTracking() join movementType in _context.MalzemeHareketTur.AsNoTracking() on stock.HareketTurID equals movementType.ID where legacyMaterialIds.Contains(stock.MalzemeID)&&stock.IsActive==true&&stock.IsDelete==false&&stock.GirisCikis&&stock.Maliyet>0 select new{stock.ID,stock.MalzemeID,Lot=(stock.LotNumara??string.Empty).Trim(),stock.Maliyet,stock.Tarih,MovementCode=movementType.Kod,MovementName=movementType.Ad}).ToListAsync(ct);
        costCandidates=costCandidates.Where(x=>IsLegacyOpeningMovement(x.MovementCode,x.MovementName)).ToList();
        var balances=oldRows.GroupBy(x=>new{x.MalzemeID,x.Code,x.LogoCode,x.Name,x.Unit,x.MaterialCost,x.CriticalQuantity,Lot=(x.LotNumara??string.Empty).Trim()}).Select(g=>new{g.Key.MalzemeID,g.Key.Code,g.Key.LogoCode,g.Key.Name,g.Key.Unit,g.Key.MaterialCost,g.Key.CriticalQuantity,g.Key.Lot,Expiration=g.Where(x=>x.SktTarih.HasValue&&x.SktTarih.Value.Year>1900).Select(x=>x.SktTarih).Min(),Quantity=g.Sum(x=>Convert.ToDecimal(x.GirisCikis?x.Miktar:-x.Miktar))}).ToList();
        var prdMaterials=await _context.PrdMaterials.AsNoTracking().Where(x=>x.IsDelete!=true).Select(x=>new{x.ID,x.Code,x.LogoCode}).ToListAsync(ct);
        foreach(var materialGroup in balances.GroupBy(x=>x.MalzemeID))
        {
            var totalBalance=materialGroup.Sum(x=>x.Quantity);if(totalBalance<=0)continue;
            var positiveLots=materialGroup.Where(x=>x.Quantity>0).OrderBy(x=>x.Expiration??DateTime.MaxValue).ThenBy(x=>x.Lot).ToList();var amountToDeduct=Math.Max(0,positiveLots.Sum(x=>x.Quantity)-totalBalance);
            foreach(var x in positiveLots)
            {
                var deduction=Math.Min(amountToDeduct,x.Quantity);var quantity=x.Quantity-deduction;amountToDeduct-=deduction;if(quantity<=0)continue;
                var match=prdMaterials.FirstOrDefault(p=>string.Equals(p.Code,x.Code,StringComparison.OrdinalIgnoreCase)||(!string.IsNullOrWhiteSpace(x.LogoCode)&&(string.Equals(p.Code,x.LogoCode,StringComparison.OrdinalIgnoreCase)||string.Equals(p.LogoCode,x.LogoCode,StringComparison.OrdinalIgnoreCase)))||(!string.IsNullOrWhiteSpace(p.LogoCode)&&string.Equals(p.LogoCode,x.Code,StringComparison.OrdinalIgnoreCase)));
                var materialCost=costCandidates.Where(c=>c.MalzemeID==x.MalzemeID).OrderBy(c=>c.Tarih).ThenBy(c=>c.ID).FirstOrDefault();
                decimal? unitCost=null;var costSource=string.Empty;
                if(materialCost!=null){unitCost=Convert.ToDecimal(materialCost.Maliyet);costSource="Malzemenin ilk maliyetli devir kaydı";}
                else if(x.MaterialCost.HasValue&&x.MaterialCost.Value>0){unitCost=Convert.ToDecimal(x.MaterialCost.Value);costSource="Eski malzeme kartı maliyeti";}
                model.Lines.Add(new LegacyStockImportLineVM{LegacyMaterialId=x.MalzemeID,PrdMaterialId=match?.ID,MaterialCode=x.Code??x.LogoCode??string.Empty,MaterialName=x.Name??string.Empty,LotNumber=x.Lot,ExpirationDate=x.Expiration,Quantity=quantity,Unit=x.Unit??string.Empty,UnitCost=unitCost,TotalCost=unitCost*quantity,CostSource=unitCost.HasValue?costSource:"Maliyet bulunamadı",CriticalQuantity=x.CriticalQuantity.HasValue?Convert.ToDecimal(x.CriticalQuantity.Value):null});
            }
        }
        model.Lines=model.Lines.OrderBy(x=>x.MaterialCode).ThenBy(x=>x.ExpirationDate).ToList();
        return model;
    }

    private static decimal ConvertProductionQuantity(decimal quantity,int sourceUnitId,string? sourceCode,string? sourceName,int targetUnitId,string? targetCode,string? targetName)
    {
        if(sourceUnitId==targetUnitId)return quantity;
        var source=ResolveProductionUnit(sourceCode,sourceName);var target=ResolveProductionUnit(targetCode,targetName);
        if(source.HasValue&&target.HasValue&&source.Value.Family==target.Value.Family)return quantity*source.Value.Factor/target.Value.Factor;
        return 0m;
    }

    private static (string Family,decimal Factor)? ResolveProductionUnit(string? code,string? name)
    {
        static string Normalize(string? value)
        {
            var text=(value??string.Empty).Trim().ToUpper(new CultureInfo("tr-TR")).Replace('İ','I');
            return new string(text.Where(char.IsLetterOrDigit).ToArray());
        }
        foreach(var value in new[]{Normalize(code),Normalize(name)}.Where(x=>x.Length>0))
        {
            if(value is "KG" or "KILOGRAM" or "KILO")return ("MASS",1000m);
            if(value is "G" or "GR" or "GRAM" or "GRM")return ("MASS",1m);
            if(value is "MG" or "MILIGRAM" or "MILLIGRAM")return ("MASS",0.001m);
            if(value is "MCG" or "UG" or "MIKROGRAM")return ("MASS",0.000001m);
            if(value is "L" or "LT" or "LITRE" or "LITER")return ("VOLUME",1000m);
            if(value is "ML" or "MILILITRE" or "MILILITER")return ("VOLUME",1m);
            if(value is "ADET" or "AD" or "PCS" or "PIECE" or "EA")return ("COUNT",1m);
        }
        var exact=Normalize(code);if(exact.Length==0)exact=Normalize(name);
        return exact.Length==0?null:($"EXACT:{exact}",1m);
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
        var requirements=recipeItems.Select(x=>{var theoretical=x.Item.Quantity/data.First(d=>d.Version.ID==x.Item.RecipeVersionId).Version.BaseQuantity*selected.First(s=>s.RecipeVersionId==x.Item.RecipeVersionId).Quantity;return new{x.Material,x.Unit,Theoretical=theoretical,Waste=theoretical*x.Item.PlannedWasteRate/100m};}).GroupBy(x=>new{x.Material.ID,x.Material.Code,MaterialName=x.Material.Name,x.Material.Type,UnitId=x.Unit.ID,UnitCode=x.Unit.Code,UnitName=x.Unit.Name}).Select(g=>new{g.Key,Theoretical=g.Sum(x=>x.Theoretical),Waste=g.Sum(x=>x.Waste),Required=g.Sum(x=>x.Theoretical+x.Waste)}).ToList();
        var materialIds=requirements.Select(x=>x.Key.ID).ToList();
        var warehouseIds=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false&&(x.Type==PrdWarehouseType.Main||x.Type==PrdWarehouseType.Production)).Select(x=>x.ID).ToListAsync(ct);
        var today=DateTime.Today;
        var stocks=await(from movement in _context.PrdStockMovements.AsNoTracking() join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID join lot0 in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot0.ID into lotJoin from lot in lotJoin.DefaultIfEmpty() where materialIds.Contains(movement.MaterialId)&&warehouseIds.Contains(movement.WarehouseId)&&movement.IsDelete!=true&&(movement.StockLotId==null||lot.ExpirationDate==null||lot.ExpirationDate.Value.Year<=1900||lot.ExpirationDate>=today) group movement by new{movement.MaterialId,movement.UnitId,unit.Code,unit.Name} into g select new{g.Key.MaterialId,g.Key.UnitId,UnitCode=g.Key.Code,UnitName=g.Key.Name,Quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity)}).ToListAsync(ct);
        var reservations=await(from reservation in _context.PrdStockReservations.AsNoTracking() join lot in _context.PrdStockLots.AsNoTracking() on reservation.StockLotId equals lot.ID join material in _context.PrdMaterials.AsNoTracking() on reservation.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID where materialIds.Contains(reservation.MaterialId)&&warehouseIds.Contains(reservation.WarehouseId)&&reservation.IsDelete!=true&&(reservation.Status==PrdReservationStatus.Active||reservation.Status==PrdReservationStatus.PartiallyUsed)&&(lot.ExpirationDate==null||lot.ExpirationDate.Value.Year<=1900||lot.ExpirationDate>=today) group reservation by new{reservation.MaterialId,material.UnitId,unit.Code,unit.Name} into g select new{g.Key.MaterialId,g.Key.UnitId,UnitCode=g.Key.Code,UnitName=g.Key.Name,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToListAsync(ct);
        model.Requirements=requirements.Select(x=>{var physical=stocks.Where(s=>s.MaterialId==x.Key.ID).Sum(s=>ConvertProductionQuantity(s.Quantity,s.UnitId,s.UnitCode,s.UnitName,x.Key.UnitId,x.Key.UnitCode,x.Key.UnitName));var reserved=reservations.Where(s=>s.MaterialId==x.Key.ID).Sum(s=>ConvertProductionQuantity(s.Quantity,s.UnitId,s.UnitCode,s.UnitName,x.Key.UnitId,x.Key.UnitCode,x.Key.UnitName));return new ProductionRequirementLineVM{MaterialId=x.Key.ID,Code=x.Key.Code,Name=x.Key.MaterialName,Type=x.Key.Type,UnitId=x.Key.UnitId,TheoreticalQuantity=x.Theoretical,PlannedWasteQuantity=x.Waste,RequiredQuantity=x.Required,PhysicalStockQuantity=physical,ReservedQuantity=reserved,AvailableStockQuantity=Math.Max(0,physical-reserved),Unit=x.Key.UnitName};}).OrderBy(x=>x.Type).ThenBy(x=>x.Code).ToList();
        return model;
    }

    private sealed class ReservableLotBalance
    {
        public int StockLotId { get; set; }
        public int MaterialId { get; set; }
        public int WarehouseId { get; set; }
        public int UnitId { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    private async Task<List<ReservableLotBalance>> GetReservableLotsAsync(int sourceWarehouseId,int productionWarehouseId,List<int> materialIds,CancellationToken ct)
    {
        var warehouseIds=new[]{sourceWarehouseId,productionWarehouseId};var today=DateTime.Today;
        var rows=await(from movement in _context.PrdStockMovements.AsNoTracking() join lot in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot.ID where movement.StockLotId.HasValue&&materialIds.Contains(movement.MaterialId)&&warehouseIds.Contains(movement.WarehouseId)&&movement.IsDelete!=true&&lot.IsDelete!=true&&(lot.ExpirationDate==null||lot.ExpirationDate.Value.Year<=1900||lot.ExpirationDate>=today) select new{StockLotId=lot.ID,movement.MaterialId,movement.WarehouseId,movement.UnitId,lot.ExpirationDate,movement.Direction,movement.Quantity}).ToListAsync(ct);
        var lotIds=rows.Select(x=>x.StockLotId).Distinct().ToList();var reserved=await _context.PrdStockReservations.AsNoTracking().Where(x=>lotIds.Contains(x.StockLotId)&&x.IsDelete!=true&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)).GroupBy(x=>x.StockLotId).Select(g=>new{StockLotId=g.Key,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToDictionaryAsync(x=>x.StockLotId,x=>x.Quantity,ct);
        return rows.GroupBy(x=>new{x.StockLotId,x.MaterialId,x.WarehouseId,x.UnitId,x.ExpirationDate}).Select(g=>new ReservableLotBalance{StockLotId=g.Key.StockLotId,MaterialId=g.Key.MaterialId,WarehouseId=g.Key.WarehouseId,UnitId=g.Key.UnitId,ExpirationDate=g.Key.ExpirationDate,AvailableQuantity=Math.Max(0,g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity)-(reserved.TryGetValue(g.Key.StockLotId,out var quantity)?quantity:0))}).Where(x=>x.AvailableQuantity>0).ToList();
    }

    private async Task<PrdStockLot> GetOrCreateProductionTargetLotAsync(PrdStockLot sourceLot,int targetWarehouseId,DateTime now,string? user,CancellationToken ct)
    {
        var targetLot=await _context.PrdStockLots.FirstOrDefaultAsync(x=>x.MaterialId==sourceLot.MaterialId&&x.WarehouseId==targetWarehouseId&&x.LotNumber==sourceLot.LotNumber&&x.IsDelete!=true,ct);
        if(targetLot!=null)
        {
            if(targetLot.ExpirationDate.HasValue&&sourceLot.ExpirationDate.HasValue&&targetLot.ExpirationDate.Value.Date!=sourceLot.ExpirationDate.Value.Date)throw new InvalidOperationException($"{sourceLot.LotNumber} lotunun üretim deposundaki SKT bilgisi kaynak depodan farklı.");
            return targetLot;
        }
        targetLot=new PrdStockLot{MaterialId=sourceLot.MaterialId,WarehouseId=targetWarehouseId,LotNumber=sourceLot.LotNumber,ProductionDate=sourceLot.ProductionDate,ExpirationDate=sourceLot.ExpirationDate,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdStockLots.Add(targetLot);await _context.SaveChangesAsync(ct);return targetLot;
    }

    private async Task<PrdStockLot> GetOrCreateProductionLotAsync(int materialId,int warehouseId,string lotNumber,DateTime? productionDate,DateTime? expirationDate,DateTime now,string? user,CancellationToken ct)
    {
        var lot=await _context.PrdStockLots.FirstOrDefaultAsync(x=>x.MaterialId==materialId&&x.WarehouseId==warehouseId&&x.LotNumber==lotNumber&&x.IsDelete!=true,ct);
        if(lot!=null)
        {
            if(lot.ProductionDate?.Date!=productionDate?.Date||lot.ExpirationDate?.Date!=expirationDate?.Date)throw new InvalidOperationException($"{lotNumber} partisinin kayıtlı üretim veya son kullanma tarihi farklı.");
            return lot;
        }
        lot=new PrdStockLot{MaterialId=materialId,WarehouseId=warehouseId,LotNumber=lotNumber,ProductionDate=productionDate,ExpirationDate=expirationDate,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdStockLots.Add(lot);await _context.SaveChangesAsync(ct);return lot;
    }

    private static PrdStockMovement CreateProductionStockMovement(PrdInventoryDocument document,PrdInventoryDocumentLine line,int warehouseId,int lotId,PrdStockDirection direction,PrdStockMovementType movementType,int productionOrderId,DateTime now,string? user)=>new(){InventoryDocumentId=document.ID,InventoryDocumentLineId=line.ID,MaterialId=line.MaterialId,WarehouseId=warehouseId,StockLotId=lotId,Direction=direction,MovementType=movementType,Quantity=line.Quantity,UnitId=line.UnitId,OriginalUnitCost=line.OriginalUnitCost,CurrencyCode=line.CurrencyCode,ExchangeRate=line.ExchangeRate,UnitCost=line.UnitCost,TotalCost=line.TotalCost,CostSource=PrdStockCostSource.Production,MovementDate=document.DocumentDate,DocumentNumber=document.DocumentNumber,DocumentType=PrdStockDocumentType.ProductionOrder,DocumentId=productionOrderId,Description=document.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};

    private async Task<ProductionExecutionDetailVM?> BuildProductionExecutionDetailAsync(int orderId,ProductionExecutionDetailVM? posted,CancellationToken ct)
    {
        var model=await(from order in _context.PrdProductionOrders.AsNoTracking()
                        join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID
                        join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID
                        where order.ID==orderId&&order.IsDelete!=true
                        select new ProductionExecutionDetailVM{OrderId=order.ID,OrderNumber=order.OrderNumber,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=order.PlannedQuantity,ActualQuantity=order.ActualQuantity,UnitId=order.UnitId,Unit=unit.Name,BatchNumber=order.BatchNumber,Status=order.Status,StartDate=order.StartDate,CompletionDate=order.CompletionDate,ProductionDate=order.PlannedProductionDate,ActualQuantityInput=order.ActualQuantity>0?order.ActualQuantity.ToString("0.######",CultureInfo.InvariantCulture):order.PlannedQuantity.ToString("0.######",CultureInfo.InvariantCulture),Notes=order.Notes}).FirstOrDefaultAsync(ct);if(model==null)return null;
        var issuedMaterialCost=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.DocumentType==PrdStockDocumentType.ProductionOrder&&x.DocumentId==orderId&&x.MovementType==PrdStockMovementType.ProductionIssue&&x.Direction==PrdStockDirection.Out&&x.IsDelete!=true).SumAsync(x=>(decimal?)x.TotalCost,ct)??0;model.MaterialCost=issuedMaterialCost;
        var result=await _context.PrdProductionResults.AsNoTracking().Where(x=>x.ProductionOrderId==orderId&&x.IsDelete!=true).OrderByDescending(x=>x.ID).FirstOrDefaultAsync(ct);
        if(result!=null)
        {
            var legacyReceiptCost=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.DocumentType==PrdStockDocumentType.ProductionOrder&&x.DocumentId==orderId&&x.MovementType==PrdStockMovementType.ProductionReceipt&&x.Direction==PrdStockDirection.In&&x.IsDelete!=true).SumAsync(x=>(decimal?)x.TotalCost,ct)??0;
            model.ProductionDate=result.ProductionDate;model.ExpirationDate=result.ExpirationDate;model.ActualQuantityInput=result.ActualQuantity.ToString("0.######",CultureInfo.InvariantCulture);model.Notes=result.Notes;model.MaterialCost=result.MaterialCost??legacyReceiptCost;model.TransportationCost=result.TransportationCost??0;model.LaborCost=result.LaborCost??0;model.OtherCost=result.OtherCost??0;model.TotalProductionCost=result.TotalProductionCost??legacyReceiptCost;model.UnitProductionCost=result.UnitProductionCost??(result.ActualQuantity>0?model.TotalProductionCost/result.ActualQuantity:0);model.TransportationCostInput=model.TransportationCost.ToString("0.######",CultureInfo.InvariantCulture);model.LaborCostInput=model.LaborCost.ToString("0.######",CultureInfo.InvariantCulture);model.OtherCostInput=model.OtherCost.ToString("0.######",CultureInfo.InvariantCulture);model.OtherCostDescription=result.OtherCostDescription;
        }
        else{model.TotalProductionCost=model.MaterialCost;if(TryParseProductionDecimal(model.ActualQuantityInput,out var preliminaryQuantity)&&preliminaryQuantity>0)model.UnitProductionCost=model.TotalProductionCost/preliminaryQuantity;}
        model.Materials=await(from actual in _context.PrdProductionMaterialActuals.AsNoTracking()
                              join material in _context.PrdMaterials.AsNoTracking() on actual.MaterialId equals material.ID
                              join lot in _context.PrdStockLots.AsNoTracking() on actual.StockLotId equals lot.ID
                              join unit in _context.PrdUnits.AsNoTracking() on actual.UnitId equals unit.ID
                              where actual.ProductionOrderId==orderId&&actual.IsDelete!=true
                              orderby material.Code,lot.ExpirationDate,lot.LotNumber
                              select new ProductionExecutionMaterialVM{ActualId=actual.ID,MaterialRequirementId=actual.MaterialRequirementId,MaterialId=actual.MaterialId,StockLotId=actual.StockLotId,MaterialCode=material.Code,MaterialName=material.Name,LotNumber=lot.LotNumber,ExpirationDate=lot.ExpirationDate,IssuedQuantity=actual.IssuedQuantity,Unit=unit.Name,ConsumedQuantityInput=actual.ConsumedQuantity.ToString("0.######",CultureInfo.InvariantCulture),ReturnedQuantityInput=actual.ReturnedQuantity.ToString("0.######",CultureInfo.InvariantCulture),WasteQuantityInput=actual.WasteQuantity.ToString("0.######",CultureInfo.InvariantCulture),WasteReason=actual.WasteReason,Notes=actual.Notes}).ToListAsync(ct);
        if(posted!=null)
        {
            model.ProductionDate=posted.ProductionDate;model.ExpirationDate=posted.ExpirationDate;model.ActualQuantityInput=posted.ActualQuantityInput;model.TransportationCostInput=posted.TransportationCostInput;model.LaborCostInput=posted.LaborCostInput;model.OtherCostInput=posted.OtherCostInput;model.OtherCostDescription=posted.OtherCostDescription;model.Notes=posted.Notes;if(TryParseProductionDecimal(posted.TransportationCostInput,out var transportation)&&transportation>=0)model.TransportationCost=transportation;if(TryParseProductionDecimal(posted.LaborCostInput,out var labor)&&labor>=0)model.LaborCost=labor;if(TryParseProductionDecimal(posted.OtherCostInput,out var other)&&other>=0)model.OtherCost=other;model.TotalProductionCost=model.MaterialCost+model.TransportationCost+model.LaborCost+model.OtherCost;if(TryParseProductionDecimal(posted.ActualQuantityInput,out var actualQuantity)&&actualQuantity>0)model.UnitProductionCost=model.TotalProductionCost/actualQuantity;var postedRows=posted.Materials.GroupBy(x=>x.ActualId).ToDictionary(x=>x.Key,x=>x.First());foreach(var row in model.Materials){if(!postedRows.TryGetValue(row.ActualId,out var input))continue;row.ConsumedQuantityInput=input.ConsumedQuantityInput;row.ReturnedQuantityInput=input.ReturnedQuantityInput;row.WasteQuantityInput=input.WasteQuantityInput;row.WasteReason=input.WasteReason;row.Notes=input.Notes;}
        }
        return model;
    }

    private static PrdStockMovement CreateWarehouseTaskMovement(PrdInventoryDocument document,PrdInventoryDocumentLine line,int warehouseId,int lotId,PrdStockDirection direction,DateTime now,string? user)=>new(){InventoryDocumentId=document.ID,InventoryDocumentLineId=line.ID,MaterialId=line.MaterialId,WarehouseId=warehouseId,StockLotId=lotId,Direction=direction,MovementType=PrdStockMovementType.Transfer,Quantity=line.Quantity,UnitId=line.UnitId,OriginalUnitCost=line.OriginalUnitCost,CurrencyCode=line.CurrencyCode,ExchangeRate=line.ExchangeRate,UnitCost=line.UnitCost,TotalCost=line.TotalCost,CostSource=PrdStockCostSource.Transfer,MovementDate=now,DocumentNumber=document.DocumentNumber,DocumentType=PrdStockDocumentType.InventoryDocument,DocumentId=document.ID,TransferNumber=document.DocumentNumber,Description=document.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};

    private static ProductionRequirementLineVM CopyRequirementForLiveStock(ProductionRequirementLineVM source)=>new(){MaterialId=source.MaterialId,Code=source.Code,Name=source.Name,Type=source.Type,UnitId=source.UnitId,TheoreticalQuantity=source.TheoreticalQuantity,PlannedWasteQuantity=source.PlannedWasteQuantity,RequiredQuantity=source.RequiredQuantity,Unit=source.Unit};

    private async Task ApplyCurrentStockAsync(List<ProductionRequirementLineVM> requirements,IEnumerable<int> selectedWarehouseIds,CancellationToken ct)
    {
        if(requirements.Count==0)return;
        var warehouseIds=selectedWarehouseIds.Where(x=>x>0).Distinct().ToList();var materialIds=requirements.Select(x=>x.MaterialId).Distinct().ToList();var unitIds=requirements.Select(x=>x.UnitId).Distinct().ToList();
        if(warehouseIds.Count==0){foreach(var item in requirements){item.PhysicalStockQuantity=0;item.ReservedQuantity=0;item.AvailableStockQuantity=0;}return;}
        var targetUnits=await _context.PrdUnits.AsNoTracking().Where(x=>unitIds.Contains(x.ID)).Select(x=>new{x.ID,x.Code,x.Name}).ToDictionaryAsync(x=>x.ID,ct);var today=DateTime.Today;
        var stocks=await(from movement in _context.PrdStockMovements.AsNoTracking() join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID join lot0 in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot0.ID into lotJoin from lot in lotJoin.DefaultIfEmpty() where materialIds.Contains(movement.MaterialId)&&warehouseIds.Contains(movement.WarehouseId)&&movement.IsDelete!=true&&(movement.StockLotId==null||lot.ExpirationDate==null||lot.ExpirationDate.Value.Year<=1900||lot.ExpirationDate>=today) group movement by new{movement.MaterialId,movement.UnitId,unit.Code,unit.Name} into g select new{g.Key.MaterialId,g.Key.UnitId,UnitCode=g.Key.Code,UnitName=g.Key.Name,Quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity)}).ToListAsync(ct);
        var reservations=await(from reservation in _context.PrdStockReservations.AsNoTracking() join lot in _context.PrdStockLots.AsNoTracking() on reservation.StockLotId equals lot.ID join material in _context.PrdMaterials.AsNoTracking() on reservation.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID where materialIds.Contains(reservation.MaterialId)&&warehouseIds.Contains(reservation.WarehouseId)&&reservation.IsDelete!=true&&(reservation.Status==PrdReservationStatus.Active||reservation.Status==PrdReservationStatus.PartiallyUsed)&&(lot.ExpirationDate==null||lot.ExpirationDate.Value.Year<=1900||lot.ExpirationDate>=today) group reservation by new{reservation.MaterialId,material.UnitId,unit.Code,unit.Name} into g select new{g.Key.MaterialId,g.Key.UnitId,UnitCode=g.Key.Code,UnitName=g.Key.Name,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToListAsync(ct);
        foreach(var item in requirements)
        {
            if(!targetUnits.TryGetValue(item.UnitId,out var targetUnit))continue;
            item.PhysicalStockQuantity=stocks.Where(x=>x.MaterialId==item.MaterialId).Sum(x=>ConvertProductionQuantity(x.Quantity,x.UnitId,x.UnitCode,x.UnitName,item.UnitId,targetUnit.Code,targetUnit.Name));
            item.ReservedQuantity=reservations.Where(x=>x.MaterialId==item.MaterialId).Sum(x=>ConvertProductionQuantity(x.Quantity,x.UnitId,x.UnitCode,x.UnitName,item.UnitId,targetUnit.Code,targetUnit.Name));
            item.AvailableStockQuantity=Math.Max(0,item.PhysicalStockQuantity-item.ReservedQuantity);
        }
    }

    private async Task<List<ProductionOrderRequirementVM>> BuildOrderRequirementsAsync(int orderId,int sourceWarehouseId,int productionWarehouseId,CancellationToken ct)
    {
        var rows=await(from requirement in _context.PrdMaterialRequirements.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on requirement.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on requirement.UnitId equals unit.ID where requirement.ProductionOrderId==orderId&&requirement.IsDelete!=true orderby material.Type,material.Code select new ProductionOrderRequirementVM{MaterialId=material.ID,UnitId=unit.ID,MaterialCode=material.Code,MaterialName=material.Name,MaterialType=material.Type,RequiredQuantity=requirement.TheoreticalQuantity,ReservedQuantity=requirement.ReservedQuantity,IssuedQuantity=requirement.IssuedQuantity,ConsumedQuantity=requirement.ConsumedQuantity,ReturnedQuantity=requirement.ReturnedQuantity,WasteQuantity=requirement.WasteQuantity,Unit=unit.Name}).ToListAsync(ct);
        var model=rows.GroupBy(x=>new{x.MaterialId,x.UnitId,x.MaterialCode,x.MaterialName,x.MaterialType,x.Unit}).Select(g=>new ProductionOrderRequirementVM{MaterialId=g.Key.MaterialId,UnitId=g.Key.UnitId,MaterialCode=g.Key.MaterialCode,MaterialName=g.Key.MaterialName,MaterialType=g.Key.MaterialType,Unit=g.Key.Unit,RequiredQuantity=g.Sum(x=>x.RequiredQuantity),ReservedQuantity=g.Sum(x=>x.ReservedQuantity),IssuedQuantity=g.Sum(x=>x.IssuedQuantity),ConsumedQuantity=g.Sum(x=>x.ConsumedQuantity),ReturnedQuantity=g.Sum(x=>x.ReturnedQuantity),WasteQuantity=g.Sum(x=>x.WasteQuantity)}).OrderBy(x=>x.MaterialType).ThenBy(x=>x.MaterialCode).ToList();
        var current=model.Select(x=>new ProductionRequirementLineVM{MaterialId=x.MaterialId,Code=x.MaterialCode,Name=x.MaterialName,Type=x.MaterialType,UnitId=x.UnitId,RequiredQuantity=x.RequiredQuantity,Unit=x.Unit}).ToList();await ApplyCurrentStockAsync(current,new[]{sourceWarehouseId,productionWarehouseId},ct);
        foreach(var item in model)
        {
            var stock=current.First(x=>x.MaterialId==item.MaterialId&&x.UnitId==item.UnitId);item.PhysicalStockQuantity=stock.PhysicalStockQuantity;item.ActiveReservationQuantity=stock.ReservedQuantity;item.FreeStockQuantity=stock.AvailableStockQuantity;item.AvailableForOrderQuantity=Math.Max(0,item.ReservedQuantity)+item.FreeStockQuantity;item.ShortageQuantity=Math.Max(0,item.RequiredQuantity-item.AvailableForOrderQuantity);
        }
        return model;
    }

    private async Task<ProductionOrderCreateVM?> BuildOrderCreateModel(int planHeaderId,List<ProductionOrderCreateLineVM>? postedLines,int? sourceWarehouseId,int? productionWarehouseId,CancellationToken ct)
    {
        var model=await _context.PrdProductionPlanHeaders.AsNoTracking().Where(x=>x.ID==planHeaderId&&x.Status==PrdProductionPlanHeaderStatus.Locked&&x.IsDelete!=true).Select(x=>new ProductionOrderCreateVM{PlanHeaderId=x.ID,PlanNumber=x.PlanNumber,PlannedProductionDate=x.TargetProductionDate}).FirstOrDefaultAsync(ct);
        if(model==null)return null;
        model.SourceWarehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.Type==PrdWarehouseType.Main&&x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        model.ProductionWarehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.Type==PrdWarehouseType.Production&&x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        model.SourceWarehouseId=sourceWarehouseId??(int.TryParse(model.SourceWarehouses.FirstOrDefault()?.Value,out var firstSource)?firstSource:0);model.ProductionWarehouseId=productionWarehouseId??(int.TryParse(model.ProductionWarehouses.FirstOrDefault()?.Value,out var firstProduction)?firstProduction:0);
        model.Lines=await(from plan in _context.PrdProductionPlans.AsNoTracking() join product in _context.PrdMaterials.AsNoTracking() on plan.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on plan.UnitId equals unit.ID join version in _context.PrdRecipeVersions.AsNoTracking() on plan.RecipeVersionId equals version.ID where plan.ProductionPlanHeaderId==planHeaderId&&!plan.IsConvertedToOrder&&plan.IsDelete!=true orderby plan.ID select new ProductionOrderCreateLineVM{ProductionPlanId=plan.ID,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=plan.PlannedQuantity,Unit=unit.Name,RecipeVersionNumber=version.VersionNumber}).ToListAsync(ct);
        if(model.Lines.Count==0)return null;
        model.CurrentRequirements=await(from requirement in _context.PrdProductionPlanRequirements.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on requirement.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on requirement.UnitId equals unit.ID where requirement.ProductionPlanHeaderId==planHeaderId&&requirement.IsDelete!=true orderby material.Type,material.Code select new ProductionRequirementLineVM{MaterialId=material.ID,Code=material.Code,Name=material.Name,Type=material.Type,UnitId=unit.ID,Unit=unit.Name,TheoreticalQuantity=requirement.TheoreticalQuantity,PlannedWasteQuantity=requirement.PlannedWasteQuantity,RequiredQuantity=requirement.TotalRequiredQuantity}).ToListAsync(ct);
        await ApplyCurrentStockAsync(model.CurrentRequirements,new[]{model.SourceWarehouseId,model.ProductionWarehouseId},ct);model.TotalShortageQuantity=model.CurrentRequirements.Sum(x=>x.ShortageQuantity);model.StockCalculationDate=DateTime.Now;
        if(postedLines!=null)foreach(var line in model.Lines){var posted=postedLines.FirstOrDefault(x=>x.ProductionPlanId==line.ProductionPlanId);if(posted!=null){line.BatchNumber=posted.BatchNumber;line.Notes=posted.Notes;}}
        return model;
    }
}
