using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;

namespace Ekomers.Models.Entity.Purchasing;

public class PurPurchaseRequest : BaseEntity
{
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public DateTime? NeededDate { get; set; }
    public string RequestedUserId { get; set; } = string.Empty;
    public PurPurchaseRequestPriority Priority { get; set; } = PurPurchaseRequestPriority.Normal;
    public PurPurchaseRequestStatus Status { get; set; } = PurPurchaseRequestStatus.Draft;
    public DateTime? SubmittedDate { get; set; }
    public string? SubmittedUserId { get; set; }
    public string? Notes { get; set; }
}

public class PurPurchaseRequestLine : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public DateTime? NeededDate { get; set; }
    public PurPurchaseRequestLineStatus Status { get; set; } = PurPurchaseRequestLineStatus.Draft;
    public PurPurchaseRequestSource Source { get; set; } = PurPurchaseRequestSource.Manual;
    public string? SourceReferenceType { get; set; }
    public int? SourceReferenceId { get; set; }
    public string? Reason { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
    public string? ApprovalNote { get; set; }
}

public class PurRequestApprovalHistory : BaseEntity
{
    public int PurchaseRequestId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public PurRequestApprovalAction Action { get; set; }
    public PurPurchaseRequestLineStatus PreviousStatus { get; set; }
    public PurPurchaseRequestLineStatus NewStatus { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal PreviousApprovedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public string? Note { get; set; }
    public DateTime ActionDate { get; set; }
    public string ActionUserId { get; set; } = string.Empty;
}

public class PurSupplier : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? TaxOffice { get; set; }
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? LogoCode { get; set; }
    public string? Notes { get; set; }
}

public class PurSupplierQuotation : BaseEntity
{
    public string QuotationNumber { get; set; } = string.Empty;
    public int PurchaseRequestId { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierQuotationNumber { get; set; }
    public DateTime QuotationDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime? ExchangeRateDate { get; set; }
    public string ExchangeRateSource { get; set; } = "Sabit";
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public int? LeadTimeDays { get; set; }
    public PurSupplierQuotationStatus Status { get; set; } = PurSupplierQuotationStatus.Draft;
    public DateTime? SubmittedDate { get; set; }
    public string? SubmittedUserId { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }
}

public class PurSupplierQuotationLine : BaseEntity
{
    public int SupplierQuotationId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal OfferedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public PurSupplierQuotationLineStatus Status { get; set; } = PurSupplierQuotationLineStatus.Draft;
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
    public string? ApprovalNote { get; set; }
    public string? Notes { get; set; }
}

public class PurQuotationApprovalHistory : BaseEntity
{
    public int SupplierQuotationId { get; set; }
    public int SupplierQuotationLineId { get; set; }
    public PurQuotationApprovalAction Action { get; set; }
    public PurSupplierQuotationLineStatus PreviousStatus { get; set; }
    public PurSupplierQuotationLineStatus NewStatus { get; set; }
    public decimal OfferedQuantity { get; set; }
    public decimal PreviousApprovedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal NetUnitPrice { get; set; }
    public string? Note { get; set; }
    public DateTime ActionDate { get; set; }
    public string ActionUserId { get; set; } = string.Empty;
}

public class PurPurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int SourceQuotationId { get; set; }
    public DateTime OrderDate { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1m;
    public DateTime? ExchangeRateDate { get; set; }
    public string ExchangeRateSource { get; set; } = "Sabit";
    public PurPurchaseOrderStatus Status { get; set; } = PurPurchaseOrderStatus.Open;
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public PurTransportationType? TransportationType { get; set; }
    public PurFreightPaymentType? FreightPaymentType { get; set; }
    public int? DeliveryWarehouseId { get; set; }
    public string? DeliveryAddress { get; set; }
    public string? CarrierName { get; set; }
    public decimal? EstimatedFreightAmount { get; set; }
    public decimal? EstimatedFreightVatRate { get; set; }
    public string FreightCurrencyCode { get; set; } = "TRY";
    public decimal FreightExchangeRate { get; set; } = 1m;
    public DateTime? FreightExchangeRateDate { get; set; }
    public string FreightExchangeRateSource { get; set; } = "Sabit";
    public DateTime? PlannedShipmentDate { get; set; }
    public DateTime? PlannedDeliveryDate { get; set; }
    public string? TrackingNumber { get; set; }
    public string? TransportationNotes { get; set; }
    public string? Notes { get; set; }
}

public class PurPurchaseOrderLine : BaseEntity
{
    public int PurchaseOrderId { get; set; }
    public int SupplierQuotationLineId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public PurPurchaseOrderLineStatus Status { get; set; } = PurPurchaseOrderLineStatus.Open;
    public string? Notes { get; set; }
}

public class PurGoodsReceipt : BaseEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public DateTime? DispatchDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public PurGoodsReceiptStatus Status { get; set; } = PurGoodsReceiptStatus.Recorded;
    public string? CarrierName { get; set; }
    public string? VehiclePlate { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? ActualFreightAmount { get; set; }
    public decimal? ActualFreightVatRate { get; set; }
    public string FreightCurrencyCode { get; set; } = "TRY";
    public decimal FreightExchangeRate { get; set; } = 1m;
    public DateTime? FreightExchangeRateDate { get; set; }
    public string FreightExchangeRateSource { get; set; } = "Sabit";
    public int? QuarantineWarehouseId { get; set; }
    public int? QuarantineInventoryDocumentId { get; set; }
    public DateTime? QuarantineDate { get; set; }
    public string? QuarantineUserId { get; set; }
    public string? Notes { get; set; }
}

public class PurGoodsReceiptLine : BaseEntity
{
    public int GoodsReceiptId { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? QuarantineStockLotId { get; set; }
    public int? QuarantineInventoryDocumentLineId { get; set; }
    public string? Notes { get; set; }
}

public class PurQualityInspection : BaseEntity
{
    public string InspectionNumber { get; set; } = string.Empty;
    public int GoodsReceiptId { get; set; }
    public int GoodsReceiptLineId { get; set; }
    public int MaterialId { get; set; }
    public int StockLotId { get; set; }
    public int WarehouseId { get; set; }
    public int? SpecificationSetId { get; set; }
    public PrdQualityControlStatus Status { get; set; } = PrdQualityControlStatus.Pending;
    public string? SampleNumber { get; set; }
    public DateTime? SampleDate { get; set; }
    public string? SampledUserId { get; set; }
    public DateTime? AnalysisDate { get; set; }
    public DateTime? ResultDate { get; set; }
    public string? LaboratoryName { get; set; }
    public string? CertificateNumber { get; set; }
    public string? ResultSummary { get; set; }
    public string? SpecificationNotes { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? DecisionUserId { get; set; }
    public string? DecisionNote { get; set; }
}

public class PurQualityInspectionSpecificationResult : BaseEntity
{
    public int QualityInspectionId { get; set; }
    public int SpecificationSetId { get; set; }
    public int SpecificationItemId { get; set; }
    public decimal? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public PrdSpecificationResultStatus Status { get; set; } = PrdSpecificationResultStatus.Pending;
    public string? EvaluationNote { get; set; }
    public DateTime? AnalysisDate { get; set; }
    public string? AnalyzedUserId { get; set; }
}
