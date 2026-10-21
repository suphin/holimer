using System.Net;
using System.Security.Claims;
using Dapper;
using Ekomers.Data;
using Ekomers.Data.Services.Reports;
using Ekomers.Models.ViewModels.Raporlar;
using Ekomers.Web.Controllers;
using Ekomers.Web.Infrastructure.Auth;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

static void Check(bool value,string message) { if(!value) throw new Exception("FAIL: "+message);Console.WriteLine("PASS "+message); }
static async Task Reject<T>(Func<Task> action,string message) where T:Exception { try{await action();}catch(T){Console.WriteLine("PASS "+message);return;}throw new Exception("FAIL: "+message); }
var prefix="CodexCariStatementTest_";var name=prefix+Guid.NewGuid().ToString("N");
var cs=new SqlConnectionStringBuilder { DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog=name,IntegratedSecurity=true,TrustServerCertificate=true }.ConnectionString;
var created=false;var begin=new DateTime(2026,10,1);var end=new DateTime(2026,10,31);
try {
    await using(var master=new SqlConnection(new SqlConnectionStringBuilder(cs){InitialCatalog="master"}.ConnectionString)){await master.OpenAsync();await master.ExecuteAsync($"CREATE DATABASE [{name}]");created=true;}
    await using var fixture=new SqlConnection(cs);await fixture.OpenAsync();
    await fixture.ExecuteAsync("""
        CREATE TABLE dbo.L_CAPIFIRM(NR int PRIMARY KEY,NAME nvarchar(250),LOCALCTYP int);
        CREATE TABLE dbo.L_CAPIPERIOD(FIRMNR int,NR int,BEGDATE datetime,ENDDATE datetime);
        INSERT dbo.L_CAPIFIRM VALUES(100,N'Firma A <script>',160),(101,N'Firma B',1);
        INSERT dbo.L_CAPIPERIOD VALUES(100,1,'20250101','20261231'),(100,2,'20270101','20271231'),(101,1,'20250101','20261231');
        """);
    foreach(var firm in new[]{100,101}) await fixture.ExecuteAsync(Schema(firm));
    await fixture.ExecuteAsync("""
        INSERT dbo.LG_100_CLCARD(LOGICALREF,CODE,DEFINITION_,ACTIVE,TAXNR) VALUES(10,N'120.01',N'Cari <script>alert(1)</script>',0,N'123'),(20,N'320.01',N'Karşı cari',1,N'456'),(30,N'ZERO',N'Hareketsiz',0,N''),(40,N'329.01',N'Alacak bakiye',0,N'789');
        INSERT dbo.LG_101_CLCARD(LOGICALREF,CODE,DEFINITION_,ACTIVE) VALUES(10,N'OTHER',N'Başka firma carisi',0);
        INSERT dbo.LG_100_01_CLFLINE(LOGICALREF,CLIENTREF,SOURCEFREF,DATE_,MODULENR,TRCODE,SIGN,AMOUNT) VALUES
          (1,10,0,'20250101',5,14,0,100),(2,10,0,'20260901',5,14,1,20),(3001,10,700,'20261002',4,38,0,120),
          (3002,10,880,'20261003',7,20,1,50),(3003,10,620,'20261004',10,1,1,10),(3004,10,500,'20261005',5,5,1,30),
          (3005,20,500,'20261005',5,5,0,30),(3006,10,600,'20261006',6,61,1,25),(3007,10,1234,'20261007',61,3,0,3),
          (3008,10,700,'20261008',4,38,0,999),(3009,10,701,'20261009',4,38,0,7),(4001,40,0,'20261001',5,14,1,100);
        UPDATE dbo.LG_100_01_CLFLINE SET CANCELLED=1 WHERE LOGICALREF=3008;
        UPDATE dbo.LG_100_01_CLFLINE SET TRCURR=20,TRNET=3,TRRATE=40 WHERE LOGICALREF=3001;
        INSERT dbo.LG_101_01_CLFLINE(LOGICALREF,CLIENTREF,SOURCEFREF,DATE_,MODULENR,TRCODE,SIGN,AMOUNT) VALUES(3001,10,700,'20261002',4,38,0,9999);
        INSERT dbo.LG_100_02_CLFLINE(LOGICALREF,CLIENTREF,SOURCEFREF,DATE_,MODULENR,TRCODE,SIGN,AMOUNT) VALUES(3001,10,0,'20270101',5,14,0,8888);
        INSERT dbo.LG_100_01_INVOICE(LOGICALREF,CLIENTREF,FICHENO,DATE_,NETTOTAL,TOTALVAT,TOTALDISCOUNTS) VALUES(700,10,N'INV-A','20261002',120,20,5),(701,20,N'WRONG CLIENT INVOICE','20261002',7000,0,0);
        INSERT dbo.LG_101_01_INVOICE(LOGICALREF,CLIENTREF,FICHENO,DATE_,NETTOTAL,TOTALVAT,TOTALDISCOUNTS) VALUES(700,10,N'INV-B','20261002',9999,0,0);
        UPDATE dbo.LG_100_01_INVOICE SET GUID=N'11111111-1111-1111-1111-111111111111',EINVOICE=1 WHERE LOGICALREF=700;
        UPDATE dbo.LG_101_01_INVOICE SET GUID=N'22222222-2222-2222-2222-222222222222',EINVOICE=1 WHERE LOGICALREF=700;
        INSERT dbo.LG_100_ITEMS(LOGICALREF,CODE,NAME) VALUES(1,N'MAT',N'Malzeme'),(2,N'WRONG',N'Hizmet yerine gelmemeli');
        INSERT dbo.LG_100_SRVCARD(LOGICALREF,CODE,DEFINITION_) VALUES(2,N'SERVICE',N'Hizmet');
        INSERT dbo.LG_100_UNITSETL(LOGICALREF,CODE) VALUES(1,N'ADET');
        INSERT dbo.LG_100_01_STLINE(LOGICALREF,INVOICEREF,INVOICELNNO,STOCKREF,LINETYPE,AMOUNT,PRICE,LINENET,VAT,VATAMNT,UOMREF) VALUES(11,700,1,1,0,2,50,100,20,20,1),(12,700,2,2,4,1,5,5,0,0,1),(13,700,3,0,2,0,0,-5,0,0,0),(14,700,4,1,0,1,999,999,0,0,1);
        UPDATE dbo.LG_100_01_STLINE SET CANCELLED=1 WHERE LOGICALREF=14;
        INSERT dbo.LG_101_ITEMS(LOGICALREF,CODE,NAME) VALUES(1,N'OTHER MAT',N'Başka firma malzemesi');
        INSERT dbo.LG_101_01_STLINE(LOGICALREF,INVOICEREF,INVOICELNNO,STOCKREF,LINETYPE,AMOUNT,PRICE,LINENET,VAT,VATAMNT) VALUES(11,700,1,1,0,1,9999,9999,0,0);
        INSERT dbo.LG_100_01_BNFICHE(LOGICALREF,FICHENO,DATE_) VALUES(990,N'BANK-HEADER','20261003'),(880,N'WRONG SAME REF','20261003');
        INSERT dbo.LG_100_BANKACC(LOGICALREF,CODE,DEFINITION_,BANKREF) VALUES(1,N'BANK-ACC',N'Hesap',1);
        INSERT dbo.LG_100_BNCARD(LOGICALREF,DEFINITION_) VALUES(1,N'Banka');
        INSERT dbo.LG_100_01_BNFLINE(LOGICALREF,SOURCEFREF,CLIENTREF,BNACCREF,LINENR,AMOUNT,SIGN,TRCURR,TRNET,TRRATE) VALUES(880,990,10,1,1,50,0,0,0,1),(881,990,20,1,2,777,0,0,0,1);
        INSERT dbo.LG_100_01_CLFICHE(LOGICALREF,FICHENO,DATE_) VALUES(500,N'CLIENT-SLIP','20261005');
        INSERT dbo.LG_100_01_KSLINES(LOGICALREF,TRANSREF,CARDREF,FICHENO,DATE_,AMOUNT,SIGN,TRCURR,TRNET,TRRATE) VALUES(620,3003,42,N'CASH','20261004',10,0,0,0,1);
        INSERT dbo.LG_100_01_CSROLL(LOGICALREF,CARDREF,ROLLNO,DATE_,TOTAL) VALUES(600,10,N'CHEQUE','20261006',25);
        INSERT dbo.LG_100_01_CSCARD(LOGICALREF,PORTFOYNO,SERINO,BANKNAME,OWING,DUEDATE,AMOUNT,TRCURR,TRNET,TRRATE) VALUES(30,N'PORT-30',N'SER-30',N'Banka',N'Borçlu','20261201',25,0,0,1);
        INSERT dbo.LG_100_01_CSTRANS(LOGICALREF,CSREF,ROLLREF,LINENO_,STATUS) VALUES(31,30,600,1,1);
        """);
    for(var i=1;i<=52;i++) await fixture.ExecuteAsync("INSERT dbo.LG_100_01_CLFLINE(LOGICALREF,CLIENTREF,DATE_,MODULENR,TRCODE,SIGN,AMOUNT,FTIME) VALUES(@id,10,'20261001',99,1,0,1,0)",new{id=2000+i});
    await using var logo=new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(cs).Options);
    var service=new LogoCustomerStatementService(logo);var companies=await service.CompaniesAsync(default);
    Check(companies.Count==2&&companies[0].Name=="Firma A <script>","Company list comes from Logo firm master, not portal aliases");
    var scope=await service.ScopeAsync(100,1,begin,end,default);
    await Reject<ArgumentException>(()=>service.ScopeAsync(-1,1,begin,end,default),"Identifier bounds reject invalid company");
    await Reject<KeyNotFoundException>(()=>service.ScopeAsync(101,2,begin,end,default),"Period is validated within selected company");
    await Reject<ArgumentException>(()=>service.ScopeAsync(100,1,new DateTime(2024,1,1),end,default),"Outside-period dates rejected");
    var accounts=await service.AccountsAsync(scope,"",true,false,1,default);
    var current=accounts.Rows.Single(x=>x.ClientRef==10);
    Check(accounts.TotalCount==4&&current.OpeningBalance==80&&current.Debit==182&&current.Credit==115&&current.ClosingBalance==147,"Opening/period movement/closing totals preserve signs and exclude cancellations");
    Check(accounts.DebitBalance==177&&accounts.CreditBalance==100,"Debit and credit balance buckets are not incorrectly netted together");
    Check((await service.AccountsAsync(scope,"",false,false,1,default)).TotalCount==3,"Passive-account filter");
    Check((await service.AccountsAsync(scope,"",true,true,1,default)).TotalCount==3,"Zero-balance filter");
    Check((await service.AccountsAsync(scope,"' OR 1=1 --",true,false,1,default)).TotalCount==0,"Search is parameterized");
    Check((await service.AccountsAsync(scope,"%",true,false,1,default)).TotalCount==0,"Search wildcard is a literal, not SQL LIKE expansion");
    var emptyPage=await service.AccountsAsync(scope,"",true,false,200,default);
    Check(emptyPage.Rows.Count==0&&emptyPage.TotalCount==4&&emptyPage.DebitBalance==177,"Out-of-range customer page retains full filtered totals");
    var first=await service.StatementAsync(scope,10,1,default);var second=await service.StatementAsync(scope,10,2,default);
    Check(first.TotalCount==59&&first.Rows.Count==50&&first.Rows[^1].Balance==130&&second.Rows[0].Balance==131&&second.ClosingBalance==147,"Running balance spans pages and same-date tie order is stable");
    Check(second.Rows.Single(x=>x.MovementRef==3001).CurrencyAmount==3&&second.Rows.Single(x=>x.MovementRef==3001).Debit==120,"Transaction currency is separate from local amounts");
    var noMovement=await service.StatementAsync(await service.ScopeAsync(100,1,new DateTime(2026,11,1),new DateTime(2026,11,2),default),10,1,default);
    Check(noMovement.Rows.Count==0&&noMovement.OpeningBalance==147&&noMovement.ClosingBalance==147,"Opening balance survives a no-movement period");
    var invoice=await service.DocumentAsync(scope,10,3001,1,default);
    Check(invoice.Number=="INV-A"&&invoice.TotalCount==3&&invoice.Rows.Count==3&&invoice.Rows.Single(x=>x.Ref==12).Code=="SERVICE"&&invoice.Rows.Any(x=>x.LineType==2),"Invoice material/service/discount rows and cancellation filtering");
    var bank=await service.DocumentAsync(scope,10,3002,1,default);
    Check(bank.Number=="BANK-HEADER"&&bank.Rows.Count==1&&bank.Rows[0].Net==50,"Bank movement resolves line to header and does not include another customer's lines");
    var cash=await service.DocumentAsync(scope,10,3003,1,default);Check(cash.Number=="CASH"&&cash.Rows.Single().Code=="42","Cash source validated through reverse movement reference");
    var clientSlip=await service.DocumentAsync(scope,10,3004,1,default);Check(clientSlip.Number=="CLIENT-SLIP"&&clientSlip.Rows.Count==2,"Client transfer/debit-credit slip includes its linked counterpart");
    var cheque=await service.DocumentAsync(scope,10,3006,1,default);Check(cheque.Number=="CHEQUE"&&cheque.Rows.Single().SerialNumber=="SER-30"&&cheque.Rows.Single().DueDate==new DateTime(2026,12,1),"Cheque roll, portfolio, serial and due date");
    var unknown=await service.DocumentAsync(scope,10,3007,1,default);Check(unknown.LineMode=="generic"&&unknown.Notice!=null,"Unknown module falls back to real movement, never guessed document");
    var mismatched=await service.DocumentAsync(scope,10,3009,1,default);Check(mismatched.LineMode=="generic"&&!mismatched.Number.Contains("WRONG CLIENT"),"Mismatched invoice customer does not leak another document");
    await Reject<KeyNotFoundException>(()=>service.DocumentAsync(scope,20,3001,1,default),"Movement is scoped to the requested customer");
    await Reject<KeyNotFoundException>(()=>service.DocumentAsync(scope,10,3008,1,default),"Cancelled movement cannot be drilled into");
    var otherScope=await service.ScopeAsync(101,1,begin,end,default);
    var otherDoc=await service.DocumentAsync(otherScope,10,3001,1,default);
    Check(otherDoc.Number=="INV-B"&&otherDoc.Rows.Single().Code=="OTHER MAT","Identical refs in another company cannot cross company tables");
    var nextScope=await service.ScopeAsync(100,2,new DateTime(2027,1,1),new DateTime(2027,1,31),default);
    Check((await service.StatementAsync(nextScope,10,1,default)).ClosingBalance==8888,"Period tables remain isolated");

    var pdfClient=new FakeInvoicePdfClient();
    var controller=new LogoCariEkstreController(service,NullLogger<LogoCariEkstreController>.Instance,pdfClient);
    var http=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Role,"Rapor")],"Fixture"))};
    controller.ControllerContext=new ControllerContext{HttpContext=http};
    var action=new ActionContext(http,new RouteData(),new ActionDescriptor(),new ModelStateDictionary());
    Check(!ReportPermissions.CanAccess(http.User,ReportPermissions.LogoCariEkstre),"Rapor role without page claim is denied");
    http.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim("Rapor","BorcAlacak")],"Fixture"));
    Check(!ReportPermissions.CanAccess(http.User,ReportPermissions.LogoCariEkstre),"BorcAlacak claim does not grant separate company statement page");
    http.User=new ClaimsPrincipal(new ClaimsIdentity([new Claim("Rapor","LogoCariEkstre")],"Fixture"));
    Check(ReportPermissions.CanAccess(http.User,ReportPermissions.LogoCariEkstre),"Dedicated statement claim authorizes report access without requiring a broad role");
    var filter=new ActionExecutingContext(action,[],new Dictionary<string,object?>(),controller);var called=false;
    await controller.OnActionExecutionAsync(filter,()=>{called=true;return Task.FromResult(new ActionExecutedContext(action,[],controller));});Check(called,"Existing report claim authorizes report access");
    var result=await controller.Ekstre(100,1,10,begin,end);Check(result is PartialViewResult&&http.Response.Headers["X-Logo-Cari-Partial"]=="Ekstre","Statement endpoint marks authenticated partial response");
    Check(await controller.BelgeDetay(100,1,20,3001,begin,end) is NotFoundResult,"Controller rejects wrong-customer document");
    Check(await controller.Ekstre(100,1,10,begin,end,0) is BadRequestResult,"Bad pagination rejected before querying");
    Check(invoice.CanOpenInvoiceImage&&invoice.EInvoiceGuid=="11111111-1111-1111-1111-111111111111"&&otherDoc.EInvoiceGuid=="22222222-2222-2222-2222-222222222222","e-Fatura UUID is read from the exact company's linked invoice");
    var image=await controller.FaturaGorseli(100,1,10,3001,begin,end);
    Check(image is FileContentResult pdf&&pdf.ContentType=="application/pdf"&&pdf.FileContents.SequenceEqual(FakeInvoicePdfClient.Pdf)&&pdfClient.Calls.Single()==invoice.EInvoiceGuid,"Statement invoice image reuses eLogo PDF client with server-resolved UUID");
    Check(http.Response.Headers.ContentDisposition.ToString().StartsWith("inline;")&&http.Response.Headers.CacheControl.ToString().Contains("no-store")&&http.Response.Headers["X-Content-Type-Options"]=="nosniff","PDF is inline, non-cacheable and sent with safe content headers");
    await controller.FaturaGorseli(101,1,10,3001,begin,end);
    Check(pdfClient.Calls.Last()==otherDoc.EInvoiceGuid,"Same movement/invoice refs in another company resolve that company's UUID, not firm 100");
    var callsBefore=pdfClient.Calls.Count;
    foreach(var target in new[]{(client:20,movement:3001),(client:10,movement:3002),(client:10,movement:3009),(client:10,movement:3008),(client:10,movement:3007)})
        Check(await controller.FaturaGorseli(100,1,target.client,target.movement,begin,end) is ContentResult { StatusCode:404 },"Missing/cancelled/wrong-client/non-invoice image is rejected before eLogo call");
    Check(await controller.FaturaGorseli(100,1,10,0,begin,end) is ContentResult {StatusCode:400}&&pdfClient.Calls.Count==callsBefore,"Invalid image scope never invokes eLogo");
    foreach(var uuid in new[]{"","not-a-guid","00000000-0000-0000-0000-000000000000"}) {
        await fixture.ExecuteAsync("UPDATE dbo.LG_100_01_INVOICE SET GUID=@uuid WHERE LOGICALREF=700",new {uuid});
        Check(!(await service.DocumentAsync(scope,10,3001,1,default)).CanOpenInvoiceImage&&await controller.FaturaGorseli(100,1,10,3001,begin,end) is ContentResult {StatusCode:404},"Missing/invalid/empty UUID disables and rejects invoice image");
    }
    await fixture.ExecuteAsync("UPDATE dbo.LG_100_01_INVOICE SET GUID=@uuid,EINVOICE=0 WHERE LOGICALREF=700",new {uuid=invoice.EInvoiceGuid});
    Check(await controller.FaturaGorseli(100,1,10,3001,begin,end) is ContentResult {StatusCode:404},"Non-e-Fatura is not sent to the e-Fatura PDF endpoint");
    await fixture.ExecuteAsync("UPDATE dbo.LG_100_01_INVOICE SET EINVOICE=1,CANCELLED=1 WHERE LOGICALREF=700");
    Check(await controller.FaturaGorseli(100,1,10,3001,begin,end) is ContentResult {StatusCode:404}&&pdfClient.Calls.Count==callsBefore,"Cancelled invoice is blocked even if its customer movement is not cancelled");
    await fixture.ExecuteAsync("UPDATE dbo.LG_100_01_INVOICE SET CANCELLED=0 WHERE LOGICALREF=700");
    pdfClient.Failure=new HttpRequestException("sensitive-service-detail");
    Check(await controller.FaturaGorseli(100,1,10,3001,begin,end) is ContentResult {StatusCode:502,Content:var errorText}&&!errorText.Contains("sensitive-service-detail"),"eLogo failures show a friendly error without exposing service details");
    pdfClient.Failure=new OperationCanceledException();
    await Reject<OperationCanceledException>(()=>controller.FaturaGorseli(100,1,10,3001,begin,end),"Image request cancellation is not swallowed");pdfClient.Failure=null;
    var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ApplicationName=typeof(LogoCariEkstreController).Assembly.GetName().Name,EnvironmentName="Development",Args=[]});
    builder.Logging.ClearProviders();builder.Services.AddControllersWithViews().AddApplicationPart(typeof(LogoCariEkstreController).Assembly);builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    await using var host=builder.Build();host.MapDefaultControllerRoute();host.Urls.Add("http://127.0.0.1:0");await host.StartAsync();
    foreach(var view in new[]{("Index",(object)accounts),("_Ekstre",(object)first),("_Belge",(object)invoice),("_Belge",(object)bank),("_Belge",(object)cheque),("_Belge",(object)unknown)}){
        var html=await Render(host,view.Item1,view.Item2);Check(!html.Contains("<script>alert(1)</script>")&&html.Contains("&lt;script&gt;"),"Compiled/encoded Razor "+view.Item1);
    }
    var statementHtml=WebUtility.HtmlDecode(await Render(host,"_Ekstre",first));Check(statementHtml.Contains("clientRef=10")&&statementHtml.Contains("firmNumber=100")&&statementHtml.Contains("data-cs-document"),"Drilldown links preserve company/period/customer scope");
    var imageHtml=WebUtility.HtmlDecode(await Render(host,"_Belge",invoice));
    Check(imageHtml.Contains("/LogoCariEkstre/FaturaGorseli?")&&imageHtml.Contains("firmNumber=100")&&imageHtml.Contains("periodNumber=1")&&imageHtml.Contains("clientRef=10")&&imageHtml.Contains("movementRef=3001")&&imageHtml.Contains("target=\"_blank\"")&&!imageHtml.Contains(invoice.EInvoiceGuid),"Image button opens a new tab with scoped refs, never an arbitrary browser-supplied UUID");
    Check(!(await Render(host,"_Belge",bank)).Contains("/FaturaGorseli?"),"No image button on bank documents");
    invoice.EInvoiceGuid="";
    var noImageHtml=WebUtility.HtmlDecode(await Render(host,"_Belge",invoice));
    Check(!noImageHtml.Contains("/FaturaGorseli?")&&noImageHtml.Contains("ETTN/UUID"),"Invoice without UUID explains why its image is unavailable");
    await host.StopAsync();
    Console.WriteLine("All synthetic SQL/Razor/controller customer statement tests passed; no live configuration was loaded.");
} finally {
    if(created&&name.StartsWith(prefix)&&Guid.TryParseExact(name[prefix.Length..],"N",out _)){SqlConnection.ClearAllPools();await using var cleanup=new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(cs).Options);await cleanup.Database.EnsureDeletedAsync();Console.WriteLine("Removed isolated test database: "+name);}
}

static string Schema(int firm) {
    var f=$"LG_{firm:000}";var p=f+"_01";
    return $"""
    CREATE TABLE dbo.{f}_CLCARD(LOGICALREF int PRIMARY KEY,CODE nvarchar(100),DEFINITION_ nvarchar(250),ACTIVE int,TAXNR nvarchar(50),TAXOFFICE nvarchar(100),ADDR1 nvarchar(250),ADDR2 nvarchar(250),TOWN nvarchar(100),CITY nvarchar(100),TELNRS1 nvarchar(50),EMAILADDR nvarchar(250));
    CREATE TABLE dbo.{p}_CLFLINE(LOGICALREF int PRIMARY KEY,CLIENTREF int,SOURCEFREF int DEFAULT 0,DATE_ datetime,FTIME int DEFAULT 0,MODULENR int,TRCODE int,TRANNO nvarchar(100),DOCODE nvarchar(100),LINEEXP nvarchar(500),SIGN int,AMOUNT decimal(28,6),TRCURR int DEFAULT 0,TRNET decimal(28,6) DEFAULT 0,TRRATE decimal(28,8) DEFAULT 1,CANCELLED int DEFAULT 0,LINENR int DEFAULT 0);
    SELECT TOP(0) * INTO dbo.{f}_02_CLFLINE FROM dbo.{p}_CLFLINE;
    CREATE TABLE dbo.{p}_INVOICE(LOGICALREF int PRIMARY KEY,CLIENTREF int,FICHENO nvarchar(100),DOCODE nvarchar(100),DATE_ datetime,NETTOTAL decimal(28,6),TOTALVAT decimal(28,6),TOTALDISCOUNTS decimal(28,6),CANCELLED int DEFAULT 0,GENEXP1 nvarchar(100),GENEXP2 nvarchar(100),GENEXP3 nvarchar(100),GENEXP4 nvarchar(100),GENEXP5 nvarchar(100),GENEXP6 nvarchar(100),GUID nvarchar(100),EINVOICE int DEFAULT 0);
    CREATE TABLE dbo.{p}_STLINE(LOGICALREF int PRIMARY KEY,INVOICEREF int,INVOICELNNO int,STOCKREF int,LINETYPE int,AMOUNT decimal(28,6),PRICE decimal(28,6),LINENET decimal(28,6),VAT decimal(28,6),VATAMNT decimal(28,6),VATINC int DEFAULT 0,UOMREF int,CANCELLED int DEFAULT 0,LINEEXP nvarchar(500));
    CREATE TABLE dbo.{f}_ITEMS(LOGICALREF int PRIMARY KEY,CODE nvarchar(100),NAME nvarchar(250));
    CREATE TABLE dbo.{f}_SRVCARD(LOGICALREF int PRIMARY KEY,CODE nvarchar(100),DEFINITION_ nvarchar(250));
    CREATE TABLE dbo.{f}_UNITSETL(LOGICALREF int PRIMARY KEY,CODE nvarchar(100));
    CREATE TABLE dbo.{p}_BNFICHE(LOGICALREF int PRIMARY KEY,FICHENO nvarchar(100),DATE_ datetime,CANCELLED int DEFAULT 0,GENEXP1 nvarchar(100),GENEXP2 nvarchar(100),GENEXP3 nvarchar(100),GENEXP4 nvarchar(100),GENEXP5 nvarchar(100),GENEXP6 nvarchar(100));
    CREATE TABLE dbo.{p}_BNFLINE(LOGICALREF int PRIMARY KEY,SOURCEFREF int,CLIENTREF int,BNACCREF int,LINENR int,AMOUNT decimal(28,6),SIGN int,TRCURR int,TRNET decimal(28,6),TRRATE decimal(28,8),TRANNO nvarchar(100),DOCODE nvarchar(100),LINEEXP nvarchar(500),CANCELLED int DEFAULT 0);
    CREATE TABLE dbo.{f}_BANKACC(LOGICALREF int PRIMARY KEY,CODE nvarchar(100),DEFINITION_ nvarchar(250),BANKREF int);
    CREATE TABLE dbo.{f}_BNCARD(LOGICALREF int PRIMARY KEY,DEFINITION_ nvarchar(250));
    CREATE TABLE dbo.{p}_CLFICHE(LOGICALREF int PRIMARY KEY,FICHENO nvarchar(100),DOCODE nvarchar(100),DATE_ datetime,CANCELLED int DEFAULT 0,GENEXP1 nvarchar(100),GENEXP2 nvarchar(100),GENEXP3 nvarchar(100),GENEXP4 nvarchar(100),GENEXP5 nvarchar(100),GENEXP6 nvarchar(100));
    CREATE TABLE dbo.{p}_KSLINES(LOGICALREF int PRIMARY KEY,TRANSREF int,CARDREF int,FICHENO nvarchar(100),DOCODE nvarchar(100),DATE_ datetime,AMOUNT decimal(28,6),SIGN int,TRCURR int,TRNET decimal(28,6),TRRATE decimal(28,8),CUSTTITLE nvarchar(250),LINEEXP nvarchar(500),CANCELLED int DEFAULT 0);
    CREATE TABLE dbo.{p}_CSROLL(LOGICALREF int PRIMARY KEY,CARDREF int,ROLLNO nvarchar(100),DOCODE nvarchar(100),DATE_ datetime,TOTAL decimal(28,6),CANCELLED int DEFAULT 0,GENEXP1 nvarchar(100),GENEXP2 nvarchar(100),GENEXP3 nvarchar(100),GENEXP4 nvarchar(100),GENEXP5 nvarchar(100),GENEXP6 nvarchar(100));
    CREATE TABLE dbo.{p}_CSCARD(LOGICALREF int PRIMARY KEY,PORTFOYNO nvarchar(100),SERINO nvarchar(100),BANKNAME nvarchar(250),OWING nvarchar(250),DUEDATE datetime,AMOUNT decimal(28,6),TRCURR int,TRNET decimal(28,6),TRRATE decimal(28,8),CANCELLED int DEFAULT 0);
    CREATE TABLE dbo.{p}_CSTRANS(LOGICALREF int PRIMARY KEY,CSREF int,ROLLREF int,LINENO_ int,STATUS int,CANCELLED int DEFAULT 0);
    """;
}
static async Task<string> Render(WebApplication host,string name,object model) {
    await using var scope=host.Services.CreateAsyncScope();var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider};http.Request.Scheme="http";http.Request.Host=new HostString("localhost");http.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(),"Render"));
    var route=new RouteData();route.Values["controller"]="LogoCariEkstre";route.Values["action"]="Index";var action=new ActionContext(http,route,new ActionDescriptor(),new ModelStateDictionary());
    var view=scope.ServiceProvider.GetRequiredService<IRazorViewEngine>().GetView(null,"/Views/LogoCariEkstre/"+name+".cshtml",false);Check(view.Success,"View found "+name);using var writer=new StringWriter();
    await view.View.RenderAsync(new ViewContext(action,view.View,new ViewDataDictionary(new EmptyModelMetadataProvider(),action.ModelState){Model=model},new TempDataDictionary(http,new MemoryTempData()),writer,new HtmlHelperOptions()));return writer.ToString();
}
sealed class MemoryTempData:ITempDataProvider { public IDictionary<string,object> LoadTempData(HttpContext c)=>new Dictionary<string,object>();public void SaveTempData(HttpContext c,IDictionary<string,object> values){} }
sealed class FakeInvoicePdfClient:IELogoPostboxClient {
    public static readonly byte[] Pdf=System.Text.Encoding.ASCII.GetBytes("%PDF-1.4\nfixture");
    public List<string> Calls {get;}=[]; public Exception? Failure {get;set;}
    public Task<ELogoDocumentFile> GetIncomingInvoicePdfAsync(string uuid,CancellationToken ct=default) {
        Calls.Add(uuid);return Failure==null?Task.FromResult(new ELogoDocumentFile(Pdf,"fatura.pdf","application/pdf")):Task.FromException<ELogoDocumentFile>(Failure);
    }
}
