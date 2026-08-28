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
}
