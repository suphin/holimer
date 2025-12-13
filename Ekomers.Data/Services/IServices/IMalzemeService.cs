using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IMalzemeService : IGenelService<MalzemelerVM, Malzeme>, IPaggingService<MalzemelerVM>
	{
		Task<List<MalzemelerVM>> MalzemeAra(string malzemeAd);
		Task<List<MalzemelerVM>> Malzemeler();
		Task<bool> LogoMalzemeAktar();
		Task<List<MalzemeFiyat>> FiyatGetir(int malzemeID);
		Task<bool> FiyatDegistir(MalzemeFiyat model);
	}
}
