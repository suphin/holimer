namespace Ekomers.Models.ViewModels.Purchasing;

public sealed class SupplierImportPreviewVM
{
    public int PortalMinimumExclusiveId { get; set; } = 4466;
    public string LogoCodePrefix { get; set; } = "320";
    public int PortalRecordCount { get; set; }
    public int LogoRecordCount { get; set; }
    public int CandidateCount { get; set; }
    public int NewCount { get; set; }
    public int ExistingCount { get; set; }
    public int InvalidCount { get; set; }
    public List<SupplierImportPreviewLineVM> Lines { get; set; } = [];
}

public sealed class SupplierImportPreviewLineVM
{
    public string Source { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxNumber { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? LogoCode { get; set; }
    public bool IsActive { get; set; }
    public int? ExistingSupplierId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MatchReason { get; set; }
}

public sealed record SupplierImportResult(int Added, int Updated, int Skipped);
