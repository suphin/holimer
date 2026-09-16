using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionCustomerOrderListVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ExternalOrderNumber { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public PrdCustomerOrderPriority Priority { get; set; }
    public PrdCustomerOrderStatus Status { get; set; }
    public int LineCount { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal PlannedQuantity { get; set; }
}

public sealed class ProductionCustomerOrderFormVM
{
    public int? Id { get; set; }
    public string? OrderNumber { get; set; }
    [Display(Name="Harici sipariş no")]
    public string? ExternalOrderNumber { get; set; }
    [Display(Name="Müşteri kodu")]
    public string? CustomerCode { get; set; }
    [Required, Display(Name="Müşteri / kanal")]
    public string CustomerName { get; set; } = string.Empty;
    [DataType(DataType.Date), Display(Name="Sipariş tarihi")]
    public DateTime OrderDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name="İstenen teslim")]
    public DateTime RequestedDeliveryDate { get; set; } = DateTime.Today.AddDays(7);
    public PrdCustomerOrderPriority Priority { get; set; } = PrdCustomerOrderPriority.Normal;
    public string? Notes { get; set; }
    public List<ProductionCustomerOrderLineFormVM> Lines { get; set; } = [new()];
    public List<SelectListItem> Products { get; set; } = [];
}

public sealed class ProductionCustomerOrderLineFormVM
{
    public int? Id { get; set; }
    public int ProductMaterialId { get; set; }
    public decimal Quantity { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductionCustomerOrderDetailVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string? ExternalOrderNumber { get; set; }
    public string? CustomerCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime RequestedDeliveryDate { get; set; }
    public PrdCustomerOrderPriority Priority { get; set; }
    public PrdCustomerOrderStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<ProductionCustomerOrderDetailLineVM> Lines { get; set; } = [];
}

public sealed class ProductionCustomerOrderDetailLineVM
{
    public int Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal RemainingQuantity => Math.Max(0, OrderedQuantity-PlannedQuantity);
    public string Unit { get; set; } = string.Empty;
    public DateTime? RequestedDeliveryDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductionOrderPlanningPoolVM
{
    public string? Search { get; set; }
    public List<ProductionOrderPlanningPoolLineVM> Lines { get; set; } = [];
}

public sealed class ProductionOrderPlanningPoolLineVM
{
    public int OrderLineId { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime RequestedDeliveryDate { get; set; }
    public PrdCustomerOrderPriority Priority { get; set; }
    public int RecipeVersionId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal CurrentFinishedStock { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class ProductionOrderPlanningSelectionVM
{
    public List<ProductionOrderPlanningSelectionLineVM> Lines { get; set; } = [];
}

public sealed class ProductionOrderPlanningSelectionLineVM
{
    public int OrderLineId { get; set; }
    public bool Selected { get; set; }
    public decimal Quantity { get; set; }
}
