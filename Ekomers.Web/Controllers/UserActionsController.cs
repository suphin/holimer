

using Ekomers.Data;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class UserActionsController : BaseController
	{
		private readonly ApplicationDbContext _context;
		public UserActionsController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			ApplicationDbContext context
			) : base(userManager, roleManager)
		{
			_context = context;
		}

		[HttpGet]
		public async Task<IActionResult> ShortcutStatus(string? url, CancellationToken ct)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var pageUrl = NormalizePageUrl(url);
			if (string.IsNullOrWhiteSpace(userId) || pageUrl == null)
				return BadRequest(new { message = "Sayfa adresi geçersiz." });

			var isAdded = await _context.UserShortCut.AsNoTracking().AnyAsync(x =>
				x.UserID == userId && x.PageUrl == pageUrl && x.IsDelete != true && x.IsActive != false, ct);
			return Ok(new { isAdded });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ShortcutToggle(string? url, string? title, CancellationToken ct)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var pageUrl = NormalizePageUrl(url);
			if (string.IsNullOrWhiteSpace(userId) || pageUrl == null)
				return BadRequest(new { message = "Sayfa adresi geçersiz." });

			var shortcut = await _context.UserShortCut
				.FirstOrDefaultAsync(x => x.UserID == userId && x.PageUrl == pageUrl, ct);

			if (shortcut != null && shortcut.IsDelete != true && shortcut.IsActive != false)
			{
				shortcut.IsDelete = true;
				shortcut.IsActive = false;
				shortcut.DeleteDate = DateTime.Now;
				shortcut.DeleteUserID = userId;
				await _context.SaveChangesAsync(ct);
				return Ok(new { isAdded = false, message = "Kısayol kaldırıldı." });
			}

			var pageTitle = string.IsNullOrWhiteSpace(title)
				? PageTitleFromUrl(pageUrl)
				: title.Trim();
			pageTitle = pageTitle.Length > 200 ? pageTitle[..200] : pageTitle;

			if (shortcut == null)
			{
				var nextOrder = (await _context.UserShortCut
					.Where(x => x.UserID == userId && x.IsDelete != true)
					.MaxAsync(x => (int?)x.SortOrder, ct) ?? 0) + 1;

				shortcut = new UserShortCut
				{
					UserID = userId,
					PageUrl = pageUrl,
					PageTitle = pageTitle,
					SortOrder = nextOrder,
					IsActive = true,
					IsDelete = false,
					CreateDate = DateTime.Now,
					CreateUserID = userId
				};
				_context.UserShortCut.Add(shortcut);
			}
			else
			{
				shortcut.PageTitle = pageTitle;
				shortcut.IsActive = true;
				shortcut.IsDelete = false;
				shortcut.DeleteDate = null;
				shortcut.DeleteUserID = null;
				shortcut.UpdateDate = DateTime.Now;
				shortcut.UpdateUserID = userId;
			}

			await _context.SaveChangesAsync(ct);
			return Ok(new { isAdded = true, message = "Sayfa ana sayfa kısayollarına eklendi." });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> ShortcutRemove(int id, string? returnUrl, CancellationToken ct)
		{
			var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
			var shortcut = await _context.UserShortCut
				.FirstOrDefaultAsync(x => x.ID == id && x.UserID == userId && x.IsDelete != true, ct);

			if (shortcut != null)
			{
				shortcut.IsDelete = true;
				shortcut.IsActive = false;
				shortcut.DeleteDate = DateTime.Now;
				shortcut.DeleteUserID = userId;
				await _context.SaveChangesAsync(ct);
			}

			return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
				? LocalRedirect(returnUrl)
				: RedirectToAction("Member", "Home");
		}

		private static string? NormalizePageUrl(string? url)
		{
			if (string.IsNullOrWhiteSpace(url)) return null;
			var value = url.Trim();
			if (!value.StartsWith('/') || value.StartsWith("//", StringComparison.Ordinal) || value.Length > 1000)
				return null;

			var hashIndex = value.IndexOf('#');
			if (hashIndex >= 0) value = value[..hashIndex];
			return value;
		}

		private static string PageTitleFromUrl(string url)
		{
			var path = url.Split('?', 2)[0].Trim('/');
			return string.IsNullOrWhiteSpace(path) ? "Ana Sayfa" : path.Replace('/', ' ');
		}




	}
}
