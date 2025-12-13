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
    public class MalzemeHareketTurController : Controller
    {
        private readonly ApplicationDbContext _context;

        public MalzemeHareketTurController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: MalzemeHareketTur
        public async Task<IActionResult> Index()
        {
            return View(await _context.MalzemeHareketTur.ToListAsync());
        }

        // GET: MalzemeHareketTur/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var malzemeHareketTur = await _context.MalzemeHareketTur
                .FirstOrDefaultAsync(m => m.ID == id);
            if (malzemeHareketTur == null)
            {
                return NotFound();
            }

            return View(malzemeHareketTur);
        }

        // GET: MalzemeHareketTur/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: MalzemeHareketTur/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] MalzemeHareketTur malzemeHareketTur)
        {
            if (ModelState.IsValid)
            {
                _context.Add(malzemeHareketTur);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(malzemeHareketTur);
        }

        // GET: MalzemeHareketTur/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var malzemeHareketTur = await _context.MalzemeHareketTur.FindAsync(id);
            if (malzemeHareketTur == null)
            {
                return NotFound();
            }
            return View(malzemeHareketTur);
        }

        // POST: MalzemeHareketTur/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Ad,Kod,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate,CreateUserID,DeleteUserID,DosyaID")] MalzemeHareketTur malzemeHareketTur)
        {
            if (id != malzemeHareketTur.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(malzemeHareketTur);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MalzemeHareketTurExists(malzemeHareketTur.ID))
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
            return View(malzemeHareketTur);
        }

        // GET: MalzemeHareketTur/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var malzemeHareketTur = await _context.MalzemeHareketTur
                .FirstOrDefaultAsync(m => m.ID == id);
            if (malzemeHareketTur == null)
            {
                return NotFound();
            }

            return View(malzemeHareketTur);
        }

        // POST: MalzemeHareketTur/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var malzemeHareketTur = await _context.MalzemeHareketTur.FindAsync(id);
            if (malzemeHareketTur != null)
            {
				malzemeHareketTur.IsDelete = true;
				malzemeHareketTur.DeleteDate = DateTime.Now;
			}

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool MalzemeHareketTurExists(int id)
        {
            return _context.MalzemeHareketTur.Any(e => e.ID == id);
        }
    }
}
