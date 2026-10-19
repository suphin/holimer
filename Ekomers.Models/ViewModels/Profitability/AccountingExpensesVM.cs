using System.ComponentModel.DataAnnotations;
using Ekomers.Models.Entity.Profitability;

namespace Ekomers.Models.ViewModels.Profitability;

public sealed class AccountingExpenseSummaryVM
{
    public const int FirmNumber = 100;
    public const int PeriodNumber = 1;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public long MovementCount { get; set; }
    public int UnmappedAccountCount { get; set; }
    public long ReviewMovementCount { get; set; }
    public long ExcludedMovementCount { get; set; }
    public decimal UnmappedAmount { get; set; }
    public decimal OperatingFixed { get; set; }
    public decimal OperatingVariable { get; set; }
    public decimal FinancingFixed { get; set; }
    public decimal FinancingVariable { get; set; }
    public decimal TotalOperating => OperatingFixed + OperatingVariable;
    public decimal TotalFinancing => FinancingFixed + FinancingVariable;
    public decimal TotalIncluded => TotalOperating + TotalFinancing;
    public bool IsComplete => MovementCount > 0 && UnmappedAccountCount == 0 && ReviewMovementCount == 0;
    public List<AccountingExpenseAccountVM> Accounts { get; set; } = [];
    public List<AccountingExpenseGroupTotalVM> Groups { get; set; } = [];
}

public sealed class AccountingExpenseAccountVM
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Net => Debit - Credit;
    public decimal IncludedAmount { get; set; }
    public long MovementCount { get; set; }
    public string Classification { get; set; } = "";
    public bool CanMap { get; set; }
    public string? ProtectedReason { get; set; }
}

public sealed class AccountingExpenseGroupTotalVM
{
    public string Name { get; set; } = "";
    public RptExpenseBehavior Behavior { get; set; }
    public RptExpenseStage Stage { get; set; }
    public decimal Amount { get; set; }
}

public sealed class AccountingExpenseManagementVM
{
    public AccountingExpenseSummaryVM Summary { get; set; } = new();
    public List<RptAccountingExpenseGroup> Groups { get; set; } = [];
    public List<AccountingExpenseRuleVM> Rules { get; set; } = [];
}

public sealed class AccountingExpenseRuleVM
{
    public int Id { get; set; }
    public int ExpenseGroupId { get; set; }
    public string AccountCode { get; set; } = "";
    public string GroupName { get; set; } = "";
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

public sealed class AccountingExpenseGroupInputVM
{
    [Required, StringLength(150)] public string Name { get; set; } = "";
    public RptExpenseBehavior Behavior { get; set; }
    public RptExpenseStage Stage { get; set; } = RptExpenseStage.Operating;
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class AccountingExpenseMappingInputVM
{
    public List<string> AccountCodes { get; set; } = [];
    public int ExpenseGroupId { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    [StringLength(500)] public string? Notes { get; set; }
}

public sealed class AccountingExpenseMovementsVM
{
    public string AccountCode { get; set; } = "";
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Page { get; set; } = 1;
    public long TotalCount { get; set; }
    public long TotalPages => Math.Max(1, (TotalCount + 49) / 50);
    public List<AccountingExpenseMovementVM> Rows { get; set; } = [];
}

public sealed class AccountingExpenseMovementVM
{
    public int MovementRef { get; set; }
    public int VoucherRef { get; set; }
    public DateTime Date { get; set; }
    public string VoucherNumber { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public string Status { get; set; } = "";
}

public sealed class AccountingExpenseSuggestionsVM
{
    public AccountingExpenseSummaryVM Summary { get; set; } = new();
    public List<RptAccountingExpenseGroup> Groups { get; set; } = [];
    public List<AccountingExpenseSuggestionRowVM> Rows { get; set; } = [];
    public int AlreadyMappedCount { get; set; }
    public int ProtectedCount { get; set; }
    public int TotalCandidates { get; set; }
    public int Page { get; set; } = 1;
    public int TotalPages => Math.Max(1, (TotalCandidates + 99) / 100);
}

public sealed class AccountingExpenseSuggestionRowVM
{
    public string AccountCode { get; set; } = "";
    public string AccountName { get; set; } = "";
    public string Category { get; set; } = "";
    public string Reason { get; set; } = "";
    public int? SuggestedGroupId { get; set; }
    public bool NeedsReview { get; set; }
}

public sealed class AccountingExpenseSuggestionApprovalVM
{
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public List<AccountingExpenseSuggestionSelectionVM> Rows { get; set; } = [];
}

public sealed class AccountingExpenseSuggestionSelectionVM
{
    public bool Selected { get; set; }
    [StringLength(100)] public string AccountCode { get; set; } = "";
    public int? ExpenseGroupId { get; set; }
}
