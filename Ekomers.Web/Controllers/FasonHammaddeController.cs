using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Policy="AdminOrUretim")]
public sealed class FasonHammaddeController(ApplicationDbContext db,FasonStockService stock):Controller
{
    private string Actor=>User.Identity?.Name??"Kullanıcı";
    private void Module()=>ViewBag.Modul="YeniUretim";
    private Task<List<SelectListItem>> Warehouses(CancellationToken ct,bool activeOnly=false)=>db.FsnWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true&&(!activeOnly||x.IsActive!=false)).OrderBy(x=>x.CustomerName).Select(x=>new SelectListItem(x.CustomerCode+" - "+x.CustomerName+" / "+x.Code,x.ID.ToString())).ToListAsync(ct);

    [HttpGet]
    public async Task<IActionResult> Index(int? warehouseId,int page=1,CancellationToken ct=default)
    {
        Module();var q=from r in db.FsnReceipts.AsNoTracking() join w in db.FsnWarehouses.AsNoTracking() on r.WarehouseId equals w.ID where r.IsDelete!=true&&w.IsDelete!=true&&(!warehouseId.HasValue||w.ID==warehouseId) select new FasonReceiptListVM{Id=r.ID,Number=r.Number,Date=r.ReceiptDate,WarehouseId=w.ID,Warehouse=w.Code,Customer=w.CustomerCode+" - "+w.CustomerName,DispatchNumber=r.DispatchNumber,Status=r.Status};
        var model=new FasonDocumentPageVM{WarehouseId=warehouseId,Warehouses=await Warehouses(ct),Count=await q.CountAsync(ct)};model.Page=Math.Clamp(page,1,Math.Max(1,model.PageCount));model.Rows=await q.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).Skip((model.Page-1)*50).Take(50).ToListAsync(ct);return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Depolar(CancellationToken ct){Module();return View(await db.FsnWarehouses.AsNoTracking().Where(x=>x.IsDelete!=true).OrderBy(x=>x.CustomerName).ToListAsync(ct));}

    [HttpGet,Authorize(Policy="FasonHammaddeYonet")]
    public IActionResult YeniDepo(){Module();return View(new FasonWarehouseFormVM());}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="FasonHammaddeYonet")]
    public async Task<IActionResult> YeniDepo(FasonWarehouseFormVM model,CancellationToken ct)
    {
        Module();model.CustomerCode=(model.CustomerCode??"").Trim().ToUpperInvariant();model.CustomerName=(model.CustomerName??"").Trim();
        if(string.IsNullOrEmpty(model.CustomerCode)||string.IsNullOrEmpty(model.CustomerName))ModelState.AddModelError("","Firma kodu ve unvanı zorunludur.");
        if(await db.FsnWarehouses.AnyAsync(x=>x.CustomerCode==model.CustomerCode,ct))ModelState.AddModelError(nameof(model.CustomerCode),"Bu firmaya ait fason depo zaten var.");
        if(!ModelState.IsValid)return View(model);
        var now=DateTime.Now;db.FsnWarehouses.Add(new(){Code="FAS-"+Guid.NewGuid().ToString("N")[..10].ToUpperInvariant(),Name=model.CustomerName+" Fason Deposu",CustomerCode=model.CustomerCode,CustomerName=model.CustomerName,Notes=model.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=Actor});
        try{await db.SaveChangesAsync(ct);}catch(DbUpdateException){ModelState.AddModelError("","Firma/depo kaydedilemedi. Firma kodunun daha önce kullanılmadığını kontrol ediniz.");return View(model);}
        TempData["success"]="Firmaya özel fason depo oluşturuldu.";return RedirectToAction(nameof(Depolar));
    }

    [HttpGet,Authorize(Policy="FasonHammaddeYonet")]
    public async Task<IActionResult> YeniGiris(int? warehouseId,CancellationToken ct){Module();var model=new FasonReceiptFormVM{WarehouseId=warehouseId};await Fill(model,ct);return View(model);}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="FasonHammaddeYonet")]
    public async Task<IActionResult> YeniGiris(FasonReceiptFormVM model,CancellationToken ct)
    {
        Module();if(model.Lines.Count>100)ModelState.AddModelError("","En fazla 100 satır girilebilir.");
        if(ModelState.IsValid){try{var id=await stock.CreateAsync(model,Actor,ct);TempData["success"]="Fason hammadde girişi taslak olarak kaydedildi. Stoğa almak için belgeyi onaylayınız.";return RedirectToAction(nameof(Detay),new{id});}catch(InvalidOperationException ex){ModelState.AddModelError("",ex.Message);}catch(DbUpdateException){ModelState.AddModelError("","Belge kaydedilemedi. Aynı form tekrar gönderildiyse giriş belgelerini kontrol ediniz.");}}
        await Fill(model,ct);return View(model);
    }
    private async Task Fill(FasonReceiptFormVM model,CancellationToken ct)
    {
        model.Warehouses=await Warehouses(ct,true);
        model.Materials=await(from m in db.PrdMaterials.AsNoTracking() join u in db.PrdUnits.AsNoTracking() on m.UnitId equals u.ID where m.IsDelete!=true&&m.IsActive!=false&&u.IsDelete!=true&&u.IsActive!=false&&(m.Type==PrdMaterialType.RawMaterial||m.Type==PrdMaterialType.Packaging) orderby m.Code select new SelectListItem(m.Code+" - "+m.Name+" (Ana birim: "+u.Name+")",m.ID.ToString())).ToListAsync(ct);
        model.Units=await db.PrdUnits.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false).OrderBy(x=>x.Name).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        if(model.Lines.Count==0)model.Lines.Add(new());
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id,CancellationToken ct)
    {
        Module();var receipt=await db.FsnReceipts.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(receipt==null)return NotFound();
        var warehouse=await db.FsnWarehouses.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==receipt.WarehouseId&&x.IsDelete!=true,ct);if(warehouse==null)return NotFound();
        var model=new FasonReceiptDetailVM{Receipt=receipt,Warehouse=warehouse,Lines=await(from l in db.FsnReceiptLines.AsNoTracking() join m in db.PrdMaterials.AsNoTracking() on l.MaterialId equals m.ID join u in db.PrdUnits.AsNoTracking() on l.UnitId equals u.ID join original in db.PrdUnits.AsNoTracking() on l.OriginalUnitId equals original.ID where l.ReceiptId==id&&l.IsDelete!=true orderby l.Sequence select new FasonReceiptDetailLineVM{Id=l.ID,Sequence=l.Sequence,Material=m.Code+" - "+m.Name,LotNumber=l.LotNumber,OriginalQuantity=l.OriginalQuantity,OriginalUnit=original.Name,Quantity=l.Quantity,Unit=u.Name,ProductionDate=l.ProductionDate,ExpirationDate=l.ExpirationDate,Notes=l.Notes}).ToListAsync(ct)};return View(model);
    }
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="FasonHammaddeYonet")]
    public async Task<IActionResult> StogaAl(int id,CancellationToken ct){try{await stock.PostAsync(id,Actor,ct);TempData["success"]="Müşteriye ait hammadde firma deposuna alındı.";}catch(InvalidOperationException ex){TempData["error"]=ex.Message;}return RedirectToAction(nameof(Detay),new{id});}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="FasonHammaddeYonet")]
    public async Task<IActionResult> Iptal(int id,CancellationToken ct){try{await stock.CancelAsync(id,Actor,ct);TempData["success"]="Taslak iptal edildi.";}catch(InvalidOperationException ex){TempData["error"]=ex.Message;}return RedirectToAction(nameof(Detay),new{id});}
    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="FasonHammaddeGeriAl")]
    public async Task<IActionResult> GeriAl(int id,string? reason,CancellationToken ct){try{await stock.ReverseAsync(id,reason??"",Actor,ct);TempData["success"]="Giriş ters hareketle geri alındı. Belge ve işlem geçmişi korundu.";}catch(InvalidOperationException ex){TempData["error"]=ex.Message;}return RedirectToAction(nameof(Detay),new{id});}

    [HttpGet]
    public async Task<IActionResult> Stoklar(int? warehouseId,string? search,int page=1,CancellationToken ct=default)
    {
        Module();search=string.IsNullOrWhiteSpace(search)?null:search.Trim();var q=stock.Stock(warehouseId,search);var model=new FasonStockPageVM{WarehouseId=warehouseId,Search=search,Warehouses=await Warehouses(ct),Count=await q.CountAsync(ct)};model.Page=Math.Clamp(page,1,Math.Max(1,model.PageCount));model.Rows=await q.OrderBy(x=>x.Customer).ThenBy(x=>x.WarehouseId).ThenBy(x=>x.MaterialId).ThenBy(x=>x.LotNumber).ThenBy(x=>x.ProductionDate).ThenBy(x=>x.ExpirationDate).ThenBy(x=>x.UnitId).Skip((model.Page-1)*50).Take(50).ToListAsync(ct);return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> Gecmis(int? warehouseId,int? materialId,string? lotNumber,int page=1,CancellationToken ct=default)
    {
        Module();var q=stock.History(warehouseId,materialId,lotNumber);var model=new FasonHistoryPageVM{WarehouseId=warehouseId,MaterialId=materialId,LotNumber=lotNumber,Warehouses=await Warehouses(ct),Count=await q.CountAsync(ct)};model.Page=Math.Clamp(page,1,Math.Max(1,model.PageCount));model.Rows=await q.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Id).Skip((model.Page-1)*50).Take(50).ToListAsync(ct);return View(model);
    }
}
