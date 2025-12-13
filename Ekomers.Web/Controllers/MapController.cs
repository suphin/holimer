
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using System.Security.Claims;

using Ekomers.Data;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
namespace Ekomers.Web.Controllers
{
	[Authorize]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class MapController : BaseController
	{
		private readonly IMapService _mapService;
		
		private readonly IEczaneService _eczaneService;
		private readonly IMusterilerService _musteriService;
		private readonly ApplicationDbContext _context;
		private string _userId;
		public MapController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager, IMapService mapService
			

			, ApplicationDbContext context, IEczaneService eczaneService, IMusterilerService musteriService) : base(userManager, roleManager)
		{
			_mapService = mapService;
		
			_context = context;
			_eczaneService = eczaneService;
			_musteriService = musteriService;
		}
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		}

		public async Task<IActionResult> KoordinatGetir(int KayitID = 0, int ModulID = 0)
		{
			var model = new MapVM
			{
				MapVMListe = await _mapService.KoordinatGetir(KayitID, ModulID),
				KayitID = KayitID,
				ModulID = ModulID
			};
			return PartialView("_KoordinatGetir", model);
		}

		[HttpPost]
		public async Task<JsonResult> SaveGeoJson(int KayitID = 0, int ModulID = 0)
		{
			try
			{
				// Gelen veriyi okuyun
				using (var reader = new StreamReader(Request.Body))
				{
					var requestBody = await reader.ReadToEndAsync();

					var geoJson = _context.Geojson.Where(p => p.KayitID == KayitID && p.ModulID == ModulID).FirstOrDefault();
					if (geoJson == null)
					{
						_context.Geojson.Add(new Geojson()
						{
							Veri = requestBody,
							CreateDate = DateTime.Now,
							CreateUserID = _userId,
							ModulID = ModulID,
							KayitID = KayitID,
							IsActive = true,
							IsDelete = false
						});
					}
					else
					{
						geoJson.Veri = requestBody;
						geoJson.UpdateDate = DateTime.Now;
						geoJson.UpdateUserID = _userId;
						_context.Update(geoJson);
					}

					_context.SaveChanges();


					return Json(new { success = true, message = "GeoJSON başarıyla kaydedildi." });
				}
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}


		public async Task<JsonResult> GetGeoJson(int KayitID = 0, int ModulID = 0)
		{
			try
			{
				var geoJson = _context.Geojson.Where(p => p.KayitID == KayitID && p.ModulID == ModulID).FirstOrDefault();



				if (geoJson != null)
				{
					return Json(new { success = true, message = "GeoJSON başarıyla getirildi.", Data = geoJson });
				}
				else
				{
					return Json(new { success = false, message = "GeoJSON bulunamadı." });
				}


			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}

		public async Task<JsonResult> GetGeoJsonAll(int ModulID = 0)
		{
			try
			{

				var geoJson = _context.Geojson.Where(p => p.ModulID == ModulID).ToList();
			


				var result = (from g in geoJson 
							  select new
							  {
								  GeoJson = g,
								  //KentTarihi = kt,
								  //KentTarihiRota = ktr
							  }).ToList();

				if (geoJson != null)
				{
					return Json(new { success = true, message = "GeoJSON başarıyla getirildi.", Data = geoJson });
				}
				else
				{
					return Json(new { success = false, message = "GeoJSON bulunamadı." });
				}


			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}
		public async Task<JsonResult> GetImageAll(int ModulID = 0)
		{
			try
			{

				var image = _context.Dosya.Where(p => p.IsActive == true && p.IsDelete == false && p.ModulID == ModulID && p.Latitude != null && p.Longitude != null && p.Is360 == true).ToList();
				


				var result = (from g in image
							  
							  select new
							  {
								  Dosya = g,
								  //KentTarihi = kt,
								  //KentTarihiRota = ktr
							  }).ToList();


				if (image != null)
				{
					return Json(new { success = true, message = "Resimler başarıyla getirildi.", Data = image });
				}
				else
				{
					return Json(new { success = false, message = "Resimler bulunamadı." });
				}


			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}
		public async Task<JsonResult> GetImage(int KayitID = 0, int ModulID = 0)
		{
			try
			{

				var image = _context.Dosya.Where(p => p.IsActive == true && p.IsDelete == false && p.ModulID == ModulID && p.KayitID == KayitID && p.Latitude != null && p.Longitude != null).ToList();



				if (image != null)
				{
					return Json(new { success = true, message = "Resimler başarıyla getirildi.", Data = image });
				}
				else
				{
					return Json(new { success = false, message = "Resimler bulunamadı." });
				}


			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}

	 
		public async Task<IActionResult> VeriGoruntule(int KayitID = 0, int ModulID = 0)
		{
			ViewBag.KayitID = KayitID;
			ViewBag.ModulID = ModulID;

			ModulEnum modul = (ModulEnum)ModulID;

			ViewBag.DosyaYolu = modul.ToString();
			dynamic model = null;

			switch (ModulID)
			{
				 
				case (int)ModulEnum.Eczane:
					model = await _eczaneService.VeriGetir(KayitID);
					break;
				case (int)ModulEnum.Musteriler:
					model = await _musteriService.VeriGetir(KayitID);
					break;
				default:
					return NotFound("Modül tanımlı değil.");
			}

			if (model == null)
			{
				return NotFound("Veri bulunamadı.");
			}

			return View("~/Views/Shared/DinamikGorunum.cshtml", model);
		}

		public IActionResult Index()
		{
			var model = new CbsVM
			{
				//ItfaiyeTurListe = _context.ItfaiyeTur.Where(p => p.IsActive == true && p.IsDelete == false).ToList(),
				//ItfaiyeDenetimTuruListe = _context.ItfaiyeDenetimTuru.Where(p => p.IsActive == true && p.IsDelete == false).ToList(),
			};

			return View(model);
		}
		public async Task<JsonResult> HaritaGetir(int ModulID = 0)
		{
			try
			{

				var geoJson = _context.Geojson.Where(p => p.ModulID == ModulID).ToList();
				


				var result = (from g in geoJson 
							  select new
							  {
								  GeoJson = g,
								  //KentTarihi = kt,
								  //KentTarihiRota = ktr
							  }).ToList();

				if (geoJson != null)
				{
					return Json(new { success = true, message = "GeoJSON başarıyla getirildi.", Data = geoJson });
				}
				else
				{
					return Json(new { success = false, message = "GeoJSON bulunamadı." });
				}


			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}
	}
}
