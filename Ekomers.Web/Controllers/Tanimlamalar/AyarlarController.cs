
using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Tanimlamalar,Admin")]
	public class AyarlarController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IMemoryCache _cache;
		private readonly IDataProtector _logoRestProtector;
		private readonly LogoRestApiClient _logoRestApiClient;

		public AyarlarController(
			ApplicationDbContext context,
			IMemoryCache cache,
			IDataProtectionProvider dataProtectionProvider,
			LogoRestApiClient logoRestApiClient)
		{
			_context = context;
			_cache = cache;
			_logoRestProtector = dataProtectionProvider.CreateProtector("Ekomers.LogoRestSettings.v1");
			_logoRestApiClient = logoRestApiClient;
		}

		// GET: AktiviteTur
		//[Authorize(Policy = "AktiviteTur")]
		public async Task<IActionResult> Index(CancellationToken cancellationToken)
		{
			ViewBag.Modul = "Tanimlamalar";
			var setting = await _context.LogoRestApiSettings
				.AsNoTracking()
				.OrderBy(x => x.ID)
				.FirstOrDefaultAsync(cancellationToken);

			return View(ToViewModel(setting));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult CacheTemizle()
		{
			// Tüm cache'i temizler
			(_cache as MemoryCache)?.Compact(1.0);

			return Json(new { success = true, message = "Cache başarıyla temizlendi." });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> LogoRestKaydet(
			LogoRestApiSettingsVM model,
			CancellationToken cancellationToken)
		{
			ViewBag.Modul = "Tanimlamalar";
			var setting = await _context.LogoRestApiSettings
				.OrderBy(x => x.ID)
				.FirstOrDefaultAsync(cancellationToken);

			ValidateLogoRestModel(model, setting);
			if (!ModelState.IsValid)
			{
				ApplyStoredState(model, setting);
				return View("Index", model);
			}

			if (setting == null)
			{
				setting = new LogoRestApiSetting();
				_context.LogoRestApiSettings.Add(setting);
			}

			var previousClientId = setting.ClientId;
			setting.ServerAddress = NormalizeServerAddress(model.ServerAddress);
			setting.Port = model.Port;
			setting.Protocol = model.Protocol.Trim().ToUpperInvariant();
			setting.RestUserName = model.RestUserName.Trim();
			setting.FirmNumber = model.FirmNumber.Trim();
			setting.PeriodNumber = model.PeriodNumber.Trim();

			if (!string.IsNullOrWhiteSpace(model.RestPassword))
				setting.RestPasswordProtected = _logoRestProtector.Protect(model.RestPassword);

			if (string.IsNullOrWhiteSpace(model.ClientId))
			{
				setting.ClientId = null;
				setting.ClientSecretProtected = null;
			}
			else
			{
				setting.ClientId = model.ClientId.Trim();
				if (!string.IsNullOrWhiteSpace(model.ClientSecret))
					setting.ClientSecretProtected = _logoRestProtector.Protect(model.ClientSecret);
				else if (!string.Equals(previousClientId, setting.ClientId, StringComparison.Ordinal))
					setting.ClientSecretProtected = null;
			}

			setting.UpdatedAt = DateTime.Now;
			setting.UpdatedBy = User.Identity?.Name;
			setting.LastTestDate = null;
			setting.LastTestSucceeded = null;
			setting.LastTestMessage = null;
			await _context.SaveChangesAsync(cancellationToken);

			TempData["Success"] = "Logo REST API ayarları kaydedildi.";
			return RedirectToAction(nameof(Index), null, null, "logo-rest-api");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> LogoRestBaglantiTesti(
			LogoRestApiSettingsVM model,
			CancellationToken cancellationToken)
		{
			ViewBag.Modul = "Tanimlamalar";
			var setting = await _context.LogoRestApiSettings
				.OrderBy(x => x.ID)
				.FirstOrDefaultAsync(cancellationToken);

			ValidateLogoRestModel(model, setting);
			if (!ModelState.IsValid)
			{
				ApplyStoredState(model, setting);
				return View("Index", model);
			}

			if (!TryResolveSecrets(model, setting, out var password, out var clientSecret))
			{
				ApplyStoredState(model, setting);
				return View("Index", model);
			}

			var options = new LogoRestConnectionOptions(
				NormalizeServerAddress(model.ServerAddress),
				model.Port,
				model.Protocol,
				model.RestUserName.Trim(),
				password,
				model.FirmNumber.Trim(),
				model.PeriodNumber.Trim(),
				string.IsNullOrWhiteSpace(model.ClientId) ? null : model.ClientId.Trim(),
				clientSecret);

			var result = await _logoRestApiClient.TestConnectionAsync(options, cancellationToken);
			model.LastTestDate = DateTime.Now;
			model.LastTestSucceeded = result.Success;
			model.LastTestMessage = result.Message;

			if (setting != null)
			{
				setting.LastTestDate = model.LastTestDate;
				setting.LastTestSucceeded = result.Success;
				setting.LastTestMessage = result.Message;
				await _context.SaveChangesAsync(cancellationToken);
			}

			ApplyStoredState(model, setting, preserveTestResult: true);
			return View("Index", model);
		}

		private void ValidateLogoRestModel(LogoRestApiSettingsVM model, LogoRestApiSetting? setting)
		{
			if (!string.Equals(model.Protocol, "HTTP", StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(model.Protocol, "HTTPS", StringComparison.OrdinalIgnoreCase))
				ModelState.AddModelError(nameof(model.Protocol), "Protokol HTTP veya HTTPS olmalıdır.");

			if (string.IsNullOrWhiteSpace(model.RestPassword)
				&& string.IsNullOrWhiteSpace(setting?.RestPasswordProtected))
				ModelState.AddModelError(nameof(model.RestPassword), "İlk kayıtta REST şifresi zorunludur.");

			if (!string.IsNullOrWhiteSpace(model.ClientId)
				&& string.IsNullOrWhiteSpace(model.ClientSecret)
				&& (string.IsNullOrWhiteSpace(setting?.ClientSecretProtected)
					|| !string.Equals(model.ClientId.Trim(), setting.ClientId, StringComparison.Ordinal)))
			{
				ModelState.AddModelError(nameof(model.ClientSecret), "Client ID girildiğinde Client Secret da zorunludur.");
			}

			if (string.IsNullOrWhiteSpace(model.ClientId) && !string.IsNullOrWhiteSpace(model.ClientSecret))
				ModelState.AddModelError(nameof(model.ClientId), "Client Secret girildiğinde Client ID de zorunludur.");
		}

		private bool TryResolveSecrets(
			LogoRestApiSettingsVM model,
			LogoRestApiSetting? setting,
			out string password,
			out string? clientSecret)
		{
			password = model.RestPassword ?? string.Empty;
			clientSecret = string.IsNullOrWhiteSpace(model.ClientId) ? null : model.ClientSecret;

			try
			{
				if (string.IsNullOrWhiteSpace(password) && !string.IsNullOrWhiteSpace(setting?.RestPasswordProtected))
					password = _logoRestProtector.Unprotect(setting.RestPasswordProtected);

				if (!string.IsNullOrWhiteSpace(model.ClientId)
					&& string.IsNullOrWhiteSpace(clientSecret)
					&& string.Equals(model.ClientId.Trim(), setting?.ClientId, StringComparison.Ordinal)
					&& !string.IsNullOrWhiteSpace(setting?.ClientSecretProtected))
				{
					clientSecret = _logoRestProtector.Unprotect(setting.ClientSecretProtected);
				}
			}
			catch (Exception)
			{
				ModelState.AddModelError(string.Empty, "Kayıtlı Logo REST gizli bilgileri çözülemedi. Parola ve Client Secret değerlerini yeniden girip kaydedin.");
				return false;
			}

			return true;
		}

		private static LogoRestApiSettingsVM ToViewModel(LogoRestApiSetting? setting)
		{
			if (setting == null)
				return new LogoRestApiSettingsVM();

			return new LogoRestApiSettingsVM
			{
				ServerAddress = setting.ServerAddress,
				Port = setting.Port,
				Protocol = setting.Protocol,
				RestUserName = setting.RestUserName,
				FirmNumber = setting.FirmNumber,
				PeriodNumber = setting.PeriodNumber,
				ClientId = setting.ClientId,
				HasSavedPassword = !string.IsNullOrWhiteSpace(setting.RestPasswordProtected),
				HasSavedClientSecret = !string.IsNullOrWhiteSpace(setting.ClientSecretProtected),
				LastTestDate = setting.LastTestDate,
				LastTestSucceeded = setting.LastTestSucceeded,
				LastTestMessage = setting.LastTestMessage
			};
		}

		private static void ApplyStoredState(
			LogoRestApiSettingsVM model,
			LogoRestApiSetting? setting,
			bool preserveTestResult = false)
		{
			model.HasSavedPassword = !string.IsNullOrWhiteSpace(setting?.RestPasswordProtected);
			model.HasSavedClientSecret = !string.IsNullOrWhiteSpace(setting?.ClientSecretProtected);
			if (!preserveTestResult)
			{
				model.LastTestDate = setting?.LastTestDate;
				model.LastTestSucceeded = setting?.LastTestSucceeded;
				model.LastTestMessage = setting?.LastTestMessage;
			}
		}

		private static string NormalizeServerAddress(string serverAddress)
		{
			var value = serverAddress.Trim();
			return Uri.TryCreate(value, UriKind.Absolute, out var uri) ? uri.Host : value;
		}

	}
}
