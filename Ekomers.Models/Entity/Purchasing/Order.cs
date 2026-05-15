using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class Order:BaseEntity
	{
		public string OrderNo { get; set; } 

		public DateTime TeslimTarihi { get; set; }
		public string Aciklama { get; set; } = string.Empty;
		public string TeslimYeri { get; set; } = string.Empty;
	}

	public class OrderVM : BaseVM
	{
		public string OrderNo { get; set; }

		public DateTime TeslimTarihi { get; set; }
		public string Aciklama { get; set; } = string.Empty;
		public string TeslimYeri { get; set; } = string.Empty;

		public List<OfferVM> Teklifler { get; set; }
	}
}
