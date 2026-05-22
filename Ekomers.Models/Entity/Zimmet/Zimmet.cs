using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Zimmet:BaseEntity
	{
		public int EnvanterID { get; set; }
		public string  KullaniciID { get; set; }
		public DateTime ZimmetTarihi { get; set; } = DateTime.Now;
		public DateTime TeslimTarihi { get; set; } = DateTime.Now;
		public string? AciklamaIlk { get; set; } = string.Empty;
		public string? AciklamaSon { get; set; } = string.Empty;
		public int DurumID { get; set; } = 0;

	}

	public class ZimmetVM : BaseVM
	{
		public int EnvanterID { get; set; } 
		public EnvanterVM Envanter { get; set; } = new EnvanterVM();
		public string  KullaniciID { get; set; }  
		public Kullanici Kullanici { get; set; } = new Kullanici();
		public DateTime ZimmetTarihi { get; set; } = DateTime.Now;
		public DateTime TeslimTarihi { get; set; } = DateTime.Now;
		public string? AciklamaIlk { get; set; } = string.Empty;
		public string? AciklamaSon { get; set; } = string.Empty;
		public int DurumID { get; set; } = 0;
	}
}
