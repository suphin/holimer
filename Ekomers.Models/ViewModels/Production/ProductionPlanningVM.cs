using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionPlanningVM
{
    public int RecipeVersionId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime TargetProductionDate { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
    public List<SelectListItem> Recipes { get; set; } = [];
    public List<ProductionPlanningLineVM> Plans { get; set; } = [];
    public List<ProductionRequirementLineVM> Requirements { get; set; } = [];
}

public sealed class ProductionPlanningLineVM
{
    public int RecipeVersionId { get; set; }
    public int ProductMaterialId { get; set; }
    public int UnitId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class ProductionRequirementLineVM
{
    public int MaterialId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdMaterialType Type { get; set; }
    public int UnitId { get; set; }
    public decimal TheoreticalQuantity { get; set; }
    public decimal PlannedWasteQuantity { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal PhysicalStockQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableStockQuantity { get; set; }
    public decimal ShortageQuantity => Math.Max(0, RequiredQuantity - AvailableStockQuantity);
    public string Unit { get; set; } = string.Empty;
}

public sealed class ProductionPlanningSessionItem
{
    public int RecipeVersionId { get; set; }
    public decimal Quantity { get; set; }
}
