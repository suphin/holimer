using Ekomers.Models.Ekomers;

namespace Ekomers.Models.Entity.Profitability;

public enum RptExpenseBehavior { Excluded = 0, Fixed = 1, Variable = 2 }
public enum RptExpenseStage { Operating = 1, Financing = 2 }

/// <summary>User-defined classification only. Amounts always come from Logo accounting SELECTs.</summary>
public sealed class RptAccountingExpenseGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? PresetKey { get; set; }
    public RptExpenseBehavior Behavior { get; set; }
    public RptExpenseStage Stage { get; set; } = RptExpenseStage.Operating;
    public string? Notes { get; set; }
}

/// <summary>Exact-account, date-effective assignment. No account-prefix expansion or amount entry.</summary>
public sealed class RptAccountingExpenseRule : BaseEntity
{
    public int FirmNumber { get; set; } = 100;
    public string AccountCode { get; set; } = string.Empty;
    public int ExpenseGroupId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Notes { get; set; }
}
