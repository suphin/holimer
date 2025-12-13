using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface ILoggerService
	{
		Task AddCrmLog(string ControllerName, string ActionName, string Parameters, string Info, string UserName, string Details);
	}
}
