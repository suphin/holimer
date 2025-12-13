using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IMapService
	{
		Task<List<MapVM>> KoordinatGetir(int KayitID, int ModulID);
	}
}
