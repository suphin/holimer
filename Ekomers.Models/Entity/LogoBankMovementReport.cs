using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity;

public sealed class LogoBankMovementReportSchedule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string CompanyIds { get; set; } = string.Empty;
    public string RecipientUserIds { get; set; } = string.Empty;
    public TimeSpan SendTime { get; set; }
    public int LookbackDays { get; set; } = 1;
    public bool IncludeToday { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public DateTime? LastRunAt { get; set; }
    public DateTime? LastSuccessAt { get; set; }
    public int? LastRecordCount { get; set; }
    public string? LastError { get; set; }
}

public sealed class LogoBankMovementReportDelivery
{
    public long Id { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime RangeStart { get; set; }
    public DateTime RangeEnd { get; set; }
    public string CompanySummary { get; set; } = string.Empty;
    public string Recipients { get; set; } = string.Empty;
    public string TriggerType { get; set; } = string.Empty;
    public int RecordCount { get; set; }
    public decimal IncomingTotalTry { get; set; }
    public decimal OutgoingTotalTry { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TriggeredBy { get; set; }
}
