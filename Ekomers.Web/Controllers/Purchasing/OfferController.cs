using DocumentFormat.OpenXml.Drawing.Charts;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
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
			var paged = await _requestService.UrunListeleAsync(page, pageSize, ct);

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

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			
			 
			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Offer";
			modelc.ModalTitle = "Talep Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(OfferVM model,string actionType)
		{
			 


			model.TarihSaat = DateTime.Now;


			 

			int sonuc = await _service.VeriEkleReturnIDAsync(model);




			if (sonuc > 0)
			{
				return Redirect("/Offer/VeriGoruntule2?view=_VeriGetir2&VeriID=" + sonuc);
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

	 
		  
		 

		 
		 
	}
}
