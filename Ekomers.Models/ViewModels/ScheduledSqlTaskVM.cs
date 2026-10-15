using System.ComponentModel.DataAnnotations;
using Ekomers.Models.Entity;

namespace Ekomers.Models.ViewModels;

public sealed class ScheduledSqlTaskCreateVM
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "Görev adı zorunludur."), StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "SQL sorgusu zorunludur."), StringLength(200000)]
    public string SqlScript { get; set; } = string.Empty;

    [Required]
    public ScheduledSqlConnectionTarget ConnectionTarget { get; set; } = ScheduledSqlConnectionTarget.Portal;

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    public TimeSpan FirstRunTime { get; set; } = new(9, 0, 0);
    public TimeSpan LastRunTime { get; set; } = new(18, 0, 0);
    public bool IsRecurring { get; set; }
    public List<int> SelectedDays { get; set; } = [];

    [Range(1, 24, ErrorMessage = "Günlük çalışma sayısı 1 ile 24 arasında olmalıdır.")]
    public int RunsPerDay { get; set; } = 1;

    [Range(30, 3600, ErrorMessage = "Zaman aşımı 30 ile 3600 saniye arasında olmalıdır.")]
    public int CommandTimeoutSeconds { get; set; } = 600;

    public bool IsEnabled { get; set; } = true;
}

public sealed class ScheduledSqlTaskListVM
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ScheduledSqlConnectionTarget ConnectionTarget { get; set; }
    public bool IsRecurring { get; set; }
    public int DaysOfWeekMask { get; set; }
    public TimeSpan FirstRunTime { get; set; }
    public TimeSpan LastRunTime { get; set; }
    public int RunsPerDay { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsRunning { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime? LastCompletedAt { get; set; }
    public bool? LastSucceeded { get; set; }
    public int? LastAffectedRows { get; set; }
    public string? LastError { get; set; }
}

public sealed class ScheduledSqlTaskRunVM
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsSuccess { get; set; }
    public int? AffectedRows { get; set; }
    public string? ErrorMessage { get; set; }
    public string TriggerType { get; set; } = string.Empty;
}

public sealed class ScheduledSqlTaskIndexVM
{
    public List<ScheduledSqlTaskListVM> Tasks { get; set; } = [];
    public List<ScheduledSqlTaskRunVM> RecentRuns { get; set; } = [];
}
