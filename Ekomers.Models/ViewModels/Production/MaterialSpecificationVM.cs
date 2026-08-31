using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class MaterialSpecificationIndexVM
{
    public string? Search { get; set; }
    public List<MaterialSpecificationMaterialVM> Materials { get; set; } = [];
}

public sealed class MaterialSpecificationMaterialVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public PrdMaterialType MaterialType { get; set; }
    public PrdQualityControlRequirement QualityRequirement { get; set; }
    public int VersionCount { get; set; }
    public int? ActiveSetId { get; set; }
    public int? ActiveVersion { get; set; }
    public int ActiveItemCount { get; set; }
}

public sealed class MaterialSpecificationDetailVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public PrdMaterialType MaterialType { get; set; }
    public int? SelectedSetId { get; set; }
    public MaterialSpecificationSetFormVM? Set { get; set; }
    public List<MaterialSpecificationVersionVM> Versions { get; set; } = [];
    public List<MaterialSpecificationItemVM> Items { get; set; } = [];
    public List<MaterialSpecificationHistoryVM> History { get; set; } = [];
    public MaterialSpecificationItemFormVM NewItem { get; set; } = new();
}

public sealed class MaterialSpecificationSetFormVM
{
    public int Id { get; set; }
    public int MaterialId { get; set; }
    [Required, StringLength(100), Display(Name = "Spesifikasyon Kodu")]
    public string SpecificationCode { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public PrdSpecificationSetStatus Status { get; set; }
    [DataType(DataType.Date), Display(Name = "Geçerlilik Başlangıcı")]
    public DateTime? ValidFrom { get; set; }
    [DataType(DataType.Date), Display(Name = "Geçerlilik Bitişi")]
    public DateTime? ValidTo { get; set; }
    [StringLength(1000), Display(Name = "Açıklama")]
    public string? Notes { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? ApprovedUserId { get; set; }
}

public sealed class MaterialSpecificationVersionVM
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public PrdSpecificationSetStatus Status { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public int ItemCount { get; set; }
}

public sealed class MaterialSpecificationItemVM
{
    public int Id { get; set; }
    public int Sequence { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdSpecificationDataType DataType { get; set; }
    public string? UnitName { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? MinimumValue { get; set; }
    public decimal? MaximumValue { get; set; }
    public string? ExpectedText { get; set; }
    public bool? ExpectedBoolean { get; set; }
    public string? AllowedValues { get; set; }
    public string? TestMethod { get; set; }
    public bool IsRequired { get; set; }
    public PrdSpecificationCriticality Criticality { get; set; }
    public int DecimalPlaces { get; set; }
    public string? Notes { get; set; }
}

public sealed class MaterialSpecificationItemFormVM
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int SpecificationSetId { get; set; }
    [Range(1, 9999), Display(Name = "Sıra")] public int Sequence { get; set; } = 1;
    [Required, StringLength(50), Display(Name = "Spek Kodu")] public string Code { get; set; } = string.Empty;
    [Required, StringLength(250), Display(Name = "Spek Adı")] public string Name { get; set; } = string.Empty;
    [Display(Name = "Veri Tipi")] public PrdSpecificationDataType DataType { get; set; } = PrdSpecificationDataType.Numeric;
    [StringLength(50), Display(Name = "Birim")] public string? UnitName { get; set; }
    [Display(Name = "Hedef Değer")] public string? TargetValue { get; set; }
    [Display(Name = "Minimum Değer")] public string? MinimumValue { get; set; }
    [Display(Name = "Maksimum Değer")] public string? MaximumValue { get; set; }
    [StringLength(500), Display(Name = "Beklenen Metin")] public string? ExpectedText { get; set; }
    [Display(Name = "Beklenen Sonuç")] public bool? ExpectedBoolean { get; set; }
    [StringLength(1000), Display(Name = "İzin Verilen Değerler")] public string? AllowedValues { get; set; }
    [StringLength(500), Display(Name = "Test / Analiz Yöntemi")] public string? TestMethod { get; set; }
    [Display(Name = "Zorunlu")] public bool IsRequired { get; set; } = true;
    [Display(Name = "Kritiklik")] public PrdSpecificationCriticality Criticality { get; set; } = PrdSpecificationCriticality.Major;
    [Range(0, 6), Display(Name = "Ondalık Hassasiyeti")] public int DecimalPlaces { get; set; } = 2;
    [StringLength(1000), Display(Name = "Not")] public string? Notes { get; set; }
}

public sealed class MaterialSpecificationHistoryVM
{
    public DateTime ActionDate { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ActionUserId { get; set; } = string.Empty;
}
