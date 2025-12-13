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
    public class BirimController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BirimController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Birim
        public async Task<IActionResult> Index()
        {
			ViewBag.Modul = "Tanimlamalar";
			var applicationDbContext = _context.DepartmanBirim.Include(d => d.Departman).OrderBy(p=>p.Departman.Ad);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: Birim/Details/5
        public async Task<IActionResult> Details(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var departmanBirim = await _context.DepartmanBirim
                .Include(d => d.Departman)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (departmanBirim == null)
            {
                return NotFound();
            }

            return View(departmanBirim);
        }

        // GET: Birim/Create
        public IActionResult Create()
        {
			ViewBag.Modul = "Tanimlamalar";
			ViewData["DepartmanID"] = new SelectList(_context.Departman.OrderBy(p=>p.Ad), "ID", "Aciklama");
            return View();
        }

        // POST: Birim/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DepartmanID,Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] DepartmanBirim departmanBirim)
        {
            if (ModelState.IsValid)
            {
                _context.Add(departmanBirim);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["DepartmanID"] = new SelectList(_context.Departman.OrderBy(p => p.Ad), "ID", "Aciklama", departmanBirim.DepartmanID);
            return View(departmanBirim);
        }

        // GET: Birim/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var departmanBirim = await _context.DepartmanBirim.FindAsync(id);
            if (departmanBirim == null)
            {
                return NotFound();
            }
            ViewData["DepartmanID"] = new SelectList(_context.Departman.OrderBy(p => p.Ad), "ID", "Aciklama", departmanBirim.DepartmanID);
            return View(departmanBirim);
        }

        // POST: Birim/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DepartmanID,Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] DepartmanBirim departmanBirim)
        {
            if (id != departmanBirim.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(departmanBirim);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmanBirimExists(departmanBirim.ID))
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
            ViewData["DepartmanID"] = new SelectList(_context.Departman.OrderBy(p => p.Ad), "ID", "Aciklama", departmanBirim.DepartmanID);
            return View(departmanBirim);
        }

        // GET: Birim/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
			ViewBag.Modul = "Tanimlamalar";
			if (id == null)
            {
                return NotFound();
            }

            var departmanBirim = await _context.DepartmanBirim
                .Include(d => d.Departman)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (departmanBirim == null)
            {
                return NotFound();
            }

            return View(departmanBirim);
        }

        // POST: Birim/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var departmanBirim = await _context.DepartmanBirim.FindAsync(id);
            if (departmanBirim != null)
            {
				departmanBirim.IsDelete = true;
				departmanBirim.DeleteDate = DateTime.Now;
			}

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmanBirimExists(int id)
        {
            return _context.DepartmanBirim.Any(e => e.ID == id);
        }
    }
}
