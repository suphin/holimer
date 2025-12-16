



using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2013.Drawing.Chart;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Ekomers.Models.Models;
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
	[Authorize(Policy = "AdminOrSatislar")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class SatislarController : BaseController
	{
		private readonly ISatislarService _service;
	 
		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly IMalzemeFiyatService _malzemeFiyatService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<SatislarDurum> _durumCache;
		private readonly ICacheService<SatislarSebep> _sebepCache;
		private readonly ICacheService<SatislarTur> _turCache;
		private readonly ICacheService<SatislarPlatform> _platformCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "DestekHizmetleri";
		public SatislarController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 ISatislarService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<SatislarDurum> durumCache
			, ICacheService<SatislarTur> turCache
			, ICacheService<Kullanici> kullaniciCache
			, ICacheService<SatislarSebep> sebepCache
			,ICacheService<SatislarPlatform> platformCache
			 , IMalzemeFiyatService malzemeFiyatService
			, IStokService stokService
			,IMalzemeService malzemeService
			, ITcmbService tcmbService
			) : base(userManager, roleManager)
		{
			_service = service;
			_malzemeFiyatService = malzemeFiyatService;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerService = sehirlerService;
			_turCache = turCache;
			_durumCache = durumCache;
			_kullaniciCache = kullaniciCache;
			 
			_stokService = stokService;
			_malzemeService = malzemeService;
			_tcmbService = tcmbService;
			 
			_sebepCache= sebepCache;
			_platformCache= platformCache;
		}
	
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	 
		private async Task ViewBagListeDoldur()
		{
			//ViewBag.SatislarDurumListe = await _durumCache.GetListeAsync(CacheKeys.SatislarDurumAll);
			 
		}

		private async Task ViewBagPartialListeDoldur()
		{
			//ViewBag.SatislarDurumListe = await _durumCache.GetListeAsync(CacheKeys.SatislarDurumAll);
			//ViewBag.SatislarSebepListe = await _sebepCache.GetListeAsync(CacheKeys.SatislarSebepAll);
			//ViewBag.SatislarPlatformListe = await _platformCache.GetListeAsync(CacheKeys.SatislarPlatformAll);


			//ViewBag.SatislarTurListe = await _turCache.GetListeAsync(CacheKeys.SatislarTurAll);
			 

			Expression<Func<Kullanici, bool>> filter = a => a.IsMhUser == true  ;

			ViewBag.SorumluListe = await _kullaniciCache.GetListeAsync(CacheKeys.SorumlularAll,filter);
			Expression<Func<Kullanici, bool>> filter2 = a => a.IsActive == true;
			ViewBag.PersonelListe = await _kullaniciCache.GetListeAsync(CacheKeys.PersonelAll, filter2,x=>x.AdSoyad,orderByDesc:false);
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

			var model = new SatislarVM
			{
				SatislarVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(SatislarVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new SatislarVM
			{
				SatislarVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();

		 

			modelc.ControllerName = "Satislar";
			modelc.ModalTitle = "Satış Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();

			
			modelc.ControllerName = "Satislar";
			modelc.ModalTitle = "Satış Bilgileri";

			modelc.UserID = _userId;
			
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(SatislarVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);
			//var SatislarID = await _service.VeriEkleReturnIDAsync(model);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new SatislarVM
				{
					SatislarVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID == 0)
				{
					return PartialView("~/Views/Satislar/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(SatislarVM model)
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

			var siparis = await _service.VeriGetir(SiparisID);
			var modelvm = new SatislarUrunlerVM()
			{
				CreateDate = DateTime.Now,
				SiparisID= SiparisID,
				SatislarUrunlerVMListe = await _service.SatislarUrunlerGetir(SiparisID)
				
			};
			   
			ViewBag.Kurlar= await _tcmbService.DovizKuruGetir();
			ViewBag.IsDone=  _service.VeriGetir(SiparisID).Result.IsDone;
			ViewBag.CariTip=  _service.VeriGetir(SiparisID).Result.CariTipi;
			ViewBag.Siparis = siparis;
			if (modelvm.SatislarUrunlerVMListe == null)
			{
				modelvm.SatislarUrunlerVMListe = new List<SatislarUrunlerVM>();
			}


			return PartialView("_UrunEkle",modelvm);
		}
		public async Task<PartialViewResult> UrunCikar(int urunId,int SiparisID)
		{
			var sonuc = await _service.SatislarUrunCikar(urunId);
			var siparis = await _service.VeriGetir(SiparisID);
			var model = new SatislarUrunlerVM
			{
				SatislarUrunlerVMListe = await _service.SatislarUrunlerGetir(SiparisID)
			};
			ViewBag.IsDone = _service.VeriGetir(SiparisID).Result.IsDone;
			ViewBag.CariTip = _service.VeriGetir(SiparisID).Result.CariTipi;
			ViewBag.Siparis = siparis;
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
		public async Task<PartialViewResult> _SiparisUrunEkle(SatislarUrunlerVM models)
		{
			var urun = await _malzemeService.VeriGetir(models.UrunID);
			var siparis = await _service.VeriGetir(models.SiparisID);
			// Yeni eklenen malzemeyi listeye ekliyoruz
			var SatislarUrunekle = new SatislarUrunlerVM
			{
				UrunAd = urun.Ad,
				UrunKod = urun.Kod,
				UrunID = models.UrunID,
				Miktar = models.Miktar,
				Iskonto = models.Iskonto,
				BirimID = urun.BirimID,
				BirimAd = urun.BirimAd,				
				Fiyat =(double)urun.FiyatSatis,
				Maliyet =(double)urun.MaliyetSatis,
				Kdv= (double)urun.Kdv,
				DovizTurAd = urun.DovizTurAd,
				DovizTur = urun.DovizTur,
				
				Aciklama = models.Aciklama,
				SiparisID=models.SiparisID
			};

			await _service.SatislarUrunEkle(SatislarUrunekle);

			  
			var model = new SatislarUrunlerVM
			{
				SatislarUrunlerVMListe = await _service.SatislarUrunlerGetir(models.SiparisID)
			};
			ViewBag.IsDone = siparis.IsDone;
			ViewBag.CariTip = siparis.CariTipi;
			ViewBag.Siparis = siparis;
			return PartialView("_UrunEklenen", model);
		}

		public async Task<IActionResult> Fiyatlar()
		{
			ViewBag.Modul = ModulAd;
			var model = await _context.Malzeme.Where(p => p.Kod.StartsWith("152MM") || p.Kod.StartsWith("153TG"))
				.Select(x => new MalzemeFiyatGuncelleVM
				{
					MalzemeId = x.ID,
					Ad = x.Ad,
					Kod = x.Kod,
					MevcutMaliyetSatis = x.MaliyetSatis,
					MevcutFiyatSatis = x.FiyatSatis,
					GuncellemeTarihiSatis = x.SonMaliyetGuncellemeTarih,
				}).OrderBy(x => x.Kod)
				.ToListAsync();

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Fiyatlar([FromBody] List<MalzemeFiyatGuncelleDto> model)
		{
			await _malzemeFiyatService.TopluMaliyetGuncelleAsync(model);
			return Ok();
		}
		public async Task<IActionResult> SiparisKapat(int SiparisID)
		{
			bool sonuc = await _service.SiparisKapat(SiparisID);
			if (sonuc)
			{
				return Ok("Sipariş kapatıldı.");
			}
			else
			{
				return BadRequest("Veri silinemedi.");
			}
		}
		[Authorize(Roles = "Admin")]
		public async Task<IActionResult> SiparisAc(int SiparisID)
		{
			bool sonuc = await _service.SiparisAc(SiparisID);
			if (sonuc)
			{
				return Ok("Sipariş açıldı.");
			}
			else
			{
				return BadRequest("Veri silinemedi.");
			}
		}
	}
}
