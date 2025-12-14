using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class Warehouse
	{
		public int Id { get; set; }
		public string Name { get; set; }
		public string Location { get; set; }
	}

	public class Product
	{
		public int Id { get; set; }
		public string Code { get; set; }
		public string GrpCode { get; set; }
		public string Name { get; set; }
		public string SKU { get; set; }
	}

	public class WarehouseInventory
	{
		public int Id { get; set; }
		public int WarehouseId { get; set; }
		public int ProductId { get; set; }
		public double SystemQuantity { get; set; }
		public Warehouse Warehouse { get; set; }
		public Product Product { get; set; }

	}

	public class StockCount
	{
		public int Id { get; set; }
		public int WarehouseId { get; set; }
		public int ProductId { get; set; } 
		public double SystemQuantity { get; set; }   // Depodaki sistemdeki miktar
		public double CountedQuantity { get; set; }  // Sayımda bulunan miktar
		public double Difference => CountedQuantity - SystemQuantity;  // Hesaplanan fark
		public DateTime CountDate { get; set; } = DateTime.Now;

		public Warehouse Warehouse { get; set; }
		public Product Product { get; set; }
	}
}
