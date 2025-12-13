



using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Office2013.Drawing.Chart;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Linq.Expressions;
using System.Security.Claims;


namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class SiparisIadeController : BaseController
	{
		private readonly ISiparisIadeService _service;
		private readonly ITeklifService _teklifService;
		private readonly IFirsatService _firsatService;
		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<SiparisIadeDurum> _durumCache;
		private readonly ICacheService<SiparisIadeSebep> _sebepCache;
		private readonly ICacheService<SiparisIadeTur> _turCache;
		private readonly ICacheService<SiparisIadePlatform> _platformCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "CRM";
		public SiparisIadeController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 ISiparisIadeService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<SiparisIadeDurum> durumCache
			, ICacheService<SiparisIadeTur> turCache
			, ICacheService<Kullanici> kullaniciCache
			, ICacheService<SiparisIadeSebep> sebepCache
			,ICacheService<SiparisIadePlatform> platformCache
			, ITeklifService TeklifService
			,IFirsatService firsatService
			, IStokService stokService
			,IMalzemeService malzemeService
			, ITcmbService tcmbService
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerService = sehirlerService;
			_turCache = turCache;
			_durumCache = durumCache;
			_kullaniciCache = kullaniciCache;
			_teklifService = TeklifService;
			_stokService = stokService;
			_malzemeService = malzemeService;
			_tcmbService = tcmbService;
			_firsatService = firsatService;
			_sebepCache= sebepCache;
			_platformCache= platformCache;
		}
	
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	 
		private async Task ViewBagListeDoldur()
		{
			ViewBag.SiparisIadeDurumListe = await _durumCache.GetListeAsync(CacheKeys.SiparisIadeDurumAll);
			 
		}

		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.SiparisIadeDurumListe = await _durumCache.GetListeAsync(CacheKeys.SiparisIadeDurumAll);
			ViewBag.SiparisIadeSebepListe = await _sebepCache.GetListeAsync(CacheKeys.SiparisIadeSebepAll);
			ViewBag.SiparisIadePlatformListe = await _platformCache.GetListeAsync(CacheKeys.SiparisIadePlatformAll);


			ViewBag.SiparisIadeTurListe = await _turCache.GetListeAsync(CacheKeys.SiparisIadeTurAll);
			 

			Expression<Func<Kullanici, bool>> filter = a => a.IsMhUser == true  ;

			ViewBag.SorumluListe = await _kullaniciCache.GetListeAsync(CacheKeys.SorumlularAll,filter);
			

		}

			 
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

			var model = new SiparisIadeVM
			{
				SiparisIadeVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(SiparisIadeVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new SiparisIadeVM
			{
				SiparisIadeVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();

		 

			modelc.ControllerName = "SiparisIade";
			modelc.ModalTitle = "SiparisIade Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "SiparisIade";
			modelc.ModalTitle = "SiparisIade Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(SiparisIadeVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);
			//var siparisIadeID = await _service.VeriEkleReturnIDAsync(model);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new SiparisIadeVM
				{
					SiparisIadeVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID == 0)
				{
					return PartialView("~/Views/SiparisIade/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(SiparisIadeVM model)
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

		
		public async Task<PartialViewResult> UrunEkle(int SiparisID)
		{
			 
			 
			var modelvm = new SiparisIadeUrunlerVM()
			{
				CreateDate = DateTime.Now,
				SiparisID= SiparisID,
				SiparisIadeUrunlerVMListe = await _service.SiparisIadeUrunlerGetir(SiparisID)
				
			};
			   
			ViewBag.Kurlar= await _tcmbService.DovizKuruGetir();

			if (modelvm.SiparisIadeUrunlerVMListe == null)
			{
				modelvm.SiparisIadeUrunlerVMListe = new List<SiparisIadeUrunlerVM>();
			}


			return PartialView("_UrunEkle",modelvm);
		}
		public async Task<PartialViewResult> UrunCikar(int urunId,int SiparisID)
		{
			var sonuc = await _service.SiparisIadeUrunCikar(urunId);

			var model = new SiparisIadeUrunlerVM
			{
				SiparisIadeUrunlerVMListe = await _service.SiparisIadeUrunlerGetir(SiparisID)
			};


			return PartialView("_UrunEklenen", model);
		}
		public async Task<IActionResult> MalzemeAra(string kelime)
		{
			if (kelime == null || kelime.Length < 3)
			{
				return BadRequest("Aranacak kelime 3 karakter veya fazla olmalı.");
			}

			var results = await _malzemeService.MalzemeAra(kelime);
			//var results = await _malzemeService.GenelListe().Where(p => p.Ad.Contains(kelime)).ToListAsync();
			return Ok(results);
		}
		public async Task<PartialViewResult> _SiparisUrunEkle(SiparisIadeUrunlerVM models)
		{
			var urun = await _malzemeService.VeriGetir(models.UrunID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var SiparisIadeUrunekle = new SiparisIadeUrunlerVM
			{
				UrunAd = urun.Ad,
				UrunKod = urun.Kod,
				UrunID = models.UrunID,
				Miktar = models.Miktar,
				Iskonto = models.Iskonto,
				BirimID = urun.BirimID,
				BirimAd = urun.BirimAd,				
				Fiyat = (double)urun.Fiyat,
				Kdv= (double)urun.Kdv,
				DovizTurAd = urun.DovizTurAd,
				DovizTur = urun.DovizTur,

				Aciklama = models.Aciklama,
				SiparisID=models.SiparisID
			};

			await _service.SiparisIadeUrunEkle(SiparisIadeUrunekle);

			  
			var model = new SiparisIadeUrunlerVM
			{
				SiparisIadeUrunlerVMListe = await _service.SiparisIadeUrunlerGetir(models.SiparisID)
			};

			return PartialView("_UrunEklenen", model);
		}
	
	}
}
