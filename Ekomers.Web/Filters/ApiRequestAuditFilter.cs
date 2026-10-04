using System.Diagnostics;
using System.Text.Json;
using Ekomers.Data;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Ekomers.Web.Filters;

public sealed class ApiRequestAuditFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiRequestAuditFilter> _logger;

    public ApiRequestAuditFilter(ApplicationDbContext context, ILogger<ApiRequestAuditFilter> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var startedAt = DateTime.Now;
        var stopwatch = Stopwatch.StartNew();
        var executed = await next();
        stopwatch.Stop();

        try
        {
            var statusCode = executed.Result switch
            {
                ObjectResult objectResult when objectResult.StatusCode.HasValue => objectResult.StatusCode.Value,
                StatusCodeResult statusResult => statusResult.StatusCode,
                _ => executed.HttpContext.Response.StatusCode is >= 100 and <= 599
                    ? executed.HttpContext.Response.StatusCode
                    : 200
            };
            if (executed.Exception != null && !executed.ExceptionHandled)
                statusCode = 500;

            var responseValue = (executed.Result as ObjectResult)?.Value;
            var responseBody = responseValue == null ? null : Truncate(JsonSerializer.Serialize(responseValue), 100_000);
            var connectionName = executed.HttpContext.Request.Path.StartsWithSegments("/api/v1/logo-clients")
                ? "Logo Cari API"
                : "Reçete Maliyet API";
            var requestHeaders = JsonSerializer.Serialize(new
            {
                Accept = executed.HttpContext.Request.Headers.Accept.ToString(),
                UserAgent = executed.HttpContext.Request.Headers.UserAgent.ToString(),
                TokenPrefix = executed.HttpContext.User.FindFirst("token_prefix")?.Value,
                RemoteIp = executed.HttpContext.Connection.RemoteIpAddress?.ToString()
            });

            _context.IntegrationRequestLogs.Add(new IntegrationRequestLog
            {
                TrackingId = Guid.NewGuid().ToString(),
                RequestedAt = startedAt,
                CompletedAt = DateTime.Now,
                Category = "Dış API",
                ConnectionName = connectionName,
                Operation = context.ActionDescriptor.DisplayName ?? context.ActionDescriptor.RouteValues["action"] ?? "API",
                HttpMethod = executed.HttpContext.Request.Method,
                RequestUrl = Truncate($"{executed.HttpContext.Request.Path}{executed.HttpContext.Request.QueryString}", 2048)!,
                RequestContentType = executed.HttpContext.Request.ContentType,
                RequestHeaders = requestHeaders,
                HttpStatusCode = statusCode,
                ResponseContentType = "application/json",
                ResponseBody = responseBody,
                DurationMilliseconds = stopwatch.ElapsedMilliseconds,
                IsSuccess = statusCode is >= 200 and < 400 && executed.Exception == null,
                ErrorMessage = Truncate(executed.Exception?.Message, 4000),
                UserName = executed.HttpContext.User.Identity?.Name
            });
            await _context.SaveChangesAsync(executed.HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dış API erişim kaydı yazılamadı.");
        }
    }

    private static string? Truncate(string? value, int length) =>
        string.IsNullOrEmpty(value) || value.Length <= length ? value : value[..length];
}
