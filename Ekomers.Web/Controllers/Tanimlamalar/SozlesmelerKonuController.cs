
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
	public class SozlesmelerKonuController : Controller
	{
		private readonly ApplicationDbContext _context;

		public SozlesmelerKonuController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: SozlesmelerKonu
		//[Authorize(Policy = "SozlesmelerKonu")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.SozlesmelerKonu.ToListAsync());
		}

		// GET: SozlesmelerKonu/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var SozlesmelerKonu = await _context.SozlesmelerKonu
				.FirstOrDefaultAsync(m => m.ID == id);
			if (SozlesmelerKonu == null)
			{
				return NotFound();
			}

			return View(SozlesmelerKonu);
		}

		// GET: SozlesmelerKonu/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] SozlesmelerKonu SozlesmelerKonu)
		{
			if (ModelState.IsValid)
			{
				SozlesmelerKonu.IsActive = true;
				SozlesmelerKonu.IsDelete = false;
				SozlesmelerKonu.CreateDate = DateTime.Now;
				_context.Add(SozlesmelerKonu);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(SozlesmelerKonu);
		}

		// GET: SozlesmelerKonu/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var SozlesmelerKonu = await _context.SozlesmelerKonu.FindAsync(id);
			if (SozlesmelerKonu == null)
			{
				return NotFound();
			}
			return View(SozlesmelerKonu);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] SozlesmelerKonu SozlesmelerKonu)
		{
			if (id != SozlesmelerKonu.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(SozlesmelerKonu);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!SozlesmelerKonuExists(SozlesmelerKonu.ID))
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
			return View(SozlesmelerKonu);
		}

		// GET: SozlesmelerKonu/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var SozlesmelerKonu = await _context.SozlesmelerKonu
				.FirstOrDefaultAsync(m => m.ID == id);
			if (SozlesmelerKonu == null)
			{
				return NotFound();
			}

			return View(SozlesmelerKonu);
		}

		// POST: SozlesmelerKonu/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var SozlesmelerKonu = await _context.SozlesmelerKonu.FindAsync(id);
			if (SozlesmelerKonu != null)
			{
				SozlesmelerKonu.IsDelete = true;
				SozlesmelerKonu.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool SozlesmelerKonuExists(int id)
		{
			return _context.SozlesmelerKonu.Any(e => e.ID == id);
		}
	}
}
