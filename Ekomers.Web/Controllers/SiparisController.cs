



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
	public class SiparisController : BaseController
	{
		private readonly ISiparisService _service;
		private readonly ITeklifService _teklifService;
		private readonly IFirsatService _firsatService;
		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<SiparisDurum> _durumCache;
		private readonly ICacheService<SiparisTur> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "CRM";
		public SiparisController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 ISiparisService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<SiparisDurum> durumCache
			, ICacheService<SiparisTur> turCache
			, ICacheService<Kullanici> kullaniciCache
			,ITeklifService TeklifService
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
		}
	
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	 
		private async Task ViewBagListeDoldur()
		{
			var turler = await _durumCache.GetListeAsync(CacheKeys.SiparisDurumAll);

			var uiList = new List<SiparisDurum>(turler.Count + 1);
			uiList.Add(new SiparisDurum { ID = 0, Ad = "Tümü" });
			uiList.AddRange(turler);

			ViewBag.SiparisDurumListe = new SelectList(uiList, "ID", "Ad");
		}

		private async Task ViewBagPartialListeDoldur()
		{
			var durumlar = await _durumCache.GetListeAsync(CacheKeys.SiparisDurumAll);
			ViewBag.SiparisDurumListe = new SelectList(durumlar, "ID", "Ad");

			var turler = await _turCache.GetListeAsync(CacheKeys.SiparisTurAll);
			ViewBag.SiparisTurListe = new SelectList(turler, "ID", "Ad");

			Expression<Func<Kullanici, bool>> filter = a => a.IsCrmUser == true  ;

			var sorumlular = await _kullaniciCache.GetListeAsync(CacheKeys.CrmUserAll, filter);
			ViewBag.SorumluListe = new SelectList(sorumlular, "Id", "AdSoyad");

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

			var model = new SiparisVM
			{
				SiparisVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(SiparisVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new SiparisVM
			{
				SiparisVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();

		 

			modelc.ControllerName = "Siparis";
			modelc.ModalTitle = "Siparis Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Siparis";
			modelc.ModalTitle = "Siparis Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(SiparisVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new SiparisVM
				{
					SiparisVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID == 0)
				{
					return PartialView("~/Views/Siparis/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(SiparisVM model)
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

		public async Task<IActionResult> TekliftenSiparise(int TeklifID)
		{
			var teklif = await _teklifService.VeriGetir(TeklifID);
			 var firsat = await _firsatService.VeriGetir(teklif.FirsatID ?? 0);

			var model = new SiparisVM
			{
				Aciklama = "",
				CreateDate = DateTime.Now,
				CreateUserID = _userId,
				IsActive = true,
				IsDelete = false,
				TarihSaat = DateTime.Now,
				TeklifID = TeklifID,
				FirsatID = teklif.FirsatID,
				Not = teklif.Not,
				DolarKuru = teklif.DolarKuru,
				EuroKuru = teklif.EuroKuru,
				KdvToplam = teklif.KdvToplam,
				IskontoToplam = teklif.IskontoToplam,
				SiparisToplam = teklif.SiparisToplam,
				Toplam=teklif.Toplam,
				SatirIskontoToplam= teklif.SatirIskontoToplam,


				DurumID =(int)EnumSiparisDurum.SiparisAlindi,
				MusteriID= teklif.MusteriID,
				GorevliID= teklif.GorevliID,
				SorumluID= teklif.SorumluID

			};

			var SiparisID =  await _service.VeriEkleReturnIDAsync(model);
			teklif.SiparisID=SiparisID;
			teklif.DurumID = (int)EnumTeklifDurum.SipariseDonustu;
			teklif.IsLocked = true;

			teklif.UpdateDate = DateTime.Now;
			teklif.UpdateUserID = _userId;
			await _teklifService.VeriEkleAsync(teklif) ;

			if (teklif.FirsatID!=null)
			{
				firsat.SiparisID = SiparisID;
				firsat.DurumID = (int)EnumFirsatDurum.SipariseDonustu;

			
				firsat.UpdateDate = DateTime.Now;
				firsat.UpdateUserID = _userId;
			
				await _firsatService.VeriEkleAsync(firsat);
			}
			

			var teklifurunler = await _teklifService.TeklifUrunlerGetir(TeklifID);
			foreach (var item in teklifurunler)
			{
				var siparisurun = new SiparisUrunlerVM
				{
					SiparisID = SiparisID,
					UrunID = item.UrunID,
					UrunAd = item.UrunAd,
					UrunKod = item.UrunKod,
					Miktar = item.Miktar,
					BirimID = item.BirimID,
					BirimAd = item.BirimAd,
					Fiyat = item.Fiyat,
					Kdv = item.Kdv,
					Iskonto = item.Iskonto,
					DovizTur = item.DovizTur,
					DovizTurAd = item.DovizTurAd,
					Aciklama = item.Aciklama
				};
				await _service.SiparisUrunEkle(siparisurun);
			}

			var teklifIskontolar = await _teklifService.TeklifIskontoGetir(TeklifID);
			foreach (var item in teklifIskontolar)
			{
				var siparisIskonto= new SiparisIskonto
				{
					SiparisID = SiparisID,
					Oran = item.Oran,
					Ad = item.Ad,
					Aciklama = item.Aciklama
				};
				await _service.SiparisIskontoEkle(siparisIskonto);
			}


			return RedirectToAction("Index");
		}
		public async Task<PartialViewResult> UrunEkle(int SiparisID)
		{
			 
			 
			var modelvm = new SiparisUrunlerVM()
			{
				CreateDate = DateTime.Now,
				SiparisID=SiparisID,
				SiparisUrunlerVMListe = await _service.SiparisUrunlerGetir(SiparisID),
				SiparisIskontoListe=await _service.SiparisIskontoGetir(SiparisID)
				
			};
			   
			ViewBag.Kurlar= await _tcmbService.DovizKuruGetir();

			if (modelvm.SiparisUrunlerVMListe == null)
			{
				modelvm.SiparisUrunlerVMListe = new List<SiparisUrunlerVM>();
			}


			return PartialView("_UrunEkle",modelvm);
		}
		public async Task<PartialViewResult> UrunCikar(int urunId,int SiparisID)
		{
			var sonuc = await _service.SiparisUrunCikar(urunId);

			var model = new SiparisUrunlerVM
			{
				SiparisUrunlerVMListe = await _service.SiparisUrunlerGetir(SiparisID),
				SiparisIskontoListe = await _service.SiparisIskontoGetir(SiparisID)
			};

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
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
		public async Task<PartialViewResult> _SiparisUrunEkle(SiparisUrunlerVM models)
		{
			var urun = await _malzemeService.VeriGetir(models.UrunID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var SiparisUrunekle = new SiparisUrunlerVM
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

			await _service.SiparisUrunEkle(SiparisUrunekle);
			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

			var model = new SiparisUrunlerVM
			{
				SiparisUrunlerVMListe = await _service.SiparisUrunlerGetir(models.SiparisID),
				SiparisIskontoListe = await _service.SiparisIskontoGetir(models.SiparisID)
			};

			return PartialView("_UrunEklenen", model);
		}


		public async Task<PartialViewResult> _SiparisIskontoEkle(SiparisUrunlerVM models)
		{
			 

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var iskonto = new SiparisIskonto
			{
				 Ad=models.IskontoAd,
				 Aciklama=models.IskontoAd,
				 Oran=models.IskontoOran,
				 SiparisID = models.SiparisID
			 
			};

			await _service.SiparisIskontoEkle(iskonto);
			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

			var model = new SiparisUrunlerVM
			{
				SiparisID = models.SiparisID,
				SiparisUrunlerVMListe = await _service.SiparisUrunlerGetir(models.SiparisID),
				SiparisIskontoListe = await _service.SiparisIskontoGetir(models.SiparisID)
			};
			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			return PartialView("_UrunEklenen", model);
		}
		public async Task<PartialViewResult> IskontoCikar(int iskontoId, int SiparisID)
		{
			var sonuc = await _service.SiparisIskontoCikar(iskontoId);
			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			var model = new SiparisUrunlerVM
			{
				SiparisID = SiparisID,
				SiparisUrunlerVMListe = await _service.SiparisUrunlerGetir(SiparisID),
				SiparisIskontoListe = await _service.SiparisIskontoGetir(SiparisID)
			};


			return PartialView("_UrunEklenen", model);
		}
	}
}
