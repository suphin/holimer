using Azure.Core;
using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Wordprocessing;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
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
using System.Security.Claims;


namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class OfferController : BaseController
	{
		private readonly IOfferService _service;
		private readonly IRequestService _requestService;
	 
		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		 
		private readonly ICacheService<OfferDurum> _durumCache;
		private readonly ICacheService<OfferTur> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private readonly ICacheService<Sirketler> _sirketCache;
		private string ModulAd = "PURCHASING";
		public OfferController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IOfferService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			 , IRequestService requestService
			, ICacheService<OfferDurum> durumCache
			, ICacheService<OfferTur> turCache
			, ICacheService<Kullanici> kullaniciCache
		 , ICacheService<Sirketler> sirketCache
			, IStokService stokService
			,IMalzemeService malzemeService
			, ITcmbService tcmbService
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_requestService = requestService;
			_turCache = turCache;
			_durumCache = durumCache;
			_kullaniciCache = kullaniciCache;
			 _sirketCache = sirketCache;
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
			 
		}

		private async Task ViewBagPartialListeDoldur()
		{ 

			 
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
			var paged = await _requestService.UrunListeleAsync(page, pageSize, ct, (int)EnumOfferDurum.TeklifAsamasinda);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}
		public async Task<IActionResult> Onay(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var paged = await _requestService.UrunListeleAsync(page, pageSize, ct, (int)EnumOfferDurum.TeklifOnayBekliyor);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Index(OfferVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new OfferVM
			{
				OfferVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}


		public async Task<IActionResult> Detail(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;
			if (model.OfferDurumID==(int)EnumOfferDurum.TeklifAsamasinda)
			{
				var offer = new OfferVM
				{
					RequestUrunID = Id
				};
				model.OfferVMListe = await _service.VeriListele(offer);
				return View(model);
			}

			return RedirectToAction("Index");

		}
		public async Task<IActionResult> OnayDetail(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;

			var offer = new OfferVM
			{
				RequestUrunID = Id
			};
			model.OfferVMListe = await _service.VeriListele(offer);
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "" )
		{

			var modelc = await _service.VeriGetir(VeriID);
			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			modelc.ControllerName = "Offer";
			modelc.ModalTitle = "Talep Bilgileri";
			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "")
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

			modelc.ControllerName = "Offer";
			modelc.ModalTitle = "Talep Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(OfferVM model)
		{ 
			model.TarihSaat = DateTime.Now;
			model.FirmaID = model.MusteriID;

			var sonuc = await _service.VeriEkleAsync(model);
			 
			if (sonuc)
			{
				return RedirectToAction("Detail",new {Id =model.RequestUrunID});
			} 
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(OfferVM model)
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
		 
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> Teklif(int requestUrunID)
		{
			var requestUrun = await _requestService.RequestUrunGetir(requestUrunID);

			var modelc = new OfferVM();
			modelc.ModalTitle = "Teklif Bilgileri";
			modelc.TeslimTarihi = DateTime.Now;
			modelc.RequestUrunID = requestUrunID;
			modelc.Miktar = requestUrun.MiktarSon;
			modelc.UserID = _userId;
			modelc.UrunKod = requestUrun.UrunKod;
			modelc.UrunAd = requestUrun.UrunAd;

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			modelc.ControllerName = "Offer";
			return PartialView("_Teklif", modelc);
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> Send(int Id)
		{
			var requestUrun = await _requestService.RequestUrunGetir(Id);
			requestUrun.OfferDurumID = (int)EnumOfferDurum.TeklifOnayBekliyor;
			var sonuc = _requestService.RequestUrunGuncelle(requestUrun);

			return RedirectToAction("Index");
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> OncekiTeklifler(int requestUrunID)
		{
			var model = await _requestService.RequestUrunGetir(requestUrunID);
			var offer = new OfferVM
			{
				UrunID = model.UrunID,
				PageSize = 10,
			};
			 
			model.OfferVMListe = await _service.VeriListele(offer);

			return PartialView("_OncekiTeklifler", model);
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> KabulEt(int Id,int requestUrunId)
		{
			var requestUrun = await _requestService.RequestUrunGetir(requestUrunId);
			requestUrun.OfferDurumID = (int)EnumOfferDurum.TeklifOnaylandi;
			var sonuc =await  _requestService.RequestUrunGuncelle(requestUrun);

			var modelc = await _service.VeriGetir(Id);
			modelc.IsSelected = true;
			 bool cevap  = await _service.VeriEkleAsync(modelc);

			return Ok("Başarılı");
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> OncekiTeklif(int teklifId,int RequestUrunID)
		{
			var requestUrun = await _requestService.RequestUrunGetir(RequestUrunID);

			var modelc =await  _service.VeriGetir(teklifId);
			modelc.ModalTitle = "Teklif Bilgileri";
			 
			modelc.RequestUrunID = RequestUrunID;
			modelc.ID = 0;

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			modelc.ControllerName = "Offer";
			return PartialView("_Teklif", modelc);
		}

		public async Task<IActionResult> Print(int offerId)
		{
			var offer = await _service.VeriGetir(offerId);

			if (offer == null)
				return NotFound();
			  
			return View("Print", offer);
		}

		public async Task<IActionResult> Arsiv(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{

			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var paged = await _requestService.OfferListeleAsync(page, pageSize, ct);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		public async Task<IActionResult> ArsivGoruntule(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;

			var offer = new OfferVM
			{
				RequestUrunID = Id
			};
			model.OfferVMListe = await _service.VeriListele(offer);
			return PartialView(model);
		}
	}
}
