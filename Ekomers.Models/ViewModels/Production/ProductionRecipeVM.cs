using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class ProductionRecipeCreateVM
{
    [Required, Display(Name = "Reçete Kodu")]
    public string Code { get; set; } = string.Empty;

    [Required, Display(Name = "Reçete Adı")]
    public string Name { get; set; } = string.Empty;

    [Required, Display(Name = "Üretilecek Ürün")]
    public int ProductMaterialId { get; set; }

    [Range(1, int.MaxValue), Display(Name = "Versiyon")]
    public int VersionNumber { get; set; } = 1;

    [Display(Name = "Baz Miktar")]
    public decimal BaseQuantity { get; set; } = 1m;

    [Required, Display(Name = "Birim")]
    public int UnitId { get; set; }

    [Display(Name = "Durum")]
    public PrdRecipeStatus Status { get; set; } = PrdRecipeStatus.Draft;

    [DataType(DataType.Date), Display(Name = "Geçerlilik Başlangıcı")]
    public DateTime? ValidFrom { get; set; }

    [DataType(DataType.Date), Display(Name = "Geçerlilik Bitişi")]
    public DateTime? ValidTo { get; set; }

    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Versiyon Notu")]
    public string? Notes { get; set; }

    public List<SelectListItem> Products { get; set; } = [];
    public List<SelectListItem> Units { get; set; } = [];
}

public sealed class ProductionRecipeListVM
{
    public int RecipeId { get; set; }
    public int RecipeVersionId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Product { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public decimal BaseQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public PrdRecipeStatus Status { get; set; }
    public int ItemCount { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
