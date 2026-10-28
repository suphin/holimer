using System.Net;
using Dapper;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.ViewModels.Purchasing;
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

static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);Console.WriteLine("PASS "+label);}
var database="CodexPurchaseCompanyTest_"+Guid.NewGuid().ToString("N");
var builderCs=new SqlConnectionStringBuilder{DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog="master",IntegratedSecurity=true,TrustServerCertificate=true};
await using var db=new SqlConnection(builderCs.ConnectionString);await db.OpenAsync();var created=false;
try {
    await db.ExecuteAsync($"CREATE DATABASE [{database}]");created=true;db.ChangeDatabase(database);builderCs.InitialCatalog=database;
    await db.ExecuteAsync("""
        CREATE TABLE dbo.L_CAPIFIRM(NR int,NAME nvarchar(200));
        INSERT dbo.L_CAPIFIRM VALUES(100,N'Gerçek Firma 100'),(101,N'View Olmayan Firma'),(103,N'Firma 103 <script>test</script>');
        INSERT dbo.L_CAPIFIRM VALUES(104,N'Portalda Kullanılmayan'),(105,N'Pasif Portal Şirketi'),(106,N'Silinmiş Portal Şirketi');
        CREATE TABLE dbo.Sirketler(ID int PRIMARY KEY,SirketAdi nvarchar(250),SirketKisaAdi nvarchar(250),LogoTigerSirketKodu nvarchar(100),IsDelete bit NULL,IsActive bit NULL);
        INSERT dbo.Sirketler VALUES(1,N'Holimer Tam Unvan',N'Holimer',' 0100 ',0,1),(2,N'View Olmayan Portal Şirketi',NULL,'101',0,NULL),
            (3,N'Firma 103 Portal <script>test</script>',N'  ','103',0,1),(4,N'Yinelenen Firma',N'holimer','100',0,1),
            (5,N'Pasif',NULL,'105',0,0),(6,N'Silinmiş',NULL,'106',1,1),(7,N'Geçersiz',NULL,'abc',0,1),(8,N'Boş',NULL,' ',0,1),
            (9,N'Logo kaydı olmayan',NULL,'107',0,1),(10,N'Sınır dışı',NULL,'1000',0,1);
        CREATE TABLE dbo.L_CAPIPERIOD(FIRMNR int,NR int,BEGDATE datetime,ENDDATE datetime);
        INSERT dbo.L_CAPIPERIOD VALUES(100,1,'20250101','20261231'),(103,1,'20250101','20261231'),(103,2,'20270101','20271231');
        CREATE TABLE dbo.LG_100_CLCARD(LOGICALREF int,CODE nvarchar(100));
        INSERT dbo.LG_100_CLCARD VALUES(1,'C1'),(2,'OTHER');
        SELECT * INTO dbo.LG_103_CLCARD FROM dbo.LG_100_CLCARD;
        CREATE TABLE dbo.LG_100_01_INVOICE(LOGICALREF int,FICHENO nvarchar(100),DATE_ datetime,CLIENTREF int,CANCELLED int,GUID nvarchar(100),EINVOICE int,ESTATUS int);
        INSERT dbo.LG_100_01_INVOICE VALUES(1,'SAME-NO','20261001',1,0,'11111111-1111-1111-1111-111111111111',1,1);
        SELECT TOP(0) * INTO dbo.LG_103_01_INVOICE FROM dbo.LG_100_01_INVOICE;
        SELECT TOP(0) * INTO dbo.LG_103_02_INVOICE FROM dbo.LG_100_01_INVOICE;
        INSERT dbo.LG_103_01_INVOICE VALUES(1,'SAME-NO','20261001',1,0,'33333333-3333-3333-3333-333333333333',1,3),
            (999,'SAME-NO','20261001',2,0,'99999999-9999-9999-9999-999999999999',1,99),
            (1000,'SAME-NO','20261001',1,1,'00000000-0000-0000-0000-000000000000',1,0);
        INSERT dbo.LG_103_02_INVOICE VALUES(1,'SAME-NO','20271001',1,0,'22222222-2222-2222-2222-222222222222',1,2);
        CREATE TABLE dbo.InvoiceFixture(Firm int,TUR nvarchar(100),[Fatura Türü] nvarchar(100),[Fatura İptal Durumu] nvarchar(100),Tarihi datetime,
            [Fatura Numarası] nvarchar(100),[Fatura Belge Numarası] nvarchar(100),[Cari Hesap Kodu] nvarchar(100),[Cari Hesap Unvanı] nvarchar(100),
            [Hizmet Kodu] nvarchar(100),[Hizmet Açıklaması] nvarchar(100),[SatIr Matrahı] decimal(18,6),[Fatura NET Toplam] decimal(18,6),
            [Fatura GİB Durumu] nvarchar(100),[Doküman İzleme Numarası] nvarchar(100),[Birim] nvarchar(20),[İşlem Döviz Türü] nvarchar(20),
            [Miktar] decimal(18,6),[Birim Fiyat] decimal(18,6),[kur] decimal(18,6),[Kdv] decimal(18,6),[Toplamı] decimal(18,6),
            [Ödeme Planı] nvarchar(100),[İrsaliye Numarası] nvarchar(100),[İrsaliye Tarihi] nvarchar(100),[SİPARİŞ Numarası] nvarchar(100));
        INSERT dbo.InvoiceFixture VALUES
            (100,N'Satınalma',N'Satınalma Faturası',N'İptal Edilmemiş','20261001','SAME-NO','DOC100','C1',N'Tedarikçi 100','P1',N'Ürün 100',111,111,N'Onay','TRACK100','KG','TL',1,111,1,22.2,133.2,N'Peşin','IR100','01.10.2026','ORD100'),
            (103,N'Satınalma',N'Satınalma Faturası',N'İptal Edilmemiş','20261001','SAME-NO','DOC103','C1',N'Tedarikçi 103','P1',N'Ürün 103',333,333,N'Onay','TRACK103','KG','TL',1,333,1,66.6,399.6,N'Peşin','IR103','01.10.2026','ORD103'),
            (103,N'Satınalma',N'Satınalma Faturası',N'İptal Edilmemiş','20271001','SAME-NO','DOC103-02','C1',N'Tedarikçi 103','P1',N'Ürün 103',222,222,N'Onay','TRACK103','KG','TL',1,222,1,44.4,266.4,N'Peşin','IR103','01.10.2027','ORD103');
        """);
    await db.ExecuteAsync("CREATE VIEW dbo.fatura_dokumu_100 AS SELECT * FROM dbo.InvoiceFixture WHERE Firm=100;");
    await db.ExecuteAsync("CREATE VIEW dbo.fatura_dokumu_103 AS SELECT * FROM dbo.InvoiceFixture WHERE Firm=103;");
    await using var logo=new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(builderCs.ConnectionString).Options);
    await using var portal=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(builderCs.ConnectionString).Options);
    var pdf=new FakePdf();var controller=new LogoPurchaseInvoiceController(logo,pdf,new PortalLogoCompanyService(portal)){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};
    var start=new DateTime(2026,10,1);var end=start.AddDays(30);
    async Task<LogoPurchaseInvoiceQueryVM> Query(int firm=100,bool run=true,int page=1,string? product=null)=>
        (LogoPurchaseInvoiceQueryVM)((ViewResult)await controller.Index(product,null,null,start,end,run,page,10,firm)).Model!;
    var initial=await Query(run:false);
    Check(initial.FirmNumber==100 && initial.Rows.Count==0 && initial.Companies.Select(c=>c.Number).SequenceEqual(new[]{100,101,103}),"Only active portal companies with valid existing Logo codes are listed; default 100 and no automatic query");
    Check(initial.Companies.Single(x=>x.Number==100).Name=="Holimer"&&initial.Companies.Single(x=>x.Number==103).Name.StartsWith("Firma 103 Portal"),"Company display names come from portal short/full names and duplicate Logo codes are collapsed");
    Check((await Query(104)).Error!=null&&await controller.FaturaGorseli("SAME-NO",start,"C1",104) is ContentResult&&pdf.Calls.Count==0,"A Logo-only company cannot be requested through a forged query or PDF link");
    Check(!initial.Companies.Single(c=>c.Number==101).HasInvoiceView && initial.Companies.Single(c=>c.Number==103).HasInvoiceView,"View availability is determined per actual Logo firm");
    var first=await Query();var third=await Query(103);
    Check(first.Error==null&&first.Rows.Single().MatchedNetTry==111&&first.Rows.Single().EInvoiceGuid.StartsWith("1111"),"Firm 100 retains existing invoice results");
    Check(third.Error==null&&third.Rows.Single().MatchedNetTry==333&&third.Rows.Single().EInvoiceGuid.StartsWith("3333"),"Same invoice number in firm 103 never reads firm 100");
    Check(third.Rows.Single().EInvoiceStatusCode==3,"Invoice metadata matches supplier and excludes cancelled records, not merely latest invoice ref");
    var detail=(LogoPurchaseInvoiceDetailVM)((PartialViewResult)await controller.Detay("SAME-NO",start,"C1",103)).Model!;
    Check(detail.Error==null&&detail.FirmNumber==103&&detail.Lines.Single().NetLineTry==333&&detail.EInvoiceGuid.StartsWith("3333"),"Detail stays in selected firm's view and invoice tables");
    var nextDate=new DateTime(2027,10,1);
    var next=(LogoPurchaseInvoiceDetailVM)((PartialViewResult)await controller.Detay("SAME-NO",nextDate,"C1",103)).Model!;
    Check(next.Error==null&&next.EInvoiceGuid.StartsWith("2222")&&next.Lines.Single().NetLineTry==222,"e-Invoice metadata follows the invoice date's Logo period, not hardcoded 01");
    var spanning=(LogoPurchaseInvoiceQueryVM)((ViewResult)await controller.Index(null,null,null,start,nextDate,true,1,10,103)).Model!;
    Check(spanning.Error==null&&spanning.Rows.Count==2&&spanning.Rows.Select(r=>r.EInvoiceGuid).Distinct().Count()==2,"Date ranges spanning periods retain both correct invoice identities");
    Check(await controller.FaturaGorseli("SAME-NO",nextDate,"C1",103) is FileContentResult&&pdf.Calls.Single().StartsWith("2222")&&pdf.Firms.Single()==103,"PDF passes selected firm and server-resolved period UUID to eLogo");
    Check((await Query(101)).Error?.Contains("view") == true && (await Query(999)).Error!=null && (await Query(-1)).Error!=null,"Missing view and unknown/out-of-range firms fail closed without falling back to 100");
    var blocked=(LogoPurchaseInvoiceDetailVM)((PartialViewResult)await controller.Detay("SAME-NO",start,"C1",101)).Model!;
    Check(blocked.Error!=null&&blocked.Lines.Count==0,"Forged detail links cannot bypass source availability checks");
    Check(await controller.FaturaGorseli("SAME-NO",start,"C1",101) is ContentResult&&pdf.Calls.Count==1,"Invalid firm source never calls eLogo");
    Check((await Query(103,product:"' OR 1=1--")).Rows.Count==0,"Search remains SQL parameterized");
    controller.ModelState.AddModelError("firmNumber","invalid");Check((await Query(103)).Error!=null,"Malformed model-bound filters cannot silently query the default firm");controller.ModelState.Clear();
    // Populate paging fixture only after identity assertions.
    await db.ExecuteAsync("INSERT dbo.InvoiceFixture SELECT TOP(12) 103,TUR,[Fatura Türü],[Fatura İptal Durumu],Tarihi,CONCAT('PAGE-',ROW_NUMBER() OVER(ORDER BY (SELECT NULL))),[Fatura Belge Numarası],[Cari Hesap Kodu],[Cari Hesap Unvanı],[Hizmet Kodu],[Hizmet Açıklaması],[SatIr Matrahı],[Fatura NET Toplam],[Fatura GİB Durumu],[Doküman İzleme Numarası],Birim,[İşlem Döviz Türü],Miktar,[Birim Fiyat],kur,Kdv,Toplamı,[Ödeme Planı],[İrsaliye Numarası],[İrsaliye Tarihi],[SİPARİŞ Numarası] FROM dbo.InvoiceFixture CROSS JOIN (SELECT TOP(12) object_id FROM sys.all_objects) n WHERE Firm=103 AND Tarihi='20261001';");
    var paged=await Query(103,page:2);Check(paged.Error==null&&paged.Rows.Count==3&&paged.TotalCount==13,"Pagination queries only the selected firm's matching invoices");
    var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ApplicationName=typeof(LogoPurchaseInvoiceController).Assembly.GetName().Name,EnvironmentName="Development",Args=[]});
    builder.Logging.ClearProviders();builder.Services.AddControllersWithViews().AddApplicationPart(typeof(LogoPurchaseInvoiceController).Assembly);builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    await using var host=builder.Build();host.MapDefaultControllerRoute();await host.StartAsync();
    var html=WebUtility.HtmlDecode(await Render(host,"Index",paged));
    Check(html.Contains("name=\"firmNumber\"")&&html.Contains("value=\"103\" selected=\"selected\"")&&html.Contains("fatura view'ı yok"),"Razor renders selected real firm and unavailable view choices");
    Check(html.Contains("firmNumber=103")&&!html.Contains("firmNumber=100"),"Paging, reset and detail links preserve selected firm");
    var raw=await Render(host,"Index",paged);Check(!raw.Contains("<script>test</script>"),"Firm names are HTML-encoded");
    var partial=WebUtility.HtmlDecode(await Render(host,"_Detay",detail));Check(partial.Contains("firmNumber=103")&&partial.Contains("Firma 103"),"Detail displays firm and includes it in PDF link");
    await db.ExecuteAsync("UPDATE dbo.Sirketler SET IsActive=0 WHERE ID IN(1,4)");
    var automatic=(LogoPurchaseInvoiceQueryVM)((ViewResult)await controller.Index(null,null,null,start,end)).Model!;
    Check(automatic.FirmNumber==103&&automatic.Error==null&&!automatic.Companies.Any(x=>x.Number==100),"Default firm uses a configured company with an invoice view when 100 is not active");
    var revoked=(LogoPurchaseInvoiceDetailVM)((PartialViewResult)await controller.Detay("SAME-NO",start,"C1",100)).Model!;
    Check(revoked.Error!=null&&revoked.Lines.Count==0,"Disabling a portal company immediately blocks direct detail links");
    await db.ExecuteAsync("UPDATE dbo.Sirketler SET IsActive=1 WHERE ID IN(1,4)");
    await host.StopAsync();Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.L_CAPIFIRM")==6&&await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Sirketler")==10,"Read-only screen does not mutate portal or Logo company metadata");
} finally {
    if(created){db.ChangeDatabase("master");SqlConnection.ClearAllPools();await db.ExecuteAsync($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];");Console.WriteLine("Removed isolated test database "+database);}
}
static async Task<string> Render(WebApplication host,string name,object model){
    await using var scope=host.Services.CreateAsyncScope();var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};http.Request.Scheme="http";http.Request.Host=new HostString("localhost");http.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(),"Fixture"));
    var route=new RouteData();route.Values["controller"]="LogoPurchaseInvoice";route.Values["action"]="Index";var action=new ActionContext(http,route,new ActionDescriptor(),new ModelStateDictionary());
    var view=scope.ServiceProvider.GetRequiredService<IRazorViewEngine>().GetView(null,"/Views/LogoPurchaseInvoice/"+name+".cshtml",false);Check(view.Success,"Compiled view "+name);
    using var writer=new StringWriter();await view.View.RenderAsync(new ViewContext(action,view.View,new ViewDataDictionary(new EmptyModelMetadataProvider(),action.ModelState){Model=model},new TempDataDictionary(http,new MemoryTempData()),writer,new HtmlHelperOptions()));return writer.ToString();
}
sealed class MemoryTempData:ITempDataProvider{public IDictionary<string,object> LoadTempData(HttpContext c)=>new Dictionary<string,object>();public void SaveTempData(HttpContext c,IDictionary<string,object> values){} }
sealed class FakePdf:IELogoPostboxClient{
    public List<string> Calls {get;}=[];
    public List<int> Firms {get;}=[];
    public Task<ELogoDocumentFile> GetIncomingInvoicePdfAsync(int firmNumber,string uuid,CancellationToken ct=default){Firms.Add(firmNumber);Calls.Add(uuid);return Task.FromResult(new ELogoDocumentFile(System.Text.Encoding.ASCII.GetBytes("%PDF-1.4 fixture"),"fixture.pdf","application/pdf"));}
}
