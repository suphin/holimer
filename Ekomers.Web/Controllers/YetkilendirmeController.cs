
 
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
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Admin")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class YetkilendirmeController : BaseController
	{
		private readonly IYetkilendirmeService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private string ModulAd = "Admin";
		public YetkilendirmeController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IYetkilendirmeService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}

		private async Task ViewBagListeDoldur()
		{


			var KategoriListe = await _context.AuthorizationCategory.OrderBy(p => p.Ad).ToListAsync();
			KategoriListe.Insert(0, new AuthorizationCategory { ID = 0, Ad = "Tümü" });
			ViewBag.KategoriListe = new SelectList(KategoriListe, "ID", "Ad");

		}
		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.KategoriListe = new SelectList(await _context.AuthorizationCategory.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");

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

		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new YetkilendirmeVM
			{
				YetkilendirmeVMListe = await _service.VeriListele()
			};

			return View(model);
		}


		public async Task<IActionResult> Bayiler()
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new YetkilendirmeVM
			{
				YetkilendirmeVMListe = await _service.VeriListele()
			};

			return View(model);
		}

		[HttpPost]
		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index(YetkilendirmeVM modelv)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new YetkilendirmeVM
			{
				YetkilendirmeVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		[Authorize(Policy = "View")]
		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{
			await ViewBagPartialListeDoldur();
			var modelc = await _service.VeriGetir(VeriID);

			modelc.ControllerName = (string)RouteData.Values["controller"]; ;
			modelc.ModalTitle = "Yetkilendirme Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Policy = "Create")]
		[HttpPost]
		public IActionResult VeriEkle(YetkilendirmeVM model)
		{
			bool sonuc = _service.VeriEkle(model);

			PageToastr(sonuc);
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> VeriSil(YetkilendirmeVM model)
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
