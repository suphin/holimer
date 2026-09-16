using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public sealed class PurchasingInventoryReversalService
{
    private readonly ApplicationDbContext _context;

    public PurchasingInventoryReversalService(ApplicationDbContext context) => _context = context;

    public async Task<PrdInventoryDocument> ReversePostedDocumentAsync(
        PrdInventoryDocument original,
        string reason,
        string user,
        CancellationToken ct)
    {
        if (original.Status != PrdInventoryDocumentStatus.Posted || original.ReversalDocumentId.HasValue)
            throw new InvalidOperationException("Stok belgesi daha önce geri alınmış veya geri alınabilir durumda değil.");

        var originalLines = await _context.PrdInventoryDocumentLines
            .AsNoTracking()
            .Where(x => x.InventoryDocumentId == original.ID && x.IsDelete != true)
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);
        var originalMovements = await _context.PrdStockMovements
            .AsNoTracking()
            .Where(x => x.InventoryDocumentId == original.ID && x.IsDelete != true)
            .OrderBy(x => x.ID)
            .ToListAsync(ct);

        if (originalLines.Count == 0 || originalMovements.Count == 0)
            throw new InvalidOperationException("Geri alınacak stok belgesinin satır veya hareket kayıtları bulunamadı.");

        var reverseOutGroups = originalMovements
            .Where(x => x.Direction == PrdStockDirection.In && x.StockLotId.HasValue)
            .GroupBy(x => new { x.WarehouseId, StockLotId = x.StockLotId!.Value })
            .Select(x => new { x.Key.WarehouseId, x.Key.StockLotId, Quantity = x.Sum(y => y.Quantity) })
            .ToList();
        if (reverseOutGroups.Count > 0)
        {
            var warehouseIds = reverseOutGroups.Select(x => x.WarehouseId).Distinct().ToList();
            var lotIds = reverseOutGroups.Select(x => x.StockLotId).Distinct().ToList();
            var currentMovements = await _context.PrdStockMovements.AsNoTracking()
                .Where(x => warehouseIds.Contains(x.WarehouseId) && x.StockLotId.HasValue && lotIds.Contains(x.StockLotId.Value) && x.IsDelete != true)
                .Select(x => new { x.WarehouseId, StockLotId = x.StockLotId!.Value, x.Direction, x.Quantity })
                .ToListAsync(ct);
            var reservations = await _context.PrdStockReservations.AsNoTracking()
                .Where(x => warehouseIds.Contains(x.WarehouseId) && lotIds.Contains(x.StockLotId) && x.IsDelete != true &&
                            (x.Status == PrdReservationStatus.Active || x.Status == PrdReservationStatus.PartiallyUsed))
                .Select(x => new { x.WarehouseId, x.StockLotId, Quantity = x.ReservedQuantity - x.UsedQuantity - x.ReleasedQuantity })
                .ToListAsync(ct);

            foreach (var item in reverseOutGroups)
            {
                var physical = currentMovements
                    .Where(x => x.WarehouseId == item.WarehouseId && x.StockLotId == item.StockLotId)
                    .Sum(x => x.Direction == PrdStockDirection.In ? x.Quantity : -x.Quantity);
                var reserved = reservations
                    .Where(x => x.WarehouseId == item.WarehouseId && x.StockLotId == item.StockLotId)
                    .Sum(x => x.Quantity);
                if (physical - reserved < item.Quantity)
                    throw new InvalidOperationException(
                        $"Geri alma için gerekli stok artık kullanılamıyor. Depo: {item.WarehouseId}, lot: {item.StockLotId}, kullanılabilir: {physical - reserved:0.######}, gerekli: {item.Quantity:0.######}.");
            }
        }

        var now = DateTime.Now;
        var reversal = new PrdInventoryDocument
        {
            DocumentNumber = $"TRS-{original.ID}-{now:yyyyMMddHHmmssfff}",
            Type = PrdInventoryDocumentType.Adjustment,
            Status = PrdInventoryDocumentStatus.Posted,
            DocumentDate = now.Date,
            PostingDate = now,
            PostedUserId = user,
            SourceWarehouseId = original.TargetWarehouseId,
            TargetWarehouseId = original.SourceWarehouseId,
            CurrencyCode = original.CurrencyCode,
            ExchangeRate = original.ExchangeRate,
            TotalCost = original.TotalCost,
            SourceDocumentType = "PrdInventoryDocumentReversal",
            SourceDocumentId = original.ID,
            Notes = $"{original.DocumentNumber} stok belgesi geri alındı. Gerekçe: {reason}",
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        };
        _context.PrdInventoryDocuments.Add(reversal);
        await _context.SaveChangesAsync(ct);

        var reversalLineByOriginalLine = new Dictionary<int, PrdInventoryDocumentLine>();
        foreach (var line in originalLines)
        {
            var reversalLine = new PrdInventoryDocumentLine
            {
                InventoryDocumentId = reversal.ID,
                Sequence = line.Sequence,
                MaterialId = line.MaterialId,
                UnitId = line.UnitId,
                SourceStockLotId = line.TargetStockLotId,
                TargetStockLotId = line.SourceStockLotId,
                LotNumber = line.LotNumber,
                ProductionDate = line.ProductionDate,
                ExpirationDate = line.ExpirationDate,
                Quantity = line.Quantity,
                OriginalUnitCost = line.OriginalUnitCost,
                CurrencyCode = line.CurrencyCode,
                ExchangeRate = line.ExchangeRate,
                UnitCost = line.UnitCost,
                TotalCost = line.TotalCost,
                CostSource = line.CostSource,
                Notes = $"Ters kayıt: {original.DocumentNumber}. {reason}",
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            };
            _context.PrdInventoryDocumentLines.Add(reversalLine);
            reversalLineByOriginalLine[line.ID] = reversalLine;
        }
        await _context.SaveChangesAsync(ct);

        foreach (var movement in originalMovements)
        {
            if (!movement.InventoryDocumentLineId.HasValue ||
                !reversalLineByOriginalLine.TryGetValue(movement.InventoryDocumentLineId.Value, out var reversalLine))
                throw new InvalidOperationException("Stok hareketinin kaynak belge satırı bulunamadı.");

            _context.PrdStockMovements.Add(new PrdStockMovement
            {
                InventoryDocumentId = reversal.ID,
                InventoryDocumentLineId = reversalLine.ID,
                MaterialId = movement.MaterialId,
                WarehouseId = movement.WarehouseId,
                StockLotId = movement.StockLotId,
                Direction = movement.Direction == PrdStockDirection.In ? PrdStockDirection.Out : PrdStockDirection.In,
                MovementType = movement.MovementType,
                Quantity = movement.Quantity,
                UnitId = movement.UnitId,
                OriginalUnitCost = movement.OriginalUnitCost,
                CurrencyCode = movement.CurrencyCode,
                ExchangeRate = movement.ExchangeRate,
                UnitCost = movement.UnitCost,
                TotalCost = movement.TotalCost,
                CostSource = movement.CostSource,
                MovementDate = now.Date,
                DocumentNumber = reversal.DocumentNumber,
                DocumentType = PrdStockDocumentType.InventoryDocument,
                DocumentId = reversal.ID,
                TransferNumber = reversal.DocumentNumber,
                Description = reversal.Notes,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            });
        }

        original.Status = PrdInventoryDocumentStatus.Reversed;
        original.ReversalDocumentId = reversal.ID;
        original.UpdateDate = now;
        original.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
        return reversal;
    }
}
