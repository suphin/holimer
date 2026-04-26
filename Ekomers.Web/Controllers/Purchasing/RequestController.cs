using DocumentFormat.OpenXml.Drawing.Charts;
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
using System.Security.Claims;


namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrCrm")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class RequestController : BaseController
	{
		private readonly IRequestService _service;
	 
		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		 
		private readonly ICacheService<RequestDurum> _durumCache;
		private readonly ICacheService<RequestTur> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private readonly ICacheService<Sirketler> _sirketCache;
		private string ModulAd = "PURCHASING";
		public RequestController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IRequestService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			 
			, ICacheService<RequestDurum> durumCache
			, ICacheService<RequestTur> turCache
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
			ViewBag.RequestTurListe = await _turCache.GetListeAsync(CacheKeys.RequestTurAll);
			ViewBag.RequestDurumListe = await _durumCache.GetListeAsync(CacheKeys.RequestDurumAll);
			ViewBag.SirketListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
		}

		private async Task ViewBagPartialListeDoldur()
		{ 

			ViewBag.RequestTurListe = await _turCache.GetListeAsync(CacheKeys.RequestTurAll);
			ViewBag.RequestDurumListe = await _durumCache.GetListeAsync(CacheKeys.RequestDurumAll);
			ViewBag.SirketListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
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
			var paged = await _service.TalepListeleAsync(page, pageSize, ct,(int)EnumRequestDurum.Taslak);

			var model = new RequestVM
			{
				RequestVMListe = paged.Items.ToList(),
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
			var paged = await _service.VeriListeleOnayAsync(page, pageSize, ct);

			var model = new RequestVM
			{
				RequestVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Index(RequestVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new RequestVM
			{
				RequestVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);
			
				await ViewBagPartialListeDoldur();

			if (view== "_VeriGoruntule" || view== "_VeriSilinecek")
			{
				modelc.RequestUrunler = await _service.RequestUrunlerGetir(VeriID);
			}

			modelc.ControllerName = "Request";
			modelc.ModalTitle = "Talep Bilgileri";
			modelc.RequestDate = DateTime.Now;
			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();



			modelc.ControllerName = "Request";
			modelc.ModalTitle = "Talep Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(RequestVM model,string actionType)
		{
			if(actionType==null)
			{
				model.DurumID = (int)EnumRequestDurum.Taslak;
			}
			

			if (actionType == "taslak")
			{
				model.DurumID= (int)EnumRequestDurum.Taslak;
			}


			model.TarihSaat = DateTime.Now;


			if (actionType == "onay")
			{
				var urunler = await _service.RequestUrunlerGetir(model.ID);
				if (urunler.Count > 0)
				{
					model.DurumID = (int)EnumRequestDurum.OnayBekliyor;
				}
				else
				{
					model.DurumID = (int)EnumRequestDurum.Taslak;
					return BadRequest("Kaydetme başarısız! Ürün ekleyiniz.");
				}

			}

			int sonuc = await _service.VeriEkleReturnIDAsync(model);




			if (sonuc > 0)
			{
				return Redirect("/Request/VeriGoruntule2?view=_VeriGetir2&VeriID=" + sonuc);
			} 
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(RequestVM model)
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

	 
		public async Task<PartialViewResult> UrunEkle(int RequestID)
		{

			var talep=await _service.VeriGetir(RequestID);
			var modelvm = new RequestUrunlerVM()
			{
				CreateDate = DateTime.Now,
				RequestID=RequestID,
				RequestUrunlerVMListe = await _service.RequestUrunlerGetir(RequestID),
				RequestDurumID=(int)talep.DurumID,
				 RequestTurID=(int)talep.TurID

			};
			   
			//ViewBag.Kurlar= await _tcmbService.DovizKuruGetir();

			if (modelvm.RequestUrunlerVMListe == null)
			{
				modelvm.RequestUrunlerVMListe = new List<RequestUrunlerVM>();
			}


			return PartialView("_UrunEkle",modelvm);
		}
		public async Task<PartialViewResult> UrunCikar(int urunId,int RequestID)
		{
			var sonuc = await _service.RequestUrunCikar(urunId);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = await _service.RequestUrunlerGetir(RequestID),
				 
			};

			//ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
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
		public async Task<PartialViewResult> _RequestUrunEkle(RequestUrunlerVM models)
		{
			var urun = await _malzemeService.VeriGetir(models.UrunID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			var RequestUrunekle = new RequestUrunlerVM
			{
				UrunAd = urun.Ad,
				UrunKod = urun.Kod,
				UrunID = models.UrunID,
				Miktar = models.Miktar, 
				BirimID = urun.BirimID,
				BirimAd = urun.BirimAd,

				Aciklama = models.Aciklama,
				RequestID=models.RequestID
			};

			await _service.RequestUrunEkle(RequestUrunekle);
			//ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = await _service.RequestUrunlerGetir(models.RequestID),
				 
			};

			return PartialView("_UrunEklenen", model);
		}
		public async Task<IActionResult> Print(int id)
		{
			var modelc = await _service.VeriGetir(id);
			 
				modelc.RequestUrunler = await _service.RequestUrunlerGetir(id);
			 
			 
			return View("Print", modelc); // Print.cshtml
		}
		public async Task<PartialViewResult> TalepOnayla(int VeriID = 0, int pageIndex = 0, int pageSize = 0)
		{

			var modelc = await _service.VeriGetir(VeriID);

			
				modelc.RequestUrunler = await _service.RequestUrunlerGetir(VeriID);
			

			modelc.ControllerName = "Request";
			modelc.ModalTitle = "Talep Bilgileri";
			modelc.RequestDate = DateTime.Now;
			modelc.UserID = _userId;
			return PartialView(modelc);
		}

		public async Task<IActionResult> TalepUrunOnay(int requestId,int kayitId, double miktar)
		{
			var urun = await _service.RequestUrunGetir(kayitId);

			 urun.MiktarSon= miktar;
			urun.OnayliMi = true;
			urun.OfferDurumID=(int)EnumOfferDurum.TeklifAsamasinda;

			await _service.RequestUrunDuzenle(urun);

			int urunDurum = await  _service.RequestUrunDurum(requestId);

			if (urunDurum==0)
			{
				var req= await _service.VeriGetir(requestId);
				req.DurumID=(int)EnumRequestDurum.Onaylandi;
				await _service.VeriEkleAsync(req);	
				return BadRequest("Onaylanacak ürün kalmadı.");
			}
			var model = new RequestVM
			{
				RequestUrunler = await _service.RequestUrunlerGetir(requestId),

			};

			return PartialView("_OnayUrunEklenen", model);
		}
		public async Task<IActionResult> TalepUrunIptal(int requestId, int kayitId)
		{
			var urun = await _service.RequestUrunGetir(kayitId);
 
			urun.OnayliMi = false;
			urun.IsActive = false;
			urun.IsDelete = true; 
			await _service.RequestUrunDuzenle(urun);

			int urunDurum = await _service.RequestUrunDurum(requestId);

			if (urunDurum == 0)
			{
				var req = await _service.VeriGetir(requestId);
				req.DurumID = (int)EnumRequestDurum.Onaylandi;
				await _service.VeriEkleAsync(req);
				return BadRequest("Onaylanacak ürün kalmadı.");
			}
			var model = new RequestVM
			{
				RequestUrunler = await _service.RequestUrunlerGetir(requestId),

			};

			return PartialView("_OnayUrunEklenen", model);
		}
		public async Task<IActionResult> Arsiv(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{

			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var paged = await _service.TalepListeleAsync(page, pageSize, ct);

			var model = new RequestVM
			{
				RequestVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Arsiv(RequestVM requestVm)
		{

			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var paged = await _service.TalepListeleAsync(requestVm);

			var model = new RequestVM
			{
				RequestVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}
	}
}
