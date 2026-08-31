namespace Ekomers.Models.ViewModels.Profitability;

public sealed class SalesProfitabilityFilterVM
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Search { get; set; }
    public string? PriceStatus { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public sealed class SalesProfitabilityPreviewVM
{
    public SalesProfitabilityFilterVM Filter { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal ReferenceGrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal DiscountRate { get; set; }
    public int MissingReferencePriceCount { get; set; }
    public decimal KnownCostAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetProfitRate { get; set; }
    public int MissingCostLineCount { get; set; }
    public int PageCount { get; set; }
    public IReadOnlyList<string> PriceStatuses { get; set; } = [];
    public IReadOnlyList<SalesProfitabilityPreviewRowVM> Rows { get; set; } = [];
}

public sealed class SalesProfitabilityPreviewRowVM
{
    public int LogoMaterialRef { get; set; }
    public string LineType { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public bool IsReturn { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public DateTime? ReferencePriceDate { get; set; }
    public decimal? ReferenceUnitPrice { get; set; }
    public decimal? ReferenceGrossRevenue { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal? DiscountAmount { get; set; }
    public decimal? DiscountRate { get; set; }
    public decimal? UnitCostTry { get; set; }
    public decimal? CostAmount { get; set; }
    public decimal? GrossProfit { get; set; }
    public decimal? NetProfit { get; set; }
    public decimal? NetProfitRate { get; set; }
    public string CostStatus { get; set; } = string.Empty;
    public string PriceStatus { get; set; } = string.Empty;
}
