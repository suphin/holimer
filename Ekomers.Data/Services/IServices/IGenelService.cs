using Ekomers.Models;
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
	public interface IGenelService<TVM, T>
	where TVM : BaseVM
	where T : BaseEntity
	{
		IQueryable<TVM> GenelListe();
		Task<TVM> VeriDoldurGenel(params string[] listTypes);
		 
		Task<List<TVM>> VeriListele(TVM model);
		 
		Task<List<TVM>> VeriListele();

        bool VeriEkle(TVM model);
        Task<bool> VeriEkleAsync(TVM model) => (Task<bool>)Task.CompletedTask;
		Task<int> VeriEkleReturnIDAsync(TVM model) => (Task<int>)Task.CompletedTask;

		Task<IstatistikVM> VeriSayisi() => (Task<IstatistikVM>)Task.CompletedTask;
		Task<TVM> VeriGetir(int id);
		 
		Task<bool> VeriSil(int id); 
	}
}
