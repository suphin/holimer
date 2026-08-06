using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class KrediOdemePlaniVM
	{
		public int YIL { get; set; }

		public int AY { get; set; }

		public string AYADI { get; set; } = "";

		public string KREDIADI { get; set; } = "";

		public decimal TUTAR { get; set; }
		public DateTime DUEDATE { get; set; }
	}
}
