using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Office2019.Drawing.Model3D;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services;
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
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrPurchasing")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class OrderController : BaseController
	{
		private readonly IOfferService _offerService;
		private readonly IOrderService _orderService;
		private readonly IRequestService _requestService;

		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private  string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;

		private readonly ICacheService<OfferDurum> _durumCache;
		private readonly ICacheService<OfferTur> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private readonly ICacheService<Sirketler> _sirketCache;
		private readonly string ModulAd = "PURCHASING";
		public OrderController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IOfferService offerService 
			,IOrderService orderService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			 , IRequestService requestService
			, ICacheService<OfferDurum> durumCache
			, ICacheService<OfferTur> turCache
			, ICacheService<Kullanici> kullaniciCache
		 , ICacheService<Sirketler> sirketCache
			, IStokService stokService
			, IMalzemeService malzemeService
			, ITcmbService tcmbService
			) : base(userManager, roleManager)
		{
			_offerService = offerService;
			_orderService = orderService;
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


		public async Task<IActionResult> Index(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;

			var paged = await _offerService.VeriListeleAsync(page, pageSize, ct, (int)EnumOrderDurum.SiparisAsamasinda);
			//var paged = await _orderService.VeriListeleAsync(page, pageSize, ct);

			var model = new OfferVM
			{
				OfferVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		public async Task<IActionResult> FirmaGrup(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;

			var paged = await _orderService.VeriListeleFirmaGrup(page, pageSize, ct);
			 

			var model = new SiparisFirmaGrupVM
			{
				SiparisFirmaGrupVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}


		public IActionResult Detay(int firmaId)
		{
			ViewBag.Modul = ModulAd;
			var model = _orderService
				.SiparisFirmaGrupListe()
				.Where(x => x.FirmaID == firmaId && x.IsSelected==true && x.DurumID == (int)EnumOrderDurum.SiparisAsamasinda)
				.ToList();

			if (!model.Any())
				return NotFound();

			var vm = new SiparisDetayVM
			{
				FirmaID = firmaId,
				FirmaAd = model.First().FirmaAd,
				Firma = model.First().Firma,

				Sirket = model.First().Sirket,

				Urunler = model,

				AraToplam = model.Sum(x => x.Miktar * x.Fiyat),

				KdvToplam = model.Sum(x =>
					(x.Miktar * x.Fiyat) * x.UrunKdv / 100
				),

				GenelToplam = model.Sum(x =>
					(x.Miktar * x.Fiyat)
					+
					((x.Miktar * x.Fiyat) * x.UrunKdv / 100)
				)
			};

			return View(vm);
		}

		public async Task<IActionResult> OnayDetail(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;

			var offer = new OfferVM
			{
				RequestUrunID = Id,
				IsSelected=true,

			};
			model.OfferVMListe = await _offerService.VeriListele(offer);
			return View(model);
		}


		public async Task<IActionResult> SiparisOnay(int OfferID)
		{
			//bool sonuc =  await _orderService.SiparisOnay(OfferID);
			//if (sonuc)
			//{
			//	return Ok("Sipariş onaylandı.");
			//}
			//else
			//{
			//	return BadRequest("İşlem sırasında hata oluştu.");
			//}

			var model = await _requestService.RequestUrunGetir(OfferID);

			model.UserID = _userId;

			var offer = new OfferVM
			{
				RequestUrunID = OfferID,
				IsSelected = true,

			}; 


			var models = new OrderVM();
			models.Teklifler= await _offerService.VeriListele(offer);
			models.TeslimTarihi = DateTime.Now;

			ViewBag.tagifyEpostalar =await  _context.Users.ToListAsync();
			return PartialView("_siparisOnayForm", models);

		}

		public async Task<IActionResult> SiparisIptal(int OfferID)
		{
			  var offer = await _offerService.VeriGetir(OfferID); 

			offer.DurumID = (int)EnumOrderDurum.SiparisIptal;
			bool sonuc = await _offerService.VeriEkleAsync(offer);


			if (sonuc)
			{
				return Ok("Sipariş iptal edildi.");
			}
			else
			{
				return BadRequest("İşlem sırasında hata oluştu.");
			}


		}


		public async Task<IActionResult> SiparisTopluOnay(int FirmaID)
		{
			bool sonuc =  await _orderService.SiparisTopluOnay(FirmaID);
			if (sonuc)
			{
				return Ok("Sipariş onaylandı.");
			}
			else
			{
				return BadRequest("İşlem sırasında hata oluştu.");
			}
		}

		public async Task<IActionResult> Arsiv(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{
			ViewBag.Modul = ModulAd;

			var paged = await _offerService.VeriListeleAsync(page, pageSize, ct, (int)EnumOrderDurum.SiparisOnaylandi);
			//var paged = await _orderService.VeriListeleAsync(page, pageSize, ct);

			var model = new OfferVM
			{
				OfferVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}
	}
}
