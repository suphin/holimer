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
        var oldUnits = await _context.MalzemeBirim.AsNoTracking().Where(x => x.IsDelete != true).ToListAsync(cancellationToken);
        var productionUnits = await _context.PrdUnits.ToListAsync(cancellationToken);
        var unitByCode = productionUnits.ToDictionary(x => Normalize(x.Code), StringComparer.OrdinalIgnoreCase);
        var unitAdded = 0;

        foreach (var oldUnit in oldUnits)
        {
            var code = Normalize(string.IsNullOrWhiteSpace(oldUnit.Kod) ? oldUnit.Ad : oldUnit.Kod);
            if (string.IsNullOrWhiteSpace(code) || unitByCode.ContainsKey(code)) continue;
            var unit = new PrdUnit { Code = code, Name = oldUnit.Ad.Trim(), IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = userId };
            _context.PrdUnits.Add(unit);
            unitByCode[code] = unit;
            unitAdded++;
        }

        if (unitByCode.Count == 0)
        {
            var unit = new PrdUnit { Code = "ADET", Name = "Adet", IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = userId };
            _context.PrdUnits.Add(unit);
            unitByCode[unit.Code] = unit;
            unitAdded++;
        }
        await _context.SaveChangesAsync(cancellationToken);

        var defaultUnit = unitByCode.Values.First();
        var unitIdByOldId = oldUnits
            .Where(x => unitByCode.ContainsKey(Normalize(string.IsNullOrWhiteSpace(x.Kod) ? x.Ad : x.Kod)))
            .ToDictionary(x => x.ID, x => unitByCode[Normalize(string.IsNullOrWhiteSpace(x.Kod) ? x.Ad : x.Kod)].ID);

        var oldTypes = await _context.MalzemeTipi.AsNoTracking().Where(x => x.IsDelete != true).ToDictionaryAsync(x => x.ID, cancellationToken);
        var portalMaterials = await _context.Malzeme.AsNoTracking().Where(x => x.IsDelete != true && x.Kod != null && x.Kod != "").ToListAsync(cancellationToken);
        var portalByCode = portalMaterials.GroupBy(x => Normalize(x.Kod), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var logoItems = await _logo.LogoItems.AsNoTracking().Where(x => x.ProductCode != null && x.ProductCode != "").ToListAsync(cancellationToken);
        var logoByCode = logoItems.GroupBy(x => Normalize(x.ProductCode), StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var materials = await _context.PrdMaterials.ToListAsync(cancellationToken);
        var materialByCode = materials.ToDictionary(x => Normalize(x.Code), StringComparer.OrdinalIgnoreCase);
        var materialAdded = 0;
        var materialUpdated = 0;
        var quickCodeAdded = 0;

        foreach (var pair in logoByCode)
        {
            portalByCode.TryGetValue(pair.Key, out var portal);
            var logo = pair.Value;
            var unitId = portal != null && unitIdByOldId.TryGetValue(portal.BirimID, out var mappedUnitId) ? mappedUnitId : defaultUnit.ID;
            var typeText = portal != null && oldTypes.TryGetValue(portal.TipID, out var oldType) ? $"{oldType.Kod} {oldType.Ad}" : pair.Key;
            if (!materialByCode.TryGetValue(pair.Key, out var material))
            {
                material = NewMaterial(pair.Key, logo.ProductName, userId, now);
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
            material.Type = DetectType(typeText);
            material.UnitId = unitId;
            material.IsActive = true;
            material.IsDelete = false;
            material.UpdateDate = now;
            material.UpdateUserID = userId;
        }

        foreach (var portal in portalMaterials)
        {
            var code = Normalize(portal.Kod);
            if (logoByCode.ContainsKey(code) || materialByCode.ContainsKey(code)) continue;
            var unitId = unitIdByOldId.TryGetValue(portal.BirimID, out var mappedUnitId) ? mappedUnitId : defaultUnit.ID;
            var typeText = oldTypes.TryGetValue(portal.TipID, out var oldType) ? $"{oldType.Kod} {oldType.Ad}" : code;
            var material = NewMaterial(code, portal.Ad, userId, now);
            material.Description = portal.Aciklama;
            material.Source = PrdMaterialSource.QuickCode;
            material.Type = DetectType(typeText);
            material.UnitId = unitId;
            _context.PrdMaterials.Add(material);
            materialByCode[code] = material;
            materialAdded++;
            quickCodeAdded++;
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
