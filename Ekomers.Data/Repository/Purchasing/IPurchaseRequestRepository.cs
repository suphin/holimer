using System;
using System.Collections.Generic;
using System.Text;
using Ekomers.Models.Entity;

namespace Ekomers.Data.Repository
{
	public interface IPurchaseRequestRepository
	{
		Task AddAsync(PurchaseRequest request);
	}
}
