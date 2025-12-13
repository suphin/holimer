using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.LogoDb
{
	public class LG_100_ITEMS
	{
		[Key]
		public int LOGICALREF { get; set; }

		public short ACTIVE { get; set; }
		public string CODE { get; set; }
		public string NAME { get; set; }
		public string SPECODE { get; set; }
		public string STGRPCODE { get; set; }
		public string CYPHCODE { get; set; }
		public string PRODUCERCODE { get; set; }
		public double VAT { get; set; }
	}
	public class PORTAL_ITEMS_LIST
	{
		public string SpecCode { get; set; }
		public string SpecCode2 { get; set; }
		public string SpecCode3 { get; set; }
		public string SpecCode4 { get; set; }
		public string SpecCode5 { get; set; }
		public string ProductCode { get; set; }
		public string ProductName { get; set; }
		public string ProductName2 { get; set; }
		public string Barcode { get; set; }
		public int ProductId { get; set; }
		public string ModelCode { get; set; }
		public double VAT { get; set; }
		 
		public string ProducerCode { get; set; }
		public string Model { get; set; }
	}
}
