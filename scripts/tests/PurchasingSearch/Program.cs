using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Ekomers.Web.Controllers;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

static void Check(bool condition,string message){if(!condition)throw new Exception("FAIL "+message);Console.WriteLine("PASS "+message);}
var database="CodexPurchasingSearch_"+Guid.NewGuid().ToString("N");
var connection=new SqlConnectionStringBuilder{DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog=database,IntegratedSecurity=true,TrustServerCertificate=true};
await using var db=new SearchContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options);
try
{
    await db.Database.EnsureCreatedAsync();
    var unit=new PrdUnit{Code="KG",Name="Kilogram"};var piece=new PrdUnit{Code="ADET",Name="Adet"};db.AddRange(unit,piece);await db.SaveChangesAsync();
    var material=new PrdMaterial{Code="150HM.TEST.001",Name="C vitamini",UnitId=unit.ID};var unused=new PrdMaterial{Code="150HM.TEST.002",Name="Yalnız talep edilen ürün",UnitId=unit.ID};
    var alpha=new PurSupplier{Code="320.TEST.A",Name="Alpha Tedarik"};var beta=new PurSupplier{Code="320.TEST.B",Name="Beta Tedarik"};db.AddRange(material,unused,alpha,beta);await db.SaveChangesAsync();
    var date=new DateTime(2026,10,1);var request=new PurPurchaseRequest{RequestNumber="TALEP-1",RequestDate=date,RequestedUserId="test",Status=PurPurchaseRequestStatus.Approved};var independent=new PurPurchaseRequest{RequestNumber="TALEP-2",RequestDate=date,RequestedUserId="test"};db.AddRange(request,independent);await db.SaveChangesAsync();
    var rl=new PurPurchaseRequestLine{PurchaseRequestId=request.ID,MaterialId=material.ID,UnitId=unit.ID,RequestedQuantity=100,ApprovedQuantity=100,Status=PurPurchaseRequestLineStatus.Approved};
    var orphan=new PurPurchaseRequestLine{PurchaseRequestId=independent.ID,MaterialId=material.ID,UnitId=unit.ID,RequestedQuantity=77};var unusedLine=new PurPurchaseRequestLine{PurchaseRequestId=independent.ID,MaterialId=unused.ID,UnitId=unit.ID,RequestedQuantity=3};db.AddRange(rl,orphan,unusedLine);await db.SaveChangesAsync();
    async Task<PurSupplierQuotation> Quote(PurSupplier supplier){var q=new PurSupplierQuotation{QuotationNumber="TEKLIF-"+supplier.Code,PurchaseRequestId=request.ID,SupplierId=supplier.ID,QuotationDate=date.AddDays(1)};db.Add(q);await db.SaveChangesAsync();db.Add(new PurSupplierQuotationLine{SupplierQuotationId=q.ID,PurchaseRequestLineId=rl.ID,MaterialId=material.ID,UnitId=unit.ID,OfferedQuantity=100});await db.SaveChangesAsync();return q;}
    var quote=await Quote(alpha);await Quote(beta);await Quote(alpha);
    async Task<(PurPurchaseOrder,PurPurchaseOrderLine)> Order(PurSupplier supplier,int unitId,decimal quantity,int? requestLine=null){var o=new PurPurchaseOrder{OrderNumber="SIP-"+Guid.NewGuid().ToString("N")[..6],SupplierId=supplier.ID,OrderDate=date.AddDays(2)};db.Add(o);await db.SaveChangesAsync();var l=new PurPurchaseOrderLine{PurchaseOrderId=o.ID,MaterialId=material.ID,UnitId=unitId,OrderedQuantity=quantity,PurchaseRequestLineId=requestLine??rl.ID};db.Add(l);await db.SaveChangesAsync();return(o,l);}
    var a=await Order(alpha,unit.ID,100);var b=await Order(beta,unit.ID,30);var pieces=await Order(alpha,piece.ID,2,0);
    async Task<PurGoodsReceiptLine> Receive((PurPurchaseOrder order,PurPurchaseOrderLine line) source,decimal quantity,int days,PurGoodsReceiptStatus status=PurGoodsReceiptStatus.Recorded,bool deleted=false){var r=new PurGoodsReceipt{ReceiptNumber="MK-"+Guid.NewGuid().ToString("N")[..6],PurchaseOrderId=source.order.ID,ReceiptDate=date.AddDays(days).AddHours(23),Status=status,DispatchNumber="IRS-TEST",InvoiceNumber="FAT-TEST",IsDelete=deleted};db.Add(r);await db.SaveChangesAsync();var line=new PurGoodsReceiptLine{GoodsReceiptId=r.ID,PurchaseOrderLineId=source.line.ID,MaterialId=material.ID,UnitId=source.line.UnitId,ReceivedQuantity=quantity,LotNumber="LOT-TEST",ProductionDate=date.AddMonths(-1),ExpirationDate=date.AddYears(2),Notes="<script>alert(1)</script>"};db.Add(line);await db.SaveChangesAsync();return line;}
    var received=await Receive(a,25,3);await Receive(a,15,4);await Receive(b,10,3);await Receive(pieces,1,3);await Receive(a,999,4,PurGoodsReceiptStatus.Cancelled);await Receive(a,888,4,deleted:true);
    var receipt=await db.PurGoodsReceipts.FindAsync(received.GoodsReceiptId);receipt!.QuarantineDate=date.AddDays(4);receipt.QuarantineUserId="test";
    db.Add(new PurQualityInspection{InspectionNumber="ANALIZ-TEST",GoodsReceiptId=received.GoodsReceiptId,GoodsReceiptLineId=received.ID,MaterialId=material.ID,DecisionDate=date.AddDays(5),Status=PrdQualityControlStatus.Approved,DecisionNote="Uygun"});
    db.Add(new PurQuotationApprovalHistory{SupplierQuotationId=quote.ID,SupplierQuotationLineId=await db.PurSupplierQuotationLines.Where(x=>x.SupplierQuotationId==quote.ID).Select(x=>x.ID).FirstAsync(),ActionDate=date.AddDays(2),ActionUserId="test",ApprovedQuantity=100,NewStatus=PurSupplierQuotationLineStatus.Approved});
    for(var i=0;i<60;i++)db.Add(new PurRequestApprovalHistory{PurchaseRequestId=request.ID,PurchaseRequestLineId=rl.ID,ActionDate=date.AddMinutes(i),ActionUserId="test",ApprovedQuantity=100,NewStatus=PurPurchaseRequestLineStatus.Approved});
    var email=new PurPriceRequestEmail{ReferenceNumber="FIYAT-TEST",SupplierId=alpha.ID,Subject="Fiyat isteği",CreateDate=date,Status=PurPriceRequestEmailStatus.Sent,SentDate=date};db.Add(email);await db.SaveChangesAsync();db.Add(new PurPriceRequestEmailLine{PriceRequestEmailId=email.ID,PurchaseRequestLineId=rl.ID,Quantity=100});await db.SaveChangesAsync();db.ChangeTracker.Clear();
    var service=new PurchasingSearchService(db);
    async Task<PurchasingSearchVM> Search(string? search=null,string? product=null,string? supplier=null,PurchasingSearchStage? stage=null,bool cancelled=false,DateTime? start=null,DateTime? end=null,int page=1){var m=new PurchasingSearchVM{Run=true,Search=search,Product=product,Supplier=supplier,Stage=stage,IncludeCancelled=cancelled,StartDate=start,EndDate=end,Page=page};await service.SearchAsync(m);return m;}
    var model=await Search(supplier:"Alpha");
    var kg=model.Summaries.Single(x=>x.UnitId==unit.ID);Check(kg.OrderedQuantity==100&&kg.ReceivedQuantity==40&&kg.QuotationCount==2,"Partial receipts and multiple quotes never multiply order/receipt totals");
    Check(model.Summaries.Count==2&&model.Summaries.Single(x=>x.UnitId==piece.ID).ReceivedQuantity==1,"Different units remain separate");
    Check(model.RequestCount==1&&model.OrderCount==2&&model.ReceiptCount==3,"Supplier filter includes linked requests without counting unrelated requests");
    Check(model.Events.Count==50&&model.EventCount>50,"History is paged on the SQL server");
    var page2=await Search(supplier:"Alpha",page:2);Check(!model.Events.Select(x=>(x.Stage,x.DocumentId,x.LineId)).Intersect(page2.Events.Select(x=>(x.Stage,x.DocumentId,x.LineId))).Any(),"Stable timeline pagination has no duplicate events");
    var general=await Search(search:"Alpha");Check(general.SummaryCount==2&&general.RequestCount==1,"General search accepts supplier names");
    var product=await Search(search:"150HM.TEST.001");Check(product.SummaryCount==3&&product.RequestCount==2,"Product search spans suppliers and standalone requests");
    var combined=await Search(product:"vitamini",supplier:"Beta");Check(combined.Summaries.Single().ReceivedQuantity==10&&combined.OrderCount==1,"Product and supplier filters combine with AND");
    var empty=await Search(product:"olmayan",supplier:"Alpha");Check(empty.EventCount==0&&empty.SummaryCount==0,"Empty results return no unrelated records");
    var cancelled=await Search(supplier:"Alpha",stage:PurchasingSearchStage.Receipt,cancelled:true);Check(cancelled.Events.Any(x=>x.Cancelled)&&cancelled.Summaries.Single(x=>x.UnitId==unit.ID).ReceivedQuantity==40,"Cancelled history remains visible on demand but never inflates totals");
    Check(!cancelled.Events.Any(x=>x.Quantity==888),"Soft-deleted parent documents are excluded");
    var dated=await Search(supplier:"Alpha",stage:PurchasingSearchStage.Receipt,start:date.AddDays(3),end:date.AddDays(3));Check(dated.Events.Count==2&&dated.Summaries.Single(x=>x.UnitId==unit.ID).ReceivedQuantity==25,"Inclusive end day includes late-night receipts");
    var history=await Search(supplier:"Alpha",stage:PurchasingSearchStage.QualityDecision);Check(history.Events.Single().Notes=="Uygun"&&history.Events.Single().DetailAction=="AnalizDetay","Quality decisions link to the analysis detail");
    Check((await Search(search:"LOT-TEST",stage:PurchasingSearchStage.Receipt)).Events.Count==4,"Lot search finds deliveries");
    var dates=(await Search(supplier:"Alpha",stage:PurchasingSearchStage.Receipt)).Events.First(x=>x.UnitId==unit.ID);Check(dates.LotNumber=="LOT-TEST"&&dates.ProductionDate.HasValue&&dates.ExpirationDate.HasValue&&dates.InvoiceNumber=="FAT-TEST","Receipt history preserves lot, production/expiry dates and invoice references");
    Check(typeof(SatinalmaAramaController).GetCustomAttributes(typeof(AuthorizeAttribute),true).Cast<AuthorizeAttribute>().Single().Policy=="SatinalmaGoruntule","Search page uses existing purchasing authorization");
    var controller=new SatinalmaAramaController(service){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};await controller.Index(new(){Run=true},default);Check(!controller.ModelState.IsValid,"Empty search is rejected before querying");
    controller.ModelState.Clear();await controller.Index(new(){Run=true,Search="Al",StartDate=date.AddDays(2),EndDate=date},default);Check(!controller.ModelState.IsValid,"Invalid date ranges are rejected");
    controller.ModelState.Clear();await controller.Index(new(){Run=true,Search="Al",Stage=(PurchasingSearchStage)999},default);Check(!controller.ModelState.IsValid,"Unknown history stages are rejected");
    var exact=new PurchasingSearchVM{Run=true,MaterialId=material.ID,SupplierId=alpha.ID};await service.SearchAsync(exact);Check(exact.SummaryCount==2&&exact.OrderCount==2,"Summary drilldown uses exact supplier and material IDs");
    for(var i=0;i<26;i++){
        var s=new PurSupplier{Code="PAGING-"+i.ToString("D2"),Name="Sayfalama firması "+i.ToString("D2")};db.Add(s);await db.SaveChangesAsync();await Order(s,unit.ID,1);
    }
    var summary1=await Search(supplier:"PAGING-");var summary2=new PurchasingSearchVM{Run=true,Supplier="PAGING-",SummaryPage=2};await service.SearchAsync(summary2);
    Check(summary1.SummaryCount==26&&summary1.Summaries.Count==25&&summary2.Summaries.Count==1&&!summary1.Summaries.Select(x=>x.SupplierId).Intersect(summary2.Summaries.Select(x=>x.SupplierId)).Any(),"Summary pagination preserves all matching suppliers without duplication");
    await using(var actual=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection.ConnectionString).Options)){
        var realModel=new PurchasingSearchVM{Run=true,Supplier="Alpha",Stage=PurchasingSearchStage.Receipt};await new PurchasingSearchService(actual).SearchAsync(realModel);
        Check(realModel.Summaries.Single(x=>x.UnitId==unit.ID).ReceivedQuantity==40&&realModel.Events.Count==3,"Queries execute with the full production EF model and its column mappings");
    }
    await Preview.Run(db,service,args.Contains("--preview"));
}
finally{await db.Database.EnsureDeletedAsync();Console.WriteLine("Removed isolated database "+database);}

sealed class SearchContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        var keep=new[]{typeof(PrdMaterial),typeof(PrdUnit),typeof(PurSupplier),typeof(PurPurchaseRequest),typeof(PurPurchaseRequestLine),typeof(PurSupplierQuotation),typeof(PurSupplierQuotationLine),typeof(PurPurchaseOrder),typeof(PurPurchaseOrderLine),typeof(PurGoodsReceipt),typeof(PurGoodsReceiptLine),typeof(PurPriceRequestEmail),typeof(PurPriceRequestEmailLine),typeof(PurRequestApprovalHistory),typeof(PurQuotationApprovalHistory),typeof(PurQualityInspection)};
        foreach(var t in b.Model.GetEntityTypes().Select(x=>x.ClrType).Where(x=>!keep.Contains(x)).ToList())b.Ignore(t);
        foreach(var t in keep){b.Entity(t).HasBaseType((Type?)null);b.Entity(t).ToTable(t.Name);b.Entity(t).HasKey("ID");}
        foreach(var property in b.Model.GetEntityTypes().SelectMany(x=>x.GetProperties()).Where(x=>x.ClrType==typeof(decimal)||x.ClrType==typeof(decimal?))){property.SetPrecision(18);property.SetScale(6);}
    }
}

static class Preview
{
    public static async Task Run(ApplicationDbContext db,PurchasingSearchService service,bool serve)
    {
        var root=new DirectoryInfo(Directory.GetCurrentDirectory());while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Ekomers.Web","wwwroot")))root=root.Parent;
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SatinalmaAramaController).Assembly);builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        await using var app=builder.Build();app.UseStaticFiles(new StaticFileOptions{FileProvider=new PhysicalFileProvider(Path.Combine(root!.FullName,"Ekomers.Web","wwwroot"))});
        async Task<string> Render(HttpContext http,PurchasingSearchVM model)
        {
            http.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(),"Fixture"));var route=new RouteData();route.Values["controller"]="SatinalmaArama";route.Values["action"]="Index";
            var action=new ActionContext(http,route,new ActionDescriptor(),new ModelStateDictionary());var view=http.RequestServices.GetRequiredService<IRazorViewEngine>().GetView(null,"/Views/SatinalmaArama/Index.cshtml",false);if(!view.Success)throw new Exception("Compiled view not found");using var writer=new StringWriter();
            await view.View.RenderAsync(new ViewContext(action,view.View,new ViewDataDictionary(new EmptyModelMetadataProvider(),action.ModelState){Model=model},new TempDataDictionary(http,new MemoryTempData()),writer,new HtmlHelperOptions()));return writer.ToString();
        }
        app.MapGet("/search",(Delegate)new Func<HttpContext,Task<IResult>>(async http=>
        {
            var q=http.Request.Query;var model=new PurchasingSearchVM{Run=q.ContainsKey("Run")||q.ContainsKey("run"),Search=q["Search"],Product=q["Product"],Supplier=q["Supplier"],Stage=Enum.TryParse<PurchasingSearchStage>(q["Stage"],out var stage)?stage:null,Page=int.TryParse(q["Page"],out var page)?page:1,SummaryPage=int.TryParse(q["SummaryPage"],out var summary)?summary:1,IncludeCancelled=q["IncludeCancelled"].ToString().StartsWith("true",StringComparison.OrdinalIgnoreCase),MaterialId=int.TryParse(q["MaterialId"],out var material)?material:null,SupplierId=int.TryParse(q["SupplierId"],out var supplier)?supplier:null,StartDate=DateTime.TryParse(q["StartDate"],out var start)?start:null,EndDate=DateTime.TryParse(q["EndDate"],out var end)?end:null};
            if(model.Run)await service.SearchAsync(model);
            var html=await Render(http,model);return Results.Content("<!doctype html><html lang='tr'><head><meta charset='utf-8'><title>TEST · Satınalma Arama</title><link rel='stylesheet' href='/assets1/plugins/global/plugins.bundle.css'><link rel='stylesheet' href='/assets1/css/style.bundle.css'></head><body style='padding:24px;background:#f4f8f6'><div class='alert alert-warning'>İZOLE TEST · Canlı satınalma verilerine bağlı değildir.</div>"+html.Replace("/SatinalmaYonetimi/Arama","/search")+"</body></html>","text/html; charset=utf-8");
        }));
        app.MapGet("/fixture-stop",(IHostApplicationLifetime lifetime)=>{lifetime.StopApplication();return "Isolated preview stopped.";});
        app.MapDefaultControllerRoute();await app.StartAsync();
        using(var scope=app.Services.CreateScope()){
            var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};var m=new PurchasingSearchVM{Run=true,Supplier="Alpha",Stage=PurchasingSearchStage.Receipt};await service.SearchAsync(m);var html=await Render(http,m);if(!html.Contains("&lt;script&gt;alert(1)&lt;/script&gt;")||html.Contains("<script>alert(1)</script>"))throw new Exception("FAIL Razor escaping");Console.WriteLine("PASS Compiled Razor escapes notes and renders supplier/product summary and history");
        }
        if(serve){Console.WriteLine("Search fixture PID="+Environment.ProcessId+" URL="+app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()+"/search");using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(15));try{await app.WaitForShutdownAsync(timeout.Token);}catch(OperationCanceledException){}}
        await app.StopAsync();
    }
}
sealed class MemoryTempData:ITempDataProvider{public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();public void SaveTempData(HttpContext context,IDictionary<string,object> values){}}
