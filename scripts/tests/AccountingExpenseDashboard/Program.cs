using System.Net;
using System.Security.Claims;
using Dapper;
using Ekomers.Data;
using Ekomers.Data.Services.Profitability;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Ekomers.Web.Controllers;
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

static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);Console.WriteLine("PASS "+label);}
var prefix="CodexExpenseDashboardTest_";var name=prefix+Guid.NewGuid().ToString("N");
var cs=new SqlConnectionStringBuilder{DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog=name,IntegratedSecurity=true,TrustServerCertificate=true}.ConnectionString;
var options=new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options;var created=false;
try {
    await using var portal=new ExpenseContext(options);await portal.Database.EnsureCreatedAsync();created=true;
    await using var connection=new SqlConnection(cs);await connection.OpenAsync();
    await connection.ExecuteAsync("""
        CREATE TABLE dbo.LG_100_EMUHACC(LOGICALREF int PRIMARY KEY,CODE varchar(100),DEFINITION_ nvarchar(250));
        CREATE TABLE dbo.LG_100_01_EMFICHE(LOGICALREF int PRIMARY KEY,DATE_ datetime,FICHENO nvarchar(50),GENEXP1 nvarchar(200),TRCODE int DEFAULT 4,CANCELLED int DEFAULT 0);
        CREATE TABLE dbo.LG_100_01_EMFLINE(LOGICALREF int PRIMARY KEY,ACCFICHEREF int,ACCOUNTREF int,LINEEXP nvarchar(200),DEBIT decimal(28,6) DEFAULT 0,CREDIT decimal(28,6) DEFAULT 0,CANCELLED int DEFAULT 0);
        INSERT dbo.LG_100_EMUHACC VALUES(1,'730.01',N'Kira <script>alert(1)</script>'),(2,'770.02',N'Ofis enerji'),(3,'780.01',N'Finansman'),(4,'770.03',N'Özel'),(5,'730.99',N'Bağlanmamış'),(6,'710.01',N'Hammadde'),(7,'731.01',N'Yansıtma'),(8,'730.02',N'Hariç grup'),(9,'770.04',N'Çakışan'),(10,'730.03',N'Pasif grup'),(11,'791.01',N'İnceleme');
        INSERT dbo.LG_100_01_EMFICHE(LOGICALREF,DATE_,FICHENO) VALUES(1,'20260110','F1'),(2,'20260112','F2'),(3,'20260120','F3'),(4,'20260201','F4'),(5,'20260125','F5'),(6,'20260111','F6'),(7,'20260109','F7'),(8,'20260121','F8'),(9,'20260122','F9'),(10,'20260123','F10'),(11,'20260124','F11'),(12,'20260125','F12'),(13,'20260126','F13'),(14,'20260127','F14'),(15,'20260128','F15'),(16,'20260129','F16'),(17,'20260130','F17');
        UPDATE dbo.LG_100_01_EMFICHE SET TRCODE=9 WHERE LOGICALREF=15;
        UPDATE dbo.LG_100_01_EMFICHE SET CANCELLED=1 WHERE LOGICALREF=16;
        INSERT dbo.LG_100_01_EMFLINE(LOGICALREF,ACCFICHEREF,ACCOUNTREF,DEBIT,CREDIT) VALUES(1,1,1,100,0),(2,2,1,0,20),(3,3,1,50,0),(4,4,1,0,120),(5,5,2,30,0),(6,6,3,10,0),(7,7,4,40,0),(8,8,5,99,0),(9,9,6,1000,0),(10,10,7,500,0),(11,10,1,500,0),(12,11,8,15,0),(13,12,9,7,0),(14,13,10,5,0),(15,14,11,6,0),(16,15,1,999,0),(17,16,1,501,0),(18,17,1,502,0);
        UPDATE dbo.LG_100_01_EMFLINE SET CANCELLED=1 WHERE LOGICALREF=18;
        """);
    var rent=new RptAccountingExpenseGroup{Name="Kira <script>alert(2)</script>",PresetKey="factory.rent.fixed",Behavior=RptExpenseBehavior.Fixed,Stage=RptExpenseStage.Operating,IsActive=true,IsDelete=false};
    var office=new RptAccountingExpenseGroup{Name="Operasyon enerji",PresetKey="operating.office.variable",Behavior=RptExpenseBehavior.Variable,Stage=RptExpenseStage.Operating,IsActive=true,IsDelete=false};
    var finance=new RptAccountingExpenseGroup{Name="Finansman komisyonu",PresetKey="finance.fees.variable",Behavior=RptExpenseBehavior.Variable,Stage=RptExpenseStage.Financing,IsActive=true,IsDelete=false};
    var custom=new RptAccountingExpenseGroup{Name="Fabrika gibi adı olan özel grup",Behavior=RptExpenseBehavior.Fixed,Stage=RptExpenseStage.Operating,IsActive=true,IsDelete=false};
    var excluded=new RptAccountingExpenseGroup{Name="Hariç",Behavior=RptExpenseBehavior.Excluded,Stage=RptExpenseStage.Operating,IsActive=true,IsDelete=false};
    var inactive=new RptAccountingExpenseGroup{Name="Pasif",Behavior=RptExpenseBehavior.Fixed,Stage=RptExpenseStage.Operating,IsActive=false,IsDelete=false};
    var empty=new RptAccountingExpenseGroup{Name="Hareketsiz grup",PresetKey="factory.other.fixed",Behavior=RptExpenseBehavior.Fixed,Stage=RptExpenseStage.Operating,IsActive=true,IsDelete=false};
    portal.AddRange(rent,office,finance,custom,excluded,inactive,empty);await portal.SaveChangesAsync();
    void Rule(string code,int group,DateTime from,DateTime? until=null)=>portal.Add(new RptAccountingExpenseRule{FirmNumber=100,AccountCode=code,ExpenseGroupId=group,ValidFrom=from,ValidTo=until,IsActive=true,IsDelete=false});
    var start=new DateTime(2026,1,1);var end=new DateTime(2026,3,31);
    Rule("730.01",rent.ID,start,new DateTime(2026,1,15));Rule("730.01",office.ID,new DateTime(2026,1,16));Rule("770.02",office.ID,start);
    Rule("780.01",finance.ID,start);Rule("770.03",custom.ID,start);Rule("730.02",excluded.ID,start);Rule("730.03",inactive.ID,start);
    Rule("770.04",rent.ID,start);Rule("770.04",custom.ID,start);await portal.SaveChangesAsync();portal.ChangeTracker.Clear();
    await using var logo=new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(cs).Options);
    var service=new AccountingExpenseService(portal,logo);var summary=await service.GetSummaryAsync(start,end,default);
    Check(summary.TotalIncluded==90&&summary.OperatingFixed==120&&summary.OperatingVariable==-40&&summary.FinancingVariable==10,"Existing profitability totals preserved; refunds reduce net expenses");
    Check(summary.MovementCount==18&&summary.UnmappedAccountCount==3&&summary.UnmappedAmount==111&&summary.ReviewMovementCount==2&&summary.ExcludedMovementCount==6,"Unmapped/overlap/inactive, review, material, reflection, excluded and cancelled rows remain outside totals");
    Check(summary.Groups.Sum(g=>g.Debit)==230&&summary.Groups.Sum(g=>g.Credit)==140&&summary.Groups.Sum(g=>g.MovementCount)==7,"Eligible group debit/credit/count reconcile with net");
    Check(summary.Groups.All(g=>g.Accounts.Sum(a=>a.Amount)==g.Amount&&g.Months.Sum(m=>m.Amount)==g.Amount),"Every group reconciles to its exact account and monthly contributions");
    Check(summary.Groups.Single(g=>g.GroupId==rent.ID).Accounts.Single().Amount==80&&summary.Groups.Single(g=>g.GroupId==office.ID).Accounts.Single(a=>a.AccountCode=="730.01").Amount==-70,"Date-effective group changes split one account correctly, not whole-account totals");
    var definitions=await portal.RptAccountingExpenseGroups.AsNoTracking().ToListAsync();
    AccountingExpenseDashboardVM Build(string category="all",string behavior="all",string search="",bool showEmpty=false)=>AccountingExpenseDashboard.Build(summary,definitions,new(){StartDate=start,EndDate=end,Category=category,Behavior=behavior,Search=search,ShowEmpty=showEmpty});
    var panel=Build();
    Check(panel.Total==90&&panel.Fixed==120&&panel.Variable==-30&&panel.Groups.Count==4&&panel.Categories.Sum(c=>c.Amount)==90,"Dashboard KPIs and disjoint category totals reconcile");
    Check(panel.Months.Select(m=>m.Amount).SequenceEqual(new[]{210m,-120m,0m})&&panel.Months.Sum(m=>m.Amount)==90,"Monthly timeline preserves negative months and fills empty months with zero");
    Check(panel.Groups.Single(g=>g.Total.GroupId==rent.ID).Category=="factory"&&panel.Groups.Single(g=>g.Total.GroupId==custom.ID).Category=="custom","Renamed preset keeps category; custom group category is never inferred from its name");
    Check(panel.Groups.Where(g=>g.Total.Amount<0).All(g=>g.PositiveShare==0)&&Math.Abs(panel.Groups.Sum(g=>g.PositiveShare)-100)<0.00001m,"Positive-only distribution has no negative/misleading percentages");
    Check(Build("factory").Total==80&&Build(behavior:"fixed").Total==120&&Build(behavior:"variable").Total==-30&&Build(search:"KİRA").Total==80,"Category, fixed/variable and Turkish search filters recalculate all indicators");
    Check(Build(search:"%").Groups.Count==0,"Punctuation-only search does not accidentally match every group");
    Check(Build(showEmpty:true).Groups.Count==5&&Build(showEmpty:true).Groups.All(g=>g.Total.GroupId!=inactive.ID&&g.Total.GroupId!=excluded.ID),"Only active non-excluded empty groups can be added to display");
    var oldCount=summary.Groups.Count;summary.Groups.Add(new(){GroupId=999,Name="Zero net but active",Behavior=RptExpenseBehavior.Fixed,Stage=RptExpenseStage.Operating,MovementCount=2,Debit=10,Credit=10});
    Check(Build().Groups.Count==oldCount+1,"Zero-net group with movements is not hidden as an empty group");summary.Groups.RemoveAt(oldCount);
    Check(!AccountingExpenseDashboard.ValidFilter(new(){StartDate=end,EndDate=start})&&!AccountingExpenseDashboard.ValidFilter(new(){StartDate=start,EndDate=end,Category="bad"})&&!AccountingExpenseDashboard.ValidFilter(new(){StartDate=start,EndDate=start.AddYears(4)}),"Invalid dates/categories and unbounded chart ranges rejected");
    Check(!AccountingExpenseDashboard.ValidFilter(new(){StartDate=start,EndDate=end,Category=null!})&&!AccountingExpenseDashboard.ValidFilter(new(){StartDate=start,EndDate=end,Search=null!}),"Malformed null filter values fail safely");
    var counting=new CountingExpenses(service);var controller=new SatisKarlilikGiderleriController(portal,logo,counting){ControllerContext=new(){HttpContext=new DefaultHttpContext()}};
    Check(await controller.Panel(start,end,ct:default) is ViewResult{Model:AccountingExpenseDashboardVM{Total:90}},"Controller projects real classified amounts into the read-only panel");var calls=counting.Calls;
    Check(await controller.Panel(end,start,ct:default) is ViewResult{Model:AccountingExpenseDashboardVM{Error:not null}}&&counting.Calls==calls,"Invalid panel filters do not query accounting data");
    Check(!portal.ChangeTracker.HasChanges()&&await portal.RptAccountingExpenseGroups.CountAsync()==7&&await portal.RptAccountingExpenseRules.CountAsync()==9,"Reading dashboard never changes groups, mappings or tracked data");
    var partial=await service.GetSummaryAsync(new DateTime(2026,1,16),new DateTime(2026,1,31),default);
    Check(partial.TotalIncluded==80,"Partial-month range includes only selected dates");
    var builder=WebApplication.CreateBuilder(new WebApplicationOptions{ApplicationName=typeof(SatisKarlilikGiderleriController).Assembly.GetName().Name,EnvironmentName="Development",Args=[]});
    builder.Logging.ClearProviders();builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SatisKarlilikGiderleriController).Assembly);builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    await using var host=builder.Build();
    var previewHtml="";
    if(args.Contains("--preview")) {
        var repository=new DirectoryInfo(builder.Environment.ContentRootPath);
        while(repository!=null&&!Directory.Exists(Path.Combine(repository.FullName,"Ekomers.Web","wwwroot")))repository=repository.Parent;
        if(repository==null)throw new DirectoryNotFoundException("Preview requires the repository's Ekomers.Web/wwwroot directory.");
        host.UseStaticFiles(new StaticFileOptions{FileProvider=new PhysicalFileProvider(Path.Combine(repository.FullName,"Ekomers.Web","wwwroot"))});
        host.MapGet("/preview",()=>Results.Content(previewHtml,"text/html; charset=utf-8"));
    }
    host.MapDefaultControllerRoute();host.Urls.Add("http://127.0.0.1:0");await host.StartAsync();
    var html=await Render(host,panel);
    Check(html.Contains("&lt;script&gt;")&&!html.Contains("<script>alert("),"Razor encodes group and account names");
    var decoded=WebUtility.HtmlDecode(html);
    Check(decoded.Contains("Yönetici Gider Paneli")&&decoded.Contains("90,00")&&decoded.Contains("-120,00")&&decoded.Contains("Hesabın Tüm Hareketleri")&&decoded.Contains("<details"),"Compiled panel renders KPIs, signed trend, expandable accounts and explicitly all-account movement links");
    Check(decoded.Contains("accountCode=730.01")&&decoded.Contains("startDate=2026-01-01")&&decoded.Contains("endDate=2026-03-31"),"Account movement links preserve date/account scope");
    var errorHtml=await Render(host,new(){Filter=panel.Filter,Error="Bağlantı okunamadı"});
    Check(WebUtility.HtmlDecode(errorHtml).Contains("Bağlantı okunamadı")&&!errorHtml.Contains("ed-kpi-main"),"Connection/filter errors never display a misleading zero dashboard");
    if(args.Contains("--preview")) {
        previewHtml="<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><link rel=\"stylesheet\" href=\"/assets1/css/style.bundle.css\"><style>body{background:#f1f6f2;padding:24px;font-family:Arial,sans-serif}</style></head><body>"+html+"</body></html>";
        Console.WriteLine("Preview: "+host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()+"/preview");
        using var previewTimeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
        await host.WaitForShutdownAsync(previewTimeout.Token);
    }
    await host.StopAsync();Console.WriteLine("All dashboard SQL/calculation/controller/Razor tests passed; no live settings loaded.");
} finally {
    if(created&&name.StartsWith(prefix)&&Guid.TryParseExact(name[prefix.Length..],"N",out _)){SqlConnection.ClearAllPools();await using var cleanup=new ExpenseContext(options);await cleanup.Database.EnsureDeletedAsync();Console.WriteLine("Removed isolated test database: "+name);}
}
static async Task<string> Render(WebApplication host,AccountingExpenseDashboardVM model){
    await using var scope=host.Services.CreateAsyncScope();var http=new DefaultHttpContext{RequestServices=scope.ServiceProvider,User=new ClaimsPrincipal(new ClaimsIdentity([new Claim("Rapor","MuhasebeGiderleri")],"Fixture"))};
    http.Request.Scheme="http";http.Request.Host=new HostString("localhost");http.SetEndpoint(new Endpoint(_=>Task.CompletedTask,new EndpointMetadataCollection(),"Render"));
    var route=new RouteData();route.Values["controller"]="SatisKarlilikGiderleri";route.Values["action"]="Panel";
    var action=new ActionContext(http,route,new ActionDescriptor(),new ModelStateDictionary());
    var view=scope.ServiceProvider.GetRequiredService<IRazorViewEngine>().GetView(null,"/Views/SatisKarlilikGiderleri/Panel.cshtml",false);Check(view.Success,"Compiled panel view found");
    var data=new ViewDataDictionary(new EmptyModelMetadataProvider(),action.ModelState){Model=model};data["PanelToday"]=new DateTime(2026,10,20);
    using var writer=new StringWriter();await view.View.RenderAsync(new ViewContext(action,view.View,data,new TempDataDictionary(http,new MemoryTempData()),writer,new HtmlHelperOptions()));return writer.ToString();
}
sealed class ExpenseContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options){
    protected override void OnModelCreating(ModelBuilder builder){
        var keep=new[]{typeof(RptAccountingExpenseGroup),typeof(RptAccountingExpenseRule)};
        foreach(var t in builder.Model.GetEntityTypes().Select(x=>x.ClrType).Where(x=>!keep.Contains(x)).ToList())builder.Ignore(t);
        foreach(var t in keep){builder.Entity(t).HasBaseType((Type?)null);builder.Entity(t).HasKey("ID");builder.Entity(t).ToTable(t.Name);}
    }
}
sealed class CountingExpenses(IAccountingExpenseService source):IAccountingExpenseService{
    public int Calls{get;private set;}
    public Task<AccountingExpenseSummaryVM> GetSummaryAsync(DateTime start,DateTime end,CancellationToken ct){Calls++;return source.GetSummaryAsync(start,end,ct);}
    public Task<AccountingExpenseMovementsVM> GetMovementsAsync(string account,DateTime start,DateTime end,int page,CancellationToken ct)=>source.GetMovementsAsync(account,start,end,page,ct);
}
sealed class MemoryTempData:ITempDataProvider{public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();public void SaveTempData(HttpContext context,IDictionary<string,object> values){} }
