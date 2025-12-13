using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Eczane:BaseEntity
	{
		public string? Il { get; set; }  
		public string? Ilce { get; set; }  
		public string? Ad { get; set; }  
		public string? Telefon { get; set; }  
		public string? Telefon2 { get; set; }  
		public string? Adres { get; set; }  
		public string? AciklamaAdres { get; set; }  
		public string? TarihDetay { get; set; }  
		public string? Tarih { get; set; } = DateTime.Now.Date.ToShortDateString();
		public string? Konum { get; set; }  
		public string? Enlem { get; set; }  
		public string? Boylam { get; set; }  
		public string? Aciklama { get; set; }  	
		public string? Plaka { get; set; }  
		public string? Sehir { get; set; }  
		public bool? BayiMi { get; set; } 
		public string? MusteriAdi { get; set; }
		public string? EczaciAdi { get; set; }
		public string? Email1 { get; set; }
		public string? Email2 { get; set; }
		public int SehirID { get; set; }
		public int IlceID { get; set; }
		public int MahalleID { get; set; }
		public string? Fotograf { get; set; }
	}
	public class EczaneVM : BaseVM	
	{
		public string? Il { get; set; }  
		public string? Ilce { get; set; }  
		public string? Mahalle { get; set; }  
		public string? Ad { get; set; }  
		public string? Telefon { get; set; }
		public string? Telefon2 { get; set; }
		public string? Adres { get; set; }  
		public string? AciklamaAdres { get; set; }  
		public string? TarihDetay { get; set; }  
		public string? Tarih { get; set; } = DateTime.Now.Date.ToShortDateString();
		public string? Konum { get; set; }  
		public string? Enlem { get; set; }  
		public string? Boylam { get; set; } 
		public string? Aciklama { get; set; }  
		public string? Plaka { get; set; }  
		public string? Sehir { get; set; }  
		public int SehirID { get; set; }
		public int IlceID { get; set; }
		public int MahalleID { get; set; }
		public bool? BayiMi { get; set; }
		[Display(Name = "İletişim")]
		public string? Bayi { get; set; }
		public string? MusteriAdi { get; set; }
		public string? EczaciAdi { get; set; }
		public string? Email1 { get; set; }
		public string? Email2 { get; set; }
		public List<EczaneVM> EczaneVMListe { get; set; }
		public bool IsMap { get; set; }
		public string? Veri { get; set; }

		public string? Fotograf { get; set; }
		public IFormFile? Dosya { get; set; }
	}
}
