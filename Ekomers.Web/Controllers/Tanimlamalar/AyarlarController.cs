
using Ekomers.Data;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Tanimlamalar,Admin")]
	public class AyarlarController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IMemoryCache _cache;
		public AyarlarController(ApplicationDbContext context, IMemoryCache cache)
		{
			_context = context;
			_cache = cache;
		}

		// GET: AktiviteTur
		//[Authorize(Policy = "AktiviteTur")]
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			return View();
		}
		[HttpPost]
		public IActionResult CacheTemizle()
		{
			// Tüm cache'i temizler
			(_cache as MemoryCache)?.Compact(1.0);

			return Json(new { success = true, message = "Cache başarıyla temizlendi." });
		}

	}
}
