



using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Drawing.Charts;
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
	public class TeklifController : BaseController
	{
		private readonly ITeklifService _service;
		private readonly IFirsatService _firsatService;
		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<TeklifDurum> _durumCache;
		private readonly ICacheService<TeklifTur> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "CRM";
		public TeklifController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 ITeklifService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<TeklifDurum> durumCache
			, ICacheService<TeklifTur> turCache
			, ICacheService<Kullanici> kullaniciCache
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
			_firsatService = firsatService;
			_stokService = stokService;
			_malzemeService = malzemeService;
			_tcmbService = tcmbService;
		}
	
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
	 
		private async Task ViewBagListeDoldur()
		{
			var turler = await _durumCache.GetListeAsync(CacheKeys.TeklifDurumAll);

			var uiList = new List<TeklifDurum>(turler.Count + 1);
			uiList.Add(new TeklifDurum { ID = 0, Ad = "Tümü" });
			uiList.AddRange(turler);

			ViewBag.TeklifDurumListe = new SelectList(uiList, "ID", "Ad");
		}

		private async Task ViewBagPartialListeDoldur()
		{
			var durumlar = await _durumCache.GetListeAsync(CacheKeys.TeklifDurumAll);
			ViewBag.TeklifDurumListe = new SelectList(durumlar, "ID", "Ad");

			var turler = await _turCache.GetListeAsync(CacheKeys.TeklifTurAll);
			ViewBag.TeklifTurListe = new SelectList(turler, "ID", "Ad");

			Expression<Func<Kullanici, bool>> filter = a => a.IsCrmUser == true  ;

			var sorumlular = await _kullaniciCache.GetListeAsync(CacheKeys.CrmUserAll,filter);
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

			var model = new TeklifVM
			{
				TeklifVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(TeklifVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new TeklifVM
			{
				TeklifVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();

		 

			modelc.ControllerName = "Teklif";
			modelc.ModalTitle = "Teklif Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Teklif";
			modelc.ModalTitle = "Teklif Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(TeklifVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new TeklifVM
				{
					TeklifVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID == 0)
				{
					return PartialView("~/Views/Teklif/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(TeklifVM model)
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
        public async Task<IActionResult> FirsattanTeklife(int FirsatID)
		{
			var firsat = await _firsatService.VeriGetir(FirsatID);
			 
			var model = new TeklifVM
			{
				Aciklama = "",
				CreateDate = DateTime.Now,
				CreateUserID = _userId,
				IsActive = true,
				IsDelete = false,
				TarihSaat = DateTime.Now,
				FirsatID = FirsatID,
				Not = firsat.Not,
				DurumID=(int)EnumTeklifDurum.TeklifVerildi,
				MusteriID= firsat.MusteriID,
				GorevliID= firsat.GorevliID,
				SorumluID= firsat.SorumluID
			};

			var teklifID =  await _service.VeriEkleReturnIDAsync(model);
			     
			firsat.TeklifID=teklifID;
			firsat.DurumID = (int)EnumFirsatDurum.TeklifeDonustu;
			firsat.UpdateDate=DateTime.Now;
			firsat.UpdateUserID = _userId;
			firsat.IsLocked = true;
			await _firsatService.VeriEkleAsync(firsat) ;


			return RedirectToAction("Index");
		}
		public async Task<PartialViewResult> UrunEkle(int TeklifID)
		{
			var teklif = await _service.VeriGetir(TeklifID);

			var modelvm = new TeklifUrunlerVM()
			{
				CreateDate = DateTime.Now,
				TeklifID=TeklifID,
				TeklifIsLocked=teklif.IsLocked,
				TeklifUrunlerVMListe = await _service.TeklifUrunlerGetir(TeklifID),
                TeklifIskontoListe = await _service.TeklifIskontoGetir(TeklifID)
            };
			   
			ViewBag.Kurlar= await _tcmbService.DovizKuruGetir();

			if (modelvm.TeklifUrunlerVMListe == null)
			{
				modelvm.TeklifUrunlerVMListe = new List<TeklifUrunlerVM>();
			}


			return PartialView("_UrunEkle",modelvm);
		}
		public async Task<PartialViewResult> UrunCikar(int urunId,int teklifID)
		{
			var sonuc = await _service.TeklifUrunCikar(urunId);

			var model = new TeklifUrunlerVM
			{
				TeklifUrunlerVMListe = await _service.TeklifUrunlerGetir(teklifID),
                TeklifIskontoListe = await _service.TeklifIskontoGetir(teklifID)
            };

            ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
            return PartialView("_UrunEklenen", model);
		}
		
		public async Task<PartialViewResult> _TeklifUrunEkle(TeklifUrunlerVM models)
		{
			var urun = await _malzemeService.VeriGetir(models.UrunID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var teklifUrunekle = new TeklifUrunlerVM
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
				TeklifID=models.TeklifID
			};

			await _service.TeklifUrunEkle(teklifUrunekle);
            ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

            var model = new TeklifUrunlerVM
			{
				TeklifUrunlerVMListe = await _service.TeklifUrunlerGetir(models.TeklifID),
                TeklifIskontoListe = await _service.TeklifIskontoGetir(models.TeklifID)
            };

			return PartialView("_UrunEklenen", model);
		}
        public async Task<PartialViewResult> _TeklifIskontoEkle(TeklifUrunlerVM models)
        {


            // Yeni eklenen malzemeyi listeye ekliyoruz
            var iskonto = new TeklifIskonto
            {
                Ad = models.IskontoAd,
                Aciklama = models.IskontoAd,
                Oran = models.IskontoOran,
                TeklifID = models.TeklifID

            };

            await _service.TeklifIskontoEkle(iskonto);
            ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

            var model = new TeklifUrunlerVM
            {
                TeklifID = models.TeklifID,
                TeklifUrunlerVMListe = await _service.TeklifUrunlerGetir(models.TeklifID),
                TeklifIskontoListe = await _service.TeklifIskontoGetir(models.TeklifID)
            };
            ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
            return PartialView("_UrunEklenen", model);
        }
        public async Task<PartialViewResult> IskontoCikar(int iskontoId, int TeklifID)
        {
            var sonuc = await _service.TeklifIskontoCikar(iskontoId);
            ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
            var model = new TeklifUrunlerVM
            {
                TeklifID = TeklifID,
                TeklifUrunlerVMListe = await _service.TeklifUrunlerGetir(TeklifID),
                TeklifIskontoListe = await _service.TeklifIskontoGetir(TeklifID)
            };


            return PartialView("_UrunEklenen", model);
        }
        //[Authorize(Policy = "Create")]
        //[HttpPost]
        //public async Task<IActionResult> _TeklifUrunEkleBitir(int teklifID)
        //{
        //	try
        //	{
        //		var malzemeStokJson = HttpContext.Session.GetString("TeklifUrunlerListe");
        //		var teklifUrunListe = JsonConvert.DeserializeObject<List<TeklifUrunlerVM>>(malzemeStokJson);


        //		var models = new TeklifUrunlerVM
        //		{
        //			TeklifUrunlerVMListe = teklifUrunListe,
        //			TeklifID=teklifID
        //		};


        //		bool sonuc = await _service.VeriEkleCoklu(models);

        //		// Session'daki "MalzemeStokListe" anahtarına bağlı veriyi kaldırıyoruz
        //		HttpContext.Session.Remove("TeklifUrunlerListe");
        //		var modelvm = new TeklifUrunlerVM()
        //		{
        //			CreateDate = DateTime.Now,
        //			TeklifUrunlerVMListe= await _service.TeklifUrunlerGetir(teklifID)
        //		};



        //		if (modelvm.TeklifUrunlerVMListe == null)
        //		{
        //			modelvm.TeklifUrunlerVMListe = new List<TeklifUrunlerVM>();
        //		}


        //		return PartialView("_UrunEkle", modelvm);
        //		//return Ok("Ürünler kaydedildi");
        //	}
        //	catch (Exception ex)
        //	{

        //		return BadRequest("Bir hata oluştu. Hata: " + ex.Message);
        //	}

        //}
    }
}
