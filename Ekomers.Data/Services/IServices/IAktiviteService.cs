using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IAktiviteService :IGenelService<AktiviteVM,Aktivite >, IPaggingService<AktiviteVM>
	{
	}
}
