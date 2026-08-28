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
    private static readonly bool LegacyStockImportEnabled=false;
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
    public async Task<IActionResult> Stoklar(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var rows=await(from movement in _context.PrdStockMovements.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on movement.MaterialId equals material.ID join warehouse in _context.PrdWarehouses.AsNoTracking() on movement.WarehouseId equals warehouse.ID join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID join lot0 in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot0.ID into lotJoin from lot in lotJoin.DefaultIfEmpty() where movement.IsDelete!=true&&material.IsDelete!=true&&warehouse.IsDelete!=true select new{movement.MaterialId,MaterialCode=material.Code,MaterialName=material.Name,WarehouseCode=warehouse.Code,WarehouseName=warehouse.Name,LotNumber=lot==null?string.Empty:lot.LotNumber,ExpirationDate=lot==null?(DateTime?)null:lot.ExpirationDate,movement.Direction,movement.Quantity,movement.TotalCost,Unit=unit.Name}).ToListAsync(ct);
        var model=rows.GroupBy(x=>new{x.MaterialId,x.MaterialCode,x.MaterialName,x.WarehouseCode,x.WarehouseName,x.LotNumber,x.ExpirationDate,x.Unit}).Select(g=>{var quantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity);var totalCost=g.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost);return new ProductionStockBalanceVM{MaterialId=g.Key.MaterialId,MaterialCode=g.Key.MaterialCode,MaterialName=g.Key.MaterialName,WarehouseCode=g.Key.WarehouseCode,WarehouseName=g.Key.WarehouseName,LotNumber=g.Key.LotNumber,ExpirationDate=g.Key.ExpirationDate,Unit=g.Key.Unit,Quantity=quantity,TotalCost=totalCost,UnitCost=quantity==0?0:totalCost/quantity};}).Where(x=>x.Quantity!=0).OrderBy(x=>x.WarehouseCode).ThenBy(x=>x.MaterialCode).ThenBy(x=>x.ExpirationDate).ToList();
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
        if(!LegacyStockImportEnabled){TempData["error"]="Maliyetli stok altyapısı tamamlanmadan eski stok aktarımı yapılamaz.";return RedirectToAction(nameof(Stoklar));}
        ViewBag.Modul="YeniUretim";var model=await BuildLegacyStockImportModel(legacyWarehouseId,targetWarehouseId,ct);
        if(!confirmImport)ModelState.AddModelError(string.Empty,"Aktarım onay kutusunu işaretleyiniz.");
        if(model.AlreadyImported)ModelState.AddModelError(string.Empty,"Bu eski depo, seçilen yeni depoya daha önce aktarılmış.");
        if(model.MatchedCount==0)ModelState.AddModelError(string.Empty,"Aktarılabilecek eşleşmiş stok kalemi bulunamadı.");
        if(!ModelState.IsValid)return View(model);
        var target=await _context.PrdWarehouses.FirstOrDefaultAsync(x=>x.ID==targetWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct);if(target==null){ModelState.AddModelError(string.Empty,"Hedef depo bulunamadı.");return View(model);}
        var matched=model.Lines.Where(x=>x.PrdMaterialId.HasValue&&x.Quantity>0).ToList();var materialIds=matched.Select(x=>x.PrdMaterialId!.Value).Distinct().ToList();var materials=await _context.PrdMaterials.Where(x=>materialIds.Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        var existingLots=await _context.PrdStockLots.Where(x=>x.WarehouseId==targetWarehouseId&&materialIds.Contains(x.MaterialId)&&x.IsDelete!=true).ToListAsync(ct);var now=DateTime.Now;var user=User.Identity?.Name;var documentNumber=$"DEVIR-{legacyWarehouseId}-{targetWarehouseId}-{now:yyyyMMddHHmmss}";
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        var lotMap=new Dictionary<string,PrdStockLot>(StringComparer.OrdinalIgnoreCase);
        foreach(var existing in existingLots)lotMap[$"{existing.MaterialId}|{existing.LotNumber}"]=existing;
        foreach(var line in matched)
        {
            var materialId=line.PrdMaterialId!.Value;var lotNumber=string.IsNullOrWhiteSpace(line.LotNumber)?$"DEVIR-{line.LegacyMaterialId}":line.LotNumber.Trim();if(lotNumber.Length>100)lotNumber=lotNumber[..100];var key=$"{materialId}|{lotNumber}";
            if(!lotMap.ContainsKey(key)){var lot=new PrdStockLot{MaterialId=materialId,WarehouseId=targetWarehouseId,LotNumber=lotNumber,ExpirationDate=line.ExpirationDate,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdStockLots.Add(lot);lotMap[key]=lot;}
        }
        await _context.SaveChangesAsync(ct);
        foreach(var line in matched)
        {
            var materialId=line.PrdMaterialId!.Value;var lotNumber=string.IsNullOrWhiteSpace(line.LotNumber)?$"DEVIR-{line.LegacyMaterialId}":line.LotNumber.Trim();if(lotNumber.Length>100)lotNumber=lotNumber[..100];var lot=lotMap[$"{materialId}|{lotNumber}"];
            _context.PrdStockMovements.Add(new PrdStockMovement{MaterialId=materialId,WarehouseId=targetWarehouseId,StockLotId=lot.ID,Direction=PrdStockDirection.In,MovementType=PrdStockMovementType.Opening,Quantity=line.Quantity,UnitId=materials[materialId].UnitId,MovementDate=now,DocumentNumber=documentNumber,DocumentType=PrdStockDocumentType.Opening,Description="Mevcut portal stoklarından kontrollü başlangıç aktarımı",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
        }
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]=$"{matched.Count} lot/stok bakiyesi {target.Code} deposuna aktarıldı. Eşleşmeyen {model.UnmatchedCount} kalem aktarılmadı.";return RedirectToAction(nameof(Stoklar));
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
        var model=await BuildOrderCreateModel(planHeaderId,null,ct);
        if(model==null){TempData["error"]="Üretim emrine dönüştürülebilecek kilitli plan bulunamadı.";return RedirectToAction(nameof(Planlar));}
        return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> EmirOlustur(ProductionOrderCreateVM model,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var rebuilt=await BuildOrderCreateModel(model.PlanHeaderId,model.Lines,ct);
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
        var model=await(from order in _context.PrdProductionOrders.AsNoTracking() join plan in _context.PrdProductionPlans.AsNoTracking() on order.ProductionPlanId equals plan.ID join header in _context.PrdProductionPlanHeaders.AsNoTracking() on plan.ProductionPlanHeaderId equals header.ID join product in _context.PrdMaterials.AsNoTracking() on order.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on order.UnitId equals unit.ID join source in _context.PrdWarehouses.AsNoTracking() on order.SourceWarehouseId equals source.ID join target in _context.PrdWarehouses.AsNoTracking() on order.ProductionWarehouseId equals target.ID join version in _context.PrdRecipeVersions.AsNoTracking() on order.RecipeVersionId equals version.ID where order.ID==id&&order.IsDelete!=true select new ProductionOrderDetailVM{Id=order.ID,OrderNumber=order.OrderNumber,PlanNumber=header.PlanNumber,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=order.PlannedQuantity,Unit=unit.Name,BatchNumber=order.BatchNumber,PlannedProductionDate=order.PlannedProductionDate,Status=order.Status,RequirementCount=_context.PrdMaterialRequirements.Count(x=>x.ProductionOrderId==order.ID&&x.IsDelete!=true),SourceWarehouse=source.Code+" - "+source.Name,ProductionWarehouse=target.Code+" - "+target.Name,RecipeVersionNumber=version.VersionNumber,Notes=order.Notes}).FirstOrDefaultAsync(ct);
        if(model==null)return NotFound();
        model.Requirements=await(from requirement in _context.PrdMaterialRequirements.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on requirement.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on requirement.UnitId equals unit.ID where requirement.ProductionOrderId==id&&requirement.IsDelete!=true orderby material.Type,material.Code select new ProductionOrderRequirementVM{MaterialCode=material.Code,MaterialName=material.Name,MaterialType=material.Type,RequiredQuantity=requirement.TheoreticalQuantity,ReservedQuantity=requirement.ReservedQuantity,IssuedQuantity=requirement.IssuedQuantity,ConsumedQuantity=requirement.ConsumedQuantity,ReturnedQuantity=requirement.ReturnedQuantity,WasteQuantity=requirement.WasteQuantity,Unit=unit.Name}).ToListAsync(ct);
        return View(model);
    }
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
        var prefix=$"DEVIR-{legacyWarehouseId.Value}-{targetWarehouseId.Value}-";model.AlreadyImported=await _context.PrdStockMovements.AsNoTracking().AnyAsync(x=>x.DocumentType==PrdStockDocumentType.Opening&&x.DocumentNumber.StartsWith(prefix)&&x.IsDelete!=true,ct);
        var oldRows=await(from stock in _context.MalzemeStok.AsNoTracking() join material in _context.Malzeme.AsNoTracking() on stock.MalzemeID equals material.ID join unit in _context.MalzemeBirim.AsNoTracking() on material.BirimID equals unit.ID where stock.DepoID==legacyWarehouseId.Value&&stock.IsActive==true&&stock.IsDelete==false&&material.IsDelete==false select new{stock.MalzemeID,Code=material.Kod,LogoCode=material.LogoKod,Name=material.Ad,Unit=unit.Ad,stock.LotNumara,stock.SktTarih,stock.GirisCikis,stock.Miktar}).ToListAsync(ct);
        var balances=oldRows.GroupBy(x=>new{x.MalzemeID,x.Code,x.LogoCode,x.Name,x.Unit,Lot=(x.LotNumara??string.Empty).Trim()}).Select(g=>new{g.Key.MalzemeID,g.Key.Code,g.Key.LogoCode,g.Key.Name,g.Key.Unit,g.Key.Lot,Expiration=g.Where(x=>x.SktTarih.HasValue).Select(x=>x.SktTarih).Min(),Quantity=g.Sum(x=>Convert.ToDecimal(x.GirisCikis?x.Miktar:-x.Miktar))}).ToList();
        var prdMaterials=await _context.PrdMaterials.AsNoTracking().Where(x=>x.IsDelete!=true).Select(x=>new{x.ID,x.Code,x.LogoCode}).ToListAsync(ct);
        foreach(var materialGroup in balances.GroupBy(x=>x.MalzemeID))
        {
            var totalBalance=materialGroup.Sum(x=>x.Quantity);if(totalBalance<=0)continue;
            var positiveLots=materialGroup.Where(x=>x.Quantity>0).OrderBy(x=>x.Expiration??DateTime.MaxValue).ThenBy(x=>x.Lot).ToList();var amountToDeduct=Math.Max(0,positiveLots.Sum(x=>x.Quantity)-totalBalance);
            foreach(var x in positiveLots)
            {
                var deduction=Math.Min(amountToDeduct,x.Quantity);var quantity=x.Quantity-deduction;amountToDeduct-=deduction;if(quantity<=0)continue;
                var match=prdMaterials.FirstOrDefault(p=>string.Equals(p.Code,x.Code,StringComparison.OrdinalIgnoreCase)||(!string.IsNullOrWhiteSpace(x.LogoCode)&&(string.Equals(p.Code,x.LogoCode,StringComparison.OrdinalIgnoreCase)||string.Equals(p.LogoCode,x.LogoCode,StringComparison.OrdinalIgnoreCase)))||(!string.IsNullOrWhiteSpace(p.LogoCode)&&string.Equals(p.LogoCode,x.Code,StringComparison.OrdinalIgnoreCase)));
                model.Lines.Add(new LegacyStockImportLineVM{LegacyMaterialId=x.MalzemeID,PrdMaterialId=match?.ID,MaterialCode=x.Code??x.LogoCode??string.Empty,MaterialName=x.Name??string.Empty,LotNumber=x.Lot,ExpirationDate=x.Expiration,Quantity=quantity,Unit=x.Unit??string.Empty});
            }
        }
        model.Lines=model.Lines.OrderBy(x=>x.MaterialCode).ThenBy(x=>x.ExpirationDate).ToList();
        return model;
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

    private async Task<ProductionOrderCreateVM?> BuildOrderCreateModel(int planHeaderId,List<ProductionOrderCreateLineVM>? postedLines,CancellationToken ct)
    {
        var model=await _context.PrdProductionPlanHeaders.AsNoTracking().Where(x=>x.ID==planHeaderId&&x.Status==PrdProductionPlanHeaderStatus.Locked&&x.IsDelete!=true).Select(x=>new ProductionOrderCreateVM{PlanHeaderId=x.ID,PlanNumber=x.PlanNumber,PlannedProductionDate=x.TargetProductionDate,TotalShortageQuantity=_context.PrdProductionPlanRequirements.Where(r=>r.ProductionPlanHeaderId==x.ID&&r.IsDelete!=true).Sum(r=>(decimal?)r.ShortageQuantity)??0}).FirstOrDefaultAsync(ct);
        if(model==null)return null;
        model.SourceWarehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.Type==PrdWarehouseType.Main&&x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        model.ProductionWarehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.Type==PrdWarehouseType.Production&&x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        model.Lines=await(from plan in _context.PrdProductionPlans.AsNoTracking() join product in _context.PrdMaterials.AsNoTracking() on plan.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on plan.UnitId equals unit.ID join version in _context.PrdRecipeVersions.AsNoTracking() on plan.RecipeVersionId equals version.ID where plan.ProductionPlanHeaderId==planHeaderId&&!plan.IsConvertedToOrder&&plan.IsDelete!=true orderby plan.ID select new ProductionOrderCreateLineVM{ProductionPlanId=plan.ID,ProductCode=product.Code,ProductName=product.Name,PlannedQuantity=plan.PlannedQuantity,Unit=unit.Name,RecipeVersionNumber=version.VersionNumber}).ToListAsync(ct);
        if(model.Lines.Count==0)return null;
        if(postedLines!=null)foreach(var line in model.Lines){var posted=postedLines.FirstOrDefault(x=>x.ProductionPlanId==line.ProductionPlanId);if(posted!=null){line.BatchNumber=posted.BatchNumber;line.Notes=posted.Notes;}}
        return model;
    }
}
