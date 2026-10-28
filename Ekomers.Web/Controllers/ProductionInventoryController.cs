using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace Ekomers.Web.Controllers;

[Authorize(Policy="AdminOrUretim")]
public sealed partial class ProductionInventoryController : Controller
{
    private const string InventoryDocumentDeletePolicy = "StokBelgesiSil";
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ProductionInventoryController>? _logger;
    public ProductionInventoryController(ApplicationDbContext context,ILogger<ProductionInventoryController>? logger=null){_context=context;_logger=logger;}

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct,string? productSearch=null)
    {
        ViewBag.Modul="YeniUretim";
        productSearch=productSearch?.Trim();
        ViewBag.ProductSearch=productSearch;
        var query=_context.PrdInventoryDocuments.AsNoTracking().Where(x=>x.IsDelete!=true);
        if(!string.IsNullOrEmpty(productSearch))
            query=query.Where(document=>(from line in _context.PrdInventoryDocumentLines
                                        join material in _context.PrdMaterials on line.MaterialId equals material.ID
                                        where line.InventoryDocumentId==document.ID&&line.IsDelete!=true
                                            &&(material.Code.Contains(productSearch)||material.Name.Contains(productSearch))
                                        select line.ID).Any());
        var model=await query.OrderByDescending(x=>x.DocumentDate).ThenByDescending(x=>x.ID).Select(x=>new InventoryDocumentListVM{Id=x.ID,DocumentNumber=x.DocumentNumber,Type=x.Type,Status=x.Status,DocumentDate=x.DocumentDate,SourceWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.SourceWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",TargetWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.TargetWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",LineCount=_context.PrdInventoryDocumentLines.Count(l=>l.InventoryDocumentId==x.ID&&l.IsDelete!=true),TotalCost=x.TotalCost,CanDeleteFromInventory=(x.Type==PrdInventoryDocumentType.Opening||x.Type==PrdInventoryDocumentType.WarehouseTransfer)&&x.SourceDocumentType==null&&!x.SourceDocumentId.HasValue&&!x.ReversalDocumentId.HasValue}).ToListAsync(ct);
        if(!string.IsNullOrEmpty(productSearch)&&model.Count>0)
        {
            var matches=await(from line in _context.PrdInventoryDocumentLines.AsNoTracking()
                              join document in query on line.InventoryDocumentId equals document.ID
                              join material in _context.PrdMaterials on line.MaterialId equals material.ID
                              join unit in _context.PrdUnits on line.UnitId equals unit.ID into units
                              from unit in units.DefaultIfEmpty()
                              where line.IsDelete!=true&&(material.Code.Contains(productSearch)||material.Name.Contains(productSearch))
                              group line by new{line.InventoryDocumentId,line.MaterialId,material.Code,material.Name,line.UnitId,UnitName=unit==null?null:unit.Name} into g
                              select new{DocumentId=g.Key.InventoryDocumentId,Product=new InventoryDocumentMatchedProductVM
                              {
                                  MaterialCode=g.Key.Code,MaterialName=g.Key.Name,Quantity=g.Sum(x=>x.Quantity),
                                  Unit=g.Key.UnitName??("Birim #"+g.Key.UnitId)
                              }}).ToListAsync(ct);
            var byDocument=matches.ToLookup(x=>x.DocumentId,x=>x.Product);
            foreach(var document in model)
                document.MatchingProducts=byDocument[document.Id].OrderBy(x=>x.MaterialCode).ThenBy(x=>x.Unit).ToList();
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Yeni(PrdInventoryDocumentType type=PrdInventoryDocumentType.Opening,int? sourceWarehouseId=null,int? targetWarehouseId=null,int? materialId=null,string? quantity=null,int? quantityUnitId=null,CancellationToken ct=default)
    {
        ViewBag.Modul="YeniUretim";
        if(type!=PrdInventoryDocumentType.Opening&&type!=PrdInventoryDocumentType.WarehouseTransfer)return BadRequest();
        var model=new InventoryDocumentCreateVM{Type=type,SourceWarehouseId=sourceWarehouseId,TargetWarehouseId=targetWarehouseId,DocumentDate=DateTime.Today,CurrencyCode="TRY",ExchangeRate="1"};
        await FillCreateModel(model,ct);
        if(type==PrdInventoryDocumentType.Opening&&materialId.HasValue)
        {
            var material=await(from item in _context.PrdMaterials.AsNoTracking()
                               join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID
                               where item.ID==materialId.Value&&item.IsActive!=false&&item.IsDelete!=true
                               select new{item.ID,item.Code,item.UnitId,UnitCode=unit.Code,UnitName=unit.Name}).FirstOrDefaultAsync(ct);
            if(material!=null)
            {
                model.Lines[0].MaterialId=material.ID;
                model.Lines[0].Notes="MRP eksik stok tamamlaması";
                model.Notes=$"MRP eksik stok tamamlaması - {material.Code}";
                if(TryParseDecimal(quantity,out var requestedQuantity)&&requestedQuantity>0)
                {
                    var convertedQuantity=requestedQuantity;
                    var sourceUnitName=material.UnitName;
                    if(quantityUnitId.HasValue&&quantityUnitId.Value!=material.UnitId)
                    {
                        var sourceUnit=await _context.PrdUnits.AsNoTracking().Where(x=>x.ID==quantityUnitId.Value).Select(x=>new{x.Code,x.Name}).FirstOrDefaultAsync(ct);
                        sourceUnitName=sourceUnit?.Name??string.Empty;
                        convertedQuantity=sourceUnit==null?0:ConvertProductionQuantity(requestedQuantity,quantityUnitId.Value,sourceUnit.Code,sourceUnit.Name,material.UnitId,material.UnitCode,material.UnitName);
                    }
                    if(convertedQuantity>0)
                    {
                        model.Lines[0].Quantity=convertedQuantity.ToString("0.######",CultureInfo.GetCultureInfo("tr-TR"));
                        ViewBag.PrefillInfo=$"MRP eksik miktarı {requestedQuantity:0.######} {sourceUnitName}; stok kartı biriminde {convertedQuantity:0.######} {material.UnitName} olarak hazırlandı.";
                    }
                    else ViewBag.PrefillWarning="Eksik miktarın birimi malzemenin stok birimine çevrilemedi. Miktarı lütfen elle giriniz.";
                }
            }
        }
        return View(model);
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
                if(balance.BlockReason!=null){ModelState.AddModelError(string.Empty,$"{balance.MaterialCode} / {balance.LotNumber}: {balance.BlockReason}");continue;}
                if(!Ekomers.Web.Services.RawMaterialConsumptionService.TryQuantity(input.Quantity,out var quantity)||quantity>balance.AvailableQuantity){ModelState.AddModelError(string.Empty,$"{balance.MaterialCode} / {balance.LotNumber} için miktar kullanılabilir {balance.AvailableQuantity:0.######} {balance.Unit} stoktan büyük veya geçersiz.");continue;}
                preparedLines.Add(new PrdInventoryDocumentLine{Sequence=preparedLines.Count+1,MaterialId=balance.MaterialId,UnitId=balance.UnitId,SourceStockLotId=balance.StockLotId,LotNumber=balance.LotNumber,ProductionDate=balance.ProductionDate,ExpirationDate=balance.ExpirationDate,Quantity=quantity,OriginalUnitCost=balance.UnitCost,CurrencyCode="TRY",ExchangeRate=1,UnitCost=balance.UnitCost,TotalCost=TransferCost(quantity,balance),CostSource=PrdStockCostSource.Transfer,Notes=input.Notes?.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user});
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
        if(await _context.PrdInventoryDocuments.AnyAsync(x=>x.ID==id&&x.IsDelete!=true&&x.Type==PrdInventoryDocumentType.RawMaterialConsumption,ct))
            return RedirectToAction("Detay","HammaddeSarf",new{id});
        var model=await _context.PrdInventoryDocuments.AsNoTracking().Where(x=>x.ID==id&&x.IsDelete!=true).Select(x=>new InventoryDocumentDetailVM{Id=x.ID,DocumentNumber=x.DocumentNumber,Type=x.Type,Status=x.Status,DocumentDate=x.DocumentDate,PostingDate=x.PostingDate,PostedUserId=x.PostedUserId,SourceWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.SourceWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",TargetWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.TargetWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",CurrencyCode=x.CurrencyCode,ExchangeRate=x.ExchangeRate,TotalCost=x.TotalCost,Notes=x.Notes,SourceDocumentType=x.SourceDocumentType,SourceDocumentId=x.SourceDocumentId,ReversalDocumentId=x.ReversalDocumentId,CanDeleteFromInventory=(x.Type==PrdInventoryDocumentType.Opening||x.Type==PrdInventoryDocumentType.WarehouseTransfer)&&x.SourceDocumentType==null&&!x.SourceDocumentId.HasValue&&!x.ReversalDocumentId.HasValue}).FirstOrDefaultAsync(ct);if(model==null)return NotFound();
        model.Lines=await(from line in _context.PrdInventoryDocumentLines.AsNoTracking() join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID where line.InventoryDocumentId==id&&line.IsDelete!=true orderby line.Sequence select new InventoryDocumentDetailLineVM{Id=line.ID,Sequence=line.Sequence,MaterialCode=material.Code,MaterialName=material.Name,LotNumber=line.LotNumber??string.Empty,ExpirationDate=line.ExpirationDate,Quantity=line.Quantity,Unit=unit.Name,UnitCost=line.UnitCost,TotalCost=line.TotalCost,MovementType=_context.PrdStockMovements.Where(m=>m.InventoryDocumentLineId==line.ID&&m.IsDelete!=true).Select(m=>(PrdStockMovementType?)m.MovementType).FirstOrDefault(),Notes=line.Notes}).ToListAsync(ct);model.LineCount=model.Lines.Count;
        model.CanDeleteLines=CanDeleteDocumentLines(new PrdInventoryDocument{Type=model.Type,Status=model.Status,SourceDocumentType=model.SourceDocumentType,SourceDocumentId=model.SourceDocumentId,ReversalDocumentId=model.ReversalDocumentId})&&!await HasCorrectionWorkflowLinksAsync(id,model.Lines.Select(x=>x.Id).ToList(),ct);
        return View(model);
    }

    [HttpGet,Authorize(Roles="Admin")]
    public async Task<IActionResult> Duzenle(int id,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await BuildEditModel(id,null,ct);
        if(model==null)return NotFound();
        if(model.Status==PrdInventoryDocumentStatus.Cancelled||model.Status==PrdInventoryDocumentStatus.Reversed)
        {
            TempData["error"]="İptal edilmiş veya ters kayıtla kapatılmış stok belgesi düzenlenemez.";
            return RedirectToAction(nameof(Detay),new{id});
        }
        return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    [RequestFormLimits(ValueCountLimit=10000)]
    public Task<IActionResult> Duzenle(InventoryDocumentEditVM model,CancellationToken ct)=>RunCorrectionAsync(model.Id,nameof(Duzenle),model,()=>SaveInventoryEditAsync(model,null,ct),ct);

    [HttpGet,Authorize(Roles="Admin")]
    public async Task<IActionResult> SatirDuzenle(int id,int lineId,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        var model=await BuildEditModel(id,null,ct);
        if(model==null)return NotFound();
        if(model.Status==PrdInventoryDocumentStatus.Cancelled||model.Status==PrdInventoryDocumentStatus.Reversed)
        {
            TempData["error"]="İptal edilmiş veya ters kayıtla kapatılmış stok belgesi düzenlenemez.";
            return RedirectToAction(nameof(Detay),new{id});
        }
        model.EditLineId=lineId;
        model.Lines=model.Lines.Where(x=>x.Id==lineId).ToList();
        return model.Lines.Count==1?View(model):NotFound();
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public Task<IActionResult> SatirDuzenle(InventoryDocumentEditVM model,CancellationToken ct)=>
        model.EditLineId is >0?RunCorrectionAsync(model.Id,nameof(SatirDuzenle),model,()=>SaveInventoryEditAsync(model,model.EditLineId,ct),ct):Task.FromResult<IActionResult>(BadRequest());

    private async Task<IActionResult> SaveInventoryEditAsync(InventoryDocumentEditVM model,int? editingLineId,CancellationToken ct)
    {
        ViewBag.Modul="YeniUretim";
        model.EditReason=model.EditReason?.Trim();
        if(string.IsNullOrWhiteSpace(model.EditReason)||model.EditReason.Length<5||model.EditReason.Length>500)
            ModelState.AddModelError(nameof(model.EditReason),"Düzenleme nedeni 5-500 karakter olmalıdır.");
        if(!editingLineId.HasValue&&model.DocumentDate==default)ModelState.AddModelError(nameof(model.DocumentDate),"Belge tarihi zorunludur.");
        var currency=(model.CurrencyCode??string.Empty).Trim().ToUpperInvariant();
        if(!editingLineId.HasValue&&currency.Length!=3)ModelState.AddModelError(nameof(model.CurrencyCode),"Para birimi üç karakter olmalıdır.");
        var exchangeRate=1m;
        if(!editingLineId.HasValue&&(!TryParseDecimal(model.ExchangeRate,out exchangeRate)||exchangeRate<=0))
            ModelState.AddModelError(nameof(model.ExchangeRate),"Geçerli bir kur giriniz.");

        await using var transaction=await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var document=await LockCorrectionDocumentAsync(model.Id,ct);
        if(document==null)return NotFound();
        if(document.Status==PrdInventoryDocumentStatus.Cancelled||document.Status==PrdInventoryDocumentStatus.Reversed)
        {
            TempData["error"]="İptal edilmiş veya ters kayıtla kapatılmış stok belgesi düzenlenemez.";
            return RedirectToAction(nameof(Detay),new{id=model.Id});
        }
        var now=DateTime.Now;
        var actor=User.Identity?.Name??"Admin";

        await LockCorrectionWarehousesAsync(document,ct);

        var allLines=await _context.PrdInventoryDocumentLines.Where(x=>x.InventoryDocumentId==document.ID&&x.IsDelete!=true).OrderBy(x=>x.Sequence).ToListAsync(ct);
        var lines=editingLineId.HasValue?allLines.Where(x=>x.ID==editingLineId.Value).ToList():allLines;
        if(editingLineId.HasValue&&lines.Count!=1)return NotFound();
        if(editingLineId.HasValue)
        {
            model.DocumentDate=document.DocumentDate;model.CurrencyCode=currency=document.CurrencyCode;
            model.ExchangeRate=document.ExchangeRate.ToString("0.######",CultureInfo.InvariantCulture);exchangeRate=document.ExchangeRate;
            model.Notes=document.Notes;
        }
        var postedLineIds=model.Lines.Select(x=>x.Id).ToList();
        if(postedLineIds.Count!=postedLineIds.Distinct().Count()||postedLineIds.Count!=lines.Count||lines.Any(x=>!postedLineIds.Contains(x.ID)))
            ModelState.AddModelError(string.Empty,"Belge kalemleri değişti. Sayfayı yenileyip tekrar deneyiniz.");

        var materialIds=lines.Select(x=>x.MaterialId).Distinct().ToList();
        var materials=await _context.PrdMaterials.Where(x=>materialIds.Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        var lineIds=lines.Select(x=>x.ID).ToList();
        var movements=await _context.PrdStockMovements.Where(x=>lineIds.Contains(x.InventoryDocumentLineId??0)&&x.IsDelete!=true).ToListAsync(ct);
        var originalLotIds=movements.Where(x=>x.StockLotId.HasValue).Select(x=>x.StockLotId!.Value).Distinct().ToList();
        var lots=await _context.PrdStockLots.Where(x=>originalLotIds.Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        var requestedLotEdits=new Dictionary<int,(string LotNumber,DateTime? ProductionDate,DateTime? ExpirationDate)>();
        var units=await _context.PrdUnits.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false).Select(x=>x.ID).ToListAsync(ct);
        var converter=await ProductionStockUnitConverter.LoadAsync(_context,materialIds,ct);
        var canChangeUnits=User.IsInRole("Admin");
        var unitChanges=new List<(int LineId,int OldUnitId,int NewUnitId)>();
        var before=JsonSerializer.Serialize(new
        {
            document.DocumentDate,document.CurrencyCode,document.ExchangeRate,document.TotalCost,document.Notes,
            Lines=lines.Select(x=>new{x.ID,x.UnitId,x.LotNumber,x.ProductionDate,x.ExpirationDate,x.Quantity,x.OriginalUnitCost,x.UnitCost,x.TotalCost,x.Notes})
        });

        foreach(var line in lines)
        {
            var input=model.Lines.FirstOrDefault(x=>x.Id==line.ID);
            if(input==null)continue;
            if(!Ekomers.Web.Services.RawMaterialConsumptionService.TryQuantity(input.Quantity,out var quantity))
            {
                ModelState.AddModelError(string.Empty,$"{line.Sequence}. satır miktarı geçersiz.");
                continue;
            }
            if(!TryParseDecimal(input.UnitCost,out var originalUnitCost)||originalUnitCost<0)
            {
                ModelState.AddModelError(string.Empty,$"{line.Sequence}. satır birim maliyeti geçersiz.");
                continue;
            }
            if(!materials.TryGetValue(line.MaterialId,out var material))
            {
                ModelState.AddModelError(string.Empty,$"{line.Sequence}. satırdaki malzeme bulunamadı.");
                continue;
            }

            if(input.UnitId!=line.UnitId)
            {
                if(!canChangeUnits){ModelState.AddModelError(string.Empty,"Birim değiştirmek için Admin yetkisi gereklidir.");continue;}
                if(!units.Contains(input.UnitId)){ModelState.AddModelError(string.Empty,$"{line.Sequence}. satırda aktif bir birim seçiniz.");continue;}
                try{converter.Factor(line.MaterialId,input.UnitId);}
                catch(InvalidOperationException ex){ModelState.AddModelError(string.Empty,ex.Message);continue;}
                if(document.Type==PrdInventoryDocumentType.WarehouseTransfer&&document.Status==PrdInventoryDocumentStatus.Draft&&!converter.IsBaseUnitEquivalent(line.MaterialId,input.UnitId))
                {ModelState.AddModelError(string.Empty,"Transfer taslağı ana stok biriminde olmalıdır; miktarı ve maliyeti ana birime göre giriniz.");continue;}
                unitChanges.Add((line.ID,line.UnitId,input.UnitId));
            }

            var lotNumber=(input.LotNumber??string.Empty).Trim();
            if(material.RequiresLotTracking&&string.IsNullOrWhiteSpace(lotNumber))
                ModelState.AddModelError(string.Empty,$"{material.Code} için lot numarası zorunludur.");
            if(material.RequiresExpirationDate&&!input.ExpirationDate.HasValue)
                ModelState.AddModelError(string.Empty,$"{material.Code} için son kullanma tarihi zorunludur.");

            foreach(var movement in movements.Where(x=>x.InventoryDocumentLineId==line.ID&&x.StockLotId.HasValue))
            {
                var lotId=movement.StockLotId!.Value;
                var requested=(string.IsNullOrWhiteSpace(lotNumber)?"LOTSUZ":lotNumber,input.ProductionDate?.Date,input.ExpirationDate?.Date);
                if(requestedLotEdits.TryGetValue(lotId,out var existing)&&existing!=requested)
                    ModelState.AddModelError(string.Empty,$"{lots.GetValueOrDefault(lotId)?.LotNumber??$"Lot #{lotId}"} aynı belgede birbiriyle uyuşmayan bilgilerle düzenlenemez.");
                else requestedLotEdits[lotId]=requested;
            }

            var unitCost=originalUnitCost*exchangeRate;
            line.LotNumber=string.IsNullOrWhiteSpace(lotNumber)?"LOTSUZ":lotNumber;
            line.ProductionDate=input.ProductionDate?.Date;
            line.ExpirationDate=input.ExpirationDate?.Date;
            line.Quantity=quantity;
            line.UnitId=input.UnitId;
            line.OriginalUnitCost=originalUnitCost;
            line.CurrencyCode=currency;
            line.ExchangeRate=exchangeRate;
            line.UnitCost=unitCost;
            line.TotalCost=quantity*unitCost;
            line.Notes=input.Notes?.Trim();
            line.UpdateDate=now;
            line.UpdateUserID=actor;
        }

        foreach(var request in requestedLotEdits)
        {
            if(!lots.TryGetValue(request.Key,out var lot))
            {
                ModelState.AddModelError(string.Empty,$"Lot #{request.Key} bulunamadı.");
                continue;
            }
            var duplicate=await _context.PrdStockLots.FirstOrDefaultAsync(x=>x.ID!=lot.ID&&x.MaterialId==lot.MaterialId&&x.WarehouseId==lot.WarehouseId&&x.LotNumber==request.Value.LotNumber&&x.IsDelete!=true,ct);
            if(duplicate!=null)
            {
                ModelState.AddModelError(string.Empty,$"{request.Value.LotNumber} lotu aynı malzeme ve depoda zaten kayıtlı. Mevcut lot numarasını kullanınız veya farklı bir lot giriniz.");
                continue;
            }
            lot.LotNumber=request.Value.LotNumber;
            lot.ProductionDate=request.Value.ProductionDate;
            lot.ExpirationDate=request.Value.ExpirationDate;
            lot.UpdateDate=now;
            lot.UpdateUserID=actor;
        }

        if(!ModelState.IsValid)
        {
            await transaction.RollbackAsync(ct);
            return await InvalidInventoryEditAsync(model,editingLineId,ct);
        }

        if(!editingLineId.HasValue)
        {
            document.DocumentDate=model.DocumentDate.Date;
            document.CurrencyCode=currency;
            document.ExchangeRate=exchangeRate;
            document.Notes=model.Notes?.Trim();
        }
        document.TotalCost=allLines.Sum(x=>x.TotalCost);
        document.UpdateDate=now;
        document.UpdateUserID=actor;
        foreach(var movement in movements)
        {
            var line=lines.First(x=>x.ID==movement.InventoryDocumentLineId);
            movement.Quantity=line.Quantity;
            movement.UnitId=line.UnitId;
            movement.OriginalUnitCost=line.OriginalUnitCost;
            movement.CurrencyCode=line.CurrencyCode;
            movement.ExchangeRate=line.ExchangeRate;
            movement.UnitCost=line.UnitCost;
            movement.TotalCost=line.TotalCost;
            movement.MovementDate=document.DocumentDate;
            movement.Description=document.Notes;
            movement.UpdateDate=now;
            movement.UpdateUserID=actor;
        }

        var balanceError=await ValidateCorrectionBalancesAsync(movements,null,ct);
        if(balanceError!=null)
        {
            await transaction.RollbackAsync(ct);
            ModelState.AddModelError(string.Empty,balanceError);
            return await InvalidInventoryEditAsync(model,editingLineId,ct);
        }

        foreach(var change in unitChanges)
            _context.UserActivityLog.Add(new UserActivityLog
            {
                DateTime=now,UserName=Limit(actor,100),ControllerName=nameof(ProductionInventoryController),ActionName=editingLineId.HasValue?nameof(SatirDuzenle):nameof(Duzenle),
                Parameters=JsonSerializer.Serialize(new{InventoryDocumentId=document.ID,LineId=change.LineId,change.OldUnitId,change.NewUnitId,Reason=model.EditReason}),
                Info="InventoryDocumentLineUnitCorrection"
            });

        var after=JsonSerializer.Serialize(new
        {
            document.DocumentDate,document.CurrencyCode,document.ExchangeRate,document.TotalCost,document.Notes,
            Lines=lines.Select(x=>new{x.ID,x.UnitId,x.LotNumber,x.ProductionDate,x.ExpirationDate,x.Quantity,x.OriginalUnitCost,x.UnitCost,x.TotalCost,x.Notes})
        });
        _context.UserActivityLog.Add(new UserActivityLog
        {
            DateTime=now,
            UserName=Limit(actor,100),
            ControllerName=nameof(ProductionInventoryController),
            ActionName=editingLineId.HasValue?nameof(SatirDuzenle):nameof(Duzenle),
            Parameters=Limit(JsonSerializer.Serialize(new{InventoryDocumentId=document.ID,document.DocumentNumber,Reason=model.EditReason,Before=before,After=after}),4096),
            Info=editingLineId.HasValue?"InventoryDocumentAdminLineEdit":"InventoryDocumentAdminEdit"
        });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"]=editingLineId.HasValue?$"{document.DocumentNumber} belgesinin {lines[0].Sequence}. satırı ve bağlı stok hareketleri güncellendi.":$"{document.DocumentNumber} belgesi ve bağlı stok hareketleri güncellendi.";
        return RedirectToAction(nameof(Detay),new{id=document.ID});
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Roles="Admin")]
    public Task<IActionResult> SatirSil(int id,int lineId,string? reason,bool confirmStockRollback,CancellationToken ct,bool returnToDetail=false)=>
        RunCorrectionAsync(id,nameof(SatirSil),null,()=>DeleteInventoryLineAsync(id,lineId,reason,confirmStockRollback,ct,returnToDetail),ct);

    private async Task<IActionResult> DeleteInventoryLineAsync(int id,int lineId,string? reason,bool confirmStockRollback,CancellationToken ct,bool returnToDetail)
    {
        IActionResult Back()=>RedirectToAction(returnToDetail?nameof(Detay):nameof(Duzenle),new{id});
        reason=reason?.Trim();
        if(string.IsNullOrWhiteSpace(reason)||reason.Length<5||reason.Length>500||!confirmStockRollback)
        {
            TempData["error"]="Silme nedeni 5-500 karakter olmalı ve bağlı stok hareketlerinin kaldırılmasını onaylamalısınız.";
            return Back();
        }

        await using var transaction=await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var document=await LockCorrectionDocumentAsync(id,ct);
        if(document==null)return NotFound();
        if(!CanDeleteDocumentLines(document))
        {
            TempData["error"]="Yalnızca bağımsız devir veya depo transfer belgelerinin satırları silinebilir. Bağlı belgeleri kaynak süreç üzerinden geri alınız.";
            return Back();
        }
        await LockCorrectionWarehousesAsync(document,ct);
        var lines=await _context.PrdInventoryDocumentLines.Where(x=>x.InventoryDocumentId==id&&x.IsDelete!=true).ToListAsync(ct);
        var line=lines.FirstOrDefault(x=>x.ID==lineId);
        if(line==null)return NotFound();
        if(lines.Count<=1)
        {
            TempData["error"]="Belgenin son satırı silinemez. Belgenin tamamını kaldırmak için detay ekranındaki Belgeyi Sil işlemini kullanınız.";
            return Back();
        }
        var hasWorkflowLink=await HasCorrectionWorkflowLinksAsync(id,[lineId],ct);
        if(hasWorkflowLink)
        {
            TempData["error"]="Satır satınalma, kalite veya ters kayıt sürecinde kullanılıyor; kaynak süreç üzerinden geri alınmalıdır.";
            return Back();
        }

        var movements=await _context.PrdStockMovements.Where(x=>x.InventoryDocumentLineId==lineId&&x.IsDelete!=true).ToListAsync(ct);
        var balanceError=await ValidateCorrectionBalancesAsync(movements,movements.Select(x=>x.ID).ToList(),ct);
        if(balanceError!=null)
        {
            TempData["error"]=balanceError;
            return Back();
        }

        var now=DateTime.Now;
        var actor=User.Identity?.Name??"Admin";
        MarkDeleted(movements,now,actor);
        MarkDeleted([line],now,actor);
        document.TotalCost=lines.Where(x=>x.IsDelete!=true).Sum(x=>x.TotalCost);
        document.UpdateDate=now;
        document.UpdateUserID=actor;
        _context.UserActivityLog.Add(new UserActivityLog
        {
            DateTime=now,UserName=Limit(actor,100),ControllerName=nameof(ProductionInventoryController),ActionName=nameof(SatirSil),
            Parameters=Limit(JsonSerializer.Serialize(new{InventoryDocumentId=id,document.DocumentNumber,LineId=line.ID,line.Sequence,line.MaterialId,line.LotNumber,line.Quantity,line.TotalCost,Reason=reason,MovementIds=movements.Select(x=>x.ID)}),4096),
            Info="InventoryDocumentLineAdminSoftDelete"
        });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"]=$"{document.DocumentNumber} belgesinin {line.Sequence}. satırı silindi; bağlı {movements.Count} stok hareketi bakiyeden çıkarıldı.";
        return Back();
    }

    private static bool CanDeleteDocumentLines(PrdInventoryDocument document)=>
        (document.Type==PrdInventoryDocumentType.Opening||document.Type==PrdInventoryDocumentType.WarehouseTransfer)&&
        (document.Status==PrdInventoryDocumentStatus.Draft||document.Status==PrdInventoryDocumentStatus.Posted)&&
        string.IsNullOrWhiteSpace(document.SourceDocumentType)&&!document.SourceDocumentId.HasValue&&!document.ReversalDocumentId.HasValue;

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> StokaIsle(int id,CancellationToken ct)
    {
        if(await FindConsumptionDocumentIdAsync(id,ct) is int consumptionId)return ConsumptionWorkflowRedirect(consumptionId);
        try
        {
            await using var transaction=await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
            var query=_context.Database.IsSqlServer()
                ?_context.PrdInventoryDocuments.FromSqlInterpolated($"SELECT * FROM [dbo].[PrdInventoryDocument] WITH (UPDLOCK,HOLDLOCK) WHERE [ID]={id}")
                :_context.PrdInventoryDocuments.AsQueryable();
            var document=await query.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);
            if(document==null)return NotFound();
            if(document.Status!=PrdInventoryDocumentStatus.Draft){TempData["error"]="Yalnızca taslak belge stoka işlenebilir.";return RedirectToAction(nameof(Detay),new{id});}
            // Same warehouse lock as manual consumption/counting, in stable order for two-way transfers.
            if(_context.Database.IsSqlServer())
                foreach(var warehouseId in new[]{document.SourceWarehouseId,document.TargetWarehouseId}.Where(x=>x.HasValue).Select(x=>x!.Value).Distinct().OrderBy(x=>x))
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdWarehouse] WITH (UPDLOCK,HOLDLOCK) WHERE [ID]={warehouseId}",ct);
            var lines=await _context.PrdInventoryDocumentLines.Where(x=>x.InventoryDocumentId==id&&x.IsDelete!=true).OrderBy(x=>x.Sequence).ToListAsync(ct);
            if(lines.Count==0)throw new InvalidOperationException("Belgede kalem bulunmuyor.");
            var now=DateTime.Now;var user=User.Identity?.Name;
            if(document.Type==PrdInventoryDocumentType.Opening)await PostOpening(document,lines,now,user,ct);else if(document.Type==PrdInventoryDocumentType.WarehouseTransfer)await PostTransfer(document,lines,now,user,ct);else throw new InvalidOperationException("Bu belge türü henüz stoka işlenemez.");
            document.Status=PrdInventoryDocumentStatus.Posted;document.PostingDate=now;document.PostedUserId=user;document.UpdateDate=now;document.UpdateUserID=user;document.TotalCost=lines.Sum(x=>x.TotalCost);await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]="Belge stoka işlendi; hareketler artık belge üzerinden değiştirilemez.";
        }
        catch(InvalidOperationException ex){_context.ChangeTracker.Clear();TempData["error"]=ex.Message;}
        catch(OverflowException){_context.ChangeTracker.Clear();TempData["error"]="Dönüştürülmüş miktar veya maliyet desteklenen aralığı aşıyor; stok işlemi yapılmadı.";}
        return RedirectToAction(nameof(Detay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> TaslagiIptalEt(int id,CancellationToken ct)
    {
        if(await FindConsumptionDocumentIdAsync(id,ct) is int consumptionId)return ConsumptionWorkflowRedirect(consumptionId);
        var document=await _context.PrdInventoryDocuments.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(document==null)return NotFound();if(document.Status!=PrdInventoryDocumentStatus.Draft){TempData["error"]="Yalnızca taslak belge iptal edilebilir.";return RedirectToAction(nameof(Detay),new{id});}document.Status=PrdInventoryDocumentStatus.Cancelled;document.IsActive=false;document.UpdateDate=DateTime.Now;document.UpdateUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);TempData["success"]="Taslak stok belgesi iptal edildi.";return RedirectToAction(nameof(Detay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy=InventoryDocumentDeletePolicy)]
    public async Task<IActionResult> Sil(int id,string? confirmationDocumentNumber,string? reason,bool confirmStockRollback,CancellationToken ct)
    {
        if(await FindConsumptionDocumentIdAsync(id,ct) is int consumptionId)return ConsumptionWorkflowRedirect(consumptionId);
        reason=reason?.Trim();
        if(string.IsNullOrWhiteSpace(reason)||reason.Length<5||reason.Length>500)
        {
            TempData["error"]="Silme nedeni 5-500 karakter olmalıdır.";
            return RedirectToAction(nameof(Detay),new{id});
        }
        if(!confirmStockRollback)
        {
            TempData["error"]="Belgeye bağlı stok hareketlerinin geri alınacağını onaylamalısınız.";
            return RedirectToAction(nameof(Detay),new{id});
        }

        await using var transaction=await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var document=await _context.PrdInventoryDocuments.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);
        if(document==null)return NotFound();
        var isAdmin=User.IsInRole("Admin");
        if(!string.Equals(document.DocumentNumber.Trim(),confirmationDocumentNumber?.Trim(),StringComparison.OrdinalIgnoreCase))
        {
            TempData["error"]="Yazdığınız stok belge numarası kayıtla eşleşmiyor.";
            return RedirectToAction(nameof(Detay),new{id});
        }
        if(!isAdmin&&document.Type!=PrdInventoryDocumentType.Opening&&document.Type!=PrdInventoryDocumentType.WarehouseTransfer)
        {
            TempData["error"]="Bu belge türü ProductionInventory ekranından silinemez; kaynak süreç üzerinden geri alınmalıdır.";
            return RedirectToAction(nameof(Detay),new{id});
        }
        if(!isAdmin&&(!string.IsNullOrWhiteSpace(document.SourceDocumentType)||document.SourceDocumentId.HasValue))
        {
            TempData["error"]=$"Bu belge {document.SourceDocumentType??"başka bir süreç"} tarafından oluşturulmuş. Kaynak süreç ekranından geri alınız.";
            return RedirectToAction(nameof(Detay),new{id});
        }

        if(!isAdmin)
        {
            var hasReversalLink=document.ReversalDocumentId.HasValue||await _context.PrdInventoryDocuments.AnyAsync(x=>
                x.IsDelete!=true&&(x.ReversalDocumentId==id||
                (x.SourceDocumentType=="PrdInventoryDocumentReversal"&&x.SourceDocumentId==id)),ct);
            if(hasReversalLink)
            {
                TempData["error"]="Bu belgenin ters kayıt bağlantısı bulunuyor; kayıt zinciri ProductionInventory ekranından silinemez.";
                return RedirectToAction(nameof(Detay),new{id});
            }
        }

        var lines=await _context.PrdInventoryDocumentLines.Where(x=>x.InventoryDocumentId==id).ToListAsync(ct);
        var lineIds=lines.Select(x=>x.ID).ToList();
        if(!isAdmin)
        {
            var isUsedByAnotherWorkflow=await _context.PurGoodsReceipts.AnyAsync(x=>x.QuarantineInventoryDocumentId==id,ct)||
                await _context.PurGoodsReceiptLandedCosts.AnyAsync(x=>x.InventoryDocumentId==id,ct)||
                await _context.PurGoodsReceiptLines.AnyAsync(x=>x.QuarantineInventoryDocumentLineId.HasValue&&lineIds.Contains(x.QuarantineInventoryDocumentLineId.Value),ct)||
                await _context.PurGoodsReceiptLandedCostAllocations.AnyAsync(x=>lineIds.Contains(x.InventoryDocumentLineId),ct);
            if(isUsedByAnotherWorkflow)
            {
                TempData["error"]="Belge satınalma veya kalite sürecinde kullanılıyor; kaynak süreç üzerinden geri alınmalıdır.";
                return RedirectToAction(nameof(Detay),new{id});
            }
        }

        var movements=await _context.PrdStockMovements.Where(x=>
            x.InventoryDocumentId==id||(x.DocumentType==PrdStockDocumentType.InventoryDocument&&x.DocumentId==id)).ToListAsync(ct);
        var activeMovements=movements.Where(x=>x.IsDelete!=true).ToList();
        var affectedLotIds=activeMovements.Where(x=>x.StockLotId.HasValue).Select(x=>x.StockLotId!.Value).Distinct().ToList();
        if(!isAdmin&&affectedLotIds.Count>0)
        {
            var allActiveMovements=await _context.PrdStockMovements.AsNoTracking()
                .Where(x=>x.IsDelete!=true&&x.StockLotId.HasValue&&affectedLotIds.Contains(x.StockLotId.Value))
                .Select(x=>new{x.StockLotId,x.Direction,x.Quantity}).ToListAsync(ct);
            var reservationRows=await _context.PrdStockReservations.AsNoTracking()
                .Where(x=>x.IsDelete!=true&&affectedLotIds.Contains(x.StockLotId)&&
                    (x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed))
                .Select(x=>new{x.StockLotId,x.ReservedQuantity,x.UsedQuantity,x.ReleasedQuantity}).ToListAsync(ct);
            var lotInfo=await(from lot in _context.PrdStockLots.AsNoTracking()
                              join material in _context.PrdMaterials.AsNoTracking() on lot.MaterialId equals material.ID
                              join warehouse in _context.PrdWarehouses.AsNoTracking() on lot.WarehouseId equals warehouse.ID
                              where affectedLotIds.Contains(lot.ID)
                              select new{lot.ID,lot.LotNumber,MaterialCode=material.Code,WarehouseCode=warehouse.Code}).ToDictionaryAsync(x=>x.ID,ct);

            foreach(var lotId in affectedLotIds)
            {
                var currentBalance=allActiveMovements.Where(x=>x.StockLotId==lotId).Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity);
                var documentContribution=activeMovements.Where(x=>x.StockLotId==lotId).Sum(x=>x.Direction==PrdStockDirection.In?x.Quantity:-x.Quantity);
                var remainingBalance=currentBalance-documentContribution;
                var reserved=reservationRows.Where(x=>x.StockLotId==lotId).Sum(x=>Math.Max(0,x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity));
                var label=lotInfo.TryGetValue(lotId,out var info)?$"{info.MaterialCode} / {info.WarehouseCode} / {info.LotNumber}":$"Lot #{lotId}";
                if(remainingBalance<0m)
                {
                    TempData["error"]=$"{label} bakiyesi silme sonrası {remainingBalance:0.######} olur. Sonraki stok hareketlerini önce geri alınız.";
                    return RedirectToAction(nameof(Detay),new{id});
                }
                if(reserved>remainingBalance)
                {
                    TempData["error"]=$"{label} için {reserved:0.######} rezerve stok var; silme sonrası bakiye rezervasyonu karşılamıyor.";
                    return RedirectToAction(nameof(Detay),new{id});
                }
            }
        }

        var now=DateTime.Now;var actor=User.Identity?.Name??"Admin";
        MarkDeleted(movements,now,actor);
        MarkDeleted(lines,now,actor);
        MarkDeleted([document],now,actor);
        document.Status=PrdInventoryDocumentStatus.Cancelled;
        _context.UserActivityLog.Add(new UserActivityLog
        {
            DateTime=now,
            UserName=Limit(actor,100),
            ControllerName=nameof(ProductionInventoryController),
            ActionName=nameof(Sil),
            Parameters=Limit(JsonSerializer.Serialize(new{InventoryDocumentId=id,document.DocumentNumber,Reason=reason,MovementCount=activeMovements.Count}),4096),
            Info="InventoryDocumentCascadeSoftDelete"
        });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"]=$"{document.DocumentNumber} silindi; {activeMovements.Count} stok hareketi bakiyeden çıkarıldı.";
        return RedirectToAction(nameof(Index));
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
        if(!document.SourceWarehouseId.HasValue||!document.TargetWarehouseId.HasValue||document.SourceWarehouseId==document.TargetWarehouseId)throw new InvalidOperationException("Transfer depo bilgileri geçersiz.");
        if(!await _context.PrdWarehouses.AnyAsync(x=>x.ID==document.SourceWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct))throw new InvalidOperationException("Kaynak depo aktif değil veya bulunamadı.");
        var target=await _context.PrdWarehouses.FirstOrDefaultAsync(x=>x.ID==document.TargetWarehouseId&&x.IsActive!=false&&x.IsDelete!=true,ct)??throw new InvalidOperationException("Hedef depo aktif değil veya bulunamadı.");
        var balances=await GetLotBalances(document.SourceWarehouseId.Value,ct);
        var converter=await ProductionStockUnitConverter.LoadAsync(_context,lines.Select(x=>x.MaterialId).Distinct().ToList(),ct);
        if(lines.GroupBy(x=>x.SourceStockLotId).Any(x=>x.Count()>1))throw new InvalidOperationException("Aynı kaynak lot bir transferde birden fazla kez seçilemez.");
        foreach(var line in lines)
        {
            var balance=balances.FirstOrDefault(x=>x.StockLotId==line.SourceStockLotId)??throw new InvalidOperationException($"{line.Sequence}. satırdaki kaynak lot bulunamadı.");
            if(balance.BlockReason!=null)throw new InvalidOperationException($"{balance.MaterialCode} / {balance.LotNumber}: {balance.BlockReason}");
            if(line.MaterialId!=balance.MaterialId||!converter.IsBaseUnitEquivalent(line.MaterialId,line.UnitId))throw new InvalidOperationException("Taslak oluşturulduktan sonra malzeme/ana birim bilgisi değişmiş. Taslağı iptal edip yeniden hazırlayınız.");
            if(line.Quantity<=0||line.Quantity>999999999999.999999m||decimal.Round(line.Quantity,6)!=line.Quantity||line.Quantity>balance.AvailableQuantity)throw new InvalidOperationException($"{balance.MaterialCode} / {balance.LotNumber} için kullanılabilir {balance.AvailableQuantity:0.######} {balance.Unit} stok yetersiz veya miktar geçersiz.");
            if(balance.ExpirationDate.HasValue&&balance.ExpirationDate.Value.Date<DateTime.Today&&target.Type!=PrdWarehouseType.Scrap)throw new InvalidOperationException($"{balance.MaterialCode} / {balance.LotNumber} süresi geçmiş; yalnızca hurda deposuna aktarılabilir.");
            line.UnitId=balance.UnitId;line.LotNumber=balance.LotNumber;line.ProductionDate=balance.ProductionDate;line.ExpirationDate=balance.ExpirationDate;
            line.UnitCost=balance.UnitCost;line.OriginalUnitCost=balance.UnitCost;line.CurrencyCode="TRY";line.ExchangeRate=1;line.TotalCost=TransferCost(line.Quantity,balance);line.CostSource=PrdStockCostSource.Transfer;var targetLot=await GetOrCreateTargetLot(balance.MaterialId,target.ID,balance.LotNumber,balance.ProductionDate,balance.ExpirationDate,now,user,ct);line.TargetStockLotId=targetLot.ID;
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
        model.Warehouses=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.IsActive!=false&&x.IsDelete!=true).OrderBy(x=>x.Type).ThenBy(x=>x.Code).Select(x=>new SelectListItem(x.Code+" - "+x.Name+" ("+x.Type.ToTurkish()+")",x.ID.ToString())).ToListAsync(ct);model.Materials=await(from material in _context.PrdMaterials.AsNoTracking() join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID where material.IsActive!=false&&material.IsDelete!=true orderby material.Code select new SelectListItem(material.Code+" - "+material.Name+" ("+unit.Name+")",material.ID.ToString())).ToListAsync(ct);
        if(model.Type==PrdInventoryDocumentType.WarehouseTransfer&&model.SourceWarehouseId.HasValue){var balances=await GetLotBalances(model.SourceWarehouseId.Value,ct);model.SourceLots=balances.Where(x=>x.BlockReason==null&&x.AvailableQuantity>0).Select(x=>new SelectListItem($"{x.MaterialCode} - {x.MaterialName} | Lot: {x.LotNumber} | Kullanılabilir: {x.AvailableQuantity:0.######} {x.Unit} | Maliyet: {x.UnitCost:N6} ₺"+(x.ExpirationDate.HasValue?$" | SKT: {x.ExpirationDate:dd.MM.yyyy}":string.Empty),x.StockLotId.ToString())).ToList();model.LotWarnings=balances.Where(x=>x.BlockReason!=null).Select(x=>$"{x.MaterialCode} / {x.LotNumber}: {x.BlockReason}").ToList();}
        while(model.Lines.Count<15)model.Lines.Add(new InventoryDocumentCreateLineVM());
    }

    private async Task<InventoryDocumentEditVM?> BuildEditModel(int id,InventoryDocumentEditVM? posted,CancellationToken ct)
    {
        var document=await _context.PrdInventoryDocuments.AsNoTracking().Where(x=>x.ID==id&&x.IsDelete!=true).Select(x=>new
        {
            x.ID,x.DocumentNumber,x.Type,x.Status,x.DocumentDate,x.CurrencyCode,x.ExchangeRate,x.Notes,x.SourceDocumentType,x.SourceDocumentId,x.ReversalDocumentId,
            SourceWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.SourceWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-",
            TargetWarehouse=_context.PrdWarehouses.Where(w=>w.ID==x.TargetWarehouseId).Select(w=>w.Code+" - "+w.Name).FirstOrDefault()??"-"
        }).FirstOrDefaultAsync(ct);
        if(document==null)return null;
        var culture=CultureInfo.GetCultureInfo("tr-TR");
        var model=new InventoryDocumentEditVM
        {
            Id=document.ID,DocumentNumber=document.DocumentNumber,Type=document.Type,Status=document.Status,
            DocumentDate=document.DocumentDate,SourceWarehouse=document.SourceWarehouse,TargetWarehouse=document.TargetWarehouse,
            CurrencyCode=document.CurrencyCode,ExchangeRate=document.ExchangeRate.ToString("0.######",culture),Notes=document.Notes,
            CanChangeUnits=User.IsInRole("Admin")&&document.Status!=PrdInventoryDocumentStatus.Cancelled&&document.Status!=PrdInventoryDocumentStatus.Reversed,
            CanDeleteLines=CanDeleteDocumentLines(new PrdInventoryDocument{Type=document.Type,Status=document.Status,SourceDocumentType=document.SourceDocumentType,SourceDocumentId=document.SourceDocumentId,ReversalDocumentId=document.ReversalDocumentId})
        };
        var lineRows=await(from line in _context.PrdInventoryDocumentLines.AsNoTracking()
                           join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                           join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                           where line.InventoryDocumentId==id&&line.IsDelete!=true
                           orderby line.Sequence
                           select new{Line=line,MaterialCode=material.Code,MaterialName=material.Name,Unit=unit.Name}).ToListAsync(ct);
        model.CanDeleteLines=model.CanDeleteLines&&!await HasCorrectionWorkflowLinksAsync(id,lineRows.Select(x=>x.Line.ID).ToList(),ct);
        var currentUnits=lineRows.Select(x=>x.Line.UnitId).Distinct().ToList();
        var options=await _context.PrdUnits.AsNoTracking().Where(x=>x.IsDelete!=true&&x.IsActive!=false||currentUnits.Contains(x.ID)).OrderBy(x=>x.Name).ToListAsync(ct);
        model.Lines=lineRows.Select(x=>new InventoryDocumentEditLineVM
        {
            Id=x.Line.ID,Sequence=x.Line.Sequence,MaterialCode=x.MaterialCode,MaterialName=x.MaterialName,Unit=x.Unit,UnitId=x.Line.UnitId,
            UnitOptions=options.Where(u=>u.ID==x.Line.UnitId||u.IsDelete!=true&&u.IsActive!=false).Select(u=>new SelectListItem(u.Code+" - "+u.Name+(u.IsActive==false||u.IsDelete==true?" (Pasif)":""),u.ID.ToString())).ToList(),
            LotNumber=x.Line.LotNumber,ProductionDate=x.Line.ProductionDate,ExpirationDate=x.Line.ExpirationDate,
            Quantity=x.Line.Quantity.ToString("0.######",culture),
            UnitCost=(x.Line.OriginalUnitCost??(x.Line.ExchangeRate>0?x.Line.UnitCost/x.Line.ExchangeRate:x.Line.UnitCost)).ToString("0.######",culture),
            Notes=x.Line.Notes
        }).ToList();
        if(posted==null)return model;

        model.DocumentDate=posted.DocumentDate;
        model.CurrencyCode=posted.CurrencyCode;
        model.ExchangeRate=posted.ExchangeRate;
        model.Notes=posted.Notes;
        model.EditReason=posted.EditReason;
        var postedLines=posted.Lines.GroupBy(x=>x.Id).ToDictionary(x=>x.Key,x=>x.First());
        foreach(var line in model.Lines)
        {
            if(!postedLines.TryGetValue(line.Id,out var input))continue;
            line.LotNumber=input.LotNumber;
            line.ProductionDate=input.ProductionDate;
            line.ExpirationDate=input.ExpirationDate;
            line.Quantity=input.Quantity;
            line.UnitId=input.UnitId;
            if(!line.UnitOptions.Any(u=>u.Value==input.UnitId.ToString()))line.UnitOptions.Add(new SelectListItem($"Geçersiz birim #{input.UnitId}",input.UnitId.ToString()));
            line.UnitCost=input.UnitCost;
            line.Notes=input.Notes;
        }
        return model;
    }

    private async Task<List<InventoryLotBalanceVM>> GetLotBalances(int warehouseId,CancellationToken ct)
    {
        var rows=await ProductionStockLotReader.LoadAsync(_context,warehouseId,ct);
        return rows.Select(x=>new InventoryLotBalanceVM{StockLotId=x.Lot.ID,MaterialId=x.Material.ID,UnitId=x.UnitId,MaterialCode=x.Material.Code,MaterialName=x.Material.Name,LotNumber=x.Lot.LotNumber,ProductionDate=x.Lot.ProductionDate,ExpirationDate=x.Lot.ExpirationDate,PhysicalQuantity=x.PhysicalQuantity,ReservedQuantity=x.ReservedQuantity,StockValue=x.StockValue,Unit=x.Unit,BlockReason=x.BlockReason}).OrderBy(x=>x.ExpirationDate??DateTime.MaxValue).ThenBy(x=>x.MaterialCode).ToList();
    }

    private static decimal TransferCost(decimal quantity,InventoryLotBalanceVM balance)=>quantity==balance.PhysicalQuantity?balance.StockValue:decimal.Round(quantity/balance.PhysicalQuantity*balance.StockValue,6);

    private static bool TryParseDecimal(string? value,out decimal result)
    {
        result=0;if(string.IsNullOrWhiteSpace(value))return false;var normalized=value.Trim().Replace(" ",string.Empty);if(normalized.Contains(',')&&normalized.Contains('.'))normalized=normalized.LastIndexOf(',')>normalized.LastIndexOf('.')?normalized.Replace(".",string.Empty).Replace(',','.'):normalized.Replace(",",string.Empty);else if(normalized.Contains(','))normalized=normalized.Replace(',','.');return decimal.TryParse(normalized,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out result);
    }

    private async Task<int?> FindConsumptionDocumentIdAsync(int id,CancellationToken ct)
    {
        var document=await _context.PrdInventoryDocuments.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);
        if(document==null)return null;
        if(document.Type==PrdInventoryDocumentType.RawMaterialConsumption||document.SourceDocumentType==Ekomers.Web.Services.RawMaterialConsumptionService.SourceType)return id;
        if(document.SourceDocumentType=="PrdInventoryDocumentReversal"&&document.SourceDocumentId.HasValue&&
            await _context.PrdInventoryDocuments.AnyAsync(x=>x.ID==document.SourceDocumentId&&x.Type==PrdInventoryDocumentType.RawMaterialConsumption,ct))return document.SourceDocumentId;
        return null;
    }

    private IActionResult ConsumptionWorkflowRedirect(int id)
    {
        TempData["error"]="Hammadde sarfı ve ters kayıtları genel stok belgesi ekranından değiştirilemez/silinemez. Hammadde Sarf ekranındaki kontrollü işlemleri kullanınız.";
        return RedirectToAction("Detay","HammaddeSarf",new{id});
    }

    private static void MarkDeleted<T>(IEnumerable<T> rows,DateTime now,string actor) where T:BaseEntity
    {
        foreach(var row in rows.Where(x=>x.IsDelete!=true))
        {
            row.IsDelete=true;row.IsActive=false;row.DeleteDate=now;row.DeleteUserID=actor;row.UpdateDate=now;row.UpdateUserID=actor;
        }
    }

    private static string Limit(string value,int maxLength)=>value.Length<=maxLength?value:value[..maxLength];

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
}
