using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.LogoDb;

public class LG_100_CLCARD
{
    [Key] public int LOGICALREF { get; set; }
    public short ACTIVE { get; set; }
    public string? CODE { get; set; }
    public string? DEFINITION_ { get; set; }
    public string? TAXNR { get; set; }
    public string? TAXOFFICE { get; set; }
    public string? ADDR1 { get; set; }
    public string? ADDR2 { get; set; }
    public string? TOWN { get; set; }
    public string? CITY { get; set; }
    public string? TELNRS1 { get; set; }
    public string? EMAILADDR { get; set; }
}
