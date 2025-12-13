using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
 
	public interface ISiparisService : IGenelService<SiparisVM, Siparis>, IPaggingService<SiparisVM>
	{
		Task<bool> SiparisUrunEkle(SiparisUrunlerVM modelv);
		Task<bool> SiparisIskontoEkle(SiparisIskonto modelv);
		Task<bool> SiparisUrunCikar(int urunId);
		Task<bool> SiparisIskontoCikar(int iskontoId);
		Task<List<SiparisUrunlerVM>> SiparisUrunlerGetir(int SiparisID);
		Task<List<SiparisIskonto>> SiparisIskontoGetir(int SiparisID);
	}
}
