using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;

namespace Ekomers.Models.Entity.Production;

/// <summary>
/// Portal stok bakiyesini değiştirmeden Logo Tiger'a sarf fişi göndermek için
/// hazırlanan yerel aktarım belgesidir.
/// </summary>
public sealed class PrdLogoConsumptionSlip : BaseEntity
{
    public string DocumentNumber { get; set; } = string.Empty;
    public string? LogoSlipNumber { get; set; }
    public string? LogoAssignedSlipNumber { get; set; }
    public DateTime DocumentDate { get; set; }
    public int WarehouseNumber { get; set; }
    public int DivisionNumber { get; set; }
    public int DepartmentNumber { get; set; }
    public int FactoryNumber { get; set; }
    public PrdLogoTransferStatus Status { get; set; } = PrdLogoTransferStatus.Draft;
    public string? Notes { get; set; }
    public int AttemptCount { get; set; }
    public DateTime? LastAttemptDate { get; set; }
    public DateTime? SentDate { get; set; }
    public string? LogoReference { get; set; }
    public string? LastError { get; set; }
}

public sealed class PrdLogoConsumptionSlipLine : BaseEntity
{
    public int SlipId { get; set; }
    public int Sequence { get; set; }
    public int MaterialId { get; set; }
    public int UnitId { get; set; }
    public decimal Quantity { get; set; }
    public string? Description { get; set; }
}
