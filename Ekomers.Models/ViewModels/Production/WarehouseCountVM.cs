using Microsoft.AspNetCore.Mvc.Rendering;
using Ekomers.Models.Entity.Production;

namespace Ekomers.Models.ViewModels.Production;

public sealed class WarehouseCountVM
{
    public int DraftId { get; set; }
    public int Revision { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
    public PrdWarehouseCountDraftStatus Status { get; set; }
    public bool IsReadOnly => Status != PrdWarehouseCountDraftStatus.Draft;
    public int? InventoryDocumentId { get; set; }
    public List<WarehouseCountHistoryVM> History { get; set; } = [];
    public int? SourceDocumentId { get; set; }
    public string? SourceDocumentNumber { get; set; }
    public bool IsCorrection => SourceDocumentId.HasValue;
    public int WarehouseId { get; set; }
    public DateTime CountDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 100;
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<WarehouseCountRowVM> Rows { get; set; } = [];
}

public sealed class WarehouseCountRowVM
{
    public List<WarehouseCountEntryVM> Entries { get; set; } = [];
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal? CriticalQuantity { get; set; }
    public decimal SnapshotCurrentQuantity { get; set; }
    public bool StockChanged => CurrentQuantity != SnapshotCurrentQuantity;
    public string SnapshotQuantity { get; set; } = string.Empty;
    public string? CountedQuantity { get; set; }
}

public sealed class WarehouseCountEntryVM
{
    public int Id { get; set; }
    public string CountedQuantity { get; set; } = "";
    public string? Location { get; set; }
    public string? Notes { get; set; }
    public string? User { get; set; }
    public DateTime? CreatedAt { get; set; }
}

public sealed class WarehouseCountHistoryVM
{
    public int Id { get; set; }
    public DateTime CountDate { get; set; }
    public PrdWarehouseCountDraftStatus Status { get; set; }
    public int? InventoryDocumentId { get; set; }
    public string? Notes { get; set; }
}
