using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class PurchaseRequest
	{
		public int Id { get; set; }

		public string RequestNo { get; set; }

		public string Description { get; set; }

		public DateTime RequestDate { get; set; }

		public RequestStatus Status { get; set; }

		public ICollection<PurchaseRequestItem> Items { get; set; }
	}
}
