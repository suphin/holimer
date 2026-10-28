using System.Data;
using System.Globalization;
using System.Text.Json;
using Dapper;
using Ekomers.Models.ViewModels.Raporlar;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Data.Services.Reports;

public interface ILogoPivotService
{
    Task<List<PivotColumnVM>> ColumnsAsync(string schema, string view, CancellationToken ct);
    Task<PivotResultVM> RunAsync(string schema, string view, PivotSettingsVM settings, PivotLayoutVM layout, CancellationToken ct);
}

/// <summary>Only SELECTs. SQL identifiers must exactly match metadata from sys.views / sys.columns.</summary>
public sealed class LogoPivotService(LogoContext logo) : ILogoPivotService
{
    private static readonly HashSet<string> NumericTypes = new(StringComparer.OrdinalIgnoreCase) { "tinyint", "smallint", "int", "bigint", "decimal", "numeric", "money", "smallmoney", "float", "real" };
    private static readonly HashSet<string> DateTypes = new(StringComparer.OrdinalIgnoreCase) { "date", "datetime", "datetime2", "smalldatetime" };
    private static readonly HashSet<string> TextTypes = new(StringComparer.OrdinalIgnoreCase) { "char", "varchar", "nchar", "nvarchar", "bit", "uniqueidentifier" };
    private static string Quote(string value) => "[" + value.Replace("]", "]]") + "]";
    private static CommandDefinition Command(string sql, object? args, CancellationToken ct) => new(sql, args, commandTimeout: 60, cancellationToken: ct);
    public async Task<List<PivotColumnVM>> ColumnsAsync(string schema, string view, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(schema) || schema.Length > 128 || string.IsNullOrWhiteSpace(view) || view.Length > 128)
            throw new ArgumentException("Geçerli şema ve view adı giriniz.");
        var connection = logo.Database.GetDbConnection();
        var columns = (await connection.QueryAsync<PivotColumnVM>(Command("""
            SELECT c.name AS Name, TYPE_NAME(c.system_type_id) AS DataType
            FROM sys.views v JOIN sys.schemas s ON s.schema_id=v.schema_id
            JOIN sys.columns c ON c.object_id=v.object_id
            WHERE s.name=@schema AND v.name=@view ORDER BY c.column_id
            """, new { schema, view }, ct))).ToList();
        if (columns.Count == 0) throw new ArgumentException("View bulunamadı veya LogoConnection kullanıcısının okuma/metadata yetkisi yok.");
        return columns.Where(c => NumericTypes.Contains(c.DataType) || DateTypes.Contains(c.DataType) || TextTypes.Contains(c.DataType))
            .Select(c => new PivotColumnVM { Name = c.Name, DataType = c.DataType, Numeric = NumericTypes.Contains(c.DataType), Date = DateTypes.Contains(c.DataType) }).ToList();
    }

    public static List<PivotColumnVM> Validate(PivotSettingsVM settings, PivotLayoutVM layout, List<PivotColumnVM> columns)
    {
        if (settings?.AllowedFields == null || settings.AllowedFields.Count is < 1 or > 100 || settings.AllowedFields.Distinct(StringComparer.Ordinal).Count() != settings.AllowedFields.Count)
            throw new ArgumentException("Kullanılabilir alanlardan 1–100 farklı sütun seçiniz.");
        var map = columns.ToDictionary(c => c.Name, StringComparer.Ordinal);
        if (settings.AllowedFields.Any(f => f == null || !map.ContainsKey(f))) throw new ArgumentException("Kaynak view değişmiş; alan eşleştirmelerini yönetici ekranında güncelleyiniz.");
        if (layout?.Rows == null || layout.Measures == null || layout.Filters == null || layout.Rows.Count > 3 || layout.Measures.Count is < 1 or > 3 || layout.Filters.Count > 5)
            throw new ArgumentException("En fazla 3 satır alanı, 1 sütun alanı, 3 değer ve 5 filtre seçiniz.");
        PivotColumnVM Field(string? name) => name != null && settings.AllowedFields.Contains(name, StringComparer.Ordinal) && map.TryGetValue(name, out var c)
            ? c : throw new ArgumentException("Tanımlı rapor alanlarından seçim yapınız.");
        var dimensions = layout.Rows.Concat(string.IsNullOrEmpty(layout.Column) ? [] : new[] { layout.Column }).ToList();
        if (dimensions.Distinct(StringComparer.Ordinal).Count() != dimensions.Count) throw new ArgumentException("Aynı alan satır ve sütun bölümlerinde tekrarlanamaz.");
        foreach (var d in dimensions) Field(d);
        if (layout.Measures.Any(m => m == null)) throw new ArgumentException("Değer seçimi geçersiz.");
        foreach (var m in layout.Measures)
            if (!new[] { "sum", "count", "avg", "min", "max" }.Contains(m.Function) || (m.Function != "count" && !Field(m.Field).Numeric))
                throw new ArgumentException("Toplam, ortalama, en az ve en çok için sayısal alan seçiniz.");
            else Field(m.Field);
        foreach (var f in layout.Filters)
        {
            if (f == null || f.Value == null || f.Value.Length > 250 || !new[] { "eq", "contains", "gte", "lte" }.Contains(f.Operator)) throw new ArgumentException("Filtre geçersiz veya 250 karakteri aşıyor.");
            var c = Field(f.Field);
            if (f.Operator == "contains" && !new[] { "char", "varchar", "nchar", "nvarchar" }.Contains(c.DataType)) throw new ArgumentException("İçerir filtresi yalnızca metin alanlarında kullanılabilir.");
            FilterValue(c, f.Value);
        }
        return settings.AllowedFields.Select(f => map[f]).ToList();
    }

    private static object FilterValue(PivotColumnVM c, string value)
    {
        if (c.Date) return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) && d.Year >= 1753
            ? d : throw new ArgumentException("Tarih filtrelerini yyyy-AA-gg biçiminde giriniz (en erken 1753).");
        if (c.Numeric) return decimal.TryParse(value, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n)
            ? n : throw new ArgumentException("Sayısal filtrede binlik ayırıcı kullanmayın; ondalık için nokta kullanın.");
        if (c.DataType == "uniqueidentifier") return Guid.TryParse(value, out var g) ? g : throw new ArgumentException("Geçerli GUID giriniz.");
        if (c.DataType == "bit") return value == "1" ? true : value == "0" ? false : throw new ArgumentException("Mantıksal alan için 0 veya 1 giriniz.");
        return value;
    }

    public async Task<PivotResultVM> RunAsync(string schema, string view, PivotSettingsVM settings, PivotLayoutVM layout, CancellationToken ct)
    {
        var all = await ColumnsAsync(schema, view, ct);
        var allowed = Validate(settings, layout, all).ToDictionary(c => c.Name, StringComparer.Ordinal);
        var dimensions = layout.Rows.Concat(string.IsNullOrEmpty(layout.Column) ? [] : new[] { layout.Column! }).ToList();
        string Dimension(string f) => allowed[f].Date ? $"CONVERT(date,{Quote(f)})" : Quote(f);
        var selects = dimensions.Select((d, i) => $"{Dimension(d)} AS [D{i}]").ToList();
        selects.Add("COUNT_BIG(*) AS [Records]");
        foreach (var (m, i) in layout.Measures.Select((m, i) => (m, i)))
        {
            var field = Quote(m.Field);
            selects.Add($"COUNT_BIG({field}) AS [N{i}]");
            if (m.Function != "count")
            {
                var numeric = $"CONVERT(decimal(28,6),{field})";
                selects.Add($"SUM({numeric}) AS [S{i}]");
                selects.Add($"MIN({numeric}) AS [L{i}]");
                selects.Add($"MAX({numeric}) AS [H{i}]");
            }
        }
        var parameters = new DynamicParameters(); var filters = new List<string>();
        var equalityAlternatives = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (f, i) in layout.Filters.Select((f, i) => (f, i)))
        {
            var c = allowed[f.Field]; var expr = c.Date ? $"CONVERT(date,{Quote(f.Field)})" : Quote(f.Field);
            var op = f.Operator switch { "eq" => "=", "gte" => ">=", "lte" => "<=", "contains" => "contains", _ => throw new ArgumentException("Geçersiz filtre.") };
            var predicate = op == "contains" ? $"CHARINDEX(@p{i},{expr})>0" : $"{expr}{op}@p{i}";
            // Multiple equalities on one field are alternatives (e.g. retail OR wholesale).
            // Ranges and other conditions remain conjunctive, including on that same field.
            if (f.Operator == "eq")
            {
                if (!equalityAlternatives.TryGetValue(f.Field, out var alternatives))
                    equalityAlternatives[f.Field] = alternatives = [];
                alternatives.Add(predicate);
            }
            else filters.Add(predicate);
            parameters.Add($"p{i}", FilterValue(c, f.Value));
        }
        filters.AddRange(equalityAlternatives.Values.Select(alternatives => "(" + string.Join(" OR ", alternatives) + ")"));
        var sql = $"SELECT TOP (5001) {string.Join(",", selects)} FROM {Quote(schema)}.{Quote(view)}" +
            (filters.Count > 0 ? " WHERE " + string.Join(" AND ", filters) : "") +
            (dimensions.Count > 0 ? " GROUP BY " + string.Join(",", dimensions.Select(Dimension)) : "");
        var data = (await logo.Database.GetDbConnection().QueryAsync(Command(sql, parameters, ct))).Cast<IDictionary<string, object>>().ToList();
        if (data.Count > 5000) throw new ArgumentException("Sonuç 5.000 pivot hücresini aşıyor. Filtreleri daraltınız; eksik toplam gösterilmedi.");
        return BuildResult(data, layout);
    }

    public static PivotResultVM BuildResult(List<IDictionary<string, object>> data, PivotLayoutVM layout)
    {
        var result = new PivotResultVM { Headings = layout.Measures.Select(m => $"{m.Field} · " + (m.Function switch { "sum" => "Toplam", "count" => "Dolu kayıt sayısı", "avg" => "Ortalama", "min" => "En az", _ => "En çok" })).ToList() };
        var rows = new Dictionary<string, (List<string?> Label, Accumulator[] Values)>();
        var cols = new Dictionary<string, (string? Label, Accumulator[] Values)>();
        Accumulator[] Empty() => layout.Measures.Select(_ => new Accumulator()).ToArray();
        var grand = Empty();
        static string? Label(object? o) => o is null or DBNull ? null : o is DateTime d ? d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : Convert.ToString(o, CultureInfo.InvariantCulture);
        foreach (var record in data)
        {
            long count = Convert.ToInt64(record["Records"]); if (count == 0) continue;
            result.SourceRowCount = checked(result.SourceRowCount + count);
            var row = Enumerable.Range(0, layout.Rows.Count).Select(i => Label(record[$"D{i}"])).ToList();
            var col = string.IsNullOrEmpty(layout.Column) ? null : Label(record[$"D{layout.Rows.Count}"]);
            if (row.Any(x => x?.Length > 500) || col?.Length > 500) throw new ArgumentException("Gruplama etiketi çok uzun. Başka bir alan seçiniz.");
            var rk = JsonSerializer.Serialize(row); var ck = JsonSerializer.Serialize(col);
            if (!rows.ContainsKey(rk)) rows[rk] = (row, Empty());
            if (!cols.ContainsKey(ck)) cols[ck] = (col, Empty());
            if (rows.Count > 500 || cols.Count > 60 || (long)rows.Count * cols.Count * layout.Measures.Count > 20000)
                throw new ArgumentException("Görünüm sınırı aşıldı (500 satır / 60 sütun grubu / 20.000 değer). Filtreleri daraltınız; eksik toplam gösterilmedi.");
            var cell = Empty();
            for (var i = 0; i < cell.Length; i++)
            {
                var a = new Accumulator { Count = Convert.ToInt64(record[$"N{i}"]) };
                if (layout.Measures[i].Function != "count" && a.Count > 0)
                { a.Sum = Convert.ToDecimal(record[$"S{i}"]); a.Min = Convert.ToDecimal(record[$"L{i}"]); a.Max = Convert.ToDecimal(record[$"H{i}"]); }
                cell[i].Add(a); rows[rk].Values[i].Add(a); cols[ck].Values[i].Add(a); grand[i].Add(a);
            }
            result.Cells.Add(new() { Row = row, Column = col, Values = cell.Select((a, i) => a.Value(layout.Measures[i].Function)).ToList() });
        }
        result.RowTotals = rows.Values.Select(r => new PivotCellVM { Row = r.Label, Values = r.Values.Select((a, i) => a.Value(layout.Measures[i].Function)).ToList() }).ToList();
        result.ColumnTotals = cols.Values.Select(c => new PivotCellVM { Column = c.Label, Values = c.Values.Select((a, i) => a.Value(layout.Measures[i].Function)).ToList() }).ToList();
        result.GrandTotal = grand.Select((a, i) => a.Value(layout.Measures[i].Function)).ToList();
        return result;
    }
    private sealed class Accumulator
    {
        public long Count; public decimal Sum; public decimal? Min, Max;
        public void Add(Accumulator a) { Count = checked(Count + a.Count); Sum = checked(Sum + a.Sum); if (a.Min.HasValue) Min = Min.HasValue ? Math.Min(Min.Value, a.Min.Value) : a.Min; if (a.Max.HasValue) Max = Max.HasValue ? Math.Max(Max.Value, a.Max.Value) : a.Max; }
        public decimal? Value(string f) => f == "count" ? Count : Count == 0 ? null : f switch { "sum" => Sum, "avg" => Sum / Count, "min" => Min, "max" => Max, _ => throw new ArgumentException("Geçersiz hesaplama.") };
    }
}
