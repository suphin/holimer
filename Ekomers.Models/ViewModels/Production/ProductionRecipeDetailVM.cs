using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionRecipeDetailVM
{
    public int RecipeId { get; set; }
    public int RecipeVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public decimal BaseQuantity { get; set; }
    public int UnitId { get; set; }
    public PrdRecipeStatus Status { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public bool CanEdit => Status == PrdRecipeStatus.Draft;
    public List<SelectListItem> Units { get; set; } = [];
    public List<SelectListItem> Materials { get; set; } = [];
    public List<ProductionRecipeItemVM> Items { get; set; } = [];
}

public sealed class ProductionRecipeItemVM
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public PrdMaterialType MaterialType { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal PlannedWasteRate { get; set; }
    public bool IsRequired { get; set; }
    public string? AlternativeGroupCode { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductionRecipeVersionUpdateVM
{
    public int RecipeVersionId { get; set; }
    [Required] public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BaseQuantity { get; set; }
    public int UnitId { get; set; }
    public PrdRecipeStatus Status { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string? Notes { get; set; }
}

public sealed class ProductionRecipeItemEditVM
{
    public int RecipeVersionId { get; set; }
    public int ItemId { get; set; }
    public int MaterialId { get; set; }
    public decimal Quantity { get; set; }
    public int UnitId { get; set; }
    public decimal PlannedWasteRate { get; set; }
    public bool IsRequired { get; set; } = true;
    public string? AlternativeGroupCode { get; set; }
    public string? Notes { get; set; }
}
