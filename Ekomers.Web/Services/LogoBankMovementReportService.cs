using Dapper;
using Ekomers.Common.Services;
using Ekomers.Models.ViewModels;
using Microsoft.Data.SqlClient;

namespace Ekomers.Web.Services;

/// <summary>
/// Logo veritabanından banka havale/EFT hareketlerini salt okunur olarak getirir.
/// Bu servis Logo REST kullanmaz ve hiçbir INSERT/UPDATE/DELETE çalıştırmaz.
/// </summary>
public sealed class LogoBankMovementReportService
{
    private readonly string _connectionString;
    private readonly ILogger<LogoBankMovementReportService> _logger;

    public LogoBankMovementReportService(
        IConfiguration configuration,
        ILogger<LogoBankMovementReportService> logger)
    {
        var encrypted = configuration.GetConnectionString("LogoConnection");
        if (string.IsNullOrWhiteSpace(encrypted))
            throw new InvalidOperationException("LogoConnection bağlantı bilgisi bulunamadı.");

        _connectionString = CryptoHelper.Decrypt(encrypted);
        _logger = logger;
    }

    public async Task<List<LogoBankMovementRowVM>> GetMovementsAsync(
        IReadOnlyCollection<LogoBankMovementCompanyVM> companies,
        DateTime start,
        DateTime endExclusive,
        string direction,
        string? clientSearch = null,
        CancellationToken cancellationToken = default)
    {
        if (endExclusive < start)
            throw new ArgumentException("Rapor bitişi başlangıçtan sonra olmalıdır.");
        if (endExclusive == start)
            return [];

        var result = new List<LogoBankMovementRowVM>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        foreach (var company in companies)
        {
            if (!int.TryParse(company.FirmNumber, out var firmNumber) || firmNumber <= 0 || firmNumber > 999)
            {
                _logger.LogWarning("{Company} şirketinin Logo firma numarası geçersiz: {FirmNumber}", company.Name, company.FirmNumber);
                continue;
            }

            var periods = (await connection.QueryAsync<LogoPeriod>(new CommandDefinition(
                """
                SELECT NR, BEGDATE, ENDDATE
                FROM dbo.L_CAPIPERIOD WITH (NOLOCK)
                WHERE FIRMNR = @FirmNumber
                  AND BEGDATE < @EndExclusive
                  AND ENDDATE >= @Start
                ORDER BY NR
                """,
                new { FirmNumber = firmNumber, Start = start.Date, EndExclusive = endExclusive },
                commandTimeout: 60,
                cancellationToken: cancellationToken))).ToList();

            foreach (var period in periods)
            {
                var rows = await ReadPeriodAsync(
                    connection,
                    company.Name,
                    firmNumber,
                    period.NR,
                    start,
                    endExclusive,
                    direction,
                    clientSearch,
                    cancellationToken);
                result.AddRange(rows);
            }
        }

        return result
            .OrderByDescending(x => x.Date)
            .ThenBy(x => x.CompanyName)
            .ThenByDescending(x => x.LogicalRef)
            .ToList();
    }

    private static async Task<IEnumerable<LogoBankMovementRowVM>> ReadPeriodAsync(
        SqlConnection connection,
        string companyName,
        int firmNumber,
        int periodNumber,
        DateTime start,
        DateTime endExclusive,
        string direction,
        string? clientSearch,
        CancellationToken cancellationToken)
    {
        var firm = firmNumber.ToString("000");
        var period = periodNumber.ToString("00");
        var lineTable = $"LG_{firm}_{period}_BNFLINE";
        var accountTable = $"LG_{firm}_BANKACC";
        var bankTable = $"LG_{firm}_BNCARD";
        var clientTable = $"LG_{firm}_CLCARD";

        var lineColumns = await ReadColumnsAsync(connection, lineTable, cancellationToken);
        if (!Has(lineColumns, "LOGICALREF", "DATE_", "TRCODE", "AMOUNT"))
            return [];

        var accountColumns = await ReadColumnsAsync(connection, accountTable, cancellationToken);
        var bankColumns = await ReadColumnsAsync(connection, bankTable, cancellationToken);
        var clientColumns = await ReadColumnsAsync(connection, clientTable, cancellationToken);

        var joins = new List<string>();
        var accountJoined = lineColumns.Contains("BNACCREF") && Has(accountColumns, "LOGICALREF");
        if (accountJoined)
            joins.Add($"LEFT JOIN dbo.[{accountTable}] BA WITH (NOLOCK) ON BA.LOGICALREF = L.BNACCREF");

        var bankJoined = accountJoined && accountColumns.Contains("BANKREF") && Has(bankColumns, "LOGICALREF");
        if (bankJoined)
            joins.Add($"LEFT JOIN dbo.[{bankTable}] B WITH (NOLOCK) ON B.LOGICALREF = BA.BANKREF");

        var clientJoined = lineColumns.Contains("CLIENTREF") && Has(clientColumns, "LOGICALREF");
        if (clientJoined)
            joins.Add($"LEFT JOIN dbo.[{clientTable}] C WITH (NOLOCK) ON C.LOGICALREF = L.CLIENTREF");

        string Line(string column, string fallback) => lineColumns.Contains(column) ? $"L.[{column}]" : fallback;
        string Account(string column) => accountJoined && accountColumns.Contains(column) ? $"BA.[{column}]" : "N''";
        string Bank(string column) => bankJoined && bankColumns.Contains(column) ? $"B.[{column}]" : "N''";
        string Client(string column) => clientJoined && clientColumns.Contains(column) ? $"C.[{column}]" : "N''";

        var transactionCurrency = Line("TRCURR", "0");
        var transactionAmount = lineColumns.Contains("TRNET")
            ? $"CASE WHEN {transactionCurrency} IN (0, 160) THEN L.AMOUNT WHEN ISNULL(L.TRNET, 0) <> 0 THEN ABS(L.TRNET) ELSE L.AMOUNT END"
            : "L.AMOUNT";

        var directionFilter = direction.ToLowerInvariant() switch
        {
            "incoming" or "gelen" => "AND L.TRCODE = 3",
            "outgoing" or "giden" => "AND L.TRCODE = 4",
            _ => string.Empty
        };
        var cancelledFilter = lineColumns.Contains("CANCELLED") ? "AND ISNULL(L.CANCELLED, 0) = 0" : string.Empty;
        var clientSearchColumns = new List<string>();
        if (clientJoined && clientColumns.Contains("CODE"))
            clientSearchColumns.Add("CHARINDEX(@ClientSearch, CONVERT(nvarchar(250), C.[CODE])) > 0");
        if (clientJoined && clientColumns.Contains("DEFINITION_"))
            clientSearchColumns.Add("CHARINDEX(@ClientSearch, CONVERT(nvarchar(250), C.[DEFINITION_])) > 0");
        var clientFilter = string.IsNullOrWhiteSpace(clientSearch)
            ? string.Empty
            : clientSearchColumns.Count > 0
                ? $"AND ({string.Join(" OR ", clientSearchColumns)})"
                : "AND 1 = 0";

        var sql = $"""
            SELECT
                @CompanyName AS CompanyName,
                @FirmNumber AS FirmNumber,
                @PeriodNumber AS PeriodNumber,
                CONVERT(bigint, L.LOGICALREF) AS LogicalRef,
                L.DATE_ AS [Date],
                CASE L.TRCODE WHEN 3 THEN N'Gelen' WHEN 4 THEN N'Giden' ELSE N'Diğer' END AS Direction,
                CONVERT(smallint, L.TRCODE) AS TransactionCode,
                CONVERT(nvarchar(100), ISNULL({Line("TRANNO", "N''")}, N'')) AS TransactionNumber,
                CONVERT(nvarchar(100), ISNULL({Line("DOCODE", "N''")}, N'')) AS DocumentNumber,
                CONVERT(nvarchar(500), ISNULL({Line("LINEEXP", "N''")}, N'')) AS Description,
                CONVERT(nvarchar(100), ISNULL({Bank("CODE")}, N'')) AS BankCode,
                CONVERT(nvarchar(250), ISNULL({Bank("DEFINITION_")}, N'')) AS BankName,
                CONVERT(nvarchar(100), ISNULL({Account("CODE")}, N'')) AS AccountCode,
                CONVERT(nvarchar(250), ISNULL({Account("DEFINITION_")}, N'')) AS AccountName,
                CONVERT(nvarchar(100), ISNULL({Account("ACCOUNTNO")}, N'')) AS AccountNumber,
                CONVERT(nvarchar(100), ISNULL({Client("CODE")}, N'')) AS ClientCode,
                CONVERT(nvarchar(250), ISNULL({Client("DEFINITION_")}, N'')) AS ClientName,
                CONVERT(decimal(18,2), ABS(ISNULL(L.AMOUNT, 0))) AS LocalAmount,
                CONVERT(int, ISNULL({transactionCurrency}, 0)) AS CurrencyType,
                CASE CONVERT(int, ISNULL({transactionCurrency}, 0))
                    WHEN 0 THEN 'TRY' WHEN 160 THEN 'TRY' WHEN 1 THEN 'USD'
                    WHEN 20 THEN 'EUR' WHEN 17 THEN 'GBP'
                    ELSE CONVERT(varchar(10), CONVERT(int, ISNULL({transactionCurrency}, 0)))
                END AS CurrencyCode,
                CONVERT(decimal(18,2), ABS(ISNULL({transactionAmount}, 0))) AS TransactionAmount,
                CONVERT(decimal(18,6), ISNULL(NULLIF({Line("TRRATE", "1")}, 0), 1)) AS ExchangeRate
            FROM dbo.[{lineTable}] L WITH (NOLOCK)
            {string.Join(Environment.NewLine, joins)}
            WHERE L.DATE_ >= @Start
              AND L.DATE_ < @EndExclusive
              AND L.TRCODE IN (3, 4)
              {cancelledFilter}
              {directionFilter}
              {clientFilter}
            ORDER BY L.DATE_ DESC, L.LOGICALREF DESC
            """;

        return await connection.QueryAsync<LogoBankMovementRowVM>(new CommandDefinition(
            sql,
            new { CompanyName = companyName, FirmNumber = firmNumber, PeriodNumber = periodNumber, Start = start, EndExclusive = endExclusive, ClientSearch = clientSearch },
            commandTimeout: 120,
            cancellationToken: cancellationToken));
    }

    private static async Task<HashSet<string>> ReadColumnsAsync(
        SqlConnection connection,
        string tableName,
        CancellationToken cancellationToken)
    {
        var columns = await connection.QueryAsync<string>(new CommandDefinition(
            """
            SELECT C.name
            FROM sys.tables T WITH (NOLOCK)
            INNER JOIN sys.schemas S WITH (NOLOCK) ON S.schema_id = T.schema_id
            INNER JOIN sys.columns C WITH (NOLOCK) ON C.object_id = T.object_id
            WHERE S.name = N'dbo' AND T.name = @TableName
            """,
            new { TableName = tableName },
            commandTimeout: 30,
            cancellationToken: cancellationToken));

        return columns.ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool Has(HashSet<string> columns, params string[] names) => names.All(columns.Contains);

    private sealed class LogoPeriod
    {
        public int NR { get; set; }
        public DateTime BEGDATE { get; set; }
        public DateTime ENDDATE { get; set; }
    }
}
