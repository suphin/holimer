
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
	public class PersonelGorevController : Controller
	{
		private readonly ApplicationDbContext _context;
		private string modul = "Tanimlamalar";
		public PersonelGorevController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: PersonelGorev
		//[Authorize(Policy = "PersonelGorev")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = modul;
			return View(await _context.PersonelGorev.ToListAsync());
		}

		// GET: PersonelGorev/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var PersonelGorev = await _context.PersonelGorev
				.FirstOrDefaultAsync(m => m.ID == id);
			if (PersonelGorev == null)
			{
				return NotFound();
			}

			return View(PersonelGorev);
		}

		// GET: PersonelGorev/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			ViewBag.Modul = modul;
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] PersonelGorev PersonelGorev)
		{
			if (ModelState.IsValid)
			{
				PersonelGorev.IsActive = true;
				PersonelGorev.IsDelete = false;
				PersonelGorev.CreateDate = DateTime.Now;
				_context.Add(PersonelGorev);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(PersonelGorev);
		}

		// GET: PersonelGorev/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var PersonelGorev = await _context.PersonelGorev.FindAsync(id);
			if (PersonelGorev == null)
			{
				return NotFound();
			}
			return View(PersonelGorev);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] PersonelGorev PersonelGorev)
		{
			if (id != PersonelGorev.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(PersonelGorev);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!PersonelGorevExists(PersonelGorev.ID))
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
			return View(PersonelGorev);
		}

		// GET: PersonelGorev/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var PersonelGorev = await _context.PersonelGorev
				.FirstOrDefaultAsync(m => m.ID == id);
			if (PersonelGorev == null)
			{
				return NotFound();
			}

			return View(PersonelGorev);
		}

		// POST: PersonelGorev/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var PersonelGorev = await _context.PersonelGorev.FindAsync(id);
			if (PersonelGorev != null)
			{
				PersonelGorev.IsDelete = true;
				PersonelGorev.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool PersonelGorevExists(int id)
		{
			return _context.PersonelGorev.Any(e => e.ID == id);
		}
	}
}
