using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    [TypeFilter(typeof(ActionFilter))]
    [TypeFilter(typeof(ErrorFilter))]
    public class UserController : BaseController
    {
         
        public UserController(UserManager<Kullanici> userManager, SignInManager<Kullanici> signInManager, RoleManager<Rol> rolManager 
             ) : base(userManager, rolManager)
        {
             
        }

        public IActionResult UserPageAuthorization()
        {
            ViewBag.Users = _userManager.Users.OrderBy(p=>p.AdSoyad).ToList();
            return View();
        }


        
        public    PartialViewResult _GetUserSubMenuAuth(string userID)
        {
            
            ViewBag.UserID = userID;
            return PartialView();
        }
        
    }
}
