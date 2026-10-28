using System.ComponentModel.DataAnnotations;
using Ekomers.Models.Entity.Production;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class FasonWarehouseFormVM
{
    [Required,StringLength(50)] public string CustomerCode { get; set; } = "";
    [Required,StringLength(250)] public string CustomerName { get; set; } = "";
    [StringLength(500)] public string? Notes { get; set; }
}
public sealed class FasonReceiptFormVM
{
    public Guid RequestId { get; set; } = Guid.NewGuid();
    [Required] public int? WarehouseId { get; set; }
    public DateTime ReceiptDate { get; set; } = DateTime.Today;
    [Required,StringLength(100)] public string DispatchNumber { get; set; } = "";
    public DateTime? DispatchDate { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
    public List<FasonReceiptInputVM> Lines { get; set; } = [new()];
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<SelectListItem> Materials { get; set; } = [];
    public List<SelectListItem> Units { get; set; } = [];
}
public sealed class FasonReceiptInputVM
{
    public int? MaterialId { get; set; }
    public int? UnitId { get; set; }
    public string? Quantity { get; set; }
    [StringLength(100)] public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    [StringLength(250)] public string? Notes { get; set; }
}
public sealed class FasonReceiptListVM
{
    public int Id { get; set; }
    public int WarehouseId { get; set; }
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public string Warehouse { get; set; } = "";
    public string Customer { get; set; } = "";
    public string DispatchNumber { get; set; } = "";
    public FsnReceiptStatus Status { get; set; }
    public string StatusName => Status switch{FsnReceiptStatus.Draft=>"Taslak",FsnReceiptStatus.Posted=>"Stoğa Alındı",FsnReceiptStatus.Cancelled=>"İptal",FsnReceiptStatus.Reversed=>"Geri Alındı",_=>""};
}
public sealed class FasonDocumentPageVM
{
    public int? WarehouseId { get; set; }
    public int Page { get; set; } = 1;
    public int Count { get; set; }
    public int PageCount => (int)Math.Ceiling(Count/50d);
    public List<FasonReceiptListVM> Rows { get; set; } = [];
    public List<SelectListItem> Warehouses { get; set; } = [];
}
public sealed class FasonReceiptDetailVM
{
    public FsnReceipt Receipt { get; set; } = new();
    public FsnWarehouse Warehouse { get; set; } = new();
    public string StatusName => new FasonReceiptListVM{Status=Receipt.Status}.StatusName;
    public List<FasonReceiptDetailLineVM> Lines { get; set; } = [];
}
public sealed class FasonReceiptDetailLineVM
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public string Material { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public decimal OriginalQuantity { get; set; }
    public string OriginalUnit { get; set; } = "";
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "";
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Notes { get; set; }
}
public sealed class FasonStockPageVM
{
    public int? WarehouseId { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int Count { get; set; }
    public int PageCount => (int)Math.Ceiling(Count/50d);
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<FasonStockRowVM> Rows { get; set; } = [];
}
public sealed class FasonStockRowVM
{
    public int WarehouseId { get; set; }
    public string Warehouse { get; set; } = "";
    public string Customer { get; set; } = "";
    public int MaterialId { get; set; }
    public string Material { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int UnitId { get; set; }
    public string Unit { get; set; } = "";
    public decimal Received { get; set; }
    public decimal Reversed { get; set; }
    public decimal Balance => Received-Reversed;
}
public sealed class FasonHistoryPageVM
{
    public int? WarehouseId { get; set; }
    public int? MaterialId { get; set; }
    public string? LotNumber { get; set; }
    public int Page { get; set; } = 1;
    public int Count { get; set; }
    public int PageCount => (int)Math.Ceiling(Count/50d);
    public List<SelectListItem> Warehouses { get; set; } = [];
    public List<FasonHistoryRowVM> Rows { get; set; } = [];
}
public sealed class FasonHistoryRowVM
{
    public int Id { get; set; }
    public int ReceiptId { get; set; }
    public string Number { get; set; } = "";
    public DateTime Date { get; set; }
    public string Warehouse { get; set; } = "";
    public string Customer { get; set; } = "";
    public string Material { get; set; } = "";
    public string LotNumber { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Quantity { get; set; }
    public bool IsReversal { get; set; }
    public string? User { get; set; }
    public string? Reason { get; set; }
    public string? LineNotes { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
}
