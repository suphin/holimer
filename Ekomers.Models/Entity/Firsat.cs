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
	 
	public class Firsat : BaseEntity
	{
		public int? MusteriID { get; set; }
		public int? GorevliID { get; set; }
		public int? TeklifID { get; set; }
		public int? SiparisID { get; set; }
		public int? DurumID { get; set; }
		public string? SorumluID { get; set; }
		public bool IsDone { get; set; }
		public DateTime? TarihSaat { get; set; }
		public string? Not { get; set; }
		public string? Aciklama { get; set; }
		public bool IsLocked { get; set; } = false;

	}

	public class FirsatVM : BaseVM
	{
		public string? MusteriAd { get; set; }
		public int? MusteriID { get; set; }
		public string? GorevliAd { get; set; }
		public int? GorevliID { get; set; }
		[Display(Name = "Fırsat")]
		public string? FirsatAd { get; set; }
		public int? FirsatID { get; set; }
		[Display(Name = "Teklif")]
		public string? TeklifAd { get; set; }
		public int? TeklifID { get; set; }
		[Display(Name = "Sipariş")]
		public string? SiparisAd { get; set; }
		public int? SiparisID { get; set; }
		public int? DurumID { get; set; }
		[Display(Name ="Son Durum")]
		public string? DurumAd { get; set; }
		public string? SorumluID { get; set; }
		[Display(Name = "Sorumlu")]
		public string? SorumluAd { get; set; }
		public bool IsDone { get; set; }
		public DateTime? TarihSaat { get; set; }
		public string? Not { get; set; }
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; }
		public List<FirsatVM> FirsatVMListe { get; set; }
		public virtual Musteriler? Musteri { get; set; }
		public string? MusteriTelefon { get; set; }
		public string? MusteriEposta { get; set; }
		public string? MusteriAdres { get; set; }
		public bool IsLocked { get; set; } = false;
	}

	public class FirsatDurum: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}



}
