using System.Text.Json;
using Ekomers.Data;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin")]
public sealed class EntegrasyonLoglariController : Controller
{
	private readonly ApplicationDbContext _context;

	public EntegrasyonLoglariController(ApplicationDbContext context)
	{
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> Index(
		string? category,
		string? connection,
		string? result,
		DateTime? startDate,
		DateTime? endDate,
		string? search,
		int page = 1,
		CancellationToken ct = default)
	{
		ViewBag.Modul = "SistemYonetimi";
		const int pageSize = 50;
		page = Math.Max(1, page);
		startDate ??= DateTime.Now.AddDays(-30);

		var allLogs = _context.IntegrationRequestLogs.AsNoTracking();
		var categories = await allLogs.Select(x => x.Category).Distinct().OrderBy(x => x).ToListAsync(ct);
		var connections = await allLogs.Select(x => x.ConnectionName).Distinct().OrderBy(x => x).ToListAsync(ct);
		var query = allLogs;

		if (!string.IsNullOrWhiteSpace(category))
			query = query.Where(x => x.Category == category);
		if (!string.IsNullOrWhiteSpace(connection))
			query = query.Where(x => x.ConnectionName == connection);
		if (string.Equals(result, "success", StringComparison.OrdinalIgnoreCase))
			query = query.Where(x => x.IsSuccess);
		else if (string.Equals(result, "error", StringComparison.OrdinalIgnoreCase))
			query = query.Where(x => !x.IsSuccess);
		if (startDate.HasValue)
			query = query.Where(x => x.RequestedAt >= startDate.Value);
		if (endDate.HasValue)
			query = query.Where(x => x.RequestedAt <= endDate.Value);

		if (!string.IsNullOrWhiteSpace(search))
		{
			search = search.Trim();
			query = query.Where(x =>
				x.TrackingId.Contains(search) ||
				x.Operation.Contains(search) ||
				x.RequestUrl.Contains(search) ||
				(x.RequestBody != null && x.RequestBody.Contains(search)) ||
				(x.ResponseBody != null && x.ResponseBody.Contains(search)) ||
				(x.ErrorMessage != null && x.ErrorMessage.Contains(search)));
		}

		var totalCount = await query.CountAsync(ct);
		var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
		page = Math.Min(page, totalPages);

		var rows = await query
			.OrderByDescending(x => x.RequestedAt)
			.ThenByDescending(x => x.Id)
			.Skip((page - 1) * pageSize)
			.Take(pageSize)
			.Select(x => new IntegrationRequestLogRowVM
			{
				Id = x.Id,
				RequestedAt = x.RequestedAt,
				TrackingId = x.TrackingId,
				Category = x.Category,
				ConnectionName = x.ConnectionName,
				Operation = x.Operation,
				HttpMethod = x.HttpMethod,
				RequestUrl = x.RequestUrl,
				HttpStatusCode = x.HttpStatusCode,
				DurationMilliseconds = x.DurationMilliseconds,
				IsSuccess = x.IsSuccess,
				ErrorMessage = x.ErrorMessage
			})
			.ToListAsync(ct);

		return View(new IntegrationRequestLogListVM
		{
			Category = category,
			Connection = connection,
			Result = result,
			StartDate = startDate,
			EndDate = endDate,
			Search = search,
			Page = page,
			PageSize = pageSize,
			TotalCount = totalCount,
			Categories = categories,
			Connections = connections,
			Rows = rows
		});
	}

	[HttpGet]
	public async Task<IActionResult> Details(long id, CancellationToken ct)
	{
		var log = await _context.IntegrationRequestLogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
		if (log == null)
			return NotFound();

		return PartialView("_DetailsModalContent", new IntegrationRequestLogDetailVM
		{
			Id = log.Id,
			TrackingId = log.TrackingId,
			RequestedAt = log.RequestedAt,
			CompletedAt = log.CompletedAt,
			Category = log.Category,
			ConnectionName = log.ConnectionName,
			Operation = log.Operation,
			HttpMethod = log.HttpMethod,
			RequestUrl = log.RequestUrl,
			RequestContentType = log.RequestContentType,
			RequestHeaders = log.RequestHeaders,
			RequestBody = PrettyPrint(log.RequestBody, log.RequestContentType),
			HttpStatusCode = log.HttpStatusCode,
			ResponseContentType = log.ResponseContentType,
			ResponseHeaders = log.ResponseHeaders,
			ResponseBody = PrettyPrint(log.ResponseBody, log.ResponseContentType),
			DurationMilliseconds = log.DurationMilliseconds,
			IsSuccess = log.IsSuccess,
			ErrorMessage = log.ErrorMessage,
			UserName = log.UserName
		});
	}

	private static string? PrettyPrint(string? body, string? contentType)
	{
		if (string.IsNullOrWhiteSpace(body)
			|| contentType?.Contains("json", StringComparison.OrdinalIgnoreCase) != true)
			return body;

		try
		{
			using var document = JsonDocument.Parse(body);
			return JsonSerializer.Serialize(document.RootElement, new JsonSerializerOptions { WriteIndented = true });
		}
		catch (JsonException)
		{
			return body;
		}
	}
}
