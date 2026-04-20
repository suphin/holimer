using System;
using System.Collections.Generic;
using System.Text;
using Ekomers.Models.Entity;

namespace Ekomers.Data.Repository
{
	public class PurchaseRequestRepository : IPurchaseRequestRepository
	{
		private readonly ApplicationDbContext _context;

		public PurchaseRequestRepository(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task AddAsync(PurchaseRequest request)
		{
			await _context.PurchaseRequests.AddAsync(request);
			await _context.SaveChangesAsync();
		}
	}
}
