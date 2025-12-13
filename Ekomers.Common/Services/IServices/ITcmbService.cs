using Ekomers.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Common.Services.IServices
{
	public interface ITcmbService
	{
		Task<KurlarVM> DovizKuruGetir();

	}
}
