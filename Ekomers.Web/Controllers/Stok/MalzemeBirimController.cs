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
	public class MalzemeBirimController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MalzemeBirimController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MalzemeBirim
        public async Task<IActionResult> Index()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.MalzemeBirim.ToListAsync());
        }

        // GET: MalzemeBirim/Details/5
        public async Task<IActionResult> Details(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var malzemeBirim = await _context.MalzemeBirim
                .FirstOrDefaultAsync(m => m.ID == id);
            if (malzemeBirim == null)
            {
                return NotFound();
            }

            return View(malzemeBirim);
        }

		// GET: MalzemeBirim/Create
		[Authorize(Roles = "Add")]
		public IActionResult Create()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View();
        }

        // POST: MalzemeBirim/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Add")]
		public async Task<IActionResult> Create([Bind("Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] MalzemeBirim malzemeBirim)
        {
            if (ModelState.IsValid)
            {
                _context.Add(malzemeBirim);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(malzemeBirim);
        }

		// GET: MalzemeBirim/Edit/5
		[Authorize(Roles = "Edit")]
		public async Task<IActionResult> Edit(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var malzemeBirim = await _context.MalzemeBirim.FindAsync(id);
            if (malzemeBirim == null)
            {
                return NotFound();
            }
            return View(malzemeBirim);
        }

        // POST: MalzemeBirim/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] MalzemeBirim malzemeBirim)
        {
            if (id != malzemeBirim.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(malzemeBirim);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MalzemeBirimExists(malzemeBirim.ID))
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
            return View(malzemeBirim);
        }

		// GET: MalzemeBirim/Delete/5
		[Authorize(Roles = "Delete")]
		public async Task<IActionResult> Delete(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var malzemeBirim = await _context.MalzemeBirim
                .FirstOrDefaultAsync(m => m.ID == id);
            if (malzemeBirim == null)
            {
                return NotFound();
            }

            return View(malzemeBirim);
        }

        // POST: MalzemeBirim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
		[Authorize(Roles = "Delete")]
		public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var malzemeBirim = await _context.MalzemeBirim.FindAsync(id);
            if (malzemeBirim != null)
            {
				malzemeBirim.IsDelete = true;
				malzemeBirim.DeleteDate = DateTime.Now;
			}

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MalzemeBirimExists(int id)
        {
            return _context.MalzemeBirim.Any(e => e.ID == id);
        }
    }
}
