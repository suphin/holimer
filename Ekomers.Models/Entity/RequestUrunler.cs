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
		public bool OnayliMi { get; set; }
	}

	public class RequestUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public int RequestID { get; set; }
		public int RequestDurumID { get; set; }
		public int RequestTurID { get; set; }
	 
		public double Miktar { get; set; }
		public double MiktarSon { get; set; }
		public string UrunAd { get; set; }
		public string UrunKod { get; set; }
		[Display(Name ="Birim")]
		public string? BirimAd { get; set; }
		public int? BirimID { get; set; }
		public string? TipAd { get; set; }
		public int? TipID { get; set; }
		public bool OnayliMi { get; set; }

		public  string? Aciklama { get; set; }
		public List<RequestUrunlerVM> RequestUrunlerVMListe { get; set; }
		 
	}
}
