using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IUretimService : IGenelService<UretimVM, Uretim>, IPaggingService<UretimVM>
	{
		Task<List<UretimUrunlerVM>> UretimUrunlerGetir(int UretimID);
		Task<List<UretimParametreDeger>> UrerimParametreGetir(int UretimID);
		Task<bool> KismiTeslimatEkle(UretimTeslimat model);
		Task<bool> KismiTeslimatCikar(int teslimatID);
		Task<List<UretimTeslimat>> KismiTeslimatGetir(int UretimID);
	}
}
