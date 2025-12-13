using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class StockCountViewModel
	{
		public DateTime CountDate { get; set; } = DateTime.Today;
		public List<ProductStockRow> Products { get; set; }
		public List<Warehouse> Warehouses { get; set; }
	}

	public class ProductStockRow
	{
		public int ProductId { get; set; }
		public string ProductName { get; set; }
		public List<WarehouseStockCell> Warehouses { get; set; }
	}

	public class WarehouseStockCell
	{
		public int WarehouseId { get; set; }
		public decimal SystemQuantity { get; set; }
		public decimal? CountedQuantity { get; set; } // girilecek miktar
	}

	public class WarehouseInventoryEditViewModel
	{
		public int WarehouseId { get; set; }
		public int ProductId { get; set; }
		public string ProductName { get; set; }
		public decimal SystemQuantity { get; set; }
	}
}
