using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
 
 
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using static Ekomers.Data.Services.StokService;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Humanizer;
using System.Security.AccessControl;

namespace Ekomers.Web.Controllers
{
	//[Authorize(Roles = "Stok,Admin")]
	[Authorize(Policy = "AdminOrStok")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class StokController : BaseController
	{
		private readonly IStokService _stokService;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IFileService _fileService; 
		private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "StokUpload");
		private readonly string _uploadFotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "StokUpload", "malzeme");
		private readonly string _path2 = "StokUpload";
        private readonly ICacheService<MalzemeBirim> _malzemeBirimCache;
        private readonly ICacheService<MalzemeTipi> _malzemeTipiCache;
        private readonly ICacheService<DovizTur> _dovizTurCache;
        private readonly ICacheService<MalzemeHareketTur> _malzemeHareketTurCache;
        public StokController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			IStokService stokService, IWebHostEnvironment hostingEnvironment, IFileService fileService 
			, ICacheService<MalzemeBirim> malzemeBirimCache
			, ICacheService<MalzemeTipi> malzemeTipiCache
			, ICacheService<DovizTur> dovizTurCache
			, ICacheService<MalzemeHareketTur> malzemeHareketTurCache
            ) : base(userManager, roleManager)
		{
			_stokService = stokService;
			_hostingEnvironment = hostingEnvironment;
			_fileService = fileService;
			_malzemeBirimCache = malzemeBirimCache;
			_malzemeTipiCache = malzemeTipiCache;
			_dovizTurCache = dovizTurCache;
			_malzemeHareketTurCache = malzemeHareketTurCache;
        }

		async Task<MalzemelerVM> VMModel()
		{
			//ToplantiVM model = new ToplantiVM();
			var model = await _stokService.VeriDoldur(
				//nameof(MalzemelerVM.MalzemeGrupListe),
				nameof(MalzemelerVM.MalzemeBirimListe),
				nameof(MalzemelerVM.MalzemeTipiListe),
				nameof(MalzemelerVM.DovizTurListe)
				);
			return model;
		}
        private async Task ViewBagListeDoldur()
        {
			ViewBag.MalzemeBirimListe = await _malzemeBirimCache.GetListeAsync(CacheKeys.MalzemeBirimAll);
			ViewBag.MalzemeTipiListe = await _malzemeTipiCache.GetListeAsync(CacheKeys.MalzemeTipiAll);
			ViewBag.DovizTurListe = await _dovizTurCache.GetListeAsync(CacheKeys.DovizTurAll);
            ViewBag.HareketListe = await _malzemeHareketTurCache.GetListeAsync(CacheKeys.MalzemeHareketTurAll);
        }
        private async Task ViewBagPartialListeDoldur()
		{
            ViewBag.MalzemeBirimListe = await _malzemeBirimCache.GetListeAsync(CacheKeys.MalzemeBirimAll);
            ViewBag.MalzemeTipiListe = await _malzemeTipiCache.GetListeAsync(CacheKeys.MalzemeTipiAll);
            ViewBag.DovizTurListe = await _dovizTurCache.GetListeAsync(CacheKeys.DovizTurAll);
            ViewBag.HareketListe = await _malzemeHareketTurCache.GetListeAsync(CacheKeys.MalzemeHareketTurAll);
        }


        private async Task SelectListFill(MalzemelerVM model)
		{
			model = await VMModel();

			//var MalzemeGrupListe = model.MalzemeGrupListe;
			//MalzemeGrupListe?.Insert(0, new MalzemeGrup { ID = 0, Ad = "Tümü" });
			//ViewBag.MalzemeGrupListe = new SelectList(MalzemeGrupListe, "ID", "Ad");

			var MalzemeBirimListe = model.MalzemeBirimListe;
			MalzemeBirimListe?.Insert(0, new MalzemeBirim { ID = 0, Ad = "Tümü" });
			ViewBag.MalzemeBirimListe = new SelectList(MalzemeBirimListe, "ID", "Ad");

			var MalzemeTipiListe = model.MalzemeTipiListe;
			MalzemeTipiListe?.Insert(0, new MalzemeTipi { ID = 0, Ad = "Tümü" });
			ViewBag.MalzemeTipiListe = new SelectList(MalzemeTipiListe, "ID", "Ad");

			var DovizTurListe = model.DovizTurListe;
			ViewBag.DovizTurListe = new SelectList(DovizTurListe, "ID", "Ad");
		}
		private void TempDataMesajAyari(bool sonuc)
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
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "CRM";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemeStokVM()
			{
				//MalzemeGrupListe = await _stokService.GrupListeleHepsi() ,
				MalzemeStokVMListe=await _stokService.VeriListele((int)user.DepartmanID) 
				
			};
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			DepoListe?.Insert(0, new MalzemeDepoVM { ID = 0, Ad = "Tümü" });
			ViewBag.DepoListe = new SelectList(DepoListe, "ID", "Ad");

			 

			await ViewBagListeDoldur();

            return View(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> Index(MalzemeStokVM models)
		{
			ViewBag.Modul = "CRM";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemeStokVM()
			{
				//MalzemeGrupListe = await _stokService.GrupListeleHepsi(),
				MalzemeStokVMListe = await _stokService.VeriListele(models, (int)user.DepartmanID),

			};
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			DepoListe?.Insert(0, new MalzemeDepoVM { ID = 0, Ad = "Tümü" });
			ViewBag.DepoListe = new SelectList(DepoListe, "ID", "Ad");

            await ViewBagListeDoldur();
            return View(modelvm);
		}


		#region "Hareketler"
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> _VeriEkleCoklu(MalzemeStokVM models)
		{
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			var malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeStokVM>>(malzemeStokJson);


			models.MalzemeStokVMListe = malzemeStokListe;
			 

			bool sonuc =await  _stokService.VeriEkleCoklu(models);

			// Session'daki "MalzemeStokListe" anahtarına bağlı veriyi kaldırıyoruz
			HttpContext.Session.Remove("MalzemeStokListe");

			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> _VeriEkleTransferCoklu(MalzemeStokVM models)
		{
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			var malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeStokVM>>(malzemeStokJson);


			models.MalzemeStokVMListe = malzemeStokListe;


			bool sonuc = await _stokService.VeriEkleTransferCoklu(models);

			// Session'daki "MalzemeStokListe" anahtarına bağlı veriyi kaldırıyoruz
			HttpContext.Session.Remove("MalzemeStokListe");

			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		public async Task<PartialViewResult> _StokHareketEkle(int HareketTurID, int InOut)
		{
			HttpContext.Session.Remove("MalzemeStokListe");
            var HareketTurListe = await _stokService.HareketListele();
            var modelvm = new MalzemeStokVM()
			{
				 Tarih=DateTime.Now,
				 SktTarih=DateTime.Now,
				 HareketTurID= HareketTurID,
				 HareketTur= HareketTurID.ToString(),
				 GirisCikis =InOut==1?true:false,
				 GirisCikisDurum= HareketTurListe.Where(p=>p.ID== HareketTurID).FirstOrDefault().GirisCikisDurum
            };
			
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama 
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");


			if (modelvm.MalzemeStokVMListe==null)
			{
				modelvm.MalzemeStokVMListe = new List<MalzemeStokVM>();
			}


			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _StokTransferEkle(int HareketTurID)
		{
			HttpContext.Session.Remove("MalzemeStokListe");
			var modelvm = new MalzemeStokVM()
			{
				Tarih = DateTime.Now,
				HareketTurID = HareketTurID,
				HareketTur = HareketTurID.ToString(), 
			};
			var HareketTurListe = await _stokService.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");


			if (modelvm.MalzemeStokVMListe == null)
			{
				modelvm.MalzemeStokVMListe = new List<MalzemeStokVM>();
			}


			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _MalzemeStokEkle(MalzemeStokVM models)
		{
			 // Session'dan mevcut MalzemeStokVM listesini alıyoruz
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			List<MalzemeStokVM> malzemeStokListe;

			if (!string.IsNullOrEmpty(malzemeStokJson))
			{
				// Session'da zaten bir liste varsa, onu deserialize ediyoruz
				malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeStokVM>>(malzemeStokJson);
			}
			else
			{
				// Session'da liste yoksa yeni bir liste oluşturuyoruz
				malzemeStokListe = new List<MalzemeStokVM>();
			}

			MalzemelerVM malzeme =await _stokService.MalzemeGetir(models.MalzemeID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			MalzemeStokVM malzemeStokekle = new MalzemeStokVM
			{
				MalzemeAd = malzeme.Ad,
				MalzemeID = models.MalzemeID,
				Miktar = models.Miktar,
				MalzemeKod=malzeme.Kod,
				BirimID = malzeme.BirimID,
				BirimAd=malzeme.BirimAd,
				Fiyat=malzeme.Fiyat,
				DovizTurAd=malzeme.DovizTurAd,
				MalzemeAciklama = models.MalzemeAciklama,
				LotNumara=models.LotNumara,
				SktTarih=models.SktTarih,
			};

			malzemeStokListe.Add(malzemeStokekle);

			// Güncellenmiş listeyi tekrar Session'a kaydediyoruz (JSON formatında)
			HttpContext.Session.SetString("MalzemeStokListe", JsonConvert.SerializeObject(malzemeStokListe));

			// Listeyi view'e göndermek için model oluşturuyoruz
			var model = new MalzemeStokVM
			{
				MalzemeStokVMListe = malzemeStokListe
			};

			return PartialView("_MalzemeEklenen", model);
		}
        [HttpPost]
        public async Task<PartialViewResult> UploadExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return PartialView("_Error", "Dosya seçilmedi veya boş dosya yüklendi.");
            }

            // Dosyayı okuma işlemi (EPPlus kullanıyoruz)
            var malzemeStokListe = new List<MalzemeStokVM>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Lisans bağlamını burada ayarlayın

                using (var package = new ExcelPackage(stream))
                {
                    ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // İlk sayfayı alıyoruz
                    int rowCount = worksheet.Dimension.Rows; // Satır sayısı
                    bool miktarVar = false; // Miktar değerinin olup olmadığını takip eder

                    // Excel'deki her bir satırdaki malzeme kodunu alıyoruz (satır 2'den başlıyoruz çünkü 1. satır başlık)
                    for (int row = 2; row <= rowCount; row++)
                    {
                        string malzemeKod = worksheet.Cells[row, 3].Text.Trim(); // 1. sütun malzeme kodu
                        string miktarText = worksheet.Cells[row, 6].Text.Trim(); // 2. sütun miktar
                        string aciklama = worksheet.Cells[row, 7].Text.Trim(); // 3. sütun açıklama

                        // Miktar değeri boş mu değil mi kontrol edelim
                        if (!string.IsNullOrEmpty(miktarText) && double.TryParse(miktarText, out double miktar) && miktar > 0)
                        {
                            miktarVar = true; // En az bir miktar değeri var

                            // Malzeme koduna göre veritabanından malzemeyi getiriyoruz
                            MalzemelerVM malzeme = await _stokService.MalzemeKodlaGetir(malzemeKod);

                            if (malzeme != null)
                            {
                                // Malzeme bilgilerini `MalzemeStokVM` modeline ekliyoruz
                                var malzemeStokEkle = new MalzemeStokVM
                                {
                                    MalzemeAd = malzeme.Ad,
                                    MalzemeID = malzeme.ID,
                                    Miktar = miktar,
                                    MalzemeKod = malzeme.Kod,
                                    BirimID = malzeme.BirimID,
                                    BirimAd = malzeme.BirimAd,
                                    MalzemeAciklama = aciklama
                                };

                                malzemeStokListe.Add(malzemeStokEkle);
                            }
                        }
                    }

                    // Eğer hiçbir miktar değeri yoksa, boş bir liste döneriz
                    if (!miktarVar)
                    {
                        return PartialView("_MalzemeEklenen", new MalzemeStokVM { MalzemeStokVMListe = new List<MalzemeStokVM>() });
                    }
                }
            }

            // Session'dan mevcut MalzemeStokVM listesini alıyoruz
            var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
            List<MalzemeStokVM> sessionMalzemeStokListe;

            if (!string.IsNullOrEmpty(malzemeStokJson))
            {
                // Session'da zaten bir liste varsa, onu deserialize ediyoruz
                sessionMalzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeStokVM>>(malzemeStokJson);
            }
            else
            {
                // Session'da liste yoksa yeni bir liste oluşturuyoruz
                sessionMalzemeStokListe = new List<MalzemeStokVM>();
            }

            // Yeni listeyi mevcut session listesinin üzerine ekliyoruz
            sessionMalzemeStokListe?.AddRange(malzemeStokListe);

            // Güncellenmiş listeyi tekrar Session'a kaydediyoruz (JSON formatında)
            HttpContext.Session.SetString("MalzemeStokListe", JsonConvert.SerializeObject(sessionMalzemeStokListe));

            // Listeyi Partial View'e göndermek için model oluşturuyoruz
            var model = new MalzemeStokVM
            {
                MalzemeStokVMListe = sessionMalzemeStokListe
            };

            return PartialView("_MalzemeEklenen", model);
        }

        //[HttpPost]
        //public async Task<PartialViewResult> UploadExcel(IFormFile file)
        //{
        //	if (file == null || file.Length == 0)
        //	{
        //		return PartialView("_Error", "Dosya seçilmedi veya boş dosya yüklendi.");
        //	}

        //	// Dosyayı okuma işlemi (EPPlus kullanıyoruz)
        //	var malzemeStokListe = new List<MalzemeStokVM>();
        //	using (var stream = new MemoryStream())
        //	{
        //		await file.CopyToAsync(stream);
        //		ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Lisans bağlamını burada ayarlayın

        //		using (var package = new ExcelPackage(stream))
        //		{
        //			ExcelWorksheet worksheet = package.Workbook.Worksheets[0]; // İlk sayfayı alıyoruz
        //			int rowCount = worksheet.Dimension.Rows; // Satır sayısı

        //			// Excel'deki her bir satırdaki malzeme kodunu alıyoruz (satır 2'den başlıyoruz çünkü 1. satır başlık)
        //			for (int row = 2; row <= rowCount; row++)
        //			{
        //				string malzemeKod = worksheet.Cells[row, 3].Text.Trim(); // 1. sütun malzeme kodu
        //				double miktar = double.Parse(worksheet.Cells[row, 6].Text.Trim()); // 2. sütun miktar
        //				string aciklama = worksheet.Cells[row, 7].Text.Trim(); // 2. sütun miktar

        //				// Malzeme koduna göre veritabanından malzemeyi getiriyoruz
        //				MalzemelerVM malzeme = await _stokService.MalzemeKodlaGetir(malzemeKod);

        //				if (malzeme != null)
        //				{
        //					// Malzeme bilgilerini `MalzemeStokVM` modeline ekliyoruz
        //					var malzemeStokEkle = new MalzemeStokVM
        //					{
        //						MalzemeAd = malzeme.Ad,
        //						MalzemeID = malzeme.ID,
        //						Miktar = miktar,
        //						MalzemeKod = malzeme.Kod,
        //						BirimID = malzeme.BirimID,
        //						BirimAd = malzeme.BirimAd,
        //						MalzemeAciklama = aciklama
        //					};

        //					malzemeStokListe.Add(malzemeStokEkle);
        //				}
        //			}
        //		}
        //	}

        //	// Session'dan mevcut MalzemeStokVM listesini alıyoruz
        //	var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
        //	List<MalzemeStokVM> sessionMalzemeStokListe;

        //	if (!string.IsNullOrEmpty(malzemeStokJson))
        //	{
        //		// Session'da zaten bir liste varsa, onu deserialize ediyoruz
        //		sessionMalzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeStokVM>>(malzemeStokJson);
        //	}
        //	else
        //	{
        //		// Session'da liste yoksa yeni bir liste oluşturuyoruz
        //		sessionMalzemeStokListe = [];
        //	}

        //	// Yeni listeyi mevcut session listesinin üzerine ekliyoruz
        //	sessionMalzemeStokListe?.AddRange(malzemeStokListe);

        //	// Güncellenmiş listeyi tekrar Session'a kaydediyoruz (JSON formatında)
        //	HttpContext.Session.SetString("MalzemeStokListe", JsonConvert.SerializeObject(sessionMalzemeStokListe));

        //	// Listeyi Partial View'e göndermek için model oluşturuyoruz
        //	var model = new MalzemeStokVM
        //	{
        //		MalzemeStokVMListe = sessionMalzemeStokListe
        //	};

        //	return PartialView("_MalzemeEklenen", model);
        //}


        public PartialViewResult MalzemeCikar(int malzemeId)
		{
			// Session'dan mevcut MalzemeStokVM listesini alıyoruz
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			var malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeStokVM>>(malzemeStokJson);
			if (!string.IsNullOrEmpty(malzemeStokJson))
			{
				// Listeyi deserialize ediyoruz
				

				// MalzemeID'ye göre malzemeyi bulup listeden çıkarıyoruz
				var malzemeToRemove = malzemeStokListe.FirstOrDefault(m => m.MalzemeID == malzemeId);
				if (malzemeToRemove != null)
				{
					malzemeStokListe.Remove(malzemeToRemove);

					// Güncellenmiş listeyi tekrar Session'a kaydediyoruz
					HttpContext.Session.SetString("MalzemeStokListe", JsonConvert.SerializeObject(malzemeStokListe));
				}
			}

			// Listeyi view'e göndermek için model oluşturuyoruz
			var model = new MalzemeStokVM
			{
				MalzemeStokVMListe = malzemeStokListe
			};

			return PartialView("_MalzemeEklenen", model);
		}
		public async Task<IActionResult> MalzemeAra(string kelime)
		{
			if (kelime == null || kelime.Length < 3)
			{
				return BadRequest("Aranacak kelime 3 karakter veya fazla olmalı.");
			}

			var results = await _stokService.MalzemeAra(kelime);

			return Ok(results);
		}

		public async Task<PartialViewResult> _VeriGetir(int StokID)
		{
			var HareketTurListe = await _stokService.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm =await _stokService.VeriGetir(StokID);

			return PartialView(modelvm);	
		}
		public async Task<PartialViewResult> _VeriKopyalanacak(int StokID)
		{
			var HareketTurListe = await _stokService.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm = await _stokService.VeriGetir(StokID);

			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _VeriSilinecek(int StokID)
		{
			var HareketTurListe = await _stokService.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm = await _stokService.VeriGetir(StokID);

			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _VeriGoruntule(int StokID)
		{
			var HareketTurListe = await _stokService.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm = await _stokService.VeriGetir(StokID);

			return PartialView(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> _VeriEkle(MalzemeStokVM models)
		{
			bool sonuc = _stokService.VeriEkle(models);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		[HttpPost]
		public async Task<IActionResult> _VeriSil(MalzemeStokVM models)
		{
			bool sonuc =await _stokService.VeriSil(models.ID);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		#endregion

		#region "Excel İşlemleri"
		public IActionResult DownloadExcelFile2()
		{
			// Dosyanın sunucuda bulunduğu yol
			var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "StokUpload", "ornek-malzeme-listesi.xlsx");

			// Dosyanın var olup olmadığını kontrol edin
			if (!System.IO.File.Exists(filePath))
			{
				return NotFound();
			}

			// Dosyanın ismi ve tipi
			var fileName = "ornek-malzeme-listesi.xlsx";
			var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

			// Dosyayı okuma ve indirme işlemi
			var fileBytes = System.IO.File.ReadAllBytes(filePath);

			// Dosyayı indirme işlemi başlatılır
			return File(fileBytes, mimeType, fileName);
		}
		public async Task<IActionResult> DownloadExcelFile()
		{
			// Veritabanından malzeme verilerini alıyoruz
			List<MalzemelerVM> malzemeListesi = await _stokService.MalzemeListele();
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // Lisans bağlamını burada ayarlayın
			 // Yeni bir Excel dosyası oluşturuyoruz (EPPlus kullanarak)
			using (var package = new ExcelPackage())
			{
				// Çalışma sayfası ekle
				var worksheet = package.Workbook.Worksheets.Add("Malzeme Listesi");

				// Başlıkları ekle
				worksheet.Cells[1, 1].Value = "Grup";
				worksheet.Cells[1, 2].Value = "Alt Grup";
				worksheet.Cells[1, 3].Value = "Malzeme Kodu"; 
				worksheet.Cells[1, 4].Value = "Malzeme Adı"; 
				worksheet.Cells[1, 5].Value = "Birim";
				worksheet.Cells[1, 6].Value = "Miktar";
				worksheet.Cells[1, 7].Value = "Açıklama";

				// Verileri ekleyelim (2. satırdan başlıyoruz, 1. satır başlıklar)
				int row = 2;
				foreach (var malzeme in malzemeListesi)
				{
					worksheet.Cells[row, 1].Value = malzeme.Grup;
					worksheet.Cells[row, 2].Value = malzeme.AltGrup;
					worksheet.Cells[row, 3].Value = malzeme.Kod;
					worksheet.Cells[row, 4].Value = malzeme.Ad;
					worksheet.Cells[row, 5].Value = malzeme.BirimAd; 
					row++;
				}

				// Excel dosyasını byte dizisine çeviriyoruz
				var excelBytes = package.GetAsByteArray();

				// Dosyayı kullanıcıya indirmek için gönderiyoruz
				var fileName = "malzeme-listesi.xlsx";
				var mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

				return File(excelBytes, mimeType, fileName);
			}
		}
		#endregion


		#region "Stok Durumu"
		public async Task<IActionResult> MalzemeDepoDurum()
		{
			ViewBag.Modul = "CRM";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemelerVM()
			{ 
				MalzemelerVMListe = await _stokService.DepoDurumu((int)user.DepartmanID) 
			};
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			DepoListe?.Insert(0, new MalzemeDepoVM { ID = 0, Ad = "Tümü" });
			ViewBag.DepoListe = new SelectList(DepoListe, "ID", "Ad");

			//await SelectListFill(modelvm);
			return View(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> MalzemeDepoDurum(MalzemelerVM modelv)
		{
			ViewBag.Modul = "CRM";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemelerVM()
			{ 
				MalzemelerVMListe = await _stokService.DepoDurumu(modelv, (int)user.DepartmanID)
			};
			var DepoListe = await _stokService.DepoListele((int)user.DepartmanID);
			DepoListe?.Insert(0, new MalzemeDepoVM { ID = 0, Ad = "Tümü" });
			ViewBag.DepoListe = new SelectList(DepoListe, "ID", "Ad");
			//await SelectListFill(modelvm);
			return View(modelvm);
		}
		#endregion

		#region "Gruplar ve Alt Gruplar"
		public async Task<IActionResult> Gruplar()
		{
			ViewBag.Modul = "CRM";
			var modelvm = new GruplarVM()
			{
				MalzemeGrupListe = await _stokService.GrupListele()
			};		 
			return View(modelvm);
		}
		public async Task<PartialViewResult> Grup_VeriGetir(int GrupID)
		{
			MalzemeGrup modelc = await _stokService.GrupGetir(GrupID);
			return PartialView("GrupGetir", modelc);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> Grup_VeriEkle(MalzemeGrup model)
		{
			bool sonuc = await   _stokService.GrupVeriEkle(model);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Gruplar");
		}
		public async Task<PartialViewResult> Grup_SilinecekVerileriGetir(int GrupID)
		{
			MalzemeGrup modelc = await _stokService.GrupGetir(GrupID);
			return PartialView("SilinecekGrupGetir", modelc);
		}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Grup_VeriSil(MalzemeGrup modelc)
		{
			bool sonuc = await _stokService.GrupSil(modelc.ID);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Gruplar");
		}
		public async Task<IActionResult> AltGruplar(int GrupID=0)
		{
			ViewBag.Modul = "CRM";
			var modelvm = new GruplarVM()
			{
				ParentGrup= await _stokService.GrupGetir(GrupID),
				MalzemeGrupListe = await _stokService.AltGrupListele(GrupID)
			};
			return View(modelvm);
		}
		public async Task<PartialViewResult> AltGrup_VeriGetir(int AltGrupID)
		{
			MalzemeGrup modelc = await _stokService.AltGrupGetir(AltGrupID);
			return PartialView("AltGrupGetir", modelc);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> AltGrup_VeriEkle(MalzemeGrup model)
		{
			bool sonuc = await _stokService.AltGrupVeriEkle(model);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("AltGruplar", new {GrupID=model.ParentID});
		}
		public async Task<PartialViewResult> AltGrup_SilinecekVerileriGetir(int AltGrupID)
		{
			MalzemeGrup modelc = await _stokService.AltGrupGetir(AltGrupID);
			return PartialView("SilinecekAltGrupGetir", modelc);
		}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> AltGrup_VeriSil(MalzemeGrup modelc)
		{
			bool sonuc = await _stokService.AltGrupSil(modelc.ID);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("AltGruplar", new { GrupID = modelc.ParentID });
		}
		#endregion

		#region "Malzemeler"
		public async Task<IActionResult> Malzemeler()
		{
			ViewBag.Modul = "CRM"; 
			var modelvm = new MalzemelerVM()
			{
				MalzemeGrupListe = await _stokService.GrupListeleHepsi(),
				MalzemelerVMListe=await _stokService.MalzemeListele(),
				DovizTurListe= await _stokService.DovizTurListele(),
			};
			await SelectListFill(modelvm);
			return View(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> Malzemeler(MalzemelerVM modelv)
		{
			ViewBag.Modul = "CRM";
			//MalzemelerVM model = await VMModel(); 
			var modelvm = new MalzemelerVM()
			{
				MalzemeGrupListe = await _stokService.GrupListeleHepsi(),
				MalzemelerVMListe = await _stokService.MalzemeListele(modelv)
			};
			modelvm.AltGrupID = modelv.AltGrupID;
			modelvm.GrupID = modelv.GrupID;
			await SelectListFill(modelvm);
			return View(modelvm);
		}
		public async Task<PartialViewResult> Malzeme_VeriGetir(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _stokService.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _stokService.GrupListeleHepsi();
			return PartialView("MalzemeGetir", modelc);
		}
		public async Task<PartialViewResult> Malzeme_VeriGoruntule(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _stokService.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _stokService.GrupListeleHepsi();
			return PartialView("MalzemeGoruntule", modelc);
		}
		public async Task<PartialViewResult> Malzeme_SilinecekVerileriGetir(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _stokService.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _stokService.GrupListeleHepsi();
			return PartialView("MalzemeSilinecek", modelc);
		}
		public async Task<PartialViewResult> Malzeme_KopyalanacakVerileriGetir(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _stokService.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _stokService.GrupListeleHepsi();
			return PartialView("MalzemeKopyalanacak", modelc);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> Malzeme_VeriEkle(MalzemelerVM models)
		{
			bool sonuc = await _stokService.MalzemeEkle(models);
			//TempDataMesajAyari(sonuc);
			//return RedirectToAction("Malzemeler");

			var modelvm = new MalzemelerVM
			{
				MalzemelerVMListe = await _stokService.KategoriMalzemeListele((int)models.GrupID)
			};
			return PartialView("_KategoriMalzemeListesiGetir", modelvm);
		}
		[Authorize(Policy = "Delete")]
		[HttpPost]
		public async Task<IActionResult> Malzeme_VeriSil(MalzemelerVM models)
		{
			bool sonuc = await _stokService.MalzemeSil(models.ID);
			//TempDataMesajAyari(sonuc);
			//return RedirectToAction("Malzemeler");

			var modelvm = new MalzemelerVM
			{
				MalzemelerVMListe = await _stokService.KategoriMalzemeListele((int)models.GrupID)
			};
			return PartialView("_KategoriMalzemeListesiGetir", modelvm);
		}
		public async Task<PartialViewResult> _FotoGetir(int MalzemeID = 0)
		{
						
			var modelc = await _stokService.MalzemeGetir(MalzemeID);
			return PartialView(modelc);
		}
		[HttpPost]
		public async Task<IActionResult> FotoYukle(MalzemelerVM models)
		{
			if (models.Dosya != null && models.Dosya.Length > 0)
			{
				var dosya_id = Guid.NewGuid().ToString();
				var fileExtension = Path.GetExtension(models.Dosya.FileName);
				var fileName = Path.GetFileName(models.Dosya.FileName);
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
						await models.Dosya.CopyToAsync(stream);
					}					 
				}

				models.Fotograf = dosyaAdi;
				_stokService.FotoYukle(models);

				//  return Json(modelc);
				return RedirectToAction("Malzemeler");
			}
			return PartialView("error");
			//  return BadRequest("Dosya yüklenemedi.");
		}

		public async Task<PartialViewResult> _FiyatGetir(int UrunID = 0)
		{
			MalzemelerVM model = new()
			{
				MalzemeFiyatListe = await _stokService.FiyatGetir(UrunID)
			};
			return PartialView("_FiyatGetir", model);
		}
		[HttpPost]
		public async Task<IActionResult> _FiyatDegistir(MalzemeFiyat model)
		{
			bool sonuc = await _stokService.FiyatDegistir(model);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Malzemeler");
		}

		#endregion

		#region "Depolar"
		public async Task<IActionResult> Depolar()
		{
			ViewBag.Modul = "CRM";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemeDepoVM()
			{ 
				MalzemeDepoVMListe = await _stokService.DepoListele((int)user.DepartmanID)
			};
			var DepartmanListe = await _stokService.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad"); 

			return View(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> Depolar(MalzemeDepoVM modelc)
		{
			ViewBag.Modul = "CRM";
			var modelvm = new MalzemeDepoVM()
			{
				MalzemeDepoVMListe = await _stokService.DepoListele(modelc)
			};
			var DepartmanListe = await _stokService.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			return View(modelvm);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> Depo_VeriEkle(MalzemeDepoVM models)
		{
			bool sonuc = await _stokService.DepoEkle(models);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Depolar");
		}
		public async Task<PartialViewResult> Depo_VeriGetir(int DepoID)
		{
			var DepartmanListe = await _stokService.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _stokService.DepoGetir(DepoID);
			return PartialView("DepoGetir", modelc);
		}
		public async Task<PartialViewResult> Depo_VeriGoruntule(int DepoID)
		{
			var DepartmanListe = await _stokService.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _stokService.DepoGetir(DepoID);
			return PartialView("DepoGoruntule", modelc);
		}
		public async Task<PartialViewResult> Depo_SilinecekVerileriGetir(int DepoID)
		{
			var DepartmanListe = await _stokService.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _stokService.DepoGetir(DepoID);
			return PartialView("DepoSilinecek", modelc);
		}
		public async Task<PartialViewResult> Depo_KopyalanacakVerileriGetir(int DepoID)
		{
			var DepartmanListe = await _stokService.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _stokService.DepoGetir(DepoID);
			return PartialView("DepoKopyalanacak", modelc);
		}
		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Depo_VeriSil(MalzemeDepoVM modelc)
		{
			bool sonuc = await _stokService.DepoSil(modelc.ID);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Depolar");
		}
		#endregion

		#region "Kategoriler"
		public async Task<IActionResult> Kategoriler()
        {
            ViewBag.Modul = "CRM";
			// Kategori ağacını alıyoruz
            var kategoriTree = _stokService.GetKategoriTree();
			var gruplarvm=new GruplarVM();
			gruplarvm.KategoriTree=kategoriTree;
            // Bu veriyi View'e gönderiyoruz
            return View(gruplarvm); 
        }
        public async Task<PartialViewResult> KategoriListesiGetir(int KategoriID=0)
        {
            var modelvm = new GruplarVM()
            {
                MalzemeGrupListe = await _stokService.KategoriListele(KategoriID) 
            };  
            return PartialView("_KategoriListesiGetir", modelvm);
        }
        //[Authorize(Roles = "Add")]
        //[HttpPost]
        //public async Task<IActionResult> Kategori_VeriEkle(MalzemeGrup model)
        //{
        //    bool sonuc = await _stokService.AltGrupVeriEkle(model);
        //    TempDataMesajAyari(sonuc);
        //    return RedirectToAction("Kategoriler");
        //}


        [Authorize(Policy = "Create")]
        [HttpPost]
        public async Task<PartialViewResult> Kategori_VeriEkle(MalzemeGrup model)
        {
            bool sonuc = await _stokService.AltGrupVeriEkle(model);
            TempDataMesajAyari(sonuc);
            var modelvm = new GruplarVM()
            {
                MalzemeGrupListe = await _stokService.KategoriListele((int)model.ParentID)
            };
            return PartialView("_KategoriListesiGetir", modelvm);
        }

        public async Task<PartialViewResult> Kategori_VeriGetir(int GrupID)
        {
            MalzemeGrup modelc = await _stokService.GrupGetir(GrupID);
            return PartialView("KategoriGetir", modelc);
        }
        public async Task<PartialViewResult> Kategori_SilinecekVerileriGetir(int GrupID)
        {
            MalzemeGrup modelc = await _stokService.GrupGetir(GrupID);
            return PartialView("KategoriSilinecek", modelc);
        }
        public async Task<PartialViewResult> Kategori_KopyalanacakVerileriGetir(int GrupID)
        {
            MalzemeGrup modelc = await _stokService.GrupGetir(GrupID);
            return PartialView("KategoriKopyalanacak", modelc);
        }
        [HttpPost]
        [Authorize(Policy = "Delete")]
        public async Task<IActionResult> Kategori_VeriSil(MalzemeGrup modelc)
        {
            bool sonuc = await _stokService.GrupSil(modelc.ID);
            TempDataMesajAyari(sonuc);
            var modelvm = new GruplarVM()
            {
                MalzemeGrupListe = await _stokService.KategoriListele((int)modelc.ParentID)
            };
            return PartialView("_KategoriListesiGetir", modelvm);
        }
		public async Task<IActionResult> KategoriMalzeme()
		{
            ViewBag.Modul = "CRM";
            // Kategori ağacını alıyoruz
            //var kategoriTree = _stokService.GetKategoriTree();
            var gruplarvm = new MalzemelerVM
            {
                KategoriTree = _stokService.GetKategoriTree()
            };
            //gruplarvm.KategoriTree = kategoriTree;
            // Bu veriyi View'e gönderiyoruz
            await SelectListFill(gruplarvm);
            return View(gruplarvm);
		}
		public async Task<PartialViewResult> KategoriMalzemeListesiGetir(int KategoriID = 0)
		{
			 
			var models = new MalzemelerVM { 
				MalzemelerVMListe = await _stokService.KategoriMalzemeListele(KategoriID)
			};
           
            return PartialView("_KategoriMalzemeListesiGetir", models);
		}

        public async Task<PartialViewResult> MalzemeYap(int KategoriID = 0,int ParentID=0)
        {
			var grup =await _stokService.GrupGetir(KategoriID);
            var models = new MalzemelerVM
            {
                MalzemelerVMListe = await _stokService.KategoriMalzemeListele(KategoriID),
				ID=KategoriID,
                GrupID=ParentID,
				AltGrupID= ParentID,
                Grup = grup.Ad,
				GrupKod= grup.Kod,

            };
            await SelectListFill(models);
			 return PartialView("_MalzemeYap", models);      
		}
		[Authorize(Policy = "Create")]
        [HttpPost]
        public async Task<PartialViewResult> Kategori_MalzemeYap(MalzemelerVM models)
        {
            bool sonuc = await _stokService.MalzemeEkle(models);
            TempDataMesajAyari(sonuc);
            var modelvm = new MalzemelerVM
            {
                MalzemelerVMListe = await _stokService.KategoriMalzemeListele((int)models.GrupID)
            };
            return PartialView("_KategoriMalzemeListesiGetir", modelvm);
        }
        #endregion

        public async Task<IActionResult> KategoriArama(string arama)
        {
            // Burada search parametresine göre kategori ağacını filtrele
            
            //var gruplarvm = new MalzemelerVM
            //{
            //    KategoriTree = _stokService.GetKategoriTree(arama)
            //};
            List<KategoriTreeItem> KategoriTree = _stokService.GetKategoriTree(arama);

            // "_KategoriPartial" view'ını güncelleyerek geri döner
            return PartialView("_KategoriPartial", KategoriTree);
        }


    }
    //public class KategoriIndexModel : PageModel
    //{
    //    private readonly IStokService _kategoriService;

    //    public KategoriIndexModel(IStokService kategoriService)
    //    {
    //        _kategoriService = kategoriService;
    //    }

    //    public List<KategoriTreeItem> KategoriTree { get; set; }

    //    public void OnGet()
    //    {
    //        KategoriTree = _kategoriService.GetKategoriTree();
    //    }
    //}
}
