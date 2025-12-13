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
	public class ReceteUrunler : BaseEntity
	{
		public int UrunID { get; set; }
		public int ReceteID { get; set; }
		 
		public double? Miktar { get; set; }
		public double? Fiyat { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double? Kdv { get; set; }
		public double? Iskonto { get; set; }
	}

	public class ReceteUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public string? UrunKod { get; set; }
		public string? UrunAd { get; set; }
		public int ReceteID { get; set; }
		public string? BirimAd { get; set; }
		public int? BirimID { get; set; }
		public string? TipAd { get; set; }
		public int? TipID { get; set; }
		public double? Miktar { get; set; }
		public double? Fiyat { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public string? Aciklama { get; set; }
		public double? Kdv { get; set; }
		public double? Iskonto { get; set; }
		public List<ReceteUrunlerVM> ReceteUrunlerVMListe { get; set; }
		public string? Grup { get; set; }
		public string? GrupKod { get; set; }
		[Display(Name = "Alt Grup")]
		public string? AltGrup { get; set; }
		[Display(Name = "Alt Grup")]
		public int? AltGrupID { get; set; }
		[Display(Name = "Grup")]
		public int GrupID { get; set; }
	}
}
