using Ekomers.Models.Enums;

namespace Ekomers.Models.Ekomers;

public sealed class DevelopmentRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public string OwnerUserId { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public string? OwnerEmail { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevelopmentRequestPriority Priority { get; set; } = DevelopmentRequestPriority.Normal;
    public DevelopmentRequestStatus Status { get; set; } = DevelopmentRequestStatus.New;
    public string? AdminNote { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string? CompletedByUserId { get; set; }
    public string? CompletedByName { get; set; }
    public bool NotificationSent { get; set; }
    public DateTime? NotificationSentDate { get; set; }
    public string? NotificationError { get; set; }
}
