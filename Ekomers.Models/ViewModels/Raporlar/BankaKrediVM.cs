using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class BankaKrediVM
	{
		public int LOGICALREF { get; set; }

		public string CODE { get; set; } = string.Empty;

		public string NAME_ { get; set; } = string.Empty;

		public DateTime? BEGDATE { get; set; }

		public DateTime? ENDDATE { get; set; }

		public short CRCARDTYPE { get; set; }

		public decimal TRTOTAL { get; set; }

		public decimal INTTOTAL { get; set; }

		public decimal KKDFTOTAL { get; set; }

		public decimal BSMVTOTAL { get; set; }

		public string SPECODE { get; set; } = string.Empty;

		public short TRCURR { get; set; }

		public short BRANCH { get; set; }

		public int PROJECTREF { get; set; }

		public short CRCALCTYPE { get; set; }

		public DateTime? STRUCTDATE { get; set; }

		public short PERIODENDPAY { get; set; }

		public string BankaHesabi { get; set; } = string.Empty;

		public string BankaAdi { get; set; } = string.Empty;

		public string ProjeKodu { get; set; } = string.Empty;
	}
}
