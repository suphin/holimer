using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Component
{
	public class CrmDashbord : ViewComponent
	{ 
		private readonly ICrmService _crmService;

		public CrmDashbord( ICrmService crmService)
		{ 
			_crmService = crmService;
		}
		public async Task<IViewComponentResult> InvokeAsync(int currentCategoryId, int currentProductId)
		{
			 

			var model =await _crmService.GetDashBoard(2025);


			return View(model);
		}
	}
}
