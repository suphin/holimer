using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
	public class Map : BaseEntity
	{
		public string? Aciklama { get; set; }
	}

	public class MapVM : BaseVM
	{
		public string[] Koordinat { get; set; }
		public int KayitID { get; set; }
		public int ModulID { get; set; }
		public string? Aciklama { get; set; }
		public List<MapVM> MapVMListe { get; set; }
	}
}
