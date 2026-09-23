using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using Ekomers.Data;
using Ekomers.Models.Entity;

namespace Ekomers.Web.Services;

public sealed class IntegrationLoggingHandler : DelegatingHandler
{
	public static readonly HttpRequestOptionsKey<string> CategoryKey = new("Ekomers.Integration.Category");
	public static readonly HttpRequestOptionsKey<string> ConnectionKey = new("Ekomers.Integration.Connection");
	public static readonly HttpRequestOptionsKey<string> OperationKey = new("Ekomers.Integration.Operation");

	private const int MaxLoggedCharacters = 1_000_000;
	private static readonly Regex QuerySecretRegex = new(
		@"(?i)(?<key>password|pass|client_secret|clientsecret|access_token|refresh_token|token|sessionid|api_key|secret)=(?<value>[^&]*)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex XmlSecretRegex = new(
		@"(?is)(?<open><(?:[a-z0-9_]+:)?(?:password|passWord|clientSecret|access_token|refresh_token|token|sessionID|api_key|secret)\b[^>]*>).*?(?<close></(?:[a-z0-9_]+:)?(?:password|passWord|clientSecret|access_token|refresh_token|token|sessionID|api_key|secret)>)",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);
	private static readonly Regex StructuredSecretRegex = new(
		"""(?i)(?<prefix>(?:"|')?(?:password|passWord|client_secret|clientSecret|access_token|refresh_token|token|sessionId|api_key|secret)(?:"|')?\s*[:=]\s*(?:"|')?)(?<value>[^&,\r\n"'<>}]+)""",
		RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly ILogger<IntegrationLoggingHandler> _logger;

	public IntegrationLoggingHandler(
		IServiceScopeFactory scopeFactory,
		IHttpContextAccessor httpContextAccessor,
		ILogger<IntegrationLoggingHandler> logger)
	{
		_scopeFactory = scopeFactory;
		_httpContextAccessor = httpContextAccessor;
		_logger = logger;
	}

	protected override async Task<HttpResponseMessage> SendAsync(
		HttpRequestMessage request,
		CancellationToken cancellationToken)
	{
		var startedAt = DateTime.Now;
		var stopwatch = Stopwatch.StartNew();
		var trackingId = Guid.NewGuid().ToString("D");
		var requestUrl = SanitizeUrl(request.RequestUri?.ToString() ?? string.Empty);
		var requestContentType = request.Content?.Headers.ContentType?.ToString();
		var requestBody = await ReadContentAsync(request.Content);
		var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
		var category = GetOption(request, CategoryKey) ?? ResolveCategory(request.RequestUri);
		var connection = GetOption(request, ConnectionKey) ?? ResolveConnection(request.RequestUri);
		var operation = GetOption(request, OperationKey) ?? ResolveOperation(request);

		HttpResponseMessage? response = null;
		string? responseBody = null;
		string? errorMessage = null;
		bool serviceError = false;

		try
		{
			response = await base.SendAsync(request, cancellationToken);
			responseBody = await ReadContentAsync(response.Content);
			serviceError = ContainsServiceError(responseBody, response.Content?.Headers.ContentType);
			if (!response.IsSuccessStatusCode)
				errorMessage = $"HTTP {(int)response.StatusCode} ({response.ReasonPhrase})";
			else if (serviceError)
				errorMessage = ExtractServiceError(responseBody) ?? "HTTP yanıtı başarılı olsa da servis içeriğinde hata bildirildi.";

			return response;
		}
		catch (Exception ex)
		{
			errorMessage = ex.Message;
			throw;
		}
		finally
		{
			stopwatch.Stop();
			var log = new IntegrationRequestLog
			{
				TrackingId = trackingId,
				RequestedAt = startedAt,
				CompletedAt = DateTime.Now,
				Category = category,
				ConnectionName = connection,
				Operation = operation,
				HttpMethod = request.Method.Method,
				RequestUrl = Truncate(requestUrl, 2048),
				RequestContentType = Truncate(requestContentType, 200),
				RequestHeaders = BuildHeaders(request.Headers, request.Content?.Headers),
				RequestBody = requestBody,
				HttpStatusCode = response == null ? null : (int)response.StatusCode,
				ResponseContentType = Truncate(response?.Content?.Headers.ContentType?.ToString(), 200),
				ResponseHeaders = response == null ? null : BuildHeaders(response.Headers, response.Content?.Headers),
				ResponseBody = responseBody,
				DurationMilliseconds = stopwatch.ElapsedMilliseconds,
				IsSuccess = response?.IsSuccessStatusCode == true && !serviceError && string.IsNullOrWhiteSpace(errorMessage),
				ErrorMessage = Truncate(errorMessage, 4000),
				UserName = Truncate(userName, 256)
			};

			await TryWriteLogAsync(log);
		}
	}

	private async Task TryWriteLogAsync(IntegrationRequestLog log)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
			context.IntegrationRequestLogs.Add(log);
			await context.SaveChangesAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Entegrasyon isteği {TrackingId} veritabanına kaydedilemedi.", log.TrackingId);
		}
	}

	private static async Task<string?> ReadContentAsync(HttpContent? content)
	{
		if (content == null)
			return null;

		var contentType = content.Headers.ContentType;
		if (!IsTextContent(contentType))
		{
			var length = content.Headers.ContentLength;
			return $"[İkili içerik gösterilmez: {contentType?.MediaType ?? "bilinmeyen tür"}, {FormatLength(length)}]";
		}

		try
		{
			// Log okuma işlemi, başarılı servis çağrısını iptal etmemeli veya bozmamalıdır.
			var text = await content.ReadAsStringAsync(CancellationToken.None);
			return Truncate(MaskSecrets(text), MaxLoggedCharacters);
		}
		catch (Exception ex)
		{
			return $"[İçerik okunamadı: {ex.Message}]";
		}
	}

	private static bool IsTextContent(MediaTypeHeaderValue? contentType)
	{
		var mediaType = contentType?.MediaType;
		if (string.IsNullOrWhiteSpace(mediaType))
			return true;

		return mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
			|| mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)
			|| mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)
			|| mediaType.Contains("form", StringComparison.OrdinalIgnoreCase)
			|| mediaType.Contains("graphql", StringComparison.OrdinalIgnoreCase);
	}

	private static string BuildHeaders(HttpHeaders headers, HttpContentHeaders? contentHeaders)
	{
		var lines = new List<string>();
		AppendHeaders(lines, headers);
		if (contentHeaders != null)
			AppendHeaders(lines, contentHeaders);
		return Truncate(string.Join(Environment.NewLine, lines.OrderBy(x => x)), MaxLoggedCharacters) ?? string.Empty;
	}

	private static void AppendHeaders(List<string> lines, HttpHeaders headers)
	{
		foreach (var header in headers)
		{
			var value = IsSensitiveHeader(header.Key) ? "***" : string.Join(", ", header.Value);
			lines.Add($"{header.Key}: {value}");
		}
	}

	private static bool IsSensitiveHeader(string name) =>
		name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
		|| name.Equals("Proxy-Authorization", StringComparison.OrdinalIgnoreCase)
		|| name.Equals("Cookie", StringComparison.OrdinalIgnoreCase)
		|| name.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("token", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("secret", StringComparison.OrdinalIgnoreCase)
		|| name.Contains("key", StringComparison.OrdinalIgnoreCase);

	private static string MaskSecrets(string value)
	{
		if (string.IsNullOrEmpty(value))
			return value;

		var masked = XmlSecretRegex.Replace(value, "${open}***${close}");
		masked = StructuredSecretRegex.Replace(masked, "${prefix}***");
		return QuerySecretRegex.Replace(masked, "${key}=***");
	}

	private static string SanitizeUrl(string value) => QuerySecretRegex.Replace(value, "${key}=***");

	private static bool ContainsServiceError(string? body, MediaTypeHeaderValue? contentType)
	{
		if (string.IsNullOrWhiteSpace(body))
			return false;

		if (body.Contains("<Fault", StringComparison.OrdinalIgnoreCase)
			|| body.Contains(":Fault", StringComparison.OrdinalIgnoreCase))
			return true;

		var resultCode = Regex.Match(body, @"(?is)<(?:[a-z0-9_]+:)?resultCode\b[^>]*>\s*(?<code>-?\d+)\s*</");
		if (resultCode.Success && resultCode.Groups["code"].Value != "1")
			return true;

		if (contentType?.MediaType?.Contains("json", StringComparison.OrdinalIgnoreCase) == true)
		{
			try
			{
				using var document = JsonDocument.Parse(body);
				if (document.RootElement.ValueKind == JsonValueKind.Object)
				{
					if (document.RootElement.TryGetProperty("success", out var success)
						&& success.ValueKind == JsonValueKind.False)
						return true;
					if (document.RootElement.TryGetProperty("error", out var error)
						&& error.ValueKind is not JsonValueKind.Null and not JsonValueKind.False)
						return true;
					if (document.RootElement.TryGetProperty("errors", out var errors)
						&& errors.ValueKind == JsonValueKind.Array
						&& errors.GetArrayLength() > 0)
						return true;
				}
			}
			catch (JsonException)
			{
				// Ham içerik yine de loglanır; geçersiz JSON servis hatası sayılmaz.
			}
		}

		return false;
	}

	private static string? ExtractServiceError(string? body)
	{
		if (string.IsNullOrWhiteSpace(body))
			return null;

		foreach (var elementName in new[] { "faultstring", "resultMsg" })
		{
			var match = Regex.Match(body, $@"(?is)<(?:[a-z0-9_]+:)?{elementName}\b[^>]*>\s*(?<value>.*?)\s*</");
			if (match.Success)
				return Truncate(Regex.Replace(match.Groups["value"].Value, "<.*?>", string.Empty), 4000);
		}

		try
		{
			using var document = JsonDocument.Parse(body);
			foreach (var name in new[] { "error_description", "message", "error" })
			{
				if (document.RootElement.ValueKind == JsonValueKind.Object
					&& document.RootElement.TryGetProperty(name, out var value))
					return Truncate(value.ToString(), 4000);
			}
		}
		catch (JsonException)
		{
			// JSON olmayan içerikler yukarıdaki XML kontrolünden geçmiştir.
		}

		return null;
	}

	private static string ResolveCategory(Uri? uri)
	{
		var host = uri?.Host ?? string.Empty;
		if (host.Contains("elogo", StringComparison.OrdinalIgnoreCase)) return "E-Fatura";
		if (uri?.AbsolutePath.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase) == true) return "ERP";
		return "Harici Servis";
	}

	private static string ResolveConnection(Uri? uri)
	{
		var host = uri?.Host ?? "Bilinmeyen bağlantı";
		if (host.Contains("elogo", StringComparison.OrdinalIgnoreCase)) return "e-Logo Postbox";
		if (uri?.AbsolutePath.StartsWith("/api/v1", StringComparison.OrdinalIgnoreCase) == true) return "Logo Tiger REST";
		return host;
	}

	private static string ResolveOperation(HttpRequestMessage request)
	{
		if (request.Headers.TryGetValues("SOAPAction", out var values))
		{
			var action = values.FirstOrDefault()?.Trim('"');
			if (!string.IsNullOrWhiteSpace(action))
				return action.Split('/').Last();
		}
		return request.RequestUri?.AbsolutePath ?? "Servis çağrısı";
	}

	private static string? GetOption(HttpRequestMessage request, HttpRequestOptionsKey<string> key) =>
		request.Options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) ? value : null;

	private static string FormatLength(long? length) => length.HasValue ? $"{length.Value:N0} bayt" : "boyut bilinmiyor";

	private static string? Truncate(string? value, int maxLength)
	{
		if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
			return value;
		return value[..maxLength] + $"{Environment.NewLine}[İçerik {maxLength:N0} karakterde kesildi.]";
	}
}
