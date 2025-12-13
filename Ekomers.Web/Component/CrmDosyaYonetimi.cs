using Ekomers.Data.Services.IServices;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Component
{
	public class CrmDosyaYonetimi : ViewComponent
	{
		private readonly IFileService _fileService;
		public CrmDosyaYonetimi(IFileService fileService)
		{
			_fileService = fileService;
		}
	 
		// Veriyi almak için bir servis veya repository eklenebilir
		public async Task<IViewComponentResult> InvokeAsync(int VeriID, int ModulID, string DosyaYolu)
		{
			// Veri işlemi burada yapılır (örneğin, veritabanından dosya bilgileri alınır)
			var data = await GetFileDataAsync(VeriID, ModulID, DosyaYolu);

			// Görünüme model ile birlikte döndür
			return View(data);
		}
	 
		private async Task<DosyaYonetimiVM> GetFileDataAsync(int VeriID, int ModulID, string DosyaYolu)
		{
			// Bu örnek işlev, veriyi temsil eder; gerçek veri kaynağına bağlanabilirsiniz
			var model = new DosyaYonetimiVM
			{
				DosyaListe = await _fileService.DosyaGetir(VeriID, ModulID),
				KayitID = VeriID,
				DosyaYolu = DosyaYolu,
				ModulID = ModulID
			};
			return model;
		}
	}
}
