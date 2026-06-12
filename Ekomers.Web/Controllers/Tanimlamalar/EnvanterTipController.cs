
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
using System.Security.AccessControl;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Tanimlamalar,Admin")]
	public class EnvanterTipController : Controller
	{
		private readonly ApplicationDbContext _context;
		private string modul = "Tanimlamalar";
		public EnvanterTipController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: EnvanterTip
		//[Authorize(Policy = "EnvanterTip")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = modul;
			return View(await _context.EnvanterTip.ToListAsync());
		}

		// GET: EnvanterTip/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var EnvanterTip = await _context.EnvanterTip
				.FirstOrDefaultAsync(m => m.ID == id);
			if (EnvanterTip == null)
			{
				return NotFound();
			}

			return View(EnvanterTip);
		}

		// GET: EnvanterTip/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			ViewBag.Modul = modul;
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] EnvanterTip EnvanterTip)
		{
			if (ModelState.IsValid)
			{
				EnvanterTip.IsActive = true;
				EnvanterTip.IsDelete = false;
				EnvanterTip.CreateDate = DateTime.Now;
				_context.Add(EnvanterTip);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(EnvanterTip);
		}

		// GET: EnvanterTip/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var EnvanterTip = await _context.EnvanterTip.FindAsync(id);
			if (EnvanterTip == null)
			{
				return NotFound();
			}
			return View(EnvanterTip);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] EnvanterTip EnvanterTip)
		{
			if (id != EnvanterTip.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(EnvanterTip);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!EnvanterTipExists(EnvanterTip.ID))
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
			return View(EnvanterTip);
		}

		// GET: EnvanterTip/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			ViewBag.Modul = modul;
			if (id == null)
			{
				return NotFound();
			}

			var EnvanterTip = await _context.EnvanterTip
				.FirstOrDefaultAsync(m => m.ID == id);
			if (EnvanterTip == null)
			{
				return NotFound();
			}

			return View(EnvanterTip);
		}

		// POST: EnvanterTip/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var EnvanterTip = await _context.EnvanterTip.FindAsync(id);
			if (EnvanterTip != null)
			{
				EnvanterTip.IsDelete = true;
				EnvanterTip.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool EnvanterTipExists(int id)
		{
			return _context.EnvanterTip.Any(e => e.ID == id);
		}

		public IActionResult OzellikEkle(int id)
		{
			ViewBag.Modul = modul;
			ViewBag.EnvanterTipID = id;
			var ozellikler = _context.EnvanterTipOzellik.Where(x => x.EnvanterTipID == id && x.IsDelete == false).ToList();


			return View(ozellikler);
		}
		[HttpPost]
		public IActionResult OzellikEkle(EnvanterTipOzellik ozellik)
		{
			 var newozellik= new EnvanterTipOzellik();
			newozellik.Ad = ozellik.Ad;
			newozellik.Aciklama = ozellik.Ad;
			newozellik.IsDelete = false;
			newozellik.IsActive = true;
			newozellik.CreateDate = DateTime.Now;
			newozellik.EnvanterTipID = ozellik.EnvanterTipID;
			_context.EnvanterTipOzellik.Add(newozellik);
			_context.SaveChanges();
			 return RedirectToAction("OzellikEkle", new { id = ozellik.EnvanterTipID });
		}
	}
}
