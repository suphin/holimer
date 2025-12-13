using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
	public class Sehirler : BaseEntity
	{
		public string Ad { get; set; }
		public int? UstID { get; set; }
		public string? MinLongitude { get; set; }
		public string? MinLatitude { get; set; }
		public string? MaxLongitude { get; set; }
		public string? MaxLatitude { get; set; }
		public int? MahalleID { get; set; }
	}
	public class VergiDairesi : BaseEntity
	{
		public int SehirID { get; set; }
		public string Ilce { get; set; }
		public string Kod { get; set; }
		public string Ad { get; set; }

	}
}
