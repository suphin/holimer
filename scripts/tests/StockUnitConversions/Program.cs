using System.Security.Claims;
using System.Text.Json;
using ClosedXML.Excel;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Controllers;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); Console.WriteLine("PASS " + label); }
static void Reject(Action action, string label) { try { action(); } catch (InvalidOperationException) { Check(true,label);return; } throw new Exception("FAIL " + label); }
if(args.Contains("--picker-preview")){await PickerFixture.Run(Render);return;}
var units = new[] { new PrdUnit {ID=1,Code="ADET",Name="Adet"}, new PrdUnit {ID=2,Code="KOLI",Name="Koli"},
    new PrdUnit {ID=3,Code="PCS",Name="Old",IsActive=false}, new PrdUnit {ID=4,Code="KG"}, new PrdUnit {ID=5,Code="GR"}, new PrdUnit {ID=6,Code="L"}, new PrdUnit {ID=7,Code="ML"} };
var materials = new[] { new PrdMaterial {ID=1,Code="M1",UnitId=1}, new PrdMaterial {ID=2,Code="M2",UnitId=4}, new PrdMaterial {ID=3,Code="M3",UnitId=7} };
PrdUnitConversion Rule(decimal factor,int? material=1,int from=2,int to=1) => new() {MaterialId=material,FromUnitId=from,ToUnitId=to,Factor=factor};
ProductionStockUnitConverter Convert(params PrdUnitConversion[] rules) => new(materials,units,rules);
Check(Convert(Rule(100)).Quantity(1,2,1.25m)==125,"Product-specific forward conversion");
Check(Convert(Rule(.01m,1,1,2)).Factor(1,2)==100,"Inverse conversion");
Check(Convert(Rule(10,null),Rule(.01m,1,1,2)).Factor(1,2)==100,"Inverse product rule wins over direct general rule");
Check(Convert(Rule(100,null)).Factor(1,2)==100,"General conversion fallback");
Check(Convert().Quantity(1,3,42)==42,"Historical inactive aliases remain interpretable");
Check(Convert().Quantity(2,5,500)==.5m && Convert().Quantity(3,6,1)==1000,"Physical mass and volume conversions");
Check(Convert(Rule(100)).UnitCost(1,2,1000)==10,"Unit cost inversely scales");
Reject(()=>Convert().Quantity(1,2,1),"Missing conversion fails closed");
Reject(()=>Convert(Rule(100,2)).Factor(1,2),"Other product rule is never used");
Reject(()=>Convert(Rule(100),Rule(200)).Factor(1,2),"Conflicting rules rejected");
Reject(()=>Convert(Rule(0)).Factor(1,2),"Invalid coefficient rejected");
Reject(()=>Convert(new PrdUnitConversion{FromUnitId=5,ToUnitId=4,Factor=2}).Factor(2,5),"Physical conversion cannot be overridden by inconsistent coefficient");
Reject(()=>Convert(Rule(100)).Quantity(1,2,decimal.MaxValue),"Quantity overflow rejected");
Reject(()=>Convert(Rule(.000001m)).Quantity(1,2,.000001m),"Subprecision nonzero quantity is not silently dropped");
var inactive=Rule(100);inactive.IsActive=false;Reject(()=>Convert(inactive).Factor(1,2),"Inactive mappings ignored");

var database="CodexStockUnitTest_"+Guid.NewGuid().ToString("N");
var cs=new SqlConnectionStringBuilder{DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog=database,IntegratedSecurity=true,TrustServerCertificate=true}.ConnectionString;
var created=false;
try {
    await using var db=new StockContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);
    await db.Database.EnsureCreatedAsync();created=true;
    foreach(var unit in units) {unit.ID=0;db.PrdUnits.Add(unit);} await db.SaveChangesAsync();
    // IDs are allocated in insertion order by this disposable fixture, never taken from live data.
    var each=units[0].ID;var box=units[1].ID;var kg=units[3].ID;
    var material=new PrdMaterial{Code="FIXTURE-CAPSULE",Name="Fixture capsule",UnitId=each,IsActive=true};
    var warehouse=new PrdWarehouse{Code="FIXTURE",Name="Fixture warehouse",IsActive=true};
    db.AddRange(material,warehouse);await db.SaveChangesAsync();
    var rule=new PrdUnitConversion{MaterialId=material.ID,FromUnitId=box,ToUnitId=each,Factor=100,IsActive=true};db.Add(rule);
    var lot=new PrdStockLot{MaterialId=material.ID,WarehouseId=warehouse.ID,LotNumber="LOT-1",ExpirationDate=DateTime.Today.AddYears(1)};
    db.Add(lot);await db.SaveChangesAsync();
    PrdStockMovement Movement(decimal qty,int unit,PrdStockDirection direction,decimal cost) => new(){MaterialId=material.ID,WarehouseId=warehouse.ID,StockLotId=lot.ID,
        Quantity=qty,UnitId=unit,Direction=direction,UnitCost=cost/qty,TotalCost=cost,MovementDate=DateTime.Today,DocumentNumber="FIXTURE"};
    db.AddRange(Movement(1,box,PrdStockDirection.In,1000),Movement(1,each,PrdStockDirection.Out,10));await db.SaveChangesAsync();
    var original=await db.PrdStockMovements.AsNoTracking().OrderBy(x=>x.ID).ToListAsync();
    var originalJson=JsonSerializer.Serialize(original);
    var originalIds=original.Select(x=>x.ID).ToList();
    UretimYonetimiController Report() => Attach(new UretimYonetimiController(db,null!));
    ProductionStockCountController Count() => Attach(new ProductionStockCountController(db));
    async Task<ProductionStockReportVM> Stock() => (ProductionStockReportVM)((ViewResult)await Report().Stoklar(material.Code,null,warehouse.ID,default)).Model!;
    async Task<WarehouseCountVM> Draft(string owner="fixture") {
        db.ChangeTracker.Clear();var c=Attach(new ProductionStockCountController(db),owner);
        return (WarehouseCountVM)((ViewResult)await c.Index(warehouse.ID,null,null,default)).Model!;
    }
    async Task Add(WarehouseCountVM draft,string qty,string owner="fixture") {
        db.ChangeTracker.Clear();var c=Attach(new ProductionStockCountController(db),owner);
        await c.AddItem(draft.DraftId,draft.Revision,Guid.NewGuid(),warehouse.ID,material.ID,qty,"Raf A","Test",null,default);
        Check(!c.TempData.ContainsKey("error"),"Count append accepted: "+qty);
    }
    async Task SetFactor(decimal value) { db.ChangeTracker.Clear();var r=await db.PrdUnitConversions.SingleAsync(x=>x.ID==rule.ID);r.Factor=value;await db.SaveChangesAsync();db.ChangeTracker.Clear(); }
    async Task CheckOriginal() { db.ChangeTracker.Clear();Check(JsonSerializer.Serialize(await db.PrdStockMovements.AsNoTracking().Where(x=>originalIds.Contains(x.ID)).OrderBy(x=>x.ID).ToListAsync())==originalJson,"Historical quantities, unit IDs and costs unchanged"); }

    var stock=await Stock();var item=stock.Items.Single();
    Check(stock.Error==null && item.IncomingQuantity==100 && item.OutgoingQuantity==1 && item.RemainingQuantity==99 && item.Unit=="Adet","Report combines mixed-unit movements into one base-unit row");
    Check(item.TotalCost==990 && item.UnitCost==10 && stock.Lots.Single().Quantity==99 && stock.Lots.Single().TotalCost==990,"Lot quantities normalize while original stock value is preserved");
    var history=(ProductionStockMovementHistoryVM)((ViewResult)await Report().StokHareketleri(material.ID,warehouse.ID,default)).Model!;
    Check(history.RemainingQuantity==99 && history.Movements.Any(x=>x.Unit=="Koli"&&x.Quantity==1),"History header normalizes; original movement units remain auditable");
    var file=(FileContentResult)await Report().StoklariExcelAktar(material.Code,null,warehouse.ID,default);
    using(var excel=new XLWorkbook(new MemoryStream(file.FileContents))) Check(excel.Worksheet("Stok Durumu").Cell(2,8).GetValue<decimal>()==99 && excel.Worksheet("Lot Detayı").Cell(2,10).GetValue<decimal>()==990,"Excel uses the same normalized quantities and unchanged value");

    var draft=await Draft();await Add(draft,"40");draft=await Draft();await Add(draft,"59");draft=await Draft();
    Check(draft.Rows.Single().SnapshotCurrentQuantity==99 && draft.Rows.Single().CurrentQuantity==99 && draft.Rows.Single().Entries.Count==2,"Count snapshots normalize and multiple location entries accumulate");
    var count=Count();await count.Save(draft,default);
    Check(await db.PrdInventoryDocuments.CountAsync()==0 && await db.PrdStockMovements.CountAsync()==2,"Equal normalized count posts without artificial stock adjustment");
    await Count().Save(draft,default);Check(await db.PrdStockMovements.CountAsync()==2,"Repeated posting remains idempotent");

    draft=await Draft();await Add(draft,"80");draft=await Draft();await Count().Save(draft,default);db.ChangeTracker.Clear();
    var shortage=await db.PrdStockMovements.AsNoTracking().Where(x=>!originalIds.Contains(x.ID)).SingleAsync();
    Check(shortage.Direction==PrdStockDirection.Out && shortage.Quantity==19 && shortage.UnitId==each && shortage.UnitCost==10 && shortage.TotalCost==190,"Count shortage uses converted lot balance, base-unit quantity and proportional cost");
    Check((await Stock()).Items.Single().RemainingQuantity==80,"Posted count reconciles report to physical count");await CheckOriginal();

    draft=await Draft();await Add(draft,"80");draft=await Draft();await SetFactor(200);
    var before=await db.PrdStockMovements.CountAsync();count=Count();await count.Save(draft,default);
    Check(count.TempData.ContainsKey("error")&&await db.PrdStockMovements.CountAsync()==before,"Coefficient change blocks stale count posting");
    draft=await Draft();Check(draft.Rows.Single().StockChanged && draft.Rows.Single().CurrentQuantity==180,"Open draft shows changed normalized stock");
    await Count().RefreshSnapshots(draft,default);draft=await Draft();
    Check(draft.Rows.Single().SnapshotCurrentQuantity==180 && draft.Rows.Single().CountedQuantity=="80","Refresh only updates normalized reference, never the physical count");
    await SetFactor(0);stock=await Stock();Check(stock.Error!=null&&stock.Items.Count==0&&stock.Lots.Count==0,"Invalid rule never shows partial report or fake stock");
    Check(await Report().StoklariExcelAktar(material.Code,null,warehouse.ID,default) is BadRequestObjectResult,"Invalid conversion blocks Excel export");
    draft=await Draft();Check(draft.Rows.Single().Error!=null&&draft.Rows.Single().Entries.Count==1,"Existing count entries survive conversion failure");
    count=Count();await count.RefreshSnapshots(draft,default);Check(count.TempData.ContainsKey("error"),"Invalid conversion blocks reference refresh");
    count=Count();await count.Save(draft,default);Check(count.TempData.ContainsKey("error") && await db.PrdStockMovements.CountAsync()==before,"Invalid conversion cannot mutate stock");
    var entryCount=await db.PrdWarehouseCountEntries.CountAsync();count=Count();
    await count.AddItem(draft.DraftId,draft.Revision,Guid.NewGuid(),warehouse.ID,material.ID,"5",null,null,null,default);
    Check(count.TempData.ContainsKey("error") && await db.PrdWarehouseCountEntries.CountAsync()==entryCount,"Invalid conversion does not append a misleading count entry");
    // Render the error state using actual compiled Razor views (no production layout or live host).
    var builder=WebApplication.CreateBuilder();builder.Services.AddControllersWithViews().AddApplicationPart(typeof(ProductionStockCountController).Assembly);
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();await using var host=builder.Build();
    using(var scope=host.Services.CreateScope()) {
        var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};http.Request.Scheme="http";http.Request.Host=new HostString("localhost");
        var html=await Render(http,"ProductionStockCount","Index",draft);
        Check(html.Contains("Hesaplanamadı")&&html.Contains("disabled")&&html.Contains("count-entry-form"),"Count error is visible, posting disabled, saved entries remain editable");
        var reportHtml=await Render(http,"UretimYonetimi","Stoklar",stock);
        Check(reportHtml.Contains("role=\"alert\"")&&!reportHtml.Contains("table-separate"),"Report Razor renders conversion error without partial rows");
        var noDraft=await Render(http,"ProductionStockCount","Index",new WarehouseCountVM{WarehouseId=warehouse.ID,Error="Conversion failed"});
        Check(!noDraft.Contains("count-add-form"),"Failed draft creation does not render a zero-ID editing form");
    }
    await SetFactor(200);draft=await Draft();await Count().RefreshSnapshots(draft,default);draft=await Draft();
    db.ChangeTracker.Clear();var changed=await db.PrdMaterials.SingleAsync(x=>x.ID==material.ID);changed.UnitId=kg;await db.SaveChangesAsync();
    draft=await Draft();Check(draft.Rows.Single().Error!=null && draft.Rows.Single().Entries.Count==1,"Base-unit change requires recount instead of reinterpreting saved numbers");
    count=Count();await count.Save(draft,default);Check(count.TempData.ContainsKey("error")&&await db.PrdStockMovements.CountAsync()==before,"Changed base unit cannot post old count");
    db.ChangeTracker.Clear();changed=await db.PrdMaterials.SingleAsync(x=>x.ID==material.ID);changed.UnitId=each;await db.SaveChangesAsync();await SetFactor(100);

    // Independent product: exhausted box stock, then count surplus must use per-piece fallback cost.
    var spare=new PrdMaterial{Code="FIXTURE-SPARE",UnitId=each,IsActive=true};db.Add(spare);await db.SaveChangesAsync();
    var spareLot=new PrdStockLot{MaterialId=spare.ID,WarehouseId=warehouse.ID,LotNumber="SPARE"};db.Add(spareLot);await db.SaveChangesAsync();
    db.Add(new PrdUnitConversion{MaterialId=spare.ID,FromUnitId=box,ToUnitId=each,Factor=100});
    foreach(var direction in new[]{PrdStockDirection.In,PrdStockDirection.Out}) db.Add(new PrdStockMovement{MaterialId=spare.ID,WarehouseId=warehouse.ID,StockLotId=spareLot.ID,Direction=direction,Quantity=1,UnitId=box,UnitCost=1000,TotalCost=1000,MovementDate=DateTime.Today});
    await db.SaveChangesAsync();var spareCount=Attach(new ProductionStockCountController(db),"spare");
    var spareDraft=(WarehouseCountVM)((ViewResult)await spareCount.Index(warehouse.ID,null,null,default)).Model!;
    await spareCount.AddItem(spareDraft.DraftId,spareDraft.Revision,Guid.NewGuid(),warehouse.ID,spare.ID,"5",null,null,null,default);
    db.ChangeTracker.Clear();spareCount=Attach(new ProductionStockCountController(db),"spare");spareDraft=(WarehouseCountVM)((ViewResult)await spareCount.Index(warehouse.ID,null,null,default)).Model!;
    await spareCount.Save(spareDraft,default);db.ChangeTracker.Clear();
    var surplus=await db.PrdStockMovements.AsNoTracking().Where(x=>x.MaterialId==spare.ID&&x.MovementType==PrdStockMovementType.InventorySurplus).SingleAsync();
    Check(surplus.Quantity==5&&surplus.UnitId==each&&surplus.UnitCost==10&&surplus.TotalCost==50,"Surplus fallback unit cost converts exhausted historical boxes to pieces");
    // Residual value must not disappear when normalization makes quantity exactly zero.
    db.Add(new PrdStockMovement{MaterialId=spare.ID,WarehouseId=warehouse.ID,StockLotId=spareLot.ID,Direction=PrdStockDirection.Out,Quantity=5,UnitId=each,UnitCost=9,TotalCost=45,MovementDate=DateTime.Today});await db.SaveChangesAsync();
    var residual=(ProductionStockReportVM)((ViewResult)await Report().Stoklar(spare.Code,null,warehouse.ID,default)).Model!;
    Check(residual.Items.Single().RemainingQuantity==0&&residual.Items.Single().TotalCost==5&&residual.Lots.Sum(x=>x.TotalCost)==5,"Zero-quantity residual stock value remains visible");
    await CheckOriginal();
    await ConsumptionAndTransfer(db,cs,each,box,kg,host.Services);
    await ProductionFlowTests.Run(db,cs,each,box,host.Services,Render);
    await using var full=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);
    Check(!full.Database.HasPendingModelChanges(),"Reservation-unit migration snapshot matches the full model");
} finally {
    if(created) {SqlConnection.ClearAllPools();await using var clean=new StockContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);await clean.Database.EnsureDeletedAsync();Console.WriteLine("Removed isolated database "+database);}
}

static async Task ConsumptionAndTransfer(StockContext db,string cs,int each,int box,int kg,IServiceProvider services) {
    var source=new PrdWarehouse{Code="OUT",Name="Source",Type=PrdWarehouseType.Main,IsActive=true};
    var target=new PrdWarehouse{Code="IN",Name="Target",Type=PrdWarehouseType.Production,IsActive=true};
    var raw=new PrdMaterial{Code="RAW-CONVERSION",Name="Convertible raw",UnitId=each,Type=PrdMaterialType.RawMaterial,IsActive=true,RequiresExpirationDate=true};
    db.AddRange(source,target,raw);await db.SaveChangesAsync();
    var lot=new PrdStockLot{MaterialId=raw.ID,WarehouseId=source.ID,LotNumber="CONVERT-LOT",ProductionDate=DateTime.Today.AddMonths(-1),ExpirationDate=DateTime.Today.AddYears(1),IsActive=true};db.Add(lot);await db.SaveChangesAsync();
    var specific=new PrdUnitConversion{MaterialId=raw.ID,FromUnitId=each,ToUnitId=box,Factor=.01m,IsActive=true};
    db.AddRange(specific,new PrdUnitConversion{FromUnitId=box,ToUnitId=each,Factor=10,IsActive=true});
    db.AddRange(new PrdStockMovement{MaterialId=raw.ID,WarehouseId=source.ID,StockLotId=lot.ID,UnitId=box,Direction=PrdStockDirection.In,Quantity=1,UnitCost=1000,TotalCost=1000,MovementDate=DateTime.Today},
        new PrdStockMovement{MaterialId=raw.ID,WarehouseId=source.ID,StockLotId=lot.ID,UnitId=each,Direction=PrdStockDirection.Out,Quantity=10,UnitCost=10,TotalCost=100,MovementDate=DateTime.Today});
    await db.SaveChangesAsync();db.ChangeTracker.Clear();
    var original=await db.PrdStockMovements.AsNoTracking().Where(x=>x.StockLotId==lot.ID).OrderBy(x=>x.ID).ToListAsync();var originalIds=original.Select(x=>x.ID).ToList();var originalJson=JsonSerializer.Serialize(original);
    HammaddeSarfController Raw(StockContext? scope=null) {var store=scope??db;return Attach(new HammaddeSarfController(store,new RawMaterialConsumptionService(store),new PurchasingInventoryReversalService(store)));}
    ProductionInventoryController Transfer(StockContext? scope=null)=>Attach(new ProductionInventoryController(scope??db));
    async Task<int> RawDraft(string quantity) {
        db.ChangeTracker.Clear();var c=Raw();var result=await c.Yeni(new RawMaterialConsumptionCreateVM{WarehouseId=source.ID,DocumentDate=DateTime.Today,Notes="Fixture manual consumption",Lines=[new(){StockLotId=lot.ID,Quantity=quantity,Notes="Keep note"}]},default);
        Check(result is RedirectToActionResult,"Raw consumption draft accepts base-unit quantity: "+quantity);return (int)((RedirectToActionResult)result).RouteValues!["id"]!;
    }
    async Task<int> TransferDraft(string quantity) {
        db.ChangeTracker.Clear();var c=Transfer();var result=await c.Yeni(new InventoryDocumentCreateVM{Type=PrdInventoryDocumentType.WarehouseTransfer,SourceWarehouseId=source.ID,TargetWarehouseId=target.ID,Lines=[new(){SourceStockLotId=lot.ID,Quantity=quantity,Notes="Transfer note"}]},default);
        Check(result is RedirectToActionResult,"Transfer draft accepts base-unit quantity: "+quantity);return (int)((RedirectToActionResult)result).RouteValues!["id"]!;
    }
    async Task SetFactor(decimal value) {db.ChangeTracker.Clear();var r=await db.PrdUnitConversions.SingleAsync(x=>x.ID==specific.ID);r.Factor=value;await db.SaveChangesAsync();db.ChangeTracker.Clear();}
    async Task Blocked(Func<Task> action,string label) {try{await action();}catch(InvalidOperationException){Check(true,label);return;}throw new Exception("FAIL "+label);}
    var loaded=(await new RawMaterialConsumptionService(db).LoadLotsAsync(source.ID,default)).Single();
    Check(loaded.PhysicalQuantity==90&&loaded.UnitId==each&&loaded.Unit=="Adet"&&loaded.UnitCost==10&&loaded.CanConsume,"Consumption lot list applies inverse product coefficient before general coefficient");
    var form=(InventoryDocumentCreateVM)((ViewResult)await Transfer().Yeni(PrdInventoryDocumentType.WarehouseTransfer,source.ID,target.ID,ct:default)).Model!;
    Check(form.SourceLots.Count==1&&form.SourceLots[0].Text.Contains("90 Adet")&&form.LotWarnings.Count==0,"Transfer offers a single normalized lot, not one option per old unit");
    await Blocked(()=>new RawMaterialConsumptionService(db).PrepareLinesAsync(source.ID,[new(){StockLotId=lot.ID,Quantity="91"}],default),"Consumption overdraw checked in normalized base units");
    var rawId=await RawDraft("15");var rawLine=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.InventoryDocumentId==rawId);
    Check(rawLine.UnitId==each&&rawLine.Quantity==15&&rawLine.TotalCost==150&&rawLine.LotNumber==lot.LotNumber&&rawLine.ExpirationDate==lot.ExpirationDate,"Consumption draft stores base-unit quantity, dates, notes and proportional value");
    await SetFactor(0);var rawController=Raw();await rawController.StoktanDus(rawId,true,default);db.ChangeTracker.Clear();
    Check(rawController.TempData.ContainsKey("error")&&!await db.PrdStockMovements.AnyAsync(x=>x.InventoryDocumentId==rawId),"Coefficient invalidated after drafting prevents consumption without partial writes");
    await SetFactor(.01m);await Raw().StoktanDus(rawId,true,default);db.ChangeTracker.Clear();
    var consumed=await db.PrdStockMovements.AsNoTracking().SingleAsync(x=>x.InventoryDocumentId==rawId);
    Check(consumed.Quantity==15&&consumed.UnitId==each&&consumed.TotalCost==150&&consumed.Direction==PrdStockDirection.Out,"Consumption posts normalized quantities and preserves cost");
    await Raw().StoktanDus(rawId,true,default);Check(await db.PrdStockMovements.CountAsync(x=>x.InventoryDocumentId==rawId)==1,"Repeated consumption cannot deduct twice");

    var transferId=await TransferDraft("25");var transferLine=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.InventoryDocumentId==transferId);
    Check(transferLine.UnitId==each&&transferLine.Quantity==25&&transferLine.UnitCost==10&&transferLine.TotalCost==250,"Transfer draft cost is based on normalized lot balance");
    var conflict=new PrdUnitConversion{MaterialId=raw.ID,FromUnitId=box,ToUnitId=each,Factor=200,IsActive=true};db.Add(conflict);await db.SaveChangesAsync();db.ChangeTracker.Clear();
    var transferController=Transfer();await transferController.StokaIsle(transferId,default);db.ChangeTracker.Clear();
    Check(transferController.TempData.ContainsKey("error")&&!await db.PrdStockMovements.AnyAsync(x=>x.InventoryDocumentId==transferId)&&!await db.PrdStockLots.AnyAsync(x=>x.WarehouseId==target.ID),"Conflicting conversion blocks transfer without creating target stock");
    var conflictRow=await db.PrdUnitConversions.SingleAsync(x=>x.ID==conflict.ID);conflictRow.IsDelete=true;await db.SaveChangesAsync();db.ChangeTracker.Clear();
    await Transfer().StokaIsle(transferId,default);db.ChangeTracker.Clear();var transferred=await db.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentId==transferId).ToListAsync();
    Check(transferred.Count==2&&transferred.All(x=>x.UnitId==each&&x.Quantity==25&&x.UnitCost==10&&x.TotalCost==250)&&transferred.Count(x=>x.Direction==PrdStockDirection.In)==1,"Transfer writes equal base-unit out/in movements with identical value");
    transferLine=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.InventoryDocumentId==transferId);
    var targetLot=await db.PrdStockLots.AsNoTracking().SingleAsync(x=>x.ID==transferLine.TargetStockLotId);
    Check(targetLot.LotNumber==lot.LotNumber&&targetLot.ProductionDate==lot.ProductionDate&&targetLot.ExpirationDate==lot.ExpirationDate,"Transfer keeps original lot number, production date and expiry");
    await Transfer().StokaIsle(transferId,default);Check(await db.PrdStockMovements.CountAsync(x=>x.InventoryDocumentId==transferId)==2,"Repeated transfer does not generate duplicate movements");
    var sourceBalance=(await ProductionStockLotReader.LoadAsync(db,source.ID,default)).Single(x=>x.Lot.ID==lot.ID);
    var targetBalance=(await ProductionStockLotReader.LoadAsync(db,target.ID,default)).Single();
    Check(sourceBalance.PhysicalQuantity==50&&sourceBalance.StockValue==500&&targetBalance.PhysicalQuantity==25&&targetBalance.StockValue==250,"Source and target normalized stock reconcile with consumption and transfer");
    var report=(ProductionStockReportVM)((ViewResult)await Attach(new UretimYonetimiController(db,null!)).Stoklar(raw.Code,null,null,default)).Model!;
    Check(report.Items.Sum(x=>x.RemainingQuantity)==75&&report.TotalStockValue==750,"Stock report agrees with normalized operational balances");
    // Active legacy reservations have no UnitId: mixed physical units must never be guessed.
    var reservation=new PrdStockReservation{MaterialId=raw.ID,WarehouseId=source.ID,StockLotId=lot.ID,ReservedQuantity=1,Status=PrdReservationStatus.Active};db.Add(reservation);await db.SaveChangesAsync();db.ChangeTracker.Clear();
    loaded=(await new RawMaterialConsumptionService(db).LoadLotsAsync(source.ID,default)).Single();
    Check(!loaded.CanConsume&&loaded.BlockReason!.Contains("rezervasyon"),"Ambiguous mixed-unit reservation blocks consumption");
    form=(InventoryDocumentCreateVM)((ViewResult)await Transfer().Yeni(PrdInventoryDocumentType.WarehouseTransfer,source.ID,target.ID,ct:default)).Model!;
    Check(form.SourceLots.Count==0&&form.LotWarnings.Any(x=>x.Contains("rezervasyon")),"Ambiguous reservation is explained in transfer form and cannot be selected");
    using(var scope=services.CreateScope()) {
        var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};http.Request.Scheme="http";http.Request.Host=new HostString("localhost");
        var html=await Render(http,"ProductionInventory","Yeni",form);Check(html.Contains("Transfer edilemeyen lotlar")&&html.Contains("ana biriminde"),"Transfer Razor displays normalized-unit guidance and blocked-lot reason");
        var rawForm=(RawMaterialConsumptionCreateVM)((ViewResult)await Raw().Yeni(source.ID,default)).Model!;
        html=await Render(http,"HammaddeSarf","Yeni",rawForm);Check(html.Contains("Sarf edilemeyen lotlar")&&html.Contains("ana biriminde"),"Consumption Razor displays conversion guidance and reservation warning");
    }
    db.ChangeTracker.Clear();var rsv=await db.PrdStockReservations.SingleAsync(x=>x.ID==reservation.ID);rsv.IsDelete=true;await db.SaveChangesAsync();
    // Base-unit reservations still deduct only free quantity, and unrelated blocked lots don't hide good ones.
    var pure=new PrdStockLot{MaterialId=raw.ID,WarehouseId=source.ID,LotNumber="PURE",ExpirationDate=lot.ExpirationDate,IsActive=true};db.Add(pure);await db.SaveChangesAsync();
    db.Add(new PrdStockMovement{MaterialId=raw.ID,WarehouseId=source.ID,StockLotId=pure.ID,UnitId=each,Direction=PrdStockDirection.In,Quantity=10,UnitCost=10,TotalCost=100,MovementDate=DateTime.Today});
    db.Add(new PrdStockReservation{MaterialId=raw.ID,WarehouseId=source.ID,StockLotId=pure.ID,ReservedQuantity=3,UsedQuantity=1,Status=PrdReservationStatus.PartiallyUsed});await db.SaveChangesAsync();
    var pureBalance=(await new RawMaterialConsumptionService(db).LoadLotsAsync(source.ID,default)).Single(x=>x.StockLotId==pure.ID);
    Check(pureBalance.AvailableQuantity==8&&pureBalance.CanConsume,"Unambiguous base-unit reservation protects reserved stock");
    await Blocked(()=>new RawMaterialConsumptionService(db).PrepareLinesAsync(source.ID,[new(){StockLotId=pure.ID,Quantity="9"}],default),"Reserved quantity cannot be consumed");
    // A base-unit change while a transfer draft is open requires recreation, not reinterpretation.
    var changedId=await TransferDraft("5");db.ChangeTracker.Clear();var changeLine=await db.PrdInventoryDocumentLines.SingleAsync(x=>x.InventoryDocumentId==changedId);changeLine.UnitId=kg;await db.SaveChangesAsync();db.ChangeTracker.Clear();
    transferController=Transfer();await transferController.StokaIsle(changedId,default);db.ChangeTracker.Clear();
    Check(transferController.TempData.ContainsKey("error")&&!await db.PrdStockMovements.AnyAsync(x=>x.InventoryDocumentId==changedId),"Old-unit transfer draft is never silently reinterpreted");
    // Competing consumption and transfer must serialize on the same source warehouse.
    var competingRaw=await RawDraft("40");var competingTransfer=await TransferDraft("40");
    async Task PostRaw() {await using var isolated=new StockContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);await Raw(isolated).StoktanDus(competingRaw,true,default);}
    async Task PostTransfer() {await using var isolated=new StockContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);await Transfer(isolated).StokaIsle(competingTransfer,default);}
    await Task.WhenAll(Task.Run(PostRaw),Task.Run(PostTransfer));db.ChangeTracker.Clear();
    var winners=await db.PrdInventoryDocuments.AsNoTracking().CountAsync(x=>(x.ID==competingRaw||x.ID==competingTransfer)&&x.Status==PrdInventoryDocumentStatus.Posted);
    sourceBalance=(await ProductionStockLotReader.LoadAsync(db,source.ID,default)).Single(x=>x.Lot.ID==lot.ID);
    Check(winners==1&&sourceBalance.PhysicalQuantity==10&&sourceBalance.StockValue==100,"Concurrent consumption/transfer cannot spend the same normalized stock twice");
    // Full remaining lot consumption preserves every last cost fraction; reversal restores it exactly.
    var lastRaw=await RawDraft("10");await Raw().StoktanDus(lastRaw,true,default);db.ChangeTracker.Clear();
    var lastMovement=await db.PrdStockMovements.AsNoTracking().SingleAsync(x=>x.InventoryDocumentId==lastRaw);
    Check(lastMovement.TotalCost==100&&lastMovement.UnitId==each,"Full-lot consumption exhausts unchanged remaining stock value");
    await Raw().GeriAl(lastRaw,"Fixture reversal",true,default);db.ChangeTracker.Clear();
    sourceBalance=(await ProductionStockLotReader.LoadAsync(db,source.ID,default)).Single(x=>x.Lot.ID==lot.ID);
    Check(sourceBalance.PhysicalQuantity==10&&sourceBalance.StockValue==100,"Consumption reversal restores base-unit quantity and value to the original lot");
    Check(JsonSerializer.Serialize(await db.PrdStockMovements.AsNoTracking().Where(x=>originalIds.Contains(x.ID)).OrderBy(x=>x.ID).ToListAsync())==originalJson,"Operational conversions do not rewrite historical box/piece records");

    var gram=await db.PrdUnits.AsNoTracking().Where(x=>x.Code=="GR").Select(x=>x.ID).SingleAsync();
    var mass=new PrdMaterial{Code="RAW-MASS",UnitId=kg,Type=PrdMaterialType.RawMaterial,IsActive=true};db.Add(mass);await db.SaveChangesAsync();
    var massLot=new PrdStockLot{MaterialId=mass.ID,WarehouseId=source.ID,LotNumber="MASS",IsActive=true};db.Add(massLot);await db.SaveChangesAsync();
    db.AddRange(new PrdStockMovement{MaterialId=mass.ID,WarehouseId=source.ID,StockLotId=massLot.ID,UnitId=gram,Direction=PrdStockDirection.In,Quantity=1000,UnitCost=.04m,TotalCost=40,MovementDate=DateTime.Today},
        new PrdStockMovement{MaterialId=mass.ID,WarehouseId=source.ID,StockLotId=massLot.ID,UnitId=kg,Direction=PrdStockDirection.Out,Quantity=.25m,UnitCost=40,TotalCost=10,MovementDate=DateTime.Today});await db.SaveChangesAsync();
    var massBalance=(await new RawMaterialConsumptionService(db).LoadLotsAsync(source.ID,default)).Single(x=>x.MaterialId==mass.ID);
    Check(massBalance.PhysicalQuantity==.75m&&massBalance.Unit=="Kilogram"&&massBalance.UnitCost==40,"Consumption combines gram and kilogram stock with unchanged total cost");
    var massLines=await new RawMaterialConsumptionService(db).PrepareLinesAsync(source.ID,[new(){StockLotId=massLot.ID,Quantity="0,25"}],default);
    Check(massLines.Single().Quantity==.25m&&massLines.Single().TotalCost==10&&massLines.Single().UnitId==kg,"Fractional kilogram consumption uses physical conversion");
    var massDraftResult=(RedirectToActionResult)await Transfer().Yeni(new InventoryDocumentCreateVM{Type=PrdInventoryDocumentType.WarehouseTransfer,SourceWarehouseId=source.ID,TargetWarehouseId=target.ID,Lines=[new(){SourceStockLotId=massLot.ID,Quantity="0,5"}]},default);
    var massDraftId=(int)massDraftResult.RouteValues!["id"]!;
    await Transfer().StokaIsle(massDraftId,default);db.ChangeTracker.Clear();
    Check(await db.PrdStockMovements.AsNoTracking().CountAsync(x=>x.InventoryDocumentId==massDraftId&&x.UnitId==kg&&x.Quantity==.5m&&x.TotalCost==20)==2,"Fractional kilogram transfer preserves equal source/target cost");
    // Force a later line failure after the first line has created a target lot and flushed changes.
    var extra=new PrdStockLot{MaterialId=raw.ID,WarehouseId=source.ID,LotNumber="ROLLBACK-LOT",IsActive=true};db.Add(extra);await db.SaveChangesAsync();
    db.Add(new PrdStockMovement{MaterialId=raw.ID,WarehouseId=source.ID,StockLotId=extra.ID,UnitId=box,Direction=PrdStockDirection.In,Quantity=1,UnitCost=1000,TotalCost=1000,MovementDate=DateTime.Today});await db.SaveChangesAsync();
    var batchResult=(RedirectToActionResult)await Transfer().Yeni(new InventoryDocumentCreateVM{Type=PrdInventoryDocumentType.WarehouseTransfer,SourceWarehouseId=source.ID,TargetWarehouseId=target.ID,Lines=[new(){SourceStockLotId=extra.ID,Quantity="1"},new(){SourceStockLotId=lot.ID,Quantity="1"}]},default);
    var batchId=(int)batchResult.RouteValues!["id"]!;db.ChangeTracker.Clear();var lastLine=await db.PrdInventoryDocumentLines.Where(x=>x.InventoryDocumentId==batchId).OrderByDescending(x=>x.Sequence).FirstAsync();lastLine.Quantity=1000;await db.SaveChangesAsync();db.ChangeTracker.Clear();
    transferController=Transfer();await transferController.StokaIsle(batchId,default);db.ChangeTracker.Clear();
    Check(transferController.TempData.ContainsKey("error")&&!await db.PrdStockMovements.AnyAsync(x=>x.InventoryDocumentId==batchId)&&!await db.PrdStockLots.AnyAsync(x=>x.WarehouseId==target.ID&&x.LotNumber==extra.LotNumber)&&await db.PrdInventoryDocuments.AnyAsync(x=>x.ID==batchId&&x.Status==PrdInventoryDocumentStatus.Draft),"Later transfer-line failure rolls back all movements, target lots and draft updates");
}

static T Attach<T>(T c,string owner="fixture") where T:Controller {
    var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,owner),new Claim(ClaimTypes.Name,owner),new Claim(ClaimTypes.Role,"Admin")],"Fixture"))};
    c.ControllerContext=new ControllerContext{HttpContext=http};c.TempData=new TempDataDictionary(http,new MemoryTempData());return c;
}
static async Task<string> Render(HttpContext http,string controller,string actionName,object model) {
    http.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(),"Fixture"));
    var route=new RouteData();route.Values["controller"]=controller;route.Values["action"]=actionName;
    var action=new ActionContext(http,route,new ActionDescriptor(),new ModelStateDictionary());
    var view=http.RequestServices.GetRequiredService<IRazorViewEngine>().GetView(null,$"/Views/{controller}/{actionName}.cshtml",false);
    Check(view.Success,"Compiled view: "+controller+"/"+actionName);using var writer=new StringWriter();
    var data=new ViewDataDictionary(new EmptyModelMetadataProvider(),action.ModelState){Model=model};
    await view.View.RenderAsync(new ViewContext(action,view.View,data,new TempDataDictionary(http,new MemoryTempData()),writer,new HtmlHelperOptions()));return System.Net.WebUtility.HtmlDecode(writer.ToString());
}
sealed class MemoryTempData:ITempDataProvider {public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();public void SaveTempData(HttpContext context,IDictionary<string,object> values){} }
sealed class StockContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options) {
    protected override void OnModelCreating(ModelBuilder b) {
        var keep=new[]{typeof(PrdMaterial),typeof(PrdUnit),typeof(PrdUnitConversion),typeof(PrdWarehouse),typeof(PrdStockLot),typeof(PrdStockMovement),typeof(PrdInventoryDocument),typeof(PrdInventoryDocumentLine),typeof(PrdStockReservation),typeof(PrdWarehouseCountDraft),typeof(PrdWarehouseCountEntry),typeof(UserActivityLog),typeof(PrdProductionOrder),typeof(PrdMaterialRequirement),typeof(PrdWarehouseTask),typeof(PrdWarehouseTaskItem),typeof(PrdWarehouseTaskLot),typeof(PrdProductionMaterialActual),typeof(PrdProductionPlan),typeof(PrdProductionPlanHeader),typeof(PrdRecipeVersion),typeof(PrdProductionResult)};
        foreach(var t in b.Model.GetEntityTypes().Select(x=>x.ClrType).Where(t=>!keep.Contains(t)).ToList())b.Ignore(t);
        foreach(var t in keep) {b.Entity(t).HasBaseType((Type?)null);b.Entity(t).ToTable(t.Name);b.Entity(t).HasKey(t==typeof(UserActivityLog)?"Id":"ID");}
        foreach(var property in b.Model.GetEntityTypes().SelectMany(x=>x.GetProperties()).Where(x=>x.ClrType==typeof(decimal)||x.ClrType==typeof(decimal?))) {property.SetPrecision(18);property.SetScale(6);}
        b.Entity<PrdWarehouseCountDraft>().Property(x=>x.Revision).IsConcurrencyToken();
        b.Entity<PrdWarehouseCountEntry>().HasIndex(x=>new{x.DraftId,x.RequestId}).IsUnique();
        b.Entity<PrdStockReservation>().Property(x=>x.RequirementUnitFactor).HasPrecision(38,18);
    }
}
