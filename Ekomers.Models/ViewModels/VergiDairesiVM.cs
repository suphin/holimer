using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class VergiDairesiVM : BaseVM
	{
		[Display(Name = "İl kodu")]
		public int SehirID { get; set; }
		[Display(Name = "İlçe")]
		public string Ilce { get; set; }
		[Display(Name = "Kod")]
		public string Kod { get; set; }
		[Display(Name = "Ad")]
		public string Ad { get; set; }
		public List<VergiDairesiVM> VergiDairesiVMListe { get; set; }

	}
}
