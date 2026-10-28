using System.Security.Claims;
using System.Text.Json;
using Dapper;
using Ekomers.Common.Services;
using Ekomers.Data;
using Ekomers.Data.Services.Reports;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels.Raporlar;
using Ekomers.Web.Controllers;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

static void Check(bool ok, string label) { if (!ok) throw new Exception("FAIL " + label); Console.WriteLine("PASS " + label); }
static async Task Reject(Func<Task> test, string label) { try { await test(); } catch (ArgumentException) { Console.WriteLine("PASS " + label); return; } throw new Exception("FAIL " + label); }
var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
while (repository != null && !Directory.Exists(Path.Combine(repository.FullName, "Ekomers.Web", "wwwroot"))) repository = repository.Parent;
if (repository == null) throw new DirectoryNotFoundException("Run inside the ECommerce repository.");
if (args.Contains("--metadata")) {
    // Read-only live metadata smoke. Never log configuration or credentials, nor invoice data.
    var config = new ConfigurationBuilder().SetBasePath(Path.Combine(repository.FullName, "Ekomers.Web")).AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json", true).AddEnvironmentVariables().Build();
    await using var live = new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(CryptoHelper.Decrypt(config.GetConnectionString("LogoConnection") ?? "")).Options);
    var metadata = await new LogoPivotService(live).ColumnsAsync("dbo", "fatura_dokumu_100", default);
    foreach (var c in metadata) Console.WriteLine($"{c.Name} ({c.DataType})");
    return;
}
var database = "CodexPivotTest_" + Guid.NewGuid().ToString("N");
var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\MSSQLLocalDB", InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = true }.ConnectionString;
var created = false;
try {
    await using var portal = new PivotContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);
    await portal.Database.EnsureCreatedAsync(); created = true;
    await using var connection = new SqlConnection(cs); await connection.OpenAsync();
    await connection.ExecuteAsync("CREATE TABLE dbo.PivotFixture(Region nvarchar(100),Currency nvarchar(20),Amount decimal(18,6),DocDate datetime2,Secret nvarchar(50));");
    await connection.ExecuteAsync("INSERT dbo.PivotFixture VALUES(N'A','TRY',10,'20260101',N'secret'),(N'A','TRY',20,'20260101 13:00',N'secret'),(N'A','EUR',100,'20260102',N'secret'),(N'B','TRY',-40,'20260101',N'secret'),(N'B','TRY',NULL,'20260101',N'secret'),(NULL,'TRY',5,'20260101',N'secret'),(N'','TRY',15,'20260101',N'secret');");
    await connection.ExecuteAsync("CREATE VIEW dbo.fatura_dokumu_100 AS SELECT * FROM dbo.PivotFixture;");
    await connection.ExecuteAsync("CREATE VIEW dbo.fatura_dokumu_103 AS SELECT * FROM dbo.PivotFixture;");
    await using var logo = new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(cs).Options);
    var service = new LogoPivotService(logo); var columns = await service.ColumnsAsync("dbo", "fatura_dokumu_100", default);
    Check(columns.Count == 5 && columns.Single(c => c.Name == "Amount").Numeric && columns.Single(c => c.Name == "DocDate").Date, "View-only typed metadata");
    await Reject(() => service.ColumnsAsync("dbo", "PivotFixture", default), "Tables cannot be used instead of views");
    await Reject(() => service.ColumnsAsync("dbo", "fatura_dokumu_100]; DROP TABLE X;--", default), "Injected view identifiers fail metadata validation");
    var settings = new PivotSettingsVM { AllowedFields = ["Region", "Currency", "Amount", "DocDate"], Layout = new() { Rows = ["Region"], Column = "Currency", Measures = [new() { Field = "Amount", Function = "sum" }, new() { Field = "Amount", Function = "avg" }, new() { Field = "Amount", Function = "count" }] } };
    var result = await service.RunAsync("dbo", "fatura_dokumu_100", settings, settings.Layout, default);
    Check(result.SourceRowCount == 7 && result.GrandTotal.SequenceEqual(new decimal?[] { 110, 110m / 6, 6 }), "Grand sum / weighted average / non-null count include negatives, not averages of averages");
    Check(result.RowTotals.Single(c => c.Row[0] == "A").Values[1] == 130m / 3 && result.ColumnTotals.Single(c => c.Column == "TRY").Values[0] == 10, "Row and column totals independently reconcile");
    Check(result.RowTotals.Any(r => r.Row[0] == null) && result.RowTotals.Any(r => r.Row[0] == ""), "NULL and empty strings stay distinct");
    PivotLayoutVM Copy() => JsonSerializer.Deserialize<PivotLayoutVM>(JsonSerializer.Serialize(settings.Layout))!;
    var hidden = Copy(); hidden.Rows = ["Secret"]; await Reject(() => service.RunAsync("dbo", "fatura_dokumu_100", settings, hidden, default), "Hidden columns cannot be read through forged requests");
    var injected = Copy(); injected.Measures[0].Function = "sum); DROP TABLE X;--"; await Reject(() => service.RunAsync("dbo", "fatura_dokumu_100", settings, injected, default), "Aggregate function allowlist");
    var duplicate = Copy(); duplicate.Rows = ["Currency"]; await Reject(() => service.RunAsync("dbo", "fatura_dokumu_100", settings, duplicate, default), "Duplicate row/column dimensions rejected");
    var filter = Copy(); filter.Filters = [new() { Field = "Region", Operator = "contains", Value = "' OR 1=1--" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, filter, default)).SourceRowCount == 0, "Filter text is parameterized, no SQL expansion");
    var alternatives = Copy(); alternatives.Filters = [new() { Field = "Region", Value = "A" }, new() { Field = "Region", Value = "B" }];
    var union = await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default);
    Check(union.SourceRowCount == 5 && union.GrandTotal.SequenceEqual(new decimal?[] { 90, 22.5m, 4 }), "Same-field equalities use OR, preserving sums, weighted averages and non-null counts");
    alternatives.Filters.Insert(1, new() { Field = "Currency", Value = "EUR" });
    var scoped = await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default);
    Check(scoped.SourceRowCount == 1 && scoped.GrandTotal[0] == 100, "Equality alternatives are parenthesized: OR cannot bypass a different field's filter");
    alternatives.Filters.Reverse();
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default)).SourceRowCount == 1, "Filter order does not change equality grouping");
    alternatives.Filters = [new() { Field = "Region", Value = "A" }, new() { Field = "Region", Value = "B" }, new() { Field = "DocDate", Value = "2026-01-01" }];
    var dayAlternatives = await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default);
    Check(dayAlternatives.SourceRowCount == 4 && dayAlternatives.GrandTotal[0] == -10, "Invoice-type-style alternatives still respect another field's calendar date filter");
    alternatives.Filters = [new() { Field = "DocDate", Operator = "gte", Value = "2026-01-02" }, new() { Field = "DocDate", Operator = "lte", Value = "2026-01-02" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default)).SourceRowCount == 1, "Same-field date bounds remain AND, not OR");
    alternatives.Filters = [new() { Field = "Amount", Operator = "gte", Value = "0" }, new() { Field = "Amount", Operator = "lte", Value = "20" }];
    var numericRange = await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default);
    Check(numericRange.SourceRowCount == 4 && numericRange.GrandTotal[0] == 50, "Same-field numeric bounds remain AND");
    alternatives.Filters = [new() { Field = "Amount", Value = "10" }, new() { Field = "Amount", Operator = "gte", Value = "15" }, new() { Field = "Amount", Value = "20" }];
    var boundedAlternatives = await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default);
    Check(boundedAlternatives.SourceRowCount == 1 && boundedAlternatives.GrandTotal[0] == 20, "Numeric equality alternatives also respect a range on the same field");
    alternatives.Filters = [new() { Field = "Region", Value = "A" }, new() { Field = "Region", Value = "A" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default)).SourceRowCount == 3, "Repeated identical equalities never double-count source records");
    alternatives.Filters = [new() { Field = "Region", Operator = "contains", Value = "A" }, new() { Field = "Region", Operator = "contains", Value = "B" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default)).SourceRowCount == 0, "Contains conditions remain AND as documented");
    alternatives.Filters = [new() { Field = "Region", Value = "A" }, new() { Field = "Region", Value = "' OR 1=1--" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, alternatives, default)).SourceRowCount == 3, "Grouped equality values remain SQL parameters");
    filter.Filters = [new() { Field = "DocDate", Operator = "eq", Value = "2026-01-01" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, filter, default)).SourceRowCount == 6, "Date equality includes the whole calendar day");
    var date = Copy(); date.Rows = ["DocDate"]; date.Column = null;
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, date, default)).RowTotals.Count == 2, "Datetime dimensions group by day");
    filter.Filters = [new() { Field = "Amount", Operator = "gte", Value = "1,000" }]; await Reject(() => service.RunAsync("dbo", "fatura_dokumu_100", settings, filter, default), "Ambiguous numeric filters rejected");
    var noAxes = Copy(); noAxes.Rows = []; noAxes.Column = null;
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, noAxes, default)).Cells.Single().Values[0] == 110, "Value-only pivot works");
    filter.Filters = [new() { Field = "Region", Value = "B" }]; var nulls = Copy(); nulls.Filters = filter.Filters; nulls.Measures = [new() { Field = "Amount", Function = "min" }, new() { Field = "Amount", Function = "max" }];
    Check((await service.RunAsync("dbo", "fatura_dokumu_100", settings, nulls, default)).GrandTotal.SequenceEqual(new decimal?[] { -40, -40 }), "Min/max ignore null and preserve negative values");
    var report = new RptPivotReport { Name = "Fixture", SchemaName = "dbo", ViewName = "fatura_dokumu_100", SettingsJson = JsonSerializer.Serialize(settings), IsActive = true, UpdatedAt = DateTime.UtcNow };
    portal.Add(report); await portal.SaveChangesAsync(); portal.ChangeTracker.Clear();
    var controller = new PivotRaporlarController(portal, service, NullLogger<PivotRaporlarController>.Instance) { ControllerContext = new() { HttpContext = new DefaultHttpContext() } };
    Check(await controller.Rapor(report.ID, default) is ViewResult { Model: PivotPageVM p } && p.Columns.All(c => c.Name != "Secret"), "Report bootstrap never includes hidden column metadata");
    Check(await controller.Veri(report.ID, hidden, default) is BadRequestObjectResult, "Controller rejects unauthorized data fields");
    Check(await controller.Rapor(123456, default) is NotFoundResult, "Unknown report denied");
    Check(!portal.ChangeTracker.HasChanges(), "Report reads never mutate portal definitions or Logo data");
    var beforeEdit = await portal.RptPivotReports.AsNoTracking().SingleAsync(r=>r.ID==report.ID);
    controller.TempData = new TempDataDictionary(controller.HttpContext, new MemoryTempData());
    var edit = new PivotEditVM { Id=report.ID,Name="Updated fixture",SchemaName="dbo",ViewName="fatura_dokumu_100",IsActive=true,SettingsJson=JsonSerializer.Serialize(settings),Version=Convert.ToBase64String(beforeEdit.Version) };
    Check(await controller.Duzenle(edit,default) is RedirectToActionResult, "Manager updates persisted report definition");
    portal.ChangeTracker.Clear();
    Check((await portal.RptPivotReports.AsNoTracking().SingleAsync(r=>r.ID==report.ID)).Name=="Updated fixture", "Report configuration survives reload");
    edit.Name="Stale overwrite";
    Check(await controller.Duzenle(edit,default) is ViewResult && !controller.ModelState.IsValid, "Stale rowversion cannot overwrite another manager's update");
    portal.ChangeTracker.Clear(); controller.ModelState.Clear();
    Check((await portal.RptPivotReports.AsNoTracking().SingleAsync(r=>r.ID==report.ID)).Name=="Updated fixture", "Concurrency failure leaves saved definition unchanged");
    await portal.RptPivotReports.Where(r=>r.ID==report.ID).ExecuteUpdateAsync(s=>s.SetProperty(r=>r.IsActive,false));
    Check(await controller.Rapor(report.ID,default) is NotFoundResult && await controller.Veri(report.ID,settings.Layout,default) is NotFoundObjectResult, "Inactive report denies both page and data endpoint");
    await portal.RptPivotReports.Where(r=>r.ID==report.ID).ExecuteUpdateAsync(s=>s.SetProperty(r=>r.IsActive,true));
    var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "reader"), new Claim("Rapor", "PivotRaporlar")], "test"); var reader = new ClaimsPrincipal(identity);
    Check(ReportPermissions.CanAccess(reader, ReportPermissions.PivotRaporlar) && !ReportPermissions.CanAccess(reader, ReportPermissions.PivotRaporlarYonet), "View permission does not grant management");
    identity = new ClaimsIdentity([new Claim("Rapor", "PivotRaporlarYonet")], "test"); Check(ReportPermissions.CanAccess(new(identity), ReportPermissions.PivotRaporlar), "Manage permission implies pivot read only");
    foreach (var action in new[] { "Yonetim", "Alanlar" }) Check(typeof(PivotRaporlarController).GetMethod(action)!.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>().Any(a => a.Policy == ReportPermissions.PivotRaporlarYonet), action + " has server-side management policy");
    foreach (var action in typeof(PivotRaporlarController).GetMethods().Where(m => m.GetCustomAttributes(typeof(HttpPostAttribute), true).Length > 0)) Check(action.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).Length > 0, action.Name + " POST requires anti-forgery");
    await using var fullContext = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);
    Check(!fullContext.Database.HasPendingModelChanges(), "Migration snapshot matches the complete ApplicationDbContext model");
    var migrationSql = fullContext.GetService<IMigrator>().GenerateScript("20261020120000_AddReportPagePermissions", "20261021120000_AddPivotReports");
    Check(migrationSql.Contains("CREATE TABLE [RptPivotReport]") && migrationSql.Contains("PivotRaporlarYonet") && migrationSql.Contains("fatura_dokumu_100"), "Migration script contains table, passive initial definition and permission definitions");
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(PivotRaporlarController).Assembly.GetName().Name, EnvironmentName = "Development", Args = [] });
    builder.Logging.ClearProviders(); builder.Services.AddControllersWithViews(options => options.Filters.Add<FixtureFormErrors>()).AddApplicationPart(typeof(PivotRaporlarController).Assembly);
    builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    builder.Services.AddSingleton<ApplicationDbContext>(portal); builder.Services.AddSingleton<ILogoPivotService>(service);
    builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions, FixtureAuth>("Fixture", _ => { }); builder.Services.AddAuthorization(ReportPermissions.AddPolicies);
    await using var host = builder.Build();
    host.UseRouting(); host.UseAuthentication(); host.UseAuthorization();
    var fixturePage = new PivotPageVM { Id = report.ID, Name = "Fatura Dökümü <script>alert(1)</script>", Source = "dbo.fatura_dokumu_100", Columns = columns.Where(c => c.Name != "Secret").ToList(), Settings = settings };
    host.MapGet("/preview", async (HttpContext http) => {
        http.Response.ContentType = "text/html; charset=utf-8";
        await http.Response.WriteAsync("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><link href=\"/assets1/css/style.bundle.css\" rel=\"stylesheet\"><style>body{background:#f2f7f4;padding:24px}</style></head><body>" + await Render(http, "Rapor", fixturePage) + "<script src=\"/js/reports/pivot-reports.js\"></script></body></html>");
    });
    var root = Path.Combine(repository.FullName, "Ekomers.Web", "wwwroot"); host.UseStaticFiles(new StaticFileOptions { FileProvider = new PhysicalFileProvider(root) });
    host.MapDefaultControllerRoute(); host.Urls.Add("http://127.0.0.1:0"); await host.StartAsync();
    using var handler = new HttpClientHandler { AllowAutoRedirect = false };
    using var client = new HttpClient(handler); var baseUrl = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
    var html = await client.GetStringAsync(baseUrl + "/preview");
    Check(System.Net.WebUtility.HtmlDecode(html).Contains("<script>alert(1)</script>") && !html.Contains("<script>alert(1)"), "Compiled report Razor encodes names");
    Check(html.Contains("__RequestVerificationToken") && html.Contains("pvBootstrap") && !html.Contains("Secret"), "Compiled pivot view has CSRF token and only allowed fields");
    // Real MVC form binding/validation (not a direct action call) reproduces the empty Version bug.
    var token = System.Net.WebUtility.HtmlDecode(System.Text.RegularExpressions.Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
    Check(token.Length > 0, "HTTP fixture obtains a real antiforgery token and cookie");
    var secondForm = new Dictionary<string,string> { ["Id"]="0", ["Name"]="Second report", ["SchemaName"]="dbo", ["ViewName"]="fatura_dokumu_103", ["IsActive"]="true", ["SettingsJson"]=JsonSerializer.Serialize(settings), ["Version"]="", ["__RequestVerificationToken"]=token };
    var beforeCreate = await portal.RptPivotReports.CountAsync();
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check(response.StatusCode == System.Net.HttpStatusCode.Redirect, "HTTP POST creates a second report with an empty Version field");
    portal.ChangeTracker.Clear();
    var second = await portal.RptPivotReports.AsNoTracking().SingleAsync(r => r.Name == "Second report");
    Check(await portal.RptPivotReports.CountAsync() == beforeCreate+1 && second.ViewName == "fatura_dokumu_103" && second.Version.Length == 8, "New report receives a database rowversion and preserves its separate view binding");
    Check((await portal.RptPivotReports.AsNoTracking().SingleAsync(r => r.ID == report.ID)).Name == "Updated fixture", "Creating a second report does not overwrite the first report");
    secondForm["Id"] = second.ID.ToString(); secondForm.Remove("Version"); secondForm["Name"] = "Missing version overwrite";
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check((int)response.StatusCode == 422 && (await response.Content.ReadAsStringAsync()).Contains("errors"), "HTTP edit still rejects a missing rowversion");
    portal.ChangeTracker.Clear();
    secondForm["Version"] = "not-base64";
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check((int)response.StatusCode == 422, "HTTP edit rejects a malformed rowversion");
    portal.ChangeTracker.Clear();
    Check((await portal.RptPivotReports.AsNoTracking().SingleAsync(r => r.ID == second.ID)).Name == "Second report", "Invalid edit tokens leave the saved second report unchanged");
    secondForm["Version"] = Convert.ToBase64String(second.Version); secondForm["Name"] = "Second report edited";
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check(response.StatusCode == System.Net.HttpStatusCode.Redirect, "HTTP edit accepts the current rowversion");
    portal.ChangeTracker.Clear(); secondForm["Name"] = "Stale HTTP overwrite";
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check((int)response.StatusCode == 422, "HTTP edit rejects a stale rowversion after another save");
    portal.ChangeTracker.Clear();
    Check((await portal.RptPivotReports.AsNoTracking().SingleAsync(r => r.ID == second.ID)).Name == "Second report edited", "HTTP concurrency protection preserves the latest report definition");
    secondForm["Id"] = "0"; secondForm["Name"] = "Third report"; secondForm.Remove("Version");
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check(response.StatusCode == System.Net.HttpStatusCode.Redirect, "HTTP create also accepts an omitted Version field");
    portal.ChangeTracker.Clear(); var afterCreates = await portal.RptPivotReports.CountAsync(); secondForm["Name"] = "";
    using (var response = await client.PostAsync(baseUrl + "/PivotRaporlar/Duzenle", new FormUrlEncodedContent(secondForm)))
        Check((int)response.StatusCode == 422 && (await response.Content.ReadAsStringAsync()).Contains("Name"), "Required report name validation is not weakened");
    Check(await portal.RptPivotReports.CountAsync() == afterCreates, "Invalid create requests insert no report");
    await using (var scope = host.Services.CreateAsyncScope()) {
        var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider }; http.Request.Scheme = "http"; http.Request.Host = new HostString("localhost");
        var editHtml = await Render(http, "Duzenle", new PivotEditVM { Id = report.ID, SettingsJson = JsonSerializer.Serialize(settings) });
        Check(editHtml.Contains("pvAllowed") && editHtml.Contains("pvSettings") && editHtml.Contains("pvEditForm"), "Compiled manager form contains controlled field selection and hidden layout");
    }
    if (args.Contains("--preview")) {
        Console.WriteLine("Fixture preview: " + baseUrl + "/preview");
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5)); await host.WaitForShutdownAsync(timeout.Token);
    }
    await host.StopAsync();
    await connection.ExecuteAsync("INSERT dbo.PivotFixture(Region,Currency,Amount) SELECT TOP (501) CONCAT('R',ROW_NUMBER() OVER(ORDER BY object_id)),'TRY',1 FROM sys.all_objects;");
    await Reject(() => service.RunAsync("dbo", "fatura_dokumu_100", settings, settings.Layout, default), "Oversized pivot is rejected, never silently truncated");
    Check(await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.PivotFixture") == 508, "SELECT-only pivot did not change ERP fixture");
    // Execute the generated migration only against this randomly named disposable local database.
    await connection.ExecuteAsync("""
        DROP TABLE dbo.RptPivotReport;
        CREATE TABLE AuthorizationCategory(ID int IDENTITY PRIMARY KEY,Ad nvarchar(250),Aciklama nvarchar(1000),IsActive bit,IsDelete bit,CreateDate datetime);
        CREATE TABLE Yetkilendirme(ID int IDENTITY PRIMARY KEY,Ad nvarchar(250),Aciklama nvarchar(1000),KategoriID int,PolicyName nvarchar(250),ClaimType nvarchar(100),ClaimName nvarchar(250),IsActive bit,IsDelete bit,CreateDate datetime,UpdateDate datetime);
        IF OBJECT_ID(N'dbo.__EFMigrationsHistory') IS NULL CREATE TABLE dbo.__EFMigrationsHistory(MigrationId nvarchar(150) PRIMARY KEY,ProductVersion nvarchar(32));
        """);
    foreach (var batch in System.Text.RegularExpressions.Regex.Split(migrationSql, @"^GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline))
        if (!string.IsNullOrWhiteSpace(batch)) await connection.ExecuteAsync(batch);
    Check(await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.RptPivotReport WHERE ViewName=N'fatura_dokumu_100' AND IsActive=0 AND DATALENGTH(Version)=8") == 1, "Generated migration applies cleanly and seeds one passive rowversion-protected report");
    Check(await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Yetkilendirme WHERE ClaimType=N'Rapor' AND ClaimName IN(N'PivotRaporlar',N'PivotRaporlarYonet')") == 2, "Generated migration installs both report permissions");
} finally {
    if (created) { await using var clean = new SqlConnection(new SqlConnectionStringBuilder(cs) { InitialCatalog = "master" }.ConnectionString); await clean.OpenAsync(); SqlConnection.ClearAllPools(); await clean.ExecuteAsync($"ALTER DATABASE [{database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{database}];"); Console.WriteLine("Removed isolated test database " + database); }
}
static async Task<string> Render(HttpContext http, string actionName, object model) {
    if (http.GetEndpoint() == null) http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Fixture render"));
    var route = new RouteData(); route.Values["controller"] = "PivotRaporlar"; route.Values["action"] = actionName;
    var action = new ActionContext(http, route, new ActionDescriptor(), new ModelStateDictionary());
    var engine = http.RequestServices.GetRequiredService<IRazorViewEngine>(); var view = engine.GetView(null, "/Views/PivotRaporlar/" + actionName + ".cshtml", false);
    Check(view.Success, "Compiled " + actionName + " view found");
    using var writer = new StringWriter(); var data = new ViewDataDictionary(new EmptyModelMetadataProvider(), action.ModelState) { Model = model };
    await view.View.RenderAsync(new ViewContext(action, view.View, data, new TempDataDictionary(http, new MemoryTempData()), writer, new HtmlHelperOptions()));
    return writer.ToString();
}
sealed class PivotContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b) {
        foreach(var type in b.Model.GetEntityTypes().Select(t=>t.ClrType).Where(t=>t!=typeof(RptPivotReport)).ToList()) b.Ignore(type);
        b.Entity<RptPivotReport>();
    }
}
sealed class MemoryTempData : ITempDataProvider { public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>(); public void SaveTempData(HttpContext context, IDictionary<string, object> values) { } }
// Fixture only: expose validation errors without loading unrelated production layout components.
sealed class FixtureFormErrors : IAsyncResultFilter {
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next) {
        if (context.HttpContext.Request.Method == "POST" && context.Result is ViewResult && !context.ModelState.IsValid)
            context.Result = new JsonResult(new { errors = context.ModelState.Where(e => e.Value?.Errors.Count > 0).ToDictionary(e => e.Key, e => e.Value!.Errors.Select(error => error.ErrorMessage).ToArray()) }) { StatusCode = 422 };
        await next();
    }
}
sealed class FixtureAuth(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
    protected override Task<AuthenticateResult> HandleAuthenticateAsync() => Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "Fixture"), new Claim(ClaimTypes.Role, "Admin")], Scheme.Name)), Scheme.Name)));
}
