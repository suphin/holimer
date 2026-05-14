

using Ekomers.Data;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Admin,CRM")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class UserActionsController : BaseController
	{
		private readonly IUserActionService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		public UserActionsController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IUserActionService service
			 
			, ApplicationDbContext context
			) : base(userManager, roleManager)
		{
			_service = service;
			_context = context;
		}

		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

		}

		 
	 

		[HttpPost]
		[Authorize(Roles = "Admin,Editor")]
		public async Task<IActionResult> ShortcutAdd(UserActionVM model)
		{
			bool sonuc = true;// await _service.VeriSil(model.ID);
			if (sonuc)
			{
				return Ok(model.ID);
			}
			else
			{
				return BadRequest("Veri silinemedi.");
			}
		}




	}
}
