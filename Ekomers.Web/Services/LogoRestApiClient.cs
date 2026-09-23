using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Ekomers.Web.Services
{
	public sealed record LogoRestConnectionOptions(
		string ServerAddress,
		int Port,
		string Protocol,
		string UserName,
		string Password,
		string FirmNumber,
		string PeriodNumber,
		string? ClientId,
		string? ClientSecret);

	public sealed record LogoRestConnectionTestResult(bool Success, string Message);

	public sealed class LogoRestApiClient
	{
		private readonly HttpClient _httpClient;

		public LogoRestApiClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<LogoRestConnectionTestResult> TestConnectionAsync(
			LogoRestConnectionOptions options,
			CancellationToken cancellationToken = default)
		{
			if (!TryCreateTokenUri(options, out var tokenUri, out var validationMessage))
				return new LogoRestConnectionTestResult(false, validationMessage);

			using var request = new HttpRequestMessage(HttpMethod.Post, tokenUri);
			request.Options.Set(IntegrationLoggingHandler.CategoryKey, "ERP");
			request.Options.Set(IntegrationLoggingHandler.ConnectionKey, "Logo Tiger REST");
			request.Options.Set(IntegrationLoggingHandler.OperationKey, "Oturum Açma / Bağlantı Testi");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			if (!string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret))
			{
				var clientCredential = Convert.ToBase64String(
					Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
				request.Headers.Authorization = new AuthenticationHeaderValue("Basic", clientCredential);
			}

			request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
			{
				["grant_type"] = "password",
				["username"] = options.UserName,
				["password"] = options.Password,
				["firmno"] = options.FirmNumber
			});

			try
			{
				using var response = await _httpClient.SendAsync(request, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);

				if (!response.IsSuccessStatusCode)
				{
					var detail = ReadLogoError(body);
					return new LogoRestConnectionTestResult(
						false,
						$"Logo REST oturumu açılamadı (HTTP {(int)response.StatusCode}).{detail}");
				}

				if (!ContainsAccessToken(body))
				{
					return new LogoRestConnectionTestResult(
						false,
						"Logo REST olumlu yanıt verdi ancak erişim anahtarı dönmedi. Servis sürümü ve kimlik doğrulama ayarlarını kontrol edin.");
				}

				return new LogoRestConnectionTestResult(
					true,
					"Firma oturumu doğrulandı. Dönem doğrulaması ve veri aktarımı henüz etkin değil.");
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return new LogoRestConnectionTestResult(false, "Logo REST bağlantısı zaman aşımına uğradı.");
			}
			catch (HttpRequestException ex)
			{
				return new LogoRestConnectionTestResult(false, $"Logo REST sunucusuna ulaşılamadı: {ex.Message}");
			}
		}

		private static bool TryCreateTokenUri(
			LogoRestConnectionOptions options,
			out Uri tokenUri,
			out string message)
		{
			tokenUri = null!;
			message = string.Empty;
			var protocol = options.Protocol.Trim().ToLowerInvariant();
			if (protocol is not ("http" or "https"))
			{
				message = "Protokol HTTP veya HTTPS olmalıdır.";
				return false;
			}

			var host = options.ServerAddress.Trim();
			if (Uri.TryCreate(host, UriKind.Absolute, out var enteredUri))
				host = enteredUri.Host;

			if (string.IsNullOrWhiteSpace(host) || host.Contains('/') || host.Contains('\\'))
			{
				message = "Sunucu adresine yalnız IP adresi veya sunucu adı girin.";
				return false;
			}

			try
			{
				tokenUri = new UriBuilder(protocol, host, options.Port, "/api/v1/token").Uri;
				return true;
			}
			catch (UriFormatException)
			{
				message = "Sunucu adresi geçerli değil.";
				return false;
			}
		}

		private static bool ContainsAccessToken(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return false;

			try
			{
				using var document = JsonDocument.Parse(body);
				return document.RootElement.TryGetProperty("access_token", out var accessToken)
					&& accessToken.ValueKind == JsonValueKind.String
					&& !string.IsNullOrWhiteSpace(accessToken.GetString());
			}
			catch (JsonException)
			{
				return false;
			}
		}

		private static string ReadLogoError(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return string.Empty;

			try
			{
				using var document = JsonDocument.Parse(body);
				foreach (var propertyName in new[] { "error_description", "message", "error" })
				{
					if (document.RootElement.TryGetProperty(propertyName, out var value)
						&& value.ValueKind == JsonValueKind.String)
					{
						var text = value.GetString();
						if (!string.IsNullOrWhiteSpace(text))
							return $" {text}";
					}
				}
			}
			catch (JsonException)
			{
				// HTML ve benzeri sunucu yanıtlarını kullanıcıya ham biçimde göstermeyin.
			}

			return string.Empty;
		}
	}
}
