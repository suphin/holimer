using Ekomers.Models.Enums;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionCatalogItemVM
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdMaterialSource Source { get; set; }
    public PrdMaterialType Type { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool LogoActive { get; set; }
    public DateTime? LogoLastSyncDate { get; set; }
}
