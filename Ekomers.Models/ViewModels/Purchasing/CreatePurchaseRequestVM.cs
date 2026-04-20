using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class CreatePurchaseRequestVM
	{
		public string Description { get; set; }

		public List<PurchaseRequestItemVM> Items { get; set; }
	}

	public class PurchaseRequestItemVM
	{
		public int? ProductId { get; set; }
		public string ProductName { get; set; }

		public decimal Quantity { get; set; }
		public string Unit { get; set; }
	}
}
