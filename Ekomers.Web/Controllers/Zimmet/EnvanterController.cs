

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
	public class EnvanterController : BaseController
	{
		private readonly IEnvanterService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICacheService<Sirketler> _sirketCache;
		private readonly ICacheService<EnvanterTur> _turCache;
		private readonly ICacheService<EnvanterDepartman> _departmanCache;
		private readonly ICacheService<EnvanterBolum> _bolumCache;

		 
		private readonly ISehirlerService _sehirlerService;
		private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ENVANTER");
		private readonly string _uploadFotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ENVANTER", "img");
		private readonly string _path2 = "ENVANTER";
		private string ModulAd = "ENVANTER";
		public EnvanterController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IEnvanterService service
			 
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory 
				, ICacheService<EnvanterTur> turCache
			, ICacheService<EnvanterDepartman> departmanCache
			, ICacheService<EnvanterBolum> bolumCache
		 , ICacheService<Sirketler> sirketCache
			, ISehirlerService sehirlerService
		
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sirketCache = sirketCache;
			_turCache = turCache;
			_sehirlerService = sehirlerService;
			_departmanCache = departmanCache;
			_bolumCache = bolumCache;
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
		}
		private async Task ViewBagListeDoldur()
		{
			ViewBag.SirketlerListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
			ViewBag.EnvanterTurListe = await _turCache.GetListeAsync(CacheKeys.EnvanterTurAll);
			ViewBag.DepartmanlarListe = await _departmanCache.GetListeAsync(CacheKeys.EnvanterDepartmanAll);
			ViewBag.BolumlerListe = await _bolumCache.GetListeAsync(CacheKeys.EnvanterBolumAll);	
		}  

		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new EnvanterVM
			{
				EnvanterVMListe	 = await _service.VeriListele()
			};

			return View(model);
		}


		public async Task<IActionResult> Bayiler()
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new EnvanterVM
			{
				EnvanterVMListe	 = await _service.VeriListele()
			};

			return View(model);
		}

		[HttpPost]
		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index(EnvanterVM modelv)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();

			modelv.EnvanterVMListe = await _service.VeriListele(modelv);
			 
			return View(modelv);
		}
		
		[Authorize(Policy = "View")]
		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{
			 
			var modelc = await _service.VeriGetir(VeriID);
			 
				await ViewBagPartialListeDoldur();
			 
			modelc.ControllerName = "Envanter";
			modelc.ModalTitle = "Envanter Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Policy = "Create")]
		[HttpPost]
		public IActionResult VeriEkle(EnvanterVM vm)
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
		public async Task<IActionResult> VeriSil(EnvanterVM vm)
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

		  
		public async Task<PartialViewResult> _FotoGetir(int DemirbasID = 0)
		{
			// ToplantiVM model = new ToplantiVM();



			var modelc = await _service.VeriGetir(DemirbasID);
			return PartialView(modelc);
		}

		[HttpPost]
		public async Task<IActionResult> FotoYukle(EnvanterVM vm)
		{
			if (vm.Dosya != null && vm.Dosya.Length > 0)
			{
				var dosya_id = Guid.NewGuid().ToString();
				var fileExtension = Path.GetExtension(vm.Dosya.FileName);
				var fileName = Path.GetFileName(vm.Dosya.FileName);
				var dosyaAdi = dosya_id + fileExtension;
				var filePath = Path.Combine(_uploadFotoPath, dosyaAdi);
				// var fileInfo = new FileInfo(fileName);

				if (!Directory.Exists(_uploadFotoPath))
				{
					Directory.CreateDirectory(_uploadFotoPath);
				}
				if (fileExtension == ".jpg" || fileExtension == ".jpeg" || fileExtension == ".png")
				{
					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await vm.Dosya.CopyToAsync(stream);
					}
					
					
				}



				vm.Fotograf = dosyaAdi;
				_service.FotoYukle(vm);

				//  return Json(modelc);
				return Ok("Kayıt işlemi başarılı");
			}
			return BadRequest("Veri aktarılamadı.");
			//  return BadRequest("Dosya yüklenemedi.");
		}


		public async Task<ActionResult> GetBolumler(int ParametreID = 0)
		{
			ViewBag.BolumlerListe = await _service.GetBolumler(ParametreID);
			return PartialView("_Bolumler");
		}
	}
}
