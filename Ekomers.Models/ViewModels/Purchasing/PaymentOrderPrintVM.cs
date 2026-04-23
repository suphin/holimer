using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class PaymentOrderPrintVM
	{
		public string? SatinAlinanFirma { get; set; }
		public DateTime TanzimTarihi { get; set; }
		public DateTime FaturaTarihi { get; set; }

		// TEK ÜRÜN
		public string? UrunAdi { get; set; }
		public string? BelgeTuru { get; set; }
		public string? BelgeNo { get; set; }
		public decimal Tutar { get; set; }

		public decimal Kdv { get; set; }
		public decimal ToplamTutar { get; set; }
		public decimal OdenecekTutar { get; set; }

		public string? Vade { get; set; }
		public string? OdemeYontemi { get; set; }
		public DateTime OdemeTarihi { get; set; }

		public string? Hazirlayan { get; set; }
		public string? BilgiIslem { get; set; }
		public string? MuhasebeMuduru { get; set; }
		public string? GenelMuduru { get; set; }
		public string? Koordinator { get; set; }
		public string? SirketAd { get; set; }
		public string? UrunAd { get; set; }
	}
}
