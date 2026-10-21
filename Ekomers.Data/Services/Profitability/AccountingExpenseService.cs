using System.Data;
using Dapper;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services.Profitability;

public interface IAccountingExpenseService
{
    Task<AccountingExpenseSummaryVM> GetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken ct);
    Task<AccountingExpenseMovementsVM> GetMovementsAsync(string accountCode, DateTime startDate, DateTime endDate, int page, CancellationToken ct);
}

/// <summary>100/01, same firm and period as the sales report. All ERP commands are SELECT-only.</summary>
public sealed class AccountingExpenseService(ApplicationDbContext context, LogoContext logoContext) : IAccountingExpenseService
{
    private static readonly HashSet<string> ExpenseMainCodes = ["720", "730", "740", "750", "760", "770", "780"];
    public static bool CanMap(string code) => code.Length >= 3 && ExpenseMainCodes.Contains(code[..3]);
    public static string? ProtectedReason(string code) => code.StartsWith("710") || code.StartsWith("790")
        ? "Hammadde/ambalaj ürün maliyetinde; ikinci kez düşülmez"
        : CanMap(code) ? null : "Bağlantı/yansıtma/fark veya bu sürümde kapsam dışı hesap";

    private const string Source = """
        WITH SourceRows AS (
            SELECT L.LOGICALREF AS MovementRef, F.LOGICALREF AS VoucherRef, F.DATE_ AS [Date],
                   LTRIM(RTRIM(A.CODE)) AS AccountCode, A.DEFINITION_ AS AccountName,
                   F.FICHENO AS VoucherNumber, COALESCE(NULLIF(L.LINEEXP,''),F.GENEXP1,'') AS Description,
                   CAST(COALESCE(L.DEBIT,0) AS decimal(28,6)) AS Debit,
                   CAST(COALESCE(L.CREDIT,0) AS decimal(28,6)) AS Credit,
                   CASE WHEN COALESCE(F.CANCELLED,0)<>0 OR COALESCE(L.CANCELLED,0)<>0 THEN 'Cancelled'
                        WHEN LEFT(LTRIM(A.CODE),3) IN ('710','790') THEN 'Material'
                        WHEN LEFT(LTRIM(A.CODE),3) IN ('791','792','793','794','795','796','797') THEN 'ReviewAccount'
                        WHEN LEFT(LTRIM(A.CODE),3) NOT IN ('720','730','740','750','760','770','780') THEN 'Protected'
                        WHEN COALESCE(F.TRCODE,-1)<>4 THEN 'ReviewVoucher'
                        WHEN EXISTS (
                            SELECT 1 FROM dbo.LG_100_01_EMFLINE R
                            JOIN dbo.LG_100_EMUHACC RA ON RA.LOGICALREF=R.ACCOUNTREF
                            WHERE R.ACCFICHEREF=F.LOGICALREF AND COALESCE(R.CANCELLED,0)=0
                              AND LEFT(LTRIM(RA.CODE),3) IN ('700','701','711','721','731','741','751','761','771','781','798','799')
                        ) THEN 'ReflectionVoucher'
                        ELSE '' END AS BlockReason
            FROM dbo.LG_100_01_EMFLINE L
            JOIN dbo.LG_100_01_EMFICHE F ON F.LOGICALREF=L.ACCFICHEREF
            JOIN dbo.LG_100_EMUHACC A ON A.LOGICALREF=L.ACCOUNTREF
            WHERE F.DATE_>=@StartDate AND F.DATE_<@EndExclusive AND LTRIM(A.CODE) LIKE '7%'
        )
        """;

    public async Task<AccountingExpenseSummaryVM> GetSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        (startDate, endDate) = Dates(startDate, endDate);
        var connection = logoContext.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(ct);
        List<DailyAmount> source;
        try
        {
            source = (await connection.QueryAsync<DailyAmount>(new CommandDefinition(Source + """
                SELECT AccountCode, MAX(AccountName) AS AccountName, CONVERT(date,[Date]) AS [Date], BlockReason,
                       COUNT_BIG(*) AS MovementCount, SUM(Debit) AS Debit, SUM(Credit) AS Credit
                FROM SourceRows GROUP BY AccountCode,CONVERT(date,[Date]),BlockReason;
                """, new { StartDate = startDate, EndExclusive = endDate.AddDays(1) }, commandTimeout: 60, cancellationToken: ct))).ToList();
        }
        finally { if (close) await connection.CloseAsync(); }
        var rules = await (from rule in context.RptAccountingExpenseRules.AsNoTracking()
                           join expenseGroup in context.RptAccountingExpenseGroups.AsNoTracking() on rule.ExpenseGroupId equals expenseGroup.ID
                           where rule.FirmNumber == 100 && rule.IsDelete != true && rule.IsActive != false && expenseGroup.IsDelete != true && expenseGroup.IsActive != false &&
                               rule.ValidFrom <= endDate && (rule.ValidTo == null || rule.ValidTo >= startDate)
                           select new { Rule = rule, Group = expenseGroup }).ToListAsync(ct);
        var byCode = rules.GroupBy(x => x.Rule.AccountCode).ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);
        var summary = new AccountingExpenseSummaryVM { StartDate = startDate, EndDate = endDate, MovementCount = source.Sum(x => x.MovementCount) };
        var unclassified = new HashSet<string>();
        var groups = new Dictionary<int, AccountingExpenseGroupTotalVM>();
        var groupAccounts = new Dictionary<(int Group, string Code), AccountingExpenseGroupAccountVM>();
        var groupMonths = new Dictionary<(int Group, DateTime Month), AccountingExpenseMonthVM>();
        foreach (var account in source.GroupBy(x => x.AccountCode))
        {
            var item = new AccountingExpenseAccountVM { AccountCode = account.Key, AccountName = account.First().AccountName,
                Debit = account.Sum(x => x.Debit), Credit = account.Sum(x => x.Credit), MovementCount = account.Sum(x => x.MovementCount),
                CanMap = CanMap(account.Key), ProtectedReason = ProtectedReason(account.Key) };
            var labels = new HashSet<string>();
            foreach (var daily in account)
            {
                if (daily.BlockReason.Length > 0)
                {
                    if (daily.BlockReason is "ReviewVoucher" or "ReviewAccount") summary.ReviewMovementCount += daily.MovementCount;
                    else summary.ExcludedMovementCount += daily.MovementCount;
                    labels.Add(BlockText(daily.BlockReason));
                    continue;
                }
                var matching = byCode.GetValueOrDefault(account.Key)?.Where(x => x.Rule.ValidFrom.Date <= daily.Date.Date &&
                    (!x.Rule.ValidTo.HasValue || x.Rule.ValidTo.Value.Date >= daily.Date.Date)).ToList();
                if (matching?.Count != 1 || !Enum.IsDefined(matching[0].Group.Behavior) || !Enum.IsDefined(matching[0].Group.Stage))
                {
                    unclassified.Add(account.Key); summary.UnmappedAmount += daily.Debit - daily.Credit;
                    labels.Add(matching?.Count > 1 ? "Çakışan sınıflandırma" : "Sınıflandırma bekliyor");
                    continue;
                }
                var group = matching[0].Group;
                labels.Add(group.Name);
                if (group.Behavior == RptExpenseBehavior.Excluded) { summary.ExcludedMovementCount += daily.MovementCount; continue; }
                var amount = daily.Debit - daily.Credit;
                item.IncludedAmount += amount;
                if (!groups.TryGetValue(group.ID, out var total)) groups[group.ID] = total = new() { GroupId = group.ID, PresetKey = group.PresetKey, Name = group.Name, Behavior = group.Behavior, Stage = group.Stage };
                total.Amount += amount;
                total.Debit += daily.Debit; total.Credit += daily.Credit; total.MovementCount += daily.MovementCount;
                if (!groupAccounts.TryGetValue((group.ID, account.Key), out var detail))
                {
                    groupAccounts[(group.ID, account.Key)] = detail = new() { AccountCode = account.Key, AccountName = item.AccountName };
                    total.Accounts.Add(detail);
                }
                detail.Debit += daily.Debit; detail.Credit += daily.Credit; detail.MovementCount += daily.MovementCount;
                var month = new DateTime(daily.Date.Year, daily.Date.Month, 1);
                if (!groupMonths.TryGetValue((group.ID, month), out var monthly))
                {
                    groupMonths[(group.ID, month)] = monthly = new() { Month = month };
                    total.Months.Add(monthly);
                }
                if (group.Behavior == RptExpenseBehavior.Fixed) monthly.Fixed += amount; else monthly.Variable += amount;
                monthly.MovementCount += daily.MovementCount;
            }
            item.Classification = string.Join(" · ", labels.OrderBy(x => x));
            summary.Accounts.Add(item);
        }
        summary.UnmappedAccountCount = unclassified.Count;
        summary.Groups = groups.Values.OrderBy(x => x.Stage).ThenBy(x => x.Behavior).ThenBy(x => x.Name).ToList();
        foreach (var total in summary.Groups)
        {
            total.Accounts = total.Accounts.OrderBy(x => x.AccountCode).ToList();
            total.Months = total.Months.OrderBy(x => x.Month).ToList();
        }
        summary.Accounts = summary.Accounts.OrderBy(x => x.AccountCode).ToList();
        summary.OperatingFixed = groups.Values.Where(x => x.Stage == RptExpenseStage.Operating && x.Behavior == RptExpenseBehavior.Fixed).Sum(x => x.Amount);
        summary.OperatingVariable = groups.Values.Where(x => x.Stage == RptExpenseStage.Operating && x.Behavior == RptExpenseBehavior.Variable).Sum(x => x.Amount);
        summary.FinancingFixed = groups.Values.Where(x => x.Stage == RptExpenseStage.Financing && x.Behavior == RptExpenseBehavior.Fixed).Sum(x => x.Amount);
        summary.FinancingVariable = groups.Values.Where(x => x.Stage == RptExpenseStage.Financing && x.Behavior == RptExpenseBehavior.Variable).Sum(x => x.Amount);
        return summary;
    }

    public async Task<AccountingExpenseMovementsVM> GetMovementsAsync(string accountCode, DateTime startDate, DateTime endDate, int page, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(accountCode) || accountCode.Length > 100 || !accountCode.StartsWith('7') || page < 1) throw new ArgumentException("Geçersiz hesap/sayfa.");
        (startDate, endDate) = Dates(startDate, endDate);
        var result = new AccountingExpenseMovementsVM { AccountCode = accountCode, StartDate = startDate, EndDate = endDate, Page = page };
        var connection = logoContext.Database.GetDbConnection();
        var close = connection.State != ConnectionState.Open;
        if (close) await connection.OpenAsync(ct);
        try
        {
            using var reader = await connection.QueryMultipleAsync(new CommandDefinition(Source + "SELECT COUNT_BIG(*) FROM SourceRows WHERE AccountCode=@AccountCode;" +
                Source + "SELECT MovementRef,VoucherRef,[Date],VoucherNumber,Description,Debit,Credit,BlockReason AS Status FROM SourceRows WHERE AccountCode=@AccountCode ORDER BY [Date] DESC,VoucherRef DESC,MovementRef DESC OFFSET @Offset ROWS FETCH NEXT 50 ROWS ONLY;",
                new { AccountCode = new DbString { Value = accountCode, IsAnsi = true, Length = 100 }, StartDate = startDate, EndExclusive = endDate.AddDays(1), Offset = ((long)page - 1) * 50 }, commandTimeout: 60, cancellationToken: ct));
            result.TotalCount = await reader.ReadSingleAsync<long>();
            result.Rows = (await reader.ReadAsync<AccountingExpenseMovementVM>()).ToList();
            foreach (var row in result.Rows) row.Status = row.Status.Length == 0 ? "Sınıflandırma kurallarına tabi" : BlockText(row.Status);
        }
        finally { if (close) await connection.CloseAsync(); }
        return result;
    }

    private static (DateTime, DateTime) Dates(DateTime start, DateTime end)
    {
        start = start.Date; end = end.Date;
        if (start < new DateTime(1900, 1, 1) || end >= DateTime.MaxValue.Date) throw new ArgumentException("Geçersiz tarih.");
        return end < start ? (end, start) : (start, end);
    }
    private static string BlockText(string reason) => reason switch
    {
        "Cancelled" => "İptal · hariç", "Material" => "Ürün malzeme maliyetinde · hariç", "Protected" => "Bağlantı/yansıtma/fark/kapsam dışı hesap",
        "ReviewVoucher" => "Fiş türü doğrulaması bekliyor · hariç", "ReviewAccount" => "7/B gider hesabı incelemesi bekliyor · hariç", "ReflectionVoucher" => "Yansıtma karşı hesabı içeren fiş · hariç", _ => reason
    };
    private sealed class DailyAmount
    {
        public string AccountCode { get; set; } = "";
        public string AccountName { get; set; } = "";
        public DateTime Date { get; set; }
        public string BlockReason { get; set; } = "";
        public long MovementCount { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }
}
