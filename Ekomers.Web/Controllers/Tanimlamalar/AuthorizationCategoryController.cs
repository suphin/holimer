
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
	public class AuthorizationCategoryController : Controller
	{
		private readonly ApplicationDbContext _context;

		public AuthorizationCategoryController(ApplicationDbContext context)
		{
			_context = context;
		}

		// GET: AuthorizationCategory
		//[Authorize(Policy = "AuthorizationCategory")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View(await _context.AuthorizationCategory.ToListAsync());
		}

		// GET: AuthorizationCategory/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var AuthorizationCategory = await _context.AuthorizationCategory
				.FirstOrDefaultAsync(m => m.ID == id);
			if (AuthorizationCategory == null)
			{
				return NotFound();
			}

			return View(AuthorizationCategory);
		}

		// GET: AuthorizationCategory/Create
		[Authorize(Policy = "Create")]
		public IActionResult Create()
		{
			return View();
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Create")]
		public async Task<IActionResult> Create([Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] AuthorizationCategory AuthorizationCategory)
		{
			if (ModelState.IsValid)
			{
				AuthorizationCategory.IsActive = true;
				AuthorizationCategory.IsDelete = false;
				AuthorizationCategory.CreateDate = DateTime.Now;
				_context.Add(AuthorizationCategory);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			return View(AuthorizationCategory);
		}

		// GET: AuthorizationCategory/Edit/5
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var AuthorizationCategory = await _context.AuthorizationCategory.FindAsync(id);
			if (AuthorizationCategory == null)
			{
				return NotFound();
			}
			return View(AuthorizationCategory);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Policy = "Edit")]
		public async Task<IActionResult> Edit(int id, [Bind("Ad,Aciklama,ID,IsActive,IsDelete,CreateDate,DeleteDate")] AuthorizationCategory AuthorizationCategory)
		{
			if (id != AuthorizationCategory.ID)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(AuthorizationCategory);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!AuthorizationCategoryExists(AuthorizationCategory.ID))
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
			return View(AuthorizationCategory);
		}

		// GET: AuthorizationCategory/Delete/5
		[Authorize(Policy = "Delete")]
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var AuthorizationCategory = await _context.AuthorizationCategory
				.FirstOrDefaultAsync(m => m.ID == id);
			if (AuthorizationCategory == null)
			{
				return NotFound();
			}

			return View(AuthorizationCategory);
		}

		// POST: AuthorizationCategory/Delete/5
		[Authorize(Policy = "Delete")]
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var AuthorizationCategory = await _context.AuthorizationCategory.FindAsync(id);
			if (AuthorizationCategory != null)
			{
				AuthorizationCategory.IsDelete = true;
				AuthorizationCategory.DeleteDate = DateTime.Now;
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool AuthorizationCategoryExists(int id)
		{
			return _context.AuthorizationCategory.Any(e => e.ID == id);
		}
	}
}
