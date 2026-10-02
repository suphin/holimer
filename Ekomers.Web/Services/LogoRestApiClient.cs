using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

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
	public sealed record LogoRestTransferResult(
		bool Success,
		string Message,
		string? LogoReference = null,
		string? LogoSlipNumber = null);

	public sealed class LogoProductionEntrySlipPayload
	{
		[JsonPropertyName("GROUP")] public int Group { get; init; } = 3;
		[JsonPropertyName("TYPE")] public int Type { get; init; } = 13;
		[JsonPropertyName("NUMBER")] public string Number { get; init; } = "~";
		[JsonPropertyName("DATE")] public DateTime Date { get; init; }
		[JsonPropertyName("SOURCE_WH")] public int WarehouseNumber { get; init; }
		[JsonPropertyName("SOURCE_COST_GRP")] public int WarehouseCostGroup { get; init; }
		[JsonPropertyName("DIVISION")] public int DivisionNumber { get; init; }
		[JsonPropertyName("DEPARTMENT")] public int DepartmentNumber { get; init; }
		[JsonPropertyName("FACTORY")] public int FactoryNumber { get; init; }
		[JsonPropertyName("FOOTNOTE1")] public string? Notes { get; init; }
		[JsonPropertyName("TRANSACTIONS")] public LogoRestItemCollection<LogoProductionEntrySlipLinePayload> Transactions { get; init; } = new();
	}

	public sealed class LogoProductionEntrySlipLinePayload
	{
		[JsonPropertyName("ITEM_CODE")] public string MaterialCode { get; init; } = string.Empty;
		[JsonPropertyName("ITEM_REFERENCE")] public int MaterialReference { get; init; }
		[JsonPropertyName("LINE_TYPE")] public int LineType { get; init; }
		[JsonPropertyName("LINE_NUMBER")] public int LineNumber { get; init; }
		[JsonPropertyName("QUANTITY")] public decimal Quantity { get; init; }
		[JsonPropertyName("UNIT_CODE")] public string UnitCode { get; init; } = string.Empty;
		[JsonPropertyName("UNIT_CONV1")] public decimal UnitConversion1 { get; init; } = 1;
		[JsonPropertyName("UNIT_CONV2")] public decimal UnitConversion2 { get; init; } = 1;
		[JsonPropertyName("SOURCEINDEX")] public int WarehouseNumber { get; init; }
		[JsonPropertyName("SOURCECOSTGRP")] public int WarehouseCostGroup { get; init; }
		[JsonPropertyName("IOCODE")] public int InputOutputCode { get; init; } = 1;
		[JsonPropertyName("DESCRIPTION")] public string? Description { get; init; }
	}

	public sealed class LogoRestItemCollection<T>
	{
		[JsonPropertyName("items")] public List<T> Items { get; init; } = [];
	}

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

		public async Task<LogoRestTransferResult> SendProductionEntrySlipAsync(
			LogoRestConnectionOptions options,
			LogoProductionEntrySlipPayload payload,
			CancellationToken cancellationToken = default)
		{
			return await SendMaterialSlipAsync(
				options,
				payload,
				"Üretimden giriş fişi",
				"Üretim giriş fişi",
				cancellationToken);
		}

		public async Task<LogoRestTransferResult> SendConsumptionSlipAsync(
			LogoRestConnectionOptions options,
			LogoProductionEntrySlipPayload payload,
			CancellationToken cancellationToken = default)
		{
			return await SendMaterialSlipAsync(
				options,
				payload,
				"Sarf fişi",
				"Sarf fişi",
				cancellationToken);
		}

		private async Task<LogoRestTransferResult> SendMaterialSlipAsync(
			LogoRestConnectionOptions options,
			LogoProductionEntrySlipPayload payload,
			string operationName,
			string displayName,
			CancellationToken cancellationToken)
		{
			if (!TryCreateApiUri(options, "/api/v1/itemSlips", out var serviceUri, out var validationMessage))
				return new LogoRestTransferResult(false, validationMessage);

			var authentication = await RequestAccessTokenAsync(options, operationName, cancellationToken);
			if (!authentication.Success)
				return new LogoRestTransferResult(false, authentication.Message);

			using var request = new HttpRequestMessage(HttpMethod.Post, serviceUri);
			request.Options.Set(IntegrationLoggingHandler.CategoryKey, "ERP");
			request.Options.Set(IntegrationLoggingHandler.ConnectionKey, "Logo Tiger REST");
			request.Options.Set(IntegrationLoggingHandler.OperationKey, $"{operationName} / {payload.Number}");
			request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

			var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions
			{
				DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
			});
			request.Content = new StringContent(json, Encoding.UTF8, "application/json");

			try
			{
				using var response = await _httpClient.SendAsync(request, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				if (!response.IsSuccessStatusCode)
				{
					var detail = ReadLogoError(body);
					return new LogoRestTransferResult(false,
						$"Logo {displayName.ToLowerInvariant()} kaydını kabul etmedi (HTTP {(int)response.StatusCode}).{detail}");
				}

				if (!TryValidateProductionEntrySlipResponse(
					body,
					payload.Transactions.Items.Count,
					out var reference,
					out var slipNumber,
					out var responseError))
				{
					return new LogoRestTransferResult(false,
						"Logo fiş başlığını oluşturdu ancak ürün satırlarını geçerli malzeme kartlarına bağlamadı. " + responseError,
						reference,
						slipNumber);
				}
				var successMessage = string.IsNullOrWhiteSpace(slipNumber)
					? $"{displayName} Logo'ya başarıyla aktarıldı."
					: $"{displayName} Logo'ya {slipNumber} numarasıyla başarıyla aktarıldı.";
				return new LogoRestTransferResult(true, successMessage, reference, slipNumber);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return new LogoRestTransferResult(false, $"Logo {displayName.ToLowerInvariant()} aktarımı zaman aşımına uğradı.");
			}
			catch (HttpRequestException ex)
			{
				return new LogoRestTransferResult(false, $"Logo REST sunucusuna ulaşılamadı: {ex.Message}");
			}
		}

		private async Task<(bool Success, string? AccessToken, string Message)> RequestAccessTokenAsync(
			LogoRestConnectionOptions options,
			string operationName,
			CancellationToken cancellationToken)
		{
			if (!TryCreateTokenUri(options, out var tokenUri, out var validationMessage))
				return (false, null, validationMessage);

			using var request = new HttpRequestMessage(HttpMethod.Post, tokenUri);
			request.Options.Set(IntegrationLoggingHandler.CategoryKey, "ERP");
			request.Options.Set(IntegrationLoggingHandler.ConnectionKey, "Logo Tiger REST");
			request.Options.Set(IntegrationLoggingHandler.OperationKey, $"Oturum Açma / {operationName}");
			request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			if (!string.IsNullOrWhiteSpace(options.ClientId) && !string.IsNullOrWhiteSpace(options.ClientSecret))
			{
				var credential = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.ClientId}:{options.ClientSecret}"));
				request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credential);
			}

			var form = new Dictionary<string, string>
			{
				["grant_type"] = "password",
				["username"] = options.UserName,
				["password"] = options.Password,
				["firmno"] = options.FirmNumber
			};
			if (!string.IsNullOrWhiteSpace(options.PeriodNumber))
				form["periodno"] = options.PeriodNumber;
			request.Content = new FormUrlEncodedContent(form);

			try
			{
				using var response = await _httpClient.SendAsync(request, cancellationToken);
				var body = await response.Content.ReadAsStringAsync(cancellationToken);
				if (!response.IsSuccessStatusCode)
					return (false, null, $"Logo REST oturumu açılamadı (HTTP {(int)response.StatusCode}).{ReadLogoError(body)}");

				using var document = JsonDocument.Parse(body);
				if (!document.RootElement.TryGetProperty("access_token", out var tokenElement)
					|| tokenElement.ValueKind != JsonValueKind.String
					|| string.IsNullOrWhiteSpace(tokenElement.GetString()))
					return (false, null, "Logo REST olumlu yanıt verdi ancak erişim anahtarı dönmedi.");

				return (true, tokenElement.GetString(), string.Empty);
			}
			catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
			{
				return (false, null, "Logo REST oturumu zaman aşımına uğradı.");
			}
			catch (HttpRequestException ex)
			{
				return (false, null, $"Logo REST sunucusuna ulaşılamadı: {ex.Message}");
			}
			catch (JsonException)
			{
				return (false, null, "Logo REST oturum yanıtı geçerli JSON değil.");
			}
		}

		private static bool TryCreateTokenUri(
			LogoRestConnectionOptions options,
			out Uri tokenUri,
			out string message)
		{
			tokenUri = null!;
			message = string.Empty;
			return TryCreateApiUri(options, "/api/v1/token", out tokenUri, out message);
		}

		private static bool TryCreateApiUri(
			LogoRestConnectionOptions options,
			string path,
			out Uri uri,
			out string message)
		{
			uri = null!;
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
				uri = new UriBuilder(protocol, host, options.Port, path).Uri;
				return true;
			}
			catch (UriFormatException)
			{
				message = "Sunucu adresi geçerli değil.";
				return false;
			}
		}

		private static string? ReadLogoReference(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return null;

			try
			{
				using var document = JsonDocument.Parse(body);
				foreach (var name in new[] { "INTERNAL_REFERENCE", "internalReference", "LOGICALREF", "logicalRef", "DATA_REFERENCE", "dataReference" })
				{
					if (!document.RootElement.TryGetProperty(name, out var value))
						continue;
					if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
						return value.GetString();
					if (value.ValueKind == JsonValueKind.Number)
						return value.GetRawText();
				}
			}
			catch (JsonException)
			{
				// Başarılı ama JSON olmayan yanıt ayrıca entegrasyon logunda görülebilir.
			}

			return null;
		}

		private static string? ReadLogoSlipNumber(string body)
		{
			if (string.IsNullOrWhiteSpace(body))
				return null;

			try
			{
				using var document = JsonDocument.Parse(body);
				foreach (var name in new[] { "NUMBER", "number", "FICHE_NO", "ficheNo" })
				{
					if (!document.RootElement.TryGetProperty(name, out var value))
						continue;
					if (value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()))
						return value.GetString()!.Trim();
					if (value.ValueKind == JsonValueKind.Number)
						return value.GetRawText();
				}
			}
			catch (JsonException)
			{
				// Ham yanıt entegrasyon logunda saklanır.
			}

			return null;
		}

		private static bool TryValidateProductionEntrySlipResponse(
			string body,
			int expectedLineCount,
			out string? reference,
			out string? slipNumber,
			out string message)
		{
			reference = ReadLogoReference(body);
			slipNumber = ReadLogoSlipNumber(body);
			message = string.Empty;
			if (string.IsNullOrWhiteSpace(body))
			{
				message = "Logo boş yanıt döndürdü.";
				return false;
			}

			try
			{
				using var document = JsonDocument.Parse(body);
				var root = document.RootElement;
				if (root.ValueKind != JsonValueKind.Object
					|| !HasPositiveNumericProperty(root, "INTERNAL_REFERENCE", "LOGICALREF", "DATA_REFERENCE"))
				{
					message = "Logo geçerli bir fiş referansı döndürmedi.";
					return false;
				}

				if (!root.TryGetProperty("TRANSACTIONS", out var transactions)
					|| transactions.ValueKind != JsonValueKind.Object
					|| !transactions.TryGetProperty("items", out var items)
					|| items.ValueKind != JsonValueKind.Array)
				{
					message = "Logo yanıtında fiş satırları bulunamadı.";
					return false;
				}

				if (items.GetArrayLength() != expectedLineCount)
				{
					message = $"Gönderilen {expectedLineCount} satıra karşılık Logo {items.GetArrayLength()} satır döndürdü.";
					return false;
				}

				var lineNumber = 0;
				foreach (var item in items.EnumerateArray())
				{
					lineNumber++;
					var hasItemReference = HasPositiveNumericProperty(item,
						"ITEM_REFERENCE", "MASTER_REFERENCE", "INTERNAL_REFERENCE", "DATA_REFERENCE", "LOGICALREF");
					var hasItemCode = HasNonEmptyStringProperty(item, "ITEM_CODE", "MASTER_CODE");
					if (!hasItemReference || !hasItemCode)
					{
						message = $"{lineNumber}. satırın ITEM_CODE/malzeme referansı bilgisi boş döndü.";
						return false;
					}
				}

				return true;
			}
			catch (JsonException)
			{
				message = "Logo yanıtı geçerli JSON değil.";
				return false;
			}
		}

		private static bool HasPositiveNumericProperty(JsonElement element, params string[] propertyNames)
		{
			foreach (var propertyName in propertyNames)
			{
				if (!element.TryGetProperty(propertyName, out var value))
					continue;

				if (value.ValueKind == JsonValueKind.Number
					&& value.TryGetInt64(out var number)
					&& number > 0)
					return true;

				if (value.ValueKind == JsonValueKind.String
					&& long.TryParse(value.GetString(), out number)
					&& number > 0)
					return true;
			}

			return false;
		}

		private static bool HasNonEmptyStringProperty(JsonElement element, params string[] propertyNames)
		{
			foreach (var propertyName in propertyNames)
			{
				if (element.TryGetProperty(propertyName, out var value)
					&& value.ValueKind == JsonValueKind.String
					&& !string.IsNullOrWhiteSpace(value.GetString()))
					return true;
			}

			return false;
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
				var messages = new List<string>();
				foreach (var propertyName in new[] { "error_description", "message", "Message", "error", "Error" })
				{
					if (document.RootElement.TryGetProperty(propertyName, out var value)
						&& value.ValueKind == JsonValueKind.String)
					{
						var text = value.GetString();
						if (!string.IsNullOrWhiteSpace(text))
							messages.Add(text.Trim());
					}
				}

				foreach (var propertyName in new[] { "ModelState", "modelState" })
				{
					if (!document.RootElement.TryGetProperty(propertyName, out var modelState)
						|| modelState.ValueKind != JsonValueKind.Object)
						continue;

					foreach (var validationEntry in modelState.EnumerateObject())
					{
						if (validationEntry.Value.ValueKind != JsonValueKind.Array)
							continue;
						foreach (var validationMessage in validationEntry.Value.EnumerateArray())
						{
							if (validationMessage.ValueKind == JsonValueKind.String
								&& !string.IsNullOrWhiteSpace(validationMessage.GetString()))
								messages.Add(validationMessage.GetString()!.Trim());
						}
					}
				}

				if (messages.Count > 0)
					return " " + string.Join(" ", messages.Distinct(StringComparer.OrdinalIgnoreCase));
			}
			catch (JsonException)
			{
				// HTML ve benzeri sunucu yanıtlarını kullanıcıya ham biçimde göstermeyin.
			}

			return string.Empty;
		}
	}
}
