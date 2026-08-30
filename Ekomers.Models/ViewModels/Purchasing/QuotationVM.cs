using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class SupplierListVM
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoCode { get; set; }
    public bool IsActive { get; set; }
    public int QuotationCount { get; set; }
}

public sealed class SupplierFormVM
{
    public int Id { get; set; }
    [Required, StringLength(50), Display(Name = "Tedarikçi Kodu")] public string Code { get; set; } = string.Empty;
    [Required, StringLength(250), Display(Name = "Tedarikçi Adı")] public string Name { get; set; } = string.Empty;
    [StringLength(20), Display(Name = "Vergi / T.C. No")] public string? TaxNumber { get; set; }
    [StringLength(150), Display(Name = "Vergi Dairesi")] public string? TaxOffice { get; set; }
    [StringLength(150), Display(Name = "Yetkili Kişi")] public string? ContactName { get; set; }
    [EmailAddress, StringLength(250), Display(Name = "E-posta")] public string? Email { get; set; }
    [StringLength(50), Display(Name = "Telefon")] public string? Phone { get; set; }
    [Display(Name = "Adres")] public string? Address { get; set; }
    [StringLength(100), Display(Name = "Logo Cari Kodu")] public string? LogoCode { get; set; }
    [Display(Name = "Not")] public string? Notes { get; set; }
    [Display(Name = "Aktif")] public bool IsActive { get; set; } = true;
}

public sealed class QuotationRequestCandidateVM
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string RequestedUserId { get; set; } = string.Empty;
    public int EligibleLineCount { get; set; }
    public int QuotationCount { get; set; }
}

public sealed class SupplierQuotationListVM
{
    public int Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierQuotationNumber { get; set; }
    public DateTime QuotationDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal GrandTotalTry => GrandTotal * ExchangeRate;
    public PurSupplierQuotationStatus Status { get; set; }
    public int LineCount { get; set; }
    public int PendingLineCount { get; set; }
}

public sealed class SupplierQuotationFormVM
{
    public int Id { get; set; }
    public int PurchaseRequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public string QuotationNumber { get; set; } = string.Empty;
    [Display(Name = "Tedarikçi")] public int SupplierId { get; set; }
    [StringLength(100), Display(Name = "Firma Teklif Numarası")] public string? SupplierQuotationNumber { get; set; }
    [DataType(DataType.Date), Display(Name = "Teklif Tarihi")] public DateTime QuotationDate { get; set; } = DateTime.Today;
    [DataType(DataType.Date), Display(Name = "Geçerlilik Tarihi")] public DateTime? ValidUntil { get; set; }
    [Display(Name = "Para Birimi")] public string CurrencyCode { get; set; } = "TRY";
    [Display(Name = "Döviz Kuru")] public string ExchangeRateInput { get; set; } = "1";
    [DataType(DataType.Date), Display(Name = "Kur Tarihi")] public DateTime? ExchangeRateDate { get; set; } = DateTime.Today;
    public string ExchangeRateSource { get; set; } = "Sabit";
    [StringLength(500), Display(Name = "Ödeme Koşulu")] public string? PaymentTerms { get; set; }
    [StringLength(500), Display(Name = "Teslim Koşulu")] public string? DeliveryTerms { get; set; }
    [Display(Name = "Termin Süresi (Gün)")] public int? LeadTimeDays { get; set; }
    [StringLength(1000), Display(Name = "Teklif Notu")] public string? Notes { get; set; }
    public List<SupplierQuotationFormLineVM> Lines { get; set; } = [];
    public List<SelectListItem> Suppliers { get; set; } = [];
    public List<SelectListItem> Currencies { get; set; } = [];
    public Dictionary<string, decimal> CurrentRates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime CurrentRateDate { get; set; } = DateTime.Today;
    public string? RateLoadWarning { get; set; }
}

public sealed class SupplierQuotationFormLineVM
{
    public int Id { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public bool Include { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal ApprovedRequestQuantity { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public string? OfferedQuantityInput { get; set; }
    public string? UnitPriceInput { get; set; }
    public string? DiscountRateInput { get; set; } = "0";
    public string? VatRateInput { get; set; } = "20";
    public DateTime? DeliveryDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class SupplierQuotationDetailVM
{
    public int Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? SupplierQuotationNumber { get; set; }
    public DateTime QuotationDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime? ExchangeRateDate { get; set; }
    public string ExchangeRateSource { get; set; } = string.Empty;
    public decimal NetTotalTry => NetTotal * ExchangeRate;
    public decimal TaxTotalTry => TaxTotal * ExchangeRate;
    public decimal GrandTotalTry => GrandTotal * ExchangeRate;
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public int? LeadTimeDays { get; set; }
    public PurSupplierQuotationStatus Status { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? Notes { get; set; }
    public bool CanEdit { get; set; }
    public bool CanApprove { get; set; }
    public int? PurchaseOrderId { get; set; }
    public List<SupplierQuotationDetailLineVM> Lines { get; set; } = [];
}

public sealed class SupplierQuotationDetailLineVM
{
    public int Id { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal RequestApprovedQuantity { get; set; }
    public decimal AlreadyOrderedQuantity { get; set; }
    public decimal RemainingRequestQuantity { get; set; }
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
    public PurSupplierQuotationLineStatus Status { get; set; }
    public string? ApprovalNote { get; set; }
    public string? Notes { get; set; }
}

public sealed class QuotationLineDecisionVM
{
    public int QuotationId { get; set; }
    public int LineId { get; set; }
    public int RequestId { get; set; }
    public bool ReturnToComparison { get; set; }
    public PurQuotationApprovalAction Decision { get; set; }
    public string ApprovedQuantityInput { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class QuotationComparisonVM
{
    public int RequestId { get; set; }
    public string RequestNumber { get; set; } = string.Empty;
    public bool CanApprove { get; set; }
    public List<QuotationComparisonLineVM> Lines { get; set; } = [];
}

public sealed class QuotationComparisonLineVM
{
    public int QuotationId { get; set; }
    public int QuotationLineId { get; set; }
    public int PurchaseRequestLineId { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public string? SupplierQuotationNumber { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal RequestApprovedQuantity { get; set; }
    public decimal AlreadyOrderedQuantity { get; set; }
    public decimal RemainingRequestQuantity { get; set; }
    public decimal OfferedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal GrandTotal { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime? ExchangeRateDate { get; set; }
    public string ExchangeRateSource { get; set; } = string.Empty;
    public decimal NetUnitPriceTry => NetUnitPrice * ExchangeRate;
    public decimal GrandTotalTry => GrandTotal * ExchangeRate;
    public DateTime QuotationDate { get; set; }
    public DateTime? ValidUntil { get; set; }
    public DateTime? DeliveryDate { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public PurSupplierQuotationLineStatus Status { get; set; }
}

public sealed class PurchaseOrderListVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string QuotationNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal GrandTotalTry => GrandTotal * ExchangeRate;
    public PurPurchaseOrderStatus Status { get; set; }
    public int LineCount { get; set; }
}

public sealed class PurchaseOrderDetailVM
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public int QuotationId { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public DateTime? ExchangeRateDate { get; set; }
    public string ExchangeRateSource { get; set; } = string.Empty;
    public decimal NetTotalTry => NetTotal * ExchangeRate;
    public decimal TaxTotalTry => TaxTotal * ExchangeRate;
    public decimal GrandTotalTry => GrandTotal * ExchangeRate;
    public PurPurchaseOrderStatus Status { get; set; }
    public decimal NetTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }
    public string? PaymentTerms { get; set; }
    public string? DeliveryTerms { get; set; }
    public List<PurchaseOrderDetailLineVM> Lines { get; set; } = [];
}

public sealed class PurchaseOrderDetailLineVM
{
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountRate { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal VatRate { get; set; }
    public decimal GrandTotal { get; set; }
    public DateTime? RequestedDeliveryDate { get; set; }
    public PurPurchaseOrderLineStatus Status { get; set; }
}
