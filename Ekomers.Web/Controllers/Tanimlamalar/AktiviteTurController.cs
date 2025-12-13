
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
	public class AktiviteTurController : Controller
	{
		private readonly ApplicationDbContext _context;

		public AktiviteTurController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: AktiviteTur
		//[Authorize(Policy = "AktiviteTur")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.AktiviteTur.ToListAsync());
		}

		// GET: AktiviteTur/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var AktiviteTur = await _context.AktiviteTur
				.FirstOrDefaultAsync(m => m.ID == id);
			if (AktiviteTur == null)
			{
				return NotFound();
			}

			return View(AktiviteTur);
		}

		// GET: AktiviteTur/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] AktiviteTur AktiviteTur)
		{
			if (ModelState.IsValid)
			{
				AktiviteTur.IsActive = true;
				AktiviteTur.IsDelete = false;
				AktiviteTur.CreateDate = DateTime.Now;
				_context.Add(AktiviteTur);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(AktiviteTur);
		}

		// GET: AktiviteTur/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var AktiviteTur = await _context.AktiviteTur.FindAsync(id);
			if (AktiviteTur == null)
			{
				return NotFound();
			}
			return View(AktiviteTur);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] AktiviteTur AktiviteTur)
		{
			if (id != AktiviteTur.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(AktiviteTur);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!AktiviteTurExists(AktiviteTur.ID))
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
			return View(AktiviteTur);
		}

		// GET: AktiviteTur/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var AktiviteTur = await _context.AktiviteTur
				.FirstOrDefaultAsync(m => m.ID == id);
			if (AktiviteTur == null)
			{
				return NotFound();
			}

			return View(AktiviteTur);
		}

		// POST: AktiviteTur/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var AktiviteTur = await _context.AktiviteTur.FindAsync(id);
			if (AktiviteTur != null)
			{
				AktiviteTur.IsDelete = true;
				AktiviteTur.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool AktiviteTurExists(int id)
		{
			return _context.AktiviteTur.Any(e => e.ID == id);
		}
	}
}
