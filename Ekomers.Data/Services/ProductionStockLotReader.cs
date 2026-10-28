using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services;

/// <summary>One balance per physical lot, in the material's base unit. Does not rewrite historical stock.</summary>
public static class ProductionStockLotReader
{
    public static async Task<List<ProductionStockLotBalance>> LoadAsync(ApplicationDbContext context, int warehouseId, CancellationToken ct, bool rawMaterialsOnly = false, IReadOnlyCollection<int>? materialIds = null, bool includeEmpty = false)
    {
        // Do not inner-join movement units: a missing historical unit must block a lot, not hide stock.
        var rows = await (from movement in context.PrdStockMovements.AsNoTracking()
                          join lot in context.PrdStockLots.AsNoTracking() on movement.StockLotId equals (int?)lot.ID
                          join material in context.PrdMaterials.AsNoTracking() on lot.MaterialId equals material.ID
                          where movement.WarehouseId == warehouseId && lot.WarehouseId == warehouseId && movement.IsDelete != true &&
                              lot.IsDelete != true && material.IsDelete != true && (!rawMaterialsOnly || material.Type == PrdMaterialType.RawMaterial) &&
                              (materialIds == null || materialIds.Contains(material.ID))
                          select new { Lot = lot, Material = material, movement.MaterialId, movement.UnitId, movement.Direction, movement.Quantity, movement.TotalCost }).ToListAsync(ct);
        var converter = await ProductionStockUnitConverter.LoadAsync(context, rows.Select(x => x.Material.ID).Distinct().ToList(), ct);
        var lotIds = rows.Select(x => x.Lot.ID).Distinct().ToList();
        var reservations = await context.PrdStockReservations.AsNoTracking()
            .Where(x => x.IsDelete != true && lotIds.Contains(x.StockLotId) &&
                (x.Status == PrdReservationStatus.Active || x.Status == PrdReservationStatus.PartiallyUsed)).ToListAsync(ct);
        var result = new List<ProductionStockLotBalance>();
        foreach (var group in rows.GroupBy(x => x.Lot.ID))
        {
            var first = group.First();
            var balance = new ProductionStockLotBalance { Lot = first.Lot, Material = first.Material, UnitId = first.Material.UnitId };
            balance.MovementUnitIds.AddRange(group.Select(x => x.UnitId).Distinct());
            try
            {
                balance.Unit = converter.BaseUnitName(first.Material.ID);
                if (group.Any(x => x.MaterialId != first.Material.ID)) throw new InvalidOperationException("Lot ile hareket malzemesi uyuşmuyor.");
                if (first.Material.IsActive == false || first.Lot.IsActive == false) throw new InvalidOperationException("Malzeme veya lot aktif değil.");
                if (!converter.IsBaseUnitActive(first.Material.ID)) throw new InvalidOperationException("Malzemenin stok birimi aktif değil.");
                balance.PhysicalQuantity = group.Sum(x => (x.Direction == PrdStockDirection.In ? 1 : -1) * converter.Quantity(first.Material.ID, x.UnitId, x.Quantity));
                balance.StockValue = group.Sum(x => x.Direction == PrdStockDirection.In ? x.TotalCost : -x.TotalCost);
                var active = reservations.Where(x => x.StockLotId == group.Key && x.ReservedQuantity - x.UsedQuantity - x.ReleasedQuantity > 0).ToList();
                if (active.Any(x => x.MaterialId != first.Material.ID || x.WarehouseId != warehouseId)) throw new InvalidOperationException("Lot ile aktif rezervasyon bilgileri uyuşmuyor.");
                foreach (var reservation in active)
                {
                    var unit = ProductionReservationUnits.Resolve(reservation, converter, group.Select(x => x.UnitId));
                    balance.ReservationUnits[reservation.ID] = unit;
                    balance.ReservationQuantities[reservation.ID] = converter.Quantity(first.Material.ID, unit, reservation.ReservedQuantity - reservation.UsedQuantity - reservation.ReleasedQuantity);
                }
                balance.ReservedQuantity = balance.ReservationQuantities.Values.Sum();
                if (balance.StockValue < 0) throw new InvalidOperationException("Lot maliyet bakiyesi negatif.");
            }
            catch (InvalidOperationException e) { balance.BlockReason = e.Message; }
            catch (OverflowException) { balance.BlockReason = "Dönüştürülmüş lot miktarı veya maliyeti desteklenen aralığı aşıyor."; }
            if (includeEmpty || balance.PhysicalQuantity > 0 || balance.BlockReason != null) result.Add(balance);
        }
        return result.OrderBy(x => x.Material.Code).ThenBy(x => x.Lot.ExpirationDate ?? DateTime.MaxValue).ThenBy(x => x.Lot.ID).ToList();
    }
}

public sealed class ProductionStockLotBalance
{
    public required PrdStockLot Lot { get; init; }
    public required PrdMaterial Material { get; init; }
    public int UnitId { get; init; }
    public string Unit { get; set; } = "";
    public decimal PhysicalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal StockValue { get; set; }
    public string? BlockReason { get; set; }
    public Dictionary<int, decimal> ReservationQuantities { get; } = [];
    public Dictionary<int, int> ReservationUnits { get; } = [];
    public List<int> MovementUnitIds { get; } = [];
}
