using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services.IServices
{
	public interface ISozlesmelerService : IGenelService<SozlesmelerVM, Sozlesmeler>, IPaggingService<SozlesmelerVM>
	{
	}
}
