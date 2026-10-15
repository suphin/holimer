using Ekomers.Data;
using Ekomers.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public static class GoodsReceiptStockWorkflow
{
    public const string DirectSource = "PurGoodsReceiptDirect";
    public const string DispositionSource = "PurQualityInspectionDisposition";

    public sealed record DirectEntry(int ReceiptLineId, int DocumentId, string DocumentNumber,
        int InventoryLineId, int? WarehouseId, int? StockLotId, DateTime? PostingDate, string? User);

    // Receipt sequences remain stable while any stock entry is active: editing is blocked until reversal.
    public static Task<List<DirectEntry>> LoadDirectEntriesAsync(ApplicationDbContext context, int receiptId, CancellationToken ct) =>
        (from document in context.PrdInventoryDocuments.AsNoTracking()
         join stockLine in context.PrdInventoryDocumentLines.AsNoTracking() on document.ID equals stockLine.InventoryDocumentId
         join receiptLine in context.PurGoodsReceiptLines.AsNoTracking()
             on new { ReceiptId = document.SourceDocumentId, stockLine.Sequence, stockLine.MaterialId, stockLine.UnitId }
             equals new { ReceiptId = (int?)receiptLine.GoodsReceiptId, receiptLine.Sequence, receiptLine.MaterialId, receiptLine.UnitId }
         where document.SourceDocumentType == DirectSource && document.SourceDocumentId == receiptId &&
               document.Status == PrdInventoryDocumentStatus.Posted && document.IsDelete != true &&
               stockLine.IsDelete != true && receiptLine.IsDelete != true
         select new DirectEntry(receiptLine.ID, document.ID, document.DocumentNumber, stockLine.ID,
             document.TargetWarehouseId, stockLine.TargetStockLotId, document.PostingDate, document.PostedUserId)).ToListAsync(ct);

    public static Task<bool> HasDirectEntryAsync(ApplicationDbContext context, int receiptId, CancellationToken ct) =>
        context.PrdInventoryDocuments.AsNoTracking().AnyAsync(x => x.SourceDocumentType == DirectSource &&
            x.SourceDocumentId == receiptId && x.Status == PrdInventoryDocumentStatus.Posted && x.IsDelete != true, ct);

    public static async Task RecalculateAsync(ApplicationDbContext context, int receiptId, DateTime now, string user, CancellationToken ct)
    {
        var receipt = await context.PurGoodsReceipts.FirstAsync(x => x.ID == receiptId && x.IsDelete != true, ct);
        if (receipt.Status == PurGoodsReceiptStatus.Cancelled) return;
        var lines = await context.PurGoodsReceiptLines.AsNoTracking()
            .Where(x => x.GoodsReceiptId == receiptId && x.IsDelete != true)
            .Select(x => new { x.ID, x.QuarantineInventoryDocumentLineId }).ToListAsync(ct);
        var direct = await LoadDirectEntriesAsync(context, receiptId, ct);
        var inspections = await context.PurQualityInspections.AsNoTracking()
            .Where(x => x.GoodsReceiptId == receiptId && x.IsDelete != true)
            .Select(x => new { x.ID, x.GoodsReceiptLineId, x.Status }).ToListAsync(ct);
        var inspectionIds = inspections.Select(x => x.ID).ToList();
        var dispositions = await context.PrdInventoryDocuments.AsNoTracking()
            .Where(x => x.SourceDocumentType == DispositionSource && x.SourceDocumentId.HasValue &&
                inspectionIds.Contains(x.SourceDocumentId.Value) && x.Status == PrdInventoryDocumentStatus.Posted && x.IsDelete != true)
            .OrderBy(x => x.ID).Select(x => new { InspectionId = x.SourceDocumentId!.Value, x.Type }).ToListAsync(ct);
        var latest = dispositions.GroupBy(x => x.InspectionId).Select(x => x.Last()).ToList();
        receipt.Status = DetermineStatus(lines.Count, direct.Select(x => x.ReceiptLineId).Distinct().Count(),
            lines.Count(x => x.QuarantineInventoryDocumentLineId.HasValue), inspections.Select(x => x.Status).ToList(),
            latest.Select(x => x.Type).ToList());
        receipt.UpdateDate = now;
        receipt.UpdateUserID = user;
        await context.SaveChangesAsync(ct);
    }

    public static PurGoodsReceiptStatus DetermineStatus(int total, int direct, int quarantine,
        IReadOnlyCollection<PrdQualityControlStatus> inspections, IReadOnlyCollection<PrdInventoryDocumentType> dispositions)
    {
        var completed = direct + dispositions.Count;
        if (total > 0 && completed == total)
        {
            if (dispositions.All(x => x == PrdInventoryDocumentType.WarehouseTransfer)) return PurGoodsReceiptStatus.TransferredToStock;
            if (direct == 0 && dispositions.All(x => x == PrdInventoryDocumentType.SupplierReturn)) return PurGoodsReceiptStatus.Returned;
            if (direct == 0 && dispositions.All(x => x == PrdInventoryDocumentType.ScrapTransfer)) return PurGoodsReceiptStatus.Scrapped;
            return PurGoodsReceiptStatus.CompletedWithMixedDisposition;
        }
        if (completed > 0 || (quarantine > 0 && direct + quarantine < total)) return PurGoodsReceiptStatus.StockPartiallyCompleted;
        if (quarantine == 0) return PurGoodsReceiptStatus.Recorded;
        if (inspections.Count < quarantine || inspections.Any(x => x is PrdQualityControlStatus.Pending or PrdQualityControlStatus.Sampled))
            return PurGoodsReceiptStatus.InQuarantine;
        if (inspections.All(x => x is PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval))
            return PurGoodsReceiptStatus.QualityApproved;
        if (inspections.All(x => x == PrdQualityControlStatus.Rejected)) return PurGoodsReceiptStatus.QualityRejected;
        return PurGoodsReceiptStatus.QualityPartiallyDecided;
    }
}
