using Ekomers.Data;
using Ekomers.Data.Services.Profitability;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
var date = new DateTime(2026, 10, 10);
var scenarios = new[] { "recipe", "manual", "future", "expired", "draft", "closed", "currency", "manual-zero", "recipe-zero", "deleted-header", "deleted-line", "inactive", "alias", "unit-mismatch", "kg", "gram", "litre", "generic", "scoped", "inverse", "conflicting", "ambiguous", "identical", "saved-version-deleted", "historical", "invalid-manual-blocks-recipe", "legacy-bulk", "legacy-bulk-no-recipe", "bulk-material" };
foreach (var scenario in scenarios)
{
    await using var connection = new SqliteConnection("Data Source=:memory:"); await connection.OpenAsync();
    await using var db = new CostContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite(connection).Options);
    await db.Database.EnsureCreatedAsync();
    var seed = await Seed(db, "SKU", 12);
    string salesUnit = "PCS";
    var manual = new RptProductCostVersion { LogoMaterialRef = 1, ProductCode = "SKU", VersionNumber = 1, Status = RptProductCostVersionStatus.Active, ValidFrom = date.AddDays(-2), MaterialCost = 5, LaborCost = 20, FreightCost = 30, OverheadCost = 40, OtherCost = 50, TotalUnitCostTry = 145, ExchangeRate = 1, UnitCode = "ADET" };
    if (scenario is "manual" or "expired" or "draft" or "closed" or "currency" or "manual-zero" or "inactive" or "invalid-manual-blocks-recipe" or "legacy-bulk" or "legacy-bulk-no-recipe" or "bulk-material") db.Add(manual);
    switch (scenario)
    {
        case "future": seed.Scenario.CalculationDate = date.AddDays(1); break;
        case "expired": manual.ValidTo = date.AddDays(-1); break;
        case "draft": manual.Status = RptProductCostVersionStatus.Draft; break;
        case "closed": manual.Status = RptProductCostVersionStatus.Closed; manual.ValidTo = date; break;
        case "currency": manual.CurrencyCode = "USD"; manual.ExchangeRate = 35; manual.TotalUnitCostTry = 175; break;
        case "manual-zero": manual.MaterialCost = 0; break;
        case "recipe-zero": seed.Line.RawMaterialUnitCost = 0; seed.Line.PackagingUnitCost = 0; break;
        case "deleted-header": seed.Scenario.IsDelete = true; break;
        case "deleted-line": seed.Line.IsDelete = true; break;
        case "inactive": manual.IsActive = false; break;
        case "alias": seed.Material.Code = "LOCAL"; seed.Material.LogoCode = " SKU "; seed.Line.ProductCode = "LOCAL"; break;
        case "unit-mismatch": salesUnit = "KOLI"; break;
        case "kg": salesUnit = "KG"; seed.Line.ProductionUnit = "Gram"; break;
        case "gram": salesUnit = "GR"; seed.Line.ProductionUnit = "Kilogram"; break;
        case "litre": salesUnit = "Litre"; seed.Line.ProductionUnit = "ML"; break;
        case "generic": salesUnit = "KOLI"; await Rule(db, null, "KOLI", "ADET", 10); break;
        case "scoped": salesUnit = "KOLI"; await Rule(db, null, "KOLI", "ADET", 10); await Rule(db, seed.Material.ID, "KOLI", "ADET", 24); break;
        case "inverse": salesUnit = "KOLI"; await Rule(db, seed.Material.ID, "ADET", "KOLI", .05m); break;
        case "conflicting": salesUnit = "KOLI"; await Rule(db, null, "KOLI", "ADET", 10); await Rule(db, null, "KOLI", "ADET", 20); break;
        case "ambiguous": case "identical":
            db.Add(new PrdRecipeCostScenarioLine { ScenarioId = seed.Scenario.ID, RecipeVersionId = seed.Version.ID, ProductCode = "SKU", ProductionUnit = "ADET", ProductionQuantity = 100, RawMaterialUnitCost = scenario == "identical" ? 10 : 11, PackagingUnitCost = 2, TotalUnitCost = 500 }); break;
        case "saved-version-deleted": seed.Version.IsDelete = true; break;
        case "historical":
            var newer = await Seed(db, "SKU", 99); newer.Scenario.CalculationDate = date.AddDays(1); break;
        case "invalid-manual-blocks-recipe": manual.UnitCode = "PAKET"; break;
        case "legacy-bulk": manual.Source = "BulkTotal"; break;
        case "legacy-bulk-no-recipe": manual.Source = "BulkTotal"; seed.Scenario.IsDelete = true; break;
        case "bulk-material": manual.Source = "BulkMaterial"; break;
    }
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    var catalog = await SalesProfitabilityCostCatalog.LoadAsync(db, [new(1, "SKU")], date.AddDays(-30), date.AddDays(30), CancellationToken.None);
    var resolved = catalog.Resolve(1, " sku ", salesUnit, date);
    decimal? expected = scenario switch {
        "manual" or "closed" or "bulk-material" => 5, "currency" => 175, "manual-zero" => 0,
        "future" or "recipe-zero" or "deleted-header" or "deleted-line" or "unit-mismatch" or "conflicting" or "ambiguous" or "invalid-manual-blocks-recipe" or "legacy-bulk-no-recipe" => null,
        "kg" or "litre" => 12000, "gram" => .012m, "generic" => 120, "scoped" => 288, "inverse" => 240, _ => 12 };
    Check(resolved.UnitCostTry == expected, scenario + " cost/source selection");
    Check(db.ChangeTracker.Entries().Count() == 0, "Read-only untracked catalog");
    if (expected.HasValue) Check(resolved.Source == (scenario is "manual" or "closed" or "currency" or "manual-zero" or "bulk-material" ? "Manual" : "Recipe"), "Source displayed");
    Console.WriteLine("PASS " + scenario);
}
Console.WriteLine($"PASS {scenarios.Length} isolated SQLite cost-selection scenarios");

if (args.Contains("--sqlserver"))
{
    var name = "CodexSalesCostsTest_" + Guid.NewGuid().ToString("N");
    var cs = new SqlConnectionStringBuilder { DataSource = "(localdb)\\MSSQLLocalDB", InitialCatalog = name, IntegratedSecurity = true, TrustServerCertificate = true }.ConnectionString;
    var created = false;
    try
    {
        await using var db = new CostContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);
        await db.Database.EnsureCreatedAsync(); created = true;
        await Seed(db, "B", 12);
        var e = await Seed(db, "E", 2); await Rule(db, e.Material.ID, "KOLI", "ADET", 24);
        var c = await Seed(db, "C", 99); c.Scenario.CalculationDate = new DateTime(2026, 1, 11);
        db.AddRange(new RptProductCostVersion { LogoMaterialRef = 1, ProductCode = "A", VersionNumber = 1, Status = RptProductCostVersionStatus.Active, ValidFrom = new DateTime(2026, 1, 1), UnitCode = "Adet", MaterialCost = 5, TotalUnitCostTry = 500, ExchangeRate = 1 },
            new RptProductCostVersion { LogoMaterialRef = 4, ProductCode = "D", VersionNumber = 1, Status = RptProductCostVersionStatus.Active, ValidFrom = new DateTime(2026, 1, 1), UnitCode = "Adet", MaterialCost = 4, TotalUnitCostTry = 400, ExchangeRate = 1 });
        await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        await db.Database.ExecuteSqlRawAsync("""
            CREATE TABLE dbo.SalesFixture (MalzemeRef int, SatirTuru nvarchar(20), FaturaTarihi datetime2, FaturaNo nvarchar(30), FaturaRef int, FaturaSatirRef int, IadeMi int, CariKod nvarchar(20), CariUnvan nvarchar(100), MalzemeKod nvarchar(30), MalzemeAdi nvarchar(100), Miktar decimal(18,6), Birim nvarchar(30), ReferansFiyatBaslangicTarihi datetime2, ReferansBirimFiyatKdvHaricTL decimal(18,6), ReferansBrutCiroTL decimal(18,6), GerceklesenNetTutarKdvHaricTL decimal(18,6), HesaplananIskontoTutarTL decimal(18,6), HesaplananIskontoOrani decimal(18,6), FiyatKarsilastirmaDurumu nvarchar(40), RaporaDahilMi int);
            """);
        await db.Database.ExecuteSqlRawAsync("CREATE VIEW dbo.VW_RPT_SATIS_KARLILIK_100 AS SELECT * FROM dbo.SalesFixture");
        var fixtures = new[] { (1, "A", 10m, "ADET", 100m, "Malzeme"), (2, "B", 3m, "Adet", 90m, "Malzeme"), (2, "B", -2m, "Adet", -60m, "Malzeme"), (3, "C", 1m, "Adet", 50m, "Malzeme"), (4, "D", 1m, "KOLI", 40m, "Malzeme"), (5, "E", 2m, "KOLI", 100m, "Malzeme"), (5, "E", 3m, "ADET", 10m, "Malzeme"), (0, "H", 1m, "ADET", 20m, "Hizmet") };
        int id = 0;
        foreach (var (reference, code, quantity, unit, revenue, type) in fixtures)
        {
            id++;
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO dbo.SalesFixture VALUES ({reference},{type},{new DateTime(2026,1,10)},N'INV',{id},{id},{(quantity<0?1:0)},N'C1',N'Customer',{code},{code},{quantity},{unit},{new DateTime(2026,1,1)},{10m},{revenue*1.25m},{revenue},{revenue*.25m},{20m},N'HESAPLANABILIR',1)");
        }
        await using var logo = new LogoContext(new DbContextOptionsBuilder<LogoContext>().UseSqlServer(cs).Options);
        var report = await new SalesProfitabilityReportService(db, logo).GetPreviewAsync(new() { StartDate = new DateTime(2026, 1, 1), EndDate = new DateTime(2026, 1, 31), PageSize = 10 }, CancellationToken.None);
        Check(report.KnownCostAmount == 164 && report.NetRevenue == 350 && report.NetProfit == 186, "Whole report cost/revenue/profit");
        Check(report.ManualCostLineCount == 1 && report.RecipeCostLineCount == 4 && report.MissingCostLineCount == 2, "Whole-filter source coverage");
        Check(report.Rows.Single(x => x.Quantity == -2).CostAmount == -24, "Return reverses cost");
        Check(report.ProductSummaries.Sum(x => x.KnownCostAmount) == 164 && report.CustomerSummaries.Sum(x => x.KnownCostAmount) == 164 && report.ChannelSummaries.Sum(x => x.KnownCostAmount) == 164 && report.SalesRepresentativeSummaries.Sum(x => x.KnownCostAmount) == 164, "All group summaries agree");
        Check(report.Rows.Where(x => x.ProductCode == "E").Sum(x => x.CostAmount) == 102, "Same product/day with different sales units");
        Check(db.ChangeTracker.Entries().Count() == 0, "Report makes no portal changes");
        Console.WriteLine("PASS SQL Server translated catalog + full profitability report, return signs, mixed units, all summaries, source counts and read-only tracking");
        await LedgerTests(db, logo);
    }
    finally
    {
        if (created && name.StartsWith("CodexSalesCostsTest_") && Guid.TryParseExact(name[20..], "N", out _))
        {
            SqlConnection.ClearAllPools();
            await using var cleanup = new CostContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs).Options);
            await cleanup.Database.EnsureDeletedAsync();
            Console.WriteLine("Removed isolated SQL test database: " + name);
        }
    }
}

static async Task LedgerTests(CostContext db, LogoContext logo)
{
    await db.Database.ExecuteSqlRawAsync("""
        CREATE TABLE dbo.LG_100_EMUHACC (LOGICALREF int PRIMARY KEY, CODE varchar(100), DEFINITION_ nvarchar(150));
        CREATE TABLE dbo.LG_100_01_EMFICHE (LOGICALREF int PRIMARY KEY, DATE_ datetime2, TRCODE int, FICHENO varchar(50), GENEXP1 nvarchar(200), CANCELLED int);
        CREATE TABLE dbo.LG_100_01_EMFLINE (LOGICALREF int PRIMARY KEY, ACCFICHEREF int, ACCOUNTREF int, DEBIT float, CREDIT float, LINEEXP nvarchar(200), CANCELLED int);
        INSERT dbo.LG_100_EMUHACC VALUES (1,'730.001',N'Rent'),(2,'760.001',N'Advertising'),(3,'780.001',N'Interest'),(4,'710.001',N'Material'),(5,'731.001',N'Reflection'),(6,'770.001',N'Unmapped');
        INSERT dbo.LG_100_01_EMFICHE VALUES (1,'20260110',4,'FIX','',0),(2,'20260110',4,'CORRECTION','',0),(3,'20260110',4,'VAR','',0),(4,'20260110',4,'FIN','',0),(5,'20260110',4,'MAT','',0),(6,'20260110',4,'REFLECTION','',0),(7,'20260110',4,'UNMAPPED','',0),(8,'20260110',7,'REVIEW','',0),(9,'20260110',4,'CANCELLED','',1),(10,'20260110',4,'CANCELLEDLINE','',0);
        INSERT dbo.LG_100_01_EMFLINE VALUES (1,1,1,1000,0,N'Rent',0),(2,2,1,0,100,N'Correction',0),(3,3,2,200,0,'',0),(4,4,3,50,0,'',0),(5,5,4,500,0,'',0),(6,6,1,0,900,'',0),(7,6,5,900,0,'',0),(8,7,6,30,0,'',0),(9,8,1,0,500,'',0),(10,9,1,777,0,'',0),(11,10,1,666,0,'',1);
        """);
    var fixedGroup = new RptAccountingExpenseGroup { Name = "Fixed", Behavior = RptExpenseBehavior.Fixed };
    var variableGroup = new RptAccountingExpenseGroup { Name = "Variable", Behavior = RptExpenseBehavior.Variable };
    var financeGroup = new RptAccountingExpenseGroup { Name = "Finance", Behavior = RptExpenseBehavior.Fixed, Stage = RptExpenseStage.Financing };
    db.AddRange(fixedGroup, variableGroup, financeGroup); await db.SaveChangesAsync();
    db.AddRange(new RptAccountingExpenseRule { AccountCode = "730.001", ExpenseGroupId = fixedGroup.ID, ValidFrom = new DateTime(2026,1,1) },
        new RptAccountingExpenseRule { AccountCode = "760.001", ExpenseGroupId = variableGroup.ID, ValidFrom = new DateTime(2026,1,1) },
        new RptAccountingExpenseRule { AccountCode = "780.001", ExpenseGroupId = financeGroup.ID, ValidFrom = new DateTime(2026,1,1) });
    await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    var service = new AccountingExpenseService(db, logo);
    var start = new DateTime(2026,1,1); var end = new DateTime(2026,1,31);
    var summary = await service.GetSummaryAsync(start, end, CancellationToken.None);
    Check(summary.OperatingFixed == 900 && summary.OperatingVariable == 200 && summary.TotalFinancing == 50, "Signed expense amounts, corrected credits, separated finance");
    Check(summary.UnmappedAccountCount == 1 && summary.UnmappedAmount == 30 && summary.ReviewMovementCount == 1 && !summary.IsComplete, "Unclassified and unreviewed vouchers block result");
    Check(summary.Accounts.Single(x => x.AccountCode == "710.001").IncludedAmount == 0 && summary.Accounts.Single(x => x.AccountCode == "731.001").IncludedAmount == 0, "No material double counting or reflection expense");
    Check(summary.ExcludedMovementCount == 5, "Cancelled lines/headers, material and both reflection counterparts excluded");
    var details = await service.GetMovementsAsync("730.001", start, end, 1, CancellationToken.None);
    Check(details.TotalCount == 6 && details.Rows.Any(x => x.Status.Contains("Yansıtma")), "Traceable paged voucher/movement detail and block reasons");
    var absent = await service.GetMovementsAsync("7' OR 1=1--", start, end, 1, CancellationToken.None);
    Check(absent.TotalCount == 0, "Parameterized account selection");
    db.Add(new RptAccountingExpenseRule { AccountCode = "770.001", ExpenseGroupId = fixedGroup.ID, ValidFrom = start }); await db.SaveChangesAsync();
    await db.Database.ExecuteSqlRawAsync("UPDATE dbo.LG_100_01_EMFICHE SET DATE_='20251231' WHERE LOGICALREF=8");
    db.ChangeTracker.Clear(); summary = await service.GetSummaryAsync(start, end, CancellationToken.None);
    Check(summary.IsComplete && summary.OperatingFixed == 930 && summary.TotalIncluded == 1180, "Complete classifications unlock totals");
    var vm = new SalesProfitabilityPreviewVM { Filter = new() { StartDate = start, EndDate = end }, NetProfit = 1500, KnownCostAmount = 1000, AccountingExpenses = summary };
    Check(vm.OperatingResult == 370 && vm.FinancingResult == 320, "Staged results subtract expenses exactly once");
    vm.Filter.Search = "Customer"; Check(vm.OperatingResult == null, "Company expenses not subtracted from filtered sales");
    vm.Filter.Search = null; vm.MissingCostLineCount = 1; Check(vm.FinancingResult == null, "Missing product costs block period results");
    vm.MissingCostLineCount = 0;
    var noData = await service.GetSummaryAsync(new DateTime(2025,1,1), new DateTime(2025,1,31), CancellationToken.None);
    Check(noData.MovementCount == 0 && !noData.IsComplete, "No data is not silently confirmed as zero expense");
    db.Add(new RptAccountingExpenseRule { AccountCode = "730.001", ExpenseGroupId = variableGroup.ID, ValidFrom = start }); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
    summary = await service.GetSummaryAsync(start, end, CancellationToken.None);
    Check(!summary.IsComplete && summary.UnmappedAccountCount == 1, "Overlapping configuration is not double counted");
    Check(db.ChangeTracker.Entries().Count() == 0, "Expense SELECTs do not track changes");
    Console.WriteLine("PASS accounting SELECT source, fixed/variable/finance, exact mapping, signed corrections, duplicates/material/reflection/cancel guards, traceability, period-result guards and overlap detection");
}

static async Task<(PrdMaterial Material, PrdRecipeVersion Version, PrdRecipeCostScenario Scenario, PrdRecipeCostScenarioLine Line)> Seed(CostContext db, string code, decimal cost)
{
    var unit = new PrdUnit { Code = "ADET", Name = "Adet" }; db.Add(unit); await db.SaveChangesAsync();
    var material = new PrdMaterial { Code = "LOCAL_" + code, LogoCode = code, Name = code, UnitId = unit.ID }; db.Add(material); await db.SaveChangesAsync();
    var recipe = new PrdRecipe { Code = "REC_" + Guid.NewGuid().ToString("N"), ProductMaterialId = material.ID }; db.Add(recipe); await db.SaveChangesAsync();
    var version = new PrdRecipeVersion { RecipeId = recipe.ID, UnitId = unit.ID, VersionNumber = 1 }; db.Add(version);
    var scenario = new PrdRecipeCostScenario { ScenarioNumber = "RM-" + Guid.NewGuid().ToString("N"), CalculationDate = new DateTime(2026, 1, 1), Name = "Saved snapshot" }; db.Add(scenario); await db.SaveChangesAsync();
    var line = new PrdRecipeCostScenarioLine { ScenarioId = scenario.ID, RecipeVersionId = version.ID, ProductCode = code, ProductionUnit = "Adet", ProductionQuantity = 100, RawMaterialUnitCost = cost * .8m, PackagingUnitCost = cost * .2m, TotalUnitCost = cost + 500, VariableCostShare = 25000, FixedCostShare = 25000 }; db.Add(line); await db.SaveChangesAsync();
    return (material, version, scenario, line);
}
static async Task Rule(CostContext db, int? materialId, string from, string to, decimal factor)
{
    var source = new PrdUnit { Code = from, Name = from }; var target = new PrdUnit { Code = to, Name = to }; db.AddRange(source, target); await db.SaveChangesAsync();
    db.Add(new PrdUnitConversion { MaterialId = materialId, FromUnitId = source.ID, ToUnitId = target.ID, Factor = factor });
}
sealed class CostContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        var allowed = new HashSet<Type> { typeof(RptProductCostVersion), typeof(PrdMaterial), typeof(PrdUnit), typeof(PrdRecipe), typeof(PrdRecipeVersion), typeof(PrdRecipeCostScenario), typeof(PrdRecipeCostScenarioLine), typeof(PrdUnitConversion), typeof(RptAccountingExpenseGroup), typeof(RptAccountingExpenseRule) };
        foreach (var type in b.Model.GetEntityTypes().Select(x => x.ClrType).Where(x => !allowed.Contains(x)).ToList()) b.Ignore(type);
        foreach (var type in allowed)
        {
            b.Entity(type).HasBaseType((Type?)null); b.Entity(type).HasKey("ID"); b.Entity(type).ToTable(type.Name);
            foreach (var p in b.Entity(type).Metadata.GetProperties().Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?))) { p.SetPrecision(18); p.SetScale(6); }
        }
    }
}
