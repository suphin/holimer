using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Profitability;

public sealed class ProductCostIndexVM
{
    public string? Search { get; set; }
    public IReadOnlyList<ProductCostSummaryVM> Products { get; set; } = [];
}

public sealed class ProductCostSummaryVM
{
    public int LogoMaterialRef { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int VersionCount { get; set; }
    public int LatestVersionNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal TotalUnitCostTry { get; set; }
}

public sealed class ProductCostFormVM
{
    [Range(1, int.MaxValue, ErrorMessage = "Ürün seçiniz.")]
    [Display(Name = "Ürün")]
    public int LogoMaterialRef { get; set; }

    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }

    [Required, DataType(DataType.Date)]
    [Display(Name = "Geçerlilik başlangıcı")]
    public DateTime ValidFrom { get; set; } = DateTime.Today;

    [Required, StringLength(50)]
    [Display(Name = "Birim")]
    public string UnitCode { get; set; } = "Adet";

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Malzeme maliyeti negatif olamaz.")]
    [Display(Name = "Malzeme maliyeti")]
    public decimal MaterialCost { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "İşçilik maliyeti negatif olamaz.")]
    [Display(Name = "İşçilik maliyeti")]
    public decimal LaborCost { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Nakliye maliyeti negatif olamaz.")]
    [Display(Name = "Nakliye maliyeti")]
    public decimal FreightCost { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Genel gider negatif olamaz.")]
    [Display(Name = "Genel üretim gideri")]
    public decimal OverheadCost { get; set; }

    [Range(typeof(decimal), "0", "999999999999", ErrorMessage = "Diğer maliyet negatif olamaz.")]
    [Display(Name = "Diğer maliyet")]
    public decimal OtherCost { get; set; }

    [Required, StringLength(3, MinimumLength = 3)]
    [Display(Name = "Para birimi")]
    public string CurrencyCode { get; set; } = "TRY";

    [Range(
        typeof(decimal),
        "0.000001",
        "999999",
        ErrorMessage = "Kur sıfırdan büyük olmalıdır.",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    [Display(Name = "Döviz kuru")]
    public decimal ExchangeRate { get; set; } = 1m;

    [Required, StringLength(1000)]
    [Display(Name = "Değişiklik nedeni")]
    public string ChangeReason { get; set; } = string.Empty;

    public IReadOnlyList<ProductCostProductOptionVM> Products { get; set; } = [];
    public ProductCostSummaryVM? PreviousVersion { get; set; }
}

public sealed class ProductCostProductOptionVM
{
    public int LogoMaterialRef { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = "Adet";
}

public sealed class ProductCostBulkVM
{
    [Display(Name = "Ürün kodu ön ekleri")]
    public string CodePrefixes { get; set; } = "152HM,153TG";

    [Required, DataType(DataType.Date)]
    [Display(Name = "Geçerlilik başlangıcı")]
    public DateTime ValidFrom { get; set; } = DateTime.Today;

    [Required, StringLength(3, MinimumLength = 3)]
    [Display(Name = "Para birimi")]
    public string CurrencyCode { get; set; } = "TRY";

    [Range(
        typeof(decimal),
        "0.000001",
        "999999",
        ErrorMessage = "Kur sıfırdan büyük olmalıdır.",
        ParseLimitsInInvariantCulture = true,
        ConvertValueInInvariantCulture = true)]
    [Display(Name = "Döviz kuru")]
    public decimal ExchangeRate { get; set; } = 1m;

    [Required, StringLength(1000)]
    [Display(Name = "Değişiklik nedeni")]
    public string ChangeReason { get; set; } = string.Empty;

    public string? Search { get; set; }
    public string StatusFilter { get; set; } = "all";
    public int TotalProductCount { get; set; }
    public bool IsTruncated { get; set; }
    public List<ProductCostBulkRowVM> Rows { get; set; } = [];
}

public sealed class ProductCostBulkRowVM
{
    public bool Selected { get; set; }
    public int LogoMaterialRef { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitCode { get; set; } = "Adet";
    public int? CurrentVersionNumber { get; set; }
    public DateTime? CurrentValidFrom { get; set; }
    public decimal? CurrentUnitCostTry { get; set; }
    public decimal? NewUnitCost { get; set; }
    public decimal? MaterialCost { get; set; }
    public decimal? LaborCost { get; set; }
    public decimal? FreightCost { get; set; }
    public decimal? OverheadCost { get; set; }
    public decimal? OtherCost { get; set; }
}

public sealed class ProductCostHistoryVM
{
    public int LogoMaterialRef { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public IReadOnlyList<ProductCostHistoryRowVM> Versions { get; set; } = [];
}

public sealed class ProductCostHistoryRowVM
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public decimal MaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal FreightCost { get; set; }
    public decimal OverheadCost { get; set; }
    public decimal OtherCost { get; set; }
    public decimal TotalUnitCost { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal TotalUnitCostTry { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? ChangeReason { get; set; }
    public DateTime? CreateDate { get; set; }
    public string? CreateUser { get; set; }
}
