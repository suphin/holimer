 
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
	[Authorize(Roles = "Admin,TableMetadata")]
	[TypeFilter(typeof(ActionFilter))]
	[TypeFilter(typeof(ErrorFilter))]
	public class TableMetadataController : BaseController
	{
		private readonly ITableMetadataService _service;
		private string _userId;
		private readonly ApplicationDbContext _context;
		public TableMetadataController(UserManager<Kullanici> userManager, RoleManager<Rol> roleManager
			, ITableMetadataService service
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
			ViewBag.Modul = "TableMetadata";
			await ViewBagListeDoldur();
			var model = new TableMetadataVM
			{
				TableMetadataVMListe = await _service.VeriListele()
			};

			return View(model);
		}
		[HttpPost]
		public async Task<IActionResult> Index(TableMetadataVM modelv)
		{
			ViewBag.Modul = "TableMetadata";
			await ViewBagListeDoldur();
			var model = new TableMetadataVM
			{
				TableMetadataVMListe = await _service.VeriListele(modelv)
			};
			return View(model);
		}

		public async Task<PartialViewResult> VeriGoruntule(int VeriID = 0, string view = "")
		{

			var modelc = await _service.VeriGetir(VeriID);

			await ViewBagPartialListeDoldur();


			modelc.ControllerName = "TableMetadata";
			modelc.ModalTitle = "Dinamik Tablo";

			modelc.UserID = _userId;
			return PartialView(view, modelc);
		}

		[Authorize(Roles = "Editor")]
		[HttpPost]
		public async Task<IActionResult> VeriEkle(TableMetadataVM model)
		{
			bool sonuc = _service.VeriEkle(model);

			PageToastr(sonuc);
			return RedirectToAction("Index");
		}

		[HttpPost]
		[Authorize(Roles = "Editor")]
		public async Task<IActionResult> VeriSil(TableMetadataVM model)
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
