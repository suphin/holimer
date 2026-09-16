namespace Ekomers.Models.Ekomers;

public class SystemErrorLog
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
