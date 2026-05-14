using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services
{
	public interface IOrderService : IGenelService<OfferVM, Offer>, IPaggingService<OfferVM>
	{
		IQueryable<OfferVM> SiparisFirmaGrupListe();
		
		Task<PagedResult<SiparisFirmaGrupVM>> VeriListeleFirmaGrup(int page, int pageSize, CancellationToken ct = default);
	}
}
