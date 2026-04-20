using Ekomers.Data.Repository;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services
{
	public class PurchaseRequestService : IPurchaseRequestService
	{
		private readonly IPurchaseRequestRepository _repository;

		public PurchaseRequestService(IPurchaseRequestRepository repository)
		{
			_repository = repository;
		}

		public async Task CreateAsync(CreatePurchaseRequestVM model, bool sendForApproval)
		{
			if (string.IsNullOrWhiteSpace(model.Description))
				throw new Exception("Talep nedeni zorunlu");

			if (model.Items == null || !model.Items.Any())
				throw new Exception("En az 1 ürün olmalı");

			var request = new PurchaseRequest
			{
				RequestNo = "PR-" + DateTime.Now.Ticks,
				Description = model.Description,
				RequestDate = DateTime.Now,
				Status = sendForApproval ? RequestStatus.PendingApproval : RequestStatus.Draft,
				Items = new List<PurchaseRequestItem>()
			};

			foreach (var item in model.Items)
			{
				if (string.IsNullOrWhiteSpace(item.Unit))
					throw new Exception("Birim zorunlu");

				if (item.Quantity <= 0)
					throw new Exception("Miktar 0'dan büyük olmalı");

				if (item.ProductId == null && string.IsNullOrWhiteSpace(item.ProductName))
					throw new Exception("Ürün adı zorunlu");

				request.Items.Add(new PurchaseRequestItem
				{
					ProductId = item.ProductId,
					ProductName = item.ProductName,
					Quantity = item.Quantity,
					Unit = item.Unit
				});
			}

			await _repository.AddAsync(request);
		}
	}
}
