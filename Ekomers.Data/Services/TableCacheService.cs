
using Ekomers.Data;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

public class TableCacheService : ITableCacheService
{
	private readonly ApplicationDbContext _context;
	private readonly IMemoryCache _cache;

	public TableCacheService(ApplicationDbContext context, IMemoryCache cache)
	{
		_context = context;
		_cache = cache;
	}

	public async Task<List<Mahalle>> GetMahalleListeAsync()
	{
		return await _cache.GetOrCreateAsync("MahalleListe", async entry =>
		{
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(120);
			return await _context.Mahalle.OrderBy(p => p.Ad).ToListAsync();
		}) ?? new List<Mahalle>();
	}

	

	

	
}
