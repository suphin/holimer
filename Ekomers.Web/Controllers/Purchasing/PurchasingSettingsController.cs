using Ekomers.Data;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Web.Controllers.Purchasing
{
	[Authorize(Policy = "AdminOrPurchasing")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class PurchasingSettingsController :  BaseController
	{
		private readonly ApplicationDbContext _context;
		public PurchasingSettingsController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager, ApplicationDbContext context): base(userManager, roleManager) 
		{
			_context = context;
		}
		 
		public IActionResult Index()
		{
			return View();
		}
		public async Task<IActionResult> Talep()
		{
			return await GetPage(MailNotificationType.Talep);
		}
		public async Task<IActionResult> Teklif()
		{
			return await GetPage(MailNotificationType.Teklif);
		}
		private async Task<IActionResult> GetPage(MailNotificationType type)
		{
			var users = _userManager.Users
				.Select(x => new SelectListItem
				{
					Value = x.Id,
					Text = x.Email
				}).ToList();

			var selectedIds = _context.MailNotificationUsers
				.Where(x => x.Type == type)
				.Select(x => x.UserId)
				.ToList();

			var vm = new MailSettingVM
			{
				Users = users,
				SelectedUserIds = selectedIds,
				Type = type
			};

			return View("Edit", vm);
		}
		[HttpPost]
		public async Task<IActionResult> Save(MailSettingVM model)
		{
			var existing = _context.MailNotificationUsers
				.Where(x => x.Type == model.Type);

			_context.MailNotificationUsers.RemoveRange(existing);

			var newList = model.SelectedUserIds?.Select(x => new MailNotificationUser
			{
				UserId = x,
				Type = model.Type,
				CreatedDate = DateTime.Now
			}) ?? new List<MailNotificationUser>();

			await _context.MailNotificationUsers.AddRangeAsync(newList);
			await _context.SaveChangesAsync();

			return RedirectToAction("Index");
		}
		public IActionResult Email()
		{
			var users = _userManager.Users
					.Select(x => new SelectListItem
					{
						Value = x.Id,
						Text = x.Email // veya AdSoyad
					}).ToList();


			var selectedUserIds = _context.MailNotificationUsers
					.Where(x => x.Type == MailNotificationType.Talep)
					.Select(x => x.UserId)
					.ToList();



			var model = new MailSettingVM
			{
				Users = users,
				SelectedUserIds = selectedUserIds
			};
			return View(model);
		}
	}
}
