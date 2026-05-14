using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class SiparisFirmaGrupVM:BaseVM
	{
		public int FirmaID { get; set; }
		public string FirmaAd { get; set; }

		public int TalepSayisi { get; set; }
		public int ToplamUrunSayisi { get; set; }

		public double ToplamUrunAdedi { get; set; }

		public double ToplamTutar { get; set; }

		public List<OfferVM> Urunler { get; set; }
		public List<RequestUrunlerVM> Talepler { get; set; }
		public List<SiparisFirmaGrupVM> SiparisFirmaGrupVMListe { get; set; }
	}
}
