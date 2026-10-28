using Ekomers.Models.Entity.Production;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services;

/// <summary>Read-time normalization only: historical movements and their total costs are never rewritten.</summary>
public sealed class ProductionStockUnitConverter
{
    private readonly Dictionary<int, PrdMaterial> materials;
    private readonly Dictionary<int, PrdUnit> units;
    private readonly List<PrdUnitConversion> rules;
    private readonly Dictionary<(int Material, int Source), decimal> factors = [];

    public ProductionStockUnitConverter(IEnumerable<PrdMaterial> materials, IEnumerable<PrdUnit> units, IEnumerable<PrdUnitConversion> rules)
    {
        this.materials = materials.ToDictionary(x => x.ID);
        this.units = units.ToDictionary(x => x.ID);
        this.rules = rules.Where(x => x.IsActive != false && x.IsDelete != true).ToList();
    }

    public static async Task<ProductionStockUnitConverter> LoadAsync(ApplicationDbContext context, IReadOnlyCollection<int> materialIds, CancellationToken ct)
    {
        var materials = await context.PrdMaterials.AsNoTracking().Where(m => materialIds.Contains(m.ID) && m.IsDelete != true).ToListAsync(ct);
        // Inactive unit masters may still describe valid historical movements.
        var units = await context.PrdUnits.AsNoTracking().ToListAsync(ct);
        var rules = materialIds.Count == 0 ? [] : await context.PrdUnitConversions.AsNoTracking()
            .Where(r => r.IsDelete != true && r.IsActive != false && (r.MaterialId == null || materialIds.Contains(r.MaterialId.Value))).ToListAsync(ct);
        return new(materials, units, rules);
    }

    public int BaseUnitId(int materialId) => Material(materialId).UnitId;
    public string UnitName(int materialId, int unitId) { var u = Unit(unitId, materialId); return ProductionUnitNormalizer.DisplayName(u.Code, u.Name); }
    public bool IsBaseUnitActive(int materialId)
    {
        var unit = Unit(BaseUnitId(materialId), materialId);
        return unit.IsDelete != true && unit.IsActive != false;
    }
    public string BaseUnitName(int materialId)
    {
        var unit = Unit(Material(materialId).UnitId, materialId);
        return ProductionUnitNormalizer.DisplayName(unit.Code, unit.Name);
    }
    public bool IsBaseUnitEquivalent(int materialId, int sourceId) => Matches(sourceId, BaseUnitId(materialId));
    public decimal FactorBetween(int materialId, int sourceId, int targetId) => Factor(materialId, sourceId) / Factor(materialId, targetId);
    public decimal ConvertQuantity(int materialId, int sourceId, int targetId, decimal quantity)
    {
        try
        {
            var value = decimal.Round(checked(quantity * FactorBetween(materialId, sourceId, targetId)), 6, MidpointRounding.AwayFromZero);
            if (quantity != 0 && value == 0) throw new InvalidOperationException($"{Material(materialId).Code}: dönüşen miktar 6 ondalık hassasiyetinde sıfıra düşüyor.");
            return value;
        }
        catch (OverflowException) { throw new InvalidOperationException($"{Material(materialId).Code}: birimler arası miktar desteklenen aralığı aşıyor."); }
    }

    public decimal Factor(int materialId, int sourceId)
    {
        if (factors.TryGetValue((materialId, sourceId), out var cached)) return cached;
        var material = Material(materialId); var source = Unit(sourceId, materialId); var target = Unit(material.UnitId, materialId);
        var from = ProductionUnitNormalizer.CanonicalCode(source.Code, source.Name);
        var to = ProductionUnitNormalizer.CanonicalCode(target.Code, target.Name);
        var candidates = rules.Where(r => (r.MaterialId == materialId || r.MaterialId == null) &&
            ((Matches(sourceId, r.FromUnitId) && Matches(target.ID, r.ToUnitId)) || (Matches(sourceId, r.ToUnitId) && Matches(target.ID, r.FromUnitId)))).ToList();
        // Product-specific rules win in BOTH directions, regardless of a general direct rule.
        if (candidates.Any(r => r.MaterialId == materialId)) candidates = candidates.Where(r => r.MaterialId == materialId).ToList();
        try
        {
            var values = candidates.Select(r => r.Factor <= 0 ? throw Failure(materialId, from, to, "Katsayı sıfırdan büyük olmalıdır.") :
                Matches(sourceId, r.FromUnitId) ? r.Factor : 1m / r.Factor).Distinct().ToList();
            var physical = PhysicalFactor(from, to);
            if (sourceId == target.ID || from.Length > 0 && from == to) physical = 1m;
            if (values.Count > 1 || physical.HasValue && values.Any(v => v != physical.Value))
                throw Failure(materialId, from, to, "Aktif dönüşüm tanımları çelişiyor; aynı birim çifti için tek ve tutarlı katsayı tanımlayınız.");
            var factor = physical ?? (values.Count == 1 ? values[0] : throw Failure(materialId, from, to, "Ürüne özel veya genel doğrudan/ters birim dönüşümü tanımlayınız."));
            if (factor <= 0) throw Failure(materialId, from, to, "Katsayı desteklenen hassasiyetin dışında.");
            factors[(materialId, sourceId)] = factor;
            return factor;
        }
        catch (OverflowException) { throw Failure(materialId, from, to, "Katsayı desteklenen aralığı aşıyor."); }
    }

    public decimal Quantity(int materialId, int sourceId, decimal quantity)
    {
        try
        {
            var converted = decimal.Round(checked(quantity * Factor(materialId, sourceId)), 6, MidpointRounding.AwayFromZero);
            if (quantity != 0 && converted == 0) throw new InvalidOperationException($"{Material(materialId).Code}: dönüşüm sonrası miktar 6 ondalık hassasiyetinde sıfıra düşüyor. Birim dönüşümünü kontrol ediniz.");
            return converted;
        }
        catch (OverflowException) { throw new InvalidOperationException($"{Material(materialId).Code}: dönüştürülmüş stok miktarı desteklenen aralığı aşıyor."); }
    }
    public decimal UnitCost(int materialId, int sourceId, decimal cost)
    {
        try { return checked(cost / Factor(materialId, sourceId)); }
        catch (OverflowException) { throw new InvalidOperationException($"{Material(materialId).Code}: dönüştürülmüş birim maliyet desteklenen aralığı aşıyor."); }
    }

    private PrdMaterial Material(int id) => materials.GetValueOrDefault(id) ?? throw new InvalidOperationException($"Malzeme #{id}: stok kartı bulunamadı.");
    private PrdUnit Unit(int id, int materialId) => units.GetValueOrDefault(id) ?? throw new InvalidOperationException($"{Material(materialId).Code}: birim #{id} bulunamadı.");
    private bool Matches(int firstId, int secondId)
    {
        if (firstId == secondId) return true;
        if (!units.TryGetValue(firstId, out var a) || !units.TryGetValue(secondId, out var b)) return false;
        var code = ProductionUnitNormalizer.CanonicalCode(a.Code, a.Name);
        return code.Length > 0 && code == ProductionUnitNormalizer.CanonicalCode(b.Code, b.Name);
    }
    private InvalidOperationException Failure(int materialId, string from, string to, string reason) => new($"{Material(materialId).Code}: {from} → {to} stok birimi dönüşümü uygulanamadı. {reason}");
    private static decimal? PhysicalFactor(string from, string to)
    {
        if (from == "KG" && to == "GRAM" || from == "LITRE" && to == "ML") return 1000m;
        if (from == "GRAM" && to == "KG" || from == "ML" && to == "LITRE") return 0.001m;
        return null;
    }
}
