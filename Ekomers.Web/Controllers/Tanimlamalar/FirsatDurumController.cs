
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
	public class FirsatDurumController : Controller
	{
		private readonly ApplicationDbContext _context;

		public FirsatDurumController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: FirsatDurum
		//[Authorize(Policy = "FirsatDurum")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.FirsatDurum.ToListAsync());
		}

		// GET: FirsatDurum/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var FirsatDurum = await _context.FirsatDurum
				.FirstOrDefaultAsync(m => m.ID == id);
			if (FirsatDurum == null)
			{
				return NotFound();
			}

			return View(FirsatDurum);
		}

		// GET: FirsatDurum/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] FirsatDurum FirsatDurum)
		{
			if (ModelState.IsValid)
			{
				FirsatDurum.IsActive = true;
				FirsatDurum.IsDelete = false;
				FirsatDurum.CreateDate = DateTime.Now;
				_context.Add(FirsatDurum);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(FirsatDurum);
		}

		// GET: FirsatDurum/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var FirsatDurum = await _context.FirsatDurum.FindAsync(id);
			if (FirsatDurum == null)
			{
				return NotFound();
			}
			return View(FirsatDurum);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] FirsatDurum FirsatDurum)
		{
			if (id != FirsatDurum.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(FirsatDurum);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!FirsatDurumExists(FirsatDurum.ID))
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
			return View(FirsatDurum);
		}

		// GET: FirsatDurum/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var FirsatDurum = await _context.FirsatDurum
				.FirstOrDefaultAsync(m => m.ID == id);
			if (FirsatDurum == null)
			{
				return NotFound();
			}

			return View(FirsatDurum);
		}

		// POST: FirsatDurum/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var FirsatDurum = await _context.FirsatDurum.FindAsync(id);
			if (FirsatDurum != null)
			{
				FirsatDurum.IsDelete = true;
				FirsatDurum.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool FirsatDurumExists(int id)
		{
			return _context.FirsatDurum.Any(e => e.ID == id);
		}
	}
}
