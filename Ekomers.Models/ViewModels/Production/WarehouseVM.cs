using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public sealed class WarehouseListVM
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PrdWarehouseType Type { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int LotCount { get; set; }
    public int MovementCount { get; set; }
}

public sealed class WarehouseFormVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Depo kodu zorunludur.")]
    [StringLength(50, ErrorMessage = "Depo kodu en fazla 50 karakter olabilir.")]
    [Display(Name = "Depo Kodu")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Depo adı zorunludur.")]
    [StringLength(150, ErrorMessage = "Depo adı en fazla 150 karakter olabilir.")]
    [Display(Name = "Depo Adı")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Depo türü seçilmelidir.")]
    [Display(Name = "Depo Türü")]
    public PrdWarehouseType Type { get; set; }

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir.")]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Display(Name = "Aktif")]
    public bool IsActive { get; set; } = true;
}
