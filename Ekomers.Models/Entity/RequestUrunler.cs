using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class RequestUrunler:BaseEntity
	{
		public int UrunID { get; set; }
		public int RequestID { get; set; }
	 
		public double Miktar { get; set; }
		public double MiktarSon { get; set; }
	 
		public int? BirimID { get; set; }
		public string? Aciklama { get; set; }
		public string? TTN { get; set; } // talep takip no
		public bool OnayliMi { get; set; }
		public int OfferDurumID { get; set; }

	}

	public class RequestUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public int RequestID { get; set; }
		[Display(Name = "Talep Tarihi")]
		public DateTime RequestDate { get; set; }
		public int RequestDurumID { get; set; }
		[Display(Name ="Durum")]
		public string RequestDurumAd { get; set; }
		public string? RequestDurumClass { get; set; }
		public int RequestTurID { get; set; }
		[Display(Name = "Talep Türü")]
		public string RequestTurAd { get; set; }
		public int SirketID { get; set; }
		[Display(Name = "Şirket")]
		public string SirketAd { get; set; }
		public double Miktar { get; set; }
		[Display(Name = "Miktar")]
		public double MiktarSon { get; set; }
		[Display(Name = "Malzeme")]
		public string UrunAd { get; set; }
		[Display(Name = "Kod")]
		public string UrunKod { get; set; }
		[Display(Name ="Birim")]
		public string? BirimAd { get; set; }
		public int? BirimID { get; set; }
		public string? TipAd { get; set; }
		public int? TipID { get; set; }
		public bool OnayliMi { get; set; }
		public int OfferDurumID { get; set; }
		public int OfferID { get; set; }
		[Display(Name = "Teklif Durum")]
		public string? OfferDurumAd { get; set; }
		public string? DurumClass { get; set; }
		[Display(Name = "Açıklama")]
		public  string? Aciklama { get; set; }
		public List<RequestUrunlerVM> RequestUrunlerVMListe { get; set; }
		public List<OfferVM> OfferVMListe { get; set; }
		[Display(Name = "Talep Takip No")]
		public string? TTN { get; set; } // talep takip no

	}
}
