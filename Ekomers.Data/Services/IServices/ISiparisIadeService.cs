using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
 
	public interface ISiparisIadeService : IGenelService<SiparisIadeVM, SiparisIade>, IPaggingService<SiparisIadeVM>
	{
		Task<bool> SiparisIadeUrunEkle(SiparisIadeUrunlerVM modelv);
		Task<bool> SiparisIadeUrunCikar(int urunId);
		Task<List<SiparisIadeUrunlerVM>> SiparisIadeUrunlerGetir(int SiparisIadeID);
	}
}
