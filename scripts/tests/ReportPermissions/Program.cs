using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Dapper;
using Ekomers.Data.Migrations;
using Ekomers.Models;
using Ekomers.Models.ViewModels.Profitability;
using Ekomers.Web.Controllers;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Options;

static void Check(bool value,string message) { if(!value) throw new Exception("FAIL "+message);Console.WriteLine("PASS "+message); }
static ClaimsPrincipal User(params Claim[] claims) => new(new ClaimsIdentity(claims,"Fixture"));
var actors=new Dictionary<string,ClaimsPrincipal> {
    ["anonymous"]=new(new ClaimsIdentity()),["authenticated"]=User(),
    ["role"]=User(new Claim(ClaimTypes.Role,"Rapor")),["manager"]=User(new Claim(ClaimTypes.Role,"Yönetici")),
    ["module"]=User(new Claim("Modul","Rapor")),["admin"]=User(new Claim(ClaimTypes.Role,"Admin")),
    ["wrong-type"]=User(new Claim("Authorize","LogoCariEkstre")),["wrong-value"]=User(new Claim("Rapor","logocariekstre"))
};
foreach(var policy in ReportPermissions.Policies) actors[policy]=User(new Claim("Rapor",policy["Rapor.".Length..]));
FixtureAuthentication.Actors=actors;
var builder=WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName=typeof(ReportsController).Assembly.GetName().Name,EnvironmentName="Development",Args=[] });
builder.Logging.ClearProviders();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(ReportsController).Assembly);
// Unrelated purchasing/other module view components are intentionally not queried in report UI tests.
builder.Services.AddTransient<IViewComponentHelper, EmptyOtherModuleMenus>();
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions,FixtureAuthentication>("Fixture",_=>{});
builder.Services.AddAuthorization(ReportPermissions.AddPolicies);
await using var host=builder.Build();
host.UseRouting();host.UseAuthentication();host.UseAuthorization();
// Actual MVC authorization runs, but authorized requests stop BEFORE controllers/services/jobs/DB.
host.Use(async(context,next)=>{ if(context.GetEndpoint()!=null) context.Response.StatusCode=204;else await next(); });
host.MapDefaultControllerRoute();host.Urls.Add("http://127.0.0.1:0");await host.StartAsync();
using var client=new HttpClient { BaseAddress=new Uri(host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()) };
var routes=new List<(string Controller,string Action,string Method,string Policy,bool AdminOnly)>();
void Add(string controller,string action,string policy,string method="GET",bool adminOnly=false)=>routes.Add((controller,action,method,policy,adminOnly));
foreach(var action in new[]{"Index","BorcAlacak","CariHareketler"}) Add("Reports",action,ReportPermissions.BorcAlacak);
foreach(var action in new[]{"Bankalar","Hesaplar","HesaplarEkstre"}) Add("Reports",action,ReportPermissions.Bankalar);
foreach(var action in new[]{"BankaKredileri","KrediOdemePlani"}) Add("Reports",action,ReportPermissions.BankaKredileri);
Add("LogoBankaHareketleri","Index",ReportPermissions.LogoBankaHareketleri);
foreach(var action in new[]{"PlanKaydet","PlanDurum","PlanSil","SimdiGonder"}) Add("LogoBankaHareketleri",action,ReportPermissions.LogoBankaHareketleri,"POST",true);
foreach(var action in new[]{"Index","Ekstre","BelgeDetay","FaturaGorseli"}) Add("LogoCariEkstre",action,ReportPermissions.LogoCariEkstre);
foreach(var action in new[]{"Index","Rapor"}) Add("PivotRaporlar",action,ReportPermissions.PivotRaporlar);
Add("PivotRaporlar","Veri",ReportPermissions.PivotRaporlar,"POST");
foreach(var action in new[]{"Yonetim","Alanlar","Duzenle"}) Add("PivotRaporlar",action,ReportPermissions.PivotRaporlarYonet);
Add("PivotRaporlar","Duzenle",ReportPermissions.PivotRaporlarYonet,"POST");
foreach(var action in new[]{"Index","Receivables","ReceivableDetail","ChannelBalance"}) Add("SatisKarlilik",action,ReportPermissions.SatisKarlilik);
foreach(var action in new[]{"Index","Gecmis"}) Add("UrunMaliyetleri",action,ReportPermissions.UrunMaliyetleri);
foreach(var action in new[]{"Yeni","Toplu"}) { Add("UrunMaliyetleri",action,ReportPermissions.UrunMaliyetleriYonet);Add("UrunMaliyetleri",action,ReportPermissions.UrunMaliyetleriYonet,"POST"); }
Add("UrunMaliyetleri","OnEkler",ReportPermissions.UrunMaliyetleriYonet);
foreach(var action in new[]{"OnEkEkle","OnEkDurum","OnEkSil"}) Add("UrunMaliyetleri",action,ReportPermissions.UrunMaliyetleriYonet,"POST");
foreach(var action in new[]{"Index","Hareketler","Panel"}) Add("SatisKarlilikGiderleri",action,ReportPermissions.MuhasebeGiderleri);
Add("SatisKarlilikGiderleri","Oneriler",ReportPermissions.MuhasebeGiderleriYonet);
foreach(var action in new[]{"GrupKaydet","GrupSil","HesaplariBagla","KuralDuzenle","KuralSil","HazirGruplariOlustur","OnerileriOnayla"}) Add("SatisKarlilikGiderleri",action,ReportPermissions.MuhasebeGiderleriYonet,"POST");
var requestCount=0;
foreach(var route in routes) foreach(var actor in actors) {
    using var request=new HttpRequestMessage(new HttpMethod(route.Method),$"/{route.Controller}/{route.Action}");
    request.Headers.Add("X-Test-Actor",actor.Key);using var response=await client.SendAsync(request);
    // Independent expectation, not the production helper used by the registered policies.
    var allowed=actor.Key=="admin"||actor.Key==route.Policy||
        (route.Policy==ReportPermissions.UrunMaliyetleri&&actor.Key==ReportPermissions.UrunMaliyetleriYonet)||
        (route.Policy==ReportPermissions.MuhasebeGiderleri&&actor.Key==ReportPermissions.MuhasebeGiderleriYonet)||
        (route.Policy==ReportPermissions.PivotRaporlar&&actor.Key==ReportPermissions.PivotRaporlarYonet);
    allowed=allowed&&(!route.AdminOnly||actor.Key=="admin");
    var expected=allowed?204:actor.Key=="anonymous"?401:403;
    if((int)response.StatusCode!=expected) throw new Exception($"FAIL {route.Controller}/{route.Action} {route.Method} actor={actor.Key}: expected {expected}, got {response.StatusCode}");
    requestCount++;
}
Check(true,$"MVC authorization: {requestCount} checks across {routes.Count} routes; anonymous, roles, isolated claims, read/manage and Admin");
var controllers=routes.Select(x=>x.Controller).ToHashSet();
var actions=host.Services.GetRequiredService<IActionDescriptorCollectionProvider>().ActionDescriptors.Items.OfType<ControllerActionDescriptor>();
foreach(var action in actions.Where(a=>controllers.Contains(a.ControllerName)&&a.MethodInfo.DeclaringType==a.ControllerTypeInfo.AsType())) {
    if(action.ControllerName=="Reports"&&new[]{"Run","ExportCsv","ExportXlsx"}.Contains(action.ActionName)) continue;
    Check(routes.Any(r=>r.Controller==action.ControllerName&&r.Action==action.ActionName),"Covered endpoint "+action.ControllerName+"/"+action.ActionName);
}
Check(!ReportPermissions.CanSeeModule(actors["role"])&&!ReportPermissions.CanSeeModule(actors["manager"])&&!ReportPermissions.CanSeeModule(actors["module"]),"Role/module alone never reveals reports menu");
Check(ReportPermissions.CanSeeModule(actors[ReportPermissions.LogoCariEkstre]),"Page claim reveals its menu without a broad role");
Check(ReportPermissions.CanAccess(actors[ReportPermissions.UrunMaliyetleriYonet],ReportPermissions.UrunMaliyetleri)&&!ReportPermissions.CanAccess(actors[ReportPermissions.UrunMaliyetleri],ReportPermissions.UrunMaliyetleriYonet),"Manage implies view, never view implies write");
Check(!ReportPermissions.CanAccess(actors["anonymous"],ReportPermissions.Bankalar)&&!ReportPermissions.CanAccess(actors["admin"],"unknown"),"Anonymous and unknown policies fail closed");
var legacy=new ReportsController(null!,null!,null!,null!,null!,null!,null!);var guards=0;
foreach(var actor in actors) foreach(var key in new[]{"BorcAlacak","SalesByDay","LogoCariEkstre","","BorcAlacak;DROP","borcalacak"}) {
    var expected=actor.Key=="admin"||(key=="BorcAlacak"&&actor.Key==ReportPermissions.BorcAlacak);
    if(ReportPermissions.CanRun(actor.Value,key)!=expected) throw new Exception("Runner guard mismatch: "+actor.Key+"/"+key);
    if(expected) continue;
    legacy.ControllerContext=new ControllerContext { HttpContext=new DefaultHttpContext { User=actor.Value } };var input=new ReportRequest { ReportKey=key };
    if(await legacy.Run(input,default) is not ForbidResult||await legacy.ExportCsv(input,default) is not ForbidResult||await legacy.ExportXlsx(input,default) is not ForbidResult) throw new Exception("Runner did not reject before querying");guards++;
}
Check(guards>90,"Legacy Run/CSV/XLSX guards reject unapproved report keys and claims before service access");
var menuPath="/Views/Shared/Metronic/menu_nav_moduller.cshtml";
var none=WebUtility.HtmlDecode(await Render(host,menuPath,new object(),actors["role"]));Check(!none.Contains(">Raporlar</span>"),"Report menu hidden without page permission");
var menu=WebUtility.HtmlDecode(await Render(host,menuPath,new object(),actors[ReportPermissions.LogoCariEkstre]));
Check(menu.Contains("Cari Ekstre · Firma Bazlı")&&!menu.Contains(">Borç / Alacak</span>")&&!menu.Contains(">Bankalar</span>"),"Statement-only user sees just its report link");
var bankMenu=WebUtility.HtmlDecode(await Render(host,menuPath,new object(),actors[ReportPermissions.Bankalar]));
Check(bankMenu.Contains(">Bankalar</span>")&&!bankMenu.Contains("Havale / EFT Hareketleri")&&!bankMenu.Contains(">Banka Kredileri</span>"),"Bank permission does not reveal separate reports");
foreach(var policy in ReportPermissions.Policies) Check(WebUtility.HtmlDecode(await Render(host,menuPath,new object(),actors[policy])).Contains(">Raporlar</span>"),"Menu renders for "+policy);
var cost=new ProductCostIndexVM { Products=[new ProductCostSummaryVM { LogoMaterialRef=1,ProductCode="TEST" }] };
var readCost=await Render(host,"/Views/UrunMaliyetleri/Index.cshtml",cost,actors[ReportPermissions.UrunMaliyetleri]);
var manageCost=await Render(host,"/Views/UrunMaliyetleri/Index.cshtml",cost,actors[ReportPermissions.UrunMaliyetleriYonet]);
Check(!readCost.Contains("Yeni Versiyon")&&!readCost.Contains("Toplu Maliyet Girişi")&&manageCost.Contains("Yeni Versiyon")&&manageCost.Contains("Toplu Maliyet Girişi"),"Cost write links hidden for readers");
var readExpenses=await Render(host,"/Views/SatisKarlilikGiderleri/Index.cshtml",new AccountingExpenseManagementVM(),actors[ReportPermissions.MuhasebeGiderleri]);
var manageExpenses=await Render(host,"/Views/SatisKarlilikGiderleri/Index.cshtml",new AccountingExpenseManagementVM(),actors[ReportPermissions.MuhasebeGiderleriYonet]);
Check(!readExpenses.Contains("Grup Ekle")&&!readExpenses.Contains("Hazır Grupları Oluştur")&&manageExpenses.Contains("Grup Ekle")&&WebUtility.HtmlDecode(manageExpenses).Contains("Hazır Grupları Oluştur"),"Expense write forms hidden for readers");
await host.StopAsync();
// Randomly named LOCAL fixture only; no live portal/Logo configuration is loaded.
var prefix="CodexReportPermissionsTest_";var name=prefix+Guid.NewGuid().ToString("N");
var cs=new SqlConnectionStringBuilder { DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog="master",IntegratedSecurity=true,TrustServerCertificate=true };
await using var db=new SqlConnection(cs.ConnectionString);await db.OpenAsync();var created=false;
try {
    await db.ExecuteAsync($"CREATE DATABASE [{name}]");created=true;db.ChangeDatabase(name);
    await db.ExecuteAsync("""
        CREATE TABLE AuthorizationCategory(ID int IDENTITY PRIMARY KEY,Ad nvarchar(250),Aciklama nvarchar(1000),IsActive bit,IsDelete bit,CreateDate datetime);
        CREATE TABLE Yetkilendirme(ID int IDENTITY PRIMARY KEY,Ad nvarchar(250),Aciklama nvarchar(1000),KategoriID int,PolicyName nvarchar(250),ClaimType nvarchar(100),ClaimName nvarchar(250),IsActive bit,IsDelete bit,CreateDate datetime,UpdateDate datetime);
        CREATE TABLE ExistingUserClaims(ClaimType nvarchar(100),ClaimValue nvarchar(250));
        INSERT AuthorizationCategory(Ad,IsActive,IsDelete) VALUES(N'Raporlar',0,1);
        INSERT Yetkilendirme(Ad,KategoriID,ClaimType,ClaimName,IsActive,IsDelete) VALUES(N'Eski Bankalar',1,N'Rapor',N'Bankalar',1,0);
        INSERT ExistingUserClaims VALUES(N'Rapor',N'Bankalar');
        """);
    var migration=new AddReportPagePermissions();
    foreach(var op in migration.UpOperations.Cast<SqlOperation>()) { await db.ExecuteAsync(op.Sql);await db.ExecuteAsync(op.Sql); }
    // Schema/initial report definition is covered by the pivot fixture; permission SQL is idempotent.
    foreach(var op in new AddPivotReports().UpOperations.OfType<SqlOperation>().Skip(1)) { await db.ExecuteAsync(op.Sql);await db.ExecuteAsync(op.Sql); }
    var definitions=(await db.QueryAsync<string>("SELECT PolicyName FROM Yetkilendirme WHERE IsActive=1 AND IsDelete=0 AND ClaimType=N'Rapor'")).Order().ToList();
    Check(definitions.SequenceEqual(ReportPermissions.Policies.Order()),"Idempotent migration definitions exactly match registered policies");
    Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ExistingUserClaims WHERE ClaimType=N'Rapor' AND ClaimValue=N'Bankalar'")==1&&await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM ExistingUserClaims")==1,"Migration preserves assignments and grants no new claims");
    foreach(var op in migration.DownOperations.Cast<SqlOperation>()) await db.ExecuteAsync(op.Sql);
    foreach(var op in new AddPivotReports().DownOperations.OfType<SqlOperation>()) await db.ExecuteAsync(op.Sql);
    Check(await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Yetkilendirme WHERE ClaimType=N'Rapor' AND ClaimName=N'Bankalar' AND IsDelete=0")==1,"Rollback preserves existing Bankalar permission");
} finally {
    if(created&&name.StartsWith(prefix)&&Guid.TryParseExact(name[prefix.Length..],"N",out _)) {
        db.ChangeDatabase("master");await db.ExecuteAsync($"ALTER DATABASE [{name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{name}]");Console.WriteLine("Removed isolated test database: "+name);
    }
}
Console.WriteLine("All authorization, UI and migration tests passed. No live connection settings loaded.");

static async Task<string> Render(WebApplication host,string path,object model,ClaimsPrincipal user) {
    await using var scope=host.Services.CreateAsyncScope();var http=new DefaultHttpContext { RequestServices=scope.ServiceProvider,User=user };
    http.Request.Scheme="http";http.Request.Host=new HostString("localhost");http.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(),"Render"));
    http.Request.RouteValues["controller"]=path.StartsWith("/Views/Shared/")?"Reports":path.Split('/')[2];http.Request.RouteValues["action"]="Index";
    var action=new ActionContext(http,new RouteData(http.Request.RouteValues),new ActionDescriptor(),new ModelStateDictionary());
    var view=scope.ServiceProvider.GetRequiredService<IRazorViewEngine>().GetView(null,path,false);if(!view.Success)throw new Exception("Missing compiled view "+path);
    using var writer=new StringWriter();
    await view.View.RenderAsync(new ViewContext(action,view.View,new ViewDataDictionary(new EmptyModelMetadataProvider(),action.ModelState){Model=model},new TempDataDictionary(http,new MemoryTempData()),writer,new HtmlHelperOptions()));return writer.ToString();
}
sealed class MemoryTempData:ITempDataProvider { public IDictionary<string,object> LoadTempData(HttpContext c)=>new Dictionary<string,object>();public void SaveTempData(HttpContext c,IDictionary<string,object> values){} }
sealed class EmptyOtherModuleMenus:IViewComponentHelper {
    public Task<IHtmlContent> InvokeAsync(string name,object? arguments)=>Task.FromResult<IHtmlContent>(HtmlString.Empty);
    public Task<IHtmlContent> InvokeAsync(Type componentType,object? arguments)=>Task.FromResult<IHtmlContent>(HtmlString.Empty);
}
sealed class FixtureAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder) {
    public static Dictionary<string,ClaimsPrincipal> Actors {get;set;}=[];
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
        var name=Request.Headers["X-Test-Actor"].ToString();
        return Task.FromResult(Actors.TryGetValue(name,out var user)&&user.Identity?.IsAuthenticated==true
            ?AuthenticateResult.Success(new AuthenticationTicket(user,Scheme.Name)):AuthenticateResult.NoResult());
    }
}
