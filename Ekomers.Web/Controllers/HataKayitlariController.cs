using Ekomers.Data;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class HataKayitlariController : Controller
{
    private readonly ApplicationDbContext _context;

    public HataKayitlariController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? trackingNumber,
        string? search,
        DateTime? startDate,
        DateTime? endDate,
        int page = 1,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "SistemYonetimi";
        const int pageSize = 50;
        page = Math.Max(page, 1);

        var query = _context.SystemErrorLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            trackingNumber = trackingNumber.Trim();
            query = query.Where(x => x.TrackingNumber.Contains(trackingNumber));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(x =>
                x.RequestPath.Contains(search) ||
                x.ExceptionType.Contains(search) ||
                x.Message.Contains(search) ||
                (x.ControllerName != null && x.ControllerName.Contains(search)) ||
                (x.ActionName != null && x.ActionName.Contains(search)) ||
                (x.UserName != null && x.UserName.Contains(search)));
        }

        if (startDate.HasValue)
            query = query.Where(x => x.OccurredAt >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(x => x.OccurredAt < endDate.Value.Date.AddDays(1));

        var totalCount = await query.CountAsync(ct);
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        page = Math.Min(page, totalPages);

        var rows = await query
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new SystemErrorLogListRowVM
            {
                Id = x.Id,
                OccurredAt = x.OccurredAt,
                TrackingNumber = x.TrackingNumber,
                RequestPath = x.RequestPath,
                HttpMethod = x.HttpMethod,
                UserName = x.UserName,
                ExceptionType = x.ExceptionType,
                Message = x.Message
            })
            .ToListAsync(ct);

        return View(new SystemErrorLogListVM
        {
            TrackingNumber = trackingNumber,
            Search = search,
            StartDate = startDate,
            EndDate = endDate,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Rows = rows
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewBag.Modul = "SistemYonetimi";
        var model = await _context.SystemErrorLogs
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SystemErrorLogDetailVM
            {
                Id = x.Id,
                OccurredAt = x.OccurredAt,
                TrackingNumber = x.TrackingNumber,
                RequestPath = x.RequestPath,
                HttpMethod = x.HttpMethod,
                ControllerName = x.ControllerName,
                ActionName = x.ActionName,
                UserName = x.UserName,
                RemoteIpAddress = x.RemoteIpAddress,
                ExceptionType = x.ExceptionType,
                Message = x.Message,
                ExceptionDetails = x.ExceptionDetails
            })
            .FirstOrDefaultAsync(ct);

        return model == null ? NotFound() : View(model);
    }
}
