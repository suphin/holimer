
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Admin,CRM")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class UserShortCutFieldController : BaseController
	{
		private readonly IUserShortCutFieldService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		public UserShortCutFieldController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager,
			 IUserShortCutFieldService service
			, IWebHostEnvironment hostingEnvironment, IFileService fileService
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

		private async Task ViewBagListeDoldur()
		{

		}
		private async Task ViewBagPartialListeDoldur()
		{

		}
		private void PageToastr(bool sonuc)
		{
			if (sonuc)
			{
				TempData["SuccessMessage"] = "Kaydetme işlemi başarılı!";
			}
			else
			{
				TempData["ErrorMessage"] = "Bir hata oluştu.";
			}
		}
		public async Task<IActionResult> Index()
		{
			ViewBag.Modul = "Tanimlamalar";
			await ViewBagListeDoldur();
			var model = new UserShortCutFieldVM
			{
				UserShortCutFieldVMListe = await _service.VeriListele()
			};

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Index(UserShortCutFieldVM modelv)
		{
			ViewBag.Modul = "Tanimlamalar";
			await ViewBagListeDoldur();
			var model = new UserShortCutFieldVM
			{
				UserShortCutFieldVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{
			await ViewBagPartialListeDoldur();
			var modelc = await _service.VeriGetir(VeriID);

			modelc.ControllerName = "UserShortCutField";
			modelc.ModalTitle = "UserShortCutField Birimi";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public IActionResult VeriEkle(UserShortCutFieldVM model)
		{
			bool sonuc = _service.VeriEkle(model);

			PageToastr(sonuc);
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(UserShortCutFieldVM model)
		{
			bool sonuc = await _service.VeriSil(model.ID);
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

