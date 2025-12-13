 
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

	public class Teklif : BaseEntity
	{
		public int? MusteriID { get; set; }
		public int? GorevliID { get; set; }
		public int? FirsatID { get; set; } 
		public int? SiparisID { get; set; }
		public int? DurumID { get; set; }
		public int? TurID { get; set; }
		public string? SorumluID { get; set; }
		public bool IsDone { get; set; }
		public DateTime? TarihSaat { get; set; }
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		public double KdvToplam { get; set; }
		public double IskontoToplam { get; set; }
		public double SatirIskontoToplam { get; set; }
		public double TeklifToplam { get; set; }
		public double BrutToplam { get; set; }
		public double Toplam { get; set; }
		public double DolarKuru { get; set; }
		public double EuroKuru { get; set; }
		public bool IsLocked { get; set; } = false;

	}

	public class TeklifVM : BaseVM
	{
		public string? MusteriAd { get; set; }
		public int? MusteriID { get; set; }
		public string? GorevliAd { get; set; }
		public int? GorevliID { get; set; }
		[Display(Name = "Fırsat")]
		public string? FirsatAd { get; set; }
		public int? FirsatID { get; set; }
	 
		[Display(Name = "Sipariş")]
		public string? SiparisAd { get; set; }
		public int? SiparisID { get; set; }
		public int? DurumID { get; set; }
		[Display(Name = "Son Durum")]
		public string? DurumAd { get; set; }
		public string? SorumluID { get; set; }
		[Display(Name = "Sorumlu")]
		public string? SorumluAd { get; set; }
		public bool IsDone { get; set; }
		public DateTime? TarihSaat { get; set; }
		public string? Not { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<TeklifVM> TeklifVMListe { get; set; }
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
        public double SatirIskontoToplam { get; set; }
        public double BrutToplam { get; set; }
        public double Toplam { get; set; }
        public bool IsLocked { get; set; } = false;
	}

	public class TeklifDurum : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
	public class TeklifTur : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}
    public class TeklifIskonto : BaseEntity
    {
        public int TeklifID { get; set; }
        public string? Ad { get; set; }
        public string? Aciklama { get; set; }
        public double Oran { get; set; }
    }

}
