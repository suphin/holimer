namespace Ekomers.Models.Entity;

public sealed class IntegrationRequestLog
{
	public long Id { get; set; }
	public string TrackingId { get; set; } = string.Empty;
	public DateTime RequestedAt { get; set; }
	public DateTime? CompletedAt { get; set; }
	public string Category { get; set; } = string.Empty;
	public string ConnectionName { get; set; } = string.Empty;
	public string Operation { get; set; } = string.Empty;
	public string HttpMethod { get; set; } = string.Empty;
	public string RequestUrl { get; set; } = string.Empty;
	public string? RequestContentType { get; set; }
	public string? RequestHeaders { get; set; }
	public string? RequestBody { get; set; }
	public int? HttpStatusCode { get; set; }
	public string? ResponseContentType { get; set; }
	public string? ResponseHeaders { get; set; }
	public string? ResponseBody { get; set; }
	public long DurationMilliseconds { get; set; }
	public bool IsSuccess { get; set; }
	public string? ErrorMessage { get; set; }
	public string? UserName { get; set; }
}
