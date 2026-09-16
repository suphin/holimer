using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class RecipeCostCalculationVM
{
    public DateTime CalculationDate { get; set; }
    public string ScenarioName { get; set; } = string.Empty;
    public List<RecipeCostCalculationRowVM> Rows { get; set; } = [];
    public int MissingCostProductCount => Rows.Count(x => x.MissingCostCount > 0);
}

public sealed class RecipeCostScenarioSaveVM
{
    public string Name { get; set; } = string.Empty;
    public string DefaultProductionQuantity { get; set; } = string.Empty;
    public string FixedCost { get; set; } = string.Empty;
    public string VariableCost { get; set; } = string.Empty;
    public List<RecipeCostScenarioSaveLineVM> Lines { get; set; } = [];
}

public sealed class RecipeCostScenarioSaveLineVM
{
    public int RecipeVersionId { get; set; }
    public string ProductionQuantity { get; set; } = string.Empty;
}

public class RecipeCostScenarioListRowVM
{
    public int Id { get; set; }
    public string ScenarioNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CalculationDate { get; set; }
    public decimal TotalProductionQuantity { get; set; }
    public decimal TotalRecipeCost { get; set; }
    public decimal FixedCost { get; set; }
    public decimal VariableCost { get; set; }
    public decimal TotalCost { get; set; }
    public int LineCount { get; set; }
    public string CreatedUser { get; set; } = string.Empty;
}

public sealed class RecipeCostScenarioDetailVM : RecipeCostScenarioListRowVM
{
    public decimal DefaultProductionQuantity { get; set; }
    public List<RecipeCostScenarioDetailLineVM> Lines { get; set; } = [];
}

public sealed class RecipeCostScenarioDetailLineVM
{
    public int RecipeVersionId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductionUnit { get; set; } = string.Empty;
    public decimal ProductionQuantity { get; set; }
    public decimal RawMaterialUnitCost { get; set; }
    public decimal PackagingUnitCost { get; set; }
    public decimal RecipeMaterialCost { get; set; }
    public decimal VariableCostShare { get; set; }
    public decimal FixedCostShare { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalUnitCost { get; set; }
}

public sealed class RecipeCostCalculationRowVM
{
    public int RecipeVersionId { get; set; }
    public string RecipeCode { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public PrdRecipeStatus RecipeStatus { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductionUnit { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public decimal RawMaterialUnitCost { get; set; }
    public decimal PackagingUnitCost { get; set; }
    public decimal RecipeUnitCost => RawMaterialUnitCost + PackagingUnitCost;
    public int RawMaterialCount { get; set; }
    public int PackagingCount { get; set; }
    public int StockCostItemCount { get; set; }
    public int ReferenceCostItemCount { get; set; }
    public int LegacyCostItemCount { get; set; }
    public int MissingCostCount => MissingCosts.Count;
    public List<RecipeCostItemDetailVM> CostItems { get; set; } = [];
    public List<RecipeMissingCostVM> MissingCosts { get; set; } = [];
}

public sealed class RecipeCostItemDetailVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public decimal RecipeQuantity { get; set; }
    public decimal PlannedWasteRate { get; set; }
    public decimal CostQuantity { get; set; }
    public string RecipeUnit { get; set; } = string.Empty;
    public decimal? PricedQuantity { get; set; }
    public string CostUnit { get; set; } = string.Empty;
    public decimal? UnitCostTry { get; set; }
    public decimal ItemCost { get; set; }
    public decimal ProductUnitCostContribution { get; set; }
    public string CostSource { get; set; } = string.Empty;
    public string CostSourceDetail { get; set; } = string.Empty;
    public bool IncludedInCalculation { get; set; }
    public string? Reason { get; set; }
}

public sealed class RecipeMissingCostVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public decimal RecipeQuantity { get; set; }
    public decimal PlannedWasteRate { get; set; }
    public decimal CostQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}

public sealed class ProductionUnitConversionManagementVM
{
    public int? MaterialId { get; set; }
    public int FromUnitId { get; set; }
    public int ToUnitId { get; set; }
    public string Factor { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<SelectListItem> Materials { get; set; } = [];
    public List<SelectListItem> Units { get; set; } = [];
    public List<ProductionUnitConversionRowVM> Rows { get; set; } = [];
}

public sealed class ProductionUnitConversionRowVM
{
    public int Id { get; set; }
    public string Material { get; set; } = string.Empty;
    public string FromUnit { get; set; } = string.Empty;
    public string ToUnit { get; set; } = string.Empty;
    public decimal Factor { get; set; }
    public string? Description { get; set; }
}
