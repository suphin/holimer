using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionOrderCleanupIndexVM
{
    public string? Search { get; set; }
    public PrdProductionOrderStatus? Status { get; set; }
    public List<ProductionOrderCleanupListItemVM> Orders { get; set; } = [];
}

public class ProductionOrderCleanupListItemVM
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
    public DateTime? CreatedDate { get; set; }
    public PrdProductionOrderStatus Status { get; set; }
    public int RequirementCount { get; set; }
    public int WarehouseTaskCount { get; set; }
    public int StockMovementCount { get; set; }
    public int ProductionResultCount { get; set; }
}

public sealed class ProductionOrderCleanupDetailVM : ProductionOrderCleanupListItemVM
{
    public int ReservationCount { get; set; }
    public int WarehouseTaskItemCount { get; set; }
    public int WarehouseTaskLotCount { get; set; }
    public int ProductionActualCount { get; set; }
    public int InventoryDocumentCount { get; set; }
    public int InventoryDocumentLineCount { get; set; }
    public int TotalDependentRecordCount { get; set; }
    public List<ProductionOrderCleanupStockImpactVM> StockImpacts { get; set; } = [];

    [Required(ErrorMessage = "Silme nedenini yazınız.")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Silme nedeni 5-500 karakter olmalıdır.")]
    public string? DeleteReason { get; set; }

    [Required(ErrorMessage = "Üretim emri numarasını yazınız.")]
    public string? ConfirmationOrderNumber { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "Stok bakiyesinin geri alınacağını onaylamalısınız.")]
    public bool ConfirmStockRollback { get; set; }
}

public sealed class ProductionOrderCleanupStockImpactVM
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal CurrentMovementContribution { get; set; }
    public decimal BalanceChangeAfterDelete => -CurrentMovementContribution;
}

public sealed class ProductionOrderCleanupResult
{
    public bool Succeeded { get; init; }
    public string Message { get; init; } = string.Empty;

    public static ProductionOrderCleanupResult Success(string message) => new() { Succeeded = true, Message = message };
    public static ProductionOrderCleanupResult Failure(string message) => new() { Succeeded = false, Message = message };
}
