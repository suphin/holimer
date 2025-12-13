using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public class CrmKullaniciVM
	{
		public int KullaniciID { get; set; }
		public string AdSoyad { get; set; }
		public string Email { get; set; }
		public string Telefon { get; set; }
		public int MusteriSayisi { get; set; }
		public int FirsatSayisi { get; set; }
		public int TeklifSayisi { get; set; }
		public int SiparisSayisi { get; set; }
		public int AktiviteSayisi { get; set; }
		public double ToplamSiparisTutari { get; set; }
		public double YillikSiparisTutari { get; set; }
		public double AylikSiparisTutari { get; set; }
		public double GunlukSiparisTutari { get; set; }
		public double HedefSiparisTutari { get; set; }
		public double HedefeOran { get; set; }
		public double HedefTutar { get; set; }
		public int HedefGun { get; set; }
		public int KalanGun { get; set; }
		public int GecenGun { get; set; }
		public double GunlukOrtalama { get; set; }
		public DateTime HedefTarih { get; set; }
		public DateTime BaslangicTarih { get; set; }


	}
}
