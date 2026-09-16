using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ekomers.Web.Controllers;

[Authorize]
public sealed class KisiselPlanlayiciController : Controller
{
    private readonly ApplicationDbContext _context;

    public KisiselPlanlayiciController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, PersonalWorkItemStatus? status, CancellationToken ct)
    {
        ViewBag.Modul = "KisiselPlanlayici";
        var ownerId = CurrentUserId;
        var query = _context.PersonalWorkItems.AsNoTracking()
            .Where(x => x.OwnerUserId == ownerId && x.IsDelete != true);

        var allItems = await query.OrderByDescending(x => x.UpdateDate ?? x.CreateDate).ToListAsync(ct);
        var visibleItems = allItems.AsEnumerable();
        if (status.HasValue) visibleItems = visibleItems.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            visibleItems = visibleItems.Where(x => x.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (x.Content?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var rows = visibleItems.Select(ToRow).ToList();
        var today = DateTime.Today;
        var openTasks = allItems.Where(x => x.ItemType == PersonalWorkItemType.Task && x.Status == PersonalWorkItemStatus.Open).ToList();
        var model = new PersonalPlannerVM
        {
            Search = search,
            Status = status,
            Notes = rows.Where(x => x.ItemType == PersonalWorkItemType.Note).ToList(),
            Tasks = rows.Where(x => x.ItemType == PersonalWorkItemType.Task)
                .OrderBy(x => x.Status == PersonalWorkItemStatus.Completed)
                .ThenBy(x => x.StartAt ?? DateTime.MaxValue).ToList(),
            OpenTaskCount = openTasks.Count,
            TodayTaskCount = openTasks.Count(x => x.StartAt.HasValue && x.StartAt.Value.Date == today),
            OverdueTaskCount = openTasks.Count(x => x.StartAt.HasValue && x.StartAt.Value < DateTime.Now),
            NoteCount = allItems.Count(x => x.ItemType == PersonalWorkItemType.Note && x.Status != PersonalWorkItemStatus.Cancelled),
            CalendarEvents = allItems.Where(x => x.ItemType == PersonalWorkItemType.Task && x.StartAt.HasValue && x.Status != PersonalWorkItemStatus.Cancelled)
                .Select(ToCalendarEvent).ToList()
        };
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(PersonalWorkItemFormVM model, CancellationToken ct)
    {
        if (model.ItemType == PersonalWorkItemType.Task && !model.StartAt.HasValue)
            ModelState.AddModelError(nameof(model.StartAt), "Görev için başlangıç tarihi ve saati zorunludur.");
        if (model.StartAt.HasValue && model.EndAt.HasValue && model.EndAt.Value < model.StartAt.Value)
            ModelState.AddModelError(nameof(model.EndAt), "Bitiş zamanı başlangıç zamanından önce olamaz.");
        if (!ModelState.IsValid)
        {
            TempData["error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.Now;
        var ownerId = CurrentUserId;
        PersonalWorkItem entity;
        if (model.Id.HasValue)
        {
            var existing = await _context.PersonalWorkItems.FirstOrDefaultAsync(x => x.ID == model.Id.Value && x.OwnerUserId == ownerId && x.IsDelete != true, ct);
            if (existing == null) return NotFound();
            entity = existing;
            entity.UpdateDate = now;
            entity.UpdateUserID = User.Identity?.Name;
        }
        else
        {
            entity = new PersonalWorkItem
            {
                OwnerUserId = ownerId,
                Status = PersonalWorkItemStatus.Open,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = User.Identity?.Name
            };
            _context.PersonalWorkItems.Add(entity);
        }

        entity.ItemType = model.ItemType;
        entity.Title = model.Title.Trim();
        entity.Content = Clean(model.Content);
        entity.StartAt = model.ItemType == PersonalWorkItemType.Task ? model.StartAt : null;
        entity.EndAt = model.ItemType == PersonalWorkItemType.Task ? model.EndAt : null;
        entity.IsAllDay = model.ItemType == PersonalWorkItemType.Task && model.IsAllDay;
        entity.Priority = model.Priority;
        if (entity.ItemType == PersonalWorkItemType.Note && entity.Status == PersonalWorkItemStatus.Completed)
        {
            entity.Status = PersonalWorkItemStatus.Open;
            entity.CompletedAt = null;
        }

        await _context.SaveChangesAsync(ct);
        TempData["success"] = model.Id.HasValue ? "Kayıt güncellendi." : (model.ItemType == PersonalWorkItemType.Note ? "Not kaydedildi." : "Görev takvime eklendi.");
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Tamamla(int id, CancellationToken ct)
    {
        var item = await OwnItem(id, ct);
        if (item == null) return NotFound();
        if (item.ItemType != PersonalWorkItemType.Task) return BadRequest();
        item.Status = item.Status == PersonalWorkItemStatus.Completed ? PersonalWorkItemStatus.Open : PersonalWorkItemStatus.Completed;
        item.CompletedAt = item.Status == PersonalWorkItemStatus.Completed ? DateTime.Now : null;
        item.UpdateDate = DateTime.Now;
        item.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = item.Status == PersonalWorkItemStatus.Completed ? "Görev tamamlandı." : "Görev yeniden açıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Sil(int id, CancellationToken ct)
    {
        var item = await OwnItem(id, ct);
        if (item == null) return NotFound();
        item.IsDelete = true;
        item.IsActive = false;
        item.DeleteDate = DateTime.Now;
        item.DeleteUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = item.ItemType == PersonalWorkItemType.Note ? "Not silindi." : "Görev silindi.";
        return RedirectToAction(nameof(Index));
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("Oturum kullanıcı kimliği bulunamadı.");

    private Task<PersonalWorkItem?> OwnItem(int id, CancellationToken ct) => _context.PersonalWorkItems
        .FirstOrDefaultAsync(x => x.ID == id && x.OwnerUserId == CurrentUserId && x.IsDelete != true, ct);

    private static PersonalWorkItemRowVM ToRow(PersonalWorkItem x) => new()
    {
        Id = x.ID, ItemType = x.ItemType, Title = x.Title, Content = x.Content, StartAt = x.StartAt,
        EndAt = x.EndAt, IsAllDay = x.IsAllDay, Priority = x.Priority, Status = x.Status,
        CompletedAt = x.CompletedAt, UpdatedAt = x.UpdateDate ?? x.CreateDate
    };

    private static PersonalCalendarEventVM ToCalendarEvent(PersonalWorkItem x)
    {
        var color = x.Status == PersonalWorkItemStatus.Completed ? "fc-event-light fc-event-solid-success" : x.Priority switch
        {
            PersonalWorkItemPriority.Urgent => "fc-event-danger",
            PersonalWorkItemPriority.High => "fc-event-warning",
            PersonalWorkItemPriority.Low => "fc-event-info",
            _ => "fc-event-primary"
        };
        return new PersonalCalendarEventVM
        {
            Id = x.ID,
            Title = x.Title,
            Start = x.StartAt!.Value.ToString(x.IsAllDay ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss"),
            End = x.EndAt?.ToString(x.IsAllDay ? "yyyy-MM-dd" : "yyyy-MM-ddTHH:mm:ss"),
            AllDay = x.IsAllDay,
            ClassName = color,
            Description = x.Content ?? string.Empty,
            Priority = (int)x.Priority,
            Status = (int)x.Status
        };
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
