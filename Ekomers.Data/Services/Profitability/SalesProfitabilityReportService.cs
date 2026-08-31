using System.Data;
using System.Data.Common;
using System.Globalization;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services.Profitability;

public interface ISalesProfitabilityReportService
{
    Task<SalesProfitabilityPreviewVM> GetPreviewAsync(SalesProfitabilityFilterVM filter, CancellationToken ct);
}

public sealed class SalesProfitabilityReportService : ISalesProfitabilityReportService
{
    private const string ViewName = "dbo.VW_RPT_SATIS_KARLILIK_100";
    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logoContext;

    public SalesProfitabilityReportService(ApplicationDbContext context, LogoContext logoContext)
    {
        _context = context;
        _logoContext = logoContext;
    }

    public async Task<SalesProfitabilityPreviewVM> GetPreviewAsync(
        SalesProfitabilityFilterVM filter,
        CancellationToken ct)
    {
        var startDate = filter.StartDate.Date;
        var endDate = filter.EndDate.Date;
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var page = Math.Max(filter.Page, 1);
        var pageSize = Math.Clamp(filter.PageSize, 10, 100);
        var search = filter.Search?.Trim();
        var priceStatus = filter.PriceStatus?.Trim();
        var whereSql = BuildWhereSql(search, priceStatus);

        var connection = _logoContext.Database.GetDbConnection();
        var closeConnection = connection.State != ConnectionState.Open;
        if (closeConnection)
        {
            await connection.OpenAsync(ct);
        }

        try
        {
            var endExclusive = endDate.AddDays(1);
            var summary = await ReadSummaryAsync(
                connection, whereSql, startDate, endExclusive, search, priceStatus, ct);
            var rows = await ReadRowsAsync(
                connection, whereSql, startDate, endExclusive, search, priceStatus,
                page, pageSize, ct);
            var costQuantities = await ReadCostQuantitiesAsync(
                connection, whereSql, startDate, endExclusive, search, priceStatus, ct);
            var statuses = await ReadStatusesAsync(connection, ct);

            var materialRefs = costQuantities.Select(x => x.LogoMaterialRef)
                .Concat(rows.Select(x => x.LogoMaterialRef))
                .Where(x => x > 0)
                .Distinct()
                .ToList();
            var costVersions = await _context.RptProductCostVersions.AsNoTracking()
                .Where(x => materialRefs.Contains(x.LogoMaterialRef) &&
                            x.IsDelete != true &&
                            x.ValidFrom <= endDate &&
                            (x.ValidTo == null || x.ValidTo >= startDate))
                .OrderByDescending(x => x.ValidFrom)
                .ThenByDescending(x => x.VersionNumber)
                .ToListAsync(ct);
            var costsByMaterial = costVersions
                .GroupBy(x => x.LogoMaterialRef)
                .ToDictionary(x => x.Key, x => x.ToList());
            var costSummary = CalculateCosts(costQuantities, costsByMaterial);
            ApplyRowCosts(rows, costsByMaterial);

            return new SalesProfitabilityPreviewVM
            {
                Filter = new SalesProfitabilityFilterVM
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Search = search,
                    PriceStatus = priceStatus,
                    Page = page,
                    PageSize = pageSize
                },
                TotalCount = summary.TotalCount,
                ReferenceGrossRevenue = summary.ReferenceGrossRevenue,
                NetRevenue = summary.NetRevenue,
                DiscountAmount = summary.DiscountAmount,
                DiscountRate = summary.ReferenceGrossRevenue == 0m
                    ? 0m
                    : summary.DiscountAmount * 100m / summary.ReferenceGrossRevenue,
                MissingReferencePriceCount = summary.MissingReferencePriceCount,
                KnownCostAmount = costSummary.KnownCostAmount,
                GrossProfit = summary.ReferenceGrossRevenue - costSummary.KnownCostAmount,
                NetProfit = summary.NetRevenue - costSummary.KnownCostAmount,
                NetProfitRate = summary.NetRevenue == 0m
                    ? 0m
                    : (summary.NetRevenue - costSummary.KnownCostAmount) * 100m / summary.NetRevenue,
                MissingCostLineCount = costSummary.MissingCostLineCount,
                PageCount = summary.TotalCount == 0
                    ? 0
                    : (int)Math.Ceiling(summary.TotalCount / (decimal)pageSize),
                PriceStatuses = statuses,
                Rows = rows
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

    private static async Task<SummaryResult> ReadSummaryAsync(
        DbConnection connection,
        string whereSql,
        DateTime startDate,
        DateTime endExclusive,
        string? search,
        string? priceStatus,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                COUNT(1) AS TotalCount,
                COALESCE(SUM(CONVERT(decimal(38, 6), ReferansBrutCiroTL)), 0) AS ReferenceGrossRevenue,
                COALESCE(SUM(CONVERT(decimal(38, 6), GerceklesenNetTutarKdvHaricTL)), 0) AS NetRevenue,
                COALESCE(SUM(CONVERT(decimal(38, 6), HesaplananIskontoTutarTL)), 0) AS DiscountAmount,
                COALESCE(SUM(CASE
                    WHEN SatirTuru = N'Malzeme' AND FiyatKarsilastirmaDurumu <> N'HESAPLANABILIR'
                    THEN 1 ELSE 0 END), 0) AS MissingReferencePriceCount
            FROM {ViewName}
            {whereSql};
            """;
        AddFilterParameters(command, startDate, endExclusive, search, priceStatus);

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct))
        {
            return new SummaryResult();
        }

        return new SummaryResult
        {
            TotalCount = ReadInt32(reader, "TotalCount"),
            ReferenceGrossRevenue = ReadDecimal(reader, "ReferenceGrossRevenue"),
            NetRevenue = ReadDecimal(reader, "NetRevenue"),
            DiscountAmount = ReadDecimal(reader, "DiscountAmount"),
            MissingReferencePriceCount = ReadInt32(reader, "MissingReferencePriceCount")
        };
    }

    private static async Task<IReadOnlyList<SalesProfitabilityPreviewRowVM>> ReadRowsAsync(
        DbConnection connection,
        string whereSql,
        DateTime startDate,
        DateTime endExclusive,
        string? search,
        string? priceStatus,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                MalzemeRef,
                SatirTuru,
                FaturaTarihi,
                FaturaNo,
                IadeMi,
                CariKod,
                CariUnvan,
                MalzemeKod,
                MalzemeAdi,
                CONVERT(decimal(38, 6), Miktar) AS Miktar,
                Birim,
                ReferansFiyatBaslangicTarihi,
                CONVERT(decimal(38, 6), ReferansBirimFiyatKdvHaricTL) AS ReferansBirimFiyatKdvHaricTL,
                CONVERT(decimal(38, 6), ReferansBrutCiroTL) AS ReferansBrutCiroTL,
                CONVERT(decimal(38, 6), GerceklesenNetTutarKdvHaricTL) AS GerceklesenNetTutarKdvHaricTL,
                CONVERT(decimal(38, 6), HesaplananIskontoTutarTL) AS HesaplananIskontoTutarTL,
                CONVERT(decimal(38, 6), HesaplananIskontoOrani) AS HesaplananIskontoOrani,
                FiyatKarsilastirmaDurumu
            FROM {ViewName}
            {whereSql}
            ORDER BY FaturaTarihi DESC, FaturaRef DESC, FaturaSatirRef DESC
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
            """;
        AddFilterParameters(command, startDate, endExclusive, search, priceStatus);
        AddParameter(command, "@skip", DbType.Int32, (page - 1) * pageSize);
        AddParameter(command, "@take", DbType.Int32, pageSize);

        var rows = new List<SalesProfitabilityPreviewRowVM>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new SalesProfitabilityPreviewRowVM
            {
                LogoMaterialRef = ReadInt32(reader, "MalzemeRef"),
                LineType = ReadString(reader, "SatirTuru") ?? string.Empty,
                InvoiceDate = ReadDateTime(reader, "FaturaTarihi"),
                InvoiceNumber = ReadString(reader, "FaturaNo") ?? string.Empty,
                IsReturn = ReadInt32(reader, "IadeMi") == 1,
                CustomerCode = ReadString(reader, "CariKod") ?? string.Empty,
                CustomerName = ReadString(reader, "CariUnvan") ?? string.Empty,
                ProductCode = ReadString(reader, "MalzemeKod"),
                ProductName = ReadString(reader, "MalzemeAdi"),
                Quantity = ReadDecimal(reader, "Miktar"),
                Unit = ReadString(reader, "Birim"),
                ReferencePriceDate = ReadNullableDateTime(reader, "ReferansFiyatBaslangicTarihi"),
                ReferenceUnitPrice = ReadNullableDecimal(reader, "ReferansBirimFiyatKdvHaricTL"),
                ReferenceGrossRevenue = ReadNullableDecimal(reader, "ReferansBrutCiroTL"),
                NetRevenue = ReadDecimal(reader, "GerceklesenNetTutarKdvHaricTL"),
                DiscountAmount = ReadNullableDecimal(reader, "HesaplananIskontoTutarTL"),
                DiscountRate = ReadNullableDecimal(reader, "HesaplananIskontoOrani"),
                PriceStatus = ReadString(reader, "FiyatKarsilastirmaDurumu") ?? string.Empty
            });
        }

        return rows;
    }

    private static async Task<IReadOnlyList<CostQuantityRow>> ReadCostQuantitiesAsync(
        DbConnection connection,
        string whereSql,
        DateTime startDate,
        DateTime endExclusive,
        string? search,
        string? priceStatus,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
                MalzemeRef,
                CONVERT(date, FaturaTarihi) AS SalesDate,
                SUM(CONVERT(decimal(38, 6), Miktar)) AS Quantity,
                COUNT(1) AS LineCount
            FROM {ViewName}
            {whereSql}
              AND SatirTuru = N'Malzeme'
            GROUP BY MalzemeRef, CONVERT(date, FaturaTarihi);
            """;
        AddFilterParameters(command, startDate, endExclusive, search, priceStatus);

        var rows = new List<CostQuantityRow>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            rows.Add(new CostQuantityRow
            {
                LogoMaterialRef = ReadInt32(reader, "MalzemeRef"),
                SalesDate = ReadDateTime(reader, "SalesDate").Date,
                Quantity = ReadDecimal(reader, "Quantity"),
                LineCount = ReadInt32(reader, "LineCount")
            });
        }

        return rows;
    }

    private static CostSummaryResult CalculateCosts(
        IReadOnlyList<CostQuantityRow> quantities,
        IReadOnlyDictionary<int, List<RptProductCostVersion>> costsByMaterial)
    {
        var result = new CostSummaryResult();
        foreach (var quantity in quantities)
        {
            var cost = ResolveCost(costsByMaterial, quantity.LogoMaterialRef, quantity.SalesDate);
            if (cost == null)
            {
                result.MissingCostLineCount += quantity.LineCount;
                continue;
            }

            result.KnownCostAmount += quantity.Quantity * cost.TotalUnitCostTry;
        }

        return result;
    }

    private static void ApplyRowCosts(
        IReadOnlyList<SalesProfitabilityPreviewRowVM> rows,
        IReadOnlyDictionary<int, List<RptProductCostVersion>> costsByMaterial)
    {
        foreach (var row in rows)
        {
            if (!string.Equals(row.LineType, "Malzeme", StringComparison.OrdinalIgnoreCase))
            {
                row.CostStatus = "Maliyet dışı";
                continue;
            }

            var cost = ResolveCost(costsByMaterial, row.LogoMaterialRef, row.InvoiceDate.Date);
            if (cost == null)
            {
                row.CostStatus = "Maliyet bulunamadı";
                continue;
            }

            row.UnitCostTry = cost.TotalUnitCostTry;
            row.CostAmount = row.Quantity * cost.TotalUnitCostTry;
            row.GrossProfit = row.ReferenceGrossRevenue.HasValue
                ? row.ReferenceGrossRevenue.Value - row.CostAmount.Value
                : null;
            row.NetProfit = row.NetRevenue - row.CostAmount.Value;
            row.NetProfitRate = row.NetRevenue == 0m
                ? null
                : row.NetProfit.Value * 100m / row.NetRevenue;
            row.CostStatus = $"v{cost.VersionNumber}";
        }
    }

    private static RptProductCostVersion? ResolveCost(
        IReadOnlyDictionary<int, List<RptProductCostVersion>> costsByMaterial,
        int logoMaterialRef,
        DateTime salesDate)
    {
        if (!costsByMaterial.TryGetValue(logoMaterialRef, out var versions))
        {
            return null;
        }

        return versions.FirstOrDefault(x =>
            x.ValidFrom.Date <= salesDate &&
            (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= salesDate));
    }

    private static async Task<IReadOnlyList<string>> ReadStatusesAsync(
        DbConnection connection,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT DISTINCT FiyatKarsilastirmaDurumu
            FROM {ViewName}
            WHERE RaporaDahilMi = 1
              AND FiyatKarsilastirmaDurumu IS NOT NULL
            ORDER BY FiyatKarsilastirmaDurumu;
            """;

        var statuses = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var status = ReadString(reader, "FiyatKarsilastirmaDurumu");
            if (!string.IsNullOrWhiteSpace(status))
            {
                statuses.Add(status);
            }
        }

        return statuses;
    }

    private static string BuildWhereSql(string? search, string? priceStatus)
    {
        var conditions = new List<string>
        {
            "RaporaDahilMi = 1",
            "FaturaTarihi >= @startDate",
            "FaturaTarihi < @endExclusive"
        };

        if (!string.IsNullOrWhiteSpace(search))
        {
            conditions.Add("""
                (FaturaNo LIKE @search OR
                 CariKod LIKE @search OR
                 CariUnvan LIKE @search OR
                 MalzemeKod LIKE @search OR
                 MalzemeAdi LIKE @search)
                """);
        }

        if (!string.IsNullOrWhiteSpace(priceStatus))
        {
            conditions.Add("FiyatKarsilastirmaDurumu = @priceStatus");
        }

        return "WHERE " + string.Join(Environment.NewLine + "  AND ", conditions);
    }

    private static void AddFilterParameters(
        DbCommand command,
        DateTime startDate,
        DateTime endExclusive,
        string? search,
        string? priceStatus)
    {
        AddParameter(command, "@startDate", DbType.DateTime2, startDate);
        AddParameter(command, "@endExclusive", DbType.DateTime2, endExclusive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            AddParameter(command, "@search", DbType.String, $"%{search}%");
        }

        if (!string.IsNullOrWhiteSpace(priceStatus))
        {
            AddParameter(command, "@priceStatus", DbType.String, priceStatus);
        }
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static int ReadInt32(DbDataReader reader, string name) =>
        reader[name] is DBNull
            ? 0
            : Convert.ToInt32(reader[name], CultureInfo.InvariantCulture);

    private static decimal ReadDecimal(DbDataReader reader, string name) =>
        reader[name] is DBNull
            ? 0m
            : Convert.ToDecimal(reader[name], CultureInfo.InvariantCulture);

    private static decimal? ReadNullableDecimal(DbDataReader reader, string name) =>
        reader[name] is DBNull
            ? null
            : Convert.ToDecimal(reader[name], CultureInfo.InvariantCulture);

    private static DateTime ReadDateTime(DbDataReader reader, string name) =>
        Convert.ToDateTime(reader[name], CultureInfo.InvariantCulture);

    private static DateTime? ReadNullableDateTime(DbDataReader reader, string name) =>
        reader[name] is DBNull
            ? null
            : Convert.ToDateTime(reader[name], CultureInfo.InvariantCulture);

    private static string? ReadString(DbDataReader reader, string name) =>
        reader[name] is DBNull
            ? null
            : Convert.ToString(reader[name], CultureInfo.InvariantCulture);

    private sealed class SummaryResult
    {
        public int TotalCount { get; set; }
        public decimal ReferenceGrossRevenue { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal DiscountAmount { get; set; }
        public int MissingReferencePriceCount { get; set; }
    }

    private sealed class CostQuantityRow
    {
        public int LogoMaterialRef { get; set; }
        public DateTime SalesDate { get; set; }
        public decimal Quantity { get; set; }
        public int LineCount { get; set; }
    }

    private sealed class CostSummaryResult
    {
        public decimal KnownCostAmount { get; set; }
        public int MissingCostLineCount { get; set; }
    }
}
