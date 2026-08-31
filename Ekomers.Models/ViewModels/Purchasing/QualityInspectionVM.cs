using Ekomers.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class QualityInspectionListVM
{
    public int Id { get; set; }
    public string InspectionNumber { get; set; } = string.Empty;
    public int GoodsReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public PrdQualityControlStatus Status { get; set; }
    public DateTime? SampleDate { get; set; }
    public DateTime? ResultDate { get; set; }
}

public sealed class QualityInspectionDetailVM
{
    public int Id { get; set; }
    public string InspectionNumber { get; set; } = string.Empty;
    public int GoodsReceiptId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public PrdQualityControlStatus Status { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? DecisionUserId { get; set; }
    public string? DecisionNote { get; set; }
    public QualityInspectionFormVM Form { get; set; } = new();
}

public sealed class QualityInspectionFormVM
{
    public int Id { get; set; }

    [StringLength(100), Display(Name = "Numune Numarası")]
    public string? SampleNumber { get; set; }

    [DataType(DataType.DateTime), Display(Name = "Numune Alma Tarihi")]
    public DateTime? SampleDate { get; set; }

    [DataType(DataType.DateTime), Display(Name = "Analiz Tarihi")]
    public DateTime? AnalysisDate { get; set; }

    [DataType(DataType.DateTime), Display(Name = "Sonuç Tarihi")]
    public DateTime? ResultDate { get; set; }

    [StringLength(250), Display(Name = "Laboratuvar")]
    public string? LaboratoryName { get; set; }

    [StringLength(100), Display(Name = "Sertifika / Rapor Numarası")]
    public string? CertificateNumber { get; set; }

    [StringLength(2000), Display(Name = "Analiz Sonuç Özeti")]
    public string? ResultSummary { get; set; }

    [StringLength(2000), Display(Name = "Spesifikasyon / Kontrol Notları")]
    public string? SpecificationNotes { get; set; }
}

public sealed class QualityInspectionDecisionVM
{
    public int Id { get; set; }
    public PrdQualityControlStatus Decision { get; set; }
    [StringLength(1000), Display(Name = "Karar Notu")] public string? DecisionNote { get; set; }
}
