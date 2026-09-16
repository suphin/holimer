namespace Ekomers.Models.ViewModels;

public sealed class OperationalDashboardVM
{
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
    public bool CanSeePurchasing { get; set; }
    public bool CanSeeCustomerOrders { get; set; }
    public bool CanSeeProduction { get; set; }
    public bool CanSeeQuality { get; set; }
    public bool CanSeeWarehouse { get; set; }

    public PurchasingDashboardSummaryVM Purchasing { get; set; } = new();
    public CustomerOrderDashboardSummaryVM CustomerOrders { get; set; } = new();
    public ProductionDashboardSummaryVM Production { get; set; } = new();
    public QualityDashboardSummaryVM Quality { get; set; } = new();
    public WarehouseDashboardSummaryVM Warehouse { get; set; } = new();

    public bool HasAnyModule => CanSeePurchasing || CanSeeCustomerOrders || CanSeeProduction || CanSeeQuality || CanSeeWarehouse;
    public int TotalAttention =>
        (CanSeePurchasing ? Purchasing.PendingRequests + Purchasing.PendingQuotations + Purchasing.QuarantineReceipts : 0) +
        (CanSeeCustomerOrders ? CustomerOrders.ReadyForPlanning + CustomerOrders.Overdue : 0) +
        (CanSeeProduction ? Production.MaterialWaiting + Production.WarehouseTasksWaiting : 0) +
        (CanSeeQuality ? Quality.Pending + Quality.Sampled : 0) +
        (CanSeeWarehouse ? Warehouse.CriticalMaterials : 0);
}

public sealed class PurchasingDashboardSummaryVM
{
    public int PendingRequests { get; set; }
    public int PendingQuotations { get; set; }
    public int OpenOrders { get; set; }
    public int QuarantineReceipts { get; set; }
}

public sealed class CustomerOrderDashboardSummaryVM
{
    public int ReadyForPlanning { get; set; }
    public int InProduction { get; set; }
    public int Overdue { get; set; }
    public List<DashboardCustomerOrderRowVM> Upcoming { get; set; } = new();
}

public sealed class DashboardCustomerOrderRowVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsOverdue { get; set; }
}

public sealed class ProductionDashboardSummaryVM
{
    public int OpenPlans { get; set; }
    public int ActiveOrders { get; set; }
    public int MaterialWaiting { get; set; }
    public int WarehouseTasksWaiting { get; set; }
    public List<DashboardProductionOrderRowVM> Upcoming { get; set; } = new();
}

public sealed class DashboardProductionOrderRowVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class QualityDashboardSummaryVM
{
    public int Pending { get; set; }
    public int Sampled { get; set; }
    public int Conditional { get; set; }
    public int Rejected { get; set; }
    public List<DashboardQualityRowVM> Waiting { get; set; } = new();
}

public sealed class DashboardQualityRowVM
{
    public int Id { get; set; }
    public string InspectionNumber { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public sealed class WarehouseDashboardSummaryVM
{
    public int ActiveWarehouses { get; set; }
    public int StockedMaterials { get; set; }
    public int CriticalMaterials { get; set; }
    public decimal TotalStockValue { get; set; }
    public List<DashboardCriticalStockRowVM> CriticalStock { get; set; } = new();
}

public sealed class DashboardCriticalStockRowVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal CriticalQuantity { get; set; }
}
