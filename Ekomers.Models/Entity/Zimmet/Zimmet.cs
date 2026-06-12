using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Zimmet:BaseEntity
	{
		public int EnvanterID { get; set; }
		public int  PersonelID { get; set; }
		public DateTime ZimmetTarihi { get; set; } = DateTime.Now;
		public DateTime TeslimTarihi { get; set; } = DateTime.Now;
		public string? AciklamaIlk { get; set; } = string.Empty;
		public string? AciklamaSon { get; set; } = string.Empty;
		public int DurumID { get; set; } = 0;
		public DateTime AyrilisTarihi { get; set; }

	}

	public class ZimmetVM : BaseVM
	{
		public int EnvanterID { get; set; } 
		public EnvanterVM Envanter { get; set; } = new EnvanterVM();

		public string? EnvanterAd { get; set; } = string.Empty;
		public string? EnvanterMarka { get; set; } = string.Empty;
		public string? EnvanterModel { get; set; } = string.Empty;
		public string? EnvanterSerino { get; set; } = string.Empty;


		public int  SirketID { get; set; }  
		public int  PersonelID { get; set; }  
		public Personel Personel { get; set; } = new Personel();
		public PersonelVM PersonelVM { get; set; } = new PersonelVM(); 
		public Sirketler Sirket { get; set; } = new Sirketler();
		[Display(Name = "Zimmet Tarihi")]
		public DateTime ZimmetTarihi { get; set; } = DateTime.Now;
		[Display(Name = "Teslim Tarihi")]
		public DateTime TeslimTarihi { get; set; } = DateTime.Now;
		[Display(Name = "İşten Ayrılış Tarihi")]
		public DateTime AyrilisTarihi { get; set; } = DateTime.Now;
		[Display(Name ="Zimmet Açıklama")]
		public string? AciklamaIlk { get; set; } = string.Empty;
		[Display(Name = "Zimmet Açıklama")]
		public string? AciklamaSon { get; set; } = string.Empty;
		public int DurumID { get; set; } = 0;
		public List<ZimmetVM> ZimmetVMListe { get; set; }
		 
		public List<EnvanterVM> EnvanterVMListe { get; set; }
	}
}
