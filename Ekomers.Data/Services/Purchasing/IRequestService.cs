
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
 
	public interface IRequestService : IGenelService<RequestVM, Request>, IPaggingService<RequestVM>
	{
		Task<bool> RequestUrunEkle(RequestUrunlerVM modelv);	 
		Task<bool> RequestUrunGuncelle(RequestUrunlerVM modelv);	 
		Task<bool> RequestUrunDuzenle(RequestUrunlerVM modelv);	 
		Task<bool> RequestUrunCikar(int urunId);		 
		Task<int> RequestUrunDurum(int RequestID);		 
		Task<List<RequestUrunlerVM>> RequestUrunlerGetir(int RequestID);
		Task<RequestUrunlerVM> RequestUrunGetir(int UrunId);
		Task<RequestUrunlerVM> RequestUrunGetir(int RequestID,int UrunId);
		Task<PagedResult<RequestUrunlerVM>> UrunListeleAsync(int page, int pageSize, CancellationToken ct = default,int offerDurumID = 0); 
		Task<PagedResult<RequestUrunlerVM>> OfferListeleAsync(int page, int pageSize, CancellationToken ct = default,int offerDurumID = 0); 
		Task<PagedResult<RequestVM>> TalepListeleAsync(int page, int pageSize, CancellationToken ct = default,int durumID=0); 
		Task<PagedResult<RequestVM>> TalepListeleAsync(RequestVM requestVM); 

	}
}
