using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public class InventoryDocumentListVM
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public PrdInventoryDocumentType Type { get; set; }
    public PrdInventoryDocumentStatus Status { get; set; }
    public DateTime DocumentDate { get; set; }
    public string SourceWarehouse { get; set; } = string.Empty;
    public string TargetWarehouse { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public decimal TotalCost { get; set; }
}

public sealed class InventoryDocumentCreateVM
{
    public PrdInventoryDocumentType Type { get; set; }
    public int? SourceWarehouseId { get; set; }
    public int? TargetWarehouseId { get; set; }
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    public string CurrencyCode { get; set; } = "TRY";
    public string ExchangeRate { get; set; } = "1";
    public string? Notes { get; set; }
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<SelectListItem> Materials { get; set; } = [];
    public List<SelectListItem> SourceLots { get; set; } = [];
    public List<InventoryDocumentCreateLineVM> Lines { get; set; } = [];
}

public sealed class InventoryDocumentCreateLineVM
{
    public int? MaterialId { get; set; }
    public int? SourceStockLotId { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Quantity { get; set; }
    public string? UnitCost { get; set; }
    public string? Notes { get; set; }
}

public sealed class InventoryDocumentDetailVM : InventoryDocumentListVM
{
    public DateTime? PostingDate { get; set; }
    public string? PostedUserId { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; }
    public string? Notes { get; set; }
    public List<InventoryDocumentDetailLineVM> Lines { get; set; } = [];
}

public sealed class InventoryDocumentDetailLineVM
{
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ExpirationDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
}

public sealed class InventoryLotBalanceVM
{
    public int StockLotId { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public decimal PhysicalQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal AvailableQuantity => Math.Max(0,PhysicalQuantity-ReservedQuantity);
    public decimal StockValue { get; set; }
    public decimal UnitCost => PhysicalQuantity==0?0:StockValue/PhysicalQuantity;
    public string Unit { get; set; } = string.Empty;
}
