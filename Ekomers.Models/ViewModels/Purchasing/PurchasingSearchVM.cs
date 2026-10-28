using System.ComponentModel.DataAnnotations;
using Ekomers.Models.Enums;

namespace Ekomers.Models.ViewModels.Purchasing;

public enum PurchasingSearchStage { Request=1, PriceRequest=2, Quotation=3, Order=4, Receipt=5, RequestApproval=6, QuotationApproval=7, WarehouseEntry=8, QualityDecision=9 }

public sealed class PurchasingSearchVM
{
    [StringLength(100)] public string? Search { get; set; }
    [StringLength(100)] public string? Product { get; set; }
    [StringLength(100)] public string? Supplier { get; set; }
    public int? MaterialId { get; set; }
    public int? SupplierId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public PurchasingSearchStage? Stage { get; set; }
    public bool IncludeCancelled { get; set; }
    public bool Run { get; set; }
    public int Page { get; set; } = 1;
    public int SummaryPage { get; set; } = 1;
    public int EventCount { get; set; }
    public int SummaryCount { get; set; }
    public int RequestCount { get; set; }
    public int OrderCount { get; set; }
    public int ReceiptCount { get; set; }
    public List<PurchasingSearchEventVM> Events { get; set; } = [];
    public List<PurchasingSearchSummaryVM> Summaries { get; set; } = [];
    public int PageCount => (int)Math.Ceiling(EventCount / 50d);
    public int SummaryPageCount => (int)Math.Ceiling(SummaryCount / 25d);
}

public sealed class PurchasingSearchEventVM
{
    public PurchasingSearchStage Stage { get; set; }
    public int DocumentId { get; set; }
    public int LineId { get; set; }
    public DateTime Date { get; set; }
    public string DocumentNumber { get; set; } = "";
    public int Status { get; set; }
    public bool Cancelled { get; set; }
    public int MaterialId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int UnitId { get; set; }
    public string Unit { get; set; } = "";
    public int? SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public int? RequestId { get; set; }
    public string? RequestNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? Notes { get; set; }
    public string? Actor { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? DispatchNumber { get; set; }
    public string? InvoiceNumber { get; set; }
    public string StageName => Stage switch {
        PurchasingSearchStage.Request=>"Talep", PurchasingSearchStage.PriceRequest=>"Fiyat isteği", PurchasingSearchStage.Quotation=>"Teklif",
        PurchasingSearchStage.Order=>"Sipariş", PurchasingSearchStage.Receipt=>"Mal kabul", PurchasingSearchStage.RequestApproval=>"Talep onayı",
        PurchasingSearchStage.QuotationApproval=>"Teklif onayı", PurchasingSearchStage.WarehouseEntry=>"Depo girişi", PurchasingSearchStage.QualityDecision=>"Kalite kararı", _=>"İşlem" };
    public string StatusName => Stage switch {
        PurchasingSearchStage.Request or PurchasingSearchStage.RequestApproval=>((PurPurchaseRequestLineStatus)Status).ToTurkish(),
        PurchasingSearchStage.Quotation or PurchasingSearchStage.QuotationApproval=>((PurSupplierQuotationLineStatus)Status).ToTurkish(),
        PurchasingSearchStage.Order=>((PurPurchaseOrderLineStatus)Status).ToTurkish(),
        PurchasingSearchStage.Receipt or PurchasingSearchStage.WarehouseEntry=>((PurGoodsReceiptStatus)Status).ToTurkish(),
        PurchasingSearchStage.PriceRequest=>((PurPriceRequestEmailStatus)Status) switch {PurPriceRequestEmailStatus.Sent=>"Gönderildi",PurPriceRequestEmailStatus.Failed=>"Gönderilemedi",_=>"Taslak"},
        PurchasingSearchStage.QualityDecision=>((PrdQualityControlStatus)Status).ToTurkish(), _=>"" };
    public string DetailController => Stage==PurchasingSearchStage.QualityDecision?"KaliteYonetimi":"SatinalmaYonetimi";
    public string DetailAction => Stage switch {
        PurchasingSearchStage.Request or PurchasingSearchStage.RequestApproval=>"TalepDetay", PurchasingSearchStage.PriceRequest=>"FiyatIstegiDetay",
        PurchasingSearchStage.Quotation or PurchasingSearchStage.QuotationApproval=>"TeklifDetay", PurchasingSearchStage.Order=>"SiparisDetay",
        PurchasingSearchStage.QualityDecision=>"AnalizDetay", _=>"MalKabulDetay" };
}

public sealed class PurchasingSearchSummaryVM
{
    public int MaterialId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public int SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public int UnitId { get; set; }
    public string Unit { get; set; } = "";
    public decimal OrderedQuantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public DateTime? FirstReceiptDate { get; set; }
    public DateTime? LastReceiptDate { get; set; }
    public bool HasRequest { get; set; }
    public int QuotationCount { get; set; }
}
