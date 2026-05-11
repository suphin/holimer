using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class PurchasingMenuVM
	{
		public int ToplamTalep { get; set; }

		public int TaslakTalep { get; set; }
		public int OnayBekleyenTalep { get; set; }

		public int OnaylananTalep { get; set; }

		public int TeklifAsamasında { get; set; }
		public int OnayBekleyenTeklif { get; set; }

		public int OnaylananTeklif { get; set; }
	}
}
