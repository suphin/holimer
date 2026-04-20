using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services
{
	public interface IPurchaseRequestService
	{
		Task CreateAsync(CreatePurchaseRequestVM model, bool sendForApproval);
	}
}
