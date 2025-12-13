using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class TeklifUrunler:BaseEntity
	{
		public int UrunID { get; set; }
		public int TeklifID { get; set; }
		public int SiparisID { get; set; }
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double Kdv { get; set; }
		public double Iskonto { get; set; }

	}

	public class TeklifUrunlerVM : BaseVM
	{
		public int UrunID { get; set; }
		public int TeklifID { get; set; }
		public int SiparisID { get; set; }
		public double Miktar { get; set; }
		public double Fiyat { get; set; }
		public double Kdv { get; set; }
		public double Iskonto { get; set; }
		public string UrunAd { get; set; }
		public string UrunKod { get; set; }
		public string? BirimAd { get; set; }
		public int? BirimID { get; set; }
		public string? TipAd { get; set; }
		public int? TipID { get; set; }
		public int? DovizTur { get; set; }
		public string? DovizTurAd { get; set; }
		public double Toplam { get; set; }
		public double KdvTutar { get; set; }
		public double IskontoTutar { get; set; }
        public string? IskontoAd { get; set; }
        public double IskontoOran { get; set; }
        public double GenelToplam { get; set; }
		public  string Aciklama { get; set; }
		public List<TeklifUrunlerVM> TeklifUrunlerVMListe { get; set; }
        public List<TeklifIskonto> TeklifIskontoListe { get; set; }
        public  bool TeklifIsLocked { get; set; }
	}
}
