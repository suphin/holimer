 

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
	public class MusteriTipController : Controller
	{
		private readonly ApplicationDbContext _context;

		public MusteriTipController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: MusteriTip
		//[Authorize(Policy = "MusteriTip")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.MusteriTip.ToListAsync());
		}

		// GET: MusteriTip/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var MusteriTip = await _context.MusteriTip
				.FirstOrDefaultAsync(m => m.ID == id);
			if (MusteriTip == null)
			{
				return NotFound();
			}

			return View(MusteriTip);
		}

		// GET: MusteriTip/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] MusteriTip MusteriTip)
		{
			if (ModelState.IsValid)
			{
				MusteriTip.IsActive = true;
				MusteriTip.IsDelete = false;
				MusteriTip.CreateDate = DateTime.Now;
				_context.Add(MusteriTip);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(MusteriTip);
		}

		// GET: MusteriTip/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var MusteriTip = await _context.MusteriTip.FindAsync(id);
			if (MusteriTip == null)
			{
				return NotFound();
			}
			return View(MusteriTip);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] MusteriTip MusteriTip)
		{
			if (id != MusteriTip.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(MusteriTip);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!MusteriTipExists(MusteriTip.ID))
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
			return View(MusteriTip);
		}

		// GET: MusteriTip/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var MusteriTip = await _context.MusteriTip
				.FirstOrDefaultAsync(m => m.ID == id);
			if (MusteriTip == null)
			{
				return NotFound();
			}

			return View(MusteriTip);
		}

		// POST: MusteriTip/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var MusteriTip = await _context.MusteriTip.FindAsync(id);
			if (MusteriTip != null)
			{
				MusteriTip.IsDelete = true;
				MusteriTip.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool MusteriTipExists(int id)
		{
			return _context.MusteriTip.Any(e => e.ID == id);
		}
	}
}
