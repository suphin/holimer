using Ekomers.Models.Ekomers;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface ICacheService<T> where T : class
	{
		Task<List<T>> GetAllAsync(
	   string cacheKey,
	   Func<CancellationToken, Task<List<T>>> fetchFunction,
	   TimeSpan? absoluteExpirationRelativeToNow = null,
	   TimeSpan? slidingExpiration = null,
	   CacheItemPriority priority = CacheItemPriority.Normal,
	   CancellationToken cancellationToken = default);

		void Remove(string cacheKey);


		Task<List<T>> GetListeAsync(string cacheKey);
		Task<List<T>> GetListeAsync(string cacheKey, Expression<Func<T, bool>> filter);
		Task<List<T>> GetListeAsync<TKey>(string cacheKey,
														Expression<Func<T, bool>> filter,
														Expression<Func<T, TKey>> orderBy,
														bool orderByDesc = false
													);
	}
}
