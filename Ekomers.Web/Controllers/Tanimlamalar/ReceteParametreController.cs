 

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
	public class ReceteParametreController : Controller
	{
		private readonly ApplicationDbContext _context;

		public ReceteParametreController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: ReceteParametre
		//[Authorize(Policy = "ReceteParametre")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.ReceteParametre.ToListAsync());
		}

		// GET: ReceteParametre/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var ReceteParametre = await _context.ReceteParametre
				.FirstOrDefaultAsync(m => m.ID == id);
			if (ReceteParametre == null)
			{
				return NotFound();
			}

			return View(ReceteParametre);
		}

		// GET: ReceteParametre/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] ReceteParametre ReceteParametre)
		{
			if (ModelState.IsValid)
			{
				ReceteParametre.IsActive = true;
				ReceteParametre.IsDelete = false;
				ReceteParametre.CreateDate = DateTime.Now;
				if (ReceteParametre.Aciklama==null)
				{
					ReceteParametre.Aciklama = ReceteParametre.Ad;
				}
				_context.Add(ReceteParametre);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(ReceteParametre);
		}

		// GET: ReceteParametre/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var ReceteParametre = await _context.ReceteParametre.FindAsync(id);
			if (ReceteParametre == null)
			{
				return NotFound();
			}
			return View(ReceteParametre);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] ReceteParametre ReceteParametre)
		{
			if (id != ReceteParametre.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(ReceteParametre);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!ReceteParametreExists(ReceteParametre.ID))
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
			return View(ReceteParametre);
		}

		// GET: ReceteParametre/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var ReceteParametre = await _context.ReceteParametre
				.FirstOrDefaultAsync(m => m.ID == id);
			if (ReceteParametre == null)
			{
				return NotFound();
			}

			return View(ReceteParametre);
		}

		// POST: ReceteParametre/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var ReceteParametre = await _context.ReceteParametre.FindAsync(id);
			if (ReceteParametre != null)
			{
				ReceteParametre.IsDelete = true;
				ReceteParametre.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool ReceteParametreExists(int id)
		{
			return _context.ReceteParametre.Any(e => e.ID == id);
		}
	}
}
