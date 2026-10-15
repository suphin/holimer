using Ekomers.Data;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class ZamanlanmisSqlGorevleriController : Controller
{
    private readonly ApplicationDbContext _context;

    public ZamanlanmisSqlGorevleriController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetModule();
        var tasks = await _context.ScheduledSqlTasks.AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderByDescending(x => x.IsEnabled)
            .ThenBy(x => x.NextRunAt)
            .ThenBy(x => x.Name)
            .Select(x => new ScheduledSqlTaskListVM
            {
                Id = x.ID,
                Name = x.Name,
                Description = x.Description,
                ConnectionTarget = x.ConnectionTarget,
                IsRecurring = x.IsRecurring,
                DaysOfWeekMask = x.DaysOfWeekMask,
                FirstRunTime = x.FirstRunTime,
                LastRunTime = x.LastRunTime,
                RunsPerDay = x.RunsPerDay,
                IsEnabled = x.IsEnabled,
                IsRunning = x.IsRunning,
                NextRunAt = x.NextRunAt,
                LastCompletedAt = x.LastCompletedAt,
                LastSucceeded = x.LastSucceeded,
                LastAffectedRows = x.LastAffectedRows,
                LastError = x.LastError
            })
            .ToListAsync(cancellationToken);

        var recentRuns = await (
            from run in _context.ScheduledSqlTaskRuns.AsNoTracking()
            join task in _context.ScheduledSqlTasks.AsNoTracking() on run.TaskId equals task.ID
            orderby run.StartedAt descending
            select new ScheduledSqlTaskRunVM
            {
                Id = run.ID,
                TaskId = run.TaskId,
                TaskName = task.Name,
                StartedAt = run.StartedAt,
                CompletedAt = run.CompletedAt,
                IsSuccess = run.IsSuccess,
                AffectedRows = run.AffectedRows,
                ErrorMessage = run.ErrorMessage,
                TriggerType = run.TriggerType
            })
            .Take(50)
            .ToListAsync(cancellationToken);

        return View(new ScheduledSqlTaskIndexVM { Tasks = tasks, RecentRuns = recentRuns });
    }

    [HttpGet]
    public IActionResult Yeni()
    {
        SetModule();
        return View(new ScheduledSqlTaskCreateVM
        {
            StartDate = DateTime.Today,
            FirstRunTime = new TimeSpan(9, 0, 0),
            LastRunTime = new TimeSpan(18, 0, 0),
            RunsPerDay = 1,
            CommandTimeoutSeconds = 600,
            IsEnabled = true
        });
    }

    [HttpGet]
    public async Task<IActionResult> Duzenle(int id, CancellationToken cancellationToken)
    {
        SetModule();
        var task = await _context.ScheduledSqlTasks.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (task is null)
            return NotFound();
        if (task.IsRunning)
        {
            TempData["error"] = "Çalışmakta olan görev düzenlenemez.";
            return RedirectToAction(nameof(Index));
        }

        return View("Yeni", ToEditModel(task));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(ScheduledSqlTaskCreateVM model, CancellationToken cancellationToken)
    {
        SetModule();
        model.Name = (model.Name ?? string.Empty).Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.SqlScript = (model.SqlScript ?? string.Empty).Trim();
        model.SelectedDays ??= [];

        if (!Enum.IsDefined(model.ConnectionTarget))
            ModelState.AddModelError(nameof(model.ConnectionTarget), "Geçerli bir bağlantı seçin.");
        if (model.IsRecurring && model.SelectedDays.Count == 0)
            ModelState.AddModelError(nameof(model.SelectedDays), "Tekrarlı görev için en az bir gün seçin.");
        if (model.IsRecurring && model.RunsPerDay > 1 && model.LastRunTime <= model.FirstRunTime)
            ModelState.AddModelError(nameof(model.LastRunTime), "Birden fazla çalışma için son saat ilk saatten sonra olmalıdır.");

        if (!model.IsRecurring)
        {
            model.SelectedDays.Clear();
            model.RunsPerDay = 1;
            model.LastRunTime = model.FirstRunTime;
        }

        var now = DateTime.Now;
        var task = new ScheduledSqlTask
        {
            Name = model.Name,
            Description = model.Description,
            SqlScript = model.SqlScript,
            ConnectionTarget = model.ConnectionTarget,
            StartDate = model.StartDate.Date,
            FirstRunTime = model.FirstRunTime,
            LastRunTime = model.LastRunTime,
            IsRecurring = model.IsRecurring,
            DaysOfWeekMask = ScheduledSqlTaskJob.BuildDaysMask(model.SelectedDays),
            RunsPerDay = model.RunsPerDay,
            CommandTimeoutSeconds = model.CommandTimeoutSeconds,
            IsEnabled = model.IsEnabled,
            IsRunning = false,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = User.Identity?.Name
        };

        task.NextRunAt = ScheduledSqlTaskJob.CalculateNextRun(task, now);
        if (task.IsEnabled && !task.NextRunAt.HasValue)
            ModelState.AddModelError(nameof(model.StartDate), "Seçilen ayarlara göre gelecekte bir çalışma zamanı bulunamadı.");

        if (!ModelState.IsValid)
            return View("Yeni", model);

        _context.ScheduledSqlTasks.Add(task);
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Zamanlanmış SQL görevi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Duzenle(ScheduledSqlTaskCreateVM model, CancellationToken cancellationToken)
    {
        SetModule();
        if (!model.Id.HasValue)
            return NotFound();

        var task = await _context.ScheduledSqlTasks
            .SingleOrDefaultAsync(x => x.ID == model.Id.Value && x.IsDelete != true, cancellationToken);
        if (task is null)
            return NotFound();
        if (task.IsRunning)
        {
            TempData["error"] = "Çalışmakta olan görev düzenlenemez.";
            return RedirectToAction(nameof(Index));
        }

        PrepareAndValidate(model);
        ApplyModel(task, model);
        task.UpdateDate = DateTime.Now;
        task.UpdateUserID = User.Identity?.Name;
        task.NextRunAt = task.IsEnabled
            ? ScheduledSqlTaskJob.CalculateNextRun(task, DateTime.Now)
            : null;
        if (task.IsEnabled && !task.NextRunAt.HasValue)
            ModelState.AddModelError(nameof(model.StartDate), "Seçilen ayarlara göre gelecekte bir çalışma zamanı bulunamadı.");

        if (!ModelState.IsValid)
            return View("Yeni", model);

        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Zamanlanmış SQL görevi güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DurumDegistir(int id, CancellationToken cancellationToken)
    {
        var task = await _context.ScheduledSqlTasks
            .SingleOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (task is null)
            return NotFound();
        if (task.IsRunning)
        {
            TempData["error"] = "Çalışmakta olan görev durdurulamaz.";
            return RedirectToAction(nameof(Index));
        }

        task.IsEnabled = !task.IsEnabled;
        task.NextRunAt = task.IsEnabled
            ? ScheduledSqlTaskJob.CalculateNextRun(task, DateTime.Now)
            : null;
        if (task.IsEnabled && !task.NextRunAt.HasValue)
        {
            task.IsEnabled = false;
            TempData["error"] = "Görev etkinleştirilemedi; gelecekte bir çalışma zamanı bulunamadı.";
        }
        else
        {
            TempData["success"] = task.IsEnabled ? "Görev etkinleştirildi." : "Görev pasife alındı.";
        }

        task.UpdateDate = DateTime.Now;
        task.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, CancellationToken cancellationToken)
    {
        var task = await _context.ScheduledSqlTasks
            .SingleOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (task is null)
            return NotFound();
        if (task.IsRunning)
        {
            TempData["error"] = "Çalışmakta olan görev silinemez.";
            return RedirectToAction(nameof(Index));
        }

        task.IsDelete = true;
        task.IsActive = false;
        task.IsEnabled = false;
        task.NextRunAt = null;
        task.DeleteDate = DateTime.Now;
        task.DeleteUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Görev silindi. Çalışma geçmişi korunmaya devam edecek.";
        return RedirectToAction(nameof(Index));
    }

    private void SetModule() => ViewBag.Modul = "SistemYonetimi";

    private void PrepareAndValidate(ScheduledSqlTaskCreateVM model)
    {
        model.Name = (model.Name ?? string.Empty).Trim();
        model.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();
        model.SqlScript = (model.SqlScript ?? string.Empty).Trim();
        model.SelectedDays ??= [];

        if (!Enum.IsDefined(model.ConnectionTarget))
            ModelState.AddModelError(nameof(model.ConnectionTarget), "Geçerli bir bağlantı seçin.");
        if (model.IsRecurring && model.SelectedDays.Count == 0)
            ModelState.AddModelError(nameof(model.SelectedDays), "Tekrarlı görev için en az bir gün seçin.");
        if (model.IsRecurring && model.RunsPerDay > 1 && model.LastRunTime <= model.FirstRunTime)
            ModelState.AddModelError(nameof(model.LastRunTime), "Birden fazla çalışma için son saat ilk saatten sonra olmalıdır.");

        if (!model.IsRecurring)
        {
            model.SelectedDays.Clear();
            model.RunsPerDay = 1;
            model.LastRunTime = model.FirstRunTime;
        }
    }

    private void ApplyModel(ScheduledSqlTask task, ScheduledSqlTaskCreateVM model)
    {
        task.Name = model.Name;
        task.Description = model.Description;
        task.SqlScript = model.SqlScript;
        task.ConnectionTarget = model.ConnectionTarget;
        task.StartDate = model.StartDate.Date;
        task.FirstRunTime = model.FirstRunTime;
        task.LastRunTime = model.LastRunTime;
        task.IsRecurring = model.IsRecurring;
        task.DaysOfWeekMask = ScheduledSqlTaskJob.BuildDaysMask(model.SelectedDays);
        task.RunsPerDay = model.RunsPerDay;
        task.CommandTimeoutSeconds = model.CommandTimeoutSeconds;
        task.IsEnabled = model.IsEnabled;
    }

    private static ScheduledSqlTaskCreateVM ToEditModel(ScheduledSqlTask task) => new()
    {
        Id = task.ID,
        Name = task.Name,
        Description = task.Description,
        SqlScript = task.SqlScript,
        ConnectionTarget = task.ConnectionTarget,
        StartDate = task.StartDate,
        FirstRunTime = task.FirstRunTime,
        LastRunTime = task.LastRunTime,
        IsRecurring = task.IsRecurring,
        SelectedDays = Enumerable.Range(0, 7)
            .Where(day => (task.DaysOfWeekMask & (1 << day)) != 0)
            .ToList(),
        RunsPerDay = task.RunsPerDay,
        CommandTimeoutSeconds = task.CommandTimeoutSeconds,
        IsEnabled = task.IsEnabled
    };
}
