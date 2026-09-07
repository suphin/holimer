using System.Data;
using System.Data.Common;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Ekomers.Data.Services.Profitability;

public interface ICustomerReceivablesService
{
    Task<CustomerReceivablesVM> GetSummaryAsync(string? search, CancellationToken ct);
    Task<CustomerReceivableDetailVM> GetDetailAsync(string customerCode, CancellationToken ct);
}

public sealed class CustomerReceivablesService : ICustomerReceivablesService
{
    private const string BalanceView = "dbo.VW_100_BorcAlacak";
    private const string PaymentTable = "dbo.LG_100_01_PAYTRANS";
    private const string CustomerTable = "dbo.LG_100_CLCARD";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly LogoContext _logoContext;
    private readonly IMemoryCache _cache;

    public CustomerReceivablesService(LogoContext logoContext, IMemoryCache cache)
    {
        _logoContext = logoContext;
        _cache = cache;
    }

    public async Task<CustomerReceivablesVM> GetSummaryAsync(string? search, CancellationToken ct)
    {
        var snapshot = await _cache.GetOrCreateAsync(
            "sales-profitability:receivables:summary:v1",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return new ReceivablesSnapshot
                {
                    RetrievedAt = DateTime.Now,
                    Rows = await ReadSummaryRowsAsync(ct)
                };
            }) ?? new ReceivablesSnapshot();
        var allRows = snapshot.Rows;

        var normalizedSearch = search?.Trim();
        var rows = string.IsNullOrWhiteSpace(normalizedSearch)
            ? allRows
            : allRows.Where(x =>
                    x.CustomerCode.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase) ||
                    x.CustomerName.Contains(normalizedSearch, StringComparison.CurrentCultureIgnoreCase))
                .ToList();

        return new CustomerReceivablesVM
        {
            RetrievedAt = snapshot.RetrievedAt,
            TotalReceivable = rows.Sum(x => x.Receivable),
            TotalCustomerCredit = rows.Sum(x => x.CustomerCredit),
            TotalOverdue = rows.Any(x => x.OverdueReceivable.HasValue)
                ? rows.Sum(x => x.OverdueReceivable ?? 0m)
                : null,
            TotalNotDue = rows.Any(x => x.NotDueReceivable.HasValue)
                ? rows.Sum(x => x.NotDueReceivable ?? 0m)
                : null,
            HasDueDateBreakdown = rows.Any(x => x.OverdueReceivable.HasValue),
            Rows = rows
                .Where(x => x.Receivable != 0m || x.CustomerCredit != 0m)
                .OrderByDescending(x => x.Receivable)
                .ThenBy(x => x.CustomerCode)
                .ToList()
        };
    }

    public async Task<CustomerReceivableDetailVM> GetDetailAsync(
        string customerCode,
        CancellationToken ct)
    {
        customerCode = customerCode.Trim();
        if (string.IsNullOrWhiteSpace(customerCode))
        {
            return new CustomerReceivableDetailVM
            {
                Message = "Cari kodu belirtilmedi."
            };
        }

        return await _cache.GetOrCreateAsync(
            $"sales-profitability:receivables:detail:v1:{customerCode.ToUpperInvariant()}",
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await ReadDetailAsync(customerCode, ct);
            }) ?? new CustomerReceivableDetailVM
        {
            CustomerCode = customerCode,
            Message = "Cari hareketleri okunamadı."
        };
    }

    private async Task<List<CustomerReceivableRowVM>> ReadSummaryRowsAsync(CancellationToken ct)
    {
        var connection = _logoContext.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            var dueBalances = new Dictionary<string, DueBalance>(StringComparer.OrdinalIgnoreCase);
            try
            {
                if (await CanReadPaymentTransactionsAsync(connection, ct))
                {
                    dueBalances = await ReadDueBalancesAsync(connection, ct);
                }
            }
            catch (DbException)
            {
                // Cari bakiye görünümü kullanılmaya devam eder; yalnızca vade kırılımı gösterilmez.
            }

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT
                    CONVERT(nvarchar(100), CHKOD) AS CustomerCode,
                    CONVERT(nvarchar(250), CHUNVAN) AS CustomerName,
                    COALESCE(CONVERT(decimal(38, 6), [Borç Toplamı]), 0) AS DebitTotal,
                    COALESCE(CONVERT(decimal(38, 6), [Alacak Toplamı]), 0) AS CreditTotal
                FROM {BalanceView};
                """;

            var rows = new List<CustomerReceivableRowVM>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var code = ReadString(reader, "CustomerCode") ?? string.Empty;
                var debit = ReadDecimal(reader, "DebitTotal");
                var credit = ReadDecimal(reader, "CreditTotal");
                dueBalances.TryGetValue(code, out var due);
                rows.Add(new CustomerReceivableRowVM
                {
                    CustomerCode = code,
                    CustomerName = ReadString(reader, "CustomerName") ?? string.Empty,
                    DebitTotal = debit,
                    CreditTotal = credit,
                    Receivable = Math.Max(debit - credit, 0m),
                    CustomerCredit = Math.Max(credit - debit, 0m),
                    OverdueReceivable = due?.Overdue,
                    NotDueReceivable = due?.NotDue
                });
            }

            return rows;
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private async Task<CustomerReceivableDetailVM> ReadDetailAsync(
        string customerCode,
        CancellationToken ct)
    {
        var connection = _logoContext.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            if (!await CanReadPaymentTransactionsAsync(connection, ct))
            {
                return new CustomerReceivableDetailVM
                {
                    CustomerCode = customerCode,
                    Message = "Logo ödeme hareketi tablosunda gerekli alanlar bulunamadı."
                };
            }

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT TOP (250)
                    CONVERT(nvarchar(100), C.CODE) AS CustomerCode,
                    CONVERT(nvarchar(250), C.DEFINITION_) AS CustomerName,
                    CONVERT(date, P.DATE_) AS DueDate,
                    P.MODULENR AS ModuleNumber,
                    P.TRCODE AS TransactionCode,
                    P.FICHEREF AS DocumentReference,
                    CONVERT(decimal(38, 6), P.TOTAL) AS Total,
                    CONVERT(decimal(38, 6), P.PAID) AS Paid,
                    CONVERT(decimal(38, 6),
                        CASE WHEN P.SIGN = 0
                             THEN P.TOTAL - P.PAID
                             ELSE -(P.TOTAL - P.PAID) END) AS OpenAmount
                FROM {PaymentTable} P WITH (NOLOCK)
                INNER JOIN {CustomerTable} C WITH (NOLOCK) ON C.LOGICALREF = P.CARDREF
                WHERE C.CODE = @customerCode
                  AND P.CANCELLED = 0
                  AND ABS(P.TOTAL - P.PAID) > 0.005
                ORDER BY P.DATE_, P.LOGICALREF;
                """;
            AddParameter(command, "@customerCode", DbType.String, customerCode);

            var movements = new List<CustomerReceivableMovementVM>();
            var customerName = string.Empty;
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                customerName = ReadString(reader, "CustomerName") ?? customerName;
                var moduleNumber = ReadInt32(reader, "ModuleNumber");
                var transactionCode = ReadInt32(reader, "TransactionCode");
                var dueDate = ReadDateTime(reader, "DueDate");
                movements.Add(new CustomerReceivableMovementVM
                {
                    DueDate = dueDate,
                    TransactionType = GetTransactionType(moduleNumber, transactionCode),
                    DocumentReference = ReadInt64(reader, "DocumentReference").ToString(),
                    Total = ReadDecimal(reader, "Total"),
                    Paid = ReadDecimal(reader, "Paid"),
                    OpenAmount = ReadDecimal(reader, "OpenAmount"),
                    IsOverdue = dueDate.Date < DateTime.Today
                });
            }

            return new CustomerReceivableDetailVM
            {
                CustomerCode = customerCode,
                CustomerName = customerName,
                OpenBalance = movements.Sum(x => x.OpenAmount),
                IsAvailable = true,
                Movements = movements
            };
        }
        catch (DbException)
        {
            return new CustomerReceivableDetailVM
            {
                CustomerCode = customerCode,
                Message = "Logo açık ödeme hareketleri şu anda okunamadı. Güncel cari bakiye özetini kullanabilirsiniz."
            };
        }
        finally
        {
            if (closeConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static async Task<Dictionary<string, DueBalance>> ReadDueBalancesAsync(
        DbConnection connection,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                CONVERT(nvarchar(100), C.CODE) AS CustomerCode,
                COALESCE(SUM(CASE
                    WHEN P.SIGN = 0 AND CONVERT(date, P.DATE_) < CONVERT(date, GETDATE())
                    THEN CONVERT(decimal(38, 6), P.TOTAL - P.PAID)
                    ELSE 0 END), 0) AS Overdue,
                COALESCE(SUM(CASE
                    WHEN P.SIGN = 0 AND CONVERT(date, P.DATE_) >= CONVERT(date, GETDATE())
                    THEN CONVERT(decimal(38, 6), P.TOTAL - P.PAID)
                    ELSE 0 END), 0) AS NotDue
            FROM {PaymentTable} P WITH (NOLOCK)
            INNER JOIN {CustomerTable} C WITH (NOLOCK) ON C.LOGICALREF = P.CARDREF
            WHERE P.CANCELLED = 0
              AND P.TOTAL - P.PAID > 0.005
            GROUP BY C.CODE;
            """;

        var result = new Dictionary<string, DueBalance>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var code = ReadString(reader, "CustomerCode");
            if (!string.IsNullOrWhiteSpace(code))
            {
                result[code] = new DueBalance
                {
                    Overdue = ReadDecimal(reader, "Overdue"),
                    NotDue = ReadDecimal(reader, "NotDue")
                };
            }
        }
        return result;
    }

    private static async Task<bool> CanReadPaymentTransactionsAsync(
        DbConnection connection,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(1)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = N'dbo'
              AND TABLE_NAME = N'LG_100_01_PAYTRANS'
              AND COLUMN_NAME IN
                  (N'LOGICALREF', N'CARDREF', N'DATE_', N'MODULENR', N'TRCODE',
                   N'FICHEREF', N'SIGN', N'TOTAL', N'PAID', N'CANCELLED');
            """;
        var count = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        return count == 10;
    }

    private static string GetTransactionType(int moduleNumber, int transactionCode) =>
        moduleNumber switch
        {
            4 => transactionCode is 31 or 32 or 33 or 34 or 36 or 37 or 38 or 39
                ? "Satış faturası"
                : "Fatura",
            5 => "Cari hareket",
            6 => "Çek / senet",
            7 => "Banka hareketi",
            10 => "Kasa hareketi",
            _ => $"İşlem {moduleNumber}/{transactionCode}"
        };

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static string? ReadString(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal));
    }

    private static int ReadInt32(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
    }

    private static long ReadInt64(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
    }

    private static decimal ReadDecimal(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetValue(ordinal));
    }

    private static DateTime ReadDateTime(DbDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? DateTime.MinValue : Convert.ToDateTime(reader.GetValue(ordinal));
    }

    private sealed class DueBalance
    {
        public decimal Overdue { get; set; }
        public decimal NotDue { get; set; }
    }

    private sealed class ReceivablesSnapshot
    {
        public DateTime RetrievedAt { get; set; }
        public List<CustomerReceivableRowVM> Rows { get; set; } = [];
    }
}
