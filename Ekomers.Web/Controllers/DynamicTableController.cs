using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Admin")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class DynamicTableController : BaseController
    {
        private readonly IDynamicTableService _dynamicTableService;
        private string _userId;
        public DynamicTableController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager , IDynamicTableService dynamicTableService) : base(userManager, roleManager)
        {
            _dynamicTableService = dynamicTableService;
			 
		}
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            _userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        }
        public async Task<IActionResult> Index()
		{
            var dinamiktable =await _dynamicTableService.GetDynamicTableDataAsExpandoAsync("DenemeDinamik");
			return View(dinamiktable);
		}
		public IActionResult Create()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create(DynamicTableVM model)
        { 
            // Kullanıcıdan gelen alan ve tür bilgilerini dinamik tablo oluşturma servisine gönderiyoruz
            await _dynamicTableService.CreateDynamicTableForUserAsync(_userId, model.TableName,
                model.Columns.Select(c => (c.ColumnName, c.ColumnType)).ToList());

            return RedirectToAction("Index");
        }
    }
}
