using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;


namespace Ekomers.Web.Controllers
{
	[Authorize(Policy = "AdminOrPurchasing")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class OfferController : BaseController
	{
		private readonly IOfferService _service;
		private readonly IRequestService _requestService;

		private readonly IStokService _stokService;
		private readonly IMalzemeService _malzemeService;
		private readonly ITcmbService _tcmbService;
		private string _userId;
		private readonly ApplicationDbContext _context;
		private readonly IHttpClientFactory _httpClientFactory;

		private readonly ICacheService<OfferDurum> _durumCache;
		private readonly ICacheService<OfferTur> _turCache;
		private readonly ICacheService<Kullanici> _kullaniciCache;
		private readonly ICacheService<Sirketler> _sirketCache;
		private string ModulAd = "PURCHASING";
		public OfferController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IOfferService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
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
			_service = service;
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
			ViewBag.SirketListe = await _sirketCache.GetListeAsync(CacheKeys.SirketAll);
			ViewBag.OfferDurumListe = await _durumCache.GetListeAsync(CacheKeys.OfferDurumAll);

		}

		private async Task ViewBagPartialListeDoldur()
		{


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
			var paged = await _requestService.UrunListeleAsync(page, pageSize, ct, (int)EnumOfferDurum.TeklifAsamasinda);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = paged.Items.ToList(),
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
			var paged = await _requestService.UrunListeleAsync(page, pageSize, ct, (int)EnumOfferDurum.TeklifOnayBekliyor);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		[HttpPost]
		public async Task<IActionResult> Index(OfferVM modelv)
		{
			ViewBag.Modul = ModulAd;
			//await ViewBagListeDoldur();
			var model = new OfferVM
			{
				OfferVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}


		public async Task<IActionResult> Detail(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;
			if (model.OfferDurumID == (int)EnumOfferDurum.TeklifAsamasinda)
			{
				var offer = new OfferVM
				{
					RequestUrunID = Id
				};
				model.OfferVMListe = await _service.VeriListele(offer);
				return View(model);
			}

			return RedirectToAction("Index");

		}
		[HttpPost]
		public async Task<IActionResult> Detail(RequestUrunlerVM modelv)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(modelv.ID);
			model.Aciklama = modelv.Aciklama;
			var sonuc = await _requestService.RequestUrunGuncelle(model);


			model.UserID = _userId;
			if (model.OfferDurumID == (int)EnumOfferDurum.TeklifAsamasinda)
			{
				var offer = new OfferVM
				{
					RequestUrunID = modelv.ID
				};
				model.OfferVMListe = await _service.VeriListele(offer);
				return View(model);
			}

			return RedirectToAction("Index");

		}
		[Authorize(Policy = "TeklifKabul")]
		public async Task<IActionResult> OnayDetail(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;

			var offer = new OfferVM
			{
				RequestUrunID = Id
			};
			model.OfferVMListe = await _service.VeriListele(offer);
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{

			var modelc = await _service.VeriGetir(VeriID);
			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			modelc.ControllerName = "Offer";
			modelc.ModalTitle = "Talep Bilgileri";
			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}
		public async Task<IActionResult> VeriGoruntule2(int VeriID = 0, string view = "")
		{
			ViewBag.Modul = ModulAd;
			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();

			modelc.ControllerName = "Offer";
			modelc.ModalTitle = "Talep Bilgileri";

			modelc.UserID = _userId;
			return View(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(OfferVM model)
		{
			model.TarihSaat = DateTime.Now;
			model.FirmaID = model.MusteriID;

			var sonuc = await _service.VeriEkleAsync(model);

			if (sonuc)
			{
				return RedirectToAction("Detail", new { Id = model.RequestUrunID });
			}
			else
			{
				return BadRequest("Kaydetme başarısız!");
			}
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(OfferVM model)
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

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> Teklif(int requestUrunID)
		{
			var requestUrun = await _requestService.RequestUrunGetir(requestUrunID);

			var modelc = new OfferVM();
			modelc.ModalTitle = "Teklif Bilgileri";
			modelc.TeslimTarihi = DateTime.Now;
			modelc.RequestUrunID = requestUrunID;
			modelc.Miktar = requestUrun.MiktarSon;
			modelc.UserID = _userId;
			modelc.UrunKod = requestUrun.UrunKod;
			modelc.UrunAd = requestUrun.UrunAd;

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			modelc.ControllerName = "Offer";
			return PartialView("_Teklif", modelc);
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> Send(int Id)
		{
			var requestUrun = await _requestService.RequestUrunGetir(Id);
			requestUrun.OfferDurumID = (int)EnumOfferDurum.TeklifOnayBekliyor;

			var sonuc = await _requestService.RequestUrunGuncelle(requestUrun);
			var teklifler = await _service.VeriListele(new OfferVM { RequestUrunID = Id });

			if (sonuc)
			{
				var _urun = requestUrun;
				var users = _context.MailNotificationUsers
							.Where(x => x.Type == MailNotificationType.Teklif)
							.Select(x => x.User.Email)
							.ToList();
				users.Add(_context.Users.Where(x => x.Id == requestUrun.TalepEdenID).Select(x => x.Email).FirstOrDefault());
				users.Add(_context.Users.Where(x => x.Id == _userId).Select(x => x.Email).FirstOrDefault());

				var mesajBody = $@"
								<h3>Talep Detayları</h3>

								<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse; width:100%; font-family:Arial;'>
									<tr>
										<th align='left'>Talep Takip No</th>
										<td>{_urun.TTN}</td>
									</tr>
									<tr>
										<th align='left'>Talep Oluşturan</th>
										<td>{_urun.TalepEdenAd}</td>
									</tr>
									<tr>
										<th align='left'>Talep Oluşturulma Tarihi</th>
										<td>{_urun.TalepEdenTarihSaat.ToLongTimeString()}</td>
									</tr>
									<tr>
										<th align='left'>Talep Kabul Eden</th>
										<td>{_urun.KabulEdenAd}</td>
									</tr>
<tr>
										<th align='left'>Talep Kabul Tarihi</th>
										<td>{_urun.KabulEdenTarihSaat.ToLongTimeString()}</td>
									</tr>
									<tr>
										<th align='left'>Ürün</th>
										<td>{_urun.UrunAd}</td>
									</tr>
									<tr>
										<th align='left'>Ürün Açıklama</th>
										<td>{_urun.Aciklama}</td>
									</tr>
									<tr>
										<th align='left'>Miktar</th>
										<td>{_urun.Miktar}</td>
									</tr>
									<tr>
										<th align='left'>Birim</th>
										<td>{_urun.BirimAd}</td>
									</tr>
								</table>
<br />

<h3>Teklifler</h3>

<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse; width:100%; font-family:Arial;'>
    <tr>
        <th>Firma</th>
        <th>Fiyat (₺)</th>
        <th>Fiyat (Döviz)</th>
        <th>Vade</th>
        <th>Ödeme Türü</th>
        <th>Teslim Tarihi</th>
    </tr>
							";
				double kur = 1;
				string doviz = "";
				foreach (var teklif in teklifler)
				{
					var odemeTurAd = "";

					if (teklif.DovizTurID == 1)
					{
						kur = 1; doviz = "₺";
					}
					else if (teklif.DovizTurID == 2)
					{
						kur = Convert.ToDouble(teklif.UsdRate); doviz = "$";
					}
					else
					{
						kur = Convert.ToDouble(teklif.EurRate); doviz = "€";
					}

					if (teklif.OdemeTurID == 1)
					{
						odemeTurAd = "Havale";

					}
					else if (teklif.OdemeTurID == 2)
					{
						odemeTurAd = "Kredi Kartı";
					}
					else if (teklif.OdemeTurID == 3)
					{
						odemeTurAd = "Çek";
					}
					mesajBody += $@"
						<tr>
							<td>{teklif.FirmaAd}</td>
							<td style='text-align:right;'>₺ {(teklif.Fiyat * kur).ToString("N2")} </td>
							<td style='text-align:right;'>{doviz} {(teklif.Fiyat).ToString("N2")} </td>
							<td>{teklif.Vade}</td>
							<td>{odemeTurAd}</td>
							<td>{teklif.TeslimTarihi.ToShortDateString()}</td>
						</tr>
						";
				}
				mesajBody += "</table>";

				foreach (var mail in users)
				{
					BackgroundJob.Enqueue<IMailJobService>(x =>
						x.SendMailAsync(mail ?? string.Empty, "Satınalma Portalı Bilgilendirme: Teklifler girildi ve onaya gönderildi.", mesajBody));
				}
			}




			return RedirectToAction("Index");
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> OncekiTeklifler(int requestUrunID)
		{
			var model = await _requestService.RequestUrunGetir(requestUrunID);
			var offer = new OfferVM
			{
				UrunID = model.UrunID,
				PageSize = 10,
			};

			model.OfferVMListe = await _service.VeriListele(offer);

			return PartialView("_OncekiTeklifler", model);
		}

		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> KabulEt(int Id, int requestUrunId, string kabulNot)
		{
			var requestUrun = await _requestService.RequestUrunGetir(requestUrunId);
			requestUrun.OfferDurumID = (int)EnumOfferDurum.TeklifOnaylandi;
			requestUrun.KabulNot = kabulNot;
			var sonuc = await _requestService.RequestUrunGuncelle(requestUrun);

			var modelc = await _service.VeriGetir(Id);
			modelc.IsSelected = true;
			modelc.DurumID = (int)EnumOrderDurum.SiparisAsamasinda;
			bool cevap = await _service.VeriEkleAsync(modelc);
			var teklifler = await _service.VeriListele(new OfferVM { RequestUrunID = requestUrunId });


			if (sonuc)
			{
				var _urun = requestUrun;
				var users = _context.MailNotificationUsers
							.Where(x => x.Type == MailNotificationType.Teklif)
							.Select(x => x.User.Email)
							.ToList();

				users.Add(_context.Users.Where(x => x.Id == requestUrun.TalepEdenID).Select(x => x.Email).FirstOrDefault());
				users.Add(_context.Users.Where(x => x.Id == _userId).Select(x => x.Email).FirstOrDefault());

				var mesajBody = $@"
								<h3>Talep Detayları</h3>

								<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse; width:100%; font-family:Arial;'>
									<tr>
										<th align='left'>Talep Takip No</th>
										<td>{_urun.TTN}</td>
									</tr>
									<tr>
										<th align='left'>Talep Oluşturan</th>
										<td>{_urun.TalepEdenAd}</td>
									</tr>
									<tr>
										<th align='left'>Talep Oluşturulma Tarihi</th>
										<td>{_urun.TalepEdenTarihSaat.ToLongTimeString()}</td>
									</tr>
									<tr>
										<th align='left'>Talep Kabul Eden</th>
										<td>{_urun.KabulEdenAd}</td>
									</tr>
<tr>
										<th align='left'>Talep Kabul Tarihi</th>
										<td>{_urun.KabulEdenTarihSaat.ToLongTimeString()}</td>
									</tr>
									<tr>
										<th align='left'>Ürün</th>
										<td>{_urun.UrunAd}</td>
									</tr>
									<tr>
										<th align='left'>Ürün Açıklama</th>
										<td>{_urun.Aciklama}</td>
									</tr>
									<tr>
										<th align='left'>Miktar</th>
										<td>{_urun.Miktar}</td>
									</tr>
									<tr>
										<th align='left'>Birim</th>
										<td>{_urun.BirimAd}</td>
									</tr>
									<tr>
										<th align='left'>Ret Notu</th>
										<td>{_urun.RedNot}</td>
									</tr>
									<tr>
										<th align='left'>Kabul Notu</th>
										<td>{_urun.KabulNot}</td>
									</tr>
								</table>
<br />

<h3>Teklifler</h3>

<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse; width:100%; font-family:Arial;'>
    <tr>
        <th>Firma</th>
        <th>Fiyat (₺)</th>
        <th>Fiyat (Döviz)</th>
        <th>Vade</th>
        <th>Ödeme Türü</th>
        <th>Teslim Tarihi</th>
        <th>Durum</th>
    </tr>
							";
				double kur = 1;
				string doviz = "";
				string durum = "";
				foreach (var teklif in teklifler)
				{
					var odemeTurAd = "";

					if (teklif.DovizTurID == 1)
					{
						kur = 1; doviz = "₺";
					}
					else if (teklif.DovizTurID == 2)
					{
						kur = Convert.ToDouble(teklif.UsdRate); doviz = "$";
					}
					else
					{
						kur = Convert.ToDouble(teklif.EurRate); doviz = "€";
					}

					if (teklif.OdemeTurID == 1)
					{
						odemeTurAd = "Havale";

					}
					else if (teklif.OdemeTurID == 2)
					{
						odemeTurAd = "Kredi Kartı";
					}
					else if (teklif.OdemeTurID == 3)
					{
						odemeTurAd = "Çek";
					}
					if (teklif.IsSelected)
					{
						durum = "Kabul Edildi";
					}
					else
					{
						durum = "";
					}

					mesajBody += $@"
						<tr>
							<td>{teklif.FirmaAd}</td>
							<td style='text-align:right;'>₺ {(teklif.Fiyat * kur).ToString("N2")} </td>
							<td style='text-align:right;'>{doviz} {(teklif.Fiyat).ToString("N2")} </td>
							<td>{teklif.Vade}</td>
							<td>{odemeTurAd}</td>
							<td>{teklif.TeslimTarihi.ToShortDateString()}</td>
							<td>{durum}</td>
						</tr>
						";
				}
				mesajBody += "</table>";

				foreach (var mail in users)
				{
					BackgroundJob.Enqueue<IMailJobService>(x =>
						x.SendMailAsync(mail, "Satınalma Portalı Bilgilendirme: Teklif kabul edildi.", mesajBody));
				}
			}




			return Ok("Başarılı");
		}


		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> TeklifRed(int requestUrunID)
		{
			var requestUrun = await _requestService.RequestUrunGetir(requestUrunID);


			return PartialView(requestUrun);
		}


		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> TeklifRed(RequestUrunlerVM model)
		{
			var requestUrun = await _requestService.RequestUrunGetir(model.ID);
			requestUrun.OfferDurumID = (int)EnumOfferDurum.TeklifAsamasinda;
			requestUrun.RedNot = model.RedNot;
			var sonuc = await _requestService.RequestUrunGuncelle(requestUrun);
			var teklifler = await _service.VeriListele(new OfferVM { RequestUrunID = model.ID });
			if (sonuc)
			{
				var _urun = requestUrun;
				var users = _context.MailNotificationUsers
							.Where(x => x.Type == MailNotificationType.Teklif)
							.Select(x => x.User.Email)
							.ToList();

				users.Add(_context.Users.Where(x => x.Id == requestUrun.TalepEdenID).Select(x => x.Email).FirstOrDefault());
				users.Add(_context.Users.Where(x => x.Id == _userId).Select(x => x.Email).FirstOrDefault());

				var mesajBody = $@"
								<h3>Talep Detayları</h3>

								<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse; width:100%; font-family:Arial;'>
									<tr>
										<th align='left'>Talep Takip No</th>
										<td>{_urun.TTN}</td>
									</tr>
									<tr>
										<th align='left'>Talep Oluşturan</th>
										<td>{_urun.TalepEdenAd}</td>
									</tr>
									<tr>
										<th align='left'>Talep Oluşturulma Tarihi</th>
										<td>{_urun.TalepEdenTarihSaat.ToLongTimeString()}</td>
									</tr>
									<tr>
										<th align='left'>Talep Kabul Eden</th>
										<td>{_urun.KabulEdenAd}</td>
									</tr>
<tr>
										<th align='left'>Talep Kabul Tarihi</th>
										<td>{_urun.KabulEdenTarihSaat.ToLongTimeString()}</td>
									</tr>
									<tr>
										<th align='left'>Ürün</th>
										<td>{_urun.UrunAd}</td>
									</tr>
									<tr>
										<th align='left'>Ürün Açıklama</th>
										<td>{_urun.Aciklama}</td>
									</tr>
									<tr>
										<th align='left'>Miktar</th>
										<td>{_urun.Miktar}</td>
									</tr>
									<tr>
										<th align='left'>Birim</th>
										<td>{_urun.BirimAd}</td>
									</tr>
	<tr>
										<th align='left'>Teklif Red Notu</th>
										<td>{_urun.RedNot}</td>
									</tr>
								</table>
<br />

<h3>Teklifler</h3>

<table border='1' cellpadding='8' cellspacing='0' style='border-collapse:collapse; width:100%; font-family:Arial;'>
    <tr>
        <th>Firma</th>
        <th>Fiyat (₺)</th>
        <th>Fiyat (Döviz)</th>
        <th>Vade</th>
        <th>Ödeme Türü</th>
        <th>Teslim Tarihi</th>
    </tr>
							";
				double kur = 1;
				string doviz = "";
				foreach (var teklif in teklifler)
				{
					var odemeTurAd = "";

					if (teklif.DovizTurID == 1)
					{
						kur = 1; doviz = "₺";
					}
					else if (teklif.DovizTurID == 2)
					{
						kur = Convert.ToDouble(teklif.UsdRate); doviz = "$";
					}
					else
					{
						kur = Convert.ToDouble(teklif.EurRate); doviz = "€";
					}

					if (teklif.OdemeTurID == 1)
					{
						odemeTurAd = "Havale";

					}
					else if (teklif.OdemeTurID == 2)
					{
						odemeTurAd = "Kredi Kartı";
					}
					else if (teklif.OdemeTurID == 3)
					{
						odemeTurAd = "Çek";
					}
					mesajBody += $@"
						<tr>
							<td>{teklif.FirmaAd}</td>
							<td style='text-align:right;'>₺ {(teklif.Fiyat * kur).ToString("N2")} </td>
							<td style='text-align:right;'>{doviz} {(teklif.Fiyat).ToString("N2")} </td>
							<td>{teklif.Vade}</td>
							<td>{odemeTurAd}</td>
							<td>{teklif.TeslimTarihi.ToShortDateString()}</td>
						</tr>
						";
				}
				mesajBody += "</table>";

				foreach (var mail in users)
				{
					BackgroundJob.Enqueue<IMailJobService>(x =>
						x.SendMailAsync(mail, "Satınalma Portalı Bilgilendirme: Teklifler geri gönderildi.", mesajBody));
				}
			}



			return RedirectToAction("Onay");
		}


		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> OncekiTeklif(int teklifId, int RequestUrunID)
		{
			var requestUrun = await _requestService.RequestUrunGetir(RequestUrunID);

			var modelc = await _service.VeriGetir(teklifId);
			modelc.ModalTitle = "Teklif Bilgileri";

			modelc.RequestUrunID = RequestUrunID;
			modelc.ID = 0;

			ViewBag.Kurlar = await _tcmbService.DovizKuruGetir();
			modelc.ControllerName = "Offer";
			return PartialView("_Teklif", modelc);
		}

		public async Task<IActionResult> Print(int offerId)
		{
			var offer = await _service.VeriGetir(offerId);

			if (offer == null)
				return NotFound();

			return View("Print", offer);
		}

		public async Task<IActionResult> Arsiv(int page = 1, int pageSize = 10, CancellationToken ct = default)
		{

			ViewBag.Modul = ModulAd;
			await ViewBagListeDoldur();
			var paged = await _requestService.OfferListeleAsync(page, pageSize, ct);

			var model = new RequestUrunlerVM
			{
				RequestUrunlerVMListe = paged.Items.ToList(),
				PageIndex = paged.PageIndex,
				PageSize = paged.PageSize,
				TotalCount = paged.TotalCount
			};

			return View(model);
		}

		public async Task<IActionResult> ArsivGoruntule(int Id)
		{
			ViewBag.Modul = ModulAd;

			var model = await _requestService.RequestUrunGetir(Id);

			model.UserID = _userId;

			var offer = new OfferVM
			{
				RequestUrunID = Id
			};
			model.OfferVMListe = await _service.VeriListele(offer);
			return PartialView(model);
		}

		public async Task<IActionResult> TeklifIptal(int Id)
		{
			var requestUrun = await _requestService.RequestUrunGetir(Id);
			requestUrun.OfferDurumID = (int)EnumOfferDurum.TeklifIptal;
			requestUrun.OnayliMi = false;
			requestUrun.IsActive = false;
			requestUrun.IsDelete = true;
			requestUrun.DeleteDate = DateTime.Now;
			requestUrun.DeleteUserID = _userId;
			var sonuc = await _requestService.RequestUrunGuncelle(requestUrun);

			var urunler = await _requestService.RequestUrunlerGetir(requestUrun.RequestID);

			int toplamUrunSayisi = urunler.Count();
			int iptalUrunSayisi=urunler.Where(x => x.OnayliMi==false && x.IsDelete==true && x.IsActive==false).Count();
			int aktifUrunSayisi = urunler.Where(x => x.OnayliMi == true && x.IsDelete == false && x.IsActive == true).Count();

			if(toplamUrunSayisi==iptalUrunSayisi)
			{
				var request = await _requestService.VeriGetir(requestUrun.RequestID);
				request.DurumID = (int)EnumRequestDurum.İptal;
				await _requestService.VeriEkleAsync(request);
			}

			




			if (sonuc)
			{
				return RedirectToAction("Index");
			}
			else
			{
				return BadRequest("Teklif iptal edilemedi.");
			}
		}
	}
}
