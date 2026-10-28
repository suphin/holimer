using System.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Ekomers.Web.Controllers;

public sealed partial class UretimYonetimiController
{
    private async Task<IDbContextTransaction> BeginMaterialFlowAsync(int id, bool warehouseTask, CancellationToken ct)
    {
        // All order/task operations lock the ORDER first, then task, then warehouses.
        // Looking up the immutable parent before the transaction avoids a task/order lock inversion.
        var orderId=warehouseTask?await _context.PrdWarehouseTasks.AsNoTracking().Where(x=>x.ID==id).Select(x=>x.ProductionOrderId).FirstOrDefaultAsync(ct):id;
        var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            if (_context.Database.IsSqlServer())
            {
                await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdProductionOrder] WITH(UPDLOCK,HOLDLOCK) WHERE [ID]={orderId}", ct);
                if (warehouseTask)
                {
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdWarehouseTask] WITH(UPDLOCK,HOLDLOCK) WHERE [ID]={id}", ct);
                    if(await _context.PrdWarehouseTasks.AsNoTracking().AnyAsync(x=>x.ID==id&&x.ProductionOrderId!=orderId,ct))throw new InvalidOperationException("Görevin üretim emri bağlantısı değişmiş; sayfayı yenileyiniz.");
                }
                var warehouses = warehouseTask
                    ? await _context.PrdWarehouseTasks.AsNoTracking().Where(x => x.ID == id).Select(x => new[] { x.SourceWarehouseId, x.TargetWarehouseId }).FirstOrDefaultAsync(ct)
                    : await _context.PrdProductionOrders.AsNoTracking().Where(x => x.ID == id).Select(x => new[] { x.SourceWarehouseId, x.ProductionWarehouseId }).FirstOrDefaultAsync(ct);
                foreach (var warehouseId in (warehouses ?? []).Distinct().OrderBy(x => x))
                    await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdWarehouse] WITH(UPDLOCK,HOLDLOCK) WHERE [ID]={warehouseId}", ct);
                if(warehouses is {Length:2})
                {
                    if(warehouses[0]==warehouses[1]||!await _context.PrdWarehouses.AnyAsync(x=>x.ID==warehouses[0]&&x.IsActive!=false&&x.IsDelete!=true&&x.Type==PrdWarehouseType.Main,ct)||
                       !await _context.PrdWarehouses.AnyAsync(x=>x.ID==warehouses[1]&&x.IsActive!=false&&x.IsDelete!=true&&x.Type==PrdWarehouseType.Production,ct))
                        throw new InvalidOperationException("Üretim emrinin kaynak ana deposu veya üretim deposu artık aktif/geçerli değil.");
                }
            }
            return tx;
        }
        catch { await tx.DisposeAsync(); throw; }
    }

    private static void CheckFlowLot(ProductionStockLotBalance balance)
    {
        if (balance.BlockReason != null) throw new InvalidOperationException($"{balance.Material.Code} / {balance.Lot.LotNumber}: {balance.BlockReason}");
        if (balance.Lot.ExpirationDate is DateTime expiry && expiry.Year > 1900 && expiry.Date < DateTime.Today)
            throw new InvalidOperationException($"{balance.Lot.LotNumber}: lotun SKT'si geçmiş.");
        if (balance.Material.RequiresExpirationDate && (!balance.Lot.ExpirationDate.HasValue || balance.Lot.ExpirationDate.Value.Year <= 1900))
            throw new InvalidOperationException($"{balance.Lot.LotNumber}: zorunlu SKT bilgisi eksik.");
    }

    private static int FlowUnit(PrdStockReservation reservation, PrdMaterialRequirement requirement, ProductionStockLotBalance balance, ProductionStockUnitConverter converter)
    {
        CheckFlowLot(balance);
        if (reservation.StockLotId != balance.Lot.ID || reservation.MaterialId != balance.Material.ID || reservation.WarehouseId != balance.Lot.WarehouseId)
            throw new InvalidOperationException("Rezervasyon ile kaynak lot bilgileri uyuşmuyor.");
        var unit = ProductionReservationUnits.Resolve(reservation, converter, balance.MovementUnitIds);
        ProductionReservationUnits.Validate(reservation, requirement, unit, converter);
        return unit;
    }

    private static decimal TakeFlowValue(ProductionStockLotBalance balance, decimal quantity)
    {
        if (quantity <= 0 || quantity > balance.PhysicalQuantity) throw new InvalidOperationException($"{balance.Lot.LotNumber}: stok miktarı yetersiz.");
        var cost = quantity == balance.PhysicalQuantity ? balance.StockValue : decimal.Round(quantity / balance.PhysicalQuantity * balance.StockValue, 6);
        balance.PhysicalQuantity -= quantity; balance.StockValue -= cost;
        return cost;
    }
}
