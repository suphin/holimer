using System.Net;
using System.Reflection;
using System.Security.Claims;
using Ekomers.Data;
using Ekomers.Data.Migrations;
using Ekomers.Data.Services.Profitability;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Ekomers.Web.Controllers;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

static void Check(bool value, string message) { if (!value) throw new Exception("FAIL: " + message); Console.WriteLine("PASS " + message); }
var presets = AccountingExpenseSuggestions.Presets;
Check(presets.Count == 56 && presets.Select(x => x.Key).Distinct().Count() == 56 && presets.Select(x => x.Name).Distinct().Count() == 56, "56 unique preset keys/names");
Check(presets.All(x => x.Key.Length <= 80 && x.Name.Length <= 150), "Preset field bounds");
Check(presets.All(x => x.Stage == (x.Key.StartsWith("finance.") ? RptExpenseStage.Financing : RptExpenseStage.Operating)), "Finance is a separate stage");
var cases = new (string Code, string Name, string? Key, string Category)[] {
    ("730.01", "FABRİKA KİRA GİDERLERİ", "factory.rent.fixed", "Fabrika / Kira"),
    ("770.01", "OFIS KIRASI", "operating.office.fixed", "Operasyon / Ofis"),
    ("730.02", "MAKİNE AMORTİSMANLARI", "factory.depreciation.fixed", "Fabrika / Amortisman"),
    ("730.03", "Elektrik ve su", null, "Fabrika / Enerji"),
    ("720.01", "ÜRETİM PERSONEL ÜCRETLERİ", null, "Fabrika / Direkt işçilik"),
    ("760.01", "SATIŞ PERSONELİ SGK PRİMİ", null, "Operasyon / Satış personeli"),
    ("730.04", "Fason dolum", null, "Fabrika / Fason"),
    ("760.02", "Kargo ve nakliye", null, "Operasyon / Lojistik"),
    ("730.05", "Nakliye", null, "Fabrika / Tedarik"),
    ("780.01", "Kredi faizi", null, "Finansman / Faiz"),
    ("780.02", "Banka komisyon giderleri", null, "Finansman / Komisyon"),
    ("780.03", "Kur farkı", null, "Finansman / Kur"),
    ("780.04", "Faiz ve komisyon", null, "Finansman · inceleme"),
    ("750.01", "Ürün geliştirme", null, "Operasyon / Ar-Ge"),
    ("740.01", "Kira", null, "Hizmet üretimi"),
    ("770.02", "Araç kira ve yakıt", null, "Operasyon / Taşıt"),
    ("730.06", "Kira ve elektrik", null, "Birden fazla"),
    ("730.07", "Yemek personel giderleri", null, "Fabrika / Yemek"),
    ("770.03", "Diğer giderler", null, "İnceleme gerekli"),
    ("770.04", "Kiraz üretimi", null, "İnceleme gerekli"),
    ("710.01", "Hammadde kira", null, "Kapsam dışı"),
    ("731.01", "Kira yansıtma", null, "Kapsam dışı"),
    ("791.01", "İşçi ücret", null, "Kapsam dışı")
};
foreach (var test in cases) { var result = AccountingExpenseSuggestions.Suggest(test.Code, test.Name); Check(result.PresetKey == test.Key && result.Category.StartsWith(test.Category), "Proposal: " + test.Code + " " + test.Name); }
Check(AccountingExpenseSuggestions.Normalize("KİRA / ŞİŞE-ÇÖĞÜ") == "kira sise cogu", "Turkish normalization");
foreach (var action in new[] { "HazirGruplariOlustur", "OnerileriOnayla" }) {
    var method = typeof(SatisKarlilikGiderleriController).GetMethod(action)!;
    Check(method.IsDefined(typeof(HttpPostAttribute)) && method.IsDefined(typeof(ValidateAntiForgeryTokenAttribute)) && method.GetCustomAttribute<AuthorizeAttribute>()?.Policy == ReportPermissions.MuhasebeGiderleriYonet, action + " has POST/CSRF/expense-management authorization");
}
Check(typeof(SatisKarlilikGiderleriController).GetMethod("Oneriler")!.GetCustomAttribute<AuthorizeAttribute>()?.Policy == ReportPermissions.MuhasebeGiderleriYonet, "Preview requires expense-management permission");
if (!args.Contains("--sqlserver")) { Console.WriteLine("Pure proposal tests complete; add --sqlserver for isolated migration/controller/Razor integration tests."); return; }

using (var metadata = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=__METADATA_ONLY_NEVER_OPEN__;Integrated Security=true").Options))
    Check(!metadata.Database.HasPendingModelChanges(), "Generated snapshot matches current model without opening a connection");

var prefix = "CodexExpenseSuggestionsTest_"; var name = prefix + Guid.NewGuid().ToString("N");
var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\MSSQLLocalDB", InitialCatalog = name, IntegratedSecurity = true, TrustServerCertificate = true }.ConnectionString;
var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options; var created = false;
try {
    await using (var baseline = new Baseline(options)) { await baseline.Database.EnsureCreatedAsync(); created = true; }
    await using var db = new EditContext(options);
    // Run real first migration, insert a pre-existing group, then real additive preset migration.
    var first = new AddAccountingExpenseClassification { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" };
    foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(first.UpOperations)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
    await db.Database.ExecuteSqlRawAsync("INSERT dbo.RptAccountingExpenseGroup(Name,Behavior,Stage,IsActive,IsDelete) VALUES(N'Legacy group',1,1,1,0)");
    var addition = new AddAccountingExpensePresets { ActiveProvider = "Microsoft.EntityFrameworkCore.SqlServer" };
    Check(addition.UpOperations.Count == 2 && addition.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.AddColumnOperation>().Single().Name == "PresetKey", "Migration only adds nullable key and unique index");
    foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(addition.UpOperations)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
    Check((await db.RptAccountingExpenseGroups.SingleAsync()).Name == "Legacy group" && (await db.RptAccountingExpenseGroups.SingleAsync()).PresetKey == null, "Migration preserves existing group");
    await using var logo = new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(cs).Options);
    var source = new FakeExpenseService();
    var start = new DateTime(2026, 1, 1); var end = new DateTime(2026, 12, 31);
    SatisKarlilikGiderleriController Make(EditContext? target = null) => new(target ?? db, logo, source) { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "fixture-admin"), new Claim(ClaimTypes.Role, "Admin")], "Test")) } }, TempData = new TempDataDictionary(new DefaultHttpContext(), new MemoryTempData()) };
    var controller = Make();
    await controller.HazirGruplariOlustur(start, end, default); db.ChangeTracker.Clear();
    var seededCount = await db.RptAccountingExpenseGroups.CountAsync();
    Check(seededCount == 57 && await db.RptAccountingExpenseRules.CountAsync() == 0, "Seed creates categories only, never mappings");
    await controller.HazirGruplariOlustur(start, end, default);
    Check(await db.RptAccountingExpenseGroups.CountAsync() == seededCount, "Seed is idempotent");
    var rent = await db.RptAccountingExpenseGroups.SingleAsync(x => x.PresetKey == "factory.rent.fixed");
    rent.Name = "Renamed factory rent"; await db.SaveChangesAsync();
    await controller.HazirGruplariOlustur(start, end, default); db.ChangeTracker.Clear();
    Check(await db.RptAccountingExpenseGroups.CountAsync() == seededCount && (await db.RptAccountingExpenseGroups.SingleAsync(x => x.ID == rent.ID)).Name == "Renamed factory rent", "Renaming a preset does not duplicate/revert it");
    var draft = (AccountingExpenseSuggestionsVM)((ViewResult)await controller.Oneriler(start, end, ct: default)).Model!;
    Check(draft.ProtectedCount == 2 && draft.Rows.Count == 4 && draft.Rows.Single(x => x.AccountCode == "730.01").SuggestedGroupId == rent.ID, "Preview filters protected codes and uses stable renamed preset");
    Check(draft.Rows.Single(x => x.AccountCode == "730.02").SuggestedGroupId == null && await db.RptAccountingExpenseRules.CountAsync() == 0, "Ambiguous accounts stay unselected; GET does not save");
    AccountingExpenseSuggestionApprovalVM Input(params (string Code, int? Group, bool Selected)[] rows) => new() { ValidFrom = start, Rows = rows.Select(x => new AccountingExpenseSuggestionSelectionVM { AccountCode = x.Code, ExpenseGroupId = x.Group, Selected = x.Selected }).ToList() };
    await controller.OnerileriOnayla(Input(("730.01", rent.ID, true), ("730.02", rent.ID, false)), start, end);
    Check(await db.RptAccountingExpenseRules.CountAsync() == 1 && (await db.RptAccountingExpenseRules.SingleAsync()).AccountCode == "730.01", "Only selected account saved");
    var old = await db.RptAccountingExpenseRules.SingleAsync(); var ruleId = old.ID;
    var variable = await db.RptAccountingExpenseGroups.SingleAsync(x => x.PresetKey == "factory.energy.variable");
    var repeated = Input(("730.01", variable.ID, true)); repeated.ValidFrom = start.AddMonths(6);
    await controller.OnerileriOnayla(repeated, start, end); db.ChangeTracker.Clear();
    old = await db.RptAccountingExpenseRules.SingleAsync();
    Check(old.ID == ruleId && old.ExpenseGroupId == rent.ID && old.ValidTo == null && old.ValidFrom == start, "Repeated/stale approval never closes/overwrites an existing rule");
    await controller.OnerileriOnayla(Input(("730.02", variable.ID, true), ("730.03", null, true)), start, end); await db.SaveChangesAsync();
    Check(await db.RptAccountingExpenseRules.CountAsync() == 1, "Invalid bulk selection leaves no partial writes");
    await controller.OnerileriOnayla(Input(("710.01", rent.ID, true)), start, end);
    await controller.OnerileriOnayla(Input(("730.99", rent.ID, true)), start, end);
    await controller.OnerileriOnayla(Input(("730.02", rent.ID, true), ("730.02", variable.ID, true)), start, end);
    Check(await db.RptAccountingExpenseRules.CountAsync() == 1, "Protected/forged/duplicate codes rejected server-side");
    await controller.OnerileriOnayla(Input(("730.02", variable.ID, true)), start, end);
    Check((await db.RptAccountingExpenseRules.SingleAsync(x => x.AccountCode == "730.02")).ExpenseGroupId == variable.ID, "User can choose a group for a review account");
    draft = (AccountingExpenseSuggestionsVM)((ViewResult)await controller.Oneriler(start, end, ct: default)).Model!;
    Check(draft.AlreadyMappedCount == 2 && draft.Rows.Count == 2, "Existing assignments excluded from preview");
    // Even an old/future rule must be left to the existing date-versioning editor.
    var future = new RptAccountingExpenseRule { FirmNumber = 100, AccountCode = "770.01", ExpenseGroupId = rent.ID, ValidFrom = new DateTime(2030, 1, 1), IsActive = false, IsDelete = false };
    db.Add(future); await db.SaveChangesAsync();
    draft = (AccountingExpenseSuggestionsVM)((ViewResult)await controller.Oneriler(start, end, ct: default)).Model!;
    Check(draft.AlreadyMappedCount == 3 && draft.Rows.All(x => x.AccountCode != "770.01"), "Future/inactive rules also protected from wizard overwrite");
    future.IsDelete = true; await db.SaveChangesAsync();
    // Independent scoped contexts and serializable isolation: the second approval must skip/reject, never duplicate.
    await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => { await using var scoped = new EditContext(options); await Make(scoped).OnerileriOnayla(Input(("730.03", rent.ID, true)), start, end); }));
    db.ChangeTracker.Clear(); Check(await db.RptAccountingExpenseRules.CountAsync(x => x.AccountCode == "730.03") == 1, "Concurrent approval has one surviving rule");
    var archived = await db.RptAccountingExpenseGroups.SingleAsync(x => x.PresetKey == "operating.office.fixed");
    archived.IsActive = false; await db.SaveChangesAsync();
    await controller.OnerileriOnayla(Input(("770.01", archived.ID, true)), start, end);
    Check(!await db.RptAccountingExpenseRules.AnyAsync(x => x.AccountCode == "770.01" && x.IsDelete != true), "Stale inactive group rejected without restoring an archived rule");
    Check(await db.UserActivityLog.AnyAsync(x => x.ActionName == "GiderOnerileriniOnayla") && await db.UserActivityLog.AnyAsync(x => x.ActionName == "HazirGiderGruplariOlustur"), "Seed and approvals audited");
    Check(await db.UserActivityLog.CountAsync(x => x.ActionName == "GiderOnerisiOnayla") == 3, "Each added rule has its own complete audit entry");
    // Existing name compatibility and idempotence with concurrent installations.
    var adoption = await db.RptAccountingExpenseGroups.SingleAsync(x => x.PresetKey == "factory.maintenance.fixed"); adoption.PresetKey = null;
    var conflict = await db.RptAccountingExpenseGroups.SingleAsync(x => x.PresetKey == "factory.maintenance.variable"); conflict.PresetKey = null; conflict.Behavior = RptExpenseBehavior.Fixed;
    await db.SaveChangesAsync();
    await controller.HazirGruplariOlustur(start, end, default); db.ChangeTracker.Clear();
    Check((await db.RptAccountingExpenseGroups.SingleAsync(x => x.ID == adoption.ID)).PresetKey == "factory.maintenance.fixed", "Compatible existing-name group adopted without duplication");
    conflict = await db.RptAccountingExpenseGroups.SingleAsync(x => x.ID == conflict.ID);
    Check(conflict.PresetKey == null && conflict.Behavior == RptExpenseBehavior.Fixed, "Incompatible same-name group is not changed");
    await Task.WhenAll(Enumerable.Range(0, 2).Select(async _ => { await using var scoped = new EditContext(options); await Make(scoped).HazirGruplariOlustur(start, end, default); }));
    db.ChangeTracker.Clear(); Check(await db.RptAccountingExpenseGroups.CountAsync() == seededCount, "Concurrent seed remains idempotent");
    // A no-configuration MVC host renders compiled Razor only; portal startup/background jobs are not invoked.
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ApplicationName = typeof(SatisKarlilikGiderleriController).Assembly.GetName().Name, EnvironmentName = "Development", Args = [] });
    builder.Logging.ClearProviders(); builder.Services.AddControllersWithViews().AddApplicationPart(typeof(SatisKarlilikGiderleriController).Assembly); builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
    await using var host = builder.Build(); host.MapDefaultControllerRoute();
    draft.Rows = [new() { AccountCode = "730.01", AccountName = "<script>unsafe</script>", Category = "Kira", SuggestedGroupId = rent.ID }];
    var html = await Render(host, draft);
    Check(html.Contains("&lt;script&gt;unsafe&lt;/script&gt;") && !html.Contains("<script>unsafe</script>"), "Razor encodes ERP names");
    Check(html.Contains("__RequestVerificationToken") && html.Contains("Rows[0].ExpenseGroupId") && !html.Contains("checked=\"checked\""), "Razor has CSRF, indexed fields and no prechecked approvals");
    Check(WebUtility.HtmlDecode(html).Contains("Renamed factory rent"), "Renamed group displayed in preview");
    foreach (var command in db.GetService<IMigrationsSqlGenerator>().Generate(addition.DownOperations)) await db.Database.ExecuteSqlRawAsync(command.CommandText);
    // Query without the removed property: no group/rule/financial data should be deleted by Down.
    await using (var connection = new SqlConnection(cs)) { await connection.OpenAsync(); await using var command = new SqlCommand("SELECT COUNT(*) FROM dbo.RptAccountingExpenseRule", connection); Check((int)(await command.ExecuteScalarAsync())! == 4, "Migration Down preserves saved groups/rules including archive"); }
} finally {
    if (created && name.StartsWith(prefix) && Guid.TryParseExact(name[prefix.Length..], "N", out _)) { SqlConnection.ClearAllPools(); await using var cleanup = new Baseline(options); await cleanup.Database.EnsureDeletedAsync(); Console.WriteLine("Removed isolated test database: " + name); }
}

static async Task<string> Render(WebApplication host, AccountingExpenseSuggestionsVM model) {
    await using var scope = host.Services.CreateAsyncScope();
    var http = new DefaultHttpContext { RequestServices = scope.ServiceProvider }; http.Request.Scheme = "http"; http.Request.Host = new HostString("localhost"); http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Render"));
    var route = new RouteData(); route.Values["controller"] = "SatisKarlilikGiderleri"; route.Values["action"] = "Oneriler";
    var action = new ActionContext(http, route, new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(), new ModelStateDictionary());
    var view = scope.ServiceProvider.GetRequiredService<IRazorViewEngine>().GetView(null, "/Views/SatisKarlilikGiderleri/Oneriler.cshtml", false);
    Check(view.Success, "Compiled suggestion view found"); using var writer = new StringWriter();
    await view.View.RenderAsync(new ViewContext(action, view.View, new ViewDataDictionary(new EmptyModelMetadataProvider(), action.ModelState) { Model = model }, new TempDataDictionary(http, new MemoryTempData()), writer, new HtmlHelperOptions())); return writer.ToString();
}
sealed class EditContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options) { protected override void OnModelCreating(ModelBuilder b) => Setup.Configure(b, true); }
sealed class Baseline(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options) { protected override void OnModelCreating(ModelBuilder b) => Setup.Configure(b, false); }
static class Setup { public static void Configure(ModelBuilder b, bool expenses) {
    var types = new HashSet<Type> { typeof(UserActivityLog) }; if (expenses) { types.Add(typeof(RptAccountingExpenseGroup)); types.Add(typeof(RptAccountingExpenseRule)); }
    foreach (var t in b.Model.GetEntityTypes().Select(x => x.ClrType).Where(x => !types.Contains(x)).ToList()) b.Ignore(t);
    foreach (var t in types) { b.Entity(t).HasBaseType((Type?)null); b.Entity(t).HasKey(t == typeof(UserActivityLog) ? "Id" : "ID"); b.Entity(t).ToTable(t.Name); }
} }
sealed class FakeExpenseService : IAccountingExpenseService {
    public Task<AccountingExpenseSummaryVM> GetSummaryAsync(DateTime s, DateTime e, CancellationToken ct) => Task.FromResult(new AccountingExpenseSummaryVM { StartDate = s, EndDate = e,
        Accounts = [new() { AccountCode = "730.01", AccountName = "Fabrika kira", CanMap = true }, new() { AccountCode = "730.02", AccountName = "Elektrik", CanMap = true },
            new() { AccountCode = "730.03", AccountName = "Diğer", CanMap = true }, new() { AccountCode = "770.01", AccountName = "Ofis kira", CanMap = true },
            new() { AccountCode = "710.01", AccountName = "Hammadde", CanMap = false }, new() { AccountCode = "731.01", AccountName = "Yansıtma", CanMap = false }] });
    public Task<AccountingExpenseMovementsVM> GetMovementsAsync(string c, DateTime s, DateTime e, int p, CancellationToken ct) => throw new NotSupportedException();
}
sealed class MemoryTempData : ITempDataProvider { public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>(); public void SaveTempData(HttpContext context, IDictionary<string, object> values) { } }
