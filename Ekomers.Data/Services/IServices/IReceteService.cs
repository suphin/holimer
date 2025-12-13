using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IReceteService : IGenelService<ReceteVM, Recete>, IPaggingService<ReceteVM>
	{
		Task<bool> ReceteUrunEkle(ReceteUrunlerVM modelv);
		 
		Task<bool> ReceteUrunCikar(int urunId);
		Task<bool> ReceteParametreEkle(int ReceteID, List<int> SeciliParametreler);
		 
		Task<List<ReceteUrunlerVM>> ReceteUrunlerGetir(int ReceteID);
		Task<List<ReceteParametreDeger>> ReceteParametreGetir(int ReceteID);

		Task<bool> UretimParametreEkle(UretimParametreDegerVM model);
		Task<bool> UretimEkle(UretimVM model);
		Task<ReceteVM> VeriGetirUrunID(int urunID);


	}
}
