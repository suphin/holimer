using Ekomers.Data;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers
{
	public class StockCountController : Controller
	{
		private readonly ApplicationDbContext _context;

		public StockCountController(ApplicationDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			var warehouses = _context.Warehouses.ToList();
			var products = _context.Products.ToList();

			var data = products.Select(p => new ProductStockRow
			{
				ProductId = p.Id,
				ProductName = p.Name,
				Warehouses = warehouses.Select(w =>
				{
					var inv = _context.WarehouseInventories
						.FirstOrDefault(i => i.WarehouseId == w.Id && i.ProductId == p.Id);

					return new WarehouseStockCell
					{
						WarehouseId = w.Id,
						SystemQuantity = inv?.SystemQuantity ?? 0
					};
				}).ToList()
			}).ToList();

			var model = new StockCountViewModel
			{
				Warehouses = warehouses,
				Products = data
			};

			return View(model);
		}

		[HttpPost]
		public IActionResult Save(StockCountViewModel model)
		{
			foreach (var product in model.Products)
			{
				foreach (var cell in product.Warehouses)
				{
					var count = new StockCount
					{
						CountDate = model.CountDate,
						ProductId = product.ProductId,
						WarehouseId = cell.WarehouseId,
						SystemQuantity = cell.SystemQuantity,
						CountedQuantity = cell.CountedQuantity ?? 0
					};
					_context.StockCounts.Add(count);
				}
			}

			_context.SaveChanges();
			TempData["success"] = "Sayım başarıyla kaydedildi.";
			return RedirectToAction("Index");
		}

		public IActionResult Report(DateTime? date = null)
		{
			var query = _context.StockCounts
				.Include(x => x.Product)
				.Include(x => x.Warehouse)
				.AsQueryable();

			if (date.HasValue)
				query = query.Where(x => x.CountDate.Date == date.Value.Date);

			var report = query
				.OrderBy(x => x.Product.Name)
				.ThenBy(x => x.Warehouse.Name)
				.ToList();

			ViewBag.SelectedDate = date?.ToString("yyyy-MM-dd");
			return View(report);
		}
	}



}
