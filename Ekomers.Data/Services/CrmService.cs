using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class CrmService : ICrmService
	{
		private readonly ILoggerService _loggerService;
		private readonly UserManager<Kullanici> _userManager;
	 

		private readonly IRepository<Aktivite> _aktiviteRepository;
		private readonly IRepository<Firsat> _firsatRepository;
		private readonly IRepository<Teklif> _teklifRepository;
		private readonly IRepository<Siparis> _siparisRepository;
		private readonly IRepository<Malzeme> _malzemeRepository;
		private readonly IRepository<CrmHedefler> _hedeflerRepository;

		public CrmService(ILoggerService loggerService,
			 
			UserManager<Kullanici> userManager,
			IRepository<Firsat> firsatRepository,
			IRepository<Teklif> teklifRepository,
			IRepository<Siparis> siparisRepository,
			IRepository<Malzeme> malzemeRepository,
			IRepository<Aktivite> aktiviteRepository,
			IRepository<CrmHedefler> hedeflerRepository)
		{
			_loggerService = loggerService;
		 
			_userManager = userManager;
			_firsatRepository = firsatRepository;
			_teklifRepository = teklifRepository;
			_siparisRepository = siparisRepository;
			_malzemeRepository = malzemeRepository;
			_aktiviteRepository = aktiviteRepository;
			_hedeflerRepository = hedeflerRepository;
		}
		public async Task<List<CrmKullaniciVM>> GetCrmKullanici(int yil)
		{
			var users =   _userManager.Users.Where(P => P.IsCrmUser).OrderBy(a => a.AdSoyad).ToList();
			var model = new List<CrmKullaniciVM>();

			foreach (var item in users)
			{
				var toplamSiparis = _siparisRepository
		  .GetAll2(p => p.SorumluID == item.Id)
		  .Sum(p => p.SiparisToplam);

				// Kullanıcının ilgili yıldaki hedeflerini çek
				var hedefTutar = _hedeflerRepository.GetAll2(h => h.UserID == item.Id && h.Yil == yil)
					.Sum(h => h.Tutar);

				model.Add(new CrmKullaniciVM { 
					AdSoyad = item.AdSoyad,
					BaslangicTarih=new DateTime(DateTime.Now.Year,1,1),
					HedefTarih=new DateTime (DateTime.Now.Year,12, 31),
					ToplamSiparisTutari = _siparisRepository.GetAll2(p => p.SorumluID ==item.Id).Sum(p=>p.SiparisToplam),
					SiparisSayisi = _siparisRepository.GetAll2(p => p.SorumluID == item.Id).Count(),
					TeklifSayisi =  _teklifRepository.GetAll2(p => p.SorumluID==item.Id).Count(),
					FirsatSayisi =  _firsatRepository.GetAll2(p => p.SorumluID== item.Id).Count(),
					AktiviteSayisi =  _aktiviteRepository.GetAll2(p => p.SorumluID == item.Id).Count(),
					 HedefTutar = hedefTutar,
					HedefeOran = hedefTutar > 0
				? Math.Round((toplamSiparis / hedefTutar) * 100, 2)
				: 0
				});
			}

			return model;
		}

		public async Task<CrmDashbordVM> GetDashBoard(int yil)
		{
			 

			var model = new CrmDashbordVM
			{
				SiparisSayisi = _siparisRepository.GetAll2().Count(),
				AktiviteSayisi = _aktiviteRepository.GetAll2().Count(),
				TeklifSayisi = _teklifRepository.GetAll2().Count(),
				FirsatSayisi = _firsatRepository.GetAll2().Count(),
				MalzemeSayisi = _malzemeRepository.GetAll2().Count(),
				ToplamSiparisTutari = _siparisRepository.GetAll2().Sum(p => p.SiparisToplam)

			};


			// --- Aylık Sipariş Toplamları (yıla göre) ---
			var siparisAyGruplari = await _siparisRepository
				.GetAll2(p => p.TarihSaat.Year == yil)              // <- tarih alanını sende neyse ona göre değiştir
				.GroupBy(p => p.TarihSaat.Month)
				.Select(g => new
				{
					Ay = g.Key,
					Toplam = g.Sum(x => (decimal)x.SiparisToplam)
				})
				.ToListAsync();

			// Doldur (boş aylar 0 kalsın)
			foreach (var r in siparisAyGruplari)
				model.AylikSiparisToplam[r.Ay - 1] = r.Toplam;

			// --- Aylık Aktivite Sayıları (yıla göre) ---
			var aktiviteAyGruplari = await _aktiviteRepository
				.GetAll2(a => a.TarihSaat.Year == yil)                      // <- aktivite tarih alanı
				.GroupBy(a => a.TarihSaat.Month)
				.Select(g => new
				{
					Ay = g.Key,
					Adet = g.Count()
				})
				.ToListAsync();

			foreach (var r in aktiviteAyGruplari)
				model.AylikAktiviteSayisi[r.Ay - 1] = r.Adet;

			// --- Aylık Sipariş Sayıları (yıla göre) ---
			var siparisSayilariAyGruplari = await _siparisRepository
				.GetAll2(a => a.TarihSaat.Year == yil)                      // <- aktivite tarih alanı
				.GroupBy(a => a.TarihSaat.Month)
				.Select(g => new
				{
					Ay = g.Key,
					Adet = g.Count()
				})
				.ToListAsync();

			foreach (var r in siparisSayilariAyGruplari)
				model.AylikSiparisSayisi[r.Ay - 1] = r.Adet;


			model.CrmKullaniciListe = await GetCrmKullanici(yil);
			return model;

		}
	}
}
