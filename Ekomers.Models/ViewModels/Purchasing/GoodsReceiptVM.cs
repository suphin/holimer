using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class GoodsReceiptListVM
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public int LineCount { get; set; }
    public PurGoodsReceiptStatus Status { get; set; }
    public decimal? ActualFreightAmount { get; set; }
    public string FreightCurrencyCode { get; set; } = "TRY";
}

public sealed class GoodsReceiptFormVM
{
    public int? Id { get; set; }
    public int PurchaseOrderId { get; set; }
    public string? ReceiptNumber { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public PurGoodsReceiptStatus Status { get; set; } = PurGoodsReceiptStatus.Recorded;

    [DataType(DataType.Date), Display(Name = "Mal Kabul Tarihi")]
    public DateTime ReceiptDate { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "İrsaliye numarası zorunludur."), StringLength(100), Display(Name = "İrsaliye Numarası")]
    public string DispatchNumber { get; set; } = string.Empty;

    [DataType(DataType.Date), Display(Name = "İrsaliye Tarihi")]
    public DateTime? DispatchDate { get; set; }

    [StringLength(100), Display(Name = "Fatura Numarası")]
    public string? InvoiceNumber { get; set; }

    [DataType(DataType.Date), Display(Name = "Fatura Tarihi")]
    public DateTime? InvoiceDate { get; set; }

    [StringLength(250), Display(Name = "Gerçekleşen Nakliye Firması")]
    public string? CarrierName { get; set; }

    [StringLength(30), Display(Name = "Araç Plakası")]
    public string? VehiclePlate { get; set; }

    [StringLength(100), Display(Name = "Takip / Sevk Numarası")]
    public string? TrackingNumber { get; set; }

    [Display(Name = "Gerçekleşen Nakliye Tutarı")]
    public string? ActualFreightAmountInput { get; set; }

    [Display(Name = "Nakliye KDV %")]
    public string? ActualFreightVatRateInput { get; set; } = "20";

    [Display(Name = "Nakliye Para Birimi")]
    public string FreightCurrencyCode { get; set; } = "TRY";

    [Display(Name = "Nakliye Döviz Kuru")]
    public string FreightExchangeRateInput { get; set; } = "1";

    [DataType(DataType.Date), Display(Name = "Nakliye Kur Tarihi")]
    public DateTime? FreightExchangeRateDate { get; set; } = DateTime.Today;

    public string FreightExchangeRateSource { get; set; } = "Sabit";

    [StringLength(1000), Display(Name = "Mal Kabul Notu")]
    public string? Notes { get; set; }

    public List<GoodsReceiptFormLineVM> Lines { get; set; } = [];
    public List<SelectListItem> Currencies { get; set; } = [];
    public Dictionary<string, decimal> CurrentRates { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTime CurrentRateDate { get; set; } = DateTime.Today;
    public string? RateLoadWarning { get; set; }
}

public sealed class GoodsReceiptFormLineVM
{
    public int? Id { get; set; }
    public bool Include { get; set; }
    public int PurchaseOrderLineId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal OrderedQuantity { get; set; }
    public decimal PreviouslyReceivedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public bool RequiresLotTracking { get; set; }
    public bool RequiresExpirationDate { get; set; }
    [Display(Name = "Teslim Alınan")] public string? ReceivedQuantityInput { get; set; }
    [StringLength(100), Display(Name = "Lot Numarası")] public string? LotNumber { get; set; }
    [DataType(DataType.Date), Display(Name = "Üretim Tarihi")] public DateTime? ProductionDate { get; set; }
    [DataType(DataType.Date), Display(Name = "Son Kullanma Tarihi")] public DateTime? ExpirationDate { get; set; }
    [StringLength(500), Display(Name = "Satır Notu")] public string? Notes { get; set; }
}

public sealed class GoodsReceiptDetailVM
{
    public int Id { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public int PurchaseOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string DispatchNumber { get; set; } = string.Empty;
    public DateTime? DispatchDate { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public PurGoodsReceiptStatus Status { get; set; }
    public string? CarrierName { get; set; }
    public string? VehiclePlate { get; set; }
    public string? TrackingNumber { get; set; }
    public decimal? ActualFreightAmount { get; set; }
    public decimal? ActualFreightVatRate { get; set; }
    public string FreightCurrencyCode { get; set; } = "TRY";
    public decimal FreightExchangeRate { get; set; }
    public DateTime? FreightExchangeRateDate { get; set; }
    public string FreightExchangeRateSource { get; set; } = string.Empty;
    public int? QuarantineWarehouseId { get; set; }
    public string? QuarantineWarehouseCode { get; set; }
    public string? QuarantineWarehouseName { get; set; }
    public int? QuarantineInventoryDocumentId { get; set; }
    public string? QuarantineDocumentNumber { get; set; }
    public DateTime? QuarantineDate { get; set; }
    public string? QuarantineUserId { get; set; }
    public string? Notes { get; set; }
    public List<GoodsReceiptDetailLineVM> Lines { get; set; } = [];
    public List<SelectListItem> QuarantineWarehouses { get; set; } = [];
    public int QualityInspectionCount { get; set; }
    public int PendingQualityInspectionCount { get; set; }
    public decimal FreightTax => (ActualFreightAmount ?? 0m) * (ActualFreightVatRate ?? 0m) / 100m;
    public decimal FreightGrandTotal => (ActualFreightAmount ?? 0m) + FreightTax;
    public decimal FreightGrandTotalTry => FreightGrandTotal * FreightExchangeRate;
}

public sealed class GoodsReceiptDetailLineVM
{
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal ReceivedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool LotRequired { get; set; }
    public bool ExpirationDateRequired { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? QuarantineStockLotId { get; set; }
    public string? Notes { get; set; }
}
