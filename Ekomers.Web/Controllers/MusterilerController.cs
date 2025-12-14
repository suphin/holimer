 
 
 
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
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class MusterilerController : BaseController
	{
		private readonly IMusterilerService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<MusteriTip> _musteriTipCache;
		private string ModulAd = "CRM";
		public MusterilerController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IMusterilerService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<MusteriTip> musteriTipCache
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerService = sehirlerService;
			_musteriTipCache = musteriTipCache;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}

		private async Task ViewBagListeDoldur()
		{


			//var MusteriTipListe = await _context.MusteriTip.OrderBy(p => p.Ad).ToListAsync();
			//MusteriTipListe.Insert(0, new MusteriTip { ID = 0, Ad = "Tümü" });
			//ViewBag.MusteriTipListe = new SelectList(MusteriTipListe, "ID", "Ad");
			ViewBag.MusteriTipListe = await _musteriTipCache.GetListeAsync(CacheKeys.MusteriTipAll);

		}
		private async Task ViewBagPartialListeDoldur(int sehirId,int ilceId)
		{
			ViewBag.MusteriTipListe = new SelectList(await _context.MusteriTip.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");
			 
			ViewBag.SehirlerListe = new SelectList(await _sehirlerService.GetSehirler(0), "ID", "Ad", sehirId);
			ViewBag.IlcelerListe = new SelectList(await _sehirlerService.GetSehirler(sehirId), "ID", "Ad");
			ViewBag.MahalleListe = new SelectList(await _sehirlerService.GetMahalle(ilceId), "ID", "Ad");
		}
		private async Task ViewBagPartialListeDoldur()
		{
			//ViewBag.MusteriTipListe = new SelectList(await _context.MusteriTip.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");
			ViewBag.MusteriTipListe = await _musteriTipCache.GetListeAsync(CacheKeys.MusteriTipAll);

			ViewBag.SehirlerListe = new SelectList(await _sehirlerService.GetSehirler(0), "ID", "Ad",34);
			ViewBag.IlcelerListe = new SelectList(await _sehirlerService.GetSehirler(34), "ID", "Ad");
			ViewBag.MahalleListe = new SelectList(await _sehirlerService.GetMahalle(1476), "ID", "Ad");
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
		//	var model = new MusterilerVM
		//	{
		//		MusterilerVMListe = await _service.VeriListele()
		//	};

		//	return View(model);
		//}

		public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var paged = await _service.VeriListeleAsync(page, pageSize, ct);

			var model = new MusterilerVM
			{
				MusterilerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};
			ViewBag.PageIndex = page;
			ViewBag.PageSize = pageSize;
			return View(model);
		}


		[HttpPost]
		public async Task<IActionResult> Index(MusterilerVM modelv)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new MusterilerVM
			{
				MusterilerVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			
			var modelc = await _service.VeriGetir(VeriID);
			if (VeriID==0)
			{
				await ViewBagPartialListeDoldur();
			}
			else
			{
				await ViewBagPartialListeDoldur((int)modelc.SehirID, (int)modelc.IlceID);
			}

				
			modelc.ControllerName = "Musteriler";
			modelc.ModalTitle = "Musteriler Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			if (VeriID == 0)
			{
				await ViewBagPartialListeDoldur();
			}
			else
			{
				if ((int)modelc.SehirID==0 && (int)modelc.IlceID==0)
				{
					await ViewBagPartialListeDoldur(1,1055);
				}
				else
				{
					await ViewBagPartialListeDoldur((int)modelc.SehirID, (int)modelc.IlceID);
				}
				//
			}



			modelc.ControllerName = "Musteriler";
			modelc.ModalTitle = "Müşteri Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}
		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(MusterilerVM modelv)
		{
			bool sonuc =  await _service.VeriEkleAsync(modelv);
			//if (sonuc)
			//{
			//	if (modelv.ID==0)
			//	{
			//		return RedirectToAction("Index", new { page = modelv.PageIndex, pageSize = modelv.PageSize });
			//	}

			//	return Ok("Kayıt işlemi başarılı");
			//}
			//else
			//{
			//	return BadRequest("Kaydetme başarısız!");
			//}
			 var paged = await _service.VeriListeleAsync(1, 10, default);

			////PageToastr(sonuc);
			////return RedirectToAction("Index");
			if (sonuc)
			{
				var modelc = new MusterilerVM
				{
					MusterilerVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};
				 
				if (modelv.ID == 0)
				{
					return PartialView("~/Views/Musteriler/_List.cshtml", modelc);
				}

				return Ok("Kayıt işlemi başarılı");
			}
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
		}


		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkleAjax(MusterilerVM modelv)
		{
			bool sonuc = await _service.VeriEkleAsync(modelv);
			if (sonuc)
			{
				return Ok("Kayıt işlemi başarılı");
			}
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}			 
		}


		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(MusterilerVM model)
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
			var query = _context.Musteriler
				.Where(x => (searchName == null || x.AdSoyad.Contains(searchName)))
				.OrderBy(x => x.AdSoyad);

			var items = query  
				.Select(x => new { id = x.ID, ad = x.AdSoyad })
				.ToList();

		 

			return Ok(items);
		}


	}
}
