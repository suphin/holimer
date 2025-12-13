 

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
	public class SiparisIadeDurumController : Controller
	{
		private readonly ApplicationDbContext _context;

		public SiparisIadeDurumController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: SiparisIadeDurum
		//[Authorize(Policy = "SiparisIadeDurum")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.SiparisIadeDurum.ToListAsync());
		}

		// GET: SiparisIadeDurum/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var SiparisIadeDurum = await _context.SiparisIadeDurum
				.FirstOrDefaultAsync(m => m.ID == id);
			if (SiparisIadeDurum == null)
			{
				return NotFound();
			}

			return View(SiparisIadeDurum);
		}

		// GET: SiparisIadeDurum/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] SiparisIadeDurum SiparisIadeDurum)
		{
			if (ModelState.IsValid)
			{
				SiparisIadeDurum.IsActive = true;
				SiparisIadeDurum.IsDelete = false;
				SiparisIadeDurum.CreateDate = DateTime.Now;
				if (SiparisIadeDurum.Aciklama==null)
				{
					SiparisIadeDurum.Aciklama = SiparisIadeDurum.Ad;
				}
				_context.Add(SiparisIadeDurum);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(SiparisIadeDurum);
		}

		// GET: SiparisIadeDurum/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var SiparisIadeDurum = await _context.SiparisIadeDurum.FindAsync(id);
			if (SiparisIadeDurum == null)
			{
				return NotFound();
			}
			return View(SiparisIadeDurum);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] SiparisIadeDurum SiparisIadeDurum)
		{
			if (id != SiparisIadeDurum.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(SiparisIadeDurum);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!SiparisIadeDurumExists(SiparisIadeDurum.ID))
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
			return View(SiparisIadeDurum);
		}

		// GET: SiparisIadeDurum/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var SiparisIadeDurum = await _context.SiparisIadeDurum
				.FirstOrDefaultAsync(m => m.ID == id);
			if (SiparisIadeDurum == null)
			{
				return NotFound();
			}

			return View(SiparisIadeDurum);
		}

		// POST: SiparisIadeDurum/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var SiparisIadeDurum = await _context.SiparisIadeDurum.FindAsync(id);
			if (SiparisIadeDurum != null)
			{
				SiparisIadeDurum.IsDelete = true;
				SiparisIadeDurum.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool SiparisIadeDurumExists(int id)
		{
			return _context.SiparisIadeDurum.Any(e => e.ID == id);
		}
	}
}
