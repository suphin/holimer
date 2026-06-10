
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
	public class EnvanterDepartmanController : Controller
	{
		private readonly ApplicationDbContext _context;
		private string modul = "Tanimlamalar";
		public EnvanterDepartmanController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: EnvanterDepartman
		//[Authorize(Policy = "EnvanterDepartman")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = modul;
			return View(await _context.EnvanterDepartman.ToListAsync());
		}

		// GET: EnvanterDepartman/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var EnvanterDepartman = await _context.EnvanterDepartman
				.FirstOrDefaultAsync(m => m.ID == id);
			if (EnvanterDepartman == null)
			{
				return NotFound();
			}

			return View(EnvanterDepartman);
		}

		// GET: EnvanterDepartman/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			ViewBag.Modul = modul;
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] EnvanterDepartman EnvanterDepartman)
		{
			if (ModelState.IsValid)
			{
				EnvanterDepartman.IsActive = true;
				EnvanterDepartman.IsDelete = false;
				EnvanterDepartman.CreateDate = DateTime.Now;
				_context.Add(EnvanterDepartman);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(EnvanterDepartman);
		}

		// GET: EnvanterDepartman/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var EnvanterDepartman = await _context.EnvanterDepartman.FindAsync(id);
			if (EnvanterDepartman == null)
			{
				return NotFound();
			}
			return View(EnvanterDepartman);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] EnvanterDepartman EnvanterDepartman)
		{
			if (id != EnvanterDepartman.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(EnvanterDepartman);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!EnvanterDepartmanExists(EnvanterDepartman.ID))
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
			return View(EnvanterDepartman);
		}

		// GET: EnvanterDepartman/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var EnvanterDepartman = await _context.EnvanterDepartman
				.FirstOrDefaultAsync(m => m.ID == id);
			if (EnvanterDepartman == null)
			{
				return NotFound();
			}

			return View(EnvanterDepartman);
		}

		// POST: EnvanterDepartman/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var EnvanterDepartman = await _context.EnvanterDepartman.FindAsync(id);
			if (EnvanterDepartman != null)
			{
				EnvanterDepartman.IsDelete = true;
				EnvanterDepartman.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool EnvanterDepartmanExists(int id)
		{
			return _context.EnvanterDepartman.Any(e => e.ID == id);
		}
	}
}
