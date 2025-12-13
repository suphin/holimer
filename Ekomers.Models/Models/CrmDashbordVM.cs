using Ekomers.Models.Ekomers;
using Ekomers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public class CrmDashbordVM
	{
		public int SiparisSayisi { get; set; }
		public int TeklifSayisi { get; set; }
		public int FirsatSayisi { get; set; }
		public int MusteriSayisi { get; set; }
		public int AktiviteSayisi { get; set; }
		public int MalzemeSayisi { get; set; }
		public double ToplamSiparisTutari { get; set; } 
		 
		public List<CrmKullaniciVM> CrmKullaniciListe { get; set; }


		// Yeni eklenenler
		public decimal[] AylikSiparisToplam { get; set; } = new decimal[12]; // Ocak..Aralık
		public int[] AylikAktiviteSayisi { get; set; } = new int[12];        // Ocak..Aralık
		public int[] AylikSiparisSayisi { get; set; } = new int[12];        // Ocak..Aralık
	}
}
