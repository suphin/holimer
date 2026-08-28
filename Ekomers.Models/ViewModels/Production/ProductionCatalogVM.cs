using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionCatalogIndexVM
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
    public List<ProductionCatalogItemVM> Items { get; set; } = [];
}

public sealed class ProductionCatalogItemVM
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdMaterialSource Source { get; set; }
    public PrdMaterialType Type { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool LogoActive { get; set; }
    public DateTime? LogoLastSyncDate { get; set; }
}

public sealed class ProductionCatalogEditVM
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public PrdMaterialSource Source { get; set; }
    [Required, Display(Name = "Malzeme Adı")] public string Name { get; set; } = string.Empty;
    [Display(Name = "Malzeme Türü")] public PrdMaterialType Type { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Birim")] public int UnitId { get; set; }
    [Display(Name = "Açıklama")] public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<SelectListItem> Units { get; set; } = [];
}
