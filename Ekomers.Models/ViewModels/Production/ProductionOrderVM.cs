using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionOrderCreateVM
{
    public int PlanHeaderId { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
    public DateTime PlannedProductionDate { get; set; }
    public int SourceWarehouseId { get; set; }
    public int ProductionWarehouseId { get; set; }
    public bool AllowShortage { get; set; }
    public decimal TotalShortageQuantity { get; set; }
    public List<SelectListItem> SourceWarehouses { get; set; } = [];
    public List<SelectListItem> ProductionWarehouses { get; set; } = [];
    public List<ProductionOrderCreateLineVM> Lines { get; set; } = [];
}

public sealed class ProductionOrderCreateLineVM
{
    public int ProductionPlanId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int RecipeVersionNumber { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ProductionOrderListVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string PlanNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PlannedProductionDate { get; set; }
    public PrdProductionOrderStatus Status { get; set; }
    public int RequirementCount { get; set; }
}

public sealed class ProductionOrderDetailVM : ProductionOrderListVM
{
    public string SourceWarehouse { get; set; } = string.Empty;
    public string ProductionWarehouse { get; set; } = string.Empty;
    public int RecipeVersionNumber { get; set; }
    public string? Notes { get; set; }
    public List<ProductionOrderRequirementVM> Requirements { get; set; } = [];
}

public sealed class ProductionOrderRequirementVM
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public PrdMaterialType MaterialType { get; set; }
    public decimal RequiredQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal IssuedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}
