using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers
{
	using Ekomers.Data;
	using Ekomers.Models.Entity;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.EntityFrameworkCore;
	using System.Linq;

	using Microsoft.AspNetCore.Mvc;
	using Microsoft.EntityFrameworkCore;
	using System.Collections.Generic;
	using System.Linq;
	using Ekomers.Models.ViewModels;

	public class WarehouseInventoryController : Controller
	{
		private readonly ApplicationDbContext _context;

		public WarehouseInventoryController(ApplicationDbContext context)
		{
			_context = context;
		}

		// 📋 Depo seçme ekranı
		public IActionResult Index()
		{
			ViewBag.Warehouses = _context.Warehouses.ToList();
			return View();
		}

		// 📦 Depo seçildikten sonra ürünlerin listesi
		[HttpGet]
		public IActionResult EditAll(int warehouseId)
		{
			var warehouse = _context.Warehouses.Find(warehouseId);
			if (warehouse == null) return NotFound();

			var products = _context.Products.ToList();

			// O depodaki mevcut kayıtları alıyoruz
			var existingInventories = _context.WarehouseInventories
				.Where(x => x.WarehouseId == warehouseId)
				.ToList();

			// ViewModel oluşturuyoruz
			var model = products.Select(p => new WarehouseInventoryEditViewModel
			{
				ProductId = p.Id,
				ProductName = p.Name,
				WarehouseId = warehouseId,
				SystemQuantity = existingInventories.FirstOrDefault(i => i.ProductId == p.Id)?.SystemQuantity ?? 0
			}).ToList();

			ViewBag.WarehouseName = warehouse.Name;
			return View(model);
		}

		// 💾 Toplu kayıt kaydetme
		[HttpPost]
		public IActionResult EditAll(List<WarehouseInventoryEditViewModel> model)
		{
			if (model == null || !model.Any())
				return BadRequest("Veri alınamadı.");

			var warehouseId = model.First().WarehouseId;

			foreach (var item in model)
			{
				var existing = _context.WarehouseInventories
					.FirstOrDefault(i => i.WarehouseId == item.WarehouseId && i.ProductId == item.ProductId);

				if (existing != null)
				{
					existing.SystemQuantity = item.SystemQuantity;
					_context.WarehouseInventories.Update(existing);
				}
				else
				{
					_context.WarehouseInventories.Add(new WarehouseInventory
					{
						WarehouseId = item.WarehouseId,
						ProductId = item.ProductId,
						SystemQuantity = item.SystemQuantity
					});
				}
			}

			_context.SaveChanges();

			TempData["success"] = "Depo miktarları başarıyla güncellendi.";
			return RedirectToAction("Index");
		}
	}


}
