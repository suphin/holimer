using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	 
	public class Offer : BaseEntity
	{
		
		
		
		public int RequestUrunID { get; set; }
		public int? DurumID { get; set; }
		public int? TurID { get; set; }
		public bool IsDone { get; set; }
		public bool IsSelected { get; set; }
		public DateTime TarihSaat { get; set; }=DateTime.Now;
		
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public double UsdRate { get; set; }
		public double EurRate { get; set; }

		public int DovizTurID { get; set; } 
		public int Vade { get; set; }
		public int OdemeTurID { get; set; }

		public DateTime? OdemeTarihi { get; set; }

		public string? Firma { get; set; }
		public int? FirmaID { get; set; }
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		
		public bool IsLocked { get; set; } = false;		 
		public DateTime TeslimTarihi { get; set; }	 		 
		public string? TeslimAdres { get; set; }
	}

	public class OfferVM : BaseVM
	{
		public string TTN { get; set; }
		public int RequestUrunID { get; set; }
		public int OfferDurumID { get; set; }
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public int Vade { get; set; }
		[Display(Name = "Ödeme Türü")]
		public int OdemeTurID { get; set; }
		public string? DovizTur { get; set; }
		public string? FirmaAd { get; set; }
		public int? FirmaID { get; set; }
		public int? MusteriID { get; set; }
		public int? DurumID { get; set; }
		[Display(Name = "Son Durum")]
		public string? DurumAd { get; set; }
		public string? DurumClass { get; set; }
		[Display(Name = "Döviz Türü")]
		public int DovizTurID { get; set; }
		public int UrunID { get; set; }
		public string? UrunKod { get; set; }
		public string? UrunAd { get; set; }
		public double UrunKdv { get; set; }
		public double UsdRate { get; set; }
		public double EurRate { get; set; }

		public bool IsDone { get; set; }
		public bool IsSelected { get; set; }
		public DateTime? TarihSaat { get; set; }
		public DateTime? OdemeTarihi { get; set; }
		[Display(Name = "Talep Tarihi")]
		public DateTime RequestDate { get; set; }
		public DateTime FaturaTarihi { get; set; }
		public string? Not { get; set; }
		public string? TalepNot { get; set; }
		public virtual Musteriler? Firma { get; set; }


		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<OfferVM> OfferVMListe { get; set; } 
		
	
		public int? TurID { get; set; }
		[Display(Name = "Talep Türü")]
		public string? TurAd { get; set; }
		public int? SirketID { get; set; }
		[Display(Name = "Şirket")]
		public string? SirketAd { get; set; }
		public virtual Sirketler? Sirket { get; set; }
		public bool IsLocked { get; set; } = false;

		[Display(Name = "Teslim Tarihi")]
		public DateTime TeslimTarihi { get; set; }
		public string? TeslimAdres { get; set; }


		public int requestID { get; set; }
		public string requestUserID { get; set; }
		public string? requestUserName { get; set; }
		[Display(Name = "Satınalma Müdürü")]
		public string? SatinalmaMuduru { get; set; }
		[Display(Name = "Muhasebe Müdürü")]
		public string? MuhasebeMuduru { get; set; }
		[Display(Name = "Genel Müdür")]
		public string? GenelMudur { get; set; }
		[Display(Name = "Genel Koordinatör")]
		public string? GenelKoordinator { get; set; }
		[Display(Name = "Şirket Adres")]
		public string? SirketAdres { get; set; }
		[Display(Name = "Şirket Vergi Dairesi")]
		public string? SirketVergiDairesi { get; set; }
		[Display(Name = "Şirket Vergi No")]
		public string? SirketVergiNo { get; set; }
		[Display(Name = "Şirket Web Sitesi")]
		public string? SirketWebSitesi { get; set; }
	}

	public class OfferDurum : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
		public string? Class { get; set; }
	}
	public class OfferTur : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
 
}
