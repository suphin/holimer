using Ekomers.Data.Services.IServices;
using Ekomers.Models.Entity;

namespace Ekomers.Data.Services
{
	public interface IEnvanterService : IGenelService<EnvanterVM, Envanter>, IPaggingService<EnvanterVM>
	{
		void FotoYukle(EnvanterVM model);
		Task<List<EnvanterBolum>> GetBolumler(int ParametreID);

		Task<List<EnvanterVM>> VeriListeleZimmet(int personelID);
		
	}
}
