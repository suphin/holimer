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
    public class DepartmanController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmanController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Departman
        public async Task<IActionResult> Index()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.Departman.ToListAsync());
        }

        // GET: Departman/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var departman = await _context.Departman
                .FirstOrDefaultAsync(m => m.ID == id);
            if (departman == null)
            {
                return NotFound();
            }

            return View(departman);
        }

		// GET: Departman/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
        {
            return View();
        }

        // POST: Departman/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] Departman departman)
        {
            if (ModelState.IsValid)
            {
                _context.Add(departman);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(departman);
        }

		// GET: Departman/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var departman = await _context.Departman.FindAsync(id);
            if (departman == null)
            {
                return NotFound();
            }
            return View(departman);
        }

        // POST: Departman/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] Departman departman)
        {
            if (id != departman.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(departman);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmanExists(departman.ID))
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
            return View(departman);
        }

		// GET: Departman/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var departman = await _context.Departman
                .FirstOrDefaultAsync(m => m.ID == id);
            if (departman == null)
            {
                return NotFound();
            }

            return View(departman);
        }

        // POST: Departman/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var departman = await _context.Departman.FindAsync(id);
            if (departman != null)
            {
               departman.IsDelete=true;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmanExists(int id)
        {
            return _context.Departman.Any(e => e.ID == id);
        }

        public async Task<ActionResult> GetBirim(int ParametreID = 0)
        {
            ViewBag.BirimlerListe = await _context.DepartmanBirim.Where(p=>p.DepartmanID== ParametreID).ToListAsync();
            return PartialView("_Birimler");
        }
    }
}
