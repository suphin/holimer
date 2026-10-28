using System.Security.Claims;
using System.Text.Json;
using Ekomers.Data;
using Ekomers.Data.Migrations;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Controllers;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

static class ProductionFlowTests
{
    static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);Console.WriteLine("PASS "+label);}
    static UretimYonetimiController Controller(StockContext db)
    {
        var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"Fixture"),new Claim(ClaimTypes.Role,"Admin")],"Fixture"))};
        return new(db,null!){ControllerContext=new(){HttpContext=http},TempData=new TempDataDictionary(http,new MemoryTempData())};
    }
    public static async Task Run(StockContext db,string cs,int each,int box,IServiceProvider services,Func<HttpContext,string,string,object,Task<string>> render)
    {
        var source=new PrdWarehouse{Code="FLOW-SOURCE",Type=PrdWarehouseType.Main,IsActive=true};var target=new PrdWarehouse{Code="FLOW-TARGET",Type=PrdWarehouseType.Production,IsActive=true};
        var material=new PrdMaterial{Code="FLOW-RAW",Name="Flow raw",UnitId=each,Type=PrdMaterialType.RawMaterial,IsActive=true};var product=new PrdMaterial{Code="FLOW-PRODUCT",UnitId=each,Type=PrdMaterialType.FinishedProduct,IsActive=true};
        db.AddRange(source,target,material,product);await db.SaveChangesAsync();
        var rule=new PrdUnitConversion{MaterialId=material.ID,FromUnitId=box,ToUnitId=each,Factor=100,IsActive=true};db.Add(rule);
        var sourceLot=new PrdStockLot{MaterialId=material.ID,WarehouseId=source.ID,LotNumber="SOURCE-LOT",ExpirationDate=DateTime.Today.AddMonths(6),IsActive=true};
        var productionLot=new PrdStockLot{MaterialId=material.ID,WarehouseId=target.ID,LotNumber="PRODUCTION-LOT",ExpirationDate=DateTime.Today.AddMonths(3),IsActive=true};
        db.AddRange(sourceLot,productionLot);await db.SaveChangesAsync();
        db.AddRange(new PrdStockMovement{MaterialId=material.ID,WarehouseId=source.ID,StockLotId=sourceLot.ID,UnitId=box,Quantity=1,Direction=PrdStockDirection.In,UnitCost=1000,TotalCost=1000,MovementDate=DateTime.Today},
            new PrdStockMovement{MaterialId=material.ID,WarehouseId=source.ID,StockLotId=sourceLot.ID,UnitId=each,Quantity=50,Direction=PrdStockDirection.In,UnitCost=10,TotalCost=500,MovementDate=DateTime.Today},
            new PrdStockMovement{MaterialId=material.ID,WarehouseId=target.ID,StockLotId=productionLot.ID,UnitId=each,Quantity=50,Direction=PrdStockDirection.In,UnitCost=10,TotalCost=500,MovementDate=DateTime.Today});await db.SaveChangesAsync();
        var originals=await db.PrdStockMovements.AsNoTracking().Where(x=>x.MaterialId==material.ID).OrderBy(x=>x.ID).ToListAsync();var originalIds=originals.Select(x=>x.ID).ToList();var originalJson=JsonSerializer.Serialize(originals);
        async Task<(PrdProductionOrder,PrdMaterialRequirement)> Order(decimal need,string number="FLOW")
        {
            var header=new PrdProductionPlanHeader{PlanNumber=number,PlanDate=DateTime.Today,TargetProductionDate=DateTime.Today};db.Add(header);await db.SaveChangesAsync();
            var version=new PrdRecipeVersion{RecipeId=1,VersionNumber=1,BaseQuantity=1,UnitId=each};db.Add(version);await db.SaveChangesAsync();
            var plan=new PrdProductionPlan{ProductionPlanHeaderId=header.ID,ProductMaterialId=product.ID,RecipeVersionId=version.ID,PlannedQuantity=1,UnitId=each};db.Add(plan);await db.SaveChangesAsync();
            var order=new PrdProductionOrder{OrderNumber=number,BatchNumber=number+"-BATCH",ProductionPlanId=plan.ID,RecipeVersionId=version.ID,ProductMaterialId=product.ID,SourceWarehouseId=source.ID,ProductionWarehouseId=target.ID,UnitId=each,PlannedQuantity=1,Status=PrdProductionOrderStatus.Draft};db.Add(order);await db.SaveChangesAsync();
            var req=new PrdMaterialRequirement{ProductionOrderId=order.ID,RecipeItemId=1,MaterialId=material.ID,UnitId=box,TheoreticalQuantity=need};db.Add(req);await db.SaveChangesAsync();db.ChangeTracker.Clear();return(order,req);
        }
        async Task SetFactor(decimal factor){db.ChangeTracker.Clear();var r=await db.PrdUnitConversions.SingleAsync(x=>x.ID==rule.ID);r.Factor=factor;await db.SaveChangesAsync();db.ChangeTracker.Clear();}
        var (order,req)=await Order(2);
        var c=Controller(db);await c.DepoGoreviOlustur(order.ID,default);db.ChangeTracker.Clear();
        var reservations=await db.PrdStockReservations.AsNoTracking().Where(x=>x.MaterialRequirementId==req.ID).ToListAsync();
        Check(reservations.Count==2&&reservations.All(x=>x.UnitId==each&&x.RequirementUnitId==box&&x.RequirementUnitFactor==.01m)&&reservations.Sum(x=>x.ReservedQuantity)==200,"Order reserves normalized quantities with explicit units and frozen requirement conversion");
        Check(reservations.Single(x=>x.WarehouseId==target.ID).ReservedQuantity==50&&reservations.Single(x=>x.WarehouseId==source.ID).ReservedQuantity==150,"Production stock is allocated first; remaining material comes from source warehouse");
        var task=await db.PrdWarehouseTasks.AsNoTracking().SingleAsync(x=>x.ProductionOrderId==order.ID);var item=await db.PrdWarehouseTaskItems.AsNoTracking().SingleAsync(x=>x.WarehouseTaskId==task.ID);var taskLot=await db.PrdWarehouseTaskLots.AsNoTracking().SingleAsync(x=>x.WarehouseTaskItemId==item.ID);
        Check(item.RequestedQuantity==1.5m&&item.UnitId==box&&taskLot.Quantity==150,"Task request stays in recipe units while lot allocation stays in base units");
        await Controller(db).DepoGoreviOlustur(order.ID,default);db.ChangeTracker.Clear();Check(await db.PrdStockReservations.CountAsync(x=>x.MaterialRequirementId==req.ID)==2,"Repeated reservation does not allocate twice");
        var detail=(ProductionWarehouseTaskDetailVM)((ViewResult)await Controller(db).DepoGorevDetay(task.ID,default)).Model!;
        Check(detail.UnitError==null&&detail.Items.Single().Lots.Single().TaskUnitConversionFactor==.01m&&detail.Items.Single().Lots.Single().TaskQuantity==1.5m,"Warehouse detail displays both actual stock units and converted recipe quantity");
        using(var scope=services.CreateScope())
        {
            var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};http.Request.Scheme="http";http.Request.Host=new HostString("localhost");
            var html=await render(http,"UretimYonetimi","DepoGorevDetay",detail);
            Check(html.Contains("150 Adet")&&(html.Contains("1.5 Koli")||html.Contains("1,5 Koli")),"Compiled task view shows both base-unit lot plan and recipe-unit quantity");
        }
        var orderDetail=(ProductionOrderDetailVM)((ViewResult)await Controller(db).EmirDetay(order.ID,default)).Model!;
        Check(orderDetail.StockError==null&&orderDetail.Requirements.Single().PhysicalStockQuantity==2&&orderDetail.Requirements.Single().ActiveReservationQuantity==2,"Order stock view agrees with normalized and reserved warehouse quantities");
        // Product-specific conversion changes must not reinterpret an open allocation.
        await Controller(db).DepoGoreviHazirlamayaBasla(task.ID,default);await SetFactor(200);
        c=Controller(db);await c.DepoGoreviHazirliginiKaydet(new(){Id=task.ID,CompletePreparation=true,Lots=[new(){Id=taskLot.ID,PreparedQuantityInput="100"}]},default);db.ChangeTracker.Clear();
        Check(c.TempData.ContainsKey("error")&&(await db.PrdWarehouseTaskLots.AsNoTracking().SingleAsync(x=>x.ID==taskLot.ID)).PreparedQuantity==0,"Changing a reserved conversion blocks preparation and preserves saved values");
        var blockedDetail=(ProductionWarehouseTaskDetailVM)((ViewResult)await Controller(db).DepoGorevDetay(task.ID,default)).Model!;
        using(var scope=services.CreateScope())
        {
            var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};http.Request.Scheme="http";http.Request.Host=new HostString("localhost");
            var html=await render(http,"UretimYonetimi","DepoGorevDetay",blockedDetail);
            Check(blockedDetail.UnitError!=null&&html.Contains("role=\"alert\"")&&!html.Contains("warehouse-prepared-input"),"Changed conversion is visible in task view and editing/shipping controls are disabled");
        }
        await SetFactor(100);
        await Controller(db).DepoGoreviHazirliginiKaydet(new(){Id=task.ID,CompletePreparation=true,Lots=[new(){Id=taskLot.ID,PreparedQuantityInput="100"}]},default);db.ChangeTracker.Clear();
        Check((await db.PrdWarehouseTaskItems.AsNoTracking().SingleAsync(x=>x.ID==item.ID)).PreparedQuantity==1,"Partial preparation converts 100 base units to one recipe box");
        await Controller(db).DepoGoreviSevkEt(task.ID,default);db.ChangeTracker.Clear();
        var docs=await db.PrdInventoryDocuments.AsNoTracking().Where(x=>x.SourceDocumentType=="ProductionOrder"&&x.SourceDocumentId==order.ID).ToListAsync();var firstDoc=docs.Single();
        var movements=await db.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentId==firstDoc.ID).ToListAsync();
        Check(movements.Count==2&&movements.All(x=>x.Quantity==100&&x.UnitId==each&&x.TotalCost==1000),"Production shipment creates equal base-unit source/target movements and preserved value");
        var sourceReservation=await db.PrdStockReservations.AsNoTracking().SingleAsync(x=>x.MaterialRequirementId==req.ID&&x.WarehouseId==source.ID);
        Check(sourceReservation.UsedQuantity==100&&sourceReservation.ReservedQuantity==150&&sourceReservation.Status==PrdReservationStatus.PartiallyUsed,"Partial shipment consumes only shipped quantity from the source reservation");
        c=Controller(db);await c.DepoGoreviSevkEt(task.ID,default);db.ChangeTracker.Clear();Check(await db.PrdInventoryDocuments.CountAsync(x=>x.SourceDocumentType=="ProductionOrder"&&x.SourceDocumentId==order.ID)==1,"Repeated shipment does not duplicate stock movements");
        await Controller(db).DepoGoreviHazirlamayaBasla(task.ID,default);
        await Controller(db).DepoGoreviHazirliginiKaydet(new(){Id=task.ID,CompletePreparation=true,Lots=[new(){Id=taskLot.ID,PreparedQuantityInput="150"}]},default);db.ChangeTracker.Clear();
        await SetFactor(200);c=Controller(db);await c.DepoGoreviSevkEt(task.ID,default);db.ChangeTracker.Clear();Check(c.TempData.ContainsKey("error")&&await db.PrdInventoryDocuments.CountAsync(x=>x.SourceDocumentId==order.ID&&x.SourceDocumentType=="ProductionOrder")==1,"Changed coefficient blocks shipping an already prepared allocation");await SetFactor(100);
        await Controller(db).DepoGoreviSevkEt(task.ID,default);db.ChangeTracker.Clear();
        Check((await db.PrdWarehouseTaskItems.AsNoTracking().SingleAsync(x=>x.ID==item.ID)).ShippedQuantity==1.5m,"Incremental shipment adds only the unshipped portion in requirement units");
        await Controller(db).DepoGoreviTeslimAl(task.ID,default);db.ChangeTracker.Clear();
        Check(await db.PrdProductionOrders.AnyAsync(x=>x.ID==order.ID&&x.Status==PrdProductionOrderStatus.Ready),"Fully shipped task can be delivered to production");
        await Controller(db).UretimiBaslat(order.ID,default);db.ChangeTracker.Clear();
        var actuals=await db.PrdProductionMaterialActuals.AsNoTracking().Where(x=>x.ProductionOrderId==order.ID).ToListAsync();
        Check(actuals.Sum(x=>x.IssuedQuantity)==200&&actuals.All(x=>x.UnitId==each)&&(await db.PrdMaterialRequirements.AsNoTracking().SingleAsync(x=>x.ID==req.ID)).IssuedQuantity==2,"Production issue consumes base units while requirement counters remain in recipe units");
        var issued=await db.PrdStockMovements.AsNoTracking().Where(x=>x.DocumentType==PrdStockDocumentType.ProductionOrder&&x.DocumentId==order.ID&&x.MovementType==PrdStockMovementType.ProductionIssue).ToListAsync();
        Check(issued.Sum(x=>x.TotalCost)==2000&&!await db.PrdStockReservations.AnyAsync(x=>x.MaterialRequirementId==req.ID&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)),"Production issue preserves full lot value and closes consumed reservations");
        await Controller(db).UretimiBaslat(order.ID,default);Check(await db.PrdProductionMaterialActuals.CountAsync(x=>x.ProductionOrderId==order.ID)==actuals.Count,"Repeated production start cannot issue material twice");
        await Controller(db).GerceklesenTamamla(new ProductionExecutionDetailVM{OrderId=order.ID,ActualQuantityInput="1",ProductionDate=DateTime.Today,Materials=actuals.Select(x=>new ProductionExecutionMaterialVM{ActualId=x.ID,ConsumedQuantityInput=(x.IssuedQuantity-10).ToString(System.Globalization.CultureInfo.InvariantCulture),ReturnedQuantityInput="10",WasteQuantityInput="0"}).ToList()},default);db.ChangeTracker.Clear();
        var completedReq=await db.PrdMaterialRequirements.AsNoTracking().SingleAsync(x=>x.ID==req.ID);
        Check(completedReq.ConsumedQuantity==1.8m&&completedReq.ReturnedQuantity==.2m&&await db.PrdProductionResults.AnyAsync(x=>x.ProductionOrderId==order.ID&&x.MaterialCost==1800),"Completion and returns convert base-unit actuals back to recipe counters without losing material cost");
        Check(JsonSerializer.Serialize(await db.PrdStockMovements.AsNoTracking().Where(x=>originalIds.Contains(x.ID)).OrderBy(x=>x.ID).ToListAsync())==originalJson,"Production allocation and shipment never rewrite historical mixed-unit movements");
        // Isolated source/target pair for competing orders and manual consumption.
        source=new PrdWarehouse{Code="RACE-SOURCE",Type=PrdWarehouseType.Main,IsActive=true};target=new PrdWarehouse{Code="RACE-TARGET",Type=PrdWarehouseType.Production,IsActive=true};db.AddRange(source,target);await db.SaveChangesAsync();
        var raceLot=new PrdStockLot{MaterialId=material.ID,WarehouseId=source.ID,LotNumber="RACE",IsActive=true};db.Add(raceLot);await db.SaveChangesAsync();
        db.Add(new PrdStockMovement{MaterialId=material.ID,WarehouseId=source.ID,StockLotId=raceLot.ID,UnitId=each,Quantity=100,Direction=PrdStockDirection.In,UnitCost=10,TotalCost=1000,MovementDate=DateTime.Today});await db.SaveChangesAsync();
        var (extraOrder,extraReq)=await Order(.5m,"EXTRA");await Controller(db).DepoGoreviOlustur(extraOrder.ID,default);db.ChangeTracker.Clear();
        var extraTask=await db.PrdWarehouseTasks.AsNoTracking().SingleAsync(x=>x.ProductionOrderId==extraOrder.ID);var extraItem=await db.PrdWarehouseTaskItems.AsNoTracking().SingleAsync(x=>x.WarehouseTaskId==extraTask.ID);var extraLot=await db.PrdWarehouseTaskLots.AsNoTracking().SingleAsync(x=>x.WarehouseTaskItemId==extraItem.ID);
        await Controller(db).DepoGoreviHazirlamayaBasla(extraTask.ID,default);
        await Controller(db).DepoGoreviHazirliginiKaydet(new(){Id=extraTask.ID,Lots=[new(){Id=extraLot.ID,PreparedQuantityInput="80"}]},default);db.ChangeTracker.Clear();
        Check((await db.PrdStockReservations.AsNoTracking().SingleAsync(x=>x.MaterialRequirementId==extraReq.ID)).ReservedQuantity==80&&(await db.PrdMaterialRequirements.AsNoTracking().SingleAsync(x=>x.ID==extraReq.ID)).ReservedQuantity==.8m,"Over-preparation increases both base-unit reservation and recipe-unit counter");
        c=Controller(db);await c.DepoGoreviHazirliginiKaydet(new(){Id=extraTask.ID,Lots=[new(){Id=extraLot.ID,PreparedQuantityInput="120"}]},default);db.ChangeTracker.Clear();
        Check(c.TempData.ContainsKey("error")&&(await db.PrdStockReservations.AsNoTracking().SingleAsync(x=>x.MaterialRequirementId==extraReq.ID)).ReservedQuantity==80,"Excess preparation cannot over-reserve free physical stock");
        await Controller(db).DepoGoreviHazirliginiKaydet(new(){Id=extraTask.ID,Lots=[new(){Id=extraLot.ID,PreparedQuantityInput="50"}]},default);db.ChangeTracker.Clear();
        var extraReservation=await db.PrdStockReservations.SingleAsync(x=>x.MaterialRequirementId==extraReq.ID);
        Check(extraReservation.ReservedQuantity==50&&(await db.PrdMaterialRequirements.AsNoTracking().SingleAsync(x=>x.ID==extraReq.ID)).ReservedQuantity==.5m,"Reducing preparation releases excess allocation without mixing units");
        extraReservation.ReleasedQuantity=extraReservation.ReservedQuantity;extraReservation.Status=PrdReservationStatus.Released;await db.SaveChangesAsync();db.ChangeTracker.Clear();
        var (orderA,reqA)=await Order(1,"RACE-A");var (orderB,reqB)=await Order(1,"RACE-B");
        async Task Reserve(int id){await using var isolated=new StockContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);await Controller(isolated).DepoGoreviOlustur(id,default);}
        await Task.WhenAll(Task.Run(()=>Reserve(orderA.ID)),Task.Run(()=>Reserve(orderB.ID)));db.ChangeTracker.Clear();
        var raceReservations=await db.PrdStockReservations.AsNoTracking().Where(x=>x.MaterialRequirementId==reqA.ID||x.MaterialRequirementId==reqB.ID).ToListAsync();
        Check(raceReservations.Count==1&&raceReservations.Single().ReservedQuantity==100,"Two concurrent orders cannot reserve the same normalized stock twice");
        var raceReserve=await db.PrdStockReservations.SingleAsync(x=>x.ID==raceReservations.Single().ID);raceReserve.ReleasedQuantity=raceReserve.ReservedQuantity;raceReserve.Status=PrdReservationStatus.Released;await db.SaveChangesAsync();db.ChangeTracker.Clear();
        Check((await ProductionStockLotReader.LoadAsync(db,source.ID,default)).Single().ReservedQuantity==0,"Released reservation no longer encumbers physical stock");
        var rawHttp=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"Fixture"),new Claim(ClaimTypes.Role,"Admin")],"Fixture"))};
        var raw=new HammaddeSarfController(db,new RawMaterialConsumptionService(db),new PurchasingInventoryReversalService(db)){ControllerContext=new(){HttpContext=rawHttp},TempData=new TempDataDictionary(rawHttp,new MemoryTempData())};
        var rawDraft=(RedirectToActionResult)await raw.Yeni(new RawMaterialConsumptionCreateVM{WarehouseId=source.ID,Notes="Race consumption",Lines=[new(){StockLotId=raceLot.ID,Quantity="100"}]},default);var rawId=(int)rawDraft.RouteValues!["id"]!;
        var (orderC,reqC)=await Order(1,"RACE-C");
        async Task Consume(){await using var isolated=new StockContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);var http=new DefaultHttpContext{User=rawHttp.User};var rc=new HammaddeSarfController(isolated,new RawMaterialConsumptionService(isolated),new PurchasingInventoryReversalService(isolated)){ControllerContext=new(){HttpContext=http},TempData=new TempDataDictionary(http,new MemoryTempData())};await rc.StoktanDus(rawId,true,default);}
        await Task.WhenAll(Task.Run(()=>Reserve(orderC.ID)),Task.Run(Consume));db.ChangeTracker.Clear();
        var afterRace=(await ProductionStockLotReader.LoadAsync(db,source.ID,default,includeEmpty:true)).Single();
        var consumed=await db.PrdInventoryDocuments.AnyAsync(x=>x.ID==rawId&&x.Status==PrdInventoryDocumentStatus.Posted);
        Check(consumed?afterRace.PhysicalQuantity==0&&afterRace.ReservedQuantity==0:afterRace.PhysicalQuantity==100&&afterRace.ReservedQuantity==100,"Concurrent production reservation and manual consumption respect the same warehouse lock");
        // Nullable legacy metadata is not guessed for a physically mixed lot.
        var ambiguous=new PrdStockLot{MaterialId=material.ID,WarehouseId=source.ID,LotNumber="LEGACY-MIXED",IsActive=true};db.Add(ambiguous);await db.SaveChangesAsync();
        db.Add(new PrdStockMovement{MaterialId=material.ID,WarehouseId=source.ID,StockLotId=ambiguous.ID,UnitId=box,Quantity=1,Direction=PrdStockDirection.In,UnitCost=1000,TotalCost=1000,MovementDate=DateTime.Today});
        var (legacyOrder,legacyReq)=await Order(.1m,"LEGACY");db.Add(new PrdStockReservation{MaterialRequirementId=legacyReq.ID,MaterialId=material.ID,WarehouseId=source.ID,StockLotId=ambiguous.ID,ReservedQuantity=1,Status=PrdReservationStatus.Active});await db.SaveChangesAsync();db.ChangeTracker.Clear();
        c=Controller(db);await c.DepoGoreviOlustur(legacyOrder.ID,default);db.ChangeTracker.Clear();
        Check(c.TempData.ContainsKey("error")&&!await db.PrdWarehouseTasks.AnyAsync(x=>x.ProductionOrderId==legacyOrder.ID),"Ambiguous legacy reservation stops allocation without creating partial tasks");
        await MigrationTest(db);
    }

    static async Task MigrationTest(StockContext db)
    {
        // Execute only this migration's DDL against an independent fixture table in the disposable database.
        await db.Database.ExecuteSqlRawAsync("CREATE TABLE ReservationMigrationFixture(ID int PRIMARY KEY, ReservedQuantity decimal(18,6)); INSERT ReservationMigrationFixture VALUES(1,12.5);");
        await using var model=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(db.Database.GetConnectionString()).Options);
        var commands=model.GetService<IMigrationsSqlGenerator>().Generate(new AddProductionReservationUnits().UpOperations,model.Model);
        foreach(var command in commands)await db.Database.ExecuteSqlRawAsync(command.CommandText.Replace("[PrdStockReservation]","[ReservationMigrationFixture]"));
        var preserved=await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM ReservationMigrationFixture WHERE ReservedQuantity=12.5 AND UnitId IS NULL AND RequirementUnitId IS NULL AND RequirementUnitFactor IS NULL").SingleAsync();
        Check(preserved==1,"Migration adds nullable unit metadata without guessing or changing legacy quantities");
    }
}
