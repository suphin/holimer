using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Envanter : BaseEntity
	{
		public string? Ad { get; set; }
		public int EnvanterDepartmanID {  get; set; }
		public int EnvanterBolumID {  get; set; }
		public int SirketID {  get; set; }
		public int TurID { get; set; } = 0;
		public int TipID { get; set; } = 0;
		public string Numara { get; set; }
		public string? Marka { get; set; } = string.Empty; 
		public string? Model { get; set; } = string.Empty;
		public string? SeriNo { get; set; } = string.Empty;
		public string? YerKodu { get; set; } = string.Empty;
		public DateTime AlimTarihi { get; set; } = DateTime.Now;
		public DateTime GarantiBas { get; set; } = DateTime.Now;
		public DateTime GarantiBit { get; set; } = DateTime.Now;
		public string? Aciklama { get; set; } = string.Empty;
		public double AlimFiyati { get; set; } = 0;
		public int DurumID { get; set; } = 0;
		public int DovizTuru { get; set; } = 0;
		public double DovizKuru { get; set; } = 0;
		public string? Fotograf { get; set; }
		public bool Zimmetli { get; set; } = false;

	}

	public class EnvanterVM : BaseVM
	{
		[Display(Name = "Envanter Adı")]
		public string? Ad { get; set; } 
		public int EnvanterDepartmanID { get; set; }
		[Display(Name = "Departman")]
		public string? Departman { get; set; } = string.Empty;
		public string? DepartmanKod { get; set; } = string.Empty;

		public int EnvanterBolumID { get; set; }
		[Display(Name = "Bölüm")]
		public string? Bolum { get; set; } = string.Empty;
		public string? BolumKod { get; set; } = string.Empty;
		public int SirketID { get; set; }
		[Display(Name = "Şirket")]
		public string Sirket { get; set; } = string.Empty;
		public int TurID { get; set; } = 0;
		[Display(Name = "Demirbaş Türü")]
		public string Tur { get; set; } = string.Empty;
		public string TurKod { get; set; } = string.Empty;

		public int TipID { get; set; } = 0;
		[Display(Name = "Demirbaş Tipi")]
		public string Tip { get; set; } = string.Empty;
		public string TipKod { get; set; } = string.Empty;


		public string Numara { get; set; }
		[Display(Name = "Marka")]
		public string? Marka { get; set; } = string.Empty;
		[Display(Name = "Model")]
		public string? Model { get; set; } = string.Empty;
		[Display(Name = "Seri No")]
		public string? SeriNo { get; set; } = string.Empty;
		[Display(Name = "Yer Kodu")]
		public string? YerKodu { get; set; } = string.Empty;
		[Display(Name = "Alım Tarihi")]
		public DateTime AlimTarihi { get; set; } = DateTime.Now;
		[Display(Name = "Garanti Başlangıç")]
		public DateTime GarantiBas { get; set; } = DateTime.Now;
		[Display(Name = "Garanti Bitiş")]
		public DateTime GarantiBit { get; set; } = DateTime.Now;
		[Display(Name = "Açıklama")]
		public string? Aciklama { get; set; } = string.Empty;
		[Display(Name = "Alım Fiyatı")]
		public double AlimFiyati { get; set; } = 0;
		public int DurumID { get; set; } = 0;
		[Display(Name = "Döviz Türü")]
		public int DovizTuru { get; set; } = 0;
		[Display(Name = "Döviz Kuru")]
		public double DovizKuru { get; set; } = 0;
		public List<EnvanterVM> EnvanterVMListe { get; set; }
		public string? Fotograf { get; set; }
		public IFormFile? Dosya { get; set; }
		public Zimmet Zimmet { get; set; }

	}

	public class EnvanterTur : BaseEntity
	{
		public string? Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
	}

	public class EnvanterTip: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
	}

	public class EnvanterDepartman: BaseEntity
	{
		public string? Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
	}

	public class EnvanterBolum : BaseEntity
	{
		public int EnvanterDepartmanID { get; set; }
		public string? Ad { get; set; }
		public string? Kod { get; set; }
		public string? Aciklama { get; set; }
	}
}
