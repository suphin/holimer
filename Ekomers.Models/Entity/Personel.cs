using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Personel: BaseEntity  
	{
		public string UserID { get; set; }
		public string AdSoyad { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Telefon { get; set; } = string.Empty;
		public string KullaniciAdi { get; set; } = string.Empty;
		public string Sifre { get; set; } = string.Empty;
		public string Not { get; set; } = string.Empty;
	}

	public class PersonelVM : BaseVM
	{
		public string UserID { get; set; }
		public string AdSoyad { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Telefon { get; set; } = string.Empty;
		public string KullaniciAdi { get; set; } = string.Empty;
		public string Sifre { get; set; } = string.Empty;
		public string? tagifyPersoneller { get; set; }
		public string Not { get; set; } = string.Empty;
		public string DepartmanAd { get; set; } = string.Empty;
		public string SirketAd { get; set; } = string.Empty;
	}
}
