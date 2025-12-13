using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Web.Component
{
	 
	public class MalzemeFiyatYonetimi : ViewComponent
	{
		private readonly IMalzemeService _malzemeService;
		private readonly ICacheService<DovizTur> _dovizTurCache;
		public MalzemeFiyatYonetimi(IMalzemeService malzemeService, ICacheService<DovizTur> dovizTurCache)
		{
			_malzemeService = malzemeService;
			_dovizTurCache = dovizTurCache;
		}

		// Veriyi almak için bir servis veya repository eklenebilir
		public async Task<IViewComponentResult> InvokeAsync(int VeriID)
		{
			ViewBag.MalzemeDovizListe = await _dovizTurCache.GetListeAsync(CacheKeys.MalzemeDovizAll);
			 
			// Veri işlemi burada yapılır (örneğin, veritabanından dosya bilgileri alınır)
			MalzemelerVM model = new()
			{
				MalzemeFiyatListe = await _malzemeService.FiyatGetir(VeriID),
				ID=VeriID
			};
			return  View(model);
 
		}

		 
	}
}
