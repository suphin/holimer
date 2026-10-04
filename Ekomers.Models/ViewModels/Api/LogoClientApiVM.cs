namespace Ekomers.Models.ViewModels.Api;

public sealed class LogoClientApiVM
{
    public int FirmNumber { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public int LogicalRef { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int CardType { get; set; }
    public string TaxNumber { get; set; } = string.Empty;
    public string TaxOffice { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string Address2 { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Phone1 { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
