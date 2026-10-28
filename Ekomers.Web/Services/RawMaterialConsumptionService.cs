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
        var rows = await ProductionStockLotReader.LoadAsync(context, warehouseId, ct, rawMaterialsOnly: true);
        return rows.Select(first =>
        {
            var balance = new RawMaterialConsumptionLotVM
            {
                StockLotId = first.Lot.ID, MaterialId = first.Material.ID, MaterialCode = first.Material.Code, MaterialName = first.Material.Name,
                UnitId = first.UnitId, Unit = first.Unit, LotNumber = first.Lot.LotNumber,
                ProductionDate = first.Lot.ProductionDate, ExpirationDate = first.Lot.ExpirationDate,
                PhysicalQuantity = first.PhysicalQuantity, StockValue = first.StockValue, ReservedQuantity = first.ReservedQuantity
            };
            balance.BlockReason = first.BlockReason ?? (
                first.Lot.ExpirationDate.HasValue && first.Lot.ExpirationDate.Value.Date < DateTime.Today ? "Lotun son kullanma tarihi geçmiş." :
                first.Material.RequiresExpirationDate && !first.Lot.ExpirationDate.HasValue ? "Lotun zorunlu SKT bilgisi eksik." : null);
            return balance;
        }).OrderBy(x => x.MaterialCode).ThenBy(x => x.ExpirationDate ?? DateTime.MaxValue).ThenBy(x => x.LotNumber).ToList();
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
                OriginalUnitCost = lot.UnitCost, UnitCost = lot.UnitCost, TotalCost = quantity == lot.PhysicalQuantity ? lot.StockValue : decimal.Round(quantity / lot.PhysicalQuantity * lot.StockValue, 6),
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
