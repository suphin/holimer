using Ekomers.Models.Enums;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionExecutionListVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public PrdProductionOrderStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public decimal MaterialCost { get; set; }
    public decimal AdditionalCost { get; set; }
    public decimal TotalProductionCost { get; set; }
    public decimal UnitProductionCost { get; set; }
}

public sealed class ProductionExecutionDetailVM
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public int UnitId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public PrdProductionOrderStatus Status { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public DateTime ProductionDate { get; set; } = DateTime.Today;
    public DateTime? ExpirationDate { get; set; }
    public string ActualQuantityInput { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public decimal TransportationCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalProductionCost { get; set; }
    public decimal UnitProductionCost { get; set; }
    public string TransportationCostInput { get; set; } = "0";
    public string LaborCostInput { get; set; } = "0";
    public string OtherCostInput { get; set; } = "0";
    public string? OtherCostDescription { get; set; }
    public string? Notes { get; set; }
    public List<ProductionExecutionMaterialVM> Materials { get; set; } = [];
}

public sealed class ProductionExecutionMaterialVM
{
    public int ActualId { get; set; }
    public int MaterialRequirementId { get; set; }
    public int MaterialId { get; set; }
    public int StockLotId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal IssuedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string ConsumedQuantityInput { get; set; } = string.Empty;
    public string ReturnedQuantityInput { get; set; } = string.Empty;
    public string WasteQuantityInput { get; set; } = string.Empty;
    public string? WasteReason { get; set; }
    public string? Notes { get; set; }
}
