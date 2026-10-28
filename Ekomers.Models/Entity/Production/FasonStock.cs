using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity.Production;

// Customer-owned inventory is deliberately separate from PrdWarehouse/PrdStockMovement.
public sealed class FsnWarehouse : BaseEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string? Notes { get; set; }
}
public enum FsnReceiptStatus { Draft=0, Posted=1, Cancelled=2, Reversed=3 }
public sealed class FsnReceipt : BaseEntity
{
    public Guid RequestId { get; set; }
    public int WarehouseId { get; set; }
    public string Number { get; set; } = "";
    public DateTime ReceiptDate { get; set; }
    public string DispatchNumber { get; set; } = "";
    public DateTime? DispatchDate { get; set; }
    public string? Notes { get; set; }
    public FsnReceiptStatus Status { get; set; }
    public DateTime? PostedDate { get; set; }
    public string? PostedUser { get; set; }
    public DateTime? ReversedDate { get; set; }
    public string? ReversedUser { get; set; }
    public string? ReversalReason { get; set; }
}
public sealed class FsnReceiptLine : BaseEntity
{
    public int ReceiptId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public string LotNumber { get; set; } = "";
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int OriginalUnitId { get; set; }
    public decimal OriginalQuantity { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
}
public sealed class FsnStockMovement : BaseEntity
{
    public int ReceiptLineId { get; set; }
    public bool IsReversal { get; set; }
    public decimal Quantity { get; set; }
    public DateTime MovementDate { get; set; }
    public string? Reason { get; set; }
}
