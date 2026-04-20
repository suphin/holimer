using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Ekomers.Web.Controllers
{
	public class PurchaseRequestController : Controller
	{
		private readonly IPurchaseRequestService _service;
		private readonly ApplicationDbContext _context;

		public PurchaseRequestController(IPurchaseRequestService service, ApplicationDbContext context)
		{
			_service = service;
			_context = context;
		}

		public IActionResult Create()
		{
			return View();
		}
		public async Task<IActionResult> Index()
		{
			var list = await _context.PurchaseRequests
				.OrderByDescending(x => x.Id)
				.ToListAsync();

			return View(list);
		}
		[HttpPost]
		public async Task<IActionResult> Create(CreatePurchaseRequestVM model, string actionType)
		{
			try
			{
				bool send = actionType == "send";
				model.Items = JsonConvert.DeserializeObject<List<PurchaseRequestItemVM>>(Request.Form["ItemsJson"]);

				await _service.CreateAsync(model, send);

				return RedirectToAction("Create");
			}
			catch (Exception ex)
			{
				ModelState.AddModelError("", ex.Message);
				return View(model);
			}
		}

		[HttpGet]
		public IActionResult Search(string term)
		{
			var result = _context.Products
				.Where(x => x.Name.Contains(term)
						 || x.Code.Contains(term)
						 || x.Barcode.Contains(term))
				.Select(x => new {
					id = x.Id,
					text = x.Name + " (" + x.Code + ")",
					unit = x.Unit
				})
				.Take(10)
				.ToList();

			return Json(result);
		}

		public async Task<IActionResult> Edit(int id)
		{
			var request = await _context.PurchaseRequests
				.Include(x => x.Items)
				.FirstOrDefaultAsync(x => x.Id == id);

			if (request.Status != RequestStatus.Draft)
				return RedirectToAction("Index");

			//var vm = new CreatePurchaseRequestVM
			//{
			//	Description = request.Description,
			//	Items = request.Items.Select(x => new PurchaseRequestItemVM
			//	{
			//		ProductId = x.ProductId,
			//		ProductName = x.ProductName,
			//		Quantity = x.Quantity,
			//		Unit = x.Unit
			//	}).ToList()
			//};
			var vm = new CreatePurchaseRequestVM
			{
				Description = request.Description,
				Items = request.Items.Select(x => new PurchaseRequestItemVM
				{
					ProductId = x.ProductId,
					ProductName = !string.IsNullOrEmpty(x.ProductName)
						? x.ProductName
						: _context.Products
							.Where(p => p.Id == x.ProductId)
							.Select(p => p.Name)
							.FirstOrDefault(),
					Quantity = x.Quantity,
					Unit = x.Unit
				}).ToList()
			};

			return View("Edit", vm);
		}
		[HttpPost]
		public async Task<IActionResult> Edit(int id, CreatePurchaseRequestVM model)
		{
			var request = await _context.PurchaseRequests
				.Include(x => x.Items)
				.FirstOrDefaultAsync(x => x.Id == id);

			if (request.Status != RequestStatus.Draft)
				return RedirectToAction("Index");

			request.Description = model.Description;
			request.Items.Clear();

			foreach (var item in model.Items)
			{
				request.Items.Add(new PurchaseRequestItem
				{
					ProductId = item.ProductId,
					ProductName = item.ProductName,
					Quantity = item.Quantity,
					Unit = item.Unit
				});
			}

			await _context.SaveChangesAsync();

			return RedirectToAction("Index");
		}
	}
}
