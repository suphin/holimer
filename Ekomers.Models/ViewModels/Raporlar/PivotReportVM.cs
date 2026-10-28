using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Raporlar;

public sealed class PivotColumnVM
{
    public string Name { get; set; } = "";
    public string DataType { get; set; } = "";
    public bool Numeric { get; set; }
    public bool Date { get; set; }
}
public sealed class PivotMeasureVM
{
    public string Field { get; set; } = "";
    public string Function { get; set; } = "sum";
}
public sealed class PivotFilterVM
{
    public string Field { get; set; } = "";
    public string Operator { get; set; } = "eq";
    public string Value { get; set; } = "";
}
public sealed class PivotLayoutVM
{
    public List<string> Rows { get; set; } = [];
    public string? Column { get; set; }
    public List<PivotMeasureVM> Measures { get; set; } = [];
    public List<PivotFilterVM> Filters { get; set; } = [];
}
public sealed class PivotSettingsVM
{
    public List<string> AllowedFields { get; set; } = [];
    public PivotLayoutVM Layout { get; set; } = new();
}
public sealed class PivotEditVM
{
    public int Id { get; set; }
    [Required, StringLength(150)] public string Name { get; set; } = "Fatura Dökümü";
    [Required, StringLength(128)] public string SchemaName { get; set; } = "dbo";
    [Required, StringLength(128)] public string ViewName { get; set; } = "fatura_dokumu_100";
    public bool IsActive { get; set; }
    // New reports have no database rowversion yet. Updates validate the token in the controller.
    public string? Version { get; set; }
    [Required, StringLength(30000)] public string SettingsJson { get; set; } = "{}";
}
public sealed class PivotPageVM
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Source { get; set; } = "";
    public string? Error { get; set; }
    public List<PivotColumnVM> Columns { get; set; } = [];
    public PivotSettingsVM Settings { get; set; } = new();
}
public sealed class PivotCellVM
{
    public List<string?> Row { get; set; } = [];
    public string? Column { get; set; }
    public List<decimal?> Values { get; set; } = [];
}
public sealed class PivotResultVM
{
    public List<PivotCellVM> Cells { get; set; } = [];
    public List<PivotCellVM> RowTotals { get; set; } = [];
    public List<PivotCellVM> ColumnTotals { get; set; } = [];
    public List<decimal?> GrandTotal { get; set; } = [];
    public List<string> Headings { get; set; } = [];
    public long SourceRowCount { get; set; }
}
