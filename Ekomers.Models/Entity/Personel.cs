using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Personel: BaseEntity  
	{
		public string? UserID { get; set; }
		public string? PersonelKod { get; set; }
		public string AdSoyad { get; set; } = string.Empty;
		public string? Email { get; set; } = string.Empty;
		public string? Telefon { get; set; } = string.Empty;
		public string? TelefonSirket { get; set; } = string.Empty;
	
		public string? Not { get; set; } = string.Empty;
		public string? Adres { get; set; } = string.Empty;
		public string? Tckn { get; set; } = string.Empty;
		public DateTime DogumTarihi {  get; set; }
		public DateTime IseBaslamaTarihi {  get; set; }
		public DateTime AyrilisTarihi {  get; set; }
		public int DepartmanID { get; set; }
		public int SirketID { get; set; }
		public int BolumID { get; set; }
		public int GorevID { get; set; }
		public int DurumID { get; set; }
		public int Cinsiyet { get; set; }

	}

	public class PersonelVM : BaseVM
	{
		public string? UserID { get; set; }
		[Display(Name = "Personel Kodu")]
		public string? PersonelKod { get; set; }
		[Display(Name = "Ad Soyad")]
		public string AdSoyad { get; set; } = string.Empty;
		public string? Email { get; set; } = string.Empty;
		public string? Telefon { get; set; } = string.Empty;
		[Display(Name = "Şirket Telefonu")]
		public string? TelefonSirket { get; set; } = string.Empty;
		public string? tagifyPersoneller { get; set; }
		public string? Not { get; set; } = string.Empty;
		[Display(Name = "Doğum Tarihi")]
		public DateTime DogumTarihi { get; set; }
		[Display(Name = "İşe Başlama Tarihi")]
		public DateTime IseBaslamaTarihi { get; set; }
		[Display(Name = "Ayrılış Tarihi")]
		public DateTime AyrilisTarihi { get; set; }
		public string? Adres { get; set; } = string.Empty;
		public string? Tckn { get; set; } = string.Empty;
		[Display(Name ="Departman Adı")]
		public string? DepartmanAd { get; set; } = string.Empty;
		public string? BolumAd { get; set; } = string.Empty;
		[Display(Name = "Şirket Adı")]
		public string? SirketAd { get; set; } = string.Empty;
		public int DepartmanID { get; set; }
		public int SirketID { get; set; }
		public int BolumID { get; set; }
		public int Cinsiyet { get; set; }
		public List<PersonelVM> PersonelVMListe { get; set; }
		public PersonelGorev PersonelGorev { get; set; }
		public PersonelDurum PersonelDurum { get; set; }
		public int GorevID { get; set; }
		public int DurumID { get; set; }
	}

	public class PersonelGorev : BaseEntity
	{
		[Display(Name = "Görev Adı")]
		public string? Ad { get; set; } 
		public string? Aciklama { get; set; }
	}

	public class PersonelDurum: BaseEntity
	{
		[Display(Name = "Durum Adı")]
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
	}

}
