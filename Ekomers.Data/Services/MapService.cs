using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class MapService : IMapService
	{
		public async Task<List<MapVM>> KoordinatGetir(int KayitID, int ModulID)
		{
			return  new List<MapVM>();
		}
	}
}
