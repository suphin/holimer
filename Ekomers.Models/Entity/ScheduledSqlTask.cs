using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity;

public enum ScheduledSqlConnectionTarget
{
    Portal = 1,
    Logo = 2
}

public sealed class ScheduledSqlTask : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SqlScript { get; set; } = string.Empty;
    public ScheduledSqlConnectionTarget ConnectionTarget { get; set; }
    public DateTime StartDate { get; set; }
    public TimeSpan FirstRunTime { get; set; }
    public TimeSpan LastRunTime { get; set; }
    public bool IsRecurring { get; set; }
    public int DaysOfWeekMask { get; set; }
    public int RunsPerDay { get; set; }
    public int CommandTimeoutSeconds { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastStartedAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public bool? LastSucceeded { get; set; }
    public int? LastAffectedRows { get; set; }
    public string? LastError { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? RunningSince { get; set; }
}

public sealed class ScheduledSqlTaskRun : BaseEntity
{
    public int TaskId { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsSuccess { get; set; }
    public int? AffectedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggerType { get; set; } = string.Empty;
    public string? TriggeredBy { get; set; }
}
