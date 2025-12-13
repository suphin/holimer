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
	public class MalzemeTipiController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MalzemeTipiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MalzemeTipi
        public async Task<IActionResult> Index()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.MalzemeTipi.ToListAsync());
        }

        // GET: MalzemeTipi/Details/5
        public async Task<IActionResult> Details(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var malzemeTipi = await _context.MalzemeTipi
                .FirstOrDefaultAsync(m => m.ID == id);
            if (malzemeTipi == null)
            {
                return NotFound();
            }

            return View(malzemeTipi);
        }

		// GET: MalzemeTipi/Create
		[Authorize(Roles = "Add")]
		public IActionResult Create()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View();
        }

        // POST: MalzemeTipi/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Add")]
		public async Task<IActionResult> Create([Bind("Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] MalzemeTipi malzemeTipi)
        {
            if (ModelState.IsValid)
            {
                _context.Add(malzemeTipi);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(malzemeTipi);
        }

		// GET: MalzemeTipi/Edit/5
		[Authorize(Roles = "Edit")]
		public async Task<IActionResult> Edit(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var malzemeTipi = await _context.MalzemeTipi.FindAsync(id);
            if (malzemeTipi == null)
            {
                return NotFound();
            }
            return View(malzemeTipi);
        }

        // POST: MalzemeTipi/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] MalzemeTipi malzemeTipi)
        {
            if (id != malzemeTipi.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(malzemeTipi);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MalzemeTipiExists(malzemeTipi.ID))
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
            return View(malzemeTipi);
        }

		// GET: MalzemeTipi/Delete/5
		[Authorize(Roles = "Delete")]
		public async Task<IActionResult> Delete(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var malzemeTipi = await _context.MalzemeTipi
                .FirstOrDefaultAsync(m => m.ID == id);
            if (malzemeTipi == null)
            {
                return NotFound();
            }

            return View(malzemeTipi);
        }

        // POST: MalzemeTipi/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var malzemeTipi = await _context.MalzemeTipi.FindAsync(id);
            if (malzemeTipi != null)
            {
                malzemeTipi.IsDelete = true;
                malzemeTipi.DeleteDate = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MalzemeTipiExists(int id)
        {
            return _context.MalzemeTipi.Any(e => e.ID == id);
        }
    }
}
