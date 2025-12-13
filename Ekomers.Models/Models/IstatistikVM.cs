using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models
{
	public class IstatistikVM
	{
		public int VeriSayisi { get; set; }
		public int IptalSayisi { get; set; }
		public int PasifSayisi { get; set; }
		public int AktifSayisi { get; set; }
		public int SilinenSayisi { get; set; }
		public double Toplam { get; set; }
	}
}
