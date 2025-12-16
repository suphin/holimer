using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Sozlesmeler:BaseEntity
	{
		public string? Aciklama { get; set; }
		public string? Baslik { get; set; }

		public int SirketID { get; set; }
		public int KonuID { get; set; }
		public int DurumID { get; set; }
		public int Taraf1ID { get; set; }
		public int Taraf2ID { get; set; }
		public string? Taraf2 { get; set; }
		public DateTime? BaslangicTarih { get; set; }
		public DateTime? BitisTarih { get; set; }
		public string? TutarAciklama { get; set; }
		public double? Tutar { get; set; }
		public string? Pdf { get; set; }
		public string? AnahtarKelimeler { get; set; }
	}
	public class SozlesmelerVM:BaseVM
	{
		[Display(Name = "Konu")]
		public string? Konu { get; set; }
		public int KonuID { get; set; }
		[Display(Name = "Durum")]
		public string? Durum { get; set; }
		public int DurumID { get; set; }

		[Display(Name = "Taraf 1")]
		public string? Taraf1 { get; set; }
		public int Taraf1ID { get; set; }

		[Display(Name = "Taraf 2")]
		public string? Taraf2 { get; set; }
		
		public int Taraf2ID { get; set; }
		public int? GunFarki { get; set; }
		[Display(Name = "Tutar Açıklama")]
		public string? TutarAciklama { get; set; }
		[DataType(DataType.Currency)]
		[DisplayFormat(DataFormatString = "{0:N2}", ApplyFormatInEditMode = true)]
		public double? Tutar { get; set; }

		[Display(Name = "Şirket")]
		public int SirketID { get; set; }
		public string? Baslik { get; set; }
		public string? Aciklama { get; set; }
		[Display(Name = "Başlangıç Tarihi")]
		public DateTime? BaslangicTarih { get; set; }

		[Display(Name = "Son Tarih")]
		public DateTime? SonBaslangicTarih { get; set; }

		[Display(Name = "Bitiş Tarihi")]
		public DateTime? BitisTarih { get; set; }

		[Display(Name = "Dosya Adı")]
		public string? Pdf { get; set; }
		[Display(Name = "Anahtar Kelimeler")]
		public string? AnahtarKelimeler { get; set; }

		public List<SozlesmelerVM>? SozlesmelerVMListe { get; set; }
	}
	public class SozlesmelerDurum : BaseEntity
	{
		public string Ad { get; set; }
		public string Aciklama { get; set; }
	}
	public class SozlesmelerKonu: BaseEntity
	{
		public string Ad { get; set; }
		public string Aciklama { get; set; }
	}
}
