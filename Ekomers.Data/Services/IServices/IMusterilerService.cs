using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ekomers.Models.Entity;

namespace Ekomers.Data.Services.IServices
{
	public interface IMusterilerService:IGenelService<MusterilerVM,Musteriler>, IPaggingService<MusterilerVM>
	{
	}
}
