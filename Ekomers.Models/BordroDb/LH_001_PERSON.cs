using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Ekomers.Models.BordroDb
{
	public class LH_001_PERSON
	{
		[Key]
		public int LREF { get; set; }
		public string? SPECODE { get; set; }

		public short? FIRMNR { get; set; }
		public short? DEPTNR { get; set; }
		public short? LOCNR { get; set; }
		public short? TYP { get; set; }

		public string? CODE { get; set; }
		public string? NAME { get; set; }
		public string? SURNAME { get; set; }
		public string? PREFIX { get; set; }
		public string? MIDNAME { get; set; }
		public string? TITLE { get; set; }

		public short? SEX { get; set; }
		public short? STATUS { get; set; }

		public DateTime? BIRTHDATE { get; set; }

		public short? EDUCATION { get; set; }

		public DateTime? APPDATE { get; set; }
		public DateTime? INDATE { get; set; }
		public DateTime? OUTDATE { get; set; }

		public string? SSKNO { get; set; }
		public string? TTFNO { get; set; }
	}
}
