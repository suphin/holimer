using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class SiparisDetayVM
	{
		public int FirmaID { get; set; }

		public string FirmaAd { get; set; }

		public Musteriler Firma { get; set; }

		public Sirketler Sirket { get; set; }

		public List<OfferVM> Urunler { get; set; }

		public double AraToplam { get; set; }

		public double KdvToplam { get; set; }

		public double GenelToplam { get; set; }
	}
}
