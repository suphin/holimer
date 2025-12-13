 
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

	public class SiparisIade : BaseEntity
	{
		public int? MusteriID { get; set; }
		public int? GorevliID { get; set; }
		public int? FirsatID { get; set; }
		public int? TeklifID { get; set; }
		public int? DurumID { get; set; }
		public int? SebepID { get; set; }
		public int? PlatformID { get; set; }
		public int? TurID { get; set; }
		public string? SorumluID { get; set; }
		public bool IsDone { get; set; }
		public bool IadeSonuc { get; set; }
		public DateTime TarihSaat { get; set; } = DateTime.Now;
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		public double KdvToplam { get; set; }
		public double IskontoToplam { get; set; }
		public double SiparisToplam { get; set; }
		public double DolarKuru { get; set; }
		public double EuroKuru { get; set; }
		public bool IsLocked { get; set; } = false;
		public string? SiparisNo { get; set; }
	}

	public class SiparisIadeVM : BaseVM
	{
		public string? MusteriAd { get; set; }
		public int? MusteriID { get; set; }
		public string? GorevliAd { get; set; }
		public int? GorevliID { get; set; }
		[Display(Name = "Fırsat")]
		public string? FirsatAd { get; set; }
		public int? FirsatID { get; set; }
		public int? TeklifID { get; set; }
		[Display(Name = "Teklif")]
		public string? TeklifAd { get; set; }
		public int? SebepID { get; set; }
		[Display(Name = "İade Sebebi")]
		public string? SebepAd { get; set; }
		public int? PlatformID { get; set; }
		public string? PlatformAd { get; set; }
		public int? DurumID { get; set; }
		[Display(Name = "İade Durumu")]
		public string? DurumAd { get; set; }
		public string? SorumluID { get; set; }
		[Display(Name = "Sorumlu")]
		public string? SorumluAd { get; set; }
		public bool IsDone { get; set; }
		public bool IadeSonuc { get; set; }
		public DateTime? TarihSaat { get; set; }
		public string? Not { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<SiparisIadeVM> SiparisIadeVMListe { get; set; }
		public virtual Musteriler? Musteri { get; set; }
		public string? MusteriTelefon { get; set; }
		public string? MusteriEposta { get; set; }
		public string? MusteriAdres { get; set; }
		public int? TurID { get; set; }
		[Display(Name = "Teklif Şekli")]
		public string? TurAd { get; set; }
		public double DolarKuru { get; set; }
		public double EuroKuru { get; set; }
		public double KdvToplam { get; set; }
		public double IskontoToplam { get; set; }
		public double SiparisToplam { get; set; }
		public bool IsLocked { get; set; } = false;
		public string? SiparisNo { get; set; }
	}

	public class SiparisIadeDurum : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class SiparisIadePlatform: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class SiparisIadeSebep: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class SiparisIadeTur : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}


	public class SiparisIadeUrunler : BaseEntity
	{
		public int UrunID { get; set; }
		public int SiparisID { get; set; }
		public int TeklifID { get; set; }
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double Kdv { get; set; }
		public double Iskonto { get; set; }

	}

	public class SiparisIadeUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public int SiparisID { get; set; }
		public int TeklifID { get; set; }
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public double Kdv { get; set; }
		public double Iskonto { get; set; }
		public string UrunAd { get; set; }
		public string UrunKod { get; set; }
		public string? BirimAd { get; set; }
		public int? BirimID { get; set; }
		public string? TipAd { get; set; }
		public int? TipID { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double Toplam { get; set; }
		public double KdvTutar { get; set; }
		public double IskontoTutar { get; set; }
		public double GenelToplam { get; set; }
		public string Aciklama { get; set; }
		public List<SiparisIadeUrunlerVM> SiparisIadeUrunlerVMListe { get; set; }

	}
}
