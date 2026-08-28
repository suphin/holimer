using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;

namespace Ekomers.Models.Entity.Purchasing;

public class PurPurchaseRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? NeededDate { get; set; }
    public string RequestedUserId { get; set; } = string.Empty;
    public PurPurchaseRequestPriority Priority { get; set; } = PurPurchaseRequestPriority.Normal;
    public PurPurchaseRequestStatus Status { get; set; } = PurPurchaseRequestStatus.Draft;
    public DateTime? SubmittedDate { get; set; }
    public string? SubmittedUserId { get; set; }
    public string? Notes { get; set; }
}

public class PurPurchaseRequestLine : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public DateTime? NeededDate { get; set; }
    public PurPurchaseRequestLineStatus Status { get; set; } = PurPurchaseRequestLineStatus.Draft;
    public PurPurchaseRequestSource Source { get; set; } = PurPurchaseRequestSource.Manual;
    public string? SourceReferenceType { get; set; }
    public int? SourceReferenceId { get; set; }
    public string? Reason { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
    public string? ApprovalNote { get; set; }
}

public class PurRequestApprovalHistory : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public PurRequestApprovalAction Action { get; set; }
    public PurPurchaseRequestLineStatus PreviousStatus { get; set; }
    public PurPurchaseRequestLineStatus NewStatus { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal PreviousApprovedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public string? Note { get; set; }
    public DateTime ActionDate { get; set; }
    public string ActionUserId { get; set; } = string.Empty;
}
