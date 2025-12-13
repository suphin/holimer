using Ekomers.Data.Services.IServices;
using Ekomers.Data.Services;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class SehirlerController : BaseController
	{

		//private readonly IMemoryCache _cache;
		private readonly ISehirlerService _service;
		//private readonly string CacheKey = "SehirlerVeriListesi"; // Cache anahtarı

		private string _userId;
		public SehirlerController(
			UserManager<Kullanici> userManager, 
			RoleManager<Rol> rolManager,
			IUserService userService,

			ISehirlerService service
			//,IMemoryCache cache
			)
			: base(userManager, rolManager)
		{
			_service = service;
			//_cache = cache;
		}
		public override void OnActionExecuting(ActionExecutingContext context)
		{
			_userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
		}
		public IActionResult Index()
		{
			return View();
		}
		public async Task<ActionResult> GetIlceler(int ParametreID = 0)
		{
			ViewBag.IlcelerListe = await _service.GetSehirler(ParametreID); 
			return PartialView("_Ilceler");
		}
		public async Task<ActionResult> GetMahalle(int ParametreID = 0)
		{
			ViewBag.IlcelerListe = await _service.GetMahalle(ParametreID);
			return PartialView("_Ilceler");
		}
	}
}
