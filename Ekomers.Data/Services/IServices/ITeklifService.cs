using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
 
	public interface ITeklifService : IGenelService<TeklifVM, Teklif>, IPaggingService<TeklifVM>
	{
		Task<bool> TeklifUrunEkle(TeklifUrunlerVM modelv);
		Task<bool> TeklifUrunCikar(int urunId);
		Task<List<TeklifUrunlerVM>> TeklifUrunlerGetir(int teklifID);
        Task<List<TeklifIskonto>> TeklifIskontoGetir(int teklifID);
        Task<bool> TeklifIskontoCikar(int iskontoId);
        Task<bool> TeklifIskontoEkle(TeklifIskonto modelv);
    }
}
