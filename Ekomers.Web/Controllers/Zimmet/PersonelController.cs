


using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrEnvanter")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class PersonelController : BaseController
	{
		private readonly IPersonelService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ISehirlerService _sehirlerService;
		private readonly ICacheService<MusteriTip> _musteriTipCache;
		private readonly ICacheService<Departman> _departmanCache;
		private readonly ICacheService<DepartmanBirim> _departmanBirimCache;
		private readonly ICacheService<Sirketler> _sirketlerCache;
		private readonly ICacheService<PersonelDurum> _personelDurumCache;
		private readonly ICacheService<PersonelGorev> _personelGorevCache;

		private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ENVANTER");
		private readonly string _uploadFotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "ENVANTER","PERSONEL", "img");
		private readonly string _path2 = "ENVANTER";
		private string ModulAd = "ENVANTER";
		public PersonelController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IPersonelService service
		 
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ISehirlerService sehirlerService
			, ICacheService<Departman> departmanCache
			, ICacheService<DepartmanBirim> departmanBirimCache
			, ICacheService<Sirketler> sirketlerCache	
			,ICacheService<PersonelDurum> personelDurumCache
			, ICacheService<PersonelGorev> personelGorevCache
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerService = sehirlerService;
			_departmanCache = departmanCache;
			_departmanBirimCache = departmanBirimCache;
			_sirketlerCache = sirketlerCache;
			_personelDurumCache = personelDurumCache;
			_personelGorevCache = personelGorevCache;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}

		private async Task ViewBagListeDoldur()
		{
 
			ViewBag.DepartmanListe = await _departmanCache.GetListeAsync(CacheKeys.DepartmanAll);
			ViewBag.BolumListe = await _departmanBirimCache.GetListeAsync(CacheKeys.DepartmanBirimAll);
			ViewBag.SirketListe = await _sirketlerCache.GetListeAsync(CacheKeys.SirketlerAll);
			ViewBag.PersonelDurumListe = await _personelDurumCache.GetListeAsync(CacheKeys.PersonelDurumAll);
			ViewBag.PersonelGorevListe = await _personelGorevCache.GetListeAsync(CacheKeys.PersonelGorevAll);
		}
		 
		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.DepartmanListe = await _departmanCache.GetListeAsync(CacheKeys.DepartmanAll);
			ViewBag.BolumListe = await _departmanBirimCache.GetListeAsync(CacheKeys.DepartmanBirimAll);
			ViewBag.SirketListe = await _sirketlerCache.GetListeAsync(CacheKeys.SirketlerAll);
			ViewBag.PersonelDurumListe = await _personelDurumCache.GetListeAsync(CacheKeys.PersonelDurumAll);
			ViewBag.PersonelGorevListe = await _personelGorevCache.GetListeAsync(CacheKeys.PersonelGorevAll);
		}
	 
		 

		public async Task<IActionResult> Index(int page = 1, int pageSize = 100, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var paged = await _service.VeriListeleAsync(new PersonelVM { DurumID = 1 }, page, pageSize, ct);

			var model = new PersonelVM
			{
				PersonelVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};
			ViewBag.PageIndex = page;
			ViewBag.PageSize = pageSize;
			return View(model);
		}
		public async Task<IActionResult> Pasif(int page = 1, int pageSize = 100, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();


			var paged = await _service.VeriListeleAsync(new PersonelVM { DurumID = 2 }, page, pageSize, ct);



			var model = new PersonelVM
			{
				PersonelVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};
			ViewBag.PageIndex = page;
			ViewBag.PageSize = pageSize;
			return View("Index",model);
		}

		[HttpPost]
		public async Task<IActionResult> Index(PersonelVM modelv)
		{
			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var model = new PersonelVM
			{
				PersonelVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			
			var modelc = await _service.VeriGetir(VeriID);
			 
				await ViewBagPartialListeDoldur();

			modelc.PageIndex = pageIndex;
			modelc.PageSize = pageSize;

			modelc.ControllerName = "Personel";
			modelc.ModalTitle = "Personel Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
	
		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(PersonelVM modelv)
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
			 var paged = await _service.VeriListeleAsync(new PersonelVM { DurumID = 1 },modelv.PageIndex, modelv.PageSize, default);

			 	var modelc = new PersonelVM
				{
					PersonelVMListe = paged.Items.ToList(),
					PageIndex = paged.PageIndex,
					PageSize = paged.PageSize,
					TotalCount = paged.TotalCount
				};

			return RedirectToAction("Index", new { page = modelv.PageIndex, pageSize = modelv.PageSize });

		}


		
		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(PersonelVM model)
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

		public async Task<IActionResult> BordroPersonelAktar()
		{
			bool sonuc = await _service.BordroPersonelAktar();

			if (sonuc)
			{
				return Ok("Veri aktarma başarılı");
			}
			else
			{
				return BadRequest("Veri aktarılamadı.");
			}
		}


		public async Task<PartialViewResult> _FotoGetir(int PersonelID = 0)
		{
			
			var modelc = await _service.VeriGetir(PersonelID);
			return PartialView(modelc);
		}

		[HttpPost]
		public async Task<IActionResult> FotoYukle(PersonelVM vm)
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

	}
}
