using Ekomers.Data;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                               MaterialCode = material.Code,
                               MaterialName = material.Name,
                               Unit = unit.Name,
                               Quantity = receiptLine.ReceivedQuantity,
                               LotNumber = lot.LotNumber,
                               ProductionDate = lot.ProductionDate,
                               ExpirationDate = lot.ExpirationDate,
                               WarehouseCode = warehouse.Code,
                               WarehouseName = warehouse.Name,
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
        return model == null ? NotFound() : View(model);
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
}
