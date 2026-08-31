namespace Ekomers.Models.Enums;

public enum PurPurchaseRequestStatus
{
    Draft = 0,
    PendingApproval = 1,
    PartiallyApproved = 2,
    Approved = 3,
    Rejected = 4,
    InQuotation = 5,
    Completed = 6,
    Cancelled = 7
}

public enum PurPurchaseRequestLineStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    InQuotation = 4,
    Ordered = 5,
    Closed = 6,
    Cancelled = 7
}

public enum PurPurchaseRequestPriority
{
    Low = 1,
    Normal = 2,
    High = 3,
    Urgent = 4
}

public enum PurPurchaseRequestSource
{
    Manual = 1,
    Mrp = 2
}

public enum PurRequestApprovalAction
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum PurSupplierQuotationStatus
{
    Draft = 0,
    PendingApproval = 1,
    PartiallyApproved = 2,
    Approved = 3,
    Rejected = 4,
    ConvertedToOrder = 5,
    Cancelled = 6,
    NotSelected = 7
}

public enum PurSupplierQuotationLineStatus
{
    Draft = 0,
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Ordered = 4,
    Cancelled = 5,
    NotSelected = 6
}

public enum PurQuotationApprovalAction
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

public enum PurPurchaseOrderStatus
{
    Open = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public enum PurPurchaseOrderLineStatus
{
    Open = 1,
    PartiallyReceived = 2,
    Received = 3,
    Cancelled = 4
}

public enum PurTransportationType
{
    Road = 1,
    Air = 2,
    Sea = 3,
    Cargo = 4,
    Courier = 5,
    SupplierVehicle = 6,
    CompanyVehicle = 7,
    Other = 8
}

public enum PurFreightPaymentType
{
    Supplier = 1,
    Buyer = 2,
    IncludedInPrice = 3
}

public enum PurGoodsReceiptStatus
{
    Recorded = 1,
    InQuarantine = 2,
    QualityApproved = 3,
    QualityRejected = 4,
    TransferredToStock = 5,
    Returned = 6,
    Scrapped = 7,
    Cancelled = 8,
    QualityPartiallyDecided = 9
}

public static class PurchasingEnumExtensions
{
    public static string ToTurkish(this PurPurchaseRequestStatus value) => value switch
    {
        PurPurchaseRequestStatus.Draft => "Taslak",
        PurPurchaseRequestStatus.PendingApproval => "Onay Bekliyor",
        PurPurchaseRequestStatus.PartiallyApproved => "Kısmen Onaylandı",
        PurPurchaseRequestStatus.Approved => "Onaylandı",
        PurPurchaseRequestStatus.Rejected => "Reddedildi",
        PurPurchaseRequestStatus.InQuotation => "Teklif Aşamasında",
        PurPurchaseRequestStatus.Completed => "Tamamlandı",
        PurPurchaseRequestStatus.Cancelled => "İptal Edildi",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurPurchaseRequestLineStatus value) => value switch
    {
        PurPurchaseRequestLineStatus.Draft => "Taslak",
        PurPurchaseRequestLineStatus.PendingApproval => "Onay Bekliyor",
        PurPurchaseRequestLineStatus.Approved => "Onaylandı",
        PurPurchaseRequestLineStatus.Rejected => "Reddedildi",
        PurPurchaseRequestLineStatus.InQuotation => "Teklif Aşamasında",
        PurPurchaseRequestLineStatus.Ordered => "Sipariş Verildi",
        PurPurchaseRequestLineStatus.Closed => "Kapandı",
        PurPurchaseRequestLineStatus.Cancelled => "İptal Edildi",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurPurchaseRequestPriority value) => value switch
    {
        PurPurchaseRequestPriority.Low => "Düşük",
        PurPurchaseRequestPriority.Normal => "Normal",
        PurPurchaseRequestPriority.High => "Yüksek",
        PurPurchaseRequestPriority.Urgent => "Acil",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurPurchaseRequestSource value) => value switch
    {
        PurPurchaseRequestSource.Manual => "Manuel",
        PurPurchaseRequestSource.Mrp => "MRP",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurSupplierQuotationStatus value) => value switch
    {
        PurSupplierQuotationStatus.Draft => "Taslak",
        PurSupplierQuotationStatus.PendingApproval => "Onay Bekliyor",
        PurSupplierQuotationStatus.PartiallyApproved => "Kısmen Onaylandı",
        PurSupplierQuotationStatus.Approved => "Onaylandı",
        PurSupplierQuotationStatus.Rejected => "Reddedildi",
        PurSupplierQuotationStatus.ConvertedToOrder => "Siparişe Dönüştü",
        PurSupplierQuotationStatus.Cancelled => "İptal Edildi",
        PurSupplierQuotationStatus.NotSelected => "Seçilmedi",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurSupplierQuotationLineStatus value) => value switch
    {
        PurSupplierQuotationLineStatus.Draft => "Taslak",
        PurSupplierQuotationLineStatus.PendingApproval => "Onay Bekliyor",
        PurSupplierQuotationLineStatus.Approved => "Onaylandı",
        PurSupplierQuotationLineStatus.Rejected => "Reddedildi",
        PurSupplierQuotationLineStatus.Ordered => "Siparişe Dönüştü",
        PurSupplierQuotationLineStatus.Cancelled => "İptal Edildi",
        PurSupplierQuotationLineStatus.NotSelected => "Seçilmedi",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurPurchaseOrderStatus value) => value switch
    {
        PurPurchaseOrderStatus.Open => "Açık",
        PurPurchaseOrderStatus.PartiallyReceived => "Kısmen Teslim Alındı",
        PurPurchaseOrderStatus.Received => "Teslim Alındı",
        PurPurchaseOrderStatus.Cancelled => "İptal Edildi",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurPurchaseOrderLineStatus value) => value switch
    {
        PurPurchaseOrderLineStatus.Open => "Açık",
        PurPurchaseOrderLineStatus.PartiallyReceived => "Kısmen Teslim Alındı",
        PurPurchaseOrderLineStatus.Received => "Teslim Alındı",
        PurPurchaseOrderLineStatus.Cancelled => "İptal Edildi",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurTransportationType value) => value switch
    {
        PurTransportationType.Road => "Karayolu",
        PurTransportationType.Air => "Havayolu",
        PurTransportationType.Sea => "Denizyolu",
        PurTransportationType.Cargo => "Kargo",
        PurTransportationType.Courier => "Kurye",
        PurTransportationType.SupplierVehicle => "Tedarikçi Aracı",
        PurTransportationType.CompanyVehicle => "Firma Aracı",
        PurTransportationType.Other => "Diğer",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurFreightPaymentType value) => value switch
    {
        PurFreightPaymentType.Supplier => "Tedarikçi Karşılayacak",
        PurFreightPaymentType.Buyer => "Biz Karşılayacağız",
        PurFreightPaymentType.IncludedInPrice => "Sipariş Fiyatına Dahil",
        _ => value.ToString()
    };

    public static string ToTurkish(this PurGoodsReceiptStatus value) => value switch
    {
        PurGoodsReceiptStatus.Recorded => "Teslim Alındı",
        PurGoodsReceiptStatus.InQuarantine => "Karantinada",
        PurGoodsReceiptStatus.QualityApproved => "Kalite Onaylı",
        PurGoodsReceiptStatus.QualityRejected => "Kalite Reddi",
        PurGoodsReceiptStatus.TransferredToStock => "Kullanılabilir Stokta",
        PurGoodsReceiptStatus.Returned => "İade Edildi",
        PurGoodsReceiptStatus.Scrapped => "Hurdaya Ayrıldı",
        PurGoodsReceiptStatus.Cancelled => "İptal Edildi",
        PurGoodsReceiptStatus.QualityPartiallyDecided => "Kalite Kararı Kısmen Tamamlandı",
        _ => value.ToString()
    };
}
