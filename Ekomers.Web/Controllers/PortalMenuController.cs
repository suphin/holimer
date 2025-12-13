 
using Ekomers.Data;

using Ekomers.Data.Services.IServices;
using Ekomers.Filters;
using Ekomers.Models.Ekomers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Ekomers.Web.Controllers
{
	[Authorize(Roles = "Admin,PortalMenu")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class PortalMenuController : BaseController
	{
		private readonly IPortalMenuService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		public PortalMenuController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager
			, IPortalMenuService service
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
			//ViewBag.Modul = "PortalMenu";
			await ViewBagListeDoldur();
			var model = new PortalMenuVM
			{
				PortalMenuVMListe = await _service.VeriListele()
			};

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Index(PortalMenuVM modelv)
		{
			//ViewBag.Modul = "PortalMenu";
			await ViewBagListeDoldur();
			var model = new PortalMenuVM
			{
				PortalMenuVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{

			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();


			modelc.ControllerName = "PortalMenu";
			modelc.ModalTitle = "PortalMenu";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(PortalMenuVM model)
		{
			bool sonuc = _service.VeriEkle(model);

			PageToastr(sonuc);
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(PortalMenuVM model)
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

		#region "Menuler"
		public  IActionResult  Menuler()
		{
			//ViewBag.Modul = "Destek";
			// Menu ağacını alıyoruz
			var MenuTree =  _service.GetMenuTree();
			var PortalMenuVM = new PortalMenuVM{
				PortalMenuTree = MenuTree
			};
			
			// Bu veriyi View'e gönderiyoruz
			return View(PortalMenuVM);
		}
		public async Task<PartialViewResult> MenuListesiGetir(int MenuID = 0)
		{
			var modelvm = new PortalMenuVM()
			{
				PortalMenuListe = await _service.MenuListele(MenuID)
			};
			return PartialView("_MenuListesiGetir", modelvm);
		}
		
		[Authorize(Roles = "Add")]
		[HttpPost]
		public async Task<PartialViewResult> Menu_VeriEkle(PortalMenu model)
		{
			bool sonuc = await _service.AltMenuVeriEkle(model);
			 
			var modelvm = new PortalMenuVM()
			{
				PortalMenuListe = await _service.MenuListele((int)model.ParentID)
			};
			return PartialView("_MenuListesiGetir", modelvm);
		}

		public async Task<PartialViewResult> Menu_VeriGetir(int GrupID)
		{
			PortalMenu modelc = await _service.MenuGetir(GrupID);
			return PartialView("MenuGetir", modelc);
		}
		public async Task<PartialViewResult> Menu_SilinecekVerileriGetir(int GrupID)
		{
			PortalMenu modelc = await _service.MenuGetir(GrupID);
			return PartialView("MenuSilinecek", modelc);
		}
		public async Task<PartialViewResult> Menu_KopyalanacakVerileriGetir(int GrupID)
		{
			PortalMenu modelc = await _service.MenuGetir(GrupID);
			return PartialView("MenuKopyalanacak", modelc);
		}
		[HttpPost]
		[Authorize(Roles = "Delete")]
		public async Task<IActionResult> Menu_VeriSil(MalzemeGrup modelc)
		{
			bool sonuc = await _service.MenuSil(modelc.ID);
			 
			var modelvm = new PortalMenuVM()
			{
				PortalMenuListe = await _service.MenuListele((int)modelc.ParentID)
			};
			return PartialView("_MenuListesiGetir", modelvm);
		}
		 
		 
		#endregion


	}
}
