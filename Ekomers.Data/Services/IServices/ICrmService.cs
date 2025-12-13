using Ekomers.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface ICrmService
	{
		Task<List<CrmKullaniciVM>> GetCrmKullanici(int yil);
		Task<CrmDashbordVM> GetDashBoard(int yil);
	}
}
