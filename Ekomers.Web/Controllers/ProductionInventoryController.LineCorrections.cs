using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Data.Common;

namespace Ekomers.Web.Controllers;

public sealed partial class ProductionInventoryController
{
    private const int CorrectionLockWaitMilliseconds=5000;

    private async Task<IActionResult> RunCorrectionAsync(int id,string action,InventoryDocumentEditVM? posted,Func<Task<IActionResult>> operation,CancellationToken ct)
    {
        var opened=false;
        int? originalLockTimeout=null;
        try
        {
            if(_context.Database.IsSqlServer())
            {
                await _context.Database.OpenConnectionAsync(ct);
                opened=true;
                await using var command=_context.Database.GetDbConnection().CreateCommand();
                command.CommandTimeout=5;
                command.CommandText="SELECT @@LOCK_TIMEOUT";
                originalLockTimeout=Convert.ToInt32(await command.ExecuteScalarAsync(ct));
                var wait=originalLockTimeout.Value<0?CorrectionLockWaitMilliseconds:Math.Min(originalLockTimeout.Value,CorrectionLockWaitMilliseconds);
                command.CommandText=$"SET LOCK_TIMEOUT {wait}";
                await command.ExecuteNonQueryAsync(ct);
            }
            return await operation();
        }
        catch(OperationCanceledException) when(ct.IsCancellationRequested||HttpContext.RequestAborted.IsCancellationRequested)
        {
            _context.ChangeTracker.Clear();
            _logger?.LogInformation("Inventory correction request cancelled. DocumentId={DocumentId}, Action={Action}",id,action);
            return StatusCode(499);
        }
        catch(Exception ex) when(CorrectionSqlError(ex) is 1222 or 1205)
        {
            // The operation's transaction has been disposed and rolled back before reaching this handler.
            _context.ChangeTracker.Clear();
            _logger?.LogWarning(ex,"Inventory correction could not acquire database locks. DocumentId={DocumentId}, Action={Action}, SqlError={SqlError}",id,action,CorrectionSqlError(ex));
            if(ct.IsCancellationRequested||HttpContext.RequestAborted.IsCancellationRequested)return StatusCode(499);
            if(posted==null){TempData["error"]="Kayıt başka bir işlem tarafından kullanılıyor. Silme yapılmadı; işlem tamamlandıktan sonra tekrar deneyiniz.";return RedirectToAction(nameof(Detay),new{id});}
            Response.StatusCode=409;
            ViewBag.CorrectionRetryAction=action;
            ViewBag.CorrectionCanRetry=true;
            ViewBag.CorrectionMessage="Kayıt başka bir işlem tarafından kullanıldığı için değişiklikler kaydedilmedi. Girdiğiniz değerler korundu; diğer işlem tamamlandığında tekrar deneyebilirsiniz.";
            return View("DuzeltmeTekrarDene",posted);
        }
        catch(Exception ex) when(CorrectionSqlError(ex)==-2||ex is OperationCanceledException)
        {
            _context.ChangeTracker.Clear();
            _logger?.LogWarning(ex,"Inventory correction interrupted or timed out. Verify persisted state before retrying. DocumentId={DocumentId}, Action={Action}",id,action);
            if(ct.IsCancellationRequested||HttpContext.RequestAborted.IsCancellationRequested)return StatusCode(499);
            Response.StatusCode=503;
            ViewBag.CorrectionCanRetry=false;
            ViewBag.CorrectionMessage="İşlem zaman aşımına uğradı veya bağlantı işlemi iptal edildi. Kaydetme sonucu doğrulanamadı; tekrar işlem yapmadan önce belge detayından son durumu kontrol ediniz.";
            return View("DuzeltmeTekrarDene",posted??new InventoryDocumentEditVM{Id=id});
        }
        finally
        {
            if(opened)
            {
                try
                {
                    var connection=_context.Database.GetDbConnection();
                    if(originalLockTimeout.HasValue&&connection.State==ConnectionState.Open)
                    {
                        await using var reset=connection.CreateCommand();
                        reset.CommandTimeout=5;
                        reset.CommandText=$"SET LOCK_TIMEOUT {originalLockTimeout.Value}";
                        await reset.ExecuteNonQueryAsync(CancellationToken.None);
                    }
                }
                catch(DbException ex)
                {
                    if(_context.Database.GetDbConnection() is SqlConnection connection)SqlConnection.ClearPool(connection);
                    _logger?.LogWarning(ex,"Inventory correction session settings could not be restored. Connection discarded from pool. DocumentId={DocumentId}",id);
                }
                finally{await _context.Database.CloseConnectionAsync();}
            }
        }
    }

    private static int? CorrectionSqlError(Exception ex)
    {
        for(Exception? error=ex;error!=null;error=error.InnerException)
            if(error is SqlException sql)return sql.Number;
        return null;
    }

    private async Task<IActionResult> InvalidInventoryEditAsync(InventoryDocumentEditVM posted,int? lineId,CancellationToken ct)
    {
        var model=await BuildEditModel(posted.Id,posted,ct);
        if(model==null)return NotFound();
        model.EditLineId=lineId;
        if(lineId.HasValue)model.Lines=model.Lines.Where(x=>x.Id==lineId.Value).ToList();
        return View(lineId.HasValue?nameof(SatirDuzenle):nameof(Duzenle),model);
    }

    private async Task<PrdInventoryDocument?> LockCorrectionDocumentAsync(int id,CancellationToken ct)
    {
        var query=_context.Database.IsSqlServer()
            ?_context.PrdInventoryDocuments.FromSqlInterpolated($"SELECT * FROM dbo.PrdInventoryDocument WITH(UPDLOCK,HOLDLOCK) WHERE ID={id}")
            :_context.PrdInventoryDocuments.AsQueryable();
        var latest=await query.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);
        if(latest==null)return null;
        var local=_context.PrdInventoryDocuments.Local.FirstOrDefault(x=>x.ID==id);
        if(local!=null){_context.Entry(local).CurrentValues.SetValues(latest);_context.Entry(local).OriginalValues.SetValues(latest);return local;}
        _context.Attach(latest);return latest;
    }

    private async Task LockCorrectionWarehousesAsync(PrdInventoryDocument document,CancellationToken ct)
    {
        if(!_context.Database.IsSqlServer())return;
        var ids=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.InventoryDocumentId==document.ID&&x.IsDelete!=true).Select(x=>x.WarehouseId).Distinct().ToListAsync(ct);
        if(document.SourceWarehouseId.HasValue)ids.Add(document.SourceWarehouseId.Value);
        if(document.TargetWarehouseId.HasValue)ids.Add(document.TargetWarehouseId.Value);
        foreach(var warehouseId in ids.Distinct().OrderBy(x=>x))
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT ID FROM dbo.PrdWarehouse WITH(UPDLOCK,HOLDLOCK) WHERE ID={warehouseId}",ct);
    }

    private async Task<bool> HasCorrectionWorkflowLinksAsync(int id,IReadOnlyCollection<int> lineIds,CancellationToken ct) =>
        await _context.PrdInventoryDocuments.AnyAsync(x=>x.IsDelete!=true&&(x.ReversalDocumentId==id||(x.SourceDocumentType=="PrdInventoryDocumentReversal"&&x.SourceDocumentId==id)),ct)||
        await _context.PurGoodsReceipts.AnyAsync(x=>x.QuarantineInventoryDocumentId==id,ct)||
        await _context.PurGoodsReceiptLandedCosts.AnyAsync(x=>x.InventoryDocumentId==id,ct)||
        await _context.PurGoodsReceiptLines.AnyAsync(x=>x.QuarantineInventoryDocumentLineId.HasValue&&lineIds.Contains(x.QuarantineInventoryDocumentLineId.Value),ct)||
        await _context.PurGoodsReceiptLandedCostAllocations.AnyAsync(x=>lineIds.Contains(x.InventoryDocumentLineId),ct);

    // Validate the proposed ledger before writing. Never add kilograms and pieces as raw numbers.
    private async Task<string?> ValidateCorrectionBalancesAsync(IReadOnlyCollection<PrdStockMovement> proposed,IReadOnlyCollection<int>? removedIds,CancellationToken ct)
    {
        var lotIds=proposed.Where(x=>x.StockLotId.HasValue).Select(x=>x.StockLotId!.Value).Distinct().ToList();
        if(lotIds.Count==0)return null;
        var lots=await _context.PrdStockLots.AsNoTracking().Where(x=>lotIds.Contains(x.ID)&&x.IsDelete!=true).ToDictionaryAsync(x=>x.ID,ct);
        if(lots.Count!=lotIds.Count)return "Bağlı stok lotlarından biri bulunamadı; işlem yapılamadı.";
        var rows=await _context.PrdStockMovements.AsNoTracking().Where(x=>x.IsDelete!=true&&x.StockLotId.HasValue&&lotIds.Contains(x.StockLotId.Value)).ToListAsync(ct);
        var replacements=proposed.ToDictionary(x=>x.ID);
        rows=rows.Select(x=>replacements.GetValueOrDefault(x.ID)??x).ToList();
        var reservations=await _context.PrdStockReservations.AsNoTracking().Where(x=>x.IsDelete!=true&&lotIds.Contains(x.StockLotId)&&(x.Status==PrdReservationStatus.Active||x.Status==PrdReservationStatus.PartiallyUsed)).ToListAsync(ct);
        var converter=await ProductionStockUnitConverter.LoadAsync(_context,lots.Values.Select(x=>x.MaterialId).Distinct().ToList(),ct);
        foreach(var lotId in lotIds)
        {
            var lot=lots[lotId];var historical=rows.Where(x=>x.StockLotId==lotId).ToList();
            var remaining=historical.Where(x=>removedIds==null||!removedIds.Contains(x.ID)).ToList();
            try
            {
                if(historical.Any(x=>x.MaterialId!=lot.MaterialId||x.WarehouseId!=lot.WarehouseId))return $"{lot.LotNumber}: lot, malzeme ve depo bilgileri uyuşmuyor.";
                var balance=remaining.Sum(x=>(x.Direction==PrdStockDirection.In?1:-1)*converter.Quantity(lot.MaterialId,x.UnitId,x.Quantity));
                var value=remaining.Sum(x=>x.Direction==PrdStockDirection.In?x.TotalCost:-x.TotalCost);
                decimal reserved=0;
                foreach(var reservation in reservations.Where(x=>x.StockLotId==lotId&&x.ReservedQuantity-x.UsedQuantity-x.ReleasedQuantity>0))
                {
                    if(reservation.MaterialId!=lot.MaterialId||reservation.WarehouseId!=lot.WarehouseId)return $"{lot.LotNumber}: rezervasyonun malzeme/depo bilgileri uyuşmuyor.";
                    var unit=ProductionReservationUnits.Resolve(reservation,converter,historical.Select(x=>x.UnitId));
                    reserved+=converter.Quantity(lot.MaterialId,unit,reservation.ReservedQuantity-reservation.UsedQuantity-reservation.ReleasedQuantity);
                }
                if(balance<0||reserved>balance||value<0)
                    return $"{lot.LotNumber}: işlem sonrası bakiye {balance:0.######} {converter.BaseUnitName(lot.MaterialId)}, aktif rezervasyon {reserved:0.######}, stok değeri {value:0.######} TRY olur. Bağlı tüketim/rezervasyonu kontrol ediniz.";
            }
            catch(InvalidOperationException ex){return ex.Message;}
            catch(OverflowException){return $"{lot.LotNumber}: miktar veya maliyet desteklenen aralığı aşıyor.";}
        }
        return null;
    }
}
