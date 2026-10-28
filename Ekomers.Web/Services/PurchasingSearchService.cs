using Ekomers.Data;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Services;

// All filtering, grouping and pagination runs in SQL; separate event stages never multiply deliveries.
public sealed class PurchasingSearchService(ApplicationDbContext db)
{
    private readonly ApplicationDbContext _db=db;
    public async Task SearchAsync(PurchasingSearchVM model,CancellationToken ct=default)
    {
        var facts=Facts();
        if(model.MaterialId.HasValue)facts=facts.Where(x=>x.MaterialId==model.MaterialId.Value);
        if(model.SupplierId.HasValue){var ids=_db.PurSuppliers.Where(x=>x.ID==model.SupplierId.Value).Select(x=>x.ID);var linked=SupplierRequestLines(ids);facts=facts.Where(x=>x.SupplierId==model.SupplierId.Value||!x.SupplierId.HasValue&&x.RequestLineId.HasValue&&linked.Contains(x.RequestLineId.Value));}
        var productMatches=_db.PrdMaterials.AsNoTracking();
        var supplierMatches=_db.PurSuppliers.AsNoTracking();
        var q=model.Search?.Trim();var product=model.Product?.Trim();var supplier=model.Supplier?.Trim();
        if(!string.IsNullOrEmpty(product))
        {
            var ids=productMatches.Where(x=>x.Code.Contains(product)||x.Name.Contains(product)||x.LogoCode!=null&&x.LogoCode.Contains(product)).Select(x=>x.ID);
            facts=facts.Where(x=>ids.Contains(x.MaterialId));
        }
        if(!string.IsNullOrEmpty(supplier))
        {
            var ids=supplierMatches.Where(x=>x.Code.Contains(supplier)||x.Name.Contains(supplier)||x.LogoCode!=null&&x.LogoCode.Contains(supplier)).Select(x=>x.ID);
            var linked=SupplierRequestLines(ids);
            facts=facts.Where(x=>x.SupplierId.HasValue&&ids.Contains(x.SupplierId.Value)||!x.SupplierId.HasValue&&x.RequestLineId.HasValue&&linked.Contains(x.RequestLineId.Value));
        }
        if(!string.IsNullOrEmpty(q))
        {
            var materials=productMatches.Where(x=>x.Code.Contains(q)||x.Name.Contains(q)||x.LogoCode!=null&&x.LogoCode.Contains(q)).Select(x=>x.ID);
            var suppliers=supplierMatches.Where(x=>x.Code.Contains(q)||x.Name.Contains(q)||x.LogoCode!=null&&x.LogoCode.Contains(q)).Select(x=>x.ID);
            var linked=SupplierRequestLines(suppliers);
            facts=facts.Where(x=>materials.Contains(x.MaterialId)||x.SupplierId.HasValue&&suppliers.Contains(x.SupplierId.Value)||!x.SupplierId.HasValue&&x.RequestLineId.HasValue&&linked.Contains(x.RequestLineId.Value)||x.Number.Contains(q)||x.LotNumber!=null&&x.LotNumber.Contains(q));
        }
        if(model.StartDate.HasValue){var start=model.StartDate.Value.Date;facts=facts.Where(x=>x.Date>=start);}
        if(model.EndDate.HasValue){var end=model.EndDate.Value.Date.AddDays(1);facts=facts.Where(x=>x.Date<end);}
        var active=facts.Where(x=>!x.Cancelled);
        model.RequestCount=await active.Where(x=>x.Stage==PurchasingSearchStage.Request).Select(x=>x.DocumentId).Distinct().CountAsync(ct);
        model.OrderCount=await active.Where(x=>x.Stage==PurchasingSearchStage.Order).Select(x=>x.DocumentId).Distinct().CountAsync(ct);
        model.ReceiptCount=await active.Where(x=>x.Stage==PurchasingSearchStage.Receipt).Select(x=>x.DocumentId).Distinct().CountAsync(ct);

        var rows=Decorate(facts);
        var summary=Decorate(active.Where(x=>x.SupplierId.HasValue&&(x.Stage==PurchasingSearchStage.PriceRequest||x.Stage==PurchasingSearchStage.Quotation||x.Stage==PurchasingSearchStage.Order||x.Stage==PurchasingSearchStage.Receipt)))
            .GroupBy(x=>new{x.SupplierId,x.SupplierCode,x.SupplierName,x.MaterialId,x.ProductCode,x.ProductName,x.UnitId,x.Unit})
            .Select(g=>new PurchasingSearchSummaryVM {
                SupplierId=g.Key.SupplierId!.Value,SupplierCode=g.Key.SupplierCode,SupplierName=g.Key.SupplierName,MaterialId=g.Key.MaterialId,ProductCode=g.Key.ProductCode,ProductName=g.Key.ProductName,UnitId=g.Key.UnitId,Unit=g.Key.Unit,
                OrderedQuantity=g.Sum(x=>x.Stage==PurchasingSearchStage.Order?x.Quantity:0),
                ReceivedQuantity=g.Sum(x=>x.Stage==PurchasingSearchStage.Receipt?x.Quantity:0),
                FirstReceiptDate=g.Min(x=>x.Stage==PurchasingSearchStage.Receipt?(DateTime?)x.Date:null),
                LastReceiptDate=g.Max(x=>x.Stage==PurchasingSearchStage.Receipt?(DateTime?)x.Date:null),
                HasRequest=g.Any(x=>x.RequestId.HasValue),
                QuotationCount=g.Where(x=>x.Stage==PurchasingSearchStage.Quotation).Select(x=>x.DocumentId).Distinct().Count()
            });
        model.SummaryCount=await summary.CountAsync(ct);
        model.SummaryPage=Math.Clamp(model.SummaryPage,1,Math.Max(1,model.SummaryPageCount));
        model.Summaries=await summary.OrderBy(x=>x.SupplierName).ThenBy(x=>x.SupplierId).ThenBy(x=>x.ProductCode).ThenBy(x=>x.MaterialId).ThenBy(x=>x.UnitId).Skip((model.SummaryPage-1)*25).Take(25).ToListAsync(ct);
        if(!model.IncludeCancelled)rows=rows.Where(x=>!x.Cancelled);
        if(model.Stage.HasValue)rows=rows.Where(x=>x.Stage==model.Stage.Value);
        model.EventCount=await rows.CountAsync(ct);
        model.Page=Math.Clamp(model.Page,1,Math.Max(1,model.PageCount));
        model.Events=await rows.OrderByDescending(x=>x.Date).ThenByDescending(x=>x.Stage).ThenByDescending(x=>x.DocumentId).ThenByDescending(x=>x.LineId).Skip((model.Page-1)*50).Take(50).ToListAsync(ct);
    }

    private IQueryable<int> SupplierRequestLines(IQueryable<int> suppliers)
    {
        var quotes=from l in _db.PurSupplierQuotationLines.AsNoTracking() join h in _db.PurSupplierQuotations.AsNoTracking() on l.SupplierQuotationId equals h.ID where l.IsDelete!=true&&h.IsDelete!=true&&suppliers.Contains(h.SupplierId) select l.PurchaseRequestLineId;
        var orders=from l in _db.PurPurchaseOrderLines.AsNoTracking() join h in _db.PurPurchaseOrders.AsNoTracking() on l.PurchaseOrderId equals h.ID where l.IsDelete!=true&&h.IsDelete!=true&&suppliers.Contains(h.SupplierId) select l.PurchaseRequestLineId;
        var emails=from l in _db.PurPriceRequestEmailLines.AsNoTracking() join h in _db.PurPriceRequestEmails.AsNoTracking() on l.PriceRequestEmailId equals h.ID where l.IsDelete!=true&&h.IsDelete!=true&&suppliers.Contains(h.SupplierId) select l.PurchaseRequestLineId;
        return quotes.Concat(orders).Concat(emails);
    }

    private IQueryable<PurchasingSearchEventVM> Decorate(IQueryable<Fact> facts) =>
        from x in facts
        join m in _db.PrdMaterials.AsNoTracking() on x.MaterialId equals m.ID
        join u in _db.PrdUnits.AsNoTracking() on x.UnitId equals u.ID
        join supplier in _db.PurSuppliers.AsNoTracking() on x.SupplierId equals (int?)supplier.ID into suppliers
        from s in suppliers.DefaultIfEmpty()
        join line in _db.PurPurchaseRequestLines.AsNoTracking().Where(l=>l.IsDelete!=true) on x.RequestLineId equals (int?)line.ID into requestLines
        from l in requestLines.DefaultIfEmpty()
        join request in _db.PurPurchaseRequests.AsNoTracking().Where(r=>r.IsDelete!=true) on l.PurchaseRequestId equals request.ID into requests
        from r in requests.DefaultIfEmpty()
        select new PurchasingSearchEventVM {
            Stage=x.Stage,DocumentId=x.DocumentId,LineId=x.LineId,Date=x.Date,DocumentNumber=x.Number,Status=x.Status,Cancelled=x.Cancelled,
            MaterialId=m.ID,ProductCode=m.Code,ProductName=m.Name,UnitId=u.ID,Unit=u.Name,SupplierId=x.SupplierId,SupplierCode=s==null?"":s.Code,SupplierName=s==null?"":s.Name,
            RequestId=r==null?(int?)null:r.ID,RequestNumber=r==null?null:r.RequestNumber,Quantity=x.Quantity,Notes=x.Notes,Actor=x.Actor,
            LotNumber=x.LotNumber,ProductionDate=x.ProductionDate,ExpirationDate=x.ExpirationDate,DispatchNumber=x.DispatchNumber,InvoiceNumber=x.InvoiceNumber
        };

    private IQueryable<Fact> Facts()
    {
        var requests=from l in _db.PurPurchaseRequestLines.AsNoTracking() join d in _db.PurPurchaseRequests.AsNoTracking() on l.PurchaseRequestId equals d.ID where l.IsDelete!=true&&d.IsDelete!=true
            select new Fact {
                Stage=PurchasingSearchStage.Request,DocumentId=d.ID,LineId=l.ID,Date=d.RequestDate,Number=d.RequestNumber,Status=(int)l.Status,Cancelled=d.Status==PurPurchaseRequestStatus.Cancelled||l.Status==PurPurchaseRequestLineStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)null,RequestLineId=(int?)l.ID,Quantity=l.RequestedQuantity,Notes=l.Reason ?? d.Notes,Actor=d.RequestedUserId,LotNumber=(string?)null,ProductionDate=(DateTime?)null,ExpirationDate=(DateTime?)null,DispatchNumber=(string?)null,InvoiceNumber=(string?)null
            };
        var quotations=from l in _db.PurSupplierQuotationLines.AsNoTracking() join d in _db.PurSupplierQuotations.AsNoTracking() on l.SupplierQuotationId equals d.ID where l.IsDelete!=true&&d.IsDelete!=true
            select new Fact {
                Stage=PurchasingSearchStage.Quotation,DocumentId=d.ID,LineId=l.ID,Date=d.QuotationDate,Number=d.QuotationNumber,Status=(int)l.Status,Cancelled=d.Status==PurSupplierQuotationStatus.Cancelled||l.Status==PurSupplierQuotationLineStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)d.SupplierId,RequestLineId=(int?)l.PurchaseRequestLineId,Quantity=l.OfferedQuantity,Notes=l.Notes ?? d.Notes,Actor=d.CreateUserID,LotNumber=(string?)null,ProductionDate=(DateTime?)null,ExpirationDate=(DateTime?)null,DispatchNumber=(string?)null,InvoiceNumber=(string?)null
            };
        var orders=from l in _db.PurPurchaseOrderLines.AsNoTracking() join d in _db.PurPurchaseOrders.AsNoTracking() on l.PurchaseOrderId equals d.ID where l.IsDelete!=true&&d.IsDelete!=true
            select new Fact {
                Stage=PurchasingSearchStage.Order,DocumentId=d.ID,LineId=l.ID,Date=d.OrderDate,Number=d.OrderNumber,Status=(int)l.Status,Cancelled=d.Status==PurPurchaseOrderStatus.Cancelled||l.Status==PurPurchaseOrderLineStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)d.SupplierId,RequestLineId=(int?)l.PurchaseRequestLineId,Quantity=l.OrderedQuantity,Notes=l.Notes ?? d.Notes,Actor=d.CreateUserID,LotNumber=(string?)null,ProductionDate=(DateTime?)null,ExpirationDate=(DateTime?)null,DispatchNumber=(string?)null,InvoiceNumber=(string?)null
            };
        var receipts=from l in _db.PurGoodsReceiptLines.AsNoTracking() join d in _db.PurGoodsReceipts.AsNoTracking() on l.GoodsReceiptId equals d.ID join ol in _db.PurPurchaseOrderLines.AsNoTracking() on l.PurchaseOrderLineId equals ol.ID join o in _db.PurPurchaseOrders.AsNoTracking() on d.PurchaseOrderId equals o.ID where l.IsDelete!=true&&d.IsDelete!=true&&ol.IsDelete!=true&&o.IsDelete!=true&&ol.PurchaseOrderId==o.ID
            select new Fact {
                Stage=PurchasingSearchStage.Receipt,DocumentId=d.ID,LineId=l.ID,Date=d.ReceiptDate,Number=d.ReceiptNumber,Status=(int)d.Status,Cancelled=d.Status==PurGoodsReceiptStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)o.SupplierId,RequestLineId=(int?)ol.PurchaseRequestLineId,Quantity=l.ReceivedQuantity,Notes=l.Notes ?? d.Notes,Actor=d.CreateUserID,LotNumber=l.LotNumber,ProductionDate=l.ProductionDate,ExpirationDate=l.ExpirationDate,DispatchNumber=d.DispatchNumber,InvoiceNumber=d.InvoiceNumber
            };
        var emails=from el in _db.PurPriceRequestEmailLines.AsNoTracking() join d in _db.PurPriceRequestEmails.AsNoTracking() on el.PriceRequestEmailId equals d.ID join l in _db.PurPurchaseRequestLines.AsNoTracking() on el.PurchaseRequestLineId equals l.ID join r in _db.PurPurchaseRequests.AsNoTracking() on l.PurchaseRequestId equals r.ID where el.IsDelete!=true&&d.IsDelete!=true&&l.IsDelete!=true&&r.IsDelete!=true
            select new Fact {
                Stage=PurchasingSearchStage.PriceRequest,DocumentId=d.ID,LineId=el.ID,Date=d.SentDate ?? d.CreateDate ?? r.RequestDate,Number=d.ReferenceNumber,Status=(int)d.Status,Cancelled=false,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)d.SupplierId,RequestLineId=(int?)l.ID,Quantity=el.Quantity,Notes=d.Subject,Actor=d.CreateUserID,LotNumber=(string?)null,ProductionDate=(DateTime?)null,ExpirationDate=(DateTime?)null,DispatchNumber=(string?)null,InvoiceNumber=(string?)null
            };
        var requestApprovals=from l in _db.PurPurchaseRequestLines.AsNoTracking() join d in _db.PurPurchaseRequests.AsNoTracking() on l.PurchaseRequestId equals d.ID join h in _db.PurRequestApprovalHistories.AsNoTracking() on l.ID equals h.PurchaseRequestLineId where l.IsDelete!=true&&d.IsDelete!=true&&h.IsDelete!=true&&h.PurchaseRequestId==d.ID
            select new Fact {
                Stage=PurchasingSearchStage.RequestApproval,DocumentId=d.ID,LineId=h.ID,Date=h.ActionDate,Number=d.RequestNumber,Status=(int)h.NewStatus,Cancelled=d.Status==PurPurchaseRequestStatus.Cancelled||l.Status==PurPurchaseRequestLineStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)null,RequestLineId=(int?)l.ID,Quantity=h.ApprovedQuantity,Notes=h.Note,Actor=h.ActionUserId,LotNumber=(string?)null,ProductionDate=(DateTime?)null,ExpirationDate=(DateTime?)null,DispatchNumber=(string?)null,InvoiceNumber=(string?)null
            };
        var quotationApprovals=from l in _db.PurSupplierQuotationLines.AsNoTracking() join d in _db.PurSupplierQuotations.AsNoTracking() on l.SupplierQuotationId equals d.ID join h in _db.PurQuotationApprovalHistories.AsNoTracking() on l.ID equals h.SupplierQuotationLineId where l.IsDelete!=true&&d.IsDelete!=true&&h.IsDelete!=true&&h.SupplierQuotationId==d.ID
            select new Fact {
                Stage=PurchasingSearchStage.QuotationApproval,DocumentId=d.ID,LineId=h.ID,Date=h.ActionDate,Number=d.QuotationNumber,Status=(int)h.NewStatus,Cancelled=d.Status==PurSupplierQuotationStatus.Cancelled||l.Status==PurSupplierQuotationLineStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)d.SupplierId,RequestLineId=(int?)l.PurchaseRequestLineId,Quantity=h.ApprovedQuantity,Notes=h.Note,Actor=h.ActionUserId,LotNumber=(string?)null,ProductionDate=(DateTime?)null,ExpirationDate=(DateTime?)null,DispatchNumber=(string?)null,InvoiceNumber=(string?)null
            };
        var entries=from l in _db.PurGoodsReceiptLines.AsNoTracking() join d in _db.PurGoodsReceipts.AsNoTracking() on l.GoodsReceiptId equals d.ID join ol in _db.PurPurchaseOrderLines.AsNoTracking() on l.PurchaseOrderLineId equals ol.ID join o in _db.PurPurchaseOrders.AsNoTracking() on d.PurchaseOrderId equals o.ID where l.IsDelete!=true&&d.IsDelete!=true&&ol.IsDelete!=true&&o.IsDelete!=true&&ol.PurchaseOrderId==o.ID&&d.QuarantineDate.HasValue
            select new Fact {
                Stage=PurchasingSearchStage.WarehouseEntry,DocumentId=d.ID,LineId=l.ID,Date=d.QuarantineDate!.Value,Number=d.ReceiptNumber,Status=(int)d.Status,Cancelled=d.Status==PurGoodsReceiptStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)o.SupplierId,RequestLineId=(int?)ol.PurchaseRequestLineId,Quantity=l.ReceivedQuantity,Notes=l.Notes ?? d.Notes,Actor=d.QuarantineUserId,LotNumber=l.LotNumber,ProductionDate=l.ProductionDate,ExpirationDate=l.ExpirationDate,DispatchNumber=d.DispatchNumber,InvoiceNumber=d.InvoiceNumber
            };
        var quality=from l in _db.PurGoodsReceiptLines.AsNoTracking() join d in _db.PurGoodsReceipts.AsNoTracking() on l.GoodsReceiptId equals d.ID join ol in _db.PurPurchaseOrderLines.AsNoTracking() on l.PurchaseOrderLineId equals ol.ID join o in _db.PurPurchaseOrders.AsNoTracking() on d.PurchaseOrderId equals o.ID join h in _db.PurQualityInspections.AsNoTracking() on l.ID equals h.GoodsReceiptLineId where l.IsDelete!=true&&d.IsDelete!=true&&ol.IsDelete!=true&&o.IsDelete!=true&&ol.PurchaseOrderId==o.ID&&h.IsDelete!=true&&h.GoodsReceiptId==d.ID&&h.DecisionDate.HasValue
            select new Fact {
                Stage=PurchasingSearchStage.QualityDecision,DocumentId=h.ID,LineId=h.ID,Date=h.DecisionDate!.Value,Number=h.InspectionNumber,Status=(int)h.Status,Cancelled=d.Status==PurGoodsReceiptStatus.Cancelled,MaterialId=l.MaterialId,UnitId=l.UnitId,SupplierId=(int?)o.SupplierId,RequestLineId=(int?)ol.PurchaseRequestLineId,Quantity=l.ReceivedQuantity,Notes=h.DecisionNote ?? h.ResultSummary,Actor=h.DecisionUserId,LotNumber=l.LotNumber,ProductionDate=l.ProductionDate,ExpirationDate=l.ExpirationDate,DispatchNumber=d.DispatchNumber,InvoiceNumber=d.InvoiceNumber
            };
        return requests.Concat(quotations).Concat(orders).Concat(receipts).Concat(emails).Concat(requestApprovals).Concat(quotationApprovals).Concat(entries).Concat(quality);
    }
    private sealed class Fact
    {
        public PurchasingSearchStage Stage {get;set;}
        public int DocumentId {get;set;}
        public int LineId {get;set;}
        public DateTime Date {get;set;}
        public string Number {get;set;} = "";
        public int Status {get;set;}
        public bool Cancelled {get;set;}
        public int MaterialId {get;set;}
        public int UnitId {get;set;}
        public int? SupplierId {get;set;}
        public int? RequestLineId {get;set;}
        public decimal Quantity {get;set;}
        public string? Notes {get;set;}
        public string? Actor {get;set;}
        public string? LotNumber {get;set;}
        public DateTime? ProductionDate {get;set;}
        public DateTime? ExpirationDate {get;set;}
        public string? DispatchNumber {get;set;}
        public string? InvoiceNumber {get;set;}
    }
}

