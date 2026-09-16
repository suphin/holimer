using Ekomers.Data;
using Ekomers.Models.Ekomers;

namespace Ekomers.Web.Services;

public sealed class SystemErrorLogWriter
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SystemErrorLogWriter> _logger;

    public SystemErrorLogWriter(IServiceScopeFactory scopeFactory, ILogger<SystemErrorLogWriter> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task WriteAsync(
        Exception exception,
        HttpContext httpContext,
        string trackingNumber,
        string? controllerName = null,
        string? actionName = null,
        string? requestPath = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.SystemErrorLogs.Add(new SystemErrorLog
            {
                OccurredAt = DateTime.Now,
                TrackingNumber = Limit(trackingNumber, 100),
                RequestPath = Limit(requestPath ?? httpContext.Request.Path.Value ?? "/", 1000),
                HttpMethod = Limit(httpContext.Request.Method, 10),
                ControllerName = LimitNullable(controllerName ?? httpContext.Request.RouteValues["controller"]?.ToString(), 100),
                ActionName = LimitNullable(actionName ?? httpContext.Request.RouteValues["action"]?.ToString(), 100),
                UserName = LimitNullable(httpContext.User.Identity?.Name, 256),
                RemoteIpAddress = LimitNullable(httpContext.Connection.RemoteIpAddress?.ToString(), 64),
                ExceptionType = Limit(exception.GetType().FullName ?? exception.GetType().Name, 500),
                Message = Limit(exception.Message, 4000),
                ExceptionDetails = exception.ToString()
            });
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception logException)
        {
            _logger.LogWarning(logException, "Hata kaydı veritabanına yazılamadı. TrackingNumber: {TrackingNumber}", trackingNumber);
        }
    }

    private static string Limit(string? value, int maxLength)
    {
        var text = value ?? string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength];
    }

    private static string? LimitNullable(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
