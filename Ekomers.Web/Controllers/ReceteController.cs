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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrUretim")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class ReceteController : BaseController
	{
		private readonly IReceteService _service;
	 
		private readonly IMalzemeService _malzemeService;
	 
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICacheService<ReceteParametre> _receteParemetreCache;
		private readonly ICacheService<Uretici> _ureticiCache;

		private readonly ICacheService<Kullanici> _kullaniciCache;
		private string ModulAd = "Uretim";
		public ReceteController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IReceteService service
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

			var model = new ReceteVM
			{
				ReceteVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Index(ReceteVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new ReceteVM
			{
				ReceteVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Recete";
			modelc.ModalTitle = "Reçete Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);
			modelc.ReceteParametreDegerListe =await  _service.ReceteParametreGetir(VeriID);
			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Recete";
			modelc.ModalTitle = "Reçete Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(ReceteVM model, List<int> SeciliParametreler)
		{
			 
			var recete = await _service.VeriGetirUrunID(model.UrunID);
			if (recete.ID!=0 && model.ID == 0)
			{
				return BadRequest("Bu ürüne ait reçete vardır.");
			}

			//bool sonuc = await _service.VeriEkleAsync(model);
			var ReceteID = await _service.VeriEkleReturnIDAsync(model);
			bool sonuc2 = await _service.ReceteParametreEkle(ReceteID, SeciliParametreler);
			var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (ReceteID>0)
			{
				var modelc = new ReceteVM
				{
					ReceteVMListe = paged.Items.ToList(),
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
		public async Task<IActionResult> VeriSil(ReceteVM model)
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

	 
		public async Task<PartialViewResult> UrunEkle(int ReceteID)
		{


			var modelvm = new ReceteUrunlerVM()
			{
				CreateDate = DateTime.Now,
				ReceteID = ReceteID,
				ReceteUrunlerVMListe = await _service.ReceteUrunlerGetir(ReceteID) 

			};
			 

			if (modelvm.ReceteUrunlerVMListe == null)
			{
				modelvm.ReceteUrunlerVMListe = new List<ReceteUrunlerVM>();
			}


			return PartialView("_UrunEkle", modelvm);
		}
		public async Task<PartialViewResult> UrunCikar(int urunId, int ReceteID)
		{
			var sonuc = await _service.ReceteUrunCikar(urunId);

			var model = new ReceteUrunlerVM
			{
				ReceteUrunlerVMListe = await _service.ReceteUrunlerGetir(ReceteID) 
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
		public async Task<PartialViewResult> _ReceteUrunEkle(ReceteUrunlerVM models)
		{
			var urun = await _malzemeService.VeriGetir(models.UrunID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var ReceteUrunEkle = new ReceteUrunlerVM
			{
				UrunAd = urun.Ad,
				UrunKod = urun.Kod,
				UrunID = models.UrunID,
				 
				BirimID = urun.BirimID,
				BirimAd = urun.BirimAd,
				 
				 
				ReceteID = models.ReceteID
			};

			await _service.ReceteUrunEkle(ReceteUrunEkle); 

			var model = new ReceteUrunlerVM
			{
				ReceteUrunlerVMListe = await _service.ReceteUrunlerGetir(models.ReceteID) 
			};

			return PartialView("_UrunEklenen", model);
		}


		public async Task<IActionResult> UretimPlaniYap(int ReceteID)
		{
			ViewBag.Modul = ModulAd; 
			var urun = await _service.VeriGetir(ReceteID);

			var modelc = new UretimVM();
			modelc.UrunAd = urun.UrunAd;
			modelc.UrunKod = urun.UrunKod;
			modelc.UrunID = urun.UrunID;
			modelc.Not = urun.Not;
			modelc.ReceteID = ReceteID;
			modelc.SiparisTarihi = DateTime.Now;

			modelc.ReceteParametreDegerListe = await _service.ReceteParametreGetir(ReceteID);
			await ViewBagPartialListeDoldur();
			modelc.ReceteUrunlerVMListe = await _service.ReceteUrunlerGetir(ReceteID);


			modelc.ControllerName = "Recete";
			modelc.ModalTitle = "Üretim Planlama";

			modelc.UserID = _userId;
			return View("UretimPlaniYap", modelc);
		}

		[HttpPost]
		public async Task<IActionResult> UretimPlaniYap(UretimVM modelc)
		{


			bool sonuc =await _service.UretimEkle(modelc);


			if (sonuc)
			{
				return RedirectToAction("Index","Uretim");
			}
			else
			{
				return BadRequest("Veri silinemedi.");
			}


		}
	}
}
