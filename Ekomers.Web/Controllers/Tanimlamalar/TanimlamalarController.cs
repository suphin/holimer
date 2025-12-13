using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers; 
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers
{
    [Authorize(Roles = "Tanimlamalar,Admin")]
    [TypeFilter(typeof(ActionFilter))]
    [TypeFilter(typeof(ErrorFilter))]
    public class TanimlamalarController : BaseController
    {
        private readonly ITanimlamalarService _tanimlamalarService;
        private IUserService _userService { get; set; }
        public TanimlamalarController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager, IUserService userService,
            ITanimlamalarService tanimlamalarService) : base(userManager, roleManager)
        {
            _tanimlamalarService = tanimlamalarService;
            _userService = userService;
        }

        public async Task<IActionResult> Imar()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View();
        }

        public async Task<IActionResult> Yardim()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View();
        }
        public async Task<IActionResult> Protokol()
        {
			ViewBag.Modul = "Tanimlamalar";
			return View();
        }
        public async Task<IActionResult> Ortak()
        {
            return View();
        }
    }
}
