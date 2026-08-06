using Ekomers.Models;
using Ekomers.Models.ViewModels; 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IReportService
	{
		Task<ReportVM> RunAsync(ReportRequest request, CancellationToken ct);
		Task<BankalarVM> Bankalar();
		Task<BankaHesapVM> BankaHesaplari(int bankRef);
		Task<List<BankaHesapEkstreVM>> BankaHesapEkstresi(int cardRef);
		Task<List<BorcAlacakVM>> BorcAlacakRaporu();
		Task<List<BankaKrediVM>> BankaKredileri();
		Task<List<KrediOdemePlaniVM>> KrediOdemePlani();
	}
}
