namespace Ekomers.Models.ViewModels.Api;

public sealed class ApiPagedResponse<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public IReadOnlyCollection<T> Data { get; set; } = [];
}

public class RecipeCostScenarioApiVM
{
    public int Id { get; set; }
    public string CalculationNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime CalculationDate { get; set; }
    public decimal DefaultProductionQuantity { get; set; }
    public decimal TotalProductionQuantity { get; set; }
    public decimal RecipeCost { get; set; }
    public decimal VariableCost { get; set; }
    public decimal FixedCost { get; set; }
    public decimal TotalCost { get; set; }
    public string Currency { get; set; } = "TRY";
    public int ProductCount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public sealed class RecipeCostScenarioDetailApiVM : RecipeCostScenarioApiVM
{
    public IReadOnlyCollection<RecipeCostProductApiVM> Products { get; set; } = [];
}

public sealed class RecipeCostProductApiVM
{
    public int CalculationId { get; set; }
    public string CalculationNumber { get; set; } = string.Empty;
    public DateTime CalculationDate { get; set; }
    public int RecipeVersionId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductionQuantity { get; set; }
    public string ProductionUnit { get; set; } = string.Empty;
    public decimal RawMaterialUnitCost { get; set; }
    public decimal PackagingUnitCost { get; set; }
    public decimal RecipeUnitCost { get; set; }
    public decimal RecipeTotalCost { get; set; }
    public decimal VariableCostShare { get; set; }
    public decimal FixedCostShare { get; set; }
    public decimal TotalCost { get; set; }
    public decimal TotalUnitCost { get; set; }
    public string Currency { get; set; } = "TRY";
}
