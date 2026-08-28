using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy="AdminOrUretim")]
public sealed class ProductionInventoryController : Controller
{
    private readonly ApplicationDbContext _context;
    public ProductionInventoryController(ApplicationDbContext context)=>_context=context;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await _context.PrdInventoryDocuments.AsNoTracking().Where(x=>x.IsDelete!=true).OrderByDescending(x=>x.DocumentDate).ThenByDescending(x=>x.ID).Select(x=>new InventoryDocumentListVM{Id=x.ID,DocumentNumber=x.DocumentNumber,Type=x.Type,Status=x.Status,DocumentDate=x.DocumentDate,SourceWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.SourceWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",TargetWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.TargetWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",LineCount=_context.PrdInventoryDocumentLines.Count(l=>l.InventoryDocumentId==x.ID&&l.IsDelete!=true),TotalCost=x.TotalCost}).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Yeni(PrdInventoryDocumentType type=PrdInventoryDocumentType.Opening,int? sourceWarehouseId=null,int? targetWarehouseId=null,CancellationToken ct=default)
    {
        ViewBag.Modul="YeniUretim";
        if(type!=PrdInventoryDocumentType.Opening&&type!=PrdInventoryDocumentType.WarehouseTransfer)return BadRequest();
        var model=new InventoryDocumentCreateVM{Type=type,SourceWarehouseId=sourceWarehouseId,TargetWarehouseId=targetWarehouseId,DocumentDate=DateTime.Today,CurrencyCode="TRY",ExchangeRate="1"};
        await FillCreateModel(model,ct);return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(InventoryDocumentCreateVM model,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        if(model.Type!=PrdInventoryDocumentType.Opening&&model.Type!=PrdInventoryDocumentType.WarehouseTransfer)return BadRequest();
        var now=DateTime.Now;var user=User.Identity?.Name;var activeLines=model.Lines.Where(x=>model.Type==PrdInventoryDocumentType.Opening?x.MaterialId.HasValue:x.SourceStockLotId.HasValue).ToList();
        if(activeLines.Count==0)ModelState.AddModelError(string.Empty,"En az bir belge kalemi giriniz.");
        var target=await _context.PrdWarehouses.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==model.TargetWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct);
        if(target==null)ModelState.AddModelError(nameof(model.TargetWarehouseId),"Geçerli bir hedef depo seçiniz.");
        PrdWarehouse? source=null;
        if(model.Type==PrdInventoryDocumentType.WarehouseTransfer)
        {
            source=await _context.PrdWarehouses.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==model.SourceWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct);
            if(source==null)ModelState.AddModelError(nameof(model.SourceWarehouseId),"Geçerli bir kaynak depo seçiniz.");
            if(model.SourceWarehouseId==model.TargetWarehouseId)ModelState.AddModelError(string.Empty,"Kaynak ve hedef depo aynı olamaz.");
        }
        if(!TryParseDecimal(model.ExchangeRate,out var exchangeRate)||exchangeRate<=0)ModelState.AddModelError(nameof(model.ExchangeRate),"Geçerli bir kur giriniz.");
        var currency=(model.CurrencyCode??"TRY").Trim().ToUpperInvariant();if(currency.Length!=3)ModelState.AddModelError(nameof(model.CurrencyCode),"Para birimi üç karakter olmalıdır.");

        var preparedLines=new List<PrdInventoryDocumentLine>();
        if(model.Type==PrdInventoryDocumentType.Opening)
        {
            var materialIds=activeLines.Where(x=>x.MaterialId.HasValue).Select(x=>x.MaterialId!.Value).Distinct().ToList();var materials=await _context.PrdMaterials.AsNoTracking().Where(x=>materialIds.Contains(x.ID)&&x.IsActive!=false&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
            for(var i=0;i<activeLines.Count;i++)
            {
                var input=activeLines[i];if(!input.MaterialId.HasValue||!materials.TryGetValue(input.MaterialId.Value,out var material)){ModelState.AddModelError(string.Empty,$"{i+1}. satırdaki malzeme bulunamadı.");continue;}
                if(!TryParseDecimal(input.Quantity,out var quantity)||quantity<=0){ModelState.AddModelError(string.Empty,$"{i+1}. satır miktarı geçersiz.");continue;}
                if(!TryParseDecimal(input.UnitCost,out var originalUnitCost)||originalUnitCost<0){ModelState.AddModelError(string.Empty,$"{i+1}. satır birim maliyeti geçersiz.");continue;}
                var lot=(input.LotNumber??string.Empty).Trim();if(material.RequiresLotTracking&&string.IsNullOrWhiteSpace(lot)){ModelState.AddModelError(string.Empty,$"{material.Code} için lot numarası zorunludur.");continue;}if(material.RequiresExpirationDate&&!input.ExpirationDate.HasValue){ModelState.AddModelError(string.Empty,$"{material.Code} için son kullanma tarihi zorunludur.");continue;}
                var unitCost=originalUnitCost*exchangeRate;preparedLines.Add(new PrdInventoryDocumentLine{Sequence=preparedLines.Count+1,MaterialId=material.ID,UnitId=material.UnitId,LotNumber=string.IsNullOrWhiteSpace(lot)?"LOTSUZ":lot,ProductionDate=input.ProductionDate,ExpirationDate=input.ExpirationDate,Quantity=quantity,OriginalUnitCost=originalUnitCost,CurrencyCode=currency,ExchangeRate=exchangeRate,UnitCost=unitCost,TotalCost=quantity*unitCost,CostSource=PrdStockCostSource.Opening,Notes=input.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
            }
        }
        else
        {
            var balances=source==null?[]:await GetLotBalances(source.ID,ct);var duplicateLots=activeLines.Where(x=>x.SourceStockLotId.HasValue).GroupBy(x=>x.SourceStockLotId!.Value).Where(x=>x.Count()>1).Select(x=>x.Key).ToList();if(duplicateLots.Count>0)ModelState.AddModelError(string.Empty,"Aynı lot bir belgede birden fazla kez seçilemez.");
            for(var i=0;i<activeLines.Count;i++)
            {
                var input=activeLines[i];var balance=balances.FirstOrDefault(x=>x.StockLotId==input.SourceStockLotId);if(balance==null){ModelState.AddModelError(string.Empty,$"{i+1}. satırdaki kaynak lot bulunamadı.");continue;}
                if(!TryParseDecimal(input.Quantity,out var quantity)||quantity<=0||quantity>balance.AvailableQuantity){ModelState.AddModelError(string.Empty,$"{balance.MaterialCode} / {balance.LotNumber} için miktar kullanılabilir stoktan büyük veya geçersiz.");continue;}
                preparedLines.Add(new PrdInventoryDocumentLine{Sequence=preparedLines.Count+1,MaterialId=balance.MaterialId,UnitId=balance.UnitId,SourceStockLotId=balance.StockLotId,LotNumber=balance.LotNumber,ProductionDate=balance.ProductionDate,ExpirationDate=balance.ExpirationDate,Quantity=quantity,OriginalUnitCost=balance.UnitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=balance.UnitCost,TotalCost=quantity*balance.UnitCost,CostSource=PrdStockCostSource.Transfer,Notes=input.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
            }
        }
        if(preparedLines.Count==0&&activeLines.Count>0)ModelState.AddModelError(string.Empty,"Kaydedilebilir belge kalemi bulunamadı.");
        if(!ModelState.IsValid){await FillCreateModel(model,ct);return View(model);}
        var document=new PrdInventoryDocument{DocumentNumber=$"STK-{now:yyyyMMddHHmmssfff}",Type=model.Type,Status=PrdInventoryDocumentStatus.Draft,DocumentDate=model.DocumentDate.Date,SourceWarehouseId=model.Type==PrdInventoryDocumentType.WarehouseTransfer?source!.ID:null,TargetWarehouseId=target!.ID,CurrencyCode=model.Type==PrdInventoryDocumentType.Opening?currency:"TRY",ExchangeRate=model.Type==PrdInventoryDocumentType.Opening?exchangeRate:1,TotalCost=preparedLines.Sum(x=>x.TotalCost),Notes=model.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
        _context.PrdInventoryDocuments.Add(document);await _context.SaveChangesAsync(ct);foreach(var line in preparedLines)line.InventoryDocumentId=document.ID;_context.PrdInventoryDocumentLines.AddRange(preparedLines);await _context.SaveChangesAsync(ct);TempData["success"]=$"{document.DocumentNumber} numaralı taslak belge oluşturuldu.";return RedirectToAction(nameof(Detay),new{id=document.ID});
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await _context.PrdInventoryDocuments.AsNoTracking().Where(x=>x.ID==id&&x.IsDelete!=true).Select(x=>new InventoryDocumentDetailVM{Id=x.ID,DocumentNumber=x.DocumentNumber,Type=x.Type,Status=x.Status,DocumentDate=x.DocumentDate,PostingDate=x.PostingDate,PostedUserId=x.PostedUserId,SourceWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.SourceWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",TargetWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.TargetWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",CurrencyCode=x.CurrencyCode,ExchangeRate=x.ExchangeRate,TotalCost=x.TotalCost,Notes=x.Notes}).FirstOrDefaultAsync(ct);if(model==null)return NotFound();
        model.Lines=await(from line in _context.PrdInventoryDocumentLines.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID where line.InventoryDocumentId==id&&line.IsDelete!=true orderby line.Sequence select new InventoryDocumentDetailLineVM{Sequence=line.Sequence,MaterialCode=material.Code,MaterialName=material.Name,LotNumber=line.LotNumber??string.Empty,ExpirationDate=line.ExpirationDate,Quantity=line.Quantity,Unit=unit.Name,UnitCost=line.UnitCost,TotalCost=line.TotalCost,Notes=line.Notes}).ToListAsync(ct);model.LineCount=model.Lines.Count;return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> StokaIsle(int id,CancellationToken ct)
    {
        var document=await _context.PrdInventoryDocuments.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(document==null)return NotFound();if(document.Status!=PrdInventoryDocumentStatus.Draft){TempData["error"]="Yalnızca taslak belge stoka işlenebilir.";return RedirectToAction(nameof(Detay),new{id});}
        var lines=await _context.PrdInventoryDocumentLines.Where(x=>x.InventoryDocumentId==id&&x.IsDelete!=true).OrderBy(x=>x.Sequence).ToListAsync(ct);if(lines.Count==0){TempData["error"]="Belgede kalem bulunmuyor.";return RedirectToAction(nameof(Detay),new{id});}
        try
        {
            await using var transaction=await _context.Database.BeginTransactionAsync(ct);var now=DateTime.Now;var user=User.Identity?.Name;
            if(document.Type==PrdInventoryDocumentType.Opening)await PostOpening(document,lines,now,user,ct);else if(document.Type==PrdInventoryDocumentType.WarehouseTransfer)await PostTransfer(document,lines,now,user,ct);else throw new InvalidOperationException("Bu belge türü henüz stoka işlenemez.");
            document.Status=PrdInventoryDocumentStatus.Posted;document.PostingDate=now;document.PostedUserId=user;document.UpdateDate=now;document.UpdateUserID=user;document.TotalCost=lines.Sum(x=>x.TotalCost);await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]="Belge stoka işlendi; hareketler artık belge üzerinden değiştirilemez.";
        }
        catch(InvalidOperationException ex){TempData["error"]=ex.Message;}
        return RedirectToAction(nameof(Detay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> TaslagiIptalEt(int id,CancellationToken ct)
    {
        var document=await _context.PrdInventoryDocuments.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(document==null)return NotFound();if(document.Status!=PrdInventoryDocumentStatus.Draft){TempData["error"]="Yalnızca taslak belge iptal edilebilir.";return RedirectToAction(nameof(Detay),new{id});}document.Status=PrdInventoryDocumentStatus.Cancelled;document.IsActive=false;document.UpdateDate=DateTime.Now;document.UpdateUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);TempData["success"]="Taslak stok belgesi iptal edildi.";return RedirectToAction(nameof(Detay),new{id});
    }

    private async Task PostOpening(PrdInventoryDocument document,List<PrdInventoryDocumentLine> lines,DateTime now,string? user,CancellationToken ct)
    {
        if(!document.TargetWarehouseId.HasValue||!await _context.PrdWarehouses.AnyAsync(x=>x.ID==document.TargetWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct))throw new InvalidOperationException("Hedef depo aktif değil veya bulunamadı.");var materials=await _context.PrdMaterials.Where(x=>lines.Select(l=>l.MaterialId).Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        foreach(var line in lines)
        {
            if(line.Quantity<=0||line.UnitCost<0)throw new InvalidOperationException($"{line.Sequence}. satır miktar veya maliyet bilgisi geçersiz.");if(!materials.TryGetValue(line.MaterialId,out var material))throw new InvalidOperationException($"{line.Sequence}. satırdaki malzeme artık kullanılamıyor.");var lotNumber=string.IsNullOrWhiteSpace(line.LotNumber)?"LOTSUZ":line.LotNumber.Trim();if(material.RequiresLotTracking&&lotNumber=="LOTSUZ")throw new InvalidOperationException($"{material.Code} için lot numarası zorunludur.");if(material.RequiresExpirationDate&&!line.ExpirationDate.HasValue)throw new InvalidOperationException($"{material.Code} için SKT zorunludur.");
            var lot=await GetOrCreateTargetLot(line.MaterialId,document.TargetWarehouseId.Value,lotNumber,line.ProductionDate,line.ExpirationDate,now,user,ct);line.TargetStockLotId=lot.ID;line.TotalCost=line.Quantity*line.UnitCost;
            _context.PrdStockMovements.Add(CreateMovement(document,line,document.TargetWarehouseId.Value,lot.ID,PrdStockDirection.In,PrdStockMovementType.Opening,line.UnitCost,line.TotalCost,now,user));
        }
    }

    private async Task PostTransfer(PrdInventoryDocument document,List<PrdInventoryDocumentLine> lines,DateTime now,string? user,CancellationToken ct)
    {
        if(!document.SourceWarehouseId.HasValue||!document.TargetWarehouseId.HasValue||document.SourceWarehouseId==document.TargetWarehouseId)throw new InvalidOperationException("Transfer depo bilgileri geçersiz.");var target=await _context.PrdWarehouses.FirstOrDefaultAsync(x=>x.ID==document.TargetWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct)??throw new InvalidOperationException("Hedef depo aktif değil veya bulunamadı.");var balances=await GetLotBalances(document.SourceWarehouseId.Value,ct);
        foreach(var line in lines)
        {
            var balance=balances.FirstOrDefault(x=>x.StockLotId==line.SourceStockLotId)??throw new InvalidOperationException($"{line.Sequence}. satırdaki kaynak lot bulunamadı.");if(line.Quantity<=0||line.Quantity>balance.AvailableQuantity)throw new InvalidOperationException($"{balance.MaterialCode} / {balance.LotNumber} için kullanılabilir stok yetersiz.");if(balance.ExpirationDate.HasValue&&balance.ExpirationDate.Value.Date<DateTime.Today&&target.Type!=PrdWarehouseType.Scrap)throw new InvalidOperationException($"{balance.MaterialCode} / {balance.LotNumber} süresi geçmiş; yalnızca hurda deposuna aktarılabilir.");
            line.UnitCost=balance.UnitCost;line.OriginalUnitCost=balance.UnitCost;line.CurrencyCode="TRY";line.ExchangeRate=1;line.TotalCost=line.Quantity*line.UnitCost;line.CostSource=PrdStockCostSource.Transfer;var targetLot=await GetOrCreateTargetLot(balance.MaterialId,target.ID,balance.LotNumber,balance.ProductionDate,balance.ExpirationDate,now,user,ct);line.TargetStockLotId=targetLot.ID;
            _context.PrdStockMovements.Add(CreateMovement(document,line,document.SourceWarehouseId.Value,balance.StockLotId,PrdStockDirection.Out,PrdStockMovementType.Transfer,line.UnitCost,line.TotalCost,now,user));_context.PrdStockMovements.Add(CreateMovement(document,line,target.ID,targetLot.ID,PrdStockDirection.In,PrdStockMovementType.Transfer,line.UnitCost,line.TotalCost,now,user));
        }
    }

    private PrdStockMovement CreateMovement(PrdInventoryDocument document,PrdInventoryDocumentLine line,int warehouseId,int lotId,PrdStockDirection direction,PrdStockMovementType movementType,decimal unitCost,decimal totalCost,DateTime now,string? user)=>new(){InventoryDocumentId=document.ID,InventoryDocumentLineId=line.ID,MaterialId=line.MaterialId,WarehouseId=warehouseId,StockLotId=lotId,Direction=direction,MovementType=movementType,Quantity=line.Quantity,UnitId=line.UnitId,OriginalUnitCost=line.OriginalUnitCost,CurrencyCode=line.CurrencyCode,ExchangeRate=line.ExchangeRate,UnitCost=unitCost,TotalCost=totalCost,CostSource=line.CostSource,MovementDate=document.DocumentDate,DocumentNumber=document.DocumentNumber,DocumentType=PrdStockDocumentType.InventoryDocument,DocumentId=document.ID,TransferNumber=document.Type==PrdInventoryDocumentType.WarehouseTransfer?document.DocumentNumber:null,Description=document.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};

    private async Task<PrdStockLot> GetOrCreateTargetLot(int materialId,int warehouseId,string lotNumber,DateTime? productionDate,DateTime? expirationDate,DateTime now,string? user,CancellationToken ct)
    {
        var lot=await _context.PrdStockLots.FirstOrDefaultAsync(x=>x.MaterialId==materialId&&x.WarehouseId==warehouseId&&x.LotNumber==lotNumber&&x.IsDelete!=true,ct);if(lot!=null){if(lot.ExpirationDate.HasValue&&expirationDate.HasValue&&lot.ExpirationDate.Value.Date!=expirationDate.Value.Date)throw new InvalidOperationException($"{lotNumber} lotunun hedef depodaki SKT bilgisi farklı.");return lot;}lot=new PrdStockLot{MaterialId=materialId,WarehouseId=warehouseId,LotNumber=lotNumber,ProductionDate=productionDate,ExpirationDate=expirationDate,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};_context.PrdStockLots.Add(lot);await _context.SaveChangesAsync(ct);return lot;
    }

    private async Task FillCreateModel(InventoryDocumentCreateVM model,CancellationToken ct)
    {
        model.Warehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Type).ThenBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name+" ("+x.Type.ToTurkish()+")",x.ID.ToString())).ToListAsync(ct);model.Materials=await _context.PrdMaterials.AsNoTracking().Where(x=>x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name,x.ID.ToString())).ToListAsync(ct);
        if(model.Type==PrdInventoryDocumentType.WarehouseTransfer&&model.SourceWarehouseId.HasValue){var balances=await GetLotBalances(model.SourceWarehouseId.Value,ct);model.SourceLots=balances.Where(x=>x.AvailableQuantity>0).Select(x=>new SelectListItem($"{x.MaterialCode} - {x.MaterialName} | Lot: {x.LotNumber} | Kullanılabilir: {x.AvailableQuantity:0.######} {x.Unit} | Maliyet: {x.UnitCost:N6} ₺"+(x.ExpirationDate.HasValue?$" | SKT: {x.ExpirationDate:dd.MM.yyyy}":string.Empty),x.StockLotId.ToString())).ToList();}
        while(model.Lines.Count<15)model.Lines.Add(new InventoryDocumentCreateLineVM());
    }

    private async Task<List<InventoryLotBalanceVM>> GetLotBalances(int warehouseId,CancellationToken ct)
    {
        var rows=await(from movement in _context.PrdStockMovements.AsNoTracking() join lot in _context.PrdStockLots.AsNoTracking() on movement.StockLotId equals lot.ID join material in _context.PrdMaterials.AsNoTracking() on movement.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID where movement.WarehouseId==warehouseId&&movement.IsDelete!=true&&lot.IsDelete!=true select new{Lot=lot,Material=material,Unit=unit,movement.Direction,movement.Quantity,movement.TotalCost}).ToListAsync(ct);var lotIds=rows.Select(x=>x.Lot.ID).Distinct().ToList();var reservations=await _context.PrdStockReservations.AsNoTracking().Where(x=>lotIds.Contains(x.StockLotId)&&x.IsDelete!=true&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)).GroupBy(x=>x.StockLotId).Select(g=>new{LotId=g.Key,Quantity=g.Sum(x=>x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity)}).ToListAsync(ct);
        return rows.GroupBy(x=>new{x.Lot.ID,x.Lot.MaterialId,x.Material.UnitId,x.Material.Code,x.Material.Name,x.Lot.LotNumber,x.Lot.ProductionDate,x.Lot.ExpirationDate,Unit=x.Unit.Name}).Select(g=>new InventoryLotBalanceVM{StockLotId=g.Key.ID,MaterialId=g.Key.MaterialId,UnitId=g.Key.UnitId,MaterialCode=g.Key.Code,MaterialName=g.Key.Name,LotNumber=g.Key.LotNumber,ProductionDate=g.Key.ProductionDate,ExpirationDate=g.Key.ExpirationDate,PhysicalQuantity=g.Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity),ReservedQuantity=reservations.Where(x=>x.LotId==g.Key.ID).Sum(x=>x.Quantity),StockValue=g.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost),Unit=g.Key.Unit}).Where(x=>x.PhysicalQuantity>0).OrderBy(x=>x.ExpirationDate??DateTime.MaxValue).ThenBy(x=>x.MaterialCode).ToList();
    }

    private static bool TryParseDecimal(string? value,out decimal result)
    {
        result=0;if(string.IsNullOrWhiteSpace(value))return false;var normalized=value.Trim().Replace(" ",string.Empty);if(normalized.Contains(',')&&normalized.Contains('.'))normalized=normalized.LastIndexOf(',')>normalized.LastIndexOf('.')?normalized.Replace(".",string.Empty).Replace(',','.'):normalized.Replace(",",string.Empty);else if(normalized.Contains(','))normalized=normalized.Replace(',','.');return decimal.TryParse(normalized,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out result);
    }
}
