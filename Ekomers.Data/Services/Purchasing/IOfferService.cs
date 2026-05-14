
using Ekomers.Models;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
 
	public interface IOfferService : IGenelService<OfferVM, Offer>, IPaggingService<OfferVM>
	{

		Task<PagedResult<OfferVM>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default,int durumId=0);


	}
}
