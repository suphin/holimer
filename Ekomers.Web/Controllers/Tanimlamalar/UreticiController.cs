 

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
	public class UreticiController : Controller
	{
		private readonly ApplicationDbContext _context;

		public UreticiController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: Uretici
		//[Authorize(Policy = "Uretici")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.Uretici.ToListAsync());
		}

		// GET: Uretici/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var Uretici = await _context.Uretici
				.FirstOrDefaultAsync(m => m.ID == id);
			if (Uretici == null)
			{
				return NotFound();
			}

			return View(Uretici);
		}

		// GET: Uretici/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] Uretici Uretici)
		{
			if (ModelState.IsValid)
			{
				Uretici.IsActive = true;
				Uretici.IsDelete = false;
				Uretici.CreateDate = DateTime.Now;
				if (Uretici.Aciklama==null)
				{
					Uretici.Aciklama = Uretici.Ad;
				}
				_context.Add(Uretici);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(Uretici);
		}

		// GET: Uretici/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var Uretici = await _context.Uretici.FindAsync(id);
			if (Uretici == null)
			{
				return NotFound();
			}
			return View(Uretici);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] Uretici Uretici)
		{
			if (id != Uretici.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(Uretici);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!UreticiExists(Uretici.ID))
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
			return View(Uretici);
		}

		// GET: Uretici/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var Uretici = await _context.Uretici
				.FirstOrDefaultAsync(m => m.ID == id);
			if (Uretici == null)
			{
				return NotFound();
			}

			return View(Uretici);
		}

		// POST: Uretici/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var Uretici = await _context.Uretici.FindAsync(id);
			if (Uretici != null)
			{
				Uretici.IsDelete = true;
				Uretici.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool UreticiExists(int id)
		{
			return _context.Uretici.Any(e => e.ID == id);
		}
	}
}
