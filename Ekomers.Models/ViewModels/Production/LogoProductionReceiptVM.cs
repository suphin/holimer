using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Production;

public class LogoProductionReceiptListVM
{
    public int Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string? LogoSlipNumber { get; set; }
    public string? LogoAssignedSlipNumber { get; set; }
    public DateTime DocumentDate { get; set; }
    public int WarehouseNumber { get; set; }
    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public PrdLogoTransferStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? SentDate { get; set; }
    public string? LogoReference { get; set; }
}

public sealed class LogoProductionReceiptCreateVM
{
    public int? Id { get; set; }
    [Required]
    public DateTime DocumentDate { get; set; } = DateTime.Today;
    [StringLength(50)]
    public string? LogoSlipNumber { get; set; }
    [Range(0, 9999)]
    public int WarehouseNumber { get; set; }
    [Range(0, 9999)]
    public int DivisionNumber { get; set; }
    [Range(0, 9999)]
    public int DepartmentNumber { get; set; }
    [Range(0, 9999)]
    public int FactoryNumber { get; set; }
    [StringLength(500)]
    public string? Notes { get; set; }
    public List<LogoProductionReceiptCreateLineVM> Lines { get; set; } = [new()];
}

public sealed class LogoProductionReceiptCreateLineVM
{
    public int? MaterialId { get; set; }
    public string Quantity { get; set; } = string.Empty;
    [StringLength(100)]
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    [StringLength(250)]
    public string? Description { get; set; }
    public string? MaterialText { get; set; }
    public string? UnitText { get; set; }
}

public sealed class LogoProductionReceiptDetailVM : LogoProductionReceiptListVM
{
    public int DivisionNumber { get; set; }
    public int DepartmentNumber { get; set; }
    public int FactoryNumber { get; set; }
    public string? Notes { get; set; }
    public DateTime? LastAttemptDate { get; set; }
    public string? LastError { get; set; }
    public List<LogoProductionReceiptDetailLineVM> Lines { get; set; } = [];
}

public sealed class LogoProductionReceiptDetailLineVM
{
    public int Sequence { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string LogoCode { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string UnitCode { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string? Description { get; set; }
}
