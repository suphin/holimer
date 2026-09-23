namespace Ekomers.Models.ViewModels;

public sealed class IntegrationRequestLogListVM
{
	public string? Category { get; set; }
	public string? Connection { get; set; }
	public string? Result { get; set; }
	public DateTime? StartDate { get; set; }
	public DateTime? EndDate { get; set; }
	public string? Search { get; set; }
	public int Page { get; set; } = 1;
	public int PageSize { get; set; } = 50;
	public int TotalCount { get; set; }
	public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
	public List<string> Categories { get; set; } = new();
	public List<string> Connections { get; set; } = new();
	public List<IntegrationRequestLogRowVM> Rows { get; set; } = new();
}

public sealed class IntegrationRequestLogRowVM
{
	public long Id { get; set; }
	public DateTime RequestedAt { get; set; }
	public string TrackingId { get; set; } = string.Empty;
	public string Category { get; set; } = string.Empty;
	public string ConnectionName { get; set; } = string.Empty;
	public string Operation { get; set; } = string.Empty;
	public string HttpMethod { get; set; } = string.Empty;
	public string RequestUrl { get; set; } = string.Empty;
	public int? HttpStatusCode { get; set; }
	public long DurationMilliseconds { get; set; }
	public bool IsSuccess { get; set; }
	public string? ErrorMessage { get; set; }
}

public sealed class IntegrationRequestLogDetailVM
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
