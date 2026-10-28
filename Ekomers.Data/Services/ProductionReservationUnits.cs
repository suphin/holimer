using Ekomers.Models.Entity.Production;

namespace Ekomers.Data.Services;

public static class ProductionReservationUnits
{
    public static int Resolve(PrdStockReservation reservation, ProductionStockUnitConverter converter, IEnumerable<int> movementUnits)
    {
        if (reservation.UnitId is int unit) { converter.Factor(reservation.MaterialId, unit); return unit; }
        var ids = movementUnits.Distinct().ToList();
        if (ids.Count == 0 || ids.Any(id => !converter.IsBaseUnitEquivalent(reservation.MaterialId, id)))
            throw new InvalidOperationException("Eski rezervasyonun birimi doğrulanamıyor. Farklı fiziksel birimli lotta rezervasyonu kaynak üretim sürecinden kontrol edip kaldırınız/yeniden ayırınız.");
        return converter.BaseUnitId(reservation.MaterialId);
    }

    public static void Validate(PrdStockReservation reservation, PrdMaterialRequirement requirement, int unitId, ProductionStockUnitConverter converter)
    {
        if (reservation.MaterialId != requirement.MaterialId || reservation.MaterialRequirementId != requirement.ID)
            throw new InvalidOperationException("Rezervasyon malzeme/ihtiyaç bağlantısı uyuşmuyor.");
        var factor = decimal.Round(converter.FactorBetween(requirement.MaterialId, unitId, requirement.UnitId), 18);
        if (reservation.UnitId.HasValue && (reservation.RequirementUnitId != requirement.UnitId || reservation.RequirementUnitFactor != factor))
            throw new InvalidOperationException("Rezervasyon sonrası ana/ihtiyaç birimi veya dönüşüm katsayısı değişmiş. Eski miktarlar yeniden yorumlanmadı; ilgili rezervasyonu kontrol edip yeniden ayırınız.");
    }

    public static void Stamp(PrdStockReservation reservation, PrdMaterialRequirement requirement, int unitId, ProductionStockUnitConverter converter)
    {
        reservation.UnitId = unitId;
        reservation.RequirementUnitId = requirement.UnitId;
        reservation.RequirementUnitFactor = decimal.Round(converter.FactorBetween(requirement.MaterialId, unitId, requirement.UnitId), 18);
        if (reservation.RequirementUnitFactor <= 0) throw new InvalidOperationException("Rezervasyon dönüşüm katsayısı desteklenen hassasiyetin dışında.");
    }
}
