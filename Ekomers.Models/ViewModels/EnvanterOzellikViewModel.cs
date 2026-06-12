using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class EnvanterOzellikViewModel
	{
		public int EnvanterID { get; set; }

		public List<EnvanterOzellikItemViewModel> Ozellikler { get; set; }
			= new List<EnvanterOzellikItemViewModel>();
	}

	public class EnvanterOzellikItemViewModel
	{
		public int EnvanterTipOzellikID { get; set; }

		public int EnvanterTipID { get; set; }

		public string Ad { get; set; }

		public string Deger { get; set; }
	}
}
