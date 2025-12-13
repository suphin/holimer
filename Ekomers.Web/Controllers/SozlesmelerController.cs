 
 
 
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Controllers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrDokuman")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class SozlesmelerController : BaseController
	{
		private readonly ISozlesmelerService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICacheService<SozlesmelerDurum> _durumCache;
		private readonly ICacheService<SozlesmelerKonu> _konuCache;
		private readonly ICacheService<Sirketler> _sirketlerCache;
		private string ModulAd = "DijitalSirket";
		public SozlesmelerController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 ISozlesmelerService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ICacheService<SozlesmelerDurum> durumCache
			, ICacheService<SozlesmelerKonu> konuCache
			, ICacheService<Sirketler> sirketlerCache
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_durumCache = durumCache;
			_konuCache = konuCache;
			_sirketlerCache = sirketlerCache;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}

		private async Task ViewBagListeDoldur()
		{
			 
			ViewBag.DurumListe =await _durumCache.GetListeAsync(CacheKeys.SozlesmeDurumAll);
			ViewBag.KonuListe = await _konuCache.GetListeAsync(CacheKeys.SozlesmeKonuAll);
			ViewBag.SirketlerListe = await _sirketlerCache.GetListeAsync(CacheKeys.SirketlerAll);
		}
		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.DurumListe = await _durumCache.GetListeAsync(CacheKeys.SozlesmeDurumAll);
			ViewBag.KonuListe = await _konuCache.GetListeAsync(CacheKeys.SozlesmeKonuAll);
			ViewBag.SirketlerListe = await _sirketlerCache.GetListeAsync(CacheKeys.SirketlerAll);

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
		//public async Task<IActionResult> Index()
		//{
		//	ViewBag.Modul = "CRM";
		//	await ViewBagListeDoldur();
		//	var model = new SozlesmelerVM
		//	{
		//		SozlesmelerVMListe = await _service.VeriListele()
		//	};

		//	return View(model);
		//}

		public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var paged = await _service.VeriListeleAsync(page, pageSize, ct);

			var model = new SozlesmelerVM
			{
				SozlesmelerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};
			ViewBag.PageIndex = page;
			ViewBag.PageSize = pageSize;
			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(SozlesmelerVM modelv)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new SozlesmelerVM
			{
				SozlesmelerVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			
			var modelc = await _service.VeriGetir(VeriID);
			 
				await ViewBagPartialListeDoldur();
			 

				
			modelc.ControllerName = "Sozlesmeler";
			modelc.ModalTitle = "Sözleşme Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			 
				await ViewBagPartialListeDoldur();
		 



			modelc.ControllerName = "Sozlesmeler";
			modelc.ModalTitle = "Sözleşme Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}
		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(SozlesmelerVM modelv)
		{
			bool sonuc =  await _service.VeriEkleAsync(modelv);
			 
			 var paged = await _service.VeriListeleAsync(1, 10, default);

			 
			if (sonuc)
			{
				var modelc = new SozlesmelerVM
				{
					SozlesmelerVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};
				 
				if (modelv.ID == 0)
				{
					return PartialView("~/Views/Sozlesmeler/_List.cshtml", modelc);
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
		public async Task<IActionResult> VeriSil(SozlesmelerVM model)
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

		public IActionResult Ara(string searchName)
		{
			 

			// Güvenli ve performanslı arama (ToLower/EF Core vs. projenize göre uyarlayın)
			var query = _context.Sozlesmeler
				.Where(x => (searchName == null || x.Aciklama.Contains(searchName)))
				.OrderBy(x => x.BitisTarih);

			var items = query  
				.Select(x => new { id = x.ID, ad = x.Aciklama })
				.ToList();	 

			return Ok(items);
		}

		public async Task<IActionResult> Rapor()
		{

			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new SozlesmelerVM
			{
				SozlesmelerVMListe = await _service.VeriListele()
			};
			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Rapor(SozlesmelerVM modelc)
		{

			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new SozlesmelerVM
			{
				SozlesmelerVMListe = await _service.VeriListele(modelc)
			};
			return View(model);
		}

	}
}
