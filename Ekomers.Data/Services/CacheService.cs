using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Linq.Expressions;

namespace Ekomers.Data.Services
{
	public class CacheService<T> : ICacheService<T> where T : class
	{
		 
		private readonly IMemoryCache _cache;
		private readonly ApplicationDbContext _context;
		public CacheService( IMemoryCache cache, ApplicationDbContext context)
		{
			 
			_cache = cache;
			_context = context;
		}
		public async Task<List<T>> GetAllAsync(
		string cacheKey,
		Func<CancellationToken, Task<List<T>>> fetchFunction,
		TimeSpan? absoluteExpirationRelativeToNow = null,
		TimeSpan? slidingExpiration = null,
		CacheItemPriority priority = CacheItemPriority.Normal,
		CancellationToken cancellationToken = default)
		{
			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				if (absoluteExpirationRelativeToNow.HasValue)
					entry.AbsoluteExpirationRelativeToNow = absoluteExpirationRelativeToNow;

				if (slidingExpiration.HasValue)
					entry.SlidingExpiration = slidingExpiration;

				entry.Priority = priority;

				// Asıl veriyi repo/EF’den çek
				var list = await fetchFunction(cancellationToken);
				return list ?? new List<T>();
			});
		}

		public async Task<List<T>> GetListeAsync(string cacheKey)
		{
			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
				return await _context.Set<T>().ToListAsync();
			}) ?? new List<T>();
		}
		public async Task<List<T>> GetListeAsync(string cacheKey, Expression<Func<T, bool>> filter)
		{
			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);
				return await _context.Set<T>().Where(filter).ToListAsync();
			}) ?? new List<T>();
		}
		public async Task<List<T>> GetListeAsync<TKey>(
														string cacheKey,
														Expression<Func<T, bool>> filter,
														Expression<Func<T, TKey>> orderBy,
														bool orderByDesc = false
													) 
		{
			return await _cache.GetOrCreateAsync(cacheKey, async entry =>
			{
				entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(12);

				IQueryable<T> query = _context.Set<T>().Where(filter);

				query = orderByDesc
					? query.OrderByDescending(orderBy)
					: query.OrderBy(orderBy);

				return await query.ToListAsync();
			}) ?? new List<T>();
		}
		public void Remove(string cacheKey) => _cache.Remove(cacheKey);
	}
}
