
 
 
 
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;


namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class AktiviteController : BaseController
	{
		private readonly IAktiviteService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<AktiviteTur> _turCache;
		private readonly ILoggerService _loggerService;
		private string ModulAd = "CRM";
		public AktiviteController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IAktiviteService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<AktiviteTur> turCache
			, ILoggerService loggerService
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerService = sehirlerService;
			_turCache = turCache;
			_loggerService = loggerService;
		}
		private static class CacheKeys
		{
			public const string AktiviteTurAll = "AktiviteTur:All";
			public const string MusterilerAll = "Musteriler:All";
		}
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	 
		private async Task ViewBagListeDoldur()
		{
			var turler = await _turCache.GetListeAsync(CacheKeys.AktiviteTurAll);

			var uiList = new List<AktiviteTur>(turler.Count + 1);
			uiList.Add(new AktiviteTur { ID = 0, Ad = "Tümü" });
			uiList.AddRange(turler);

			ViewBag.AktiviteTurListe = new SelectList(uiList, "ID", "Ad");
		}

		private async Task ViewBagPartialListeDoldur()
		{
			var turler = await _turCache.GetListeAsync(CacheKeys.AktiviteTurAll);

			ViewBag.AktiviteTurListe = new SelectList(turler, "ID", "Ad");


		}

		//private async Task ViewBagListeDoldur()
		//{


		//	var AktiviteTurListe = await _context.AktiviteTur.OrderBy(p => p.Ad).ToListAsync();
		//	AktiviteTurListe.Insert(0, new AktiviteTur { ID = 0, Ad = "Tümü" });
		//	ViewBag.AktiviteTurListe = new SelectList(AktiviteTurListe, "ID", "Ad");

		//}
		//private async Task ViewBagPartialListeDoldur()
		//{
		//	ViewBag.AktiviteTurListe = new SelectList(await _context.AktiviteTur.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");			 
		//	//ViewBag.MusterilerListe = new SelectList(await _context.Musteriler.OrderBy(p => p.AdSoyad).ToListAsync(), "ID", "AdSoyad");			 
		//}
		 
		private void PageToastr(bool sonuc)
		{
			if (sonuc)
			{
				TempData["SuccessMessage"] = "Kaydetme işlemi başarılı!";
			}
			else
			{
				TempData["ErrorMessage"] = "Bir hata oluştu.";
			}
		}

		public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var paged = await _service.VeriListeleAsync(page, pageSize, ct);

			var model = new AktiviteVM
			{
				AktiviteVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(AktiviteVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new AktiviteVM
			{
				AktiviteVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();
		


			modelc.ControllerName = "Aktivite";
			modelc.ModalTitle = "Aktivite Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Aktivite";
			modelc.ModalTitle = "Aktivite Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}
		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(AktiviteVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new AktiviteVM
				{
					AktiviteVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID==0)
				{
					return PartialView("~/Views/Aktivite/_List.cshtml", modelc);
				}
				
				return Ok("Kayıt işlemi başarılı");
			}
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(AktiviteVM model)
		{
			bool sonuc = await _service.VeriSil(model.ID);
			if (sonuc)
			{
				return Ok(model.ID);
			}
			else
			{
				return BadRequest("Veri silinemedi.");
			}
		}




	}
}
