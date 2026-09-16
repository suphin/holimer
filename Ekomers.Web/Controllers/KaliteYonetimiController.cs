using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "KaliteAnalizGoruntule")]
public sealed class KaliteYonetimiController : Controller
{
    private const string DispositionSourceDocumentType = "PurQualityInspectionDisposition";
    private readonly ApplicationDbContext _context;
    private readonly PurchasingInventoryReversalService _reversalService;

    public KaliteYonetimiController(ApplicationDbContext context, PurchasingInventoryReversalService reversalService)
    {
        _context = context;
        _reversalService = reversalService;
    }

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
        var disposition = await _context.PrdInventoryDocuments.AsNoTracking()
            .Where(x => x.SourceDocumentType == DispositionSourceDocumentType && x.SourceDocumentId == id &&
                        x.Status == PrdInventoryDocumentStatus.Posted && x.IsDelete != true)
            .OrderByDescending(x => x.PostingDate)
            .Select(x => new { x.DocumentNumber, x.Type, x.PostingDate })
            .FirstOrDefaultAsync(ct);
        if (disposition != null)
        {
            model.DispositionDocumentNumber = disposition.DocumentNumber;
            model.DispositionDate = disposition.PostingDate;
            model.DispositionText = DispositionActionFromDocumentType(disposition.Type)?.ToTurkish() ?? disposition.Type.ToTurkish();
        }
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "KaliteAnalizDuzenle")]
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

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "KaliteAnalizDuzenle")]
    public async Task<IActionResult> SpekSonuclariKaydet(QualityInspectionSpecificationResultsFormVM model, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.InspectionId && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (await HasDispositionAsync(inspection.ID, ct))
        {
            TempData["error"] = "Stok sonucu işlenmiş kalite kaydının spesifikasyon sonuçları değiştirilemez.";
            return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
        }
        if (IsFinal(inspection.Status))
        {
            TempData["error"] = "Tamamlanmış kalite kararı doğrudan değiştirilemez. Önce yetkili geri alma işlemini kullanınız.";
            return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
        }
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

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "KaliteAnalizDuzenle")]
    public async Task<IActionResult> AnalizKaydet([Bind(Prefix = "Form")] QualityInspectionFormVM model, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (await HasDispositionAsync(inspection.ID, ct))
        {
            TempData["error"] = "Stok sonucu işlenmiş kalite kaydının analiz bilgileri değiştirilemez.";
            return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
        }
        if (IsFinal(inspection.Status))
        {
            TempData["error"] = "Tamamlanmış kalite kararı doğrudan değiştirilemez. Önce yetkili geri alma işlemini kullanınız.";
            return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
        }
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

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "KaliteKararVer")]
    public async Task<IActionResult> KararVer(QualityInspectionDecisionVM model, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (await HasDispositionAsync(inspection.ID, ct))
        {
            TempData["error"] = "Stok sonucu işlenmiş kalite kaydının kararı değiştirilemez. Önce stok belgesi kontrollü olarak geri alınmalıdır.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        if (IsFinal(inspection.Status))
        {
            TempData["error"] = "Tamamlanmış kalite kararı doğrudan değiştirilemez. Önce kalite kararını geri alınız.";
            return RedirectToAction(nameof(AnalizDetay), new { id = inspection.ID });
        }
        if (model.Decision is not (PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval or PrdQualityControlStatus.Rejected))
        {
            TempData["error"] = "Geçerli bir kalite kararı seçiniz.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }
        if (model.Decision != PrdQualityControlStatus.Approved && string.IsNullOrWhiteSpace(model.DecisionNote))
        {
            TempData["error"] = "Şartlı onay veya red kararı için karar notu zorunludur.";
            return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
        }

        if (model.Decision != PrdQualityControlStatus.Rejected)
        {
            if (!inspection.SpecificationSetId.HasValue)
            {
                TempData["error"] = "Onay kararı vermeden önce aktif spesifikasyonu analize bağlayınız.";
                return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
            }
            var specificationResults = await (from result in _context.PurQualityInspectionSpecificationResults.AsNoTracking()
                                              join item in _context.PrdMaterialSpecificationItems.AsNoTracking() on result.SpecificationItemId equals item.ID
                                              where result.QualityInspectionId == inspection.ID && result.IsDelete != true && item.IsDelete != true
                                              select new { result.Status, item.IsRequired }).ToListAsync(ct);
            if (specificationResults.Count == 0 || specificationResults.Any(x => x.IsRequired && x.Status == PrdSpecificationResultStatus.Pending))
            {
                TempData["error"] = "Tüm zorunlu spesifikasyon sonuçları tamamlanmadan onay kararı verilemez.";
                return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
            }
            if (model.Decision == PrdQualityControlStatus.Approved && specificationResults.Any(x => x.Status is PrdSpecificationResultStatus.NonConforming or PrdSpecificationResultStatus.Conditional))
            {
                TempData["error"] = "Uygun olmayan veya şartlı spek sonucu varken doğrudan onay verilemez. Şartlı onay veya red kararı seçiniz.";
                return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
            }
            if (!inspection.ResultDate.HasValue || string.IsNullOrWhiteSpace(inspection.ResultSummary))
            {
                TempData["error"] = "Onay kararı vermeden önce sonuç tarihi ve analiz sonuç özetini kaydediniz.";
                return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
            }
        }

        var now = DateTime.Now;
        inspection.Status = model.Decision;
        inspection.DecisionDate = now;
        inspection.DecisionUserId = CurrentUser;
        inspection.DecisionNote = Clean(model.DecisionNote);
        inspection.UpdateDate = now;
        inspection.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        await RecalculateGoodsReceiptWorkflowStatusAsync(inspection.GoodsReceiptId, now, ct);
        TempData["success"] = $"Kalite kararı “{model.Decision.ToTurkish()}” olarak kaydedildi. Stok henüz karantina deposundadır.";
        return RedirectToAction(nameof(AnalizDetay), new { id = model.Id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "KaliteKarariGeriAl")]
    public async Task<IActionResult> KarariGeriAl(int id, string? reason, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (!IsFinal(inspection.Status))
        {
            TempData["error"] = "Bu kalite kaydında geri alınacak tamamlanmış bir karar bulunmuyor.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }
        if (await HasDispositionAsync(id, ct))
        {
            TempData["error"] = "Önce kalite stok sonucunu geri alınız; ardından kalite kararını geri alabilirsiniz.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }
        reason = Clean(reason);
        if (reason == null || reason.Length < 5)
        {
            TempData["error"] = "Geri alma gerekçesi en az 5 karakter olmalıdır.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }

        var previousDecision = inspection.Status.ToTurkish();
        var now = DateTime.Now;
        inspection.Status = inspection.SampleDate.HasValue || inspection.AnalysisDate.HasValue || inspection.ResultDate.HasValue
            ? PrdQualityControlStatus.Sampled
            : PrdQualityControlStatus.Pending;
        inspection.SpecificationNotes = AppendAuditNote(inspection.SpecificationNotes,
            $"{now:dd.MM.yyyy HH:mm} - {CurrentUser}: {previousDecision} kararı geri alındı. Gerekçe: {reason}");
        inspection.DecisionDate = null;
        inspection.DecisionUserId = null;
        inspection.DecisionNote = null;
        inspection.UpdateDate = now;
        inspection.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        await RecalculateGoodsReceiptWorkflowStatusAsync(inspection.GoodsReceiptId, now, ct);
        TempData["success"] = $"{inspection.InspectionNumber} kalite kararı geri alındı ve yeniden değerlendirmeye açıldı.";
        return RedirectToAction(nameof(AnalizDetay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SatinalmaKayitSil")]
    public async Task<IActionResult> KaliteKaydiSil(int id, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (await HasDispositionAsync(id, ct))
        {
            TempData["error"] = "Stok sonucu bulunan kalite kaydı silinemez. Önce stok sonucunu geri alınız.";
            return RedirectToAction(nameof(AnalizDetay), new { id });
        }

        var now = DateTime.Now;
        var results = await _context.PurQualityInspectionSpecificationResults
            .Where(x => x.QualityInspectionId == id && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var result in results)
        {
            result.IsActive = false;
            result.IsDelete = true;
            result.DeleteDate = now;
            result.DeleteUserID = CurrentUser;
        }
        inspection.IsActive = false;
        inspection.IsDelete = true;
        inspection.DeleteDate = now;
        inspection.DeleteUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        await RecalculateGoodsReceiptWorkflowStatusAsync(inspection.GoodsReceiptId, now, ct);
        TempData["success"] = $"{inspection.InspectionNumber} kalite kaydı silindi. Mal kabul üzerinden yeniden analiz kaydı oluşturabilirsiniz.";
        return RedirectToAction("MalKabulDetay", "SatinalmaYonetimi", new { id = inspection.GoodsReceiptId });
    }

    [HttpGet]
    public async Task<IActionResult> StokSonuclandirma(string? search, string status = "pending", CancellationToken ct = default)
    {
        ViewBag.Modul = "KaliteYonetimi";
        search = Clean(search) ?? string.Empty;
        status = status?.Trim().ToLowerInvariant() == "completed" ? "completed" :
            status?.Trim().ToLowerInvariant() == "all" ? "all" : "pending";

        var sourceRows = await (
            from inspection in _context.PurQualityInspections.AsNoTracking()
            join receipt in _context.PurGoodsReceipts.AsNoTracking() on inspection.GoodsReceiptId equals receipt.ID
            join receiptLine in _context.PurGoodsReceiptLines.AsNoTracking() on inspection.GoodsReceiptLineId equals receiptLine.ID
            join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
            join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
            join material in _context.PrdMaterials.AsNoTracking() on inspection.MaterialId equals material.ID
            join lot in _context.PrdStockLots.AsNoTracking() on inspection.StockLotId equals lot.ID
            join warehouse in _context.PrdWarehouses.AsNoTracking() on inspection.WarehouseId equals warehouse.ID
            join unit in _context.PrdUnits.AsNoTracking() on receiptLine.UnitId equals unit.ID
            where inspection.IsDelete != true && receipt.IsDelete != true && receiptLine.IsDelete != true &&
                  (inspection.Status == PrdQualityControlStatus.Approved ||
                   inspection.Status == PrdQualityControlStatus.ConditionalApproval ||
                   inspection.Status == PrdQualityControlStatus.Rejected)
            orderby inspection.DecisionDate descending, inspection.ID descending
            select new
            {
                InspectionId = inspection.ID,
                inspection.InspectionNumber,
                GoodsReceiptId = receipt.ID,
                receipt.ReceiptNumber,
                SupplierCode = supplier.Code,
                SupplierName = supplier.Name,
                MaterialCode = material.Code,
                MaterialName = material.Name,
                lot.LotNumber,
                Quantity = receiptLine.ReceivedQuantity,
                Unit = unit.Name,
                SourceWarehouse = warehouse.Code + " - " + warehouse.Name,
                QualityStatus = inspection.Status,
                inspection.DecisionDate,
                inspection.DecisionNote
            }).ToListAsync(ct);

        var inspectionIds = sourceRows.Select(x => x.InspectionId).ToList();
        var documents = inspectionIds.Count == 0
            ? []
            : await _context.PrdInventoryDocuments.AsNoTracking()
                .Where(x => x.SourceDocumentType == DispositionSourceDocumentType && x.SourceDocumentId.HasValue &&
                            inspectionIds.Contains(x.SourceDocumentId.Value) && x.Status == PrdInventoryDocumentStatus.Posted &&
                            x.IsDelete != true)
                .Select(x => new { InspectionId = x.SourceDocumentId!.Value, x.DocumentNumber, x.Type, x.PostingDate, x.TargetWarehouseId, x.ID })
                .ToListAsync(ct);
        var documentByInspection = documents.GroupBy(x => x.InspectionId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.ID).First());
        var targetWarehouseIds = documents.Where(x => x.TargetWarehouseId.HasValue).Select(x => x.TargetWarehouseId!.Value).Distinct().ToList();
        var targetWarehouses = targetWarehouseIds.Count == 0
            ? new Dictionary<int, string>()
            : await _context.PrdWarehouses.AsNoTracking()
                .Where(x => targetWarehouseIds.Contains(x.ID))
                .ToDictionaryAsync(x => x.ID, x => x.Code + " - " + x.Name, ct);

        var allRows = sourceRows.Select(x =>
        {
            documentByInspection.TryGetValue(x.InspectionId, out var document);
            var action = document == null ? null : DispositionActionFromDocumentType(document.Type);
            return new QualityStockDispositionRowVM
            {
                InspectionId = x.InspectionId,
                InspectionNumber = x.InspectionNumber,
                GoodsReceiptId = x.GoodsReceiptId,
                ReceiptNumber = x.ReceiptNumber,
                SupplierCode = x.SupplierCode,
                SupplierName = x.SupplierName,
                MaterialCode = x.MaterialCode,
                MaterialName = x.MaterialName,
                LotNumber = x.LotNumber,
                Quantity = x.Quantity,
                Unit = x.Unit,
                SourceWarehouse = x.SourceWarehouse,
                QualityStatus = x.QualityStatus,
                DecisionDate = x.DecisionDate,
                DecisionNote = x.DecisionNote,
                IsCompleted = document != null,
                DispositionAction = action,
                DispositionDocumentNumber = document?.DocumentNumber,
                DispositionDate = document?.PostingDate,
                TargetWarehouse = document?.TargetWarehouseId is int warehouseId && targetWarehouses.TryGetValue(warehouseId, out var warehouseName)
                    ? warehouseName
                    : null
            };
        }).ToList();

        var model = new QualityStockDispositionListVM
        {
            Search = search,
            Status = status,
            PendingCount = allRows.Count(x => !x.IsCompleted),
            CompletedCount = allRows.Count(x => x.IsCompleted),
            UsableWarehouses = await _context.PrdWarehouses.AsNoTracking()
                .Where(x => x.IsDelete != true && x.IsActive != false && x.Type != PrdWarehouseType.Quarantine && x.Type != PrdWarehouseType.Scrap)
                .OrderBy(x => x.Type).ThenBy(x => x.Code)
                .Select(x => new SelectListItem(x.Code + " - " + x.Name + " (" + x.Type.ToTurkish() + ")", x.ID.ToString()))
                .ToListAsync(ct),
            ScrapWarehouses = await _context.PrdWarehouses.AsNoTracking()
                .Where(x => x.IsDelete != true && x.IsActive != false && x.Type == PrdWarehouseType.Scrap)
                .OrderBy(x => x.Code)
                .Select(x => new SelectListItem(x.Code + " - " + x.Name, x.ID.ToString()))
                .ToListAsync(ct)
        };

        IEnumerable<QualityStockDispositionRowVM> filtered = allRows;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(x => x.InspectionNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.ReceiptNumber.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.SupplierCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.SupplierName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.MaterialCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.MaterialName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.LotNumber.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        filtered = status switch
        {
            "completed" => filtered.Where(x => x.IsCompleted),
            "all" => filtered,
            _ => filtered.Where(x => !x.IsCompleted)
        };
        model.Rows = filtered.ToList();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SatinalmaStokSonuclandir")]
    public async Task<IActionResult> StokSonuclandir(QualityStockDispositionFormVM model, CancellationToken ct)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == model.InspectionId && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        if (!IsFinal(inspection.Status))
            return DispositionError(inspection, "Kalite kararı tamamlanmadan stok sonucu işlenemez.");
        if (await HasDispositionAsync(inspection.ID, ct))
            return DispositionError(inspection, "Bu kalite kaydının stok sonucu daha önce işlenmiş.");

        var isAccepted = inspection.Status is PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval;
        if (isAccepted && model.Action != PurQualityDispositionAction.ReleaseToUsableStock)
            return DispositionError(inspection, "Onaylanan lot yalnızca kullanılabilir stoğa aktarılabilir.");
        if (!isAccepted && model.Action is not (PurQualityDispositionAction.ReturnToSupplier or PurQualityDispositionAction.MoveToScrap))
            return DispositionError(inspection, "Reddedilen lot için tedarikçiye iade veya hurda işlemi seçiniz.");
        if ((inspection.Status == PrdQualityControlStatus.ConditionalApproval || !isAccepted) && string.IsNullOrWhiteSpace(model.Notes))
            return DispositionError(inspection, "Şartlı onay, iade ve hurda işlemlerinde açıklama zorunludur.");

        var receiptLine = await _context.PurGoodsReceiptLines.FirstOrDefaultAsync(x => x.ID == inspection.GoodsReceiptLineId && x.IsDelete != true, ct);
        var receipt = await _context.PurGoodsReceipts.FirstOrDefaultAsync(x => x.ID == inspection.GoodsReceiptId && x.IsDelete != true, ct);
        var sourceLot = await _context.PrdStockLots.FirstOrDefaultAsync(x => x.ID == inspection.StockLotId && x.IsDelete != true, ct);
        var sourceWarehouse = await _context.PrdWarehouses.FirstOrDefaultAsync(x => x.ID == inspection.WarehouseId && x.IsDelete != true, ct);
        if (receiptLine == null || receipt == null || sourceLot == null || sourceWarehouse == null || sourceLot.WarehouseId != sourceWarehouse.ID)
            return DispositionError(inspection, "Karantina lotu veya mal kabul bağlantısı bulunamadı.");

        var movements = await _context.PrdStockMovements.AsNoTracking()
            .Where(x => x.WarehouseId == sourceWarehouse.ID && x.StockLotId == sourceLot.ID && x.IsDelete != true)
            .Select(x => new { x.Direction, x.Quantity, x.TotalCost })
            .ToListAsync(ct);
        var physicalQuantity = movements.Sum(x => x.Direction == PrdStockDirection.In ? x.Quantity : -x.Quantity);
        var stockValue = movements.Sum(x => x.Direction == PrdStockDirection.In ? x.TotalCost : -x.TotalCost);
        var quantity = receiptLine.ReceivedQuantity;
        if (quantity <= 0 || physicalQuantity < quantity)
            return DispositionError(inspection, $"{sourceLot.LotNumber} lotunda sonuçlandırılacak miktar kadar karantina stoku bulunmuyor. Mevcut: {physicalQuantity:0.######}.");
        var unitCost = physicalQuantity == 0 ? 0 : stockValue / physicalQuantity;
        if (unitCost < 0)
            return DispositionError(inspection, "Karantina lotunun stok maliyeti geçersiz.");

        PrdWarehouse? targetWarehouse = null;
        if (model.Action == PurQualityDispositionAction.ReleaseToUsableStock)
        {
            targetWarehouse = model.TargetWarehouseId.HasValue
                ? await _context.PrdWarehouses.FirstOrDefaultAsync(x => x.ID == model.TargetWarehouseId && x.IsDelete != true && x.IsActive != false && x.Type != PrdWarehouseType.Quarantine && x.Type != PrdWarehouseType.Scrap, ct)
                : null;
            if (targetWarehouse == null)
                return DispositionError(inspection, "Aktif bir kullanılabilir hedef depo seçiniz.");
            if (sourceLot.ExpirationDate.HasValue && sourceLot.ExpirationDate.Value.Date < DateTime.Today)
                return DispositionError(inspection, "Son kullanma tarihi geçmiş lot kullanılabilir stoğa aktarılamaz.");
        }
        else if (model.Action == PurQualityDispositionAction.MoveToScrap)
        {
            targetWarehouse = model.TargetWarehouseId.HasValue
                ? await _context.PrdWarehouses.FirstOrDefaultAsync(x => x.ID == model.TargetWarehouseId && x.IsDelete != true && x.IsActive != false && x.Type == PrdWarehouseType.Scrap, ct)
                : null;
            if (targetWarehouse == null)
                return DispositionError(inspection, "Aktif bir hurda deposu seçiniz.");
        }

        var now = DateTime.Now;
        var documentType = model.Action switch
        {
            PurQualityDispositionAction.ReleaseToUsableStock => PrdInventoryDocumentType.WarehouseTransfer,
            PurQualityDispositionAction.ReturnToSupplier => PrdInventoryDocumentType.SupplierReturn,
            PurQualityDispositionAction.MoveToScrap => PrdInventoryDocumentType.ScrapTransfer,
            _ => throw new InvalidOperationException("Geçersiz stok sonuçlandırma işlemi.")
        };
        var prefix = model.Action switch
        {
            PurQualityDispositionAction.ReleaseToUsableStock => "KLT",
            PurQualityDispositionAction.ReturnToSupplier => "KIA",
            _ => "KHR"
        };

        PrdStockLot? targetLot = null;
        if (targetWarehouse != null)
        {
            targetLot = await _context.PrdStockLots.FirstOrDefaultAsync(x =>
                x.MaterialId == sourceLot.MaterialId && x.WarehouseId == targetWarehouse.ID &&
                x.LotNumber == sourceLot.LotNumber && x.IsDelete != true, ct);
            if (targetLot != null &&
                ((targetLot.ProductionDate.HasValue && sourceLot.ProductionDate.HasValue && targetLot.ProductionDate.Value.Date != sourceLot.ProductionDate.Value.Date) ||
                 (targetLot.ExpirationDate.HasValue && sourceLot.ExpirationDate.HasValue && targetLot.ExpirationDate.Value.Date != sourceLot.ExpirationDate.Value.Date)))
                return DispositionError(inspection, "Hedef depodaki aynı lot numarasının üretim tarihi veya SKT bilgisi farklı.");
            if (targetLot == null)
            {
                targetLot = new PrdStockLot
                {
                    MaterialId = sourceLot.MaterialId,
                    WarehouseId = targetWarehouse.ID,
                    LotNumber = sourceLot.LotNumber,
                    ProductionDate = sourceLot.ProductionDate,
                    ExpirationDate = sourceLot.ExpirationDate,
                    IsActive = true,
                    IsDelete = false,
                    CreateDate = now,
                    CreateUserID = CurrentUser
                };
                _context.PrdStockLots.Add(targetLot);
                await _context.SaveChangesAsync(ct);
            }
        }

        var totalCost = quantity * unitCost;
        var document = new PrdInventoryDocument
        {
            DocumentNumber = $"{prefix}-{inspection.ID}-{now:yyyyMMddHHmmssfff}",
            Type = documentType,
            Status = PrdInventoryDocumentStatus.Posted,
            DocumentDate = now.Date,
            PostingDate = now,
            PostedUserId = CurrentUser,
            SourceWarehouseId = sourceWarehouse.ID,
            TargetWarehouseId = targetWarehouse?.ID,
            CurrencyCode = "TRY",
            ExchangeRate = 1m,
            TotalCost = totalCost,
            SourceDocumentType = DispositionSourceDocumentType,
            SourceDocumentId = inspection.ID,
            Notes = $"{inspection.InspectionNumber} kalite stok sonucu: {model.Action.ToTurkish()}. {Clean(model.Notes)}".Trim(),
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = CurrentUser
        };
        _context.PrdInventoryDocuments.Add(document);
        await _context.SaveChangesAsync(ct);

        var documentLine = new PrdInventoryDocumentLine
        {
            InventoryDocumentId = document.ID,
            Sequence = 1,
            MaterialId = receiptLine.MaterialId,
            UnitId = receiptLine.UnitId,
            SourceStockLotId = sourceLot.ID,
            TargetStockLotId = targetLot?.ID,
            LotNumber = sourceLot.LotNumber,
            ProductionDate = sourceLot.ProductionDate,
            ExpirationDate = sourceLot.ExpirationDate,
            Quantity = quantity,
            OriginalUnitCost = unitCost,
            CurrencyCode = "TRY",
            ExchangeRate = 1m,
            UnitCost = unitCost,
            TotalCost = totalCost,
            CostSource = PrdStockCostSource.Transfer,
            Notes = Clean(model.Notes),
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = CurrentUser
        };
        _context.PrdInventoryDocumentLines.Add(documentLine);
        await _context.SaveChangesAsync(ct);

        var movementType = model.Action switch
        {
            PurQualityDispositionAction.ReturnToSupplier => PrdStockMovementType.SupplierReturn,
            PurQualityDispositionAction.MoveToScrap => PrdStockMovementType.ScrapTransfer,
            _ => PrdStockMovementType.Transfer
        };
        _context.PrdStockMovements.Add(CreateDispositionMovement(document, documentLine, sourceWarehouse.ID, sourceLot.ID, PrdStockDirection.Out, movementType, now));
        if (targetWarehouse != null && targetLot != null)
            _context.PrdStockMovements.Add(CreateDispositionMovement(document, documentLine, targetWarehouse.ID, targetLot.ID, PrdStockDirection.In, movementType, now));

        await _context.SaveChangesAsync(ct);
        await RecalculateGoodsReceiptDispositionStatusAsync(receipt.ID, now, ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{inspection.InspectionNumber} için {model.Action.ToTurkish().ToLowerInvariant()} ve {document.DocumentNumber} numaralı stok belgesi oluşturuldu.";
        return RedirectToAction(nameof(StokSonuclandirma), new { search = inspection.InspectionNumber, status = "all" });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "SatinalmaSurecGeriAl")]
    public async Task<IActionResult> StokSonucunuGeriAl(int inspectionId, string? reason, CancellationToken ct)
    {
        var inspection = await _context.PurQualityInspections.FirstOrDefaultAsync(x => x.ID == inspectionId && x.IsDelete != true, ct);
        if (inspection == null) return NotFound();
        reason = Clean(reason);
        if (reason == null || reason.Length < 5)
            return DispositionError(inspection, "Geri alma gerekçesi en az 5 karakter olmalıdır.");

        var document = await _context.PrdInventoryDocuments.FirstOrDefaultAsync(x =>
            x.SourceDocumentType == DispositionSourceDocumentType && x.SourceDocumentId == inspectionId &&
            x.Status == PrdInventoryDocumentStatus.Posted && x.IsDelete != true, ct);
        if (document == null)
            return DispositionError(inspection, "Geri alınacak aktif stok sonuç belgesi bulunamadı.");

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var landedCosts = await _context.PurGoodsReceiptLandedCosts
                .Where(x => x.GoodsReceiptId == inspection.GoodsReceiptId && x.IsDelete != true)
                .ToListAsync(ct);
            foreach (var landedCost in landedCosts)
            {
                var landedDocument = await _context.PrdInventoryDocuments.FirstOrDefaultAsync(x =>
                    x.ID == landedCost.InventoryDocumentId && x.Status == PrdInventoryDocumentStatus.Posted && x.IsDelete != true, ct);
                if (landedDocument != null)
                    await _reversalService.ReversePostedDocumentAsync(landedDocument,
                        $"Kalite stok sonucu geri alındı: {reason}", CurrentUser, ct);

                var allocations = await _context.PurGoodsReceiptLandedCostAllocations
                    .Where(x => x.LandedCostId == landedCost.ID && x.IsDelete != true)
                    .ToListAsync(ct);
                var deletedAt = DateTime.Now;
                foreach (var allocation in allocations)
                {
                    allocation.IsActive = false;
                    allocation.IsDelete = true;
                    allocation.DeleteDate = deletedAt;
                    allocation.DeleteUserID = CurrentUser;
                }
                landedCost.IsActive = false;
                landedCost.IsDelete = true;
                landedCost.DeleteDate = deletedAt;
                landedCost.DeleteUserID = CurrentUser;
                await _context.SaveChangesAsync(ct);
            }
            var reversal = await _reversalService.ReversePostedDocumentAsync(document, reason, CurrentUser, ct);
            await RecalculateGoodsReceiptWorkflowStatusAsync(inspection.GoodsReceiptId, DateTime.Now, ct);
            await transaction.CommitAsync(ct);
            TempData["success"] = $"{document.DocumentNumber} geri alındı; {reversal.DocumentNumber} numaralı ters stok belgesi oluşturuldu.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["error"] = ex.Message;
        }
        return RedirectToAction(nameof(AnalizDetay), new { id = inspectionId });
    }

    private IActionResult DispositionError(PurQualityInspection inspection, string message)
    {
        TempData["error"] = message;
        return RedirectToAction(nameof(StokSonuclandirma), new { search = inspection.InspectionNumber, status = "all" });
    }

    private async Task<bool> HasDispositionAsync(int inspectionId, CancellationToken ct) =>
        await _context.PrdInventoryDocuments.AsNoTracking().AnyAsync(x =>
            x.SourceDocumentType == DispositionSourceDocumentType &&
            x.SourceDocumentId == inspectionId &&
            x.Status == PrdInventoryDocumentStatus.Posted &&
            x.IsDelete != true, ct);

    private static PurQualityDispositionAction? DispositionActionFromDocumentType(PrdInventoryDocumentType documentType) => documentType switch
    {
        PrdInventoryDocumentType.WarehouseTransfer => PurQualityDispositionAction.ReleaseToUsableStock,
        PrdInventoryDocumentType.SupplierReturn => PurQualityDispositionAction.ReturnToSupplier,
        PrdInventoryDocumentType.ScrapTransfer => PurQualityDispositionAction.MoveToScrap,
        _ => null
    };

    private PrdStockMovement CreateDispositionMovement(
        PrdInventoryDocument document,
        PrdInventoryDocumentLine line,
        int warehouseId,
        int lotId,
        PrdStockDirection direction,
        PrdStockMovementType movementType,
        DateTime now) => new()
    {
        InventoryDocumentId = document.ID,
        InventoryDocumentLineId = line.ID,
        MaterialId = line.MaterialId,
        WarehouseId = warehouseId,
        StockLotId = lotId,
        Direction = direction,
        MovementType = movementType,
        Quantity = line.Quantity,
        UnitId = line.UnitId,
        OriginalUnitCost = line.OriginalUnitCost,
        CurrencyCode = line.CurrencyCode,
        ExchangeRate = line.ExchangeRate,
        UnitCost = line.UnitCost,
        TotalCost = line.TotalCost,
        CostSource = line.CostSource,
        MovementDate = document.DocumentDate,
        DocumentNumber = document.DocumentNumber,
        DocumentType = PrdStockDocumentType.InventoryDocument,
        DocumentId = document.ID,
        TransferNumber = document.TargetWarehouseId.HasValue ? document.DocumentNumber : null,
        Description = document.Notes,
        IsActive = true,
        IsDelete = false,
        CreateDate = now,
        CreateUserID = CurrentUser
    };

    private async Task RecalculateGoodsReceiptDispositionStatusAsync(int receiptId, DateTime now, CancellationToken ct)
    {
        var receipt = await _context.PurGoodsReceipts.FirstAsync(x => x.ID == receiptId && x.IsDelete != true, ct);
        var inspections = await _context.PurQualityInspections.AsNoTracking()
            .Where(x => x.GoodsReceiptId == receiptId && x.IsDelete != true)
            .Select(x => new { x.ID, x.Status })
            .ToListAsync(ct);

        if (inspections.Count == 0 || inspections.Any(x => !IsFinal(x.Status)))
        {
            receipt.Status = PurGoodsReceiptStatus.InQuarantine;
        }
        else
        {
            var inspectionIds = inspections.Select(x => x.ID).ToList();
            var dispositions = await _context.PrdInventoryDocuments.AsNoTracking()
                .Where(x => x.SourceDocumentType == DispositionSourceDocumentType && x.SourceDocumentId.HasValue &&
                            inspectionIds.Contains(x.SourceDocumentId.Value) && x.Status == PrdInventoryDocumentStatus.Posted &&
                            x.IsDelete != true)
                .Select(x => new { InspectionId = x.SourceDocumentId!.Value, x.Type })
                .ToListAsync(ct);
            var latestByInspection = dispositions
                .GroupBy(x => x.InspectionId)
                .Select(x => x.Last())
                .ToList();

            if (latestByInspection.Count < inspections.Count)
            {
                receipt.Status = PurGoodsReceiptStatus.StockPartiallyCompleted;
            }
            else if (latestByInspection.All(x => x.Type == PrdInventoryDocumentType.WarehouseTransfer))
            {
                receipt.Status = PurGoodsReceiptStatus.TransferredToStock;
            }
            else if (latestByInspection.All(x => x.Type == PrdInventoryDocumentType.SupplierReturn))
            {
                receipt.Status = PurGoodsReceiptStatus.Returned;
            }
            else if (latestByInspection.All(x => x.Type == PrdInventoryDocumentType.ScrapTransfer))
            {
                receipt.Status = PurGoodsReceiptStatus.Scrapped;
            }
            else
            {
                receipt.Status = PurGoodsReceiptStatus.CompletedWithMixedDisposition;
            }
        }

        receipt.UpdateDate = now;
        receipt.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
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

    private async Task RecalculateGoodsReceiptWorkflowStatusAsync(int receiptId, DateTime now, CancellationToken ct)
    {
        var inspectionIds = await _context.PurQualityInspections.AsNoTracking()
            .Where(x => x.GoodsReceiptId == receiptId && x.IsDelete != true)
            .Select(x => x.ID)
            .ToListAsync(ct);
        var hasActiveDisposition = inspectionIds.Count > 0 && await _context.PrdInventoryDocuments.AsNoTracking().AnyAsync(x =>
            x.SourceDocumentType == DispositionSourceDocumentType && x.SourceDocumentId.HasValue &&
            inspectionIds.Contains(x.SourceDocumentId.Value) && x.Status == PrdInventoryDocumentStatus.Posted && x.IsDelete != true, ct);
        if (hasActiveDisposition)
            await RecalculateGoodsReceiptDispositionStatusAsync(receiptId, now, ct);
        else
            await RecalculateGoodsReceiptQualityStatusAsync(receiptId, now, ct);
    }

    private string CurrentUser => User.Identity?.Name ?? "system";
    private static bool IsFinal(PrdQualityControlStatus status) => status is PrdQualityControlStatus.Approved or PrdQualityControlStatus.ConditionalApproval or PrdQualityControlStatus.Rejected;
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string AppendAuditNote(string? current, string entry)
    {
        var combined = string.IsNullOrWhiteSpace(current) ? entry : $"{current.Trim()}\n{entry}";
        return combined.Length <= 2000 ? combined : combined[^2000..];
    }

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
