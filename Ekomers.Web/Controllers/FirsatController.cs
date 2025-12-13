
 
 
 
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
using System.Linq.Expressions;
using System.Security.Claims;


namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class FirsatController : BaseController
	{
		private readonly IFirsatService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<FirsatDurum> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "CRM";
		public FirsatController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IFirsatService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<FirsatDurum> turCache
			, ICacheService<Kullanici> kullaniciCache
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerService = sehirlerService;
			_turCache = turCache;
			_kullaniciCache = kullaniciCache;
		}
	
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	 
		private async Task ViewBagListeDoldur()
		{
			var turler = await _turCache.GetListeAsync(CacheKeys.FirsatDurumAll);

			var uiList = new List<FirsatDurum>(turler.Count + 1);
			uiList.Add(new FirsatDurum { ID = 0, Ad = "Tümü" });
			uiList.AddRange(turler);

			ViewBag.FirsatDurumListe = new SelectList(uiList, "ID", "Ad");
		}

		private async Task ViewBagPartialListeDoldur()
		{
			var turler = await _turCache.GetListeAsync(CacheKeys.FirsatDurumAll);
			ViewBag.FirsatDurumListe = new SelectList(turler, "ID", "Ad");

			Expression<Func<Kullanici, bool>> filter = a => a.IsCrmUser == true  ;

			var sorumlular = await _kullaniciCache.GetListeAsync(CacheKeys.SorumlularAll,filter);
			ViewBag.SorumluListe = new SelectList(sorumlular, "Id", "AdSoyad");

		}

		//private async Task ViewBagListeDoldur()
		//{


		//	var FirsatDurumListe = await _context.FirsatDurum.OrderBy(p => p.Ad).ToListAsync();
		//	FirsatDurumListe.Insert(0, new FirsatDurum { ID = 0, Ad = "Tümü" });
		//	ViewBag.FirsatDurumListe = new SelectList(FirsatDurumListe, "ID", "Ad");

		//}
		//private async Task ViewBagPartialListeDoldur()
		//{
		//	ViewBag.FirsatDurumListe = new SelectList(await _context.FirsatDurum.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");			 
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

			var model = new FirsatVM
			{
				FirsatVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(FirsatVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new FirsatVM
			{
				FirsatVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();

		 

			modelc.ControllerName = "Firsat";
			modelc.ModalTitle = "Fırsat Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Firsat";
			modelc.ModalTitle = "Fırsat Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}
		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(FirsatVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new FirsatVM
				{
					FirsatVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID == 0)
				{
					return PartialView("~/Views/Firsat/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(FirsatVM model)
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
