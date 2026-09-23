namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionCapacityReportVM
{
    public string? Search { get; set; }
    public bool OnlyProducible { get; set; }
    public DateTime CalculationDate { get; set; }
    public int WarehouseCount { get; set; }
    public List<ProductionCapacityProductVM> Products { get; set; } = [];
    public int ProducibleCount => Products.Count(x => x.MaximumProductionQuantity > 0 && !x.HasCalculationIssue);
    public int IssueCount => Products.Count(x => x.HasCalculationIssue);
}

public sealed class ProductionCapacityProductVM
{
    public int RecipeVersionId { get; set; }
    public string RecipeCode { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public decimal BaseQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsWholeNumberUnit { get; set; }
    public decimal MaximumProductionQuantity { get; set; }
    public bool HasCalculationIssue { get; set; }
    public string? CalculationIssue { get; set; }
    public List<ProductionCapacityMaterialVM> Materials { get; set; } = [];
    public IEnumerable<ProductionCapacityMaterialVM> LimitingMaterials => Materials.Where(x => x.IsLimiting);
}

public sealed class ProductionCapacityMaterialVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RequiredPerProductUnit { get; set; }
    public decimal PhysicalStockQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableStockQuantity { get; set; }
    public decimal SupportedProductionQuantity { get; set; }
    public bool IsLimiting { get; set; }
    public bool HasConversionIssue { get; set; }
}
