using System.Security.Claims;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

static void Check(bool ok,string message){if(!ok)throw new Exception("FAIL "+message);Console.WriteLine("PASS "+message);}
var database="CodexInventoryCorrections_"+Guid.NewGuid().ToString("N");var cs=new SqlConnectionStringBuilder{DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog=database,IntegratedSecurity=true,TrustServerCertificate=true}.ConnectionString;
DbContextOptions<ApplicationDbContext> Options()=>new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;
ProductionInventoryController Controller(ApplicationDbContext c)=>new(c){ControllerContext=new(){HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"Test Admin"),new Claim(ClaimTypes.Role,"Admin")},"Fixture"))}},TempData=new TempDataDictionary(new DefaultHttpContext(),new MemoryTempData())};
await using var db=new CorrectionContext(Options());
try
{
    await db.Database.EnsureCreatedAsync();
    var kg=new PrdUnit{Code="KG",Name="Kilogram",IsActive=true};var grams=new PrdUnit{Code="GRAM",Name="Gram",IsActive=true};var each=new PrdUnit{Code="ADET",Name="Adet",IsActive=true};var box=new PrdUnit{Code="KOLI",Name="Koli",IsActive=true};var inactive=new PrdUnit{Code="OLD",Name="Pasif",IsActive=false};db.AddRange(kg,grams,each,box,inactive);await db.SaveChangesAsync();
    var mass=new PrdMaterial{Code="MASS",Name="Hammadde",UnitId=kg.ID,IsActive=true};var pieces=new PrdMaterial{Code="PIECES",Name="Ambalaj",UnitId=each.ID,IsActive=true};db.AddRange(mass,pieces);await db.SaveChangesAsync();db.Add(new PrdUnitConversion{MaterialId=pieces.ID,FromUnitId=box.ID,ToUnitId=each.ID,Factor=100,IsActive=true});
    var source=new PrdWarehouse{Code="SOURCE",Name="Ana Depo",Type=PrdWarehouseType.Main,IsActive=true};var target=new PrdWarehouse{Code="TARGET",Name="Hedef Depo",Type=PrdWarehouseType.Production,IsActive=true};db.AddRange(source,target);await db.SaveChangesAsync();
    async Task<PrdInventoryDocument> Document(PrdInventoryDocumentType type=PrdInventoryDocumentType.Opening,string? sourceType=null){var d=new PrdInventoryDocument{DocumentNumber="TEST-"+Guid.NewGuid().ToString("N"),Type=type,Status=PrdInventoryDocumentStatus.Posted,DocumentDate=DateTime.Today,CurrencyCode="TRY",ExchangeRate=1,SourceWarehouseId=type==PrdInventoryDocumentType.WarehouseTransfer?source.ID:null,TargetWarehouseId=type==PrdInventoryDocumentType.WarehouseTransfer?target.ID:source.ID,SourceDocumentType=sourceType};db.Add(d);await db.SaveChangesAsync();return d;}
    async Task<(PrdInventoryDocumentLine line,PrdStockLot lot)> Line(PrdInventoryDocument doc,PrdMaterial material,int unit,decimal qty,string? lotName=null){var lot=new PrdStockLot{MaterialId=material.ID,WarehouseId=source.ID,LotNumber=lotName??Guid.NewGuid().ToString("N"),IsActive=true};db.Add(lot);await db.SaveChangesAsync();var l=new PrdInventoryDocumentLine{InventoryDocumentId=doc.ID,Sequence=await db.PrdInventoryDocumentLines.CountAsync(x=>x.InventoryDocumentId==doc.ID)+1,MaterialId=material.ID,UnitId=unit,Quantity=qty,OriginalUnitCost=10,UnitCost=10,TotalCost=qty*10,ExchangeRate=1,CurrencyCode="TRY",LotNumber=lot.LotNumber,TargetStockLotId=lot.ID};db.Add(l);await db.SaveChangesAsync();db.Add(new PrdStockMovement{InventoryDocumentId=doc.ID,InventoryDocumentLineId=l.ID,WarehouseId=source.ID,StockLotId=lot.ID,MaterialId=material.ID,UnitId=unit,Quantity=qty,UnitCost=10,TotalCost=qty*10,Direction=PrdStockDirection.In,MovementDate=DateTime.Today});doc.TotalCost+=l.TotalCost;await db.SaveChangesAsync();return(l,lot);}
    async Task OtherMovement(PrdStockLot lot,PrdMaterial material,int unit,decimal qty,PrdStockDirection direction,decimal total){db.Add(new PrdStockMovement{WarehouseId=lot.WarehouseId,StockLotId=lot.ID,MaterialId=material.ID,UnitId=unit,Quantity=qty,Direction=direction,TotalCost=total,MovementDate=DateTime.Today});await db.SaveChangesAsync();}
    async Task<(IActionResult result,ProductionInventoryController controller)> Edit(int id,Action<InventoryDocumentEditVM> change){await using var context=new CorrectionContext(Options());var controller=Controller(context);var get=(ViewResult)await controller.Duzenle(id,default);var model=(InventoryDocumentEditVM)get.Model!;model.EditReason="Yanlış birim düzeltmesi";change(model);return(await controller.Duzenle(model,default),controller);}
    async Task<(IActionResult result,ProductionInventoryController controller)> Delete(int id,int lineId,bool detail=true){await using var context=new CorrectionContext(Options());var controller=Controller(context);return(await controller.SatirSil(id,lineId,"Hatalı satır kaydı",true,default,detail),controller);}

    var opening=await Document();var first=await Line(opening,mass,each.ID,280);var spare=await Line(opening,mass,kg.ID,10);
    await OtherMovement(first.lot,mass,kg.ID,20,PrdStockDirection.Out,200);db.Add(new PrdStockReservation{MaterialId=mass.ID,WarehouseId=source.ID,StockLotId=first.lot.ID,UnitId=kg.ID,ReservedQuantity=35,Status=PrdReservationStatus.Active});await db.SaveChangesAsync();
    var edit=await Edit(opening.ID,m=>m.Lines.Single(x=>x.Id==first.line.ID).UnitId=kg.ID);Check(edit.result is RedirectToActionResult,"Admin can correct an erroneously entered piece unit to kilograms");
    var stored=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.ID==first.line.ID);var movement=await db.PrdStockMovements.AsNoTracking().SingleAsync(x=>x.InventoryDocumentLineId==stored.ID);
    Check(stored.UnitId==kg.ID&&movement.UnitId==kg.ID&&stored.Quantity==280&&movement.Quantity==280&&stored.TotalCost==2800,"Unit correction updates the line and linked movement without silently converting numbers");
    Check(await db.PrdMaterials.AsNoTracking().Where(x=>x.ID==mass.ID).Select(x=>x.UnitId).SingleAsync()==kg.ID,"Material master unit and other documents remain unchanged");
    Check(await db.UserActivityLog.AnyAsync(x=>x.Info=="InventoryDocumentLineUnitCorrection"&&x.Parameters!.Contains("OldUnitId")),"Unit correction has a dedicated before/after unit audit record");
    var lotBalance=(await ProductionStockLotReader.LoadAsync(db,source.ID,default)).Single(x=>x.Lot.ID==first.lot.ID);Check(lotBalance.PhysicalQuantity==260&&lotBalance.ReservedQuantity==35,"Stock balances reflect corrected units and active reservations");
    edit=await Edit(opening.ID,m=>m.Lines.Single(x=>x.Id==first.line.ID).Quantity="30");Check(edit.result is ViewResult&&!edit.controller.ModelState.IsValid,"Changing stock below its reserved balance is rejected");
    Check(await db.PrdInventoryDocumentLines.AsNoTracking().Where(x=>x.ID==first.line.ID).Select(x=>x.Quantity).SingleAsync()==280,"Rejected edits roll back all line and movement changes");
    edit=await Edit(opening.ID,m=>m.Lines[0].UnitId=999999);Check(edit.result is ViewResult&&!edit.controller.ModelState.IsValid,"Unknown unit IDs are rejected");
    edit=await Edit(opening.ID,m=>m.Lines[0].UnitId=inactive.ID);Check(edit.result is ViewResult&&!edit.controller.ModelState.IsValid,"Inactive units cannot be assigned as a correction");
    edit=await Edit(opening.ID,m=>m.Lines[0].UnitId=each.ID);Check(edit.result is ViewResult&&!edit.controller.ModelState.IsValid,"An undefined physical unit conversion is rejected");
    var failed=await Delete(opening.ID,first.line.ID);Check(failed.controller.TempData.ContainsKey("error")&&!await db.PrdInventoryDocumentLines.AsNoTracking().Where(x=>x.ID==first.line.ID).Select(x=>x.IsDelete==true).SingleAsync(),"Consumed or reserved incoming stock cannot be deleted");

    var mixed=await Document();var removable=await Line(mixed,pieces,box.ID,1);await Line(mixed,mass,kg.ID,3);await OtherMovement(removable.lot,pieces,box.ID,2,PrdStockDirection.In,200);await OtherMovement(removable.lot,pieces,each.ID,80,PrdStockDirection.Out,80);
    var deleted=await Delete(mixed.ID,removable.line.ID);Check(deleted.result is RedirectToActionResult{ActionName:"Detay"}&&!deleted.controller.TempData.ContainsKey("error"),"Detail-row delete returns to detail and normalizes box/piece balances");
    Check(await db.PrdInventoryDocumentLines.AsNoTracking().Where(x=>x.ID==removable.line.ID).Select(x=>x.IsDelete==true).SingleAsync()&&await db.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentLineId==removable.line.ID).AllAsync(x=>x.IsDelete==true),"Line deletion soft-deletes linked movements atomically");
    Check(await db.PrdInventoryDocuments.AsNoTracking().Where(x=>x.ID==mixed.ID).Select(x=>x.TotalCost).SingleAsync()==30,"Document total is recalculated after deleting a line");
    lotBalance=(await ProductionStockLotReader.LoadAsync(db,source.ID,default)).Single(x=>x.Lot.ID==removable.lot.ID);Check(lotBalance.PhysicalQuantity==120&&lotBalance.StockValue==120,"Deleting one box preserves the correct remaining quantity and stock value");
    var last=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.InventoryDocumentId==mixed.ID&&x.IsDelete!=true);failed=await Delete(mixed.ID,last.ID);Check(failed.controller.TempData.ContainsKey("error"),"The last document line remains protected");
    failed=await Delete(opening.ID,last.ID);Check(failed.result is NotFoundResult,"A line from another document cannot be deleted");
    var reservedDoc=await Document();var guarded=await Line(reservedDoc,pieces,each.ID,20);await Line(reservedDoc,mass,kg.ID,1);await OtherMovement(guarded.lot,pieces,each.ID,80,PrdStockDirection.In,800);db.Add(new PrdStockReservation{MaterialId=pieces.ID,WarehouseId=source.ID,StockLotId=guarded.lot.ID,UnitId=box.ID,ReservedQuantity=.9m,Status=PrdReservationStatus.Active});await db.SaveChangesAsync();
    failed=await Delete(reservedDoc.ID,guarded.line.ID);Check(failed.controller.TempData.ContainsKey("error"),"A reservation in boxes is compared against stock in pieces");
    var managed=await Document(sourceType:"WarehouseCount");var managedLine=await Line(managed,mass,kg.ID,5);await Line(managed,mass,kg.ID,1);
    edit=await Edit(managed.ID,m=>{Check(m.CanChangeUnits&&!m.CanDeleteLines,"Source-managed documents allow Admin unit correction independently of row deletion");m.Lines[0].UnitId=grams.ID;m.Lines[0].Quantity="5000";m.Lines[0].UnitCost="0,01";});Check(edit.result is RedirectToActionResult,"Admin can correct units in a source-managed document");
    Check(await db.PrdInventoryDocumentLines.AsNoTracking().AnyAsync(x=>x.ID==managedLine.line.ID&&x.UnitId==grams.ID)&&await db.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentLineId==managedLine.line.ID).AllAsync(x=>x.UnitId==grams.ID&&x.Quantity==5000),"Source-managed unit correction updates both line and linked stock movement");
    failed=await Delete(managed.ID,managedLine.line.ID);Check(failed.controller.TempData.ContainsKey("error"),"Source-managed row deletion remains protected");

    var transfer=await Document(PrdInventoryDocumentType.WarehouseTransfer);
    async Task<(PrdInventoryDocumentLine line,PrdStockLot sourceLot,PrdStockLot targetLot)> Pair(int unit,decimal qty){var entry=await Line(transfer,mass,unit,qty);var output=await db.PrdStockMovements.SingleAsync(x=>x.InventoryDocumentLineId==entry.line.ID);output.Direction=PrdStockDirection.Out;var toLot=new PrdStockLot{WarehouseId=target.ID,MaterialId=mass.ID,LotNumber=entry.lot.LotNumber,IsActive=true};db.Add(toLot);await db.SaveChangesAsync();entry.line.SourceStockLotId=entry.lot.ID;entry.line.TargetStockLotId=toLot.ID;await OtherMovement(entry.lot,mass,kg.ID,100,PrdStockDirection.In,1000);db.Add(new PrdStockMovement{InventoryDocumentId=transfer.ID,InventoryDocumentLineId=entry.line.ID,WarehouseId=target.ID,StockLotId=toLot.ID,MaterialId=mass.ID,UnitId=unit,Quantity=qty,UnitCost=10,TotalCost=qty*10,Direction=PrdStockDirection.In,MovementDate=DateTime.Today});await db.SaveChangesAsync();return(entry.line,entry.lot,toLot);}
    var pair=await Pair(each.ID,5);await Pair(kg.ID,1);
    edit=await Edit(transfer.ID,m=>m.Lines.Single(x=>x.Id==pair.line.ID).UnitId=kg.ID);Check(edit.result is RedirectToActionResult,"Posted transfer unit correction is accepted");
    Check(await db.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentLineId==pair.line.ID).AllAsync(x=>x.UnitId==kg.ID&&x.Quantity==5),"Transfer source and target movements receive the same corrected unit");
    await OtherMovement(pair.targetLot,mass,kg.ID,1,PrdStockDirection.Out,10);failed=await Delete(transfer.ID,pair.line.ID);Check(failed.controller.TempData.ContainsKey("error"),"Transfer deletion is blocked if target stock was consumed");
    await db.PrdStockMovements.Where(x=>x.StockLotId==pair.targetLot.ID&&x.InventoryDocumentLineId==null).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.IsDelete,true));
    deleted=await Delete(transfer.ID,pair.line.ID);Check(!deleted.controller.TempData.ContainsKey("error")&&await db.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentLineId==pair.line.ID).AllAsync(x=>x.IsDelete==true),"Unused transfer row deletion removes both source and target contributions");

    var concurrent=await Document();var c1=await Line(concurrent,mass,kg.ID,1);var c2=await Line(concurrent,mass,kg.ID,2);await Task.WhenAll(Delete(concurrent.ID,c1.line.ID),Delete(concurrent.ID,c2.line.ID));
    Check(await db.PrdInventoryDocumentLines.AsNoTracking().CountAsync(x=>x.InventoryDocumentId==concurrent.ID&&x.IsDelete!=true)==1,"Concurrent row deletion cannot remove both final lines");
    var rowDoc=await Document();var rowFirst=await Line(rowDoc,mass,kg.ID,5);var rowOther=await Line(rowDoc,mass,kg.ID,3);
    async Task<(IActionResult result,ProductionInventoryController controller)> EditRow(int id,int lineId,Action<InventoryDocumentEditVM> change){await using var context=new CorrectionContext(Options());var controller=Controller(context);var get=await controller.SatirDuzenle(id,lineId,default);if(get is not ViewResult view)return(get,controller);var model=(InventoryDocumentEditVM)view.Model!;Check(model.Lines.Count==1&&model.Lines[0].Id==lineId,"Row edit loads only the selected line");model.EditReason="Tek satır düzeltmesi";change(model);return(await controller.SatirDuzenle(model,default),controller);}
    var managedRow=await EditRow(managed.ID,managedLine.line.ID,m=>{m.Lines[0].UnitId=kg.ID;m.Lines[0].Quantity="5";m.Lines[0].UnitCost="10";});Check(managedRow.result is RedirectToActionResult,"Admin single-row edit can correct a source-managed unit");
    managedRow=await EditRow(managed.ID,managedLine.line.ID,m=>m.Lines[0].UnitId=each.ID);Check(managedRow.result is ViewResult&&!managedRow.controller.ModelState.IsValid,"Source-managed unit correction still rejects undefined physical conversion");
    var rowEdit=await EditRow(rowDoc.ID,rowFirst.line.ID,m=>{m.Lines[0].UnitId=grams.ID;m.Lines[0].Quantity="6000";m.Lines[0].UnitCost="0,01";m.Lines[0].LotNumber="ROW-CORRECTED";m.Lines[0].ProductionDate=DateTime.Today.AddDays(-1);m.Lines[0].ExpirationDate=DateTime.Today.AddYears(1);m.Lines[0].Notes="Düzeltilmiş satır notu";m.CurrencyCode="USD";m.ExchangeRate="99";m.Notes="Forged header";m.DocumentDate=DateTime.Today.AddDays(-10);});
    Check(rowEdit.result is RedirectToActionResult{ActionName:"Detay"},"Row edit saves and returns to document detail");
    stored=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.ID==rowFirst.line.ID);movement=await db.PrdStockMovements.AsNoTracking().SingleAsync(x=>x.InventoryDocumentLineId==stored.ID);
    Check(stored.UnitId==grams.ID&&stored.Quantity==6000&&stored.UnitCost==.01m&&stored.Notes=="Düzeltilmiş satır notu"&&stored.LotNumber=="ROW-CORRECTED"&&stored.ProductionDate==DateTime.Today.AddDays(-1)&&stored.ExpirationDate==DateTime.Today.AddYears(1)&&movement.UnitId==grams.ID&&movement.TotalCost==60,"Row quantity, unit, cost, lot, dates and notes persist with linked movements");
    var untouched=await db.PrdInventoryDocumentLines.AsNoTracking().SingleAsync(x=>x.ID==rowOther.line.ID);var savedHeader=await db.PrdInventoryDocuments.AsNoTracking().SingleAsync(x=>x.ID==rowDoc.ID);
    Check(untouched.Quantity==3&&untouched.UnitId==kg.ID&&untouched.UpdateDate==null&&savedHeader.TotalCost==90&&savedHeader.CurrencyCode=="TRY"&&savedHeader.ExchangeRate==1&&savedHeader.Notes==null&&savedHeader.DocumentDate==DateTime.Today,"Row edit leaves other lines and document header fields unchanged and recalculates total");
    Check(await db.PrdStockLots.AsNoTracking().AnyAsync(x=>x.ID==rowFirst.lot.ID&&x.LotNumber=="ROW-CORRECTED"&&x.ExpirationDate==stored.ExpirationDate)&&await db.UserActivityLog.AnyAsync(x=>x.Info=="InventoryDocumentAdminLineEdit"),"Row edit updates linked lot metadata and writes an audit record");
    rowEdit=await EditRow(rowDoc.ID,rowFirst.line.ID,m=>m.Lines[0].Quantity="0");Check(rowEdit.result is ViewResult{ViewName:"SatirDuzenle",Model:InventoryDocumentEditVM{Lines.Count:1}}&&!rowEdit.controller.ModelState.IsValid,"Invalid row edits retain the single-row form and show validation errors");
    rowEdit=await EditRow(rowDoc.ID,rowOther.line.ID,m=>m.Lines.Add(new InventoryDocumentEditLineVM{Id=rowFirst.line.ID,Quantity="999",UnitId=kg.ID}));Check(rowEdit.result is ViewResult&&!rowEdit.controller.ModelState.IsValid,"A row form cannot update extra document lines");
    Check(await Controller(db).SatirDuzenle(rowDoc.ID,first.line.ID,default) is NotFoundResult,"Row edit rejects a line belonging to another document");
    rowEdit=await EditRow(opening.ID,first.line.ID,m=>m.Lines[0].Quantity="30");Check(rowEdit.result is ViewResult&&!rowEdit.controller.ModelState.IsValid,"Single-row correction preserves reservation balance checks");
    async Task<IReadOnlyList<InventoryDocumentListVM>> Search(string? term)=>(IReadOnlyList<InventoryDocumentListVM>)((ViewResult)await Controller(db).Index(default,term)).Model!;
    var found=await Search("  PIECES  ");Check(found.Count==1&&found[0].Id==reservedDoc.ID,"Product-code search trims input and ignores deleted matching lines");
    Check(found[0].MatchingProducts.Count==1&&found[0].MatchingProducts[0] is {MaterialCode:"PIECES",Quantity:20,Unit:"Adet"},"Search quantities include only the matching product, not other document lines");
    found=await Search("mbalaj");Check(found.Count==1&&found[0].Id==reservedDoc.ID,"Partial product-name search returns the document containing that product");
    found=await Search("MASS");Check(found.Count(x=>x.Id==opening.ID)==1&&found.Single(x=>x.Id==opening.ID).TotalCost==2900,"Multiple matching lines return one document and preserve full document totals");
    Check(found.Single(x=>x.Id==opening.ID).MatchingProducts.Single() is {Quantity:290,Unit:"Kilogram"},"Repeated matching product lines in one unit are summed");
    var unitDoc=await Document();await Line(unitDoc,mass,kg.ID,2);await Line(unitDoc,mass,grams.ID,500);await Line(unitDoc,mass,kg.ID,3);var removed=await Line(unitDoc,mass,kg.ID,99);removed.line.IsDelete=true;await db.SaveChangesAsync();
    var quantities=(await Search("MASS")).Single(x=>x.Id==unitDoc.ID).MatchingProducts;
    Check(quantities.Count==2&&quantities.Any(x=>x.Quantity==5&&x.Unit=="Kilogram")&&quantities.Any(x=>x.Quantity==500&&x.Unit=="Gram"),"Different document units stay separate and deleted quantities are excluded");
    Check((await Search("NO-SUCH-PRODUCT")).Count==0&&(await Search("   ")).Any(x=>x.Id==opening.ID),"Unknown products return no documents; clearing search restores the list");
    var busyDoc=await Document();var busyLine=await Line(busyDoc,mass,kg.ID,2);await Line(busyDoc,mass,kg.ID,1);
    await using(var correction=new CorrectionContext(Options()))
    {
        var controller=Controller(correction);var input=(InventoryDocumentEditVM)((ViewResult)await controller.SatirDuzenle(busyDoc.ID,busyLine.line.ID,default)).Model!;input.EditReason="Kilit bekleme testi";input.Lines[0].Quantity="3";input.Lines[0].Notes="Korunacak düzenleme";
        await correction.Database.OpenConnectionAsync();await correction.Database.ExecuteSqlRawAsync("SET LOCK_TIMEOUT 23000");
        async Task<int> SessionLockTimeout(){await using var command=correction.Database.GetDbConnection().CreateCommand();command.CommandText="SELECT @@LOCK_TIMEOUT";return Convert.ToInt32(await command.ExecuteScalarAsync());}
        await using var blocker=new SqlConnection(cs);await blocker.OpenAsync();await using var block=await blocker.BeginTransactionAsync();
        await using(var hold=blocker.CreateCommand()){hold.Transaction=(SqlTransaction)block;hold.CommandText="UPDATE dbo.PrdInventoryDocument SET Notes=Notes WHERE ID=@id";hold.Parameters.AddWithValue("@id",busyDoc.ID);await hold.ExecuteNonQueryAsync();}
        var timer=System.Diagnostics.Stopwatch.StartNew();var blocked=await controller.SatirDuzenle(input,default);
        Check(blocked is ViewResult{ViewName:"DuzeltmeTekrarDene",Model:InventoryDocumentEditVM retry}&&retry.Lines[0].Quantity=="3"&&retry.Lines[0].Notes=="Korunacak düzenleme"&&controller.Response.StatusCode==409&&timer.Elapsed<TimeSpan.FromSeconds(15),"A blocked document returns a bounded retry response with entered values preserved");
        Check(await SessionLockTimeout()==23000,"Lock timeout is restored on a caller-owned connection after lock failure");
        using(var cancellation=new CancellationTokenSource(TimeSpan.FromMilliseconds(250))){var cancelled=await controller.SatirDuzenle(input,cancellation.Token);Check(cancelled is StatusCodeResult{StatusCode:499},"Cancelling a request while waiting for a document lock does not become an unhandled exception");}
        Check(await SessionLockTimeout()==23000,"Cancelled requests also restore SQL session settings");
        await block.RollbackAsync();
        Check(await correction.PrdInventoryDocumentLines.AsNoTracking().AnyAsync(x=>x.ID==busyLine.line.ID&&x.Quantity==2)&&!await correction.UserActivityLog.AnyAsync(x=>x.Info=="InventoryDocumentAdminLineEdit"&&x.Parameters!.Contains(busyDoc.DocumentNumber)),"Blocked and cancelled corrections leave no partial stock updates");
        var retried=await controller.SatirDuzenle(input,default);Check(retried is RedirectToActionResult{ActionName:"Detay"}&&await correction.PrdInventoryDocumentLines.AsNoTracking().AnyAsync(x=>x.ID==busyLine.line.ID&&x.Quantity==3),"Explicit retry succeeds after the blocking transaction releases its lock");
        Check(await SessionLockTimeout()==23000,"Successful corrections restore SQL session settings");await correction.Database.CloseConnectionAsync();
    }
    await using(var full=new ApplicationDbContext(Options()))Check(!full.Database.HasPendingModelChanges(),"No migration is required for edit/view changes");
    await InventoryPreview.Run(cs,opening.ID,args.Contains("--preview"));
}
finally{await db.Database.EnsureDeletedAsync();Console.WriteLine("Removed isolated database "+database);}

sealed class CorrectionContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        var keep=new[]{typeof(PrdUnit),typeof(PrdUnitConversion),typeof(PrdMaterial),typeof(PrdWarehouse),typeof(PrdStockLot),typeof(PrdStockMovement),typeof(PrdStockReservation),typeof(PrdInventoryDocument),typeof(PrdInventoryDocumentLine),typeof(UserActivityLog),typeof(PurGoodsReceipt),typeof(PurGoodsReceiptLine),typeof(PurGoodsReceiptLandedCost),typeof(PurGoodsReceiptLandedCostAllocation)};
        foreach(var t in b.Model.GetEntityTypes().Select(x=>x.ClrType).Where(x=>!keep.Contains(x)).ToList())b.Ignore(t);
        foreach(var t in keep){b.Entity(t).HasBaseType((Type?)null);b.Entity(t).ToTable(t.Name);b.Entity(t).HasKey(t==typeof(UserActivityLog)?"Id":"ID");}
        foreach(var p in b.Model.GetEntityTypes().SelectMany(x=>x.GetProperties()).Where(x=>x.ClrType==typeof(decimal)||x.ClrType==typeof(decimal?))){p.SetPrecision(18);p.SetScale(6);}
    }
}
sealed class MemoryTempData:ITempDataProvider{public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();public void SaveTempData(HttpContext context,IDictionary<string,object> values){}}
