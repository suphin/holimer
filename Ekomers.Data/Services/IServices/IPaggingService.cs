using Ekomers.Models;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.FilterVM;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IPaggingService<T> where T : BaseVM
	{
		Task<PagedResult<T>> VeriListeleAsync(int page, int pageSize, CancellationToken ct = default);
		Task<PagedResult<T>> VeriListeleAsync(int page, int pageSize, MalzemelerFilterVM f, CancellationToken ct = default)=>(Task<PagedResult<T>>)Task.CompletedTask;
		Task<PagedResult<T>> VeriListeleAsync(T model);

	}
}
