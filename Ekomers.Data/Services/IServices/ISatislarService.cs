using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
 
	public interface ISatislarService : IGenelService<SatislarVM, Satislar>, IPaggingService<SatislarVM>
	{
		Task<bool> SatislarUrunEkle(SatislarUrunlerVM modelv);
		Task<bool> SatislarUrunCikar(int urunId);
		Task<List<SatislarUrunlerVM>> SatislarUrunlerGetir(int SatislarID);
	}
}
