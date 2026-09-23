using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services;

public sealed record ProductionCatalogSyncResult(int UnitAdded, int MaterialAdded, int MaterialUpdated, int QuickCodeAdded);

public sealed class ProductionCatalogSyncService
{
    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logo;

    public ProductionCatalogSyncService(ApplicationDbContext context, LogoContext logo)
    {
        _context = context;
        _logo = logo;
    }

    public async Task<ProductionCatalogSyncResult> SyncAsync(string? userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.Now;
        var productionUnits = await _context.PrdUnits.ToListAsync(cancellationToken);
        var unitByCode = productionUnits
            .GroupBy(x => ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x => x.OrderByDescending(y => y.IsDelete != true && y.IsActive != false)
                    .ThenByDescending(y => Normalize(y.Code) == x.Key)
                    .ThenBy(y => y.ID)
                    .First(),
                StringComparer.OrdinalIgnoreCase);
        var unitAdded = 0;

        if (!unitByCode.TryGetValue("ADET", out var defaultUnit))
        {
            var unit = new PrdUnit { Code = "ADET", Name = "Adet", IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = userId };
            _context.PrdUnits.Add(unit);
            unitByCode[unit.Code] = unit;
            defaultUnit = unit;
            unitAdded++;
        }
        else if (defaultUnit.IsDelete == true || defaultUnit.IsActive == false)
        {
            defaultUnit.Name = "Adet";
            defaultUnit.IsActive = true;
            defaultUnit.IsDelete = false;
            defaultUnit.UpdateDate = now;
            defaultUnit.UpdateUserID = userId;
        }
        await _context.SaveChangesAsync(cancellationToken);

        var logoItems = await _logo.LogoItems.AsNoTracking().Where(x => x.ProductCode != null && x.ProductCode != "").ToListAsync(cancellationToken);
        var logoByCode = logoItems.GroupBy(x => Normalize(x.ProductCode), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var materials = await _context.PrdMaterials.ToListAsync(cancellationToken);
        var materialByCode = materials.ToDictionary(x => Normalize(x.Code), StringComparer.OrdinalIgnoreCase);
        var materialAdded = 0;
        var materialUpdated = 0;
        var quickCodeAdded = 0;

        foreach (var pair in logoByCode)
        {
            var logo = pair.Value;
            if (!materialByCode.TryGetValue(pair.Key, out var material))
            {
                material = NewMaterial(pair.Key, logo.ProductName, userId, now);
                material.Source = PrdMaterialSource.Logo;
                material.Type = DetectType(pair.Key);
                material.UnitId = defaultUnit.ID;
                _context.PrdMaterials.Add(material);
                materialByCode[pair.Key] = material;
                materialAdded++;
            }
            else materialUpdated++;

            material.Name = string.IsNullOrWhiteSpace(logo.ProductName) ? pair.Key : logo.ProductName.Trim();
            material.Description = logo.ProductName2;
            material.LogoCode = pair.Key;
            material.LogoActive = true;
            material.LogoLastSyncDate = now;
            material.Source = PrdMaterialSource.Logo;
            material.UpdateDate = now;
            material.UpdateUserID = userId;
        }

        foreach (var material in materials.Where(x => x.Source == PrdMaterialSource.Logo && !logoByCode.ContainsKey(Normalize(x.Code))))
        {
            material.LogoActive = false;
            material.LogoLastSyncDate = now;
            material.UpdateDate = now;
            material.UpdateUserID = userId;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new ProductionCatalogSyncResult(unitAdded, materialAdded, materialUpdated, quickCodeAdded);
    }

    private static PrdMaterial NewMaterial(string code, string? name, string? userId, DateTime now) => new()
    {
        Code = code, Name = string.IsNullOrWhiteSpace(name) ? code : name.Trim(), Source = PrdMaterialSource.QuickCode,
        Type = PrdMaterialType.Other, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = userId
    };

    private static string Normalize(string? value) => (value ?? string.Empty).Trim().ToUpperInvariant();

    private static PrdMaterialType DetectType(string? value)
    {
        var text = Normalize(value).Replace("İ", "I");
        if (text.Contains("HAMMADDE") || text.Contains("HAM MADDE")) return PrdMaterialType.RawMaterial;
        if (text.Contains("YARI MAMUL") || text.Contains("YARIMAMUL")) return PrdMaterialType.SemiFinished;
        if (text.Contains("AMBALAJ")) return PrdMaterialType.Packaging;
        if (text.Contains("MAMUL") || text.Contains("BITMIS")) return PrdMaterialType.FinishedProduct;

        var tokens = text.Split(['.', '-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Any(x => x == "HM" || x.EndsWith("HM"))) return PrdMaterialType.RawMaterial;
        if (tokens.Any(x => x == "YM" || x.EndsWith("YM"))) return PrdMaterialType.SemiFinished;
        if (tokens.Any(x => x == "AM" || x.EndsWith("AM"))) return PrdMaterialType.Packaging;
        if (tokens.Any(x => x == "MM" || x.EndsWith("MM"))) return PrdMaterialType.FinishedProduct;
        return PrdMaterialType.Other;
    }
}
