using Ekomers.Models.Enums;

namespace Ekomers.Models.ViewModels.Production;

public class ProductionWarehouseTaskListVM
{
    public int Id { get; set; }
    public string TaskNumber { get; set; } = string.Empty;
    public int ProductionOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SourceWarehouse { get; set; } = string.Empty;
    public string TargetWarehouse { get; set; } = string.Empty;
    public PrdWarehouseTaskStatus Status { get; set; }
    public DateTime RequestDate { get; set; }
    public string? AssignedUserId { get; set; }
    public int ItemCount { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal PreparedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
}

public sealed class ProductionWarehouseTaskDetailVM : ProductionWarehouseTaskListVM
{
    public DateTime? PreparedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? Notes { get; set; }
    public List<ProductionWarehouseTaskItemVM> Items { get; set; } = [];
}

public sealed class ProductionWarehouseTaskItemVM
{
    public int Id { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal PreparedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public decimal PreparationShortageQuantity => Math.Max(0, RequestedQuantity - PreparedQuantity);
    public decimal OverPreparedQuantity => Math.Max(0, PreparedQuantity - RequestedQuantity);
    public List<ProductionWarehouseTaskLotVM> Lots { get; set; } = [];
}

public sealed class ProductionWarehouseTaskLotVM
{
    public int Id { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal PreparedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal TaskQuantity { get; set; }
    public decimal TaskPreparedQuantity { get; set; }
    public decimal TaskShippedQuantity { get; set; }
    public decimal TaskUnitConversionFactor { get; set; } = 1;
    public string TaskUnit { get; set; } = string.Empty;
    public bool UsesDifferentUnit => !string.Equals(Unit, TaskUnit, StringComparison.OrdinalIgnoreCase);
}

public sealed class ProductionWarehousePreparationInputVM
{
    public int Id { get; set; }
    public bool CompletePreparation { get; set; }
    public List<ProductionWarehousePreparationLotInputVM> Lots { get; set; } = [];
}

public sealed class ProductionWarehousePreparationLotInputVM
{
    public int Id { get; set; }
    public string PreparedQuantityInput { get; set; } = string.Empty;
}
