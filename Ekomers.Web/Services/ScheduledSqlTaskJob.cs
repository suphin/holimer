using Ekomers.Common.Services;
using Ekomers.Data;
using Ekomers.Models.Entity;
using Hangfire;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

public sealed class ScheduledSqlTaskJob
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ScheduledSqlTaskJob> _logger;
    private readonly string _portalConnectionString;
    private readonly string _logoConnectionString;

    public ScheduledSqlTaskJob(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<ScheduledSqlTaskJob> logger)
    {
        _context = context;
        _logger = logger;
        _portalConnectionString = DecryptConnection(configuration, "DefaultConnection");
        _logoConnectionString = DecryptConnection(configuration, "LogoConnection");
    }

    [DisableConcurrentExecution(timeoutInSeconds: 300)]
    public async Task ProcessDueTasksAsync()
    {
        var now = DateTime.Now;
        var ids = await _context.ScheduledSqlTasks.AsNoTracking()
            .Where(x => x.IsDelete != true
                && x.IsEnabled
                && x.NextRunAt.HasValue
                && x.NextRunAt <= now
                && (!x.IsRunning || x.RunningSince < now.AddHours(-2)))
            .OrderBy(x => x.NextRunAt)
            .Select(x => x.ID)
            .Take(20)
            .ToListAsync();

        foreach (var id in ids)
            await ExecuteTaskAsync(id);
    }

    private async Task ExecuteTaskAsync(int taskId)
    {
        var startedAt = DateTime.Now;
        var staleBefore = startedAt.AddHours(-2);
        var claimed = await _context.ScheduledSqlTasks
            .Where(x => x.ID == taskId
                && x.IsDelete != true
                && x.IsEnabled
                && x.NextRunAt.HasValue
                && x.NextRunAt <= startedAt
                && (!x.IsRunning || x.RunningSince < staleBefore))
            .ExecuteUpdateAsync(update => update
                .SetProperty(x => x.IsRunning, true)
                .SetProperty(x => x.RunningSince, startedAt)
                .SetProperty(x => x.LastStartedAt, startedAt));
        if (claimed == 0)
            return;

        _context.ChangeTracker.Clear();
        var task = await _context.ScheduledSqlTasks.AsNoTracking().SingleAsync(x => x.ID == taskId);
        var run = new ScheduledSqlTaskRun
        {
            TaskId = task.ID,
            ScheduledFor = task.NextRunAt,
            StartedAt = startedAt,
            TriggerType = "Zamanlanmış",
            TriggeredBy = "Hangfire",
            IsActive = true,
            IsDelete = false,
            CreateDate = startedAt
        };
        _context.ScheduledSqlTaskRuns.Add(run);
        await _context.SaveChangesAsync();

        int? affectedRows = null;
        string? error = null;
        try
        {
            var connectionString = task.ConnectionTarget == ScheduledSqlConnectionTarget.Logo
                ? _logoConnectionString
                : _portalConnectionString;
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = task.SqlScript;
            command.CommandTimeout = task.CommandTimeoutSeconds;
            affectedRows = await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            error = Limit(ex.GetBaseException().Message, 4000);
            _logger.LogError(ex, "Zamanlanmış SQL görevi çalıştırılamadı. TaskId={TaskId}", task.ID);
        }

        var completedAt = DateTime.Now;
        run.CompletedAt = completedAt;
        run.IsSuccess = error is null;
        run.AffectedRows = affectedRows;
        run.ErrorMessage = error;

        var trackedTask = await _context.ScheduledSqlTasks.SingleAsync(x => x.ID == task.ID);
        trackedTask.IsRunning = false;
        trackedTask.RunningSince = null;
        trackedTask.LastCompletedAt = completedAt;
        trackedTask.LastSucceeded = error is null;
        trackedTask.LastAffectedRows = affectedRows;
        trackedTask.LastError = error;
        trackedTask.UpdateDate = completedAt;

        if (trackedTask.IsRecurring)
            trackedTask.NextRunAt = CalculateNextRun(trackedTask, completedAt.AddSeconds(1));
        else
        {
            trackedTask.IsEnabled = false;
            trackedTask.NextRunAt = null;
        }

        await _context.SaveChangesAsync();
    }

    public static DateTime? CalculateNextRun(ScheduledSqlTask task, DateTime after)
    {
        if (!task.IsRecurring)
        {
            var planned = task.StartDate.Date.Add(task.FirstRunTime);
            return planned >= after ? planned : null;
        }

        if (task.DaysOfWeekMask == 0)
            return null;

        var slots = BuildDailySlots(task.FirstRunTime, task.LastRunTime, task.RunsPerDay);
        var firstDate = task.StartDate.Date > after.Date ? task.StartDate.Date : after.Date;
        for (var dayOffset = 0; dayOffset < 21; dayOffset++)
        {
            var date = firstDate.AddDays(dayOffset);
            if ((task.DaysOfWeekMask & (1 << (int)date.DayOfWeek)) == 0)
                continue;
            foreach (var slot in slots)
            {
                var candidate = date.Add(slot);
                if (candidate >= after)
                    return candidate;
            }
        }

        return null;
    }

    public static int BuildDaysMask(IEnumerable<int> days) =>
        days.Where(x => x is >= 0 and <= 6).Distinct().Aggregate(0, (mask, day) => mask | (1 << day));

    public static IReadOnlyList<TimeSpan> BuildDailySlots(TimeSpan first, TimeSpan last, int runsPerDay)
    {
        runsPerDay = Math.Clamp(runsPerDay, 1, 24);
        if (runsPerDay == 1)
            return [first];
        if (last <= first)
            return [];

        var intervalTicks = (last - first).Ticks / (runsPerDay - 1);
        return Enumerable.Range(0, runsPerDay)
            .Select(index => first.Add(TimeSpan.FromTicks(intervalTicks * index)))
            .ToArray();
    }

    private static string DecryptConnection(IConfiguration configuration, string name)
    {
        var encrypted = configuration.GetConnectionString(name);
        if (string.IsNullOrWhiteSpace(encrypted))
            throw new InvalidOperationException($"{name} bağlantı bilgisi bulunamadı.");
        return CryptoHelper.Decrypt(encrypted);
    }

    private static string Limit(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
