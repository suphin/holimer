using Ekomers.Models.Entity.Profitability;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services.Profitability;

public sealed record SalesCostProduct(int LogoMaterialRef, string? ProductCode);
public sealed record SalesCostResolution(decimal? UnitCostTry, string Status, string? Source = null, string? Description = null, int? ScenarioId = null);

/// <summary>Read-only, date-aware selection of manual costs and saved recipe snapshots. Never recalculates or publishes costs.</summary>
public sealed class SalesProfitabilityCostCatalog
{
    private readonly Dictionary<int, List<RptProductCostVersion>> _manual;
    private readonly Dictionary<string, List<RecipeCost>> _recipes;
    private readonly Dictionary<string, int> _materialIds;
    private readonly List<Conversion> _conversions;

    private SalesProfitabilityCostCatalog(List<RptProductCostVersion> manual, List<RecipeCost> recipes,
        Dictionary<string, int> materialIds, List<Conversion> conversions)
    {
        _manual = manual.GroupBy(x => x.LogoMaterialRef).ToDictionary(x => x.Key,
            x => x.OrderByDescending(y => y.ValidFrom).ThenByDescending(y => y.VersionNumber).ThenByDescending(y => y.ID).ToList());
        _recipes = recipes.GroupBy(x => Key(x.ProductCode)).ToDictionary(x => x.Key,
            x => x.OrderByDescending(y => y.CalculationDate).ThenByDescending(y => y.ScenarioId).ThenByDescending(y => y.LineId).ToList());
        _materialIds = materialIds;
        _conversions = conversions;
    }

    public static async Task<SalesProfitabilityCostCatalog> LoadAsync(ApplicationDbContext context,
        IEnumerable<SalesCostProduct> products, DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var requested = products.Distinct().ToList();
        var refs = requested.Select(x => x.LogoMaterialRef).Where(x => x > 0).Distinct().ToList();
        var codes = requested.Select(x => Key(x.ProductCode)).Where(x => x.Length > 0).Distinct().ToList();
        var endExclusive = endDate.Date.AddDays(1);
        var manual = refs.Count == 0 ? [] : await context.RptProductCostVersions.AsNoTracking()
            .Where(x => refs.Contains(x.LogoMaterialRef) && x.IsDelete != true && x.IsActive != false &&
                (x.Status == RptProductCostVersionStatus.Active || x.Status == RptProductCostVersionStatus.Closed) &&
                x.ValidFrom < endExclusive && (x.ValidTo == null || x.ValidTo >= startDate.Date))
            .ToListAsync(ct);
        var recipes = codes.Count == 0 ? [] : await (
            from line in context.PrdRecipeCostScenarioLines.AsNoTracking()
            join scenario in context.PrdRecipeCostScenarios.AsNoTracking() on line.ScenarioId equals scenario.ID
            join version in context.PrdRecipeVersions.AsNoTracking() on line.RecipeVersionId equals version.ID
            join recipe in context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID
            join product in context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID
            where line.IsDelete != true && line.IsActive != false && scenario.IsDelete != true && scenario.IsActive != false &&
                scenario.CalculationDate < endExclusive &&
                (codes.Contains(line.ProductCode.Trim().ToUpper()) ||
                 (product.LogoCode != null && codes.Contains(product.LogoCode.Trim().ToUpper())))
            select new RecipeCost
            {
                LineId = line.ID, ScenarioId = scenario.ID, ScenarioNumber = scenario.ScenarioNumber,
                CalculationDate = scenario.CalculationDate,
                ProductCode = product.LogoCode != null && product.LogoCode.Trim() != "" ? product.LogoCode : line.ProductCode,
                Unit = line.ProductionUnit, ProductionQuantity = line.ProductionQuantity,
                RawMaterialCost = line.RawMaterialUnitCost, PackagingCost = line.PackagingUnitCost,
                UnitCostTry = line.RawMaterialUnitCost + line.PackagingUnitCost
            }).ToListAsync(ct);
        var materials = codes.Count == 0 ? [] : await context.PrdMaterials.AsNoTracking()
            .Where(x => x.IsDelete != true && (codes.Contains(x.Code.Trim().ToUpper()) ||
                (x.LogoCode != null && codes.Contains(x.LogoCode.Trim().ToUpper()))))
            .Select(x => new { x.ID, x.Code, x.LogoCode }).ToListAsync(ct);
        // An ERP code must identify one portal material before product-specific conversions can be used.
        var idsByCode = materials.Select(x => new { x.ID, Code = Key(string.IsNullOrWhiteSpace(x.LogoCode) ? x.Code : x.LogoCode) })
            .GroupBy(x => x.Code).Where(x => x.Select(y => y.ID).Distinct().Count() == 1)
            .ToDictionary(x => x.Key, x => x.First().ID);
        var ids = idsByCode.Values.Distinct().ToList();
        var rules = codes.Count == 0 ? [] : await (
            from rule in context.PrdUnitConversions.AsNoTracking()
            join fromUnit in context.PrdUnits.AsNoTracking() on rule.FromUnitId equals fromUnit.ID
            join toUnit in context.PrdUnits.AsNoTracking() on rule.ToUnitId equals toUnit.ID
            where rule.IsDelete != true && rule.IsActive != false && rule.Factor > 0 &&
                (rule.MaterialId == null || ids.Contains(rule.MaterialId.Value))
            select new { rule.MaterialId, FromCode = fromUnit.Code, FromName = fromUnit.Name, ToCode = toUnit.Code, ToName = toUnit.Name, rule.Factor })
            .ToListAsync(ct);
        return new SalesProfitabilityCostCatalog(manual, recipes, idsByCode, rules.Select(x => new Conversion(x.MaterialId,
            ProductionUnitNormalizer.CanonicalCode(x.FromCode, x.FromName), ProductionUnitNormalizer.CanonicalCode(x.ToCode, x.ToName), x.Factor)).ToList());
    }

    public SalesCostResolution Resolve(int logoMaterialRef, string? productCode, string? salesUnit, DateTime salesDate)
    {
        var date = salesDate.Date;
        var manual = _manual.GetValueOrDefault(logoMaterialRef)?.FirstOrDefault(x => x.ValidFrom.Date <= date && (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date));
        // Historical bulk entry stored an undivided TOTAL in MaterialCost; it cannot safely be called raw/packaging.
        var unseparatedBulk = string.Equals(manual?.Source, "BulkTotal", StringComparison.OrdinalIgnoreCase);
        if (unseparatedBulk) manual = null;
        if (manual != null)
        {
            var description = $"Ürün Maliyetleri · v{manual.VersionNumber} · {manual.ValidFrom:dd.MM.yyyy} · {manual.UnitCode} · yalnızca hammadde/ambalaj";
            var currency = manual.CurrencyCode.Trim().ToUpperInvariant();
            var rate = currency is "TRY" or "TL" or "TRL" ? 1m : manual.ExchangeRate;
            if (manual.MaterialCost < 0 || rate <= 0 || currency.Length == 0)
                return new(null, "Manuel maliyet tutarı/kur geçersiz", "Manual", description);
            try { return Convert(checked(manual.MaterialCost * rate), manual.UnitCode, salesUnit, productCode, $"Manuel malzeme · v{manual.VersionNumber}", "Manual", description, null); }
            catch (OverflowException) { return new(null, "Maliyet desteklenen tutarı aşıyor", "Manual", description); }
        }
        var candidates = _recipes.GetValueOrDefault(Key(productCode));
        var latest = candidates?.FirstOrDefault(x => x.CalculationDate.Date <= date);
        if (latest == null) return new(null, unseparatedBulk ? "Eski toplu toplam maliyet ayrıştırılmamış" : "Tarihe uygun maliyet bulunamadı");
        var details = $"{latest.ScenarioNumber} · {latest.CalculationDate:dd.MM.yyyy} · {latest.Unit} · yalnızca hammadde + ambalaj";
        var sameScenario = candidates!.Where(x => x.ScenarioId == latest.ScenarioId).ToList();
        if (sameScenario.Select(x => (x.UnitCostTry, Unit: ProductionUnitNormalizer.CanonicalCode(x.Unit))).Distinct().Count() > 1)
            return new(null, "Aynı hesapta birden fazla ürün maliyeti", "Recipe", details, latest.ScenarioId);
        if (latest.ProductionQuantity <= 0 || latest.UnitCostTry <= 0 || latest.RawMaterialCost < 0 || latest.PackagingCost < 0)
            return new(null, "Kayıtlı reçete maliyeti geçersiz/sıfır", "Recipe", details, latest.ScenarioId);
        return Convert(latest.UnitCostTry, latest.Unit, salesUnit, productCode, "Kayıtlı reçete", "Recipe", details, latest.ScenarioId);
    }

    private SalesCostResolution Convert(decimal unitCost, string costUnit, string? salesUnit, string? productCode,
        string status, string source, string description, int? scenarioId)
    {
        if (!TryFactor(salesUnit, costUnit, productCode, out var factor))
            return new(null, $"Birim dönüşümü yok/çelişkili ({salesUnit} → {costUnit})", source, description, scenarioId);
        try { return new(checked(unitCost * factor), status, source, description, scenarioId); }
        catch (OverflowException) { return new(null, "Maliyet desteklenen tutarı aşıyor", source, description, scenarioId); }
    }

    private bool TryFactor(string? from, string? to, string? productCode, out decimal factor)
    {
        factor = 0;
        var source = ProductionUnitNormalizer.CanonicalCode(from);
        var target = ProductionUnitNormalizer.CanonicalCode(to);
        if (source.Length == 0 || target.Length == 0) return false;
        if (source == target) { factor = 1; return true; }
        if (source == "KG" && target == "GRAM" || source == "LITRE" && target == "ML") { factor = 1000; return true; }
        if (source == "GRAM" && target == "KG" || source == "ML" && target == "LITRE") { factor = .001m; return true; }
        _materialIds.TryGetValue(Key(productCode), out var materialId);
        var matches = _conversions.Where(x => (x.MaterialId == null || x.MaterialId == materialId) &&
            (x.From == source && x.To == target || x.From == target && x.To == source)).ToList();
        if (matches.Any(x => x.MaterialId == materialId && materialId > 0)) matches = matches.Where(x => x.MaterialId == materialId).ToList();
        var factors = matches.Select(x => x.From == source ? x.Factor : 1m / x.Factor).Distinct().ToList();
        if (factors.Count != 1) return false;
        factor = factors[0];
        return factor > 0;
    }

    private static string Key(string? code) => (code ?? "").Trim().ToUpperInvariant();
    private sealed record Conversion(int? MaterialId, string From, string To, decimal Factor);
    private sealed class RecipeCost
    {
        public int LineId { get; set; }
        public int ScenarioId { get; set; }
        public string ScenarioNumber { get; set; } = "";
        public DateTime CalculationDate { get; set; }
        public string ProductCode { get; set; } = "";
        public string Unit { get; set; } = "";
        public decimal ProductionQuantity { get; set; }
        public decimal UnitCostTry { get; set; }
        public decimal RawMaterialCost { get; set; }
        public decimal PackagingCost { get; set; }
    }
}
