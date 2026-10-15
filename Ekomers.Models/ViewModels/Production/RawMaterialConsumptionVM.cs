using System.ComponentModel.DataAnnotations;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class RawMaterialConsumptionCreateVM
{
    public int? WarehouseId { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    [Required, StringLength(500, MinimumLength = 5)]
    public string? Notes { get; set; }
    public List<RawMaterialConsumptionInputVM> Lines { get; set; } = [];
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<RawMaterialConsumptionLotVM> Lots { get; set; } = [];
}

public sealed class RawMaterialConsumptionInputVM
{
    public int? StockLotId { get; set; }
    public string? Quantity { get; set; }
    [StringLength(250)] public string? Notes { get; set; }
}

public sealed class RawMaterialConsumptionLotVM
{
    public int StockLotId { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Unit { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity => Math.Max(0, PhysicalQuantity - ReservedQuantity);
    public decimal StockValue { get; set; }
    public decimal UnitCost => PhysicalQuantity > 0 ? decimal.Round(StockValue / PhysicalQuantity, 6) : 0;
    public string? BlockReason { get; set; }
    public bool CanConsume => BlockReason == null && AvailableQuantity > 0;
}

public sealed class RawMaterialConsumptionListVM
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public string Warehouse { get; set; } = "";
    public PrdInventoryDocumentStatus Status { get; set; }
    public int LineCount { get; set; }
    public decimal TotalCost { get; set; }
    public string? CreatedBy { get; set; }
    public string? Notes { get; set; }
}

public sealed class RawMaterialConsumptionDetailVM
{
    public int Id { get; set; }
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public string Warehouse { get; set; } = "";
    public PrdInventoryDocumentStatus Status { get; set; }
    public DateTime? PostingDate { get; set; }
    public string? PostedBy { get; set; }
    public string? CreatedBy { get; set; }
    public string? Notes { get; set; }
    public decimal TotalCost { get; set; }
    public int? ReversalDocumentId { get; set; }
    public List<RawMaterialConsumptionDetailLineVM> Lines { get; set; } = [];
}

public sealed class RawMaterialConsumptionDetailLineVM
{
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string Unit { get; set; } = "";
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
}
