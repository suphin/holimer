

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
using Microsoft.AspNetCore.Http;
using System.Text.Json;
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
		private readonly IPersonelService _personelService;
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
			, IPersonelService personelService
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
			_personelService = personelService;
			 
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
			Expression<Func<Personel, bool>> filter = a => a.DurumID == 1;
			ViewBag.PersonellerListe = await _personelCache.GetListeAsync(CacheKeys.PersonelAll,filter);
		}
		private async Task ViewBagListeDoldur()
		{
			ViewBag.SirketlerListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
			ViewBag.EnvanterTurListe = await _turCache.GetListeAsync(CacheKeys.EnvanterTurAll);
			ViewBag.DepartmanlarListe = await _departmanCache.GetListeAsync(CacheKeys.EnvanterDepartmanAll);
			ViewBag.BolumlerListe = await _bolumCache.GetListeAsync(CacheKeys.EnvanterBolumAll);
			Expression<Func<Personel, bool>> filter = a => a.DurumID == 1;
			ViewBag.PersonellerListe = await _personelCache.GetListeAsync(CacheKeys.PersonelAll, filter);
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
		public async Task<IActionResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
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

		public async Task<IActionResult> EnvanterListesi(int personelID)
		{
			var model = new ZimmetVM
			{
				EnvanterVMListe = await _envanterService.VeriListeleZimmet(personelID),
				ControllerName = "Zimmet",
				ModalTitle = "Envanter Bilgileri"
			};

			return PartialView("_EnvanterListesi",model);
		}
		public async Task<IActionResult> ZimmetListeyeEkle(int envanterID)
		{
			// Session'daki mevcut liste
			var liste = HttpContext.Session
				.GetObject<List<GeciciZimmetItem>>("ZimmetListe")
				?? new List<GeciciZimmetItem>();

			// Aynı kayıt tekrar eklenmesin
			if (!liste.Any(x => x.EnvanterID == envanterID))
			{
				var envanter = await _envanterService.VeriGetir(envanterID);

				liste.Add(new GeciciZimmetItem
				{
					EnvanterID = envanter.ID,
					EnvanterAdi = envanter.Ad??string.Empty,
					Yerkodu = envanter.DepartmanKod +"_"+ envanter.BolumKod + "_" + envanter.TurKod + "_" + envanter.Numara,
					Marka = envanter.Marka ?? string.Empty,
					Model = envanter.Model ?? string.Empty,
					Serino = envanter.SeriNo ?? string.Empty
				});
			}

			// Session'a geri yaz
			HttpContext.Session.SetObject("ZimmetListe", liste);

			return PartialView("_ZimmetEkliListe", liste);
		}



		public async Task<PartialViewResult> ZimmetEkle(int envanterID)
		{
			var model = new ZimmetVM
			{
				Envanter = await _envanterService.VeriGetir(envanterID),
				ControllerName = "Zimmet",
				ModalTitle = "Zimmet Bilgileri",
				UserID = _userId
			};
			 

			await ViewBagPartialListeDoldur();

			
			return PartialView("_zimmetEkle", model);
		}

		[HttpPost]
		public async Task<IActionResult> ZimmetEkle(ZimmetVM vM)
		{
			var sonuc = await _service.VeriEkleAsync(vM);
			if (sonuc)
			{
				return Ok("Kaydetmet Başarılı");
			}
			else
			{
				return BadRequest("Kaydetme başarısız oldu.");
			}			
		}

		public async Task<IActionResult> Print(int envanterID)
		{
			var Zimmet = await _service.ZimmetGetir(envanterID);

			Zimmet.Envanter = await _envanterService.VeriGetir(envanterID);
			Zimmet.ControllerName = "Zimmet";
			Zimmet.ModalTitle = "Zimmet Bilgileri";
			Zimmet.UserID = _userId;
			Zimmet.PersonelVM = await _personelService.VeriGetir(Zimmet.PersonelID);
			 
			return PartialView("Print", Zimmet);
		}

		public async Task<PartialViewResult> ZimmetGoster(int envanterID)
		{
			var Zimmet = await _service.ZimmetGetir(envanterID);

			Zimmet.Envanter = await _envanterService.VeriGetir(envanterID);
				Zimmet.ControllerName = "Zimmet";
				Zimmet.ModalTitle = "Zimmet Bilgileri";
				Zimmet.UserID = _userId;
			 


			await ViewBagPartialListeDoldur();


			return PartialView("_zimmetGoster", Zimmet);
		}

	}

	public class GeciciZimmetItem
	{
		public int EnvanterID { get; set; }
		public string EnvanterAdi { get; set; }
		public string Yerkodu { get; set; }
		public string Marka { get; set; }
		public string Model { get; set; }
		public string Serino { get; set; }
	}
	
	public static class SessionExtensions
	{
		public static void SetObject<T>(this ISession session, string key, T value)
		{
			session.SetString(key, JsonSerializer.Serialize(value));
		}

		public static T GetObject<T>(this ISession session, string key)
		{
			var value = session.GetString(key);

			return value == null
				? default
				: JsonSerializer.Deserialize<T>(value);
		}
	}
}
