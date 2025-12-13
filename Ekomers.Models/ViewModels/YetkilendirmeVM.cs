using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class YetkilendirmeVM:BaseVM
	{
		public string Ad { get; set; }
		public string Aciklama { get; set; }
		public int KategoriID { get; set; }
		public string KategoriAd { get; set; }
		public string PolicyName { get; set; }
		public string ClaimType { get; set; }
		public string ClaimName { get; set; }
		public List<YetkilendirmeVM> YetkilendirmeVMListe { get; set; }
		public bool Selected { get; set; }
	}
}
