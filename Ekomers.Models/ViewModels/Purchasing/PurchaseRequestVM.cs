using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class PurchaseRequestListVM
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? NeededDate { get; set; }
    public string RequestedUserId { get; set; } = string.Empty;
    public PurPurchaseRequestPriority Priority { get; set; }
    public PurPurchaseRequestStatus Status { get; set; }
    public int LineCount { get; set; }
    public int PendingLineCount { get; set; }
    public int ApprovedLineCount { get; set; }
}

public sealed class PurchaseRequestFormVM
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;

    [Display(Name = "Talep Tarihi")]
    [DataType(DataType.Date)]
    public DateTime RequestDate { get; set; } = DateTime.Today;

    [Display(Name = "Genel İhtiyaç Tarihi")]
    [DataType(DataType.Date)]
    public DateTime? NeededDate { get; set; }

    [Display(Name = "Öncelik")]
    public PurPurchaseRequestPriority Priority { get; set; } = PurPurchaseRequestPriority.Normal;

    [Display(Name = "Talep Notu")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<PurchaseRequestFormLineVM> Lines { get; set; } = [];
    public List<PurchaseMaterialOptionVM> MaterialOptions { get; set; } = [];
}

public sealed class PurchaseRequestFormLineVM
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    public string RequestedQuantityInput { get; set; } = string.Empty;
    public DateTime? NeededDate { get; set; }
    [StringLength(500)] public string? Reason { get; set; }
}

public sealed class PurchaseMaterialOptionVM
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public sealed class PurchaseRequestDetailVM
{
    public int Id { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? NeededDate { get; set; }
    public string RequestedUserId { get; set; } = string.Empty;
    public PurPurchaseRequestPriority Priority { get; set; }
    public PurPurchaseRequestStatus Status { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public string? SubmittedUserId { get; set; }
    public string? Notes { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public List<PurchaseRequestDetailLineVM> Lines { get; set; } = [];
}

public sealed class PurchaseRequestDetailLineVM
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime? NeededDate { get; set; }
    public PurPurchaseRequestLineStatus Status { get; set; }
    public PurPurchaseRequestSource Source { get; set; }
    public string? Reason { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
    public string? ApprovalNote { get; set; }
}

public sealed class PurchaseRequestLineDecisionVM
{
    public int RequestId { get; set; }
    public int LineId { get; set; }
    public PurRequestApprovalAction Decision { get; set; }
    public string ApprovedQuantityInput { get; set; } = string.Empty;
    public string? Note { get; set; }
}
