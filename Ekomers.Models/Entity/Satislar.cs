 
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

	public class Satislar : BaseEntity
	{
		public int? MusteriID { get; set; }
		public int? GorevliID { get; set; } 
		public string? PersonelID { get; set; } 
	
		public int? DurumID { get; set; }
		 
		public int? TurID { get; set; }
		public string? SorumluID { get; set; }
		public bool IsDone { get; set; }
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
		public bool CariTipi { get; set; }
	}

	public class SatislarVM : BaseVM
	{
		[Display(Name = "Müşteri Ad")]
		public string? MusteriAd { get; set; }
		public int? MusteriID { get; set; }
		public string? GorevliAd { get; set; }
		public string? PersonelAd { get; set; }
		public string? PersonelID { get; set; }
		public int? GorevliID { get; set; } 
		public bool CariTipi { get; set; } 
		public int? DurumID { get; set; }
		[Display(Name = "İade Durumu")]
		public string? DurumAd { get; set; }
		public string? SorumluID { get; set; }
		[Display(Name = "Sorumlu")]
		public string? SorumluAd { get; set; }
		public bool IsDone { get; set; }
		public DateTime? TarihSaat { get; set; }
		public string? Not { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<SatislarVM> SatislarVMListe { get; set; }
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

	public class SatislarDurum : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class SatislarPlatform: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class SatislarSebep: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class SatislarTur : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}


	public class SatislarUrunler : BaseEntity
	{
		public int UrunID { get; set; }
		public int SiparisID { get; set; } 
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double Kdv { get; set; }
		public double Iskonto { get; set; }

	}

	public class SatislarUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public int SiparisID { get; set; }
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
		public List<SatislarUrunlerVM> SatislarUrunlerVMListe { get; set; }

	}
}
