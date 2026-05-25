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
		public string  PersonelID { get; set; }
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
		public string  PersonelID { get; set; }  
		public Kullanici Kullanici { get; set; } = new Kullanici();
		public Sirketler Sirket { get; set; } = new Sirketler();
		public DateTime ZimmetTarihi { get; set; } = DateTime.Now;
		public DateTime TeslimTarihi { get; set; } = DateTime.Now;
		public string? AciklamaIlk { get; set; } = string.Empty;
		public string? AciklamaSon { get; set; } = string.Empty;
		public int DurumID { get; set; } = 0;
		public List<ZimmetVM> ZimmetVMListe { get; set; }
		public List<EnvanterVM> EnvanterVMListe { get; set; }
	}
}
