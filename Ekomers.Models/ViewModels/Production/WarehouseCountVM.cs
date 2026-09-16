using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class WarehouseCountVM
{
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
