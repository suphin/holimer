using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class SehirlerVM : BaseVM
	{
		[Display(Name = "Şehir Adı")]
		public string Ad { get; set; }
		[Display(Name = "Parent ID")]
		public int? UstID { get; set; }

		public string? MinLongitude { get; set; }
		public string? MinLatitude { get; set; }
		public string? MaxLongitude { get; set; }
		public string? MaxLatitude { get; set; }
		public int? MahalleID { get; set; }

		public List<SehirlerVM> SehirlerVMListe { get; set; }
	}
}
