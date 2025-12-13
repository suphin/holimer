using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Microsoft.AspNetCore.Authorization;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Tanimlamalar,Admin")]
	public class MahalleController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MahalleController(ApplicationDbContext context)
        {
            _context = context;
        }

		// GET: Mahalle
		//[Authorize(Policy = "Mahalle")]
		public async Task<IActionResult> Index()
        {
            ViewBag.Modul = "Tanimlamalar";
            return View(await _context.Mahalle.ToListAsync());
        }

        // GET: Mahalle/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mahalle = await _context.Mahalle
                .FirstOrDefaultAsync(m => m.ID == id);
            if (mahalle == null)
            {
                return NotFound();
            }

            return View(mahalle);
        }

		// GET: Mahalle/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
        {
            return View();
        }

      
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] Mahalle mahalle)
        {
            if (ModelState.IsValid)
            {
                _context.Add(mahalle);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(mahalle);
        }

		// GET: Mahalle/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mahalle = await _context.Mahalle.FindAsync(id);
            if (mahalle == null)
            {
                return NotFound();
            }
            return View(mahalle);
        }

     
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] Mahalle mahalle)
        {
            if (id != mahalle.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(mahalle);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MahalleExists(mahalle.ID))
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
            return View(mahalle);
        }

		// GET: Mahalle/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var mahalle = await _context.Mahalle
                .FirstOrDefaultAsync(m => m.ID == id);
            if (mahalle == null)
            {
                return NotFound();
            }

            return View(mahalle);
        }

		// POST: Mahalle/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var mahalle = await _context.Mahalle.FindAsync(id);
            if (mahalle != null)
            {
                mahalle.IsDelete = true;
                mahalle.DeleteDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MahalleExists(int id)
        {
            return _context.Mahalle.Any(e => e.ID == id);
        }
    }
}
