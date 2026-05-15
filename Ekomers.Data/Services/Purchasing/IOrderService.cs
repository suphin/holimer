using Ekomers.Data.Services.IServices;
using Ekomers.Models;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;

namespace Ekomers.Data.Services
{
	public interface IOrderService : IGenelService<OfferVM, Offer>, IPaggingService<OfferVM>
	{
		IQueryable<OfferVM> SiparisFirmaGrupListe();

		Task<bool> SiparisOnay(int OfferID);
		Task<bool> SiparisTopluOnay(int FirmaID);
		
		Task<PagedResult<SiparisFirmaGrupVM>> VeriListeleFirmaGrup(int page, int pageSize, CancellationToken ct = default);
	}
}
