

using DocumentFormat.OpenXml.Wordprocessing;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Ekomers.Web.Controllers
{
	//[Authorize(Roles = "Admin")]
	[Authorize(Policy = "AdminOrEnvanter")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class ZimmetController : BaseController
	{
		private readonly IZimmetService _service;
		private readonly IEnvanterService _envanterService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICacheService<Sirketler> _sirketCache;
		private readonly ICacheService<EnvanterTur> _turCache;
		private readonly ICacheService<EnvanterDepartman> _departmanCache;
		private readonly ICacheService<EnvanterBolum> _bolumCache;
		private readonly ICacheService<Personel> _personelCache;

		  
		private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ENVANTER");
		private readonly string _uploadFotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ENVANTER", "img");
		private readonly string _path2 = "ENVANTER";
		private string ModulAd = "ENVANTER";
		public ZimmetController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IZimmetService service
			 
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory 
				, ICacheService<EnvanterTur> turCache
			, ICacheService<EnvanterDepartman> departmanCache
			, ICacheService<EnvanterBolum> bolumCache
		 , ICacheService<Sirketler> sirketCache
		 ,IEnvanterService envanterService
		, ICacheService<Personel> personelCache
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sirketCache = sirketCache;
			_turCache = turCache;
			_envanterService = envanterService;
			_departmanCache = departmanCache;
			_bolumCache = bolumCache;
			_personelCache = personelCache;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		} 
		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.SirketlerListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
			ViewBag.EnvanterTurListe = await _turCache.GetListeAsync(CacheKeys.EnvanterTurAll);
			ViewBag.DepartmanlarListe = await _departmanCache.GetListeAsync(CacheKeys.EnvanterDepartmanAll);
			ViewBag.BolumlerListe = await _bolumCache.GetListeAsync(CacheKeys.EnvanterBolumAll);
			ViewBag.PersonellerListe = await _personelCache.GetListeAsync(CacheKeys.PersonelAll);
		}
		private async Task ViewBagListeDoldur()
		{
			ViewBag.SirketlerListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
			ViewBag.EnvanterTurListe = await _turCache.GetListeAsync(CacheKeys.EnvanterTurAll);
			ViewBag.DepartmanlarListe = await _departmanCache.GetListeAsync(CacheKeys.EnvanterDepartmanAll);
			ViewBag.BolumlerListe = await _bolumCache.GetListeAsync(CacheKeys.EnvanterBolumAll);	
			ViewBag.PersonellerListe = await _personelCache.GetListeAsync(CacheKeys.PersonelAll);
		}  

		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur(); 
			var paged = await _service.VeriListeleAsync(page, pageSize, ct);

			var model = new ZimmetVM
			{
				ZimmetVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};
			ViewBag.PageIndex = page;
			ViewBag.PageSize = pageSize;
			return View(model); 
		}
	

		[HttpPost]
		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index(ZimmetVM modelv)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();

			modelv.ZimmetVMListe = await _service.VeriListele(modelv);
			 
			return View(modelv);
		}
		
		[Authorize(Policy = "View")]
		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			 
				await ViewBagPartialListeDoldur();
			 
			modelc.ControllerName = "Zimmet";
			modelc.ModalTitle = "Zimmet Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Policy = "Create")]
		[HttpPost]
		public IActionResult VeriEkle(ZimmetVM vm)
		{
			bool sonuc = _service.VeriEkle(vm);

			//PageToastr(sonuc);
			return RedirectToAction("Index");
			//if (sonuc)
			//{
			//	return Ok("Kayıt işlemi başarılı");
			//}
			//else
			//{
			//	return BadRequest("Kaydetme başarısız!");
			//}
		}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> VeriSil(ZimmetVM vm)
		{
			bool sonuc = await _service.VeriSil(vm.ID);
			if (sonuc)
			{
				return Ok(vm.ID);
			}
			else
			{
				return BadRequest("Veri silinemedi.");
			}
		}

		public async Task<IActionResult> EnvanterListesi()
		{
			var model = new ZimmetVM
			{
				EnvanterVMListe = await _envanterService.VeriListele(),
				ControllerName = "Zimmet",
				ModalTitle = "Zimmet Bilgileri"
			};

			return PartialView("_EnvanterListesi",model);
		}
	}
}
