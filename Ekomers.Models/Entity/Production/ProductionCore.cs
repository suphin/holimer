using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;

namespace Ekomers.Models.Entity.Production;

public class PrdUnit : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class PrdMaterial : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdMaterialSource Source { get; set; }
    public PrdMaterialType Type { get; set; }
    public int UnitId { get; set; }
    public string? LogoCode { get; set; }
    public bool LogoActive { get; set; }
    public DateTime? LogoLastSyncDate { get; set; }
    public bool RequiresLotTracking { get; set; } = true;
    public bool RequiresExpirationDate { get; set; }
    public PrdQualityControlRequirement QualityControlRequirement { get; set; } = PrdQualityControlRequirement.NotRequired;
    public decimal? CriticalQuantity { get; set; }
    public string? Description { get; set; }
}

public class PrdMaterialSpecificationSet : BaseEntity
{
    public int MaterialId { get; set; }
    public string SpecificationCode { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public PrdSpecificationSetStatus Status { get; set; } = PrdSpecificationSetStatus.Draft;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
    public string? Notes { get; set; }
}

public class PrdMaterialSpecificationItem : BaseEntity
{
    public int SpecificationSetId { get; set; }
    public int Sequence { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdSpecificationDataType DataType { get; set; }
    public string? UnitName { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? MinimumValue { get; set; }
    public decimal? MaximumValue { get; set; }
    public string? ExpectedText { get; set; }
    public bool? ExpectedBoolean { get; set; }
    public string? AllowedValues { get; set; }
    public string? TestMethod { get; set; }
    public bool IsRequired { get; set; } = true;
    public PrdSpecificationCriticality Criticality { get; set; } = PrdSpecificationCriticality.Major;
    public int DecimalPlaces { get; set; } = 2;
    public string? Notes { get; set; }
}

public class PrdMaterialSpecificationHistory : BaseEntity
{
    public int SpecificationSetId { get; set; }
    public int? SpecificationItemId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActionDate { get; set; }
    public string ActionUserId { get; set; } = string.Empty;
}

public class PrdWarehouse : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdWarehouseType Type { get; set; }
    public string? Description { get; set; }
}

public class PrdStockLot : BaseEntity
{
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public DateTime? ProductionDate { get; set; }
}

public class PrdStockMovement : BaseEntity
{
    public int? InventoryDocumentId { get; set; }
    public int? InventoryDocumentLineId { get; set; }
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public int? StockLotId { get; set; }
    public PrdStockDirection Direction { get; set; }
    public PrdStockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public decimal? OriginalUnitCost { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public PrdStockCostSource CostSource { get; set; } = PrdStockCostSource.Manual;
    public DateTime MovementDate { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public PrdStockDocumentType DocumentType { get; set; }
    public int? DocumentId { get; set; }
    public string? TransferNumber { get; set; }
    public string? Description { get; set; }
}

public class PrdInventoryDocument : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public PrdInventoryDocumentType Type { get; set; }
    public PrdInventoryDocumentStatus Status { get; set; } = PrdInventoryDocumentStatus.Draft;
    public DateTime DocumentDate { get; set; }
    public DateTime? PostingDate { get; set; }
    public string? PostedUserId { get; set; }
    public int? SourceWarehouseId { get; set; }
    public int? TargetWarehouseId { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal TotalCost { get; set; }
    public string? SourceDocumentType { get; set; }
    public int? SourceDocumentId { get; set; }
    public int? ReversalDocumentId { get; set; }
    public string? Notes { get; set; }
}

public class PrdInventoryDocumentLine : BaseEntity
{
    public int InventoryDocumentId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public int? SourceStockLotId { get; set; }
    public int? TargetStockLotId { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal? OriginalUnitCost { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public PrdStockCostSource CostSource { get; set; } = PrdStockCostSource.Manual;
    public string? Notes { get; set; }
}

public class PrdRecipe : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int ProductMaterialId { get; set; }
    public string? Description { get; set; }
}

public class PrdRecipeVersion : BaseEntity
{
    public int RecipeId { get; set; }
    public int VersionNumber { get; set; }
    public decimal BaseQuantity { get; set; }
    public int UnitId { get; set; }
    public PrdRecipeStatus Status { get; set; } = PrdRecipeStatus.Draft;
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
    public string? Notes { get; set; }
}

public class PrdRecipeItem : BaseEntity
{
    public int RecipeVersionId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public decimal PlannedWasteRate { get; set; }
    public int Sequence { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? AlternativeGroupCode { get; set; }
    public string? Notes { get; set; }
}

public class PrdRecipeHistory : BaseEntity
{
    public int RecipeId { get; set; }
    public int? RecipeVersionId { get; set; }
    public int? RecipeItemId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string? ActionUserId { get; set; }
}

public class PrdProductionPlanHeader : BaseEntity
{
    public string PlanNumber { get; set; } = string.Empty;
    public DateTime PlanDate { get; set; }
    public DateTime TargetProductionDate { get; set; }
    public PrdProductionPlanHeaderStatus Status { get; set; } = PrdProductionPlanHeaderStatus.Draft;
    public DateTime? CalculatedDate { get; set; }
    public DateTime? LockedDate { get; set; }
    public string? LockedUserId { get; set; }
    public string? Notes { get; set; }
}

public class PrdProductionPlan : BaseEntity
{
    public int? ProductionPlanHeaderId { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
    public int RecipeVersionId { get; set; }
    public int ProductMaterialId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public int UnitId { get; set; }
    public DateTime PlannedProductionDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public PrdProductionPlanStatus Status { get; set; } = PrdProductionPlanStatus.Draft;
    public bool IsConvertedToOrder { get; set; }
    public string? Notes { get; set; }
}

public class PrdProductionPlanRequirement : BaseEntity
{
    public int ProductionPlanHeaderId { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal TheoreticalQuantity { get; set; }
    public decimal PlannedWasteQuantity { get; set; }
    public decimal TotalRequiredQuantity { get; set; }
    public decimal PhysicalStockQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableStockQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public DateTime CalculationDate { get; set; }
}

public class PrdProductionOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int ProductionPlanId { get; set; }
    public int RecipeVersionId { get; set; }
    public int ProductMaterialId { get; set; }
    public int SourceWarehouseId { get; set; }
    public int ProductionWarehouseId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public int UnitId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime PlannedProductionDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public PrdProductionOrderStatus Status { get; set; } = PrdProductionOrderStatus.Draft;
    public string? Notes { get; set; }
}

public class PrdMaterialRequirement : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public int RecipeItemId { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal TheoreticalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal IssuedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
}

public class PrdStockReservation : BaseEntity
{
    public int MaterialRequirementId { get; set; }
    public int MaterialId { get; set; }
    public int WarehouseId { get; set; }
    public int StockLotId { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal UsedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; }
    public PrdReservationStatus Status { get; set; } = PrdReservationStatus.Active;
}

public class PrdWarehouseTask : BaseEntity
{
    public string TaskNumber { get; set; } = string.Empty;
    public int ProductionOrderId { get; set; }
    public int SourceWarehouseId { get; set; }
    public int TargetWarehouseId { get; set; }
    public PrdWarehouseTaskStatus Status { get; set; } = PrdWarehouseTaskStatus.Waiting;
    public string? AssignedUserId { get; set; }
    public DateTime RequestDate { get; set; }
    public DateTime? PreparedDate { get; set; }
    public DateTime? ShippedDate { get; set; }
    public DateTime? DeliveredDate { get; set; }
    public string? Notes { get; set; }
}

public class PrdWarehouseTaskItem : BaseEntity
{
    public int WarehouseTaskId { get; set; }
    public int MaterialRequirementId { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal PreparedQuantity { get; set; }
    public decimal ShippedQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
}

public class PrdWarehouseTaskLot : BaseEntity
{
    public int WarehouseTaskItemId { get; set; }
    public int StockReservationId { get; set; }
    public int StockLotId { get; set; }
    public decimal Quantity { get; set; }
    public decimal? PreparedQuantity { get; set; }
    public decimal? ShippedQuantity { get; set; }
}

public class PrdProductionMaterialActual : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public int MaterialRequirementId { get; set; }
    public int MaterialId { get; set; }
    public int StockLotId { get; set; }
    public int UnitId { get; set; }
    public decimal IssuedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal WasteQuantity { get; set; }
    public string? WasteReason { get; set; }
    public string? Notes { get; set; }
}

public class PrdProductionResult : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public int ProductMaterialId { get; set; }
    public int WarehouseId { get; set; }
    public int UnitId { get; set; }
    public decimal ActualQuantity { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? StockLotId { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? TransportationCost { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? OtherCost { get; set; }
    public decimal? TotalProductionCost { get; set; }
    public decimal? UnitProductionCost { get; set; }
    public string? OtherCostDescription { get; set; }
    public string? Notes { get; set; }
}
