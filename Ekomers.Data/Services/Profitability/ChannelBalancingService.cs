using System.Data;
using System.Data.Common;
using System.Globalization;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Ekomers.Data.Services.Profitability;

public interface IChannelBalancingService
{
    Task<ChannelBalancingVM> GetAsync(SalesProfitabilityFilterVM filter, CancellationToken ct);
}

public sealed class ChannelBalancingService : IChannelBalancingService
{
    private const string ViewName = "dbo.VW_RPT_SATIS_KARLILIK_100";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);
    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logoContext;
    private readonly IMemoryCache _cache;

    public ChannelBalancingService(
        ApplicationDbContext context,
        LogoContext logoContext,
        IMemoryCache cache)
    {
        _context = context;
        _logoContext = logoContext;
        _cache = cache;
    }

    public async Task<ChannelBalancingVM> GetAsync(
        SalesProfitabilityFilterVM filter,
        CancellationToken ct)
    {
        var startDate = filter.StartDate.Date;
        var endDate = filter.EndDate.Date;
        if (endDate < startDate)
        {
            (startDate, endDate) = (endDate, startDate);
        }

        var search = filter.Search?.Trim();
        var priceStatus = filter.PriceStatus?.Trim();
        var cacheKey = string.Join('|',
            "sales-profitability:channel-balance:v1",
            startDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            endDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            search?.ToUpperInvariant() ?? string.Empty,
            priceStatus?.ToUpperInvariant() ?? string.Empty);

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await CalculateAsync(startDate, endDate, search, priceStatus, ct);
            }) ?? new ChannelBalancingVM
        {
            StartDate = startDate,
            EndDate = endDate,
            Message = "Kanal dengeleme hesabı oluşturulamadı."
        };
    }

    private async Task<ChannelBalancingVM> CalculateAsync(
        DateTime startDate,
        DateTime endDate,
        string? search,
        string? priceStatus,
        CancellationToken ct)
    {
        var prefixes = await _context.RptProductScopePrefixes.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive == true)
            .OrderBy(x => x.Prefix)
            .Select(x => x.Prefix)
            .ToListAsync(ct);

        if (prefixes.Count == 0)
        {
            return new ChannelBalancingVM
            {
                StartDate = startDate,
                EndDate = endDate,
                RetrievedAt = DateTime.Now,
                Message = "Kanal hesabına dahil edilecek etkin ürün ön eki bulunamadı."
            };
        }

        var salesRows = await ReadSalesRowsAsync(
            startDate, endDate.AddDays(1), search, priceStatus, prefixes, ct);
        if (salesRows.Count == 0)
        {
            return new ChannelBalancingVM
            {
                StartDate = startDate,
                EndDate = endDate,
                RetrievedAt = DateTime.Now,
                ProductPrefixes = prefixes,
                Message = "Seçilen filtrelerde kanal dengelemesine uygun ürün satışı bulunamadı."
            };
        }

        var materialRefs = salesRows.Select(x => x.LogoMaterialRef)
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

        foreach (var row in salesRows)
        {
            var cost = ResolveCost(costsByMaterial, row.LogoMaterialRef, row.SalesDate);
            if (cost == null)
            {
                row.MissingCostLineCount = row.LineCount;
            }
            else
            {
                row.KnownCostAmount = row.Quantity * cost.TotalUnitCostTry;
            }
        }

        var channelTotals = salesRows
            .GroupBy(x => Normalize(x.Channel))
            .Select(group =>
            {
                var units = group.Select(x => NormalizeUnit(x.Unit))
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var quantity = group.Sum(x => x.Quantity);
                var netRevenue = group.Sum(x => x.NetRevenue);
                var knownCost = group.Sum(x => x.KnownCostAmount);
                return new ChannelBalancingRowVM
                {
                    Channel = group.Key,
                    Quantity = quantity,
                    Unit = units.Count switch { 0 => null, 1 => units[0], _ => "Karma" },
                    NetRevenue = netRevenue,
                    AverageNetRevenuePerUnit = quantity > 0m ? netRevenue / quantity : 0m,
                    KnownCostAmount = knownCost,
                    NetProfit = netRevenue - knownCost,
                    NetProfitRate = netRevenue == 0m ? 0m : (netRevenue - knownCost) * 100m / netRevenue,
                    MissingCostLineCount = group.Sum(x => x.MissingCostLineCount)
                };
            })
            .OrderByDescending(x => x.NetRevenue)
            .ToList();

        var target = channelTotals.First();
        foreach (var row in channelTotals)
        {
            row.IsTarget = string.Equals(row.Channel, target.Channel, StringComparison.OrdinalIgnoreCase);
            row.RevenueGap = Math.Max(target.NetRevenue - row.NetRevenue, 0m);
            row.CompletionRate = target.NetRevenue == 0m
                ? 0m
                : row.NetRevenue * 100m / target.NetRevenue;
            row.RequiredAdditionalQuantity = row.RevenueGap == 0m
                ? 0m
                : row.Unit == "Karma" || row.AverageNetRevenuePerUnit <= 0m
                    ? null
                    : Math.Ceiling(row.RevenueGap / row.AverageNetRevenuePerUnit);
        }

        var hasDefinedChannels = channelTotals.Count(x =>
            !string.Equals(x.Channel, "Tanımsız", StringComparison.OrdinalIgnoreCase)) > 1;

        return new ChannelBalancingVM
        {
            StartDate = startDate,
            EndDate = endDate,
            RetrievedAt = DateTime.Now,
            TargetChannel = target.Channel,
            TargetNetRevenue = target.NetRevenue,
            HasDefinedChannels = hasDefinedChannels,
            ProductPrefixes = prefixes,
            Rows = channelTotals,
            Message = hasDefinedChannels
                ? null
                : "Karşılaştırma için en az iki tanımlı satış kanalı gerekir. Logo rapor görünümündeki satış kanalı alanını kontrol edin."
        };
    }

    private async Task<List<ChannelSalesSourceRow>> ReadSalesRowsAsync(
        DateTime startDate,
        DateTime endExclusive,
        string? search,
        string? priceStatus,
        IReadOnlyList<string> prefixes,
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
            var columns = await ReadAvailableColumnsAsync(connection, ct);
            var channelExpression = BuildTextColumnExpression(
                columns,
                "SatisKanali", "Satış Kanalı", "CariOzelKod", "Cari Özel Kod", "Kanal");
            var whereSql = BuildWhereSql(search, priceStatus, prefixes.Count);

            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT
                    {channelExpression} AS ChannelCode,
                    MalzemeRef,
                    MalzemeKod,
                    Birim,
                    CONVERT(date, FaturaTarihi) AS SalesDate,
                    COUNT(1) AS LineCount,
                    COALESCE(SUM(CONVERT(decimal(38, 6), Miktar)), 0) AS Quantity,
                    COALESCE(SUM(CONVERT(decimal(38, 6), GerceklesenNetTutarKdvHaricTL)), 0) AS NetRevenue
                FROM {ViewName}
                {whereSql}
                GROUP BY
                    {channelExpression},
                    MalzemeRef,
                    MalzemeKod,
                    Birim,
                    CONVERT(date, FaturaTarihi);
                """;
            AddParameter(command, "@startDate", DbType.DateTime2, startDate);
            AddParameter(command, "@endDate", DbType.DateTime2, endExclusive);
            if (!string.IsNullOrWhiteSpace(search))
            {
                AddParameter(command, "@search", DbType.String, $"%{search}%");
            }
            if (!string.IsNullOrWhiteSpace(priceStatus))
            {
                AddParameter(command, "@priceStatus", DbType.String, priceStatus);
            }
            for (var index = 0; index < prefixes.Count; index++)
            {
                AddParameter(command, $"@prefix{index}", DbType.String, $"{prefixes[index]}%");
            }

            var rows = new List<ChannelSalesSourceRow>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                rows.Add(new ChannelSalesSourceRow
                {
                    Channel = ReadString(reader, "ChannelCode") ?? "Tanımsız",
                    LogoMaterialRef = ReadInt32(reader, "MalzemeRef"),
                    ProductCode = ReadString(reader, "MalzemeKod") ?? string.Empty,
                    Unit = ReadString(reader, "Birim"),
                    SalesDate = ReadDateTime(reader, "SalesDate").Date,
                    LineCount = ReadInt32(reader, "LineCount"),
                    Quantity = ReadDecimal(reader, "Quantity"),
                    NetRevenue = ReadDecimal(reader, "NetRevenue")
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

    private static string BuildWhereSql(string? search, string? priceStatus, int prefixCount)
    {
        var parts = new List<string>
        {
            "FaturaTarihi >= @startDate",
            "FaturaTarihi < @endDate",
            "SatirTuru = N'Malzeme'"
        };
        if (!string.IsNullOrWhiteSpace(search))
        {
            parts.Add("(FaturaNo LIKE @search OR CariKod LIKE @search OR CariUnvan LIKE @search OR MalzemeKod LIKE @search OR MalzemeAdi LIKE @search)");
        }
        if (!string.IsNullOrWhiteSpace(priceStatus))
        {
            parts.Add("FiyatKarsilastirmaDurumu = @priceStatus");
        }
        parts.Add("(" + string.Join(" OR ", Enumerable.Range(0, prefixCount).Select(x => $"MalzemeKod LIKE @prefix{x}")) + ")");
        return "WHERE " + string.Join(Environment.NewLine + "  AND ", parts);
    }

    private static async Task<HashSet<string>> ReadAvailableColumnsAsync(
        DbConnection connection,
        CancellationToken ct)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = N'dbo'
              AND TABLE_NAME = N'VW_RPT_SATIS_KARLILIK_100';
            """;
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var column = ReadString(reader, "COLUMN_NAME");
            if (!string.IsNullOrWhiteSpace(column))
            {
                columns.Add(column);
            }
        }
        return columns;
    }

    private static string BuildTextColumnExpression(
        IReadOnlySet<string> columns,
        params string[] candidates)
    {
        var column = candidates.FirstOrDefault(columns.Contains);
        if (column == null)
        {
            return "N'Tanımsız'";
        }
        var safeColumn = column.Replace("]", "]]", StringComparison.Ordinal);
        return $"COALESCE(NULLIF(LTRIM(RTRIM(CONVERT(nvarchar(250), [{safeColumn}]))), N''), N'Tanımsız')";
    }

    private static RptProductCostVersion? ResolveCost(
        IReadOnlyDictionary<int, List<RptProductCostVersion>> costsByMaterial,
        int materialRef,
        DateTime date)
    {
        if (!costsByMaterial.TryGetValue(materialRef, out var versions))
        {
            return null;
        }
        return versions.FirstOrDefault(x =>
            x.ValidFrom.Date <= date.Date &&
            (!x.ValidTo.HasValue || x.ValidTo.Value.Date >= date.Date));
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "Tanımsız" : value.Trim();

    private static string? NormalizeUnit(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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

    private sealed class ChannelSalesSourceRow
    {
        public string Channel { get; set; } = string.Empty;
        public int LogoMaterialRef { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public DateTime SalesDate { get; set; }
        public int LineCount { get; set; }
        public decimal Quantity { get; set; }
        public decimal NetRevenue { get; set; }
        public decimal KnownCostAmount { get; set; }
        public int MissingCostLineCount { get; set; }
    }
}
