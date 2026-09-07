namespace Ekomers.Models.ViewModels.Profitability;

public sealed class CustomerReceivablesVM
{
    public DateTime RetrievedAt { get; set; }
    public decimal TotalReceivable { get; set; }
    public decimal TotalCustomerCredit { get; set; }
    public decimal? TotalOverdue { get; set; }
    public decimal? TotalNotDue { get; set; }
    public bool HasDueDateBreakdown { get; set; }
    public IReadOnlyList<CustomerReceivableRowVM> Rows { get; set; } = [];
}

public sealed class CustomerReceivableRowVM
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal DebitTotal { get; set; }
    public decimal CreditTotal { get; set; }
    public decimal Receivable { get; set; }
    public decimal CustomerCredit { get; set; }
    public decimal? OverdueReceivable { get; set; }
    public decimal? NotDueReceivable { get; set; }
}

public sealed class CustomerReceivableDetailVM
{
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal OpenBalance { get; set; }
    public bool IsAvailable { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<CustomerReceivableMovementVM> Movements { get; set; } = [];
}

public sealed class CustomerReceivableMovementVM
{
    public DateTime DueDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string DocumentReference { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal Paid { get; set; }
    public decimal OpenAmount { get; set; }
    public bool IsOverdue { get; set; }
}
