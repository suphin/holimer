using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public class CrmLogVM
	{
		public CrmLogVM() { }
		public int MusteriID { get; set; }
		public int VeriID { get; set; }
		public int ModulID { get; set; }
		public string ModulAd { get; set; }


		public List<AktiviteVM> ActiviteListe { get; set; }
		public List<FirsatVM> FirsatListe { get; set; }
		public MusterilerVM Musteri{ get; set; }
		public List<TeklifVM> TeklifListe { get; set; }
		public List<SiparisVM> SiparisListe { get; set; }
		public DosyaYonetimiVM DosyaYonetimi { get; set; }

		 

	}
}
