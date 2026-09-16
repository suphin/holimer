using Ekomers.Models.Enums;

namespace Ekomers.Models.Ekomers;

public sealed class PersonalWorkItem : BaseEntity
{
    public string OwnerUserId { get; set; } = string.Empty;
    public PersonalWorkItemType ItemType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public PersonalWorkItemPriority Priority { get; set; } = PersonalWorkItemPriority.Normal;
    public PersonalWorkItemStatus Status { get; set; } = PersonalWorkItemStatus.Open;
    public DateTime? CompletedAt { get; set; }
}
