using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ekomers.Models.Entity;

[Table("RptPivotReport")]
public sealed class RptPivotReport
{
    [Key] public int ID { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    [MaxLength(128)] public string SchemaName { get; set; } = "dbo";
    [MaxLength(128)] public string ViewName { get; set; } = "fatura_dokumu_100";
    public string SettingsJson { get; set; } = "{}";
    public bool IsActive { get; set; }
    public DateTime UpdatedAt { get; set; }
    [MaxLength(256)] public string UpdatedBy { get; set; } = "";
    [Timestamp] public byte[] Version { get; set; } = [];
}
