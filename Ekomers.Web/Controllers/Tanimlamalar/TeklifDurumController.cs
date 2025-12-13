
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Authorization;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Tanimlamalar,Admin")]
	public class TeklifDurumController : Controller
	{
		private readonly ApplicationDbContext _context;

		public TeklifDurumController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: TeklifDurum
		//[Authorize(Policy = "TeklifDurum")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.TeklifDurum.ToListAsync());
		}

		// GET: TeklifDurum/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var TeklifDurum = await _context.TeklifDurum
				.FirstOrDefaultAsync(m => m.ID == id);
			if (TeklifDurum == null)
			{
				return NotFound();
			}

			return View(TeklifDurum);
		}

		// GET: TeklifDurum/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] TeklifDurum TeklifDurum)
		{
			if (ModelState.IsValid)
			{
				TeklifDurum.IsActive = true;
				TeklifDurum.IsDelete = false;
				TeklifDurum.CreateDate = DateTime.Now;
				_context.Add(TeklifDurum);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(TeklifDurum);
		}

		// GET: TeklifDurum/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var TeklifDurum = await _context.TeklifDurum.FindAsync(id);
			if (TeklifDurum == null)
			{
				return NotFound();
			}
			return View(TeklifDurum);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] TeklifDurum TeklifDurum)
		{
			if (id != TeklifDurum.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(TeklifDurum);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!TeklifDurumExists(TeklifDurum.ID))
					{
						return NotFound();
					}
					else
					{
						throw;
					}
				}
				return RedirectToAction(nameof(Index));
			}
			return View(TeklifDurum);
		}

		// GET: TeklifDurum/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var TeklifDurum = await _context.TeklifDurum
				.FirstOrDefaultAsync(m => m.ID == id);
			if (TeklifDurum == null)
			{
				return NotFound();
			}

			return View(TeklifDurum);
		}

		// POST: TeklifDurum/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var TeklifDurum = await _context.TeklifDurum.FindAsync(id);
			if (TeklifDurum != null)
			{
				TeklifDurum.IsDelete = true;
				TeklifDurum.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool TeklifDurumExists(int id)
		{
			return _context.TeklifDurum.Any(e => e.ID == id);
		}
	}
}
