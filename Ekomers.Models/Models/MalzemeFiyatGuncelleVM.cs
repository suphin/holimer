using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models
{
	public class MalzemeFiyatGuncelleVM
	{
		public int MalzemeId { get; set; }
		public string Ad { get; set; }
		public string Kod { get; set; }
		public DateTime? GuncellemeTarihi { get; set; }
		public DateTime? GuncellemeTarihiSatis { get; set; }
		public double? MevcutFiyat { get; set; }
		public double? MevcutFiyatSatis { get; set; }
		public double? YeniFiyat { get; set; }
		public double? YeniMaliyet { get; set; }
		public double? YeniMaliyetSatis { get; set; }
		public double? MevcutMaliyet { get; set; }
		public double? MevcutMaliyetSatis { get; set; }
		public int? DovizTur { get; set; }
	}

}
