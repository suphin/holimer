using Ekomers.Models.Entity.Profitability;

namespace Ekomers.Models.ViewModels.Profitability;

public sealed class AccountingExpenseDashboardFilterVM
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Category { get; set; } = "all";
    public string Behavior { get; set; } = "all";
    public string Search { get; set; } = "";
    public bool ShowEmpty { get; set; }
}

public sealed class AccountingExpenseDashboardVM
{
    public AccountingExpenseDashboardFilterVM Filter { get; set; } = new();
    public AccountingExpenseSummaryVM Summary { get; set; } = new();
    public string? Error { get; set; }
    public List<AccountingExpenseDashboardGroupVM> Groups { get; set; } = [];
    public List<AccountingExpenseDashboardCategoryVM> Categories { get; set; } = [];
    public List<AccountingExpenseMonthVM> Months { get; set; } = [];
    public decimal Total => Groups.Sum(x => x.Total.Amount);
    public decimal Fixed => Groups.Where(x => x.Total.Behavior == RptExpenseBehavior.Fixed).Sum(x => x.Total.Amount);
    public decimal Variable => Total - Fixed;
    public decimal Debit => Groups.Sum(x => x.Total.Debit);
    public decimal Credit => Groups.Sum(x => x.Total.Credit);
    public long MovementCount => Groups.Sum(x => x.Total.MovementCount);
    public decimal PositiveTotal => Groups.Where(x => x.Total.Amount > 0).Sum(x => x.Total.Amount);
    public decimal TrendScale => Math.Max(1m, Months.Select(x => Math.Abs(x.Amount)).DefaultIfEmpty(0).Max());
}

public sealed class AccountingExpenseDashboardGroupVM
{
    public string Category { get; set; } = "";
    public string CategoryName { get; set; } = "";
    public AccountingExpenseGroupTotalVM Total { get; set; } = new();
    public decimal PositiveShare { get; set; }
}

public sealed class AccountingExpenseDashboardCategoryVM
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal PositiveShare { get; set; }
    public int GroupCount { get; set; }
}
