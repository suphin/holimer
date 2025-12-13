using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;


namespace Ekomers.Web.Component
{
	public class CrmLogYonetimi : ViewComponent
	{
		private readonly ILoggerService _loggerService;
		private readonly IFileService _fileService;
		private readonly IAktiviteService _activiteService;
		private readonly IFirsatService _firsatService;
		private readonly IMusterilerService _musterilerService;
		private readonly ITeklifService _teklifService;
		private readonly ISiparisService _siparisService;

		public CrmLogYonetimi(
			
			IFileService fileService,
			ILoggerService loggerService,
			IAktiviteService activiteService,
			IFirsatService firsatService,
			IMusterilerService musterilerService,
			ITeklifService teklifService,
			ISiparisService siparisService


			)
		{
			_loggerService = loggerService;
			_fileService = fileService;
			_activiteService = activiteService;
			_firsatService = firsatService;
			_musterilerService = musterilerService;
			_teklifService = teklifService;
			_siparisService = siparisService;


		}
	 
		// Veriyi almak için bir servis veya repository eklenebilir
		public async Task<IViewComponentResult> InvokeAsync(int MusteriID)
		{
			// Veri işlemi burada yapılır (örneğin, veritabanından dosya bilgileri alınır)
			 

			var crmLogModel = new CrmLogVM
			{
				MusteriID = MusteriID,

				ActiviteListe = await _activiteService.VeriListele(new AktiviteVM { MusteriID=MusteriID}),
				FirsatListe = await _firsatService.VeriListele(new FirsatVM { MusteriID = MusteriID }),
				Musteri = await _musterilerService.VeriGetir(MusteriID),
				TeklifListe = await _teklifService.VeriListele(new TeklifVM { MusteriID = MusteriID }),
				SiparisListe = await _siparisService.VeriListele(new SiparisVM { MusteriID = MusteriID }),
				 
			};
			 



			return View(crmLogModel);
		}
	 
		 
	}
}
