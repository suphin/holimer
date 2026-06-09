using Ekomers.Data.Services.IServices;
using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services
{
	public interface IZimmetService : IGenelService<ZimmetVM,Zimmet>, IPaggingService<ZimmetVM>
	{
		Task<ZimmetVM> ZimmetGetir(int envanterID); 
	}
}
