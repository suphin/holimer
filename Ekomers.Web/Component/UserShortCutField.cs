 
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Component
{
	public class UserShortCutField : ViewComponent
	{
		private readonly IUserShortCutFieldService _Service;
		public UserShortCutField(IUserShortCutFieldService Service)
		{
			_Service = Service;
		}

		// Veriyi almak için bir servis veya repository eklenebilir
		public async Task<IViewComponentResult> InvokeAsync()
		{
			var model = new UserShortCutFieldVM
			{
				UserShortCutFieldVMListe = await _Service.VeriListele()
			};			 
			return View(model);
		}

	}
}