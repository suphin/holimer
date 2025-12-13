 
 
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Controllers;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace Ekomers.Web.Controllers
{
	//[Authorize(Roles = "Admin")]
	[Authorize(Policy = "AdminOrFW")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class EczaneController : BaseController
	{
		private readonly IEczaneService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;
		private readonly ICacheService<Sehirler> _sehirlerCache;
		private readonly ISehirlerService _sehirlerService;
		private readonly string _uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Eczane");
		private readonly string _uploadFotoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Eczane", "Upload", "Eczaci");
		private readonly string _path2 = "Eczane";
		public EczaneController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IEczaneService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
			, ApplicationDbContext context
			, IHttpClientFactory httpClientFactory
			, ICacheService<Sehirler> sehirlerCache
			,ISehirlerService sehirlerService
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
			_httpClientFactory = httpClientFactory;
			_sehirlerCache = sehirlerCache;	
			_sehirlerService= sehirlerService;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}
		private async Task ViewBagPartialListeDoldur(int sehirId, int ilceId)
		{
			ViewBag.MusteriTipListe = new SelectList(await _context.MusteriTip.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");

			ViewBag.SehirlerListe = new SelectList(await _sehirlerService.GetSehirler(0), "ID", "Ad", sehirId);
			ViewBag.IlcelerListe = new SelectList(await _sehirlerService.GetSehirler(sehirId), "ID", "Ad");
			ViewBag.MahalleListe = new SelectList(await _sehirlerService.GetMahalle(ilceId), "ID", "Ad");
		}
		private async Task ViewBagPartialListeDoldur()
		{
			ViewBag.MusteriTipListe = new SelectList(await _context.MusteriTip.OrderBy(p => p.Ad).ToListAsync(), "ID", "Ad");

			ViewBag.SehirlerListe = new SelectList(await _sehirlerService.GetSehirler(0), "ID", "Ad", 34);
			ViewBag.IlcelerListe = new SelectList(await _sehirlerService.GetSehirler(34), "ID", "Ad");
			ViewBag.MahalleListe = new SelectList(await _sehirlerService.GetMahalle(1476), "ID", "Ad");
		}
		private async Task ViewBagListeDoldur()
		{
			Expression<Func<Sehirler, bool>> filter = a => a.UstID == 0;
			ViewBag.SehirlerAramaListe = await _sehirlerCache.GetListeAsync(CacheKeys.SehirlerAll, filter);


		}


		//private async Task ViewBagPartialListeDoldur()
		//{
		//	//ViewBag.SehirlerListe = await _sehirlerCache.GetListeAsync(CacheKeys.SehirlerAll);

		//}
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

		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "FW";
			await ViewBagListeDoldur();
			var model = new EczaneVM
			{
				EczaneVMListe = await _service.VeriListele()
			};

			return View(model);
		}


		public async Task<IActionResult> Bayiler()
		{
			ViewBag.Modul = "FW";
			await ViewBagListeDoldur();
			var model = new EczaneVM
			{
				EczaneVMListe = await _service.VeriListele()
			};

			return View(model);
		}

		[HttpPost]
		[Authorize(Policy = "View")]
		public async Task<IActionResult> Index(EczaneVM modelv)
		{
			ViewBag.Modul = "FW";
			await ViewBagListeDoldur();
			var model = new EczaneVM
			{
				EczaneVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}
		
		[Authorize(Policy = "View")]
		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{
			 
			var modelc = await _service.VeriGetir(VeriID);
			if (VeriID == 0)
			{
				await ViewBagPartialListeDoldur();
			}
			else
			{
				await ViewBagPartialListeDoldur((int)modelc.SehirID, (int)modelc.IlceID);
			}
			modelc.ControllerName = "Eczane";
			modelc.ModalTitle = "Eczane Bilgileri";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Policy = "Create")]
		[HttpPost]
		public IActionResult VeriEkle(EczaneVM model)
		{
			bool sonuc = _service.VeriEkle(model);

			//PageToastr(sonuc);
			//return RedirectToAction("Index");
			if (sonuc)
			{
				return Ok("Kayıt işlemi başarılı");
			}
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
}

		[HttpPost]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> VeriSil(EczaneVM model)
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

		public async Task<IActionResult> EczaneAktar()
		{
			//List<Eczane> eczanelistesi =await GetEczanelerByLokasyon("34");
			List<Eczane> eczaneListesi = new List<Eczane>();

			for (int plaka = 1; plaka <= 81; plaka++)
			{
				string plakaKodu = plaka.ToString("D2"); // 01, 02, ... 81

				var gelenListe = await GetEczanelerByLokasyon(plakaKodu);

				if (gelenListe != null && gelenListe.Count > 0)
					eczaneListesi.AddRange(gelenListe);
			}


			bool sonuc = await _service.EczaneAktar(eczaneListesi);
			PageToastr(sonuc);
			return RedirectToAction("Index");
		}
		private async Task<List<Eczane>> GetEczanelerByLokasyon(string lokasyonId)
		{
			var sehirler = new Dictionary<string, string>
						{
							{ "01", "ADANA" }, { "02", "ADIYAMAN" }, { "03", "AFYONKARAHİSAR" }, { "04", "AĞRI" },
							{ "05", "AMASYA" }, { "06", "ANKARA" }, { "07", "ANTALYA" }, { "08", "ARTVİN" },
							{ "09", "AYDIN" }, { "10", "BALIKESİR" }, { "11", "BİLECİK" }, { "12", "BİNGÖL" },
							{ "13", "BİTLİS" }, { "14", "BOLU" }, { "15", "BURDUR" }, { "16", "BURSA" },
							{ "17", "ÇANAKKALE" }, { "18", "ÇANKIRI" }, { "19", "ÇORUM" }, { "20", "DENİZLİ" },
							{ "21", "DİYARBAKIR" }, { "22", "EDİRNE" }, { "23", "ELAZIĞ" }, { "24", "ERZİNCAN" },
							{ "25", "ERZURUM" }, { "26", "ESKİŞEHİR" }, { "27", "GAZİANTEP" }, { "28", "GİRESUN" },
							{ "29", "GÜMÜŞHANE" }, { "30", "HAKKARİ" }, { "31", "HATAY" }, { "32", "ISPARTA" },
							{ "33", "MERSİN" }, { "34", "İSTANBUL" }, { "35", "İZMİR" }, { "36", "KARS" },
							{ "37", "KASTAMONU" }, { "38", "KAYSERİ" }, { "39", "KIRKLARELİ" }, { "40", "KIRŞEHİR" },
							{ "41", "KOCAELİ" }, { "42", "KONYA" }, { "43", "KÜTAHYA" }, { "44", "MALATYA" },
							{ "45", "MANİSA" }, { "46", "KAHRAMANMARAŞ" }, { "47", "MARDİN" }, { "48", "MUĞLA" },
							{ "49", "MUŞ" }, { "50", "NEVŞEHİR" }, { "51", "NİĞDE" }, { "52", "ORDU" },
							{ "53", "RİZE" }, { "54", "SAKARYA" }, { "55", "SAMSUN" }, { "56", "SİİRT" },
							{ "57", "SİNOP" }, { "58", "SİVAS" }, { "59", "TEKİRDAĞ" }, { "60", "TOKAT" },
							{ "61", "TRABZON" }, { "62", "TUNCELİ" }, { "63", "ŞANLIURFA" }, { "64", "UŞAK" },
							{ "65", "VAN" }, { "66", "YOZGAT" }, { "67", "ZONGULDAK" }, { "68", "AKSARAY" },
							{ "69", "BAYBURT" }, { "70", "KARAMAN" }, { "71", "KIRIKKALE" }, { "72", "BATMAN" },
							{ "73", "ŞIRNAK" }, { "74", "BARTIN" }, { "75", "ARDAHAN" }, { "76", "IĞDIR" },
							{ "77", "YALOVA" }, { "78", "KARABÜK" }, { "79", "KİLİS" }, { "80", "OSMANİYE" },
							{ "81", "DÜZCE" }
						};
			var url = $"https://www.eczaneler.gen.tr/iframe.php?lokasyon={lokasyonId}";

			var httpClient = _httpClientFactory.CreateClient("EczaneClient");
			httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");

			var html = await httpClient.GetStringAsync(url);
			var doc = new HtmlDocument();
			doc.LoadHtml(html);

			var eczaneler = new List<Eczane>();

			// Tarih bilgisini al
			var tarihNode = doc.DocumentNode.SelectSingleNode("//thead[@class='thead-dark']/tr/th");
			string tarih = tarihNode != null ? tarihNode.InnerText.Trim() : "Tarih bilgisi bulunamadı";

			// İl ve ilçe bilgilerini başlıktan al
			string il = string.Empty;
			string ilce = string.Empty;
			var titleNode = doc.DocumentNode.SelectSingleNode("//title");
			if (titleNode != null)
			{
				string title = titleNode.InnerText.Trim();
				// "Eczaneler.gen.tr | Doğubayazıt Nöbetçi Eczaneleri (Ağrı)" formatından il ve ilçeyi çıkar
				var match = Regex.Match(title, @"^Eczaneler\.gen\.tr \| (.+) Nöbetçi Eczaneleri \((.+)\)");
				if (match.Success && match.Groups.Count >= 3)
				{
					ilce = match.Groups[1].Value.Trim();
					il = match.Groups[2].Value.Trim();
				}
			}

			// İl ve ilçe bilgisi başlıktan alınamadıysa, tarih bilgisinden almayı dene
			if (string.IsNullOrEmpty(il) || string.IsNullOrEmpty(ilce))
			{
				if (tarihNode != null)
				{
					// "18 Nisan Cuma Doğubayazıt Nöbetçi Eczaneleri" formatından ilçeyi çıkarmaya çalış
					var match = Regex.Match(tarih, @"(\d+\s+\w+\s+\w+)\s+(.+?)\s+Nöbetçi Eczaneleri");
					if (match.Success && match.Groups.Count >= 3)
					{
						ilce = match.Groups[2].Value.Trim();
					}
				}
			}

			// Eczane bilgilerini topla
			var eczaneParcalari = doc.DocumentNode.SelectNodes("//tbody/tr");

			if (eczaneParcalari != null)
			{
				Eczane currentEczane = null;
				

				for (int i = 0; i < eczaneParcalari.Count; i++)
				{
					var tr = eczaneParcalari[i];

					// Yeni eczane başlangıcı
					if (tr.SelectSingleNode(".//b") != null)
					{
						if (currentEczane != null)
						{
							
							eczaneler.Add(currentEczane);
						}

						currentEczane = new Eczane
						{
							TarihDetay = tarih,
							Ad = tr.SelectSingleNode(".//b").InnerText.Trim(),
							Il = il,
							Ilce = ilce,
							Tarih = DateTime.Now.Date.ToShortDateString(),
							IsActive = true,
							IsDelete = false,
							CreateUserID = _userId,
							CreateDate = DateTime.Now,
							Plaka = lokasyonId, // Plaka kodunu ekle
							Sehir = sehirler[lokasyonId],
							BayiMi= false // BayiMi varsayılan olarak false
						};

						// Konum bilgisini al
						var konumLink = tr.SelectSingleNode(".//a[@href]");
						if (konumLink != null)
						{
							string href = konumLink.GetAttributeValue("href", "");
							if (href.Contains("daddr="))
							{
								currentEczane.Konum = href.Substring(href.IndexOf("daddr=") + 6);
								var koordinatlar = currentEczane.Konum.Split(',');
								if (koordinatlar.Length >= 2)
								{
									currentEczane.Enlem = koordinatlar[0];
									currentEczane.Boylam = koordinatlar[1];
								}
							}
						}
					}
					// Telefon bilgisi
					else if (tr.SelectSingleNode(".//img[@src='/resimler/telefon.png']") != null)
					{
						if (currentEczane != null && tr.SelectNodes("td")?.Count > 1)
						{
							currentEczane.Telefon = tr.SelectNodes("td")[1].InnerText.Trim();
						}
					}
					// Adres bilgisi
					else if (tr.SelectSingleNode(".//img[@src='/resimler/adres.png']") != null)
					{
						if (currentEczane != null && tr.SelectNodes("td")?.Count > 1)
						{
							var addressTd = tr.SelectNodes("td")[1];
							string fullAddress = addressTd.InnerText.Trim();

							var aciklamaNode = addressTd.SelectSingleNode(".//span[@class='text-muted']");
							if (aciklamaNode != null)
							{
								currentEczane.AciklamaAdres = aciklamaNode.InnerText.Trim().Trim('(', ')');
								currentEczane.Adres = fullAddress.Replace(aciklamaNode.OuterHtml, "").Trim();
							}
							else
							{
								currentEczane.Adres = fullAddress;
							}

							//var match = Regex.Match(currentEczane.Adres, @"(?<ilce>[A-Za-zÇĞİÖŞÜçğıöşü0-9\s]+?)\s*/\s*(?<il>[A-Za-zÇĞİÖŞÜçğıöşü]+)");
							//currentEczane.Ilce = ExtractBetween(currentEczane.Adres, ",", "/").Trim(); 
							//currentEczane.Il = ExtractBetween(currentEczane.Adres, "/", "(").Trim();
							//
							//		var match = Regex.Match(currentEczane.Adres,
							//@"(?:(?<Kod>[0-9A-Za-zÇĞİÖŞÜçğıöşü]+)\s+)?(?<Ilce>[A-Za-zÇĞİÖŞÜçğıöşü\s]+)\s*/\s*(?<Il>[A-Za-zÇĞİÖŞÜçğıöşü]+)",
							//RegexOptions.IgnoreCase);

							//	var match = Regex.Match(currentEczane.Adres,
							//@"^(?<Adres>.+?)\s+(?<Ilce>[A-Za-zÇĞİÖŞÜçğıöşü]+)\s*/\s*(?<Il>[A-Za-zÇĞİÖŞÜçğıöşü]+)$",
							//RegexOptions.IgnoreCase);
							//currentEczane.Ilce = match.Groups["Ilce"].Value.Trim();
							//currentEczane.Il = match.Groups["Il"].Value.Trim();

							int lastSlash = currentEczane.Adres.LastIndexOf('/');
							if (lastSlash != -1)
							{
								// Sol tarafın son kelimesi
								string leftPart = currentEczane.Adres.Substring(0, lastSlash).Trim();
								currentEczane.Ilce = leftPart.Substring(leftPart.LastIndexOf(' ') + 1);

								// Sağ tarafın ilk kelimesi
								string rightPart = currentEczane.Adres.Substring(lastSlash + 1).Trim();
								//currentEczane.Il = rightPart.Split(' ')[0]; 
								currentEczane.Il = sehirler[lokasyonId];
							}

							// İl ve ilçe bilgisi daha önce alınamdıysa adresten çıkarmayı dene
							//if (string.IsNullOrEmpty(currentEczane.Il) || string.IsNullOrEmpty(currentEczane.Ilce))
							//{
							//	if (currentEczane.Adres.Contains("/"))
							//	{
							//		var adresBolumleri = currentEczane.Adres.Split('/');
							//		if (adresBolumleri.Length >= 2)
							//		{
							//			var ilIlce = adresBolumleri[adresBolumleri.Length - 1].Trim();
							//			var ilIlceBolumleri = ilIlce.Split(' ');
							//			if (ilIlceBolumleri.Length >= 2)
							//			{
							//				currentEczane.Ilce = ilIlceBolumleri[0].Trim();
							//				currentEczane.Il = ilIlceBolumleri.Length > 1 ? ilIlceBolumleri[1].Trim() : string.Empty;
							//			}
							//		}
							//	}
							//}
						}
					}
				}

				// Son eczaneyi ekle
				if (currentEczane != null)
				{ 
					
					eczaneler.Add(currentEczane);
				}
			}

			return eczaneler;
		}


		static string ExtractBetween(string input, string startDelimiter, string endDelimiter)
		{
			int start = input.IndexOf(startDelimiter);
			if (start == -1) return "";
			start += startDelimiter.Length;
			int end = input.IndexOf(endDelimiter, start);
			if (end == -1) return input.Substring(start);
			return input.Substring(start, end - start);
		}


		[HttpPost]
		public async Task<JsonResult> SaveGeoJson(int ModulID = 0)
		{
			try
			{
				// Sadece ihtiyacımız olan kolonları çekelim
				var eczaneler = await _context.Eczane
					.Where(p => !string.IsNullOrEmpty(p.Konum))
					.Select(p => new { p.ID, p.Enlem, p.Boylam })
					.ToListAsync();

				// Var olan GeoJSON kayıtlarını memory'e alın (tek sorgu)
				var mevcutGeoJsonlar = await _context.Geojson
					.Where(p => p.ModulID == ModulID && eczaneler.Select(e => e.ID).Contains((int)p.KayitID))
					.Select(p => p.KayitID)
					.ToHashSetAsync();

				var now = DateTime.Now;

				foreach (var eczane in eczaneler)
				{
					// Eğer zaten kayıt varsa atla
					if (mevcutGeoJsonlar.Contains(eczane.ID))
						continue;

					var haritaJson = System.Text.Json.JsonSerializer.Serialize(new
					{
						type = "FeatureCollection",
						features = new[]
						{
					new {
						type = "Feature",
						properties = new { },
						geometry = new {
							type = "Point",
							coordinates = new[] { eczane.Boylam, eczane.Enlem }
						}
					}
				}
					});

					_context.Geojson.Add(new Geojson
					{
						Veri = haritaJson,
						CreateDate = now,
						CreateUserID = _userId,
						ModulID = ModulID,
						KayitID = eczane.ID,
						IsActive = true,
						IsDelete = false
					});
				}

				await _context.SaveChangesAsync();

				return Json(new { success = true, message = "GeoJSON başarıyla kaydedildi." });
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}



		[HttpPost]
		public async Task<JsonResult> SaveGeoJson1(int ModulID = 0)
		{
			try
			{

				var eczaneler = _context.Eczane.Where(p=>p.Konum!="").ToList();

				foreach (var eczane in eczaneler)
				{
					// Eczane tablosundaki enlem-boylam bilgileri
					string boylam = eczane.Boylam; // X
					string enlem = eczane.Enlem;  // Y

					// GeoJSON formatına yerleştir
					string haritaJson =
						$"{{\"type\":\"FeatureCollection\",\"features\":[{{\"type\":\"Feature\",\"properties\":{{}},\"geometry\":{{\"type\":\"Point\",\"coordinates\":[{boylam},{enlem}]}}}}]}}";

					// Geojson kaydını kontrol et
					var geoJson = _context.Geojson
						.FirstOrDefault(p => p.KayitID == eczane.ID && p.ModulID == ModulID);

					if (geoJson == null)
					{
						// Yeni kayıt
						_context.Geojson.Add(new Geojson()
						{
							Veri = haritaJson,
							CreateDate = DateTime.Now,
							CreateUserID = _userId,
							ModulID = ModulID,
							KayitID = eczane.ID,
							IsActive = true,
							IsDelete = false
						});
					}
					else
					{
						// Güncelleme
						geoJson.Veri = haritaJson;
						geoJson.UpdateDate = DateTime.Now;
						geoJson.UpdateUserID = _userId;
						_context.Update(geoJson);
					}
				}

				_context.SaveChanges();


				return Json(new { success = true, message = "GeoJSON başarıyla kaydedildi." });
				 
			}
			catch (Exception ex)
			{
				return Json(new { success = false, message = "Bir hata oluştu.", error = ex.Message });
			}
		}


		public async Task<IActionResult> OmtWebeGonder()
		{
			bool sonuc = await _service.OmtWebeGonder(CacheKeys.SehirlerAll);

			if (sonuc)
			{
				return Ok("Aktarma Başarılı");
			}
			else
			{
				return BadRequest("Veri aktarılamadı.");
			}
		}
		public async Task<IActionResult> FwWebeGonder()
		{
			bool sonuc = await _service.FwWebeGonder(CacheKeys.SehirlerAll);

			if (sonuc)
			{
				return Ok("Aktarma Başarılı");
			}
			else
			{
				return BadRequest("Veri aktarılamadı.");
			}
		}

		public async Task<PartialViewResult> _FotoGetir(int eczaneID = 0)
		{
			// ToplantiVM model = new ToplantiVM();



			var modelc = await _service.VeriGetir(eczaneID);
			return PartialView(modelc);
		}

		[HttpPost]
		public async Task<IActionResult> FotoYukle(EczaneVM model)
		{
			if (model.Dosya != null && model.Dosya.Length > 0)
			{
				var dosya_id = Guid.NewGuid().ToString();
				var fileExtension = Path.GetExtension(model.Dosya.FileName);
				var fileName = Path.GetFileName(model.Dosya.FileName);
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
						await model.Dosya.CopyToAsync(stream);
					}
					
					// FTP'ye yükleme işlemi
					try
					{
						///eczane.ortomolekuler.com/wwwroot/img
						string ftpUrl = "ftp://ortomolekuler.com/eczane.ortomolekuler.com/wwwroot/img/" + dosyaAdi;
						var ftpUserName = "ortomolekuler";
						var ftpUserPassword = "H@UznpocvVz48t2^";
						ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
						//var ftpUrl = "ftp://104.247.167.18/cafe.afyon.bel.tr/wwwroot/Cafe/Upload/Urun/" + dosyaAdi;
						var request = (FtpWebRequest)WebRequest.Create(ftpUrl);
						request.Method = WebRequestMethods.Ftp.UploadFile;

						// FTP kullanıcı adı ve şifre
						request.Credentials = new NetworkCredential(ftpUserName, ftpUserPassword);
						request.EnableSsl = true;
						// FTP'ye dosya yükleme
						using (var ftpStream = request.GetRequestStream())
						{
							await model.Dosya.CopyToAsync(ftpStream);
						}
					}
					catch (Exception ex)
					{
						// Hata durumunda yapılacak işlemler
						return BadRequest("Veri aktarılamadı.");
					}
				}



				model.Fotograf = dosyaAdi;
				_service.FotoYukle(model);

				//  return Json(modelc);
				return Ok("Kayıt işlemi başarılı");
			}
			return BadRequest("Veri aktarılamadı.");
			//  return BadRequest("Dosya yüklenemedi.");
		}
	}
}
