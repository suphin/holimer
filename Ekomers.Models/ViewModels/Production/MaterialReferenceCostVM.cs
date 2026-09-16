using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Ekomers.Models.ViewModels.Production;

public sealed class MaterialReferenceCostListVM
{
    public string Search { get; set; } = string.Empty;
    public string Status { get; set; } = "missing";
    public List<MaterialReferenceCostListRowVM> Rows { get; set; } = [];
    public int MissingCount { get; set; }
    public int ReferenceCount { get; set; }
    public int StockCount { get; set; }
}

public sealed class MaterialReferenceCostListRowVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public int RecipeCount { get; set; }
    public bool HasStockCost { get; set; }
    public decimal? StockUnitCostTry { get; set; }
    public string? StockCostUnit { get; set; }
    public DateTime? StockCostDate { get; set; }
    public int? ReferenceVersion { get; set; }
    public decimal? ReferenceUnitCost { get; set; }
    public string? ReferenceCurrencyCode { get; set; }
    public decimal? ReferenceExchangeRate { get; set; }
    public decimal? ReferenceUnitCostTry { get; set; }
    public string? ReferenceUnit { get; set; }
    public DateTime? ReferenceValidFrom { get; set; }
    public string? ReferenceSource { get; set; }
}

public sealed class MaterialReferenceCostEditVM
{
    public int CostId { get; set; }
    public int VersionNumber { get; set; }
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string UnitCost { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "TRY";
    public string ExchangeRate { get; set; } = "1";
    public DateTime ValidFrom { get; set; } = DateTime.Today;
    public PrdMaterialReferenceCostSource Source { get; set; } = PrdMaterialReferenceCostSource.Manual;
    public string? Notes { get; set; }
    public string? ReturnUrl { get; set; }
    public MaterialReferenceCostHistoryRowVM? CurrentVersion { get; set; }
    public List<SelectListItem> Units { get; set; } = [];
    public List<LogoMaterialPurchasePriceVM> LogoPurchasePrices { get; set; } = [];
    public string? LogoPurchasePriceError { get; set; }
}

public sealed class LogoMaterialPurchasePriceVM
{
    public DateTime InvoiceDate { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal LogoUnitPrice { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal NetLineAmountTry { get; set; }
    public decimal NetUnitPriceTry { get; set; }
    public int? ProductionUnitId { get; set; }
    public bool CanUseAsReference => ProductionUnitId.HasValue && NetUnitPriceTry > 0;
}

public sealed class MaterialReferenceCostDetailVM
{
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = string.Empty;
    public List<MaterialReferenceCostHistoryRowVM> Versions { get; set; } = [];
}

public sealed class MaterialReferenceCostHistoryRowVM
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public decimal UnitCost { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public decimal ExchangeRate { get; set; }
    public decimal UnitCostTry { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public string Source { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? CreateDate { get; set; }
    public string CreatedUser { get; set; } = string.Empty;
    public DateTime? UpdateDate { get; set; }
    public string? UpdatedUser { get; set; }
    public int? ImportBatchId { get; set; }
    public string? ImportBatchNumber { get; set; }
    public string? SourceSheet { get; set; }
    public int? SourceRow { get; set; }
}

public sealed class MaterialReferenceCostImportUploadVM
{
    public DateTime ValidFrom { get; set; } = DateTime.Today;
    public string? Notes { get; set; }
}

public sealed class MaterialReferenceCostImportPreviewVM
{
    public string FileName { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public string? Notes { get; set; }
    public string Payload { get; set; } = string.Empty;
    public List<MaterialReferenceCostImportPreviewRowVM> Rows { get; set; } = [];
    public int ReadyCount => Rows.Count(x => x.CanImport && !x.HasConflict);
    public int ReviewCount => Rows.Count(x => x.CanImport && x.HasConflict);
    public int SkippedCount => Rows.Count(x => !x.CanImport);
}

public sealed class MaterialReferenceCostImportPreviewRowVM
{
    public bool Selected { get; set; }
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string MaterialType { get; set; } = string.Empty;
    public int UnitId { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal UnitCostTry { get; set; }
    public decimal ExcelValueTry { get; set; }
    public string SourceSheet { get; set; } = string.Empty;
    public int SourceRow { get; set; }
    public string? ProductName { get; set; }
    public bool CanImport { get; set; }
    public bool HasConflict { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class MaterialReferenceCostImportSaveVM
{
    public string FileName { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public string? Notes { get; set; }
    public string Payload { get; set; } = string.Empty;
}

public class MaterialReferenceCostImportBatchListRowVM
{
    public int Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public int ImportedCount { get; set; }
    public int ReviewCount { get; set; }
    public int SkippedCount { get; set; }
    public DateTime? CreateDate { get; set; }
    public string CreatedUser { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public sealed class MaterialReferenceCostImportBatchDetailVM : MaterialReferenceCostImportBatchListRowVM
{
    public List<MaterialReferenceCostImportBatchDetailRowVM> Rows { get; set; } = [];
}

public sealed class MaterialReferenceCostImportBatchDetailRowVM
{
    public int CostId { get; set; }
    public int MaterialId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public decimal UnitCostTry { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string SourceSheet { get; set; } = string.Empty;
    public int? SourceRow { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
