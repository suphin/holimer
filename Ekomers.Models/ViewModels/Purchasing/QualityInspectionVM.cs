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
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public string WarehouseCode { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int? SpecificationSetId { get; set; }
    public string? SpecificationCode { get; set; }
    public int? SpecificationVersion { get; set; }
    public PrdQualityControlStatus Status { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? DecisionUserId { get; set; }
    public string? DecisionNote { get; set; }
    public QualityInspectionFormVM Form { get; set; } = new();
    public List<QualityInspectionSpecificationResultVM> SpecificationResults { get; set; } = [];
}

public sealed class QualityInspectionSpecificationResultVM
{
    public int ResultId { get; set; }
    public int SpecificationItemId { get; set; }
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
    public string? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public PrdSpecificationResultStatus Status { get; set; }
    public string? EvaluationNote { get; set; }
}

public sealed class QualityInspectionSpecificationResultsFormVM
{
    public int InspectionId { get; set; }
    public List<QualityInspectionSpecificationResultInputVM> Results { get; set; } = [];
}

public sealed class QualityInspectionSpecificationResultInputVM
{
    public int ResultId { get; set; }
    public string? NumericValue { get; set; }
    public string? TextValue { get; set; }
    public bool? BooleanValue { get; set; }
    public PrdSpecificationResultStatus ManualStatus { get; set; }
    [StringLength(1000)] public string? EvaluationNote { get; set; }
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
