using Ekomers.Models.Enums;

namespace Ekomers.Models.ViewModels.Production;

public sealed class MaterialUsageReportVM
{
    public string? Search { get; set; }
    public string MaterialType { get; set; } = "all";
    public string VersionScope { get; set; } = "active";
    public int TotalMatchedMaterialCount { get; set; }
    public bool IsSearchPerformed => !string.IsNullOrWhiteSpace(Search);
    public List<MaterialUsageMaterialVM> Materials { get; set; } = [];
    public List<MaterialUsageRowVM> Rows { get; set; } = [];
}

public sealed class MaterialUsageMaterialVM
{
    public int MaterialId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdMaterialType Type { get; set; }
    public string BaseUnit { get; set; } = string.Empty;
    public int UsageCount { get; set; }
}

public sealed class MaterialUsageRowVM
{
    public int MaterialId { get; set; }
    public int RecipeId { get; set; }
    public int RecipeVersionId { get; set; }
    public string RecipeCode { get; set; } = string.Empty;
    public string RecipeName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public PrdRecipeStatus VersionStatus { get; set; }
    public decimal BaseQuantity { get; set; }
    public string OutputUnit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UsageUnit { get; set; } = string.Empty;
    public decimal PlannedWasteRate { get; set; }
    public int Sequence { get; set; }
    public bool IsRequired { get; set; }
    public string? AlternativeGroupCode { get; set; }
    public string? Notes { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public decimal QuantityIncludingWaste => Quantity * (1m + PlannedWasteRate / 100m);
}
