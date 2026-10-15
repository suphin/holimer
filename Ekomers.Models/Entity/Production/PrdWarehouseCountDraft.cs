using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity.Production;

public enum PrdWarehouseCountDraftStatus { Draft = 0, Posted = 1, Cancelled = 2 }

public sealed class PrdWarehouseCountDraft : BaseEntity
{
    public string OwnerUserId { get; set; } = "";
    public int WarehouseId { get; set; }
    public int? SourceDocumentId { get; set; }
    public int? InventoryDocumentId { get; set; }
    public DateTime CountDate { get; set; }
    public string? Notes { get; set; }
    public PrdWarehouseCountDraftStatus Status { get; set; }
    public int Revision { get; set; } = 1;
    public DateTime? CompletedAt { get; set; }
}

public sealed class PrdWarehouseCountEntry : BaseEntity
{
    public int DraftId { get; set; }
    public Guid RequestId { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string UnitName { get; set; } = "";
    public decimal SnapshotQuantity { get; set; }
    // Null is an uncounted placeholder used by correction/legacy drafts, not zero stock.
    public decimal? CountedQuantity { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}
