using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public interface ISehirlerService : IGenelService<SehirlerVM, Sehirler>
	{
		Task<List<SehirlerVM>> GetSehirler(int ParametreID);
		Task<List<SehirlerVM>> GetMahalle(int ParametreID);

	}
}
