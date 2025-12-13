 





using Azure.Core;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Ekomers.Models.FilterVM;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Drawing.Printing;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;


namespace Ekomers.Web.Controllers
{ 
	[Authorize(Policy = "AdminOrMalzeme")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class MalzemeController : BaseController
	{
		private readonly IMalzemeService _service;
		private readonly IStokService _stokService;
	 
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		
		private readonly ICacheService<MalzemeBirim> _birimCache;
		private readonly ICacheService<MalzemeTipi> _malzemeTipCache;
		private readonly ICacheService<DovizTur> _dovizTurCache;
		private readonly ICacheService<MalzemeGrup> _malzemeGrupCache;
		 
		public MalzemeController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IMalzemeService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			
			, ICacheService<MalzemeBirim> birimCache
			, ICacheService<MalzemeTipi> malzemeTipCache
			, ICacheService<DovizTur> dovizTurCache
			, ICacheService<MalzemeGrup> malzemeGrupCache
			 

			, IFirsatService firsatService
			, IStokService stokService
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			
			_birimCache = birimCache;
			_malzemeTipCache = malzemeTipCache;
			_dovizTurCache = dovizTurCache;
			_malzemeGrupCache = malzemeGrupCache;
			_stokService = stokService;
			
			 
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}

		private async Task ViewBagListeDoldur()
		{
			//var turler = await _durumCache.GetListeAsync(CacheKeys.MalzemeDurumAll);

			//var uiList = new List<MalzemeDurum>(turler.Count + 1);
			//uiList.Add(new MalzemeDurum { ID = 0, Ad = "Tümü" });
			//uiList.AddRange(turler);

			//ViewBag.MalzemeDurumListe = new SelectList(uiList, "ID", "Ad");
		}

		private async Task ViewBagPartialListeDoldur()
		{
			var durumlar = await _birimCache.GetListeAsync(CacheKeys.MalzemeBirimAll);
			ViewBag.MalzemeBirimListe = new SelectList(durumlar, "ID", "Ad");

			var tipler = await _malzemeTipCache.GetListeAsync(CacheKeys.MalzemeTipiAll);
			ViewBag.MalzemeTipiListe = new SelectList(tipler, "ID", "Ad");

			var dovizler = await _dovizTurCache.GetListeAsync(CacheKeys.MalzemeDovizAll);
			ViewBag.MalzemeDovizListe = new SelectList(dovizler, "ID", "Ad");

			//var gruplar = await _malzemeGrupCache.GetListeAsync(CacheKeys.MalzemeGrupAll);
			//ViewBag.MalzemeGrupListe = new SelectList(gruplar, "ID", "Ad");


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
			ViewBag.Modul = "CRM";
			//await ViewBagListeDoldur();
			var paged = await _service.VeriListeleAsync(page, pageSize, ct);

			var model = new MalzemelerVM
			{
				MalzemelerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};
			ViewBag.PageIndex = page;
			ViewBag.PageSize = pageSize;
			return View(model);
		}

		[HttpGet]
		public async Task<IActionResult> Arama()
		{
			ViewBag.Modul = "CRM";

			var model = new MalzemelerVM
			{
				MalzemeGrupListe = await _malzemeGrupCache.GetListeAsync(CacheKeys.MalzemeGrupAll),
				MalzemelerVMListe = new List<MalzemelerVM>()
			};		

			await ViewBagPartialListeDoldur();
			return View(model);
		}
		
		// POST /Arama  (PRG: Post -> Redirect -> Get)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Arama(MalzemelerVM modelv)
		{
			ViewBag.Modul = "CRM";
			//await ViewBagListeDoldur();


			modelv.MalzemeGrupListe = await _malzemeGrupCache.GetListeAsync(CacheKeys.MalzemeGrupAll);
			modelv.MalzemelerVMListe = await _service.VeriListele(modelv);
			modelv.tagifyGruplar = modelv.tagifyGruplar;

			await ViewBagPartialListeDoldur();
			return View(modelv);
		}
		public IActionResult Ara(string searchName)
				{


					// Güvenli ve performanslı arama (ToLower/EF Core vs. projenize göre uyarlayın)
					var query = _context.Malzeme
						.Where(x => (searchName == null || x.Ad.Contains(searchName)))
						.OrderBy(x => x.Ad);

					var items = query
						.Select(x => new { id = x.ID, ad = x.Ad })
						.ToList();



					return Ok(items);
				}
		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "",int pageIndex=0,int pageSize=0)
		{

			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();
			modelc.MalzemeGrupListe= await _malzemeGrupCache.GetListeAsync(CacheKeys.MalzemeGrupAll);
			var kategoriTree = _stokService.GetKategoriTree();

			kategoriTree = BuildFullPathForTree(kategoriTree);

			modelc.KategoriTree = kategoriTree;

			modelc.ControllerName = "Malzeme";
			modelc.ModalTitle = "Malzeme Bilgileri";
			modelc.PageIndex = pageIndex;
			modelc.PageSize = pageSize;
			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "", int pageIndex = 0, int pageSize = 0)
		{
			ViewBag.Modul = "CRM";
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();
			modelc.MalzemeGrupListe = await _malzemeGrupCache.GetListeAsync(CacheKeys.MalzemeGrupAll);
			var kategoriTree = _stokService.GetKategoriTree();

			kategoriTree = BuildFullPathForTree(kategoriTree);

			modelc.KategoriTree = kategoriTree;

			modelc.ControllerName = "Malzeme";
			modelc.ModalTitle = "Malzeme Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}
		private List<KategoriTreeItem> FlattenTree(List<KategoriTreeItem> nodes)
		{
			var list = new List<KategoriTreeItem>();

			foreach (var n in nodes)
			{
				list.Add(n);
				if (n.Children != null && n.Children.Any())
					list.AddRange(FlattenTree(n.Children));
			}

			return list;
		}
		private List<KategoriTreeItem> BuildFullPathForTree(List<KategoriTreeItem> tree)
		{
			var flatList = FlattenTree(tree);

			var dict = flatList.ToDictionary(x => x.ID, x => x);

			foreach (var item in flatList)
			{
				item.FullPath = BuildFullPath(item, dict);
			}

			return tree;
		}
		private string BuildFullPath(KategoriTreeItem item, Dictionary<int, KategoriTreeItem> dict)
		{
			var names = new List<string>();
			var current = item;

			while (current != null)
			{
				names.Add(current.Ad);

				if (current.ParentID == 0 || !dict.ContainsKey(current.ParentID))
					break;

				current = dict[current.ParentID];
			}

			names.Reverse();
			return string.Join(" >> ", names);
		}
		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(MalzemelerVM modelv)
		{
			bool sonuc = await _service.VeriEkleAsync(modelv);
			//var paged = await _service.VeriListeleAsync(1, 10, default);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				return RedirectToAction("Index",new {page=modelv.PageIndex,pageSize=modelv.PageSize});

				//var modelc = new MalzemelerVM
				//{
				//	MalzemelerVMListe = paged.Items.ToList(),
				//	PageIndex = paged.PageIndex,
				//	PageSize = paged.PageSize,
				//	TotalCount = paged.TotalCount
				//};

				//return PartialView("~/Views/Malzeme/_List.cshtml", modelc);
			}
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(MalzemelerVM model)
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
		public async Task<IActionResult> LogoMalzemeAktar()
		{
			bool sonuc = await _service.LogoMalzemeAktar();

			if (sonuc)
			{
				return Ok("Veri aktarma başarılı");
			}
			else
			{
				return BadRequest("Veri aktarılamadı.");
			}
		}


		[HttpPost]
		public async Task<IActionResult> FiyatDegistir(MalzemeFiyat model)
		{
			bool sonuc = await _service.FiyatDegistir(model);

			if (sonuc)
			{
				return Ok("Veri aktarma başarılı");
			}
			else
			{
				return BadRequest("Veri aktarılamadı.");
			}
		}



	}
}
