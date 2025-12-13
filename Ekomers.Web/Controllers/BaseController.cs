using Ekomers.Data.Repository.IRepository;
using Ekomers.Models.Ekomers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers
{
    public class BaseController : Controller
    {
        protected readonly IUnitOfWork _uow;
        protected UserManager<Kullanici> _userManager { get; }
        protected RoleManager<Rol> _roleManager { get; }
         public Kullanici CurrentUser => _userManager.FindByNameAsync(User.Identity.Name).Result;


        public BaseController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        public BaseController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager, IUnitOfWork uow)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _uow = uow;
        }

        public void AddModelError(IdentityResult result)
        {
            foreach (var item in result.Errors)
            {
                ModelState.AddModelError("", item.Description);
            }
        }
    }
}
