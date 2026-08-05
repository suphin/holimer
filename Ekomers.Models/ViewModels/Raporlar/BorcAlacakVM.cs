using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class BorcAlacakVM
	{
		public string CHKOD { get; set; } = string.Empty;

		public string CHUNVAN { get; set; } = string.Empty;

		public decimal BorcToplami { get; set; }

		public decimal AlacakToplami { get; set; }
	}
}
