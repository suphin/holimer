namespace Ekomers.Models.ViewModels.Profitability;

public sealed class ChannelBalancingVM
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime RetrievedAt { get; set; }
    public string TargetChannel { get; set; } = string.Empty;
    public decimal TargetNetRevenue { get; set; }
    public bool HasDefinedChannels { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<string> ProductPrefixes { get; set; } = [];
    public IReadOnlyList<ChannelBalancingRowVM> Rows { get; set; } = [];
}

public sealed class ChannelBalancingRowVM
{
    public string Channel { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal AverageNetRevenuePerUnit { get; set; }
    public decimal KnownCostAmount { get; set; }
    public decimal NetProfit { get; set; }
    public decimal NetProfitRate { get; set; }
    public decimal RevenueGap { get; set; }
    public decimal? RequiredAdditionalQuantity { get; set; }
    public decimal CompletionRate { get; set; }
    public int MissingCostLineCount { get; set; }
    public bool IsTarget { get; set; }
}
