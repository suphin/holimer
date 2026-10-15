using System.Globalization;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public sealed class RawMaterialConsumptionService(ApplicationDbContext context)
{
    public const string SourceType = "ManualRawMaterialConsumption";

    public static bool IsAllowedWarehouse(PrdWarehouse warehouse) => warehouse.IsDelete != true && warehouse.IsActive != false &&
        warehouse.Type != PrdWarehouseType.Quarantine && warehouse.Type != PrdWarehouseType.Scrap;

    public async Task<List<RawMaterialConsumptionLotVM>> LoadLotsAsync(int warehouseId, CancellationToken ct)
    {
        var rows = await (from movement in context.PrdStockMovements.AsNoTracking()
                          join lot in context.PrdStockLots.AsNoTracking() on movement.StockLotId equals (int?)lot.ID
                          join material in context.PrdMaterials.AsNoTracking() on lot.MaterialId equals material.ID
                          join unit in context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                          join movementUnit in context.PrdUnits.AsNoTracking() on movement.UnitId equals movementUnit.ID
                          where lot.WarehouseId == warehouseId && movement.WarehouseId == warehouseId && movement.IsDelete != true &&
                              lot.IsDelete != true && lot.IsActive != false && material.IsDelete != true && material.IsActive != false &&
                              material.Type == PrdMaterialType.RawMaterial
                          select new { Lot = lot, Material = material, Unit = unit, MovementUnit = movementUnit,
                              movement.MaterialId, movement.Direction, movement.Quantity, movement.TotalCost }).ToListAsync(ct);
        var lotIds = rows.Select(x => x.Lot.ID).Distinct().ToList();
        var reservations = await context.PrdStockReservations.AsNoTracking()
            .Where(x => x.IsDelete != true && lotIds.Contains(x.StockLotId) &&
                (x.Status == PrdReservationStatus.Active || x.Status == PrdReservationStatus.PartiallyUsed))
            .Select(x => new { x.StockLotId, x.ReservedQuantity, x.UsedQuantity, x.ReleasedQuantity }).ToListAsync(ct);
        return rows.GroupBy(x => x.Lot.ID).Select(group =>
        {
            var first = group.First();
            var unitKey = ProductionUnitNormalizer.CanonicalCode(first.Unit.Code, first.Unit.Name);
            var mixedUnits = group.Any(x => ProductionUnitNormalizer.CanonicalCode(x.MovementUnit.Code, x.MovementUnit.Name) != unitKey);
            var invalidMaterial = group.Any(x => x.MaterialId != first.Material.ID);
            var balance = new RawMaterialConsumptionLotVM
            {
                StockLotId = first.Lot.ID, MaterialId = first.Material.ID, MaterialCode = first.Material.Code, MaterialName = first.Material.Name,
                UnitId = first.Unit.ID, Unit = ProductionUnitNormalizer.DisplayName(first.Unit.Code, first.Unit.Name), LotNumber = first.Lot.LotNumber,
                ProductionDate = first.Lot.ProductionDate, ExpirationDate = first.Lot.ExpirationDate,
                PhysicalQuantity = group.Sum(x => x.Direction == PrdStockDirection.In ? x.Quantity : -x.Quantity),
                StockValue = group.Sum(x => x.Direction == PrdStockDirection.In ? x.TotalCost : -x.TotalCost),
                ReservedQuantity = reservations.Where(x => x.StockLotId == group.Key).Sum(x => Math.Max(0, x.ReservedQuantity - x.UsedQuantity - x.ReleasedQuantity))
            };
            balance.BlockReason = invalidMaterial ? "Lot ile hareket malzemesi uyuşmuyor." : mixedUnits ? "Lot hareketlerinde farklı birimler var; önce birimleri düzeltiniz." :
                first.Unit.IsDelete == true || first.Unit.IsActive == false ? "Malzemenin stok birimi aktif değil." :
                first.Lot.ExpirationDate.HasValue && first.Lot.ExpirationDate.Value.Date < DateTime.Today ? "Lotun son kullanma tarihi geçmiş." :
                first.Material.RequiresExpirationDate && !first.Lot.ExpirationDate.HasValue ? "Lotun zorunlu SKT bilgisi eksik." :
                balance.StockValue < 0 ? "Lot maliyet bakiyesi negatif." : null;
            return balance;
        }).Where(x => x.PhysicalQuantity > 0).OrderBy(x => x.MaterialCode).ThenBy(x => x.ExpirationDate ?? DateTime.MaxValue).ThenBy(x => x.LotNumber).ToList();
    }

    public async Task<List<PrdInventoryDocumentLine>> PrepareLinesAsync(int warehouseId, IReadOnlyCollection<RawMaterialConsumptionInputVM> inputs, CancellationToken ct)
    {
        var warehouse = await context.PrdWarehouses.AsNoTracking().FirstOrDefaultAsync(x => x.ID == warehouseId, ct);
        if (warehouse == null || !IsAllowedWarehouse(warehouse)) throw new InvalidOperationException("Aktif ve kullanılabilir bir kaynak depo seçiniz. Karantina/hurda depolarından sarf yapılamaz.");
        var selected = inputs.Where(x => x.StockLotId.HasValue || !string.IsNullOrWhiteSpace(x.Quantity) || !string.IsNullOrWhiteSpace(x.Notes)).ToList();
        if (selected.Count is 0 or > 100) throw new InvalidOperationException("1-100 sarf satırı giriniz.");
        if (selected.GroupBy(x => x.StockLotId).Any(x => x.Count() > 1)) throw new InvalidOperationException("Aynı lot bir fişte birden fazla kez seçilemez.");
        var lots = (await LoadLotsAsync(warehouseId, ct)).ToDictionary(x => x.StockLotId);
        var lines = new List<PrdInventoryDocumentLine>();
        foreach (var input in selected)
        {
            if (!input.StockLotId.HasValue || !lots.TryGetValue(input.StockLotId.Value, out var lot)) throw new InvalidOperationException($"{lines.Count + 1}. satırda bu depoya ait aktif hammadde lotu seçiniz.");
            if (!lot.CanConsume) throw new InvalidOperationException($"{lot.MaterialCode} / {lot.LotNumber}: {lot.BlockReason ?? "Kullanılabilir stok yok."}");
            if (!TryQuantity(input.Quantity, out var quantity) || quantity > lot.AvailableQuantity) throw new InvalidOperationException($"{lot.MaterialCode} / {lot.LotNumber}: miktar sıfırdan büyük olmalı ve kullanılabilir {lot.AvailableQuantity:0.######} {lot.Unit} miktarını aşmamalıdır.");
            if (input.Notes?.Length > 250) throw new InvalidOperationException("Satır notu en fazla 250 karakter olabilir.");
            lines.Add(new PrdInventoryDocumentLine
            {
                Sequence = lines.Count + 1, MaterialId = lot.MaterialId, UnitId = lot.UnitId, SourceStockLotId = lot.StockLotId,
                LotNumber = lot.LotNumber, ProductionDate = lot.ProductionDate, ExpirationDate = lot.ExpirationDate, Quantity = quantity,
                OriginalUnitCost = lot.UnitCost, UnitCost = lot.UnitCost, TotalCost = decimal.Round(quantity * lot.StockValue / lot.PhysicalQuantity, 6),
                CurrencyCode = "TRY", ExchangeRate = 1, CostSource = PrdStockCostSource.Manual, Notes = input.Notes?.Trim(), IsActive = true, IsDelete = false
            });
        }
        return lines;
    }

    public static bool TryQuantity(string? value, out decimal quantity)
    {
        quantity = 0;
        var text = value?.Trim().Replace(" ", "");
        if (string.IsNullOrWhiteSpace(text)) return false;
        if (text.Contains(',') && text.Contains('.')) text = text.LastIndexOf(',') > text.LastIndexOf('.') ? text.Replace(".", "").Replace(',', '.') : text.Replace(",", "");
        else text = text.Replace(',', '.');
        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out quantity) &&
            quantity > 0 && quantity <= 999999999999.999999m && decimal.Round(quantity, 6) == quantity;
    }
}
