using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Models
{
	public class KurlarVM
	{
		public string UsdAlis { get; set; }
		public string UsdSatis { get; set; }
		public string EurSatis { get; set; }
		public string EurAlis { get; set; }
		//public double SterlinKuru { get; set; }
		//public double AltinKuru { get; set; }
		public DateTime Tarih { get; set; }
	}
}
