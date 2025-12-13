using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
	 
	public class Geojson : BaseEntity
	{
	 
		public string? Aciklama { get; set; }
		public string? Veri { get; set; }

		public int? KayitID { get; set; }
		public int? ModulID { get; set; }
	}
}
