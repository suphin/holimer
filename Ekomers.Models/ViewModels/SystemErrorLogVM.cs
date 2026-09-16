namespace Ekomers.Models.ViewModels;

public sealed class SystemErrorLogListVM
{
    public string? TrackingNumber { get; set; }
    public string? Search { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public List<SystemErrorLogListRowVM> Rows { get; set; } = new();
}

public sealed class SystemErrorLogListRowVM
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class SystemErrorLogDetailVM
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string RequestPath { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
    public string? ControllerName { get; set; }
    public string? ActionName { get; set; }
    public string? UserName { get; set; }
    public string? RemoteIpAddress { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string ExceptionDetails { get; set; } = string.Empty;
}
