using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IVergiDairesiService : IGenelService<VergiDairesiVM, VergiDairesi>
	{
		Task<List<VergiDairesiVM>> GetVergiDairesi(int ParametreID);

	}
}
