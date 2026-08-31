using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity.Profitability;

public enum RptProductCostVersionStatus
{
    Draft = 1,
    Active = 2,
    Closed = 3
}

/// <summary>
/// Logo ürünlerinin uygulama tarafından yönetilen tarihsel maliyet versiyonudur.
/// Kayıt EkomerDB'de tutulur; Logo veritabanına yazılmaz.
/// </summary>
public sealed class RptProductCostVersion : BaseEntity
{
    public int LogoMaterialRef { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public RptProductCostVersionStatus Status { get; set; } = RptProductCostVersionStatus.Draft;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal FreightCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalUnitCost { get; set; }
    public string CurrencyCode { get; set; } = "TRY";
    public decimal ExchangeRate { get; set; } = 1m;
    public decimal TotalUnitCostTry { get; set; }
    public string Source { get; set; } = "Manual";
    public string? ChangeReason { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
}
