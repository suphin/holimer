using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class PurchaseRequestItem
	{
		public int Id { get; set; }

		public int PurchaseRequestId { get; set; }

		public int? ProductId { get; set; }

		public string ProductName { get; set; }

		public decimal Quantity { get; set; }

		public string Unit { get; set; }
	}
}
