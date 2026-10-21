using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;

namespace Ekomers.Data.Services.Profitability;

/// <summary>Read-only projection of the SAME eligible, date-classified amounts used in profitability.</summary>
public static class AccountingExpenseDashboard
{
    public static IReadOnlyDictionary<string, string> Categories { get; } = new Dictionary<string, string>
    {
        ["factory"] = "Fabrika", ["operating"] = "Operasyon", ["finance"] = "Finansman", ["custom"] = "Özel işletme grupları"
    }.AsReadOnly();

    public static string Category(AccountingExpenseGroupTotalVM group) => group.Stage == RptExpenseStage.Financing ? "finance" :
        group.PresetKey?.StartsWith("factory.", StringComparison.Ordinal) == true ? "factory" :
        group.PresetKey?.StartsWith("operating.", StringComparison.Ordinal) == true ? "operating" : "custom";

    public static bool ValidFilter(AccountingExpenseDashboardFilterVM filter) =>
        filter.StartDate >= new DateTime(1900, 1, 1) && filter.EndDate >= filter.StartDate && filter.EndDate < DateTime.MaxValue.Date &&
        (filter.EndDate.Year - filter.StartDate.Year) * 12 + filter.EndDate.Month - filter.StartDate.Month < 36 &&
        (filter.Category == "all" || (filter.Category != null && Categories.ContainsKey(filter.Category))) &&
        filter.Behavior is "all" or "fixed" or "variable" && filter.Search is { Length: <= 150 };

    public static AccountingExpenseDashboardVM Build(AccountingExpenseSummaryVM summary, IEnumerable<RptAccountingExpenseGroup> definitions, AccountingExpenseDashboardFilterVM filter)
    {
        if (!ValidFilter(filter)) throw new ArgumentException("Geçerli tarih/kategori/tür seçiniz. En fazla 36 takvim ayı ve 150 karakter arama kullanılabilir.");
        if (summary.StartDate.Date != filter.StartDate.Date || summary.EndDate.Date != filter.EndDate.Date)
            throw new ArgumentException("Panel ve kaynak özetin tarih aralıkları aynı olmalıdır.");
        var all = definitions.Where(g => g.IsDelete != true && g.IsActive != false && g.Behavior != RptExpenseBehavior.Excluded && Enum.IsDefined(g.Behavior) && Enum.IsDefined(g.Stage))
            .ToDictionary(g => g.ID, g => new AccountingExpenseGroupTotalVM { GroupId = g.ID, PresetKey = g.PresetKey, Name = g.Name, Behavior = g.Behavior, Stage = g.Stage });
        // Never drop already-calculated amounts if a definition is edited between the two reads.
        foreach (var total in summary.Groups) all[total.GroupId] = total;
        var searchKey = AccountingExpenseSuggestions.Normalize(filter.Search);
        var rows = all.Values.Where(g => (filter.ShowEmpty || g.MovementCount > 0) &&
            (filter.Category == "all" || Category(g) == filter.Category) &&
            (filter.Behavior == "all" || g.Behavior == (filter.Behavior == "fixed" ? RptExpenseBehavior.Fixed : RptExpenseBehavior.Variable)) &&
            (filter.Search.Length == 0 || (searchKey.Length > 0
                ? AccountingExpenseSuggestions.Normalize(g.Name).Contains(searchKey, StringComparison.Ordinal)
                : g.Name.Contains(filter.Search, StringComparison.OrdinalIgnoreCase))))
            .OrderByDescending(g => g.Amount).ThenBy(g => g.Name).ToList();
        var model = new AccountingExpenseDashboardVM { Summary = summary, Filter = filter };
        var positive = rows.Where(g => g.Amount > 0).Sum(g => g.Amount);
        model.Groups = rows.Select(g => new AccountingExpenseDashboardGroupVM { Total = g, Category = Category(g), CategoryName = Categories[Category(g)], PositiveShare = positive > 0 ? Math.Max(0, g.Amount) / positive * 100 : 0 }).ToList();
        model.Categories = Categories.Select(c => new AccountingExpenseDashboardCategoryVM {
            Key = c.Key, Name = c.Value, Amount = rows.Where(g => Category(g) == c.Key).Sum(g => g.Amount),
            GroupCount = rows.Count(g => Category(g) == c.Key),
            PositiveShare = model.Groups.Where(g => g.Category == c.Key).Sum(g => g.PositiveShare)
        }).ToList();
        var amounts = rows.SelectMany(g => g.Months).GroupBy(m => m.Month).ToDictionary(m => m.Key, m => new AccountingExpenseMonthVM {
            Month = m.Key, Fixed = m.Sum(v => v.Fixed), Variable = m.Sum(v => v.Variable), MovementCount = m.Sum(v => v.MovementCount)
        });
        var last = new DateTime(filter.EndDate.Year, filter.EndDate.Month, 1);
        for (var month = new DateTime(filter.StartDate.Year, filter.StartDate.Month, 1);; month = month.AddMonths(1))
        {
            model.Months.Add(amounts.GetValueOrDefault(month) ?? new() { Month = month });
            if (month == last) break;
        }
        return model;
    }
}
