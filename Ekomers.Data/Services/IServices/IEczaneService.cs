using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
    public interface IEczaneService:IGenelService<EczaneVM, Eczane>
	{
		Task<bool> EczaneAktar(List<Eczane> liste);
		Task<bool> OmtWebeGonder(string sehirlerCacheKeys);
		Task<bool> FwWebeGonder(string sehirlerCacheKeys);
		void FotoYukle(EczaneVM model);
	}
}
