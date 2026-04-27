using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services
{
	public interface IMailJobService
	{
		Task SendMailAsync(string to, string subject, string body);
	}
}
