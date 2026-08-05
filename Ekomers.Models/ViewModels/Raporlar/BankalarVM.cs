using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class BankalarVM
	{
		public int LOGICALREF { get; set; }

		public string CODE { get; set; } = string.Empty;

		public string DEFINITION_ { get; set; } = string.Empty;

		public string BRANCH { get; set; } = string.Empty;

		public short ACTIVE { get; set; }

		public string SPECODE { get; set; } = string.Empty;

		public int ORGLOGICREF { get; set; }
		public List<BankalarVM> Bankalar { get; set; } = new List<BankalarVM>();
	 
	}
	public class BankaHesapVM
	{
		public int LOGICALREF { get; set; }

		public string CODE { get; set; } = "";

		public string DEFINITION_ { get; set; } = "";

		public string ACCOUNTNO { get; set; } = "";

		public short ACTIVE { get; set; }

		public short CARDTYPE { get; set; }

		public string SPECODE { get; set; } = "";

		public int BANKREF { get; set; }

		public int CURRENCY { get; set; }

		public int ORGLOGICREF { get; set; }

		public short KKUSAGE { get; set; }

		public decimal DEBIT { get; set; }

		public decimal CREDIT { get; set; }

		public decimal BAKIYE => DEBIT - CREDIT;
		public List<BankaHesapVM> Hesaplar { get; set; } = new List<BankaHesapVM>();
	}
	public class BankaHesapEkstreVM
	{
		public int Yil { get; set; }

		public int AyNo { get; set; }

		public string Ay { get; set; } = string.Empty;

		public decimal Borc { get; set; }

		public decimal Alacak { get; set; }

		public decimal Bakiye { get; set; }
	}
}
