using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrQualityOrPurchasing")]
public sealed class KaliteYonetimiController : Controller
{
    private readonly ApplicationDbContext _context;

    public KaliteYonetimiController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Analizler(int? goodsReceiptId, CancellationToken ct)
    {
        ViewBag.Modul = "KaliteYonetimi";
        ViewBag.GoodsReceiptId = goodsReceiptId;
        var query = from inspection in _context.PurQualityInspections.AsNoTracking()
                    join receipt in _context.PurGoodsReceipts.AsNoTracking() on inspection.GoodsReceiptId equals receipt.ID
                    join receiptLine in _context.PurGoodsReceiptLines.AsNoTracking() on inspection.GoodsReceiptLineId equals receiptLine.ID
                    join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
                    join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                    join material in _context.PrdMaterials.AsNoTracking() on inspection.MaterialId equals material.ID
                    join lot in _context.PrdStockLots.AsNoTracking() on inspection.StockLotId equals lot.ID
                    join warehouse in _context.PrdWarehouses.AsNoTracking() on inspection.WarehouseId equals warehouse.ID
                    join unit in _context.PrdUnits.AsNoTracking() on receiptLine.UnitId equals unit.ID
                    where inspection.IsDelete != true && receipt.IsDelete != true && receiptLine.IsDelete != true &&
                          (!goodsReceiptId.HasValue || inspection.GoodsReceiptId == goodsReceiptId.Value)
                    orderby inspection.Status, inspection.ID descending
                    select new QualityInspectionListVM
                    {
                        Id = inspection.ID,
                        InspectionNumber = inspection.InspectionNumber,
                        GoodsReceiptId = receipt.ID,
                        ReceiptNumber = receipt.ReceiptNumber,
                        SupplierName = supplier.Name,
                        MaterialCode = material.Code,
                        MaterialName = material.Name,
                        LotNumber = lot.LotNumber,
                        Quantity = receiptLine.ReceivedQuantity,
                        Unit = unit.Name,
                        WarehouseCode = warehouse.Code,
                        WarehouseName = warehouse.Name,
                        Status = inspection.Status,
                        SampleDate = inspection.SampleDate,
                        ResultDate = inspection.ResultDate
                    };
        return View(await query.ToListAsync(ct));
    }

    [HttpGet]
    public async Task<IActionResult> AnalizDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "KaliteYonetimi";
        var model = await (from inspection in _context.PurQualityInspections.AsNoTracking()
                           join receipt in _context.PurGoodsReceipts.AsNoTracking() on inspection.GoodsReceiptId equals receipt.ID
                           join receiptLine in _context.PurGoodsReceiptLines.AsNoTracking() on inspection.GoodsReceiptLineId equals receiptLine.ID
                           join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
                           join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                           join material in _context.PrdMaterials.AsNoTracking() on inspection.MaterialId equals material.ID
                           join lot in _context.PrdStockLots.AsNoTracking() on inspection.StockLotId equals lot.ID
                           join warehouse in _context.PrdWarehouses.AsNoTracking() on inspection.WarehouseId equals warehouse.ID
                           join unit in _context.PrdUnits.AsNoTracking() on receiptLine.UnitId equals unit.ID
                           where inspection.ID == id && inspection.IsDelete != true
                           select new QualityInspectionDetailVM
                           {
                               Id = inspection.ID,
                               InspectionNumber = inspection.InspectionNumber,
                               GoodsReceiptId = receipt.ID,
                               ReceiptNumber = receipt.ReceiptNumber,
                               OrderNumber = order.OrderNumber,
                               SupplierCode = supplier.Code,
                               SupplierName = supplier.Name,
                               MaterialId = material.ID,
                               MaterialCode = material.Code,
                               MaterialName = material.Name,
                               Unit = unit.Name,
                               Quantity = receiptLine.ReceivedQuantity,
                               LotNumber = lot.LotNumber,
                               ProductionDate = lot.ProductionDate,
                               ExpirationDate = lot.ExpirationDate,
                               WarehouseCode = warehouse.Code,
                               WarehouseName = warehouse.Name,
                               SpecificationSetId = inspection.SpecificationSetId,
                               Status = inspection.Status,
                               DecisionDate = inspection.DecisionDate,
                               DecisionUserId = inspection.DecisionUserId,
                               DecisionNote = inspection.DecisionNote,
                               Form = new QualityInspectionFormVM
                               {
                                   Id = inspection.ID,
                                   SampleNumber = inspection.SampleNumber,
                                   SampleDate = inspection.SampleDate,
                                   AnalysisDate = inspection.AnalysisDate,
                                   ResultDate = inspection.ResultDate,
                                   LaboratoryName = inspection.LaboratoryName,
                                   CertificateNumber = inspection.CertificateNumber,
                                   ResultSummary = inspection.ResultSummary,
                                   SpecificationNotes = inspection.SpecificationNotes
                               }
                           }).FirstOrDefaultAsync(ct);
        if (model == null) return NotFound();
        if (model.SpecificationSetId.HasValue)
        {
            var specification = await _context.PrdMaterialSpecificationSets.AsNoTracking().FirstOrDefaultAsync(x => x.ID == model.SpecificationSetId && x.IsDelete != true, ct);
            if (specification != null)
            {
                model.SpecificationCode = specification.SpecificationCode;
                model.SpecificationVersion = specification.VersionNumber;
                model.SpecificationResults = await (from result in _context.PurQualityInspectionSpecificationResults.AsNoTracking()
                                                      join item in _context.PrdMaterialSpecificationItems.AsNoTracking() on result.SpecificationItemId equals item.ID
                                                      where result.QualityInspectionId == id && result.IsDelete != true && item.IsDelete != true
                                                      orderby item.Sequence
                                                      select new QualityInspectionSpecificationResultVM
                                                      {
                                                          ResultId = result.ID, SpecificationItemId = item.ID, Sequence = item.Sequence,
                                                          Code = item.Code, Name = item.Name, DataType = item.DataType, UnitName = item.UnitName,
                                                          TargetValue = item.TargetValue, MinimumValue = item.MinimumValue, MaximumValue = item.MaximumValue,
                                                          ExpectedText = item.ExpectedText, ExpectedBoolean = item.ExpectedBoolean, AllowedValues = item.AllowedValues,
                                                          TestMethod = item.TestMethod, IsRequired = item.IsRequired, Criticality = item.Criticality,
                                                          NumericValue = result.NumericValue.HasValue ? result.NumericValue.Value.ToString("0.########", CultureInfo.GetCultureInfo("tr-TR")) : null,
                                                          TextValue = result.TextValue, BooleanValue = result.BooleanValue, Status = result.Status,
                                                          EvaluationNote = result.EvaluationNote
                                                      }).ToListAsync(ct);
            }
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> AktifSpesifikasyonuBagla(int id, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (inspection.SpecificationSetId.HasValue)
        {
            TempData["error"] = "Bu analize daha önce bir spesifikasyon versiyonu bağlanmış.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }
        if (IsFinal(inspection.Status))
        {
            TempData["error"] = "Kararı tamamlanmış analize spesifikasyon bağlanamaz.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }
        var set = await FindActiveSpecificationAsync(inspection.MaterialId, DateTime.Now, ct);
        if (set == null)
        {
            TempData["error"] = "Malzeme için geçerli aktif spesifikasyon bulunamadı.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }
        inspection.SpecificationSetId = set.ID;
        inspection.UpdateDate = DateTime.Now;
        inspection.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        await CreateSpecificationResultRowsAsync(inspection.ID, set.ID, ct);
        TempData["success"] = $"{set.SpecificationCode} v{set.VersionNumber} analize bağlandı.";
        return RedirectToAction(nameof(AnalizDetay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> SpekSonuclariKaydet(QualityInspectionSpecificationResultsFormVM model, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.InspectionId && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (IsFinal(inspection.Status) && !User.IsInRole("Admin")) return Forbid();
        if (!inspection.SpecificationSetId.HasValue)
        {
            TempData["error"] = "Önce aktif spesifikasyonu analize bağlayınız.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.InspectionId });
        }
        var resultIds = model.Results.Select(x => x.ResultId).Distinct().ToList();
        var results = await _context.PurQualityInspectionSpecificationResults.Where(x => resultIds.Contains(x.ID) && x.QualityInspectionId == inspection.ID && x.IsDelete != true).ToListAsync(ct);
        var itemIds = results.Select(x => x.SpecificationItemId).Distinct().ToList();
        var items = await _context.PrdMaterialSpecificationItems.AsNoTracking().Where(x => itemIds.Contains(x.ID) && x.IsDelete != true).ToDictionaryAsync(x => x.ID, ct);
        var now = DateTime.Now;
        foreach (var input in model.Results)
        {
            var result = results.FirstOrDefault(x => x.ID == input.ResultId);
            if (result == null || !items.TryGetValue(result.SpecificationItemId, out var item)) continue;
            result.NumericValue = null;
            result.TextValue = null;
            result.BooleanValue = null;
            result.EvaluationNote = Clean(input.EvaluationNote);
            result.Status = PrdSpecificationResultStatus.Pending;
            switch (item.DataType)
            {
                case PrdSpecificationDataType.Numeric:
                    if (!string.IsNullOrWhiteSpace(input.NumericValue))
                    {
                        if (!TryParseDecimal(input.NumericValue, out var numeric))
                        {
                            TempData["error"] = $"{item.Code} için geçerli bir sayısal sonuç giriniz.";
                            return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
                        }
                        result.NumericValue = numeric;
                        result.Status = IsNumericConforming(numeric, item) ? PrdSpecificationResultStatus.Conforming : PrdSpecificationResultStatus.NonConforming;
                    }
                    break;
                case PrdSpecificationDataType.Text:
                    result.TextValue = Clean(input.TextValue);
                    if (result.TextValue != null)
                    {
                        result.Status = input.ManualStatus is PrdSpecificationResultStatus.Conforming or PrdSpecificationResultStatus.NonConforming or PrdSpecificationResultStatus.Conditional
                            ? input.ManualStatus : PrdSpecificationResultStatus.Pending;
                    }
                    break;
                case PrdSpecificationDataType.Boolean:
                    result.BooleanValue = input.BooleanValue;
                    if (input.BooleanValue.HasValue && item.ExpectedBoolean.HasValue)
                        result.Status = input.BooleanValue == item.ExpectedBoolean ? PrdSpecificationResultStatus.Conforming : PrdSpecificationResultStatus.NonConforming;
                    break;
                case PrdSpecificationDataType.Selection:
                    result.TextValue = Clean(input.TextValue);
                    if (result.TextValue != null)
                    {
                        var allowed = SplitAllowedValues(item.AllowedValues);
                        result.Status = allowed.Contains(result.TextValue, StringComparer.OrdinalIgnoreCase) ? PrdSpecificationResultStatus.Conforming : PrdSpecificationResultStatus.NonConforming;
                    }
                    break;
            }
            result.AnalysisDate = result.Status == PrdSpecificationResultStatus.Pending ? null : now;
            result.AnalyzedUserId = result.Status == PrdSpecificationResultStatus.Pending ? null : CurrentUser;
            result.UpdateDate = now;
            result.UpdateUserID = CurrentUser;
        }
        if (!IsFinal(inspection.Status)) inspection.Status = PrdQualityControlStatus.Sampled;
        inspection.AnalysisDate ??= now;
        inspection.UpdateDate = now;
        inspection.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Spesifikasyon sonuçları kaydedildi ve değerlendirildi.";
        return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> AnalizKaydet([Bind(Prefix = "Form")] QualityInspectionFormVM model, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (IsFinal(inspection.Status) && !User.IsInRole("Admin")) return Forbid();
        if (model.AnalysisDate.HasValue && model.SampleDate.HasValue && model.AnalysisDate < model.SampleDate)
            ModelState.AddModelError("Form.AnalysisDate", "Analiz tarihi numune alma tarihinden önce olamaz.");
        if (model.ResultDate.HasValue && model.AnalysisDate.HasValue && model.ResultDate < model.AnalysisDate)
            ModelState.AddModelError("Form.ResultDate", "Sonuç tarihi analiz tarihinden önce olamaz.");
        if (!ModelState.IsValid)
        {
            TempData["error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }

        inspection.SampleNumber = Clean(model.SampleNumber);
        inspection.SampleDate = model.SampleDate;
        inspection.SampledUserId = model.SampleDate.HasValue || !string.IsNullOrWhiteSpace(model.SampleNumber) ? CurrentUser : null;
        inspection.AnalysisDate = model.AnalysisDate;
        inspection.ResultDate = model.ResultDate;
        inspection.LaboratoryName = Clean(model.LaboratoryName);
        inspection.CertificateNumber = Clean(model.CertificateNumber);
        inspection.ResultSummary = Clean(model.ResultSummary);
        inspection.SpecificationNotes = Clean(model.SpecificationNotes);
        if (!IsFinal(inspection.Status))
            inspection.Status = inspection.SampleDate.HasValue || !string.IsNullOrWhiteSpace(inspection.SampleNumber)
                ? PrdQualityControlStatus.Sampled
                : PrdQualityControlStatus.Pending;
        inspection.UpdateDate = DateTime.Now;
        inspection.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Numune ve analiz bilgileri kaydedildi.";
        return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> KararVer(QualityInspectionDecisionVM model, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (IsFinal(inspection.Status) && !User.IsInRole("Admin")) return Forbid();
        if (model.Decision is not (PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval or PrdQualityControlStatus.Rejected))
        {
            TempData["error"] = "Geçerli bir kalite kararı seçiniz.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        if (!inspection.SpecificationSetId.HasValue)
        {
            TempData["error"] = "Kalite kararı vermeden önce aktif spesifikasyonu analize bağlayınız.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        var specificationResults = await (from result in _context.PurQualityInspectionSpecificationResults.AsNoTracking()
                                          join item in _context.PrdMaterialSpecificationItems.AsNoTracking() on result.SpecificationItemId equals item.ID
                                          where result.QualityInspectionId == inspection.ID && result.IsDelete != true && item.IsDelete != true
                                          select new { result.Status, item.IsRequired, item.Criticality, item.Code }).ToListAsync(ct);
        if (specificationResults.Count == 0 || specificationResults.Any(x => x.IsRequired && x.Status == PrdSpecificationResultStatus.Pending))
        {
            TempData["error"] = "Tüm zorunlu spesifikasyon sonuçları tamamlanmadan kalite kararı verilemez.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        if (model.Decision == PrdQualityControlStatus.Approved && specificationResults.Any(x => x.Status is PrdSpecificationResultStatus.NonConforming or PrdSpecificationResultStatus.Conditional))
        {
            TempData["error"] = "Uygun olmayan veya şartlı spek sonucu varken doğrudan onay verilemez. Şartlı onay veya red kararı seçiniz.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        if (!inspection.ResultDate.HasValue || string.IsNullOrWhiteSpace(inspection.ResultSummary))
        {
            TempData["error"] = "Karar vermeden önce sonuç tarihi ve analiz sonuç özetini kaydediniz.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        if (model.Decision != PrdQualityControlStatus.Approved && string.IsNullOrWhiteSpace(model.DecisionNote))
        {
            TempData["error"] = "Şartlı onay veya red kararı için karar notu zorunludur.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }

        var now = DateTime.Now;
        inspection.Status = model.Decision;
        inspection.DecisionDate = now;
        inspection.DecisionUserId = CurrentUser;
        inspection.DecisionNote = Clean(model.DecisionNote);
        inspection.UpdateDate = now;
        inspection.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        await RecalculateGoodsReceiptQualityStatusAsync(inspection.GoodsReceiptId, now, ct);
        TempData["success"] = $"Kalite kararı “{model.Decision.ToTurkish()}” olarak kaydedildi. Stok henüz karantina deposundadır.";
        return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
    }

    private async Task RecalculateGoodsReceiptQualityStatusAsync(int receiptId, DateTime now, CancellationToken ct)
    {
        var receipt = await _context.PurGoodsReceipts.FirstAsync(x => x.ID == receiptId, ct);
        var statuses = await _context.PurQualityInspections.AsNoTracking()
            .Where(x => x.GoodsReceiptId == receiptId && x.IsDelete != true)
            .Select(x => x.Status)
            .ToListAsync(ct);
        if (statuses.Count == 0 || statuses.Any(x => x is PrdQualityControlStatus.Pending or PrdQualityControlStatus.Sampled))
            receipt.Status = PurGoodsReceiptStatus.InQuarantine;
        else if (statuses.All(x => x is PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval))
            receipt.Status = PurGoodsReceiptStatus.QualityApproved;
        else if (statuses.All(x => x == PrdQualityControlStatus.Rejected))
            receipt.Status = PurGoodsReceiptStatus.QualityRejected;
        else
            receipt.Status = PurGoodsReceiptStatus.QualityPartiallyDecided;
        receipt.UpdateDate = now;
        receipt.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
    }

    private string CurrentUser => User.Identity?.Name ?? "system";
    private static bool IsFinal(PrdQualityControlStatus status) => status is PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval or PrdQualityControlStatus.Rejected;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<PrdMaterialSpecificationSet?> FindActiveSpecificationAsync(int materialId, DateTime date, CancellationToken ct)
    {
        var dayStart = date.Date;
        var nextDay = dayStart.AddDays(1);
        return await _context.PrdMaterialSpecificationSets.AsNoTracking()
            .Where(x => x.MaterialId == materialId && x.Status == PrdSpecificationSetStatus.Active && x.IsDelete != true &&
                        (!x.ValidFrom.HasValue || x.ValidFrom < nextDay) && (!x.ValidTo.HasValue || x.ValidTo >= dayStart))
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);
    }

    private async Task CreateSpecificationResultRowsAsync(int inspectionId, int setId, CancellationToken ct)
    {
        var existingItemIds = await _context.PurQualityInspectionSpecificationResults.AsNoTracking().Where(x => x.QualityInspectionId == inspectionId && x.IsDelete != true).Select(x => x.SpecificationItemId).ToListAsync(ct);
        var itemIds = await _context.PrdMaterialSpecificationItems.AsNoTracking().Where(x => x.SpecificationSetId == setId && x.IsDelete != true && !existingItemIds.Contains(x.ID)).Select(x => x.ID).ToListAsync(ct);
        var now = DateTime.Now;
        foreach (var itemId in itemIds)
            _context.PurQualityInspectionSpecificationResults.Add(new PurQualityInspectionSpecificationResult { QualityInspectionId = inspectionId, SpecificationSetId = setId, SpecificationItemId = itemId, Status = PrdSpecificationResultStatus.Pending, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser });
        if (itemIds.Count > 0) await _context.SaveChangesAsync(ct);
    }

    private static bool IsNumericConforming(decimal value, PrdMaterialSpecificationItem item)
    {
        if (item.MinimumValue.HasValue && value < item.MinimumValue.Value) return false;
        if (item.MaximumValue.HasValue && value > item.MaximumValue.Value) return false;
        if (!item.MinimumValue.HasValue && !item.MaximumValue.HasValue && item.TargetValue.HasValue) return value == item.TargetValue.Value;
        return true;
    }

    private static List<string> SplitAllowedValues(string? value) => (value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim().Replace(" ", string.Empty);
        if (normalized.Contains(',') && normalized.Contains('.')) normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.') ? normalized.Replace(".", string.Empty).Replace(',', '.') : normalized.Replace(",", string.Empty);
        else if (normalized.Contains(',')) normalized = normalized.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result);
    }
}
