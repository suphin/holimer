using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ekomers.Data;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Authorization;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Tanimlamalar,Admin")]
	public class EnvanterBolumController : Controller
	{
		private readonly ApplicationDbContext _context;
		private string modul = "Tanimlamalar";
		public EnvanterBolumController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: EnvanterBolum
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = modul;
			var liste = await _context.EnvanterBolum.Include(b => b.EnvanterDepartman).ToListAsync();
			return View(liste);
		}

		// GET: EnvanterBolum/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			ViewBag.Modul = modul;

			if (id == null)
			{
				return NotFound();
			}

			var envanterBolum = await _context.EnvanterBolum
				.Include(b => b.EnvanterDepartman)
				.FirstOrDefaultAsync(m => m.ID == id);
			if (envanterBolum == null)
			{
				return NotFound();
			}

			return View(envanterBolum);
		}

		// GET: EnvanterBolum/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			ViewBag.Modul = modul;
			ViewData["EnvanterDepartmanID"] = new SelectList(_context.EnvanterDepartman, "ID", "Ad");
			return View();
		}

		// POST: EnvanterBolum/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("EnvanterDepartmanID,Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] EnvanterBolum envanterBolum)
		{
			if (ModelState.IsValid)
			{
				envanterBolum.IsActive = true;
				envanterBolum.IsDelete = false;
				envanterBolum.CreateDate = DateTime.Now;
				_context.Add(envanterBolum);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			ViewData["EnvanterDepartmanID"] = new SelectList(_context.EnvanterDepartman, "ID", "Ad", envanterBolum.EnvanterDepartmanID);
			return View(envanterBolum);
		}

		// GET: EnvanterBolum/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var envanterBolum = await _context.EnvanterBolum.FindAsync(id);
			if (envanterBolum == null)
			{
				return NotFound();
			}
			ViewData["EnvanterDepartmanID"] = new SelectList(_context.EnvanterDepartman, "ID", "Ad", envanterBolum.EnvanterDepartmanID);
			return View(envanterBolum);
		}

		// POST: EnvanterBolum/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("EnvanterDepartmanID,Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] EnvanterBolum envanterBolum)
		{
			if (id != envanterBolum.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(envanterBolum);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!EnvanterBolumExists(envanterBolum.ID))
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
			ViewData["EnvanterDepartmanID"] = new SelectList(_context.EnvanterDepartman, "ID", "Ad", envanterBolum.EnvanterDepartmanID);
			return View(envanterBolum);
		}

		// GET: EnvanterBolum/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var envanterBolum = await _context.EnvanterBolum
				.Include(b => b.EnvanterDepartman)
				.FirstOrDefaultAsync(m => m.ID == id);
			if (envanterBolum == null)
			{
				return NotFound();
			}

			return View(envanterBolum);
		}

		// POST: EnvanterBolum/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var envanterBolum = await _context.EnvanterBolum.FindAsync(id);
			if (envanterBolum != null)
			{
				envanterBolum.IsDelete = true;
				envanterBolum.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool EnvanterBolumExists(int id)
		{
			return _context.EnvanterBolum.Any(e => e.ID == id);
		}
	}
}
