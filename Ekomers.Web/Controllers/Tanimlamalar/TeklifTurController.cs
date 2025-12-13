
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
	public class TeklifTurController : Controller
	{
		private readonly ApplicationDbContext _context;

		public TeklifTurController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: TeklifTur
		//[Authorize(Policy = "TeklifTur")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.TeklifTur.ToListAsync());
		}

		// GET: TeklifTur/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var TeklifTur = await _context.TeklifTur
				.FirstOrDefaultAsync(m => m.ID == id);
			if (TeklifTur == null)
			{
				return NotFound();
			}

			return View(TeklifTur);
		}

		// GET: TeklifTur/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] TeklifTur TeklifTur)
		{
			if (ModelState.IsValid)
			{
				TeklifTur.IsActive = true;
				TeklifTur.IsDelete = false;
				TeklifTur.CreateDate = DateTime.Now;
				_context.Add(TeklifTur);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(TeklifTur);
		}

		// GET: TeklifTur/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var TeklifTur = await _context.TeklifTur.FindAsync(id);
			if (TeklifTur == null)
			{
				return NotFound();
			}
			return View(TeklifTur);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] TeklifTur TeklifTur)
		{
			if (id != TeklifTur.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(TeklifTur);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!TeklifTurExists(TeklifTur.ID))
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
			return View(TeklifTur);
		}

		// GET: TeklifTur/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var TeklifTur = await _context.TeklifTur
				.FirstOrDefaultAsync(m => m.ID == id);
			if (TeklifTur == null)
			{
				return NotFound();
			}

			return View(TeklifTur);
		}

		// POST: TeklifTur/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var TeklifTur = await _context.TeklifTur.FindAsync(id);
			if (TeklifTur != null)
			{
				TeklifTur.IsDelete = true;
				TeklifTur.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool TeklifTurExists(int id)
		{
			return _context.TeklifTur.Any(e => e.ID == id);
		}
	}
}
