using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class BankaKrediVM
	{
		public string NAME_ { get; set; } = string.Empty;

		public string CODE { get; set; } = string.Empty;

		public DateTime? BEGDATE { get; set; }

		public DateTime? ENDDATE { get; set; }

		public decimal TRTOTAL { get; set; }

		public decimal INTTOTAL { get; set; }

		public decimal BSMVTOTAL { get; set; }

		public decimal ANAPARA_ODENEN { get; set; }

		public decimal FAIZ_ODENEN { get; set; }

		public decimal BSMV_ODENEN { get; set; }

		public decimal KKDF_ODENEN { get; set; }

		public decimal KALAN_ANAPARA { get; set; }

		public decimal KALAN_FAIZ { get; set; }

		public decimal KALAN_BSMV { get; set; }

		public decimal KALAN_KKDF { get; set; }
	}
}
