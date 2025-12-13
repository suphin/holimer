using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.InkML;
using Ekomers.Common.Services;
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
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrUretim")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class UretimController : BaseController
	{
		private readonly IUretimService _service;

		private readonly IMalzemeService _malzemeService;

		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICacheService<ReceteParametre> _receteParemetreCache;
		private readonly ICacheService<Uretici> _ureticiCache;

		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "Uretim";
		public UretimController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IUretimService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
		 , ICacheService<Uretici> ureticiCache
			, ICacheService<Kullanici> kullaniciCache
			 , ICacheService<ReceteParametre> receteParemetreCache
			, IMalzemeService malzemeService

			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_ureticiCache = ureticiCache;
			_kullaniciCache = kullaniciCache;
			_receteParemetreCache = receteParemetreCache;

			_malzemeService = malzemeService;

		}
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
		private async Task ViewBagListeDoldur()
		{
			ViewBag.ReceteParametreListe = await _receteParemetreCache.GetListeAsync(CacheKeys.ReceteParametreAll);
			ViewBag.UreticiListe = await _ureticiCache.GetListeAsync(CacheKeys.UreticiAll);
		}

		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.ReceteParametreListe = await _receteParemetreCache.GetListeAsync(CacheKeys.ReceteParametreAll);
			ViewBag.UreticiListe = await _ureticiCache.GetListeAsync(CacheKeys.UreticiAll);
		}
		public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var paged = await _service.VeriListeleAsync(page, pageSize, ct);

			var model = new UretimVM
			{
				UretimVMListe = paged.Items.ToList(), 
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Index(UretimVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new UretimVM
			{
				UretimVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Uretim";
			modelc.ModalTitle = "Uretim Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);
			modelc.UretimParametreDeger = await _service.UrerimParametreGetir(VeriID);
			modelc.UretimUrunlerVMListe = await _service.UretimUrunlerGetir(VeriID);
			modelc.UretimTeslimatListe = await _service.KismiTeslimatGetir(VeriID);
			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Uretim";
			modelc.ModalTitle = "Üretim Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(UretimVM model)
		{
			bool sonuc = await _service.VeriEkleAsync(model);


			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new UretimVM
				{
					UretimVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

				if (model.ID == 0)
				{
					return PartialView("~/Views/Recete/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(UretimVM model)
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


		public async Task<PartialViewResult> KismiTeslimatEkle(DateTime Tarih,int Miktar,int UretimID)
		{
			

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var teslimat = new UretimTeslimat
			{
				Tarih=Tarih,
				Miktar=Miktar,
				UretimID=UretimID
			};

			var sonuc = await _service.KismiTeslimatEkle(teslimat);

			var model = new UretimVM
			{
				UretimTeslimatListe = await _service.KismiTeslimatGetir(UretimID)
			};

			return PartialView("_TeslimatEklenenListe", model);
		}

		public async Task<PartialViewResult> TeslimatCikar(int teslimatId, int UretimID)
		{
			var sonuc = await _service.KismiTeslimatCikar(teslimatId);

			var model = new UretimVM
			{
				UretimTeslimatListe = await _service.KismiTeslimatGetir(UretimID)
			};


			return PartialView("_TeslimatEklenenListe", model);
		}

		public	async Task<IActionResult> Takvim()
		{

			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new UretimVM
			{
				UretimVMListe = await _service.VeriListele()
			};
			return View(model);
		}
		public	async Task<IActionResult> Rapor()
		{

			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			ViewBag.Malzemeler = await _malzemeService.Malzemeler();

			var model = new UretimVM
			{
				UretimVMListe = await _service.VeriListele(),
				SiparisTarihiBas=new DateTime(DateTime.Now.Year,1,1),
				SiparisTarihiSon= new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)
			};
			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Rapor(UretimVM modelg)
		{

			ViewBag.Modul = ModulAd;
			// JSON -> Listeye dönüştür
			

			ViewBag.Malzemeler = await _malzemeService.Malzemeler();

			var model = new UretimVM
			{
				UretimVMListe = await _service.VeriListele(modelg),
				tagifyMalzemeler=modelg.tagifyMalzemeler

			};
			return View(model);
		}

		public async Task<IActionResult> Dashboard()
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			//ViewBag.Malzemeler = await _malzemeService.Malzemeler();
			var model = new UretimVM
			{
				UretimVMListe = await _service.VeriListele(),
				SiparisTarihiBas = new DateTime(DateTime.Now.Year, 1, 1),
				SiparisTarihiSon = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day)
			};
			return View(model);
		}
	}
}

