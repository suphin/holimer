using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels;

public sealed class PersonalPlannerVM
{
    public string? Search { get; set; }
    public PersonalWorkItemStatus? Status { get; set; }
    public List<PersonalWorkItemRowVM> Notes { get; set; } = [];
    public List<PersonalWorkItemRowVM> Tasks { get; set; } = [];
    public List<PersonalCalendarEventVM> CalendarEvents { get; set; } = [];
    public int OpenTaskCount { get; set; }
    public int TodayTaskCount { get; set; }
    public int OverdueTaskCount { get; set; }
    public int NoteCount { get; set; }
}

public sealed class PersonalWorkItemFormVM
{
    public int? Id { get; set; }
    public PersonalWorkItemType ItemType { get; set; }

    [Required(ErrorMessage = "Başlık zorunludur."), StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [StringLength(4000)]
    public string? Content { get; set; }

    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public PersonalWorkItemPriority Priority { get; set; } = PersonalWorkItemPriority.Normal;
}

public sealed class PersonalWorkItemRowVM
{
    public int Id { get; set; }
    public PersonalWorkItemType ItemType { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public DateTime? StartAt { get; set; }
    public DateTime? EndAt { get; set; }
    public bool IsAllDay { get; set; }
    public PersonalWorkItemPriority Priority { get; set; }
    public PersonalWorkItemStatus Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsOverdue => ItemType == PersonalWorkItemType.Task && Status == PersonalWorkItemStatus.Open && StartAt < DateTime.Now;
}

public sealed class PersonalCalendarEventVM
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Start { get; set; } = string.Empty;
    public string? End { get; set; }
    public bool AllDay { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int Status { get; set; }
}
