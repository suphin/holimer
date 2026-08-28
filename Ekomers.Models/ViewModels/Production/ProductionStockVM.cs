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
    public int MissingCostCount => Lines.Count(x=>x.PrdMaterialId.HasValue&&!x.UnitCost.HasValue);
    public int ImportableCount => Lines.Count(x=>x.PrdMaterialId.HasValue&&x.UnitCost.HasValue&&x.Quantity>0);
    public decimal ImportableTotalCost => Lines.Where(x=>x.PrdMaterialId.HasValue&&x.UnitCost.HasValue&&x.Quantity>0).Sum(x=>x.TotalCost??0);
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
    public decimal? UnitCost { get; set; }
    public decimal? TotalCost { get; set; }
    public string CostSource { get; set; } = string.Empty;
    public decimal? CriticalQuantity { get; set; }
}

public sealed class ProductionStockReportVM
{
    public string? Code { get; set; }
    public string? Name { get; set; }
    public int? WarehouseId { get; set; }
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<ProductionStockReportItemVM> Items { get; set; } = [];
    public List<ProductionStockUnitSummaryVM> UnitSummaries { get; set; } = [];
    public List<ProductionStockBalanceVM> Lots { get; set; } = [];
    public int MaterialCount => Items.Select(x=>x.MaterialId).Distinct().Count();
    public decimal TotalStockValue => Items.Sum(x=>x.TotalCost);
    public decimal TotalIncoming => Items.Sum(x=>x.IncomingQuantity);
    public decimal TotalOutgoing => Items.Sum(x=>x.OutgoingQuantity);
    public int CriticalMaterialCount => Items.Where(x=>x.IsCritical).Select(x=>x.MaterialId).Distinct().Count();
}

public sealed class ProductionStockReportItemVM
{
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal IncomingQuantity { get; set; }
    public decimal OutgoingQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? CriticalQuantity { get; set; }
    public bool IsCritical => CriticalQuantity.HasValue&&CriticalQuantity.Value>0&&RemainingQuantity<=CriticalQuantity.Value;
}

public sealed class ProductionStockUnitSummaryVM
{
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}
