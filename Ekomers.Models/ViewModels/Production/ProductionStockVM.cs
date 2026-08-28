using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionStockBalanceVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class LegacyStockImportVM
{
    public int? LegacyWarehouseId { get; set; }
    public int? TargetWarehouseId { get; set; }
    public List<SelectListItem> LegacyWarehouses { get; set; } = [];
    public List<SelectListItem> TargetWarehouses { get; set; } = [];
    public List<LegacyStockImportLineVM> Lines { get; set; } = [];
    public bool AlreadyImported { get; set; }
    public int MatchedCount => Lines.Count(x=>x.PrdMaterialId.HasValue);
    public int UnmatchedCount => Lines.Count(x=>!x.PrdMaterialId.HasValue);
}

public sealed class LegacyStockImportLineVM
{
    public int LegacyMaterialId { get; set; }
    public int? PrdMaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
