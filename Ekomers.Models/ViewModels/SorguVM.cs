using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class SorguVM:BaseVM
	{
		public string? AdSoyad { get; set; }
		public string? Telefon { get; set; }
		public string? Tckn { get; set; }
		public string? Adres { get; set; }
		public string? Egitim { get; set; }
		public string? Meslek { get; set; }
		public string? Aciklama { get; set; }
		public string? Konu { get; set; }
		public string? Durum { get; set; } 
	}
}
