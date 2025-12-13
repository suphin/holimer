using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
 
 
using OfficeOpenXml;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using static Ekomers.Data.Services.StokService;

namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrIade")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class IadeController : BaseController
	{
		private readonly IIadeService _iadeRepo;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IFileService _fileService;
		private readonly ICacheService<IadeSebep> _iadeSebepCache;
		private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "IadeUpload");
		private readonly string _uploadFotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "IadeUpload", "malzeme");
		private readonly string _path2 = "IadeUpload";
		public IadeController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			IIadeService iadeService, IWebHostEnvironment hostingEnvironment, IFileService fileService ,
			ICacheService<IadeSebep> iadeSebepCache
			) : base(userManager, roleManager)
		{
			_iadeRepo = iadeService;
			_hostingEnvironment = hostingEnvironment;
			_fileService = fileService; 
			_iadeSebepCache = iadeSebepCache;
		}

		async Task<MalzemelerVM> VMModel()
		{
			//ToplantiVM model = new ToplantiVM();
			var model = await _iadeRepo.VeriDoldur(
				//nameof(MalzemelerVM.MalzemeGrupListe),
				nameof(MalzemelerVM.MalzemeBirimListe),
				nameof(MalzemelerVM.MalzemeTipiListe),
				nameof(MalzemelerVM.DovizTurListe)
				);
			return model;
		}


		private async Task ViewBagListeDoldur()
		{ 
			ViewBag.IadeSebepListe = await _iadeSebepCache.GetListeAsync(CacheKeys.IadeSebepAll);
		}
		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.IadeSebepListe = await _iadeSebepCache.GetListeAsync(CacheKeys.IadeSebepAll);

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
			ViewBag.Modul = "DestekHizmetleri";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemeIadeVM()
			{
				//MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi() ,
				MalzemeIadeVMListe=await _iadeRepo.VeriListele((int)user.DepartmanID) 
				
			};
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			DepoListe?.Insert(0, new MalzemeDepoVM { ID = 0, Ad = "Tümü" });
			ViewBag.DepoListe = new SelectList(DepoListe, "ID", "Ad");

		
			var modeldoldur = new MalzemelerVM();
			await SelectListFill(modeldoldur);
			return View(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> Index(MalzemeIadeVM models)
		{
			ViewBag.Modul = "DestekHizmetleri";
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var modelvm = new MalzemeIadeVM()
			{
				//MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi(),
				MalzemeIadeVMListe = await _iadeRepo.VeriListele(models, (int)user.DepartmanID),

			};
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			DepoListe?.Insert(0, new MalzemeDepoVM { ID = 0, Ad = "Tümü" });
			ViewBag.DepoListe = new SelectList(DepoListe, "ID", "Ad");

			

			var modeldoldur = new MalzemelerVM();
			await SelectListFill(modeldoldur);
			return View(modelvm);
		}


		#region "Hareketler"
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> _VeriEkleCoklu(MalzemeIadeVM models)
		{
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			var malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeIadeVM>>(malzemeStokJson);


			models.MalzemeIadeVMListe = malzemeStokListe;
			 

			bool sonuc =await  _iadeRepo.VeriEkleCoklu(models);

			// Session'daki "MalzemeStokListe" anahtarına bağlı veriyi kaldırıyoruz
			HttpContext.Session.Remove("MalzemeStokListe");

			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> _VeriEkleTransferCoklu(MalzemeIadeVM models)
		{
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			var malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeIadeVM>>(malzemeStokJson);


			models.MalzemeIadeVMListe = malzemeStokListe;


			bool sonuc = await _iadeRepo.VeriEkleTransferCoklu(models);

			// Session'daki "MalzemeStokListe" anahtarına bağlı veriyi kaldırıyoruz
			HttpContext.Session.Remove("MalzemeStokListe");

			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		public async Task<PartialViewResult> _StokHareketEkle(int HareketTurID, int InOut)
		{
			HttpContext.Session.Remove("MalzemeStokListe");
            var HareketTurListe = await _iadeRepo.HareketListele();
            var modelvm = new MalzemeIadeVM()
			{
				 Tarih=DateTime.Now,
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

			//ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			await ViewBagPartialListeDoldur();

			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");


			if (modelvm.MalzemeIadeVMListe==null)
			{
				modelvm.MalzemeIadeVMListe = new List<MalzemeIadeVM>();
			}


			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _StokTransferEkle(int HareketTurID)
		{
			HttpContext.Session.Remove("MalzemeStokListe");
			var modelvm = new MalzemeIadeVM()
			{
				Tarih = DateTime.Now,
				HareketTurID = HareketTurID,
				HareketTur = HareketTurID.ToString(), 
			};
			var HareketTurListe = await _iadeRepo.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");


			if (modelvm.MalzemeIadeVMListe == null)
			{
				modelvm.MalzemeIadeVMListe = new List<MalzemeIadeVM>();
			}


			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _MalzemeStokEkle(MalzemeIadeVM models)
		{
			 // Session'dan mevcut MalzemeIadeVM listesini alıyoruz
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			List<MalzemeIadeVM> malzemeStokListe;

			if (!string.IsNullOrEmpty(malzemeStokJson))
			{
				// Session'da zaten bir liste varsa, onu deserialize ediyoruz
				malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeIadeVM>>(malzemeStokJson);
			}
			else
			{
				// Session'da liste yoksa yeni bir liste oluşturuyoruz
				malzemeStokListe = new List<MalzemeIadeVM>();
			}

			MalzemelerVM malzeme =await _iadeRepo.MalzemeGetir(models.MalzemeID);

			// Yeni eklenen malzemeyi listeye ekliyoruz
			MalzemeIadeVM malzemeStokekle = new MalzemeIadeVM
			{
				MalzemeAd = malzeme.Ad,
				MalzemeID = models.MalzemeID,
				Miktar = models.Miktar,
				MalzemeKod=malzeme.Kod,
				BirimID = malzeme.BirimID,
				BirimAd=malzeme.BirimAd,
				Fiyat=malzeme.Fiyat,
				DovizTurAd=malzeme.DovizTurAd,
				MalzemeAciklama = models.MalzemeAciklama
			};

			malzemeStokListe.Add(malzemeStokekle);

			// Güncellenmiş listeyi tekrar Session'a kaydediyoruz (JSON formatında)
			HttpContext.Session.SetString("MalzemeStokListe", JsonConvert.SerializeObject(malzemeStokListe));

			// Listeyi view'e göndermek için model oluşturuyoruz
			var model = new MalzemeIadeVM
			{
				MalzemeIadeVMListe = malzemeStokListe
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
            var malzemeStokListe = new List<MalzemeIadeVM>();
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
                            MalzemelerVM malzeme = await _iadeRepo.MalzemeKodlaGetir(malzemeKod);

                            if (malzeme != null)
                            {
                                // Malzeme bilgilerini `MalzemeIadeVM` modeline ekliyoruz
                                var malzemeStokEkle = new MalzemeIadeVM
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
                        return PartialView("_MalzemeEklenen", new MalzemeIadeVM { MalzemeIadeVMListe = new List<MalzemeIadeVM>() });
                    }
                }
            }

            // Session'dan mevcut MalzemeIadeVM listesini alıyoruz
            var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
            List<MalzemeIadeVM> sessionMalzemeStokListe;

            if (!string.IsNullOrEmpty(malzemeStokJson))
            {
                // Session'da zaten bir liste varsa, onu deserialize ediyoruz
                sessionMalzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeIadeVM>>(malzemeStokJson);
            }
            else
            {
                // Session'da liste yoksa yeni bir liste oluşturuyoruz
                sessionMalzemeStokListe = new List<MalzemeIadeVM>();
            }

            // Yeni listeyi mevcut session listesinin üzerine ekliyoruz
            sessionMalzemeStokListe?.AddRange(malzemeStokListe);

            // Güncellenmiş listeyi tekrar Session'a kaydediyoruz (JSON formatında)
            HttpContext.Session.SetString("MalzemeStokListe", JsonConvert.SerializeObject(sessionMalzemeStokListe));

            // Listeyi Partial View'e göndermek için model oluşturuyoruz
            var model = new MalzemeIadeVM
            {
                MalzemeIadeVMListe = sessionMalzemeStokListe
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
        //	var malzemeStokListe = new List<MalzemeIadeVM>();
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
        //				MalzemelerVM malzeme = await _iadeRepo.MalzemeKodlaGetir(malzemeKod);

        //				if (malzeme != null)
        //				{
        //					// Malzeme bilgilerini `MalzemeIadeVM` modeline ekliyoruz
        //					var malzemeStokEkle = new MalzemeIadeVM
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

        //	// Session'dan mevcut MalzemeIadeVM listesini alıyoruz
        //	var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
        //	List<MalzemeIadeVM> sessionMalzemeStokListe;

        //	if (!string.IsNullOrEmpty(malzemeStokJson))
        //	{
        //		// Session'da zaten bir liste varsa, onu deserialize ediyoruz
        //		sessionMalzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeIadeVM>>(malzemeStokJson);
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
        //	var model = new MalzemeIadeVM
        //	{
        //		MalzemeIadeVMListe = sessionMalzemeStokListe
        //	};

        //	return PartialView("_MalzemeEklenen", model);
        //}


        public PartialViewResult MalzemeCikar(int malzemeId)
		{
			// Session'dan mevcut MalzemeIadeVM listesini alıyoruz
			var malzemeStokJson = HttpContext.Session.GetString("MalzemeStokListe");
			var malzemeStokListe = JsonConvert.DeserializeObject<List<MalzemeIadeVM>>(malzemeStokJson);
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
			var model = new MalzemeIadeVM
			{
				MalzemeIadeVMListe = malzemeStokListe
			};

			return PartialView("_MalzemeEklenen", model);
		}
		public async Task<IActionResult> MalzemeAra(string kelime)
		{
			if (kelime == null || kelime.Length < 3)
			{
				return BadRequest("Aranacak kelime 3 karakter veya fazla olmalı.");
			}

			var results = await _iadeRepo.MalzemeAra(kelime);

			return Ok(results);
		}

		public async Task<PartialViewResult> _VeriGetir(int StokID)
		{
			var HareketTurListe = await _iadeRepo.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm =await _iadeRepo.VeriGetir(StokID);

			return PartialView(modelvm);	
		}
		public async Task<PartialViewResult> _VeriKopyalanacak(int StokID)
		{
			var HareketTurListe = await _iadeRepo.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm = await _iadeRepo.VeriGetir(StokID);

			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _VeriSilinecek(int StokID)
		{
			var HareketTurListe = await _iadeRepo.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm = await _iadeRepo.VeriGetir(StokID);

			return PartialView(modelvm);
		}
		public async Task<PartialViewResult> _VeriGoruntule(int StokID)
		{
			var HareketTurListe = await _iadeRepo.HareketListele();
			var HareketTur = HareketTurListe.Select(d => new
			{
				d.ID,
				Ad = d.Aciklama
			}).ToList();

			ViewBag.HareketTurListe = new SelectList(HareketTur, "ID", "Ad");
			var user = await _userManager.FindByNameAsync(User.Identity!.Name);
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
			var depoViewList = DepoListe.Select(d => new
			{
				d.ID,
				Ad = d.Departman + " - " + d.Ad
			}).ToList();
			ViewBag.DepoListe = new SelectList(depoViewList, "ID", "Ad");

			var modelvm = await _iadeRepo.VeriGetir(StokID);

			return PartialView(modelvm);
		}
		[HttpPost]
		public async Task<IActionResult> _VeriEkle(MalzemeIadeVM models)
		{
			bool sonuc = _iadeRepo.VeriEkle(models);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Index");
		}
		[HttpPost]
		public async Task<IActionResult> _VeriSil(MalzemeIadeVM models)
		{
			bool sonuc =await _iadeRepo.VeriSil(models.ID);
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
			List<MalzemelerVM> malzemeListesi = await _iadeRepo.MalzemeListele();
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
				MalzemelerVMListe = await _iadeRepo.DepoDurumu((int)user.DepartmanID) 
			};
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
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
				MalzemelerVMListe = await _iadeRepo.DepoDurumu(modelv, (int)user.DepartmanID)
			};
			var DepoListe = await _iadeRepo.DepoListele((int)user.DepartmanID);
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
				MalzemeGrupListe = await _iadeRepo.GrupListele()
			};		 
			return View(modelvm);
		}
		public async Task<PartialViewResult> Grup_VeriGetir(int GrupID)
		{
			MalzemeGrup modelc = await _iadeRepo.GrupGetir(GrupID);
			return PartialView("GrupGetir", modelc);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> Grup_VeriEkle(MalzemeGrup model)
		{
			bool sonuc = await   _iadeRepo.GrupVeriEkle(model);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Gruplar");
		}
		public async Task<PartialViewResult> Grup_SilinecekVerileriGetir(int GrupID)
		{
			MalzemeGrup modelc = await _iadeRepo.GrupGetir(GrupID);
			return PartialView("SilinecekGrupGetir", modelc);
		}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Grup_VeriSil(MalzemeGrup modelc)
		{
			bool sonuc = await _iadeRepo.GrupSil(modelc.ID);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Gruplar");
		}
		public async Task<IActionResult> AltGruplar(int GrupID=0)
		{
			ViewBag.Modul = "CRM";
			var modelvm = new GruplarVM()
			{
				ParentGrup= await _iadeRepo.GrupGetir(GrupID),
				MalzemeGrupListe = await _iadeRepo.AltGrupListele(GrupID)
			};
			return View(modelvm);
		}
		public async Task<PartialViewResult> AltGrup_VeriGetir(int AltGrupID)
		{
			MalzemeGrup modelc = await _iadeRepo.AltGrupGetir(AltGrupID);
			return PartialView("AltGrupGetir", modelc);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> AltGrup_VeriEkle(MalzemeGrup model)
		{
			bool sonuc = await _iadeRepo.AltGrupVeriEkle(model);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("AltGruplar", new {GrupID=model.ParentID});
		}
		public async Task<PartialViewResult> AltGrup_SilinecekVerileriGetir(int AltGrupID)
		{
			MalzemeGrup modelc = await _iadeRepo.AltGrupGetir(AltGrupID);
			return PartialView("SilinecekAltGrupGetir", modelc);
		}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> AltGrup_VeriSil(MalzemeGrup modelc)
		{
			bool sonuc = await _iadeRepo.AltGrupSil(modelc.ID);
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
				MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi(),
				MalzemelerVMListe=await _iadeRepo.MalzemeListele(),
				DovizTurListe= await _iadeRepo.DovizTurListele(),
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
				MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi(),
				MalzemelerVMListe = await _iadeRepo.MalzemeListele(modelv)
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
			MalzemelerVM modelc = await _iadeRepo.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi();
			return PartialView("MalzemeGetir", modelc);
		}
		public async Task<PartialViewResult> Malzeme_VeriGoruntule(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _iadeRepo.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi();
			return PartialView("MalzemeGoruntule", modelc);
		}
		public async Task<PartialViewResult> Malzeme_SilinecekVerileriGetir(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _iadeRepo.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi();
			return PartialView("MalzemeSilinecek", modelc);
		}
		public async Task<PartialViewResult> Malzeme_KopyalanacakVerileriGetir(int MalzemeID)
		{
			MalzemelerVM model = await VMModel();
			await SelectListFill(model);
			MalzemelerVM modelc = await _iadeRepo.MalzemeGetir(MalzemeID);
			modelc.MalzemeGrupListe = await _iadeRepo.GrupListeleHepsi();
			return PartialView("MalzemeKopyalanacak", modelc);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> Malzeme_VeriEkle(MalzemelerVM models)
		{
			bool sonuc = await _iadeRepo.MalzemeEkle(models);
			//TempDataMesajAyari(sonuc);
			//return RedirectToAction("Malzemeler");

			var modelvm = new MalzemelerVM
			{
				MalzemelerVMListe = await _iadeRepo.KategoriMalzemeListele((int)models.GrupID)
			};
			return PartialView("_KategoriMalzemeListesiGetir", modelvm);
		}
		[Authorize(Policy = "Delete")]
		[HttpPost]
		public async Task<IActionResult> Malzeme_VeriSil(MalzemelerVM models)
		{
			bool sonuc = await _iadeRepo.MalzemeSil(models.ID);
			//TempDataMesajAyari(sonuc);
			//return RedirectToAction("Malzemeler");

			var modelvm = new MalzemelerVM
			{
				MalzemelerVMListe = await _iadeRepo.KategoriMalzemeListele((int)models.GrupID)
			};
			return PartialView("_KategoriMalzemeListesiGetir", modelvm);
		}
		public async Task<PartialViewResult> _FotoGetir(int MalzemeID = 0)
		{
						
			var modelc = await _iadeRepo.MalzemeGetir(MalzemeID);
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
				_iadeRepo.FotoYukle(models);

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
				MalzemeFiyatListe = await _iadeRepo.FiyatGetir(UrunID)
			};
			return PartialView("_FiyatGetir", model);
		}
		[HttpPost]
		public async Task<IActionResult> _FiyatDegistir(MalzemeFiyat model)
		{
			bool sonuc = await _iadeRepo.FiyatDegistir(model);
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
				MalzemeDepoVMListe = await _iadeRepo.DepoListele((int)user.DepartmanID)
			};
			var DepartmanListe = await _iadeRepo.DepartmanListele();
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
				MalzemeDepoVMListe = await _iadeRepo.DepoListele(modelc)
			};
			var DepartmanListe = await _iadeRepo.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			return View(modelvm);
		}
		[Authorize(Policy = "Create")]
		[HttpPost]
		public async Task<IActionResult> Depo_VeriEkle(MalzemeDepoVM models)
		{
			bool sonuc = await _iadeRepo.DepoEkle(models);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Depolar");
		}
		public async Task<PartialViewResult> Depo_VeriGetir(int DepoID)
		{
			var DepartmanListe = await _iadeRepo.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _iadeRepo.DepoGetir(DepoID);
			return PartialView("DepoGetir", modelc);
		}
		public async Task<PartialViewResult> Depo_VeriGoruntule(int DepoID)
		{
			var DepartmanListe = await _iadeRepo.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _iadeRepo.DepoGetir(DepoID);
			return PartialView("DepoGoruntule", modelc);
		}
		public async Task<PartialViewResult> Depo_SilinecekVerileriGetir(int DepoID)
		{
			var DepartmanListe = await _iadeRepo.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _iadeRepo.DepoGetir(DepoID);
			return PartialView("DepoSilinecek", modelc);
		}
		public async Task<PartialViewResult> Depo_KopyalanacakVerileriGetir(int DepoID)
		{
			var DepartmanListe = await _iadeRepo.DepartmanListele();
			DepartmanListe?.Insert(0, new Departman { ID = 0, Ad = "Tümü" });
			ViewBag.DepartmanListe = new SelectList(DepartmanListe, "ID", "Ad");

			MalzemeDepoVM modelc = await _iadeRepo.DepoGetir(DepoID);
			return PartialView("DepoKopyalanacak", modelc);
		}
		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Depo_VeriSil(MalzemeDepoVM modelc)
		{
			bool sonuc = await _iadeRepo.DepoSil(modelc.ID);
			TempDataMesajAyari(sonuc);
			return RedirectToAction("Depolar");
		}
		#endregion

		#region "Kategoriler"
		public async Task<IActionResult> Kategoriler()
        {
            ViewBag.Modul = "CRM";
			// Kategori ağacını alıyoruz
            var kategoriTree = _iadeRepo.GetKategoriTree();
			var gruplarvm=new GruplarVM();
			gruplarvm.KategoriTree=kategoriTree;
            // Bu veriyi View'e gönderiyoruz
            return View(gruplarvm); 
        }
        public async Task<PartialViewResult> KategoriListesiGetir(int KategoriID=0)
        {
            var modelvm = new GruplarVM()
            {
                MalzemeGrupListe = await _iadeRepo.KategoriListele(KategoriID) 
            };  
            return PartialView("_KategoriListesiGetir", modelvm);
        }
        //[Authorize(Roles = "Add")]
        //[HttpPost]
        //public async Task<IActionResult> Kategori_VeriEkle(MalzemeGrup model)
        //{
        //    bool sonuc = await _iadeRepo.AltGrupVeriEkle(model);
        //    TempDataMesajAyari(sonuc);
        //    return RedirectToAction("Kategoriler");
        //}


        [Authorize(Policy = "Create")]
        [HttpPost]
        public async Task<PartialViewResult> Kategori_VeriEkle(MalzemeGrup model)
        {
            bool sonuc = await _iadeRepo.AltGrupVeriEkle(model);
            TempDataMesajAyari(sonuc);
            var modelvm = new GruplarVM()
            {
                MalzemeGrupListe = await _iadeRepo.KategoriListele((int)model.ParentID)
            };
            return PartialView("_KategoriListesiGetir", modelvm);
        }

        public async Task<PartialViewResult> Kategori_VeriGetir(int GrupID)
        {
            MalzemeGrup modelc = await _iadeRepo.GrupGetir(GrupID);
            return PartialView("KategoriGetir", modelc);
        }
        public async Task<PartialViewResult> Kategori_SilinecekVerileriGetir(int GrupID)
        {
            MalzemeGrup modelc = await _iadeRepo.GrupGetir(GrupID);
            return PartialView("KategoriSilinecek", modelc);
        }
        public async Task<PartialViewResult> Kategori_KopyalanacakVerileriGetir(int GrupID)
        {
            MalzemeGrup modelc = await _iadeRepo.GrupGetir(GrupID);
            return PartialView("KategoriKopyalanacak", modelc);
        }
        [HttpPost]
        [Authorize(Policy = "Delete")]
        public async Task<IActionResult> Kategori_VeriSil(MalzemeGrup modelc)
        {
            bool sonuc = await _iadeRepo.GrupSil(modelc.ID);
            TempDataMesajAyari(sonuc);
            var modelvm = new GruplarVM()
            {
                MalzemeGrupListe = await _iadeRepo.KategoriListele((int)modelc.ParentID)
            };
            return PartialView("_KategoriListesiGetir", modelvm);
        }
		public async Task<IActionResult> KategoriMalzeme()
		{
            ViewBag.Modul = "CRM";
            // Kategori ağacını alıyoruz
            //var kategoriTree = _iadeRepo.GetKategoriTree();
            var gruplarvm = new MalzemelerVM
            {
                KategoriTree = _iadeRepo.GetKategoriTree()
            };
            //gruplarvm.KategoriTree = kategoriTree;
            // Bu veriyi View'e gönderiyoruz
            await SelectListFill(gruplarvm);
            return View(gruplarvm);
		}
		public async Task<PartialViewResult> KategoriMalzemeListesiGetir(int KategoriID = 0)
		{
			 
			var models = new MalzemelerVM { 
				MalzemelerVMListe = await _iadeRepo.KategoriMalzemeListele(KategoriID)
			};
           
            return PartialView("_KategoriMalzemeListesiGetir", models);
		}

        public async Task<PartialViewResult> MalzemeYap(int KategoriID = 0,int ParentID=0)
        {
			var grup =await _iadeRepo.GrupGetir(KategoriID);
            var models = new MalzemelerVM
            {
                MalzemelerVMListe = await _iadeRepo.KategoriMalzemeListele(KategoriID),
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
            bool sonuc = await _iadeRepo.MalzemeEkle(models);
            TempDataMesajAyari(sonuc);
            var modelvm = new MalzemelerVM
            {
                MalzemelerVMListe = await _iadeRepo.KategoriMalzemeListele((int)models.GrupID)
            };
            return PartialView("_KategoriMalzemeListesiGetir", modelvm);
        }
        #endregion

        public async Task<IActionResult> KategoriArama(string arama)
        {
            // Burada search parametresine göre kategori ağacını filtrele
            
            //var gruplarvm = new MalzemelerVM
            //{
            //    KategoriTree = _iadeRepo.GetKategoriTree(arama)
            //};
            List<KategoriTreeItem> KategoriTree = _iadeRepo.GetKategoriTree(arama);

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
