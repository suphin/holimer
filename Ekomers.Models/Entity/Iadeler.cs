using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Iadeler:BaseEntity
	{
		DateTime Tarih { get; set; }

		public string IadeNo { get; set; }
		public string SiparisNo { get; set; } 
		public int MusteriID { get; set; }
		public string Musteri { get; set; }

		public int UrunID { get; set; }
		public string Urun { get; set; }
		public string Platform { get; set; }
		public int Adet { get; set; }
		public string IadeNedeni { get; set; }
		public string Aciklama { get; set; }
		public int DurumID { get; set; }
		public string Durum { get; set; }
		public bool Sonuc { get; set; }
	}

	public class IadelerVM : BaseVM
	{
	}
}
