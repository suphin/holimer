using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Web.Component
{
	using Ekomers.Data;
	using Ekomers.Models.Enums;
	using Microsoft.AspNetCore.Mvc;
	using Microsoft.EntityFrameworkCore;

	public class PurchasingMenuViewComponent : ViewComponent
	{
		private readonly ApplicationDbContext _context;

		public PurchasingMenuViewComponent(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<IViewComponentResult> InvokeAsync()
		{
			var model = new PurchasingMenuVM();

			// Talep Sayıları
			model.ToplamTalep = await _context.Request.Where(a=> a.IsActive==true && a.IsDelete==false).CountAsync();

			model.TaslakTalep = await _context.Request
				.CountAsync(x => x.DurumID == (int)EnumRequestDurum.Taslak);	

			model.OnayBekleyenTalep = await _context.Request
				.CountAsync(x => x.DurumID == (int)EnumRequestDurum.OnayBekliyor);

			// Teklif Sayıları
			model.OnayBekleyenTeklif = await _context.RequestUrunler
				.CountAsync(x => x.OfferDurumID == (int)EnumOfferDurum.TeklifOnayBekliyor);
			model.TeklifAsamasında = await _context.RequestUrunler
				.CountAsync(x => x.OfferDurumID == (int)EnumOfferDurum.TeklifAsamasinda);

			return View(model);
		}
	}
}
