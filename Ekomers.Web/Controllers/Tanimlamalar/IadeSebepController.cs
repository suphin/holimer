 

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
	public class IadeSebepController : Controller
	{
		private readonly ApplicationDbContext _context;

		public IadeSebepController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: IadeSebep
		//[Authorize(Policy = "IadeSebep")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.IadeSebep.ToListAsync());
		}

		// GET: IadeSebep/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var IadeSebep = await _context.IadeSebep
				.FirstOrDefaultAsync(m => m.ID == id);
			if (IadeSebep == null)
			{
				return NotFound();
			}

			return View(IadeSebep);
		}

		// GET: IadeSebep/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] IadeSebep IadeSebep)
		{
			if (ModelState.IsValid)
			{
				IadeSebep.IsActive = true;
				IadeSebep.IsDelete = false;
				IadeSebep.CreateDate = DateTime.Now;
				if (IadeSebep.Aciklama==null)
				{
					IadeSebep.Aciklama = IadeSebep.Ad;
				}
				_context.Add(IadeSebep);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(IadeSebep);
		}

		// GET: IadeSebep/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var IadeSebep = await _context.IadeSebep.FindAsync(id);
			if (IadeSebep == null)
			{
				return NotFound();
			}
			return View(IadeSebep);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] IadeSebep IadeSebep)
		{
			if (id != IadeSebep.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(IadeSebep);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!IadeSebepExists(IadeSebep.ID))
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
			return View(IadeSebep);
		}

		// GET: IadeSebep/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var IadeSebep = await _context.IadeSebep
				.FirstOrDefaultAsync(m => m.ID == id);
			if (IadeSebep == null)
			{
				return NotFound();
			}

			return View(IadeSebep);
		}

		// POST: IadeSebep/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var IadeSebep = await _context.IadeSebep.FindAsync(id);
			if (IadeSebep != null)
			{
				IadeSebep.IsDelete = true;
				IadeSebep.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool IadeSebepExists(int id)
		{
			return _context.IadeSebep.Any(e => e.ID == id);
		}
	}
}
