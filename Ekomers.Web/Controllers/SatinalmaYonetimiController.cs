using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrPurchasing")]
public sealed class SatinalmaYonetimiController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly PurchasingSupplierImportService _supplierImport;
    private readonly ITcmbService _tcmbService;

    public SatinalmaYonetimiController(ApplicationDbContext context, PurchasingSupplierImportService supplierImport, ITcmbService tcmbService)
    {
        _context = context;
        _supplierImport = supplierImport;
        _tcmbService = tcmbService;
    }

    [HttpGet]
    public async Task<IActionResult> Talepler(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await (from request in _context.PurPurchaseRequests.AsNoTracking()
                           where request.IsDelete != true
                           orderby request.ID descending
                           select new PurchaseRequestListVM
                           {
                               Id = request.ID,
                               RequestNumber = request.RequestNumber,
                               RequestDate = request.RequestDate,
                               NeededDate = request.NeededDate,
                               RequestedUserId = request.RequestedUserId,
                               Priority = request.Priority,
                               Status = request.Status,
                               LineCount = _context.PurPurchaseRequestLines.Count(x => x.PurchaseRequestId == request.ID && x.IsDelete != true),
                               PendingLineCount = _context.PurPurchaseRequestLines.Count(x => x.PurchaseRequestId == request.ID && x.Status == PurPurchaseRequestLineStatus.PendingApproval && x.IsDelete != true),
                               ApprovedLineCount = _context.PurPurchaseRequestLines.Count(x => x.PurchaseRequestId == request.ID && x.Status == PurPurchaseRequestLineStatus.Approved && x.IsDelete != true)
                           }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> YeniTalep(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = new PurchaseRequestFormVM
        {
            RequestDate = DateTime.Today,
            NeededDate = DateTime.Today.AddDays(7),
            Lines = [new PurchaseRequestFormLineVM { NeededDate = DateTime.Today.AddDays(7) }]
        };
        await FillMaterialOptionsAsync(model, ct);
        return View("TalepFormu", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YeniTalep(PurchaseRequestFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var validLines = await ValidateFormAsync(model, ct);
        if (!ModelState.IsValid)
        {
            await FillMaterialOptionsAsync(model, ct);
            return View("TalepFormu", model);
        }

        var now = DateTime.Now;
        var user = CurrentUser;
        var request = new PurPurchaseRequest
        {
            RequestNumber = $"ST-{now:yyyyMMddHHmmssfff}",
            RequestDate = model.RequestDate.Date,
            NeededDate = model.NeededDate?.Date,
            RequestedUserId = user,
            Priority = model.Priority,
            Status = PurPurchaseRequestStatus.Draft,
            Notes = model.Notes?.Trim(),
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.PurPurchaseRequests.Add(request);
        await _context.SaveChangesAsync(ct);
        var sequence = 0;
        foreach (var item in validLines)
        {
            item.Input.NeededDate ??= model.NeededDate;
            _context.PurPurchaseRequestLines.Add(CreateLine(request.ID, ++sequence, item.Input, item.UnitId, item.Quantity, now, user));
        }
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        TempData["success"] = $"{request.RequestNumber} numaralı satınalma talebi taslak olarak oluşturuldu.";
        return RedirectToAction(nameof(TalepDetay), new { id = request.ID });
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TalepDuzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var request = await _context.PurPurchaseRequests.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (!CanEditRequest(request)) return Forbid();

        var model = new PurchaseRequestFormVM
        {
            Id = request.ID,
            RequestNumber = request.RequestNumber,
            Status = request.Status,
            RequestDate = request.RequestDate,
            NeededDate = request.NeededDate,
            Priority = request.Priority,
            Notes = request.Notes,
            Lines = await _context.PurPurchaseRequestLines.AsNoTracking()
                .Where(x => x.PurchaseRequestId == id && x.IsDelete != true)
                .OrderBy(x => x.Sequence)
                .Select(x => new PurchaseRequestFormLineVM
                {
                    Id = x.ID,
                    MaterialId = x.MaterialId,
                    RequestedQuantityInput = x.RequestedQuantity.ToString("0.######", CultureInfo.InvariantCulture),
                    NeededDate = x.NeededDate,
                    Reason = x.Reason
                }).ToListAsync(ct)
        };
        await FillMaterialOptionsAsync(model, ct);
        return View("TalepFormu", model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TalepDuzenle(PurchaseRequestFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var request = await _context.PurPurchaseRequests.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (!CanEditRequest(request)) return Forbid();
        model.Status = request.Status;
        if (await HasPostedGoodsReceiptForRequestAsync(request.ID, ct))
        {
            TempData["error"] = "Bu talebe bağlı mal kabul karantina stoğuna işlendiği için talep geriye alınamaz.";
            return RedirectToAction(nameof(TalepDetay), new { id = request.ID });
        }

        var validLines = await ValidateFormAsync(model, ct);
        if (!ModelState.IsValid)
        {
            model.RequestNumber = request.RequestNumber;
            await FillMaterialOptionsAsync(model, ct);
            return View("TalepFormu", model);
        }

        var now = DateTime.Now;
        var user = CurrentUser;
        var workflowWasReset = request.Status != PurPurchaseRequestStatus.Draft;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        if (workflowWasReset)
            await RollbackRequestToDraftAsync(request, now, user, ct);
        request.RequestDate = model.RequestDate.Date;
        request.NeededDate = model.NeededDate?.Date;
        request.Priority = model.Priority;
        request.Notes = model.Notes?.Trim();
        request.UpdateDate = now;
        request.UpdateUserID = user;

        var existingLines = await _context.PurPurchaseRequestLines.Where(x => x.PurchaseRequestId == request.ID && x.IsDelete != true).ToListAsync(ct);
        var postedIds = validLines.Where(x => x.Input.Id > 0).Select(x => x.Input.Id).ToHashSet();
        foreach (var removed in existingLines.Where(x => !postedIds.Contains(x.ID)))
        {
            removed.IsDelete = true;
            removed.IsActive = false;
            removed.DeleteDate = now;
            removed.DeleteUserID = user;
        }

        var sequence = 0;
        foreach (var item in validLines)
        {
            item.Input.NeededDate ??= model.NeededDate;
            var line = existingLines.FirstOrDefault(x => x.ID == item.Input.Id);
            if (line == null)
            {
                _context.PurPurchaseRequestLines.Add(CreateLine(request.ID, ++sequence, item.Input, item.UnitId, item.Quantity, now, user));
                continue;
            }

            line.Sequence = ++sequence;
            line.MaterialId = item.Input.MaterialId;
            line.UnitId = item.UnitId;
            line.RequestedQuantity = item.Quantity;
            line.ApprovedQuantity = 0;
            line.NeededDate = item.Input.NeededDate?.Date ?? model.NeededDate?.Date;
            line.Reason = item.Input.Reason?.Trim();
            line.UpdateDate = now;
            line.UpdateUserID = user;
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = workflowWasReset
            ? $"{request.RequestNumber} güncellendi; bağlı teklifler ve siparişler geri alınarak talep taslağa döndürüldü."
            : $"{request.RequestNumber} numaralı taslak güncellendi.";
        return RedirectToAction(nameof(TalepDetay), new { id = request.ID });
    }

    [HttpGet]
    public async Task<IActionResult> TalepDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await BuildDetailAsync(id, ct);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TalepSil(int id, CancellationToken ct)
    {
        var request = await _context.PurPurchaseRequests.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (await HasPostedGoodsReceiptForRequestAsync(request.ID, ct))
        {
            TempData["error"] = "Bu talebe bağlı mal kabul karantina stoğuna işlendiği için talep silinemez.";
            return RedirectToAction(nameof(TalepDetay), new { id });
        }
        var now = DateTime.Now;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        await DeleteRequestWorkflowAsync(request, now, CurrentUser, ct);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{request.RequestNumber} ve bağlı satınalma kayıtları silindi.";
        return RedirectToAction(nameof(Talepler));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OnayaGonder(int id, CancellationToken ct)
    {
        var request = await _context.PurPurchaseRequests.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (!CanSubmitRequest(request)) return Forbid();
        if (request.Status != PurPurchaseRequestStatus.Draft)
        {
            TempData["error"] = "Yalnızca taslak talep onaya gönderilebilir.";
            return RedirectToAction(nameof(TalepDetay), new { id });
        }

        var lines = await _context.PurPurchaseRequestLines.Where(x => x.PurchaseRequestId == id && x.IsDelete != true).ToListAsync(ct);
        if (lines.Count == 0)
        {
            TempData["error"] = "Onaya göndermek için en az bir talep satırı bulunmalıdır.";
            return RedirectToAction(nameof(TalepDetay), new { id });
        }

        var now = DateTime.Now;
        var user = CurrentUser;
        request.Status = PurPurchaseRequestStatus.PendingApproval;
        request.SubmittedDate = now;
        request.SubmittedUserId = user;
        request.UpdateDate = now;
        request.UpdateUserID = user;
        foreach (var line in lines)
        {
            var previous = line.Status;
            line.Status = PurPurchaseRequestLineStatus.PendingApproval;
            line.UpdateDate = now;
            line.UpdateUserID = user;
            _context.PurRequestApprovalHistories.Add(CreateHistory(request.ID, line, PurRequestApprovalAction.Submitted, previous, line.Status, 0, null, now, user));
        }
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"{request.RequestNumber} onaya gönderildi. Satırlar ayrı ayrı değerlendirilebilir.";
        return RedirectToAction(nameof(TalepDetay), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> OnayBekleyenler(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        if (!CanApproveRequests) return Forbid();
        var model = await (from request in _context.PurPurchaseRequests.AsNoTracking()
                           where request.IsDelete != true && _context.PurPurchaseRequestLines.Any(x => x.PurchaseRequestId == request.ID && x.Status == PurPurchaseRequestLineStatus.PendingApproval && x.IsDelete != true)
                           orderby request.SubmittedDate, request.ID
                           select new PurchaseRequestListVM
                           {
                               Id = request.ID,
                               RequestNumber = request.RequestNumber,
                               RequestDate = request.RequestDate,
                               NeededDate = request.NeededDate,
                               RequestedUserId = request.RequestedUserId,
                               Priority = request.Priority,
                               Status = request.Status,
                               LineCount = _context.PurPurchaseRequestLines.Count(x => x.PurchaseRequestId == request.ID && x.IsDelete != true),
                               PendingLineCount = _context.PurPurchaseRequestLines.Count(x => x.PurchaseRequestId == request.ID && x.Status == PurPurchaseRequestLineStatus.PendingApproval && x.IsDelete != true),
                               ApprovedLineCount = _context.PurPurchaseRequestLines.Count(x => x.PurchaseRequestId == request.ID && x.Status == PurPurchaseRequestLineStatus.Approved && x.IsDelete != true)
                           }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TalepOnay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        if (!CanApproveRequests) return Forbid();
        var model = await BuildDetailAsync(id, ct);
        if (model == null) return NotFound();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SatirKarari(PurchaseRequestLineDecisionVM input, CancellationToken ct)
    {
        if (!CanApproveRequests) return Forbid();
        var line = await _context.PurPurchaseRequestLines.FirstOrDefaultAsync(x => x.ID == input.LineId && x.PurchaseRequestId == input.RequestId && x.IsDelete != true, ct);
        if (line == null) return NotFound();
        if (line.Status != PurPurchaseRequestLineStatus.PendingApproval)
        {
            TempData["error"] = "Yalnızca onay bekleyen satırlar değerlendirilebilir.";
            return RedirectToAction(nameof(TalepOnay), new { id = input.RequestId });
        }

        decimal approvedQuantity = 0;
        if (input.Decision == PurRequestApprovalAction.Approved && (!TryParseDecimal(input.ApprovedQuantityInput, out approvedQuantity) || approvedQuantity <= 0))
        {
            TempData["error"] = "Onaylanan miktar sıfırdan büyük olmalıdır.";
            return RedirectToAction(nameof(TalepOnay), new { id = input.RequestId });
        }
        if (input.Decision == PurRequestApprovalAction.Rejected && string.IsNullOrWhiteSpace(input.Note))
        {
            TempData["error"] = "Reddedilen satır için açıklama zorunludur.";
            return RedirectToAction(nameof(TalepOnay), new { id = input.RequestId });
        }
        if (input.Decision != PurRequestApprovalAction.Approved && input.Decision != PurRequestApprovalAction.Rejected)
        {
            TempData["error"] = "Geçersiz onay kararı.";
            return RedirectToAction(nameof(TalepOnay), new { id = input.RequestId });
        }

        var now = DateTime.Now;
        var user = CurrentUser;
        var previousStatus = line.Status;
        var previousApprovedQuantity = line.ApprovedQuantity;
        line.Status = input.Decision == PurRequestApprovalAction.Approved ? PurPurchaseRequestLineStatus.Approved : PurPurchaseRequestLineStatus.Rejected;
        line.ApprovedQuantity = input.Decision == PurRequestApprovalAction.Approved ? approvedQuantity : 0;
        line.ApprovedDate = now;
        line.ApprovedUserId = user;
        line.ApprovalNote = input.Note?.Trim();
        line.UpdateDate = now;
        line.UpdateUserID = user;
        _context.PurRequestApprovalHistories.Add(CreateHistory(input.RequestId, line, input.Decision, previousStatus, line.Status, previousApprovedQuantity, input.Note, now, user));
        await _context.SaveChangesAsync(ct);
        await RecalculateRequestStatusAsync(input.RequestId, now, user, ct);

        TempData["success"] = input.Decision == PurRequestApprovalAction.Approved ? "Talep satırı onaylandı." : "Talep satırı reddedildi.";
        return RedirectToAction(nameof(TalepOnay), new { id = input.RequestId });
    }

    [HttpGet]
    public async Task<IActionResult> Tedarikciler(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await _context.PurSuppliers.AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderBy(x => x.Code)
            .Select(x => new SupplierListVM
            {
                Id = x.ID,
                Code = x.Code,
                Name = x.Name,
                TaxNumber = x.TaxNumber,
                ContactName = x.ContactName,
                Phone = x.Phone,
                Email = x.Email,
                LogoCode = x.LogoCode,
                IsActive = x.IsActive != false,
                QuotationCount = _context.PurSupplierQuotations.Count(q => q.SupplierId == x.ID && q.IsDelete != true)
            }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TedarikciAktar(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        if (!User.IsInRole("Admin")) return Forbid();
        var model = await _supplierImport.PreviewAsync(ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TedarikciAktar(bool aktarimiOnayliyorum, CancellationToken ct)
    {
        if (!User.IsInRole("Admin")) return Forbid();
        if (!aktarimiOnayliyorum)
        {
            TempData["error"] = "Aktarım için önizleme kontrolü onaylanmalıdır.";
            return RedirectToAction(nameof(TedarikciAktar));
        }
        var result = await _supplierImport.ImportAsync(CurrentUser, ct);
        TempData["success"] = $"Tedarikçi aktarımı tamamlandı: {result.Added} yeni, {result.Updated} eşleşen/güncellenen, {result.Skipped} atlanan kayıt.";
        return RedirectToAction(nameof(Tedarikciler));
    }

    [HttpGet]
    public IActionResult TedarikciYeni()
    {
        ViewBag.Modul = "YeniSatinalma";
        return View("TedarikciFormu", new SupplierFormVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TedarikciYeni(SupplierFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        await ValidateSupplierAsync(model, ct);
        if (!ModelState.IsValid) return View("TedarikciFormu", model);
        var now = DateTime.Now;
        var entity = new PurSupplier
        {
            Code = model.Code.Trim().ToUpperInvariant(), Name = model.Name.Trim(), TaxNumber = Clean(model.TaxNumber),
            TaxOffice = Clean(model.TaxOffice), ContactName = Clean(model.ContactName), Email = Clean(model.Email),
            Phone = Clean(model.Phone), Address = Clean(model.Address), LogoCode = Clean(model.LogoCode), Notes = Clean(model.Notes),
            IsActive = model.IsActive, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser
        };
        _context.PurSuppliers.Add(entity);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"{entity.Code} kodlu tedarikçi oluşturuldu.";
        return RedirectToAction(nameof(Tedarikciler));
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TedarikciDuzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var entity = await _context.PurSuppliers.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (entity == null) return NotFound();
        return View("TedarikciFormu", new SupplierFormVM
        {
            Id = entity.ID, Code = entity.Code, Name = entity.Name, TaxNumber = entity.TaxNumber, TaxOffice = entity.TaxOffice,
            ContactName = entity.ContactName, Email = entity.Email, Phone = entity.Phone, Address = entity.Address,
            LogoCode = entity.LogoCode, Notes = entity.Notes, IsActive = entity.IsActive != false
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TedarikciDuzenle(SupplierFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var entity = await _context.PurSuppliers.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (entity == null) return NotFound();
        await ValidateSupplierAsync(model, ct);
        if (!ModelState.IsValid) return View("TedarikciFormu", model);
        entity.Code = model.Code.Trim().ToUpperInvariant(); entity.Name = model.Name.Trim(); entity.TaxNumber = Clean(model.TaxNumber);
        entity.TaxOffice = Clean(model.TaxOffice); entity.ContactName = Clean(model.ContactName); entity.Email = Clean(model.Email);
        entity.Phone = Clean(model.Phone); entity.Address = Clean(model.Address); entity.LogoCode = Clean(model.LogoCode);
        entity.Notes = Clean(model.Notes); entity.IsActive = model.IsActive; entity.UpdateDate = DateTime.Now; entity.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"{entity.Code} kodlu tedarikçi güncellendi.";
        return RedirectToAction(nameof(Tedarikciler));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TedarikciSil(int id, CancellationToken ct)
    {
        var supplier = await _context.PurSuppliers.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (supplier == null) return NotFound();
        SoftDelete(supplier, DateTime.Now, CurrentUser);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"{supplier.Code} kodlu tedarikçi silindi. Geçmiş teklif ve sipariş kayıtları korunmuştur.";
        return RedirectToAction(nameof(Tedarikciler));
    }

    [HttpGet]
    public async Task<IActionResult> TeklifTalepleri(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var orderedByRequestLine = await _context.PurPurchaseOrderLines.AsNoTracking()
            .Where(x => x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .GroupBy(x => x.PurchaseRequestLineId)
            .Select(g => new { RequestLineId = g.Key, Quantity = g.Sum(x => x.OrderedQuantity) })
            .ToDictionaryAsync(x => x.RequestLineId, x => x.Quantity, ct);

        var approvedLines = await _context.PurPurchaseRequestLines.AsNoTracking()
            .Where(x => x.IsDelete != true &&
                        (x.Status == PurPurchaseRequestLineStatus.Approved || x.Status == PurPurchaseRequestLineStatus.InQuotation))
            .Select(x => new { x.ID, x.PurchaseRequestId, x.ApprovedQuantity })
            .ToListAsync(ct);

        var eligibleCounts = approvedLines
            .Where(x => x.ApprovedQuantity > (orderedByRequestLine.TryGetValue(x.ID, out var ordered) ? ordered : 0))
            .GroupBy(x => x.PurchaseRequestId)
            .ToDictionary(x => x.Key, x => x.Count());
        var requestIds = eligibleCounts.Keys.ToList();

        var quotationCounts = await _context.PurSupplierQuotations.AsNoTracking()
            .Where(x => requestIds.Contains(x.PurchaseRequestId) && x.IsDelete != true)
            .GroupBy(x => x.PurchaseRequestId)
            .Select(x => new { RequestId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.RequestId, x => x.Count, ct);

        var model = await _context.PurPurchaseRequests.AsNoTracking()
            .Where(x => requestIds.Contains(x.ID) && x.IsDelete != true)
            .OrderBy(x => x.RequestDate).ThenBy(x => x.ID)
            .Select(x => new QuotationRequestCandidateVM
            {
                RequestId = x.ID,
                RequestNumber = x.RequestNumber,
                RequestDate = x.RequestDate,
                RequestedUserId = x.RequestedUserId
            }).ToListAsync(ct);
        foreach (var item in model)
        {
            item.EligibleLineCount = eligibleCounts[item.RequestId];
            item.QuotationCount = quotationCounts.GetValueOrDefault(item.RequestId);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Teklifler(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await (from quotation in _context.PurSupplierQuotations.AsNoTracking()
                           join request in _context.PurPurchaseRequests.AsNoTracking() on quotation.PurchaseRequestId equals request.ID
                           join supplier in _context.PurSuppliers.AsNoTracking() on quotation.SupplierId equals supplier.ID
                           where quotation.IsDelete != true
                           orderby quotation.ID descending
                           select new SupplierQuotationListVM
                           {
                               Id = quotation.ID, QuotationNumber = quotation.QuotationNumber, RequestId = request.ID,
                               RequestNumber = request.RequestNumber, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                               SupplierQuotationNumber = quotation.SupplierQuotationNumber, QuotationDate = quotation.QuotationDate,
                                CurrencyCode = quotation.CurrencyCode, ExchangeRate = quotation.ExchangeRate,
                                GrandTotal = quotation.GrandTotal, Status = quotation.Status,
                               LineCount = _context.PurSupplierQuotationLines.Count(x => x.SupplierQuotationId == quotation.ID && x.IsDelete != true),
                               PendingLineCount = _context.PurSupplierQuotationLines.Count(x => x.SupplierQuotationId == quotation.ID && x.Status == PurSupplierQuotationLineStatus.PendingApproval && x.IsDelete != true)
                           }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> YeniTeklif(int requestId, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await BuildQuotationFormAsync(requestId, null, ct);
        if (model == null) return NotFound();
        return View("TeklifFormu", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YeniTeklif(SupplierQuotationFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var validLines = await ValidateQuotationFormAsync(model, ct);
        if (!ModelState.IsValid)
        {
            await RefillQuotationFormAsync(model, ct);
            return View("TeklifFormu", model);
        }
        var now = DateTime.Now;
        _ = TryParseDecimal(model.ExchangeRateInput, out var exchangeRate);
        var quotation = new PurSupplierQuotation
        {
            QuotationNumber = $"TF-{now:yyyyMMddHHmmssfff}", PurchaseRequestId = model.PurchaseRequestId, SupplierId = model.SupplierId,
            SupplierQuotationNumber = Clean(model.SupplierQuotationNumber), QuotationDate = model.QuotationDate.Date,
            ValidUntil = model.ValidUntil?.Date, CurrencyCode = model.CurrencyCode, ExchangeRate = exchangeRate,
            ExchangeRateDate = model.ExchangeRateDate?.Date, ExchangeRateSource = model.ExchangeRateSource,
            PaymentTerms = Clean(model.PaymentTerms), DeliveryTerms = Clean(model.DeliveryTerms), LeadTimeDays = model.LeadTimeDays,
            Status = PurSupplierQuotationStatus.Draft, Notes = Clean(model.Notes), IsActive = true, IsDelete = false,
            CreateDate = now, CreateUserID = CurrentUser
        };
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.PurSupplierQuotations.Add(quotation);
        await _context.SaveChangesAsync(ct);
        var sequence = 0;
        foreach (var item in validLines)
            _context.PurSupplierQuotationLines.Add(CreateQuotationLine(quotation.ID, ++sequence, item, now, CurrentUser));
        await _context.SaveChangesAsync(ct);
        await RecalculateQuotationTotalsAsync(quotation.ID, now, CurrentUser, ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{quotation.QuotationNumber} numaralı teklif taslak olarak kaydedildi.";
        return RedirectToAction(nameof(TeklifDetay), new { id = quotation.ID });
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TeklifDuzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var quotation = await _context.PurSupplierQuotations.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (!CanEditQuotation(quotation)) return Forbid();
        var model = await BuildQuotationFormAsync(quotation.PurchaseRequestId, quotation, ct);
        return View("TeklifFormu", model!);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TeklifDuzenle(SupplierQuotationFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var quotation = await _context.PurSupplierQuotations.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (!CanEditQuotation(quotation)) return Forbid();
        model.Status = quotation.Status;
        model.PurchaseRequestId = quotation.PurchaseRequestId;
        if (await HasPostedGoodsReceiptForQuotationAsync(quotation.ID, ct))
        {
            TempData["error"] = "Bu teklife bağlı mal kabul karantina stoğuna işlendiği için teklif geriye alınamaz.";
            return RedirectToAction(nameof(TeklifDetay), new { id = quotation.ID });
        }
        var validLines = await ValidateQuotationFormAsync(model, ct);
        if (!ModelState.IsValid)
        {
            model.QuotationNumber = quotation.QuotationNumber;
            await RefillQuotationFormAsync(model, ct);
            return View("TeklifFormu", model);
        }
        var now = DateTime.Now;
        var workflowWasReset = quotation.Status != PurSupplierQuotationStatus.Draft;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        if (workflowWasReset)
            await RollbackQuotationToDraftAsync(quotation, now, CurrentUser, ct);
        _ = TryParseDecimal(model.ExchangeRateInput, out var exchangeRate);
        quotation.SupplierId = model.SupplierId; quotation.SupplierQuotationNumber = Clean(model.SupplierQuotationNumber);
        quotation.QuotationDate = model.QuotationDate.Date; quotation.ValidUntil = model.ValidUntil?.Date;
        quotation.CurrencyCode = model.CurrencyCode; quotation.ExchangeRate = exchangeRate;
        quotation.ExchangeRateDate = model.ExchangeRateDate?.Date; quotation.ExchangeRateSource = model.ExchangeRateSource;
        quotation.PaymentTerms = Clean(model.PaymentTerms);
        quotation.DeliveryTerms = Clean(model.DeliveryTerms); quotation.LeadTimeDays = model.LeadTimeDays; quotation.Notes = Clean(model.Notes);
        quotation.UpdateDate = now; quotation.UpdateUserID = CurrentUser;
        var oldLines = await _context.PurSupplierQuotationLines.Where(x => x.SupplierQuotationId == quotation.ID && x.IsDelete != true).ToListAsync(ct);
        foreach (var oldLine in oldLines)
        {
            oldLine.IsDelete = true; oldLine.IsActive = false; oldLine.DeleteDate = now; oldLine.DeleteUserID = CurrentUser;
        }
        var sequence = 0;
        foreach (var item in validLines)
            _context.PurSupplierQuotationLines.Add(CreateQuotationLine(quotation.ID, ++sequence, item, now, CurrentUser));
        await _context.SaveChangesAsync(ct);
        await RecalculateQuotationTotalsAsync(quotation.ID, now, CurrentUser, ct);
        if (workflowWasReset)
        {
            await ReconcileQuotationAlternativesAsync(quotation.PurchaseRequestId, now, CurrentUser, ct);
            await RecalculateRequestAfterOrdersAsync(quotation.PurchaseRequestId, now, CurrentUser, ct);
        }
        await transaction.CommitAsync(ct);
        TempData["success"] = workflowWasReset
            ? $"{quotation.QuotationNumber} güncellendi; bağlı sipariş geri alınarak teklif taslağa döndürüldü."
            : $"{quotation.QuotationNumber} güncellendi.";
        return RedirectToAction(nameof(TeklifDetay), new { id = quotation.ID });
    }

    [HttpGet]
    public async Task<IActionResult> TeklifDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await BuildQuotationDetailAsync(id, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> TeklifSil(int id, CancellationToken ct)
    {
        var quotation = await _context.PurSupplierQuotations.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (await HasPostedGoodsReceiptForQuotationAsync(quotation.ID, ct))
        {
            TempData["error"] = "Bu teklife bağlı mal kabul karantina stoğuna işlendiği için teklif silinemez.";
            return RedirectToAction(nameof(TeklifDetay), new { id });
        }
        var requestId = quotation.PurchaseRequestId;
        var now = DateTime.Now;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        await DeleteQuotationWorkflowAsync(quotation, now, CurrentUser, ct);
        await _context.SaveChangesAsync(ct);
        await ReconcileQuotationAlternativesAsync(requestId, now, CurrentUser, ct);
        await RecalculateRequestAfterOrdersAsync(requestId, now, CurrentUser, ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{quotation.QuotationNumber} ve varsa bağlı satınalma siparişi silindi.";
        return RedirectToAction(nameof(Teklifler));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeklifiOnayaGonder(int id, CancellationToken ct)
    {
        var quotation = await _context.PurSupplierQuotations.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (!CanSubmitQuotation(quotation)) return Forbid();
        if (quotation.Status != PurSupplierQuotationStatus.Draft)
        {
            TempData["error"] = "Yalnızca taslak teklif onaya gönderilebilir.";
            return RedirectToAction(nameof(TeklifDetay), new { id });
        }
        var lines = await _context.PurSupplierQuotationLines.Where(x => x.SupplierQuotationId == id && x.IsDelete != true).ToListAsync(ct);
        if (lines.Count == 0)
        {
            TempData["error"] = "Teklifin en az bir satırı olmalıdır.";
            return RedirectToAction(nameof(TeklifDetay), new { id });
        }
        var now = DateTime.Now;
        quotation.Status = PurSupplierQuotationStatus.PendingApproval; quotation.SubmittedDate = now; quotation.SubmittedUserId = CurrentUser;
        quotation.UpdateDate = now; quotation.UpdateUserID = CurrentUser;
        foreach (var line in lines)
        {
            var previous = line.Status; line.Status = PurSupplierQuotationLineStatus.PendingApproval; line.UpdateDate = now; line.UpdateUserID = CurrentUser;
            _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(quotation.ID, line, PurQuotationApprovalAction.Submitted, previous, line.Status, 0, null, now, CurrentUser));
        }
        var requestLines = await _context.PurPurchaseRequestLines.Where(x => lines.Select(l => l.PurchaseRequestLineId).Contains(x.ID)).ToListAsync(ct);
        foreach (var requestLine in requestLines.Where(x => x.Status == PurPurchaseRequestLineStatus.Approved))
        {
            requestLine.Status = PurPurchaseRequestLineStatus.InQuotation; requestLine.UpdateDate = now; requestLine.UpdateUserID = CurrentUser;
        }
        var request = await _context.PurPurchaseRequests.FirstAsync(x => x.ID == quotation.PurchaseRequestId, ct);
        request.Status = PurPurchaseRequestStatus.InQuotation; request.UpdateDate = now; request.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"{quotation.QuotationNumber} yönetici onayına gönderildi.";
        return RedirectToAction(nameof(TeklifDetay), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> TeklifKarsilastir(int requestId, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var request = await _context.PurPurchaseRequests.AsNoTracking().FirstOrDefaultAsync(x => x.ID == requestId && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        await ReconcileQuotationAlternativesAsync(request.ID, DateTime.Now, CurrentUser, ct);
        var model = new QuotationComparisonVM { RequestId = request.ID, RequestNumber = request.RequestNumber, CanApprove = CanApproveQuotations };
        model.Lines = await (from line in _context.PurSupplierQuotationLines.AsNoTracking()
                             join quotation in _context.PurSupplierQuotations.AsNoTracking() on line.SupplierQuotationId equals quotation.ID
                             join supplier in _context.PurSuppliers.AsNoTracking() on quotation.SupplierId equals supplier.ID
                             join requestLine in _context.PurPurchaseRequestLines.AsNoTracking() on line.PurchaseRequestLineId equals requestLine.ID
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where quotation.PurchaseRequestId == requestId && quotation.IsDelete != true && line.IsDelete != true
                             orderby material.Code, line.NetUnitPrice
                             select new QuotationComparisonLineVM
                             {
                                 QuotationId = quotation.ID, QuotationLineId = line.ID, PurchaseRequestLineId = line.PurchaseRequestLineId,
                                 QuotationNumber = quotation.QuotationNumber, SupplierQuotationNumber = quotation.SupplierQuotationNumber,
                                 SupplierCode = supplier.Code, SupplierName = supplier.Name,
                                 MaterialCode = material.Code, MaterialName = material.Name,
                                 RequestApprovedQuantity = requestLine.ApprovedQuantity, OfferedQuantity = line.OfferedQuantity,
                                 Unit = unit.Name, UnitPrice = line.UnitPrice, DiscountRate = line.DiscountRate,
                                 NetUnitPrice = line.NetUnitPrice, VatRate = line.VatRate, GrandTotal = line.GrandTotal,
                                 CurrencyCode = quotation.CurrencyCode, ExchangeRate = quotation.ExchangeRate,
                                 ExchangeRateDate = quotation.ExchangeRateDate, ExchangeRateSource = quotation.ExchangeRateSource,
                                 QuotationDate = quotation.QuotationDate,
                                 ValidUntil = quotation.ValidUntil, DeliveryDate = line.DeliveryDate,
                                 PaymentTerms = quotation.PaymentTerms, DeliveryTerms = quotation.DeliveryTerms, Status = line.Status
                             }).ToListAsync(ct);
        var requestLineIds = model.Lines.Select(x => x.PurchaseRequestLineId).Distinct().ToList();
        var ordered = await _context.PurPurchaseOrderLines.AsNoTracking()
            .Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .GroupBy(x => x.PurchaseRequestLineId)
            .Select(x => new { x.Key, Quantity = x.Sum(y => y.OrderedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        foreach (var line in model.Lines)
        {
            line.AlreadyOrderedQuantity = ordered.GetValueOrDefault(line.PurchaseRequestLineId);
            line.RemainingRequestQuantity = Math.Max(0, line.RequestApprovedQuantity - line.AlreadyOrderedQuantity);
        }
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TeklifOnaylari(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        if (!CanApproveQuotations) return Forbid();
        var model = await (from quotation in _context.PurSupplierQuotations.AsNoTracking()
                           join request in _context.PurPurchaseRequests.AsNoTracking() on quotation.PurchaseRequestId equals request.ID
                           join supplier in _context.PurSuppliers.AsNoTracking() on quotation.SupplierId equals supplier.ID
                           where quotation.IsDelete != true && _context.PurSupplierQuotationLines.Any(x => x.SupplierQuotationId == quotation.ID && x.Status == PurSupplierQuotationLineStatus.PendingApproval && x.IsDelete != true)
                           orderby quotation.SubmittedDate, quotation.ID
                           select new SupplierQuotationListVM
                           {
                               Id = quotation.ID, QuotationNumber = quotation.QuotationNumber, RequestId = request.ID, RequestNumber = request.RequestNumber,
                               SupplierCode = supplier.Code, SupplierName = supplier.Name, SupplierQuotationNumber = quotation.SupplierQuotationNumber,
                                QuotationDate = quotation.QuotationDate, CurrencyCode = quotation.CurrencyCode,
                                ExchangeRate = quotation.ExchangeRate, GrandTotal = quotation.GrandTotal,
                               Status = quotation.Status,
                               LineCount = _context.PurSupplierQuotationLines.Count(x => x.SupplierQuotationId == quotation.ID && x.IsDelete != true),
                               PendingLineCount = _context.PurSupplierQuotationLines.Count(x => x.SupplierQuotationId == quotation.ID && x.Status == PurSupplierQuotationLineStatus.PendingApproval && x.IsDelete != true)
                           }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> TeklifOnay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        if (!CanApproveQuotations) return Forbid();
        var model = await BuildQuotationDetailAsync(id, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeklifSatirKarari(QuotationLineDecisionVM input, CancellationToken ct)
    {
        if (!CanApproveQuotations) return Forbid();
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var line = await _context.PurSupplierQuotationLines.FirstOrDefaultAsync(x => x.ID == input.LineId && x.SupplierQuotationId == input.QuotationId && x.IsDelete != true, ct);
        if (line == null) return NotFound();
        if (line.Status != PurSupplierQuotationLineStatus.PendingApproval)
        {
            TempData["error"] = "Yalnızca onay bekleyen teklif satırları değerlendirilebilir.";
            return RedirectAfterQuotationDecision(input);
        }
        if (input.Decision != PurQuotationApprovalAction.Approved && input.Decision != PurQuotationApprovalAction.Rejected)
        {
            TempData["error"] = "Geçersiz onay kararı.";
            return RedirectAfterQuotationDecision(input);
        }
        if (input.Decision == PurQuotationApprovalAction.Rejected && string.IsNullOrWhiteSpace(input.Note))
        {
            TempData["error"] = "Reddedilen teklif satırı için açıklama zorunludur.";
            return RedirectAfterQuotationDecision(input);
        }
        var quotation = await _context.PurSupplierQuotations.FirstAsync(x => x.ID == input.QuotationId, ct);
        if (input.Decision == PurQuotationApprovalAction.Approved && quotation.ValidUntil.HasValue && quotation.ValidUntil.Value.Date < DateTime.Today)
        {
            TempData["error"] = "Geçerlilik tarihi dolmuş teklif onaylanamaz. Teklifin güncellenmesi gerekir.";
            return RedirectAfterQuotationDecision(input);
        }
        decimal approvedQuantity = 0;
        if (input.Decision == PurQuotationApprovalAction.Approved && (!TryParseDecimal(input.ApprovedQuantityInput, out approvedQuantity) || approvedQuantity <= 0 || approvedQuantity > line.OfferedQuantity))
        {
            TempData["error"] = "Onay miktarı sıfırdan büyük ve teklif edilen miktardan fazla olmamalıdır.";
            return RedirectAfterQuotationDecision(input);
        }
        var requestLine = await _context.PurPurchaseRequestLines.FirstAsync(x => x.ID == line.PurchaseRequestLineId, ct);
        var alreadyOrdered = await _context.PurPurchaseOrderLines.AsNoTracking()
            .Where(x => x.PurchaseRequestLineId == requestLine.ID && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .SumAsync(x => (decimal?)x.OrderedQuantity, ct) ?? 0;
        if (input.Decision == PurQuotationApprovalAction.Approved && approvedQuantity > requestLine.ApprovedQuantity - alreadyOrdered)
        {
            TempData["error"] = $"Kalan talep miktarı {Math.Max(0, requestLine.ApprovedQuantity - alreadyOrdered):0.######}. Bu miktar aşılamaz.";
            return RedirectAfterQuotationDecision(input);
        }

        var now = DateTime.Now;
        var previous = line.Status;
        var previousQuantity = line.ApprovedQuantity;
        var affectedQuotationIds = new HashSet<int> { quotation.ID };
        if (input.Decision == PurQuotationApprovalAction.Rejected)
        {
            line.Status = PurSupplierQuotationLineStatus.Rejected; line.ApprovedQuantity = 0;
        }
        else
        {
            line.Status = PurSupplierQuotationLineStatus.Ordered; line.ApprovedQuantity = approvedQuantity;
            var order = await _context.PurPurchaseOrders.FirstOrDefaultAsync(x => x.SourceQuotationId == quotation.ID, ct);
            if (order == null)
            {
                order = new PurPurchaseOrder
                {
                    OrderNumber = $"SS-{now:yyyyMMddHHmmssfff}", SupplierId = quotation.SupplierId, SourceQuotationId = quotation.ID,
                    OrderDate = now.Date, CurrencyCode = quotation.CurrencyCode, ExchangeRate = quotation.ExchangeRate,
                    ExchangeRateDate = quotation.ExchangeRateDate, ExchangeRateSource = quotation.ExchangeRateSource,
                    FreightCurrencyCode = quotation.CurrencyCode, FreightExchangeRate = quotation.ExchangeRate,
                    FreightExchangeRateDate = quotation.ExchangeRateDate, FreightExchangeRateSource = quotation.ExchangeRateSource,
                    Status = PurPurchaseOrderStatus.Open,
                    PaymentTerms = quotation.PaymentTerms, DeliveryTerms = quotation.DeliveryTerms, Notes = quotation.Notes,
                    IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser
                };
                _context.PurPurchaseOrders.Add(order);
                await _context.SaveChangesAsync(ct);
            }
            else if (order.IsDelete == true)
            {
                order.SupplierId = quotation.SupplierId;
                order.OrderDate = now.Date;
                order.CurrencyCode = quotation.CurrencyCode;
                order.ExchangeRate = quotation.ExchangeRate;
                order.ExchangeRateDate = quotation.ExchangeRateDate;
                order.ExchangeRateSource = quotation.ExchangeRateSource;
                order.FreightCurrencyCode = quotation.CurrencyCode;
                order.FreightExchangeRate = quotation.ExchangeRate;
                order.FreightExchangeRateDate = quotation.ExchangeRateDate;
                order.FreightExchangeRateSource = quotation.ExchangeRateSource;
                order.TransportationType = null;
                order.FreightPaymentType = null;
                order.DeliveryWarehouseId = null;
                order.DeliveryAddress = null;
                order.CarrierName = null;
                order.EstimatedFreightAmount = null;
                order.EstimatedFreightVatRate = null;
                order.PlannedShipmentDate = null;
                order.PlannedDeliveryDate = null;
                order.TrackingNumber = null;
                order.TransportationNotes = null;
                order.Status = PurPurchaseOrderStatus.Open;
                order.PaymentTerms = quotation.PaymentTerms;
                order.DeliveryTerms = quotation.DeliveryTerms;
                order.Notes = quotation.Notes;
                order.IsActive = true;
                order.IsDelete = false;
                order.DeleteDate = null;
                order.DeleteUserID = null;
                order.UpdateDate = now;
                order.UpdateUserID = CurrentUser;
                await _context.SaveChangesAsync(ct);
            }
            var sequence = await _context.PurPurchaseOrderLines.CountAsync(x => x.PurchaseOrderId == order.ID && x.IsDelete != true, ct) + 1;
            var netTotal = line.NetUnitPrice * approvedQuantity;
            var taxTotal = netTotal * line.VatRate / 100m;
            var orderLine = await _context.PurPurchaseOrderLines.FirstOrDefaultAsync(x => x.SupplierQuotationLineId == line.ID, ct);
            if (orderLine == null)
            {
                orderLine = new PurPurchaseOrderLine
                {
                    SupplierQuotationLineId = line.ID,
                    CreateDate = now,
                    CreateUserID = CurrentUser
                };
                _context.PurPurchaseOrderLines.Add(orderLine);
            }
            orderLine.PurchaseOrderId = order.ID;
            orderLine.PurchaseRequestLineId = line.PurchaseRequestLineId;
            orderLine.Sequence = sequence;
            orderLine.MaterialId = line.MaterialId;
            orderLine.UnitId = line.UnitId;
            orderLine.OrderedQuantity = approvedQuantity;
            orderLine.ReceivedQuantity = 0;
            orderLine.UnitPrice = line.UnitPrice;
            orderLine.DiscountRate = line.DiscountRate;
            orderLine.NetUnitPrice = line.NetUnitPrice;
            orderLine.VatRate = line.VatRate;
            orderLine.NetTotal = netTotal;
            orderLine.TaxTotal = taxTotal;
            orderLine.GrandTotal = netTotal + taxTotal;
            orderLine.RequestedDeliveryDate = line.DeliveryDate;
            orderLine.Status = PurPurchaseOrderLineStatus.Open;
            orderLine.Notes = line.Notes;
            orderLine.IsActive = true;
            orderLine.IsDelete = false;
            orderLine.DeleteDate = null;
            orderLine.DeleteUserID = null;
            orderLine.UpdateDate = now;
            orderLine.UpdateUserID = CurrentUser;
            await _context.SaveChangesAsync(ct);
            await RecalculatePurchaseOrderTotalsAsync(order.ID, now, CurrentUser, ct);

        }
        line.ApprovedDate = now; line.ApprovedUserId = CurrentUser; line.ApprovalNote = Clean(input.Note); line.UpdateDate = now; line.UpdateUserID = CurrentUser;
        _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(quotation.ID, line, input.Decision, previous, line.Status, previousQuantity, input.Note, now, CurrentUser));
        await _context.SaveChangesAsync(ct);
        var reconciledQuotationIds = await ReconcileQuotationAlternativesAsync(requestLine.PurchaseRequestId, now, CurrentUser, ct);
        foreach (var reconciledQuotationId in reconciledQuotationIds) affectedQuotationIds.Add(reconciledQuotationId);
        foreach (var affectedQuotationId in affectedQuotationIds)
            await RecalculateQuotationStatusAsync(affectedQuotationId, now, CurrentUser, ct);
        await RecalculateRequestAfterOrdersAsync(requestLine.PurchaseRequestId, now, CurrentUser, ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = input.Decision == PurQuotationApprovalAction.Approved ? "Teklif satırı onaylandı ve satınalma siparişine aktarıldı." : "Teklif satırı reddedildi.";
        return RedirectAfterQuotationDecision(input);
    }

    [HttpGet]
    public async Task<IActionResult> Siparisler(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await (from order in _context.PurPurchaseOrders.AsNoTracking()
                           join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                           join quotation in _context.PurSupplierQuotations.AsNoTracking() on order.SourceQuotationId equals quotation.ID
                           where order.IsDelete != true
                           orderby order.ID descending
                           select new PurchaseOrderListVM
                           {
                               Id = order.ID, OrderNumber = order.OrderNumber, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                                QuotationNumber = quotation.QuotationNumber, OrderDate = order.OrderDate, CurrencyCode = order.CurrencyCode,
                                ExchangeRate = order.ExchangeRate,
                               GrandTotal = order.GrandTotal, Status = order.Status,
                               HasTransportationPlan = order.TransportationType.HasValue && order.FreightPaymentType.HasValue,
                               LineCount = _context.PurPurchaseOrderLines.Count(x => x.PurchaseOrderId == order.ID && x.IsDelete != true)
                           }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SiparisDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await (from order in _context.PurPurchaseOrders.AsNoTracking()
                           join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                           join quotation in _context.PurSupplierQuotations.AsNoTracking() on order.SourceQuotationId equals quotation.ID
                           where order.ID == id && order.IsDelete != true
                           select new PurchaseOrderDetailVM
                           {
                               Id = order.ID, OrderNumber = order.OrderNumber, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                               QuotationId = quotation.ID, QuotationNumber = quotation.QuotationNumber, OrderDate = order.OrderDate,
                                CurrencyCode = order.CurrencyCode, ExchangeRate = order.ExchangeRate,
                                ExchangeRateDate = order.ExchangeRateDate, ExchangeRateSource = order.ExchangeRateSource,
                               Status = order.Status, NetTotal = order.NetTotal,
                               TaxTotal = order.TaxTotal, GrandTotal = order.GrandTotal, PaymentTerms = order.PaymentTerms, DeliveryTerms = order.DeliveryTerms
                           }).FirstOrDefaultAsync(ct);
        if (model == null) return NotFound();

        var transportation = await _context.PurPurchaseOrders.AsNoTracking()
            .Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new PurchaseOrderTransportationFormVM
            {
                OrderId = x.ID,
                TransportationType = x.TransportationType,
                FreightPaymentType = x.FreightPaymentType,
                DeliveryWarehouseId = x.DeliveryWarehouseId,
                DeliveryAddress = x.DeliveryAddress,
                CarrierName = x.CarrierName,
                EstimatedFreightAmount = x.EstimatedFreightAmount,
                EstimatedFreightVatRate = x.EstimatedFreightVatRate,
                FreightCurrencyCode = x.FreightCurrencyCode,
                FreightExchangeRate = x.FreightExchangeRate,
                FreightExchangeRateDate = x.FreightExchangeRateDate,
                FreightExchangeRateSource = x.FreightExchangeRateSource,
                PlannedShipmentDate = x.PlannedShipmentDate,
                PlannedDeliveryDate = x.PlannedDeliveryDate,
                TrackingNumber = x.TrackingNumber,
                TransportationNotes = x.TransportationNotes
            }).FirstAsync(ct);
        transportation.CanEdit = IsAdmin;
        transportation.EstimatedFreightAmountInput = transportation.EstimatedFreightAmount?.ToString("0.######", CultureInfo.InvariantCulture);
        transportation.EstimatedFreightVatRateInput = transportation.EstimatedFreightVatRate?.ToString("0.##", CultureInfo.InvariantCulture) ?? "20";
        transportation.FreightExchangeRateInput = transportation.FreightExchangeRate.ToString("0.######", CultureInfo.InvariantCulture);
        await FillPurchaseOrderTransportationOptionsAsync(transportation, ct);
        model.Transportation = transportation;

        model.Lines = await (from line in _context.PurPurchaseOrderLines.AsNoTracking()
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where line.PurchaseOrderId == id && line.IsDelete != true
                             orderby line.Sequence
                             select new PurchaseOrderDetailLineVM
                             {
                                 Sequence = line.Sequence, MaterialCode = material.Code, MaterialName = material.Name,
                                 OrderedQuantity = line.OrderedQuantity, ReceivedQuantity = line.ReceivedQuantity, Unit = unit.Name,
                                 UnitPrice = line.UnitPrice, DiscountRate = line.DiscountRate, NetUnitPrice = line.NetUnitPrice,
                                 VatRate = line.VatRate, GrandTotal = line.GrandTotal, RequestedDeliveryDate = line.RequestedDeliveryDate, Status = line.Status
                             }).ToListAsync(ct);
        model.Receipts = await _context.PurGoodsReceipts.AsNoTracking()
            .Where(x => x.PurchaseOrderId == id && x.IsDelete != true)
            .OrderByDescending(x => x.ReceiptDate).ThenByDescending(x => x.ID)
            .Select(x => new GoodsReceiptListVM
            {
                Id = x.ID,
                ReceiptNumber = x.ReceiptNumber,
                OrderNumber = model.OrderNumber,
                SupplierCode = model.SupplierCode,
                SupplierName = model.SupplierName,
                ReceiptDate = x.ReceiptDate,
                DispatchNumber = x.DispatchNumber,
                LineCount = _context.PurGoodsReceiptLines.Count(y => y.GoodsReceiptId == x.ID && y.IsDelete != true),
                Status = x.Status,
                ActualFreightAmount = x.ActualFreightAmount,
                FreightCurrencyCode = x.FreightCurrencyCode
            }).ToListAsync(ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> SiparisSil(int id, CancellationToken ct)
    {
        var order = await _context.PurPurchaseOrders.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (order == null) return NotFound();
        if (await HasPostedGoodsReceiptForOrderAsync(order.ID, ct))
        {
            TempData["error"] = "Bu siparişe bağlı mal kabul karantina stoğuna işlendiği için sipariş silinemez.";
            return RedirectToAction(nameof(SiparisDetay), new { id });
        }
        var quotation = await _context.PurSupplierQuotations.FirstAsync(x => x.ID == order.SourceQuotationId, ct);
        var requestId = quotation.PurchaseRequestId;
        var now = DateTime.Now;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        await DeleteOrderAndReopenQuotationAsync(order, now, CurrentUser, ct);
        await _context.SaveChangesAsync(ct);
        await ReconcileQuotationAlternativesAsync(requestId, now, CurrentUser, ct);
        await RecalculateQuotationStatusAsync(quotation.ID, now, CurrentUser, ct);
        await RecalculateRequestAfterOrdersAsync(requestId, now, CurrentUser, ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{order.OrderNumber} silindi; kaynak teklif yeniden değerlendirmeye açıldı.";
        return RedirectToAction(nameof(TeklifDetay), new { id = quotation.ID });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> SiparisNakliyeKaydet([Bind(Prefix = "Transportation")] PurchaseOrderTransportationFormVM model, CancellationToken ct)
    {
        var order = await _context.PurPurchaseOrders.FirstOrDefaultAsync(x => x.ID == model.OrderId && x.IsDelete != true, ct);
        if (order == null) return NotFound();

        var errors = new List<string>();
        if (!ModelState.IsValid) errors.Add("Nakliye alanlarından biri izin verilen uzunluğu aşıyor.");
        if (!model.TransportationType.HasValue) errors.Add("Nakliye şeklini seçiniz.");
        if (!model.FreightPaymentType.HasValue) errors.Add("Nakliye bedelini kimin karşılayacağını seçiniz.");
        if (model.DeliveryWarehouseId.HasValue && !await _context.PrdWarehouses.AsNoTracking().AnyAsync(x => x.ID == model.DeliveryWarehouseId && x.IsActive != false && x.IsDelete != true, ct))
            errors.Add("Geçerli ve aktif bir teslimat deposu seçiniz.");
        if (model.PlannedShipmentDate.HasValue && model.PlannedDeliveryDate.HasValue && model.PlannedDeliveryDate.Value.Date < model.PlannedShipmentDate.Value.Date)
            errors.Add("Tahmini teslim tarihi tahmini sevk tarihinden önce olamaz.");

        decimal? estimatedFreightAmount = null;
        decimal? estimatedFreightVatRate = null;
        var freightCurrencyCode = "TRY";
        var freightExchangeRate = 1m;
        DateTime? freightExchangeRateDate = null;
        var freightExchangeRateSource = "Sabit";

        if (model.FreightPaymentType == PurFreightPaymentType.Buyer)
        {
            freightExchangeRateDate = model.FreightExchangeRateDate?.Date;
            if (!string.IsNullOrWhiteSpace(model.EstimatedFreightAmountInput))
            {
                if (!TryParseDecimal(model.EstimatedFreightAmountInput, out var amount) || amount < 0)
                    errors.Add("Tahmini nakliye tutarı sıfır veya daha büyük olmalıdır.");
                else
                    estimatedFreightAmount = amount;
            }

            if (!string.IsNullOrWhiteSpace(model.EstimatedFreightVatRateInput))
            {
                if (!TryParseDecimal(model.EstimatedFreightVatRateInput, out var vatRate) || vatRate < 0 || vatRate > 100)
                    errors.Add("Nakliye KDV oranı 0 ile 100 arasında olmalıdır.");
                else
                    estimatedFreightVatRate = vatRate;
            }
            else
            {
                estimatedFreightVatRate = 0m;
            }

            freightCurrencyCode = model.FreightCurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
            if (!new[] { "TRY", "USD", "EUR", "GBP" }.Contains(freightCurrencyCode))
                errors.Add("Geçerli bir nakliye para birimi seçiniz.");
            if (freightCurrencyCode == "TRY")
            {
                freightExchangeRate = 1m;
                freightExchangeRateSource = "Sabit";
                freightExchangeRateDate ??= DateTime.Today;
            }
            else if (!TryParseDecimal(model.FreightExchangeRateInput, out freightExchangeRate) || freightExchangeRate <= 0)
            {
                errors.Add("Nakliye döviz kuru sıfırdan büyük olmalıdır.");
            }
            else
            {
                freightExchangeRateSource = string.Equals(model.FreightExchangeRateSource, "TCMB", StringComparison.OrdinalIgnoreCase) ? "TCMB" : "Manuel";
                if (!freightExchangeRateDate.HasValue) errors.Add("Nakliye kur tarihi zorunludur.");
            }
        }

        if (errors.Count > 0)
        {
            TempData["error"] = string.Join(" ", errors);
            return RedirectToAction(nameof(SiparisDetay), new { id = model.OrderId });
        }

        order.TransportationType = model.TransportationType;
        order.FreightPaymentType = model.FreightPaymentType;
        order.DeliveryWarehouseId = model.DeliveryWarehouseId;
        order.DeliveryAddress = Clean(model.DeliveryAddress);
        order.CarrierName = Clean(model.CarrierName);
        order.EstimatedFreightAmount = estimatedFreightAmount;
        order.EstimatedFreightVatRate = estimatedFreightVatRate;
        order.FreightCurrencyCode = freightCurrencyCode;
        order.FreightExchangeRate = freightExchangeRate;
        order.FreightExchangeRateDate = freightExchangeRateDate;
        order.FreightExchangeRateSource = freightExchangeRateSource;
        order.PlannedShipmentDate = model.PlannedShipmentDate?.Date;
        order.PlannedDeliveryDate = model.PlannedDeliveryDate?.Date;
        order.TrackingNumber = Clean(model.TrackingNumber);
        order.TransportationNotes = Clean(model.TransportationNotes);
        order.UpdateDate = DateTime.Now;
        order.UpdateUserID = CurrentUser;
        await _context.SaveChangesAsync(ct);

        TempData["success"] = "Planlanan nakliye bilgileri kaydedildi.";
        return RedirectToAction(nameof(SiparisDetay), new { id = model.OrderId });
    }

    [HttpGet]
    public async Task<IActionResult> MalKabuller(CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await (from receipt in _context.PurGoodsReceipts.AsNoTracking()
                           join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
                           join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                           where receipt.IsDelete != true && order.IsDelete != true
                           orderby receipt.ID descending
                           select new GoodsReceiptListVM
                           {
                               Id = receipt.ID,
                               ReceiptNumber = receipt.ReceiptNumber,
                               OrderNumber = order.OrderNumber,
                               SupplierCode = supplier.Code,
                               SupplierName = supplier.Name,
                               ReceiptDate = receipt.ReceiptDate,
                               DispatchNumber = receipt.DispatchNumber,
                               LineCount = _context.PurGoodsReceiptLines.Count(x => x.GoodsReceiptId == receipt.ID && x.IsDelete != true),
                               Status = receipt.Status,
                               ActualFreightAmount = receipt.ActualFreightAmount,
                               FreightCurrencyCode = receipt.FreightCurrencyCode
                           }).ToListAsync(ct);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> YeniMalKabul(int orderId, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = new GoodsReceiptFormVM
        {
            PurchaseOrderId = orderId,
            ReceiptDate = DateTime.Today,
            DispatchDate = DateTime.Today,
            FreightExchangeRateDate = DateTime.Today
        };
        if (!await FillGoodsReceiptFormAsync(model, ct)) return NotFound();
        if (model.Status == PurGoodsReceiptStatus.Cancelled) return BadRequest();
        return View("MalKabulFormu", model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> YeniMalKabul(GoodsReceiptFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var validLines = await ValidateGoodsReceiptFormAsync(model, null, ct);
        if (!ModelState.IsValid)
        {
            await FillGoodsReceiptFormAsync(model, ct);
            return View("MalKabulFormu", model);
        }

        var now = DateTime.Now;
        var receipt = new PurGoodsReceipt
        {
            ReceiptNumber = $"MK-{now:yyyyMMddHHmmssfff}",
            PurchaseOrderId = model.PurchaseOrderId,
            Status = PurGoodsReceiptStatus.Recorded,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = CurrentUser
        };
        await SaveGoodsReceiptAsync(receipt, model, validLines, false, ct);
        TempData["success"] = $"{receipt.ReceiptNumber} numaralı parçalı mal kabul kaydedildi. Miktarlar henüz kullanılabilir stoğa alınmadı.";
        return RedirectToAction(nameof(MalKabulDetay), new { id = receipt.ID });
    }

    [HttpGet, Authorize(Roles = "Admin")]
    public async Task<IActionResult> MalKabulDuzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var receipt = await _context.PurGoodsReceipts.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (receipt == null) return NotFound();
        if (receipt.Status != PurGoodsReceiptStatus.Recorded)
        {
            TempData["error"] = "Karantina girişi yapılmış mal kabul doğrudan düzenlenemez. Önce stok hareketinin kontrollü olarak geri alınması gerekir.";
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }
        var model = new GoodsReceiptFormVM
        {
            Id = receipt.ID,
            ReceiptNumber = receipt.ReceiptNumber,
            PurchaseOrderId = receipt.PurchaseOrderId,
            Status = receipt.Status,
            ReceiptDate = receipt.ReceiptDate,
            DispatchNumber = receipt.DispatchNumber,
            DispatchDate = receipt.DispatchDate,
            InvoiceNumber = receipt.InvoiceNumber,
            InvoiceDate = receipt.InvoiceDate,
            CarrierName = receipt.CarrierName,
            VehiclePlate = receipt.VehiclePlate,
            TrackingNumber = receipt.TrackingNumber,
            ActualFreightAmountInput = receipt.ActualFreightAmount?.ToString("0.######", CultureInfo.InvariantCulture),
            ActualFreightVatRateInput = receipt.ActualFreightVatRate?.ToString("0.##", CultureInfo.InvariantCulture) ?? "20",
            FreightCurrencyCode = receipt.FreightCurrencyCode,
            FreightExchangeRateInput = receipt.FreightExchangeRate.ToString("0.######", CultureInfo.InvariantCulture),
            FreightExchangeRateDate = receipt.FreightExchangeRateDate,
            FreightExchangeRateSource = receipt.FreightExchangeRateSource,
            Notes = receipt.Notes,
            Lines = await _context.PurGoodsReceiptLines.AsNoTracking()
                .Where(x => x.GoodsReceiptId == id && x.IsDelete != true)
                .OrderBy(x => x.Sequence)
                .Select(x => new GoodsReceiptFormLineVM
                {
                    Id = x.ID,
                    Include = true,
                    PurchaseOrderLineId = x.PurchaseOrderLineId,
                    ReceivedQuantityInput = x.ReceivedQuantity.ToString("0.######", CultureInfo.InvariantCulture),
                    LotNumber = x.LotNumber,
                    ProductionDate = x.ProductionDate,
                    ExpirationDate = x.ExpirationDate,
                    Notes = x.Notes
                }).ToListAsync(ct)
        };
        if (!await FillGoodsReceiptFormAsync(model, ct)) return NotFound();
        return View("MalKabulFormu", model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> MalKabulDuzenle(GoodsReceiptFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        if (!model.Id.HasValue) return BadRequest();
        var receipt = await _context.PurGoodsReceipts.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (receipt == null) return NotFound();
        if (receipt.Status != PurGoodsReceiptStatus.Recorded)
        {
            TempData["error"] = "Karantina girişi yapılmış mal kabul doğrudan düzenlenemez.";
            return RedirectToAction(nameof(MalKabulDetay), new { id = receipt.ID });
        }
        if (receipt.PurchaseOrderId != model.PurchaseOrderId) return BadRequest();

        var validLines = await ValidateGoodsReceiptFormAsync(model, receipt.ID, ct);
        if (!ModelState.IsValid)
        {
            await FillGoodsReceiptFormAsync(model, ct);
            return View("MalKabulFormu", model);
        }

        await SaveGoodsReceiptAsync(receipt, model, validLines, true, ct);
        TempData["success"] = $"{receipt.ReceiptNumber} numaralı mal kabul kaydı güncellendi; sipariş teslim miktarları yeniden hesaplandı.";
        return RedirectToAction(nameof(MalKabulDetay), new { id = receipt.ID });
    }

    [HttpGet]
    public async Task<IActionResult> MalKabulDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await (from receipt in _context.PurGoodsReceipts.AsNoTracking()
                           join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
                           join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                           where receipt.ID == id && receipt.IsDelete != true
                           select new GoodsReceiptDetailVM
                           {
                               Id = receipt.ID,
                               ReceiptNumber = receipt.ReceiptNumber,
                               PurchaseOrderId = order.ID,
                               OrderNumber = order.OrderNumber,
                               SupplierCode = supplier.Code,
                               SupplierName = supplier.Name,
                               ReceiptDate = receipt.ReceiptDate,
                               DispatchNumber = receipt.DispatchNumber,
                               DispatchDate = receipt.DispatchDate,
                               InvoiceNumber = receipt.InvoiceNumber,
                               InvoiceDate = receipt.InvoiceDate,
                               Status = receipt.Status,
                               CarrierName = receipt.CarrierName,
                               VehiclePlate = receipt.VehiclePlate,
                               TrackingNumber = receipt.TrackingNumber,
                               ActualFreightAmount = receipt.ActualFreightAmount,
                               ActualFreightVatRate = receipt.ActualFreightVatRate,
                               FreightCurrencyCode = receipt.FreightCurrencyCode,
                               FreightExchangeRate = receipt.FreightExchangeRate,
                               FreightExchangeRateDate = receipt.FreightExchangeRateDate,
                               FreightExchangeRateSource = receipt.FreightExchangeRateSource,
                               QuarantineWarehouseId = receipt.QuarantineWarehouseId,
                               QuarantineInventoryDocumentId = receipt.QuarantineInventoryDocumentId,
                               QuarantineDate = receipt.QuarantineDate,
                               QuarantineUserId = receipt.QuarantineUserId,
                               Notes = receipt.Notes
                           }).FirstOrDefaultAsync(ct);
        if (model == null) return NotFound();
        model.Lines = await (from line in _context.PurGoodsReceiptLines.AsNoTracking()
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where line.GoodsReceiptId == id && line.IsDelete != true
                             orderby line.Sequence
                             select new GoodsReceiptDetailLineVM
                             {
                                 Sequence = line.Sequence,
                                 MaterialCode = material.Code,
                                 MaterialName = material.Name,
                                 ReceivedQuantity = line.ReceivedQuantity,
                                 Unit = unit.Name,
                                 LotRequired = material.Type == PrdMaterialType.RawMaterial || material.Type == PrdMaterialType.Packaging || material.RequiresLotTracking,
                                 ExpirationDateRequired = material.RequiresExpirationDate,
                                 LotNumber = line.LotNumber,
                                 ProductionDate = line.ProductionDate,
                                 ExpirationDate = line.ExpirationDate,
                                 QuarantineStockLotId = line.QuarantineStockLotId,
                                 Notes = line.Notes
                             }).ToListAsync(ct);
        model.QuarantineWarehouses = await _context.PrdWarehouses.AsNoTracking()
            .Where(x => x.Type == PrdWarehouseType.Quarantine && x.IsActive != false && x.IsDelete != true)
            .OrderBy(x => x.Code)
            .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.ID.ToString(), Text = x.Code + " - " + x.Name })
            .ToListAsync(ct);
        if (model.QuarantineWarehouses.Count == 1) model.QuarantineWarehouses[0].Selected = true;
        if (model.QuarantineWarehouseId.HasValue)
        {
            var warehouse = await _context.PrdWarehouses.AsNoTracking().FirstOrDefaultAsync(x => x.ID == model.QuarantineWarehouseId, ct);
            model.QuarantineWarehouseCode = warehouse?.Code;
            model.QuarantineWarehouseName = warehouse?.Name;
        }
        if (model.QuarantineInventoryDocumentId.HasValue)
            model.QuarantineDocumentNumber = await _context.PrdInventoryDocuments.AsNoTracking()
                .Where(x => x.ID == model.QuarantineInventoryDocumentId)
                .Select(x => x.DocumentNumber)
                .FirstOrDefaultAsync(ct);
        model.QualityInspectionCount = await _context.PurQualityInspections.AsNoTracking().CountAsync(x => x.GoodsReceiptId == id && x.IsDelete != true, ct);
        model.PendingQualityInspectionCount = await _context.PurQualityInspections.AsNoTracking().CountAsync(x => x.GoodsReceiptId == id && x.IsDelete != true && (x.Status == PrdQualityControlStatus.Pending || x.Status == PrdQualityControlStatus.Sampled), ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> MalKabulSil(int id, CancellationToken ct)
    {
        var receipt = await _context.PurGoodsReceipts.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (receipt == null) return NotFound();
        if (receipt.Status != PurGoodsReceiptStatus.Recorded || receipt.QuarantineInventoryDocumentId.HasValue)
        {
            TempData["error"] = "Stok hareketi bulunan mal kabul silinemez. Önce karantina girişinin kontrollü geri alma işlemi yapılmalıdır.";
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }
        var now = DateTime.Now;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var lines = await _context.PurGoodsReceiptLines.Where(x => x.GoodsReceiptId == id && x.IsDelete != true).ToListAsync(ct);
        foreach (var line in lines) SoftDelete(line, now, CurrentUser);
        SoftDelete(receipt, now, CurrentUser);
        await _context.SaveChangesAsync(ct);
        await RecalculatePurchaseOrderReceiptStateAsync(receipt.PurchaseOrderId, now, CurrentUser, ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{receipt.ReceiptNumber} silindi; sipariş teslim miktarları yeniden hesaplandı.";
        return RedirectToAction(nameof(SiparisDetay), new { id = receipt.PurchaseOrderId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KarantinayaAl(int id, int quarantineWarehouseId, CancellationToken ct)
    {
        var receipt = await _context.PurGoodsReceipts.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (receipt == null) return NotFound();
        if (receipt.Status != PurGoodsReceiptStatus.Recorded || receipt.QuarantineInventoryDocumentId.HasValue)
        {
            TempData["error"] = "Bu mal kabul daha önce karantinaya alınmış veya artık bu işleme uygun değil.";
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }

        var warehouse = await _context.PrdWarehouses.FirstOrDefaultAsync(x => x.ID == quarantineWarehouseId && x.Type == PrdWarehouseType.Quarantine && x.IsActive != false && x.IsDelete != true, ct);
        if (warehouse == null)
        {
            TempData["error"] = "Aktif bir karantina deposu seçiniz.";
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }

        var order = await _context.PurPurchaseOrders.FirstOrDefaultAsync(x => x.ID == receipt.PurchaseOrderId && x.IsDelete != true, ct);
        if (order == null) return NotFound();
        var rows = await (from receiptLine in _context.PurGoodsReceiptLines
                          join orderLine in _context.PurPurchaseOrderLines on receiptLine.PurchaseOrderLineId equals orderLine.ID
                          join material in _context.PrdMaterials on receiptLine.MaterialId equals material.ID
                          where receiptLine.GoodsReceiptId == receipt.ID && receiptLine.IsDelete != true && orderLine.IsDelete != true
                          orderby receiptLine.Sequence
                          select new { ReceiptLine = receiptLine, OrderLine = orderLine, Material = material }).ToListAsync(ct);
        if (rows.Count == 0)
        {
            TempData["error"] = "Karantinaya alınacak mal kabul satırı bulunamadı.";
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }

        var validationErrors = new List<string>();
        foreach (var row in rows)
        {
            var lotRequired = row.Material.Type is PrdMaterialType.RawMaterial or PrdMaterialType.Packaging || row.Material.RequiresLotTracking;
            if (lotRequired && string.IsNullOrWhiteSpace(row.ReceiptLine.LotNumber))
                validationErrors.Add($"{row.Material.Code} için lot numarası zorunludur.");
            if (row.Material.RequiresExpirationDate && !row.ReceiptLine.ExpirationDate.HasValue)
                validationErrors.Add($"{row.Material.Code} için son kullanma tarihi zorunludur.");
        }
        if (validationErrors.Count > 0)
        {
            TempData["error"] = string.Join(" ", validationErrors.Distinct());
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }

        var lotKeys = rows.Select(x => new { x.ReceiptLine.MaterialId, LotNumber = x.ReceiptLine.LotNumber!.Trim() }).Distinct().ToList();
        var materialIds = lotKeys.Select(x => x.MaterialId).Distinct().ToList();
        var lotNumbers = lotKeys.Select(x => x.LotNumber).Distinct().ToList();
        var existingLots = await _context.PrdStockLots
            .Where(x => x.WarehouseId == warehouse.ID && materialIds.Contains(x.MaterialId) && lotNumbers.Contains(x.LotNumber) && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var row in rows)
        {
            var lotNumber = row.ReceiptLine.LotNumber!.Trim();
            var existingLot = existingLots.FirstOrDefault(x => x.MaterialId == row.ReceiptLine.MaterialId && x.LotNumber == lotNumber);
            if (existingLot == null) continue;
            if (existingLot.ProductionDate.HasValue && row.ReceiptLine.ProductionDate.HasValue && existingLot.ProductionDate.Value.Date != row.ReceiptLine.ProductionDate.Value.Date)
                validationErrors.Add($"{row.Material.Code} / {lotNumber} lotunun üretim tarihi mevcut stok lotuyla uyuşmuyor.");
            if (existingLot.ExpirationDate.HasValue && row.ReceiptLine.ExpirationDate.HasValue && existingLot.ExpirationDate.Value.Date != row.ReceiptLine.ExpirationDate.Value.Date)
                validationErrors.Add($"{row.Material.Code} / {lotNumber} lotunun SKT bilgisi mevcut stok lotuyla uyuşmuyor.");
        }
        if (validationErrors.Count > 0)
        {
            TempData["error"] = string.Join(" ", validationErrors.Distinct());
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }

        var now = DateTime.Now;
        var user = CurrentUser;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        var document = new PrdInventoryDocument
        {
            DocumentNumber = $"PKAR-{receipt.ID}-{now:yyyyMMddHHmmssfff}",
            Type = PrdInventoryDocumentType.PurchaseReceipt,
            Status = PrdInventoryDocumentStatus.Posted,
            DocumentDate = receipt.ReceiptDate.Date,
            PostingDate = now,
            PostedUserId = user,
            TargetWarehouseId = warehouse.ID,
            CurrencyCode = "TRY",
            ExchangeRate = 1m,
            TotalCost = rows.Sum(x => x.ReceiptLine.ReceivedQuantity * x.OrderLine.NetUnitPrice * order.ExchangeRate),
            SourceDocumentType = "PurGoodsReceipt",
            SourceDocumentId = receipt.ID,
            Notes = $"{receipt.ReceiptNumber} / {receipt.DispatchNumber} karantina girişi",
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        };
        _context.PrdInventoryDocuments.Add(document);
        await _context.SaveChangesAsync(ct);

        var lotMap = existingLots.ToDictionary(x => $"{x.MaterialId}|{x.LotNumber}", StringComparer.OrdinalIgnoreCase);
        var sequence = 0;
        foreach (var row in rows)
        {
            var lotNumber = row.ReceiptLine.LotNumber!.Trim();
            var key = $"{row.ReceiptLine.MaterialId}|{lotNumber}";
            if (!lotMap.TryGetValue(key, out var lot))
            {
                lot = new PrdStockLot
                {
                    MaterialId = row.ReceiptLine.MaterialId,
                    WarehouseId = warehouse.ID,
                    LotNumber = lotNumber,
                    ProductionDate = row.ReceiptLine.ProductionDate?.Date,
                    ExpirationDate = row.ReceiptLine.ExpirationDate?.Date,
                    IsActive = true,
                    IsDelete = false,
                    CreateDate = now,
                    CreateUserID = user
                };
                _context.PrdStockLots.Add(lot);
                await _context.SaveChangesAsync(ct);
                lotMap[key] = lot;
            }
            else
            {
                if (!lot.ProductionDate.HasValue && row.ReceiptLine.ProductionDate.HasValue) lot.ProductionDate = row.ReceiptLine.ProductionDate.Value.Date;
                if (!lot.ExpirationDate.HasValue && row.ReceiptLine.ExpirationDate.HasValue) lot.ExpirationDate = row.ReceiptLine.ExpirationDate.Value.Date;
            }

            var unitCostTry = row.OrderLine.NetUnitPrice * order.ExchangeRate;
            var totalCostTry = unitCostTry * row.ReceiptLine.ReceivedQuantity;
            var documentLine = new PrdInventoryDocumentLine
            {
                InventoryDocumentId = document.ID,
                Sequence = ++sequence,
                MaterialId = row.ReceiptLine.MaterialId,
                UnitId = row.ReceiptLine.UnitId,
                TargetStockLotId = lot.ID,
                LotNumber = lotNumber,
                ProductionDate = row.ReceiptLine.ProductionDate?.Date,
                ExpirationDate = row.ReceiptLine.ExpirationDate?.Date,
                Quantity = row.ReceiptLine.ReceivedQuantity,
                OriginalUnitCost = row.OrderLine.NetUnitPrice,
                CurrencyCode = order.CurrencyCode,
                ExchangeRate = order.ExchangeRate,
                UnitCost = unitCostTry,
                TotalCost = totalCostTry,
                CostSource = PrdStockCostSource.ApprovedOffer,
                Notes = $"{receipt.ReceiptNumber} / {receipt.DispatchNumber}",
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            };
            _context.PrdInventoryDocumentLines.Add(documentLine);
            await _context.SaveChangesAsync(ct);
            _context.PrdStockMovements.Add(new PrdStockMovement
            {
                InventoryDocumentId = document.ID,
                InventoryDocumentLineId = documentLine.ID,
                MaterialId = row.ReceiptLine.MaterialId,
                WarehouseId = warehouse.ID,
                StockLotId = lot.ID,
                Direction = PrdStockDirection.In,
                MovementType = PrdStockMovementType.Purchase,
                Quantity = row.ReceiptLine.ReceivedQuantity,
                UnitId = row.ReceiptLine.UnitId,
                OriginalUnitCost = row.OrderLine.NetUnitPrice,
                CurrencyCode = order.CurrencyCode,
                ExchangeRate = order.ExchangeRate,
                UnitCost = unitCostTry,
                TotalCost = totalCostTry,
                CostSource = PrdStockCostSource.ApprovedOffer,
                MovementDate = receipt.ReceiptDate.Date,
                DocumentNumber = document.DocumentNumber,
                DocumentType = PrdStockDocumentType.InventoryDocument,
                DocumentId = document.ID,
                Description = document.Notes,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            });
            row.ReceiptLine.QuarantineStockLotId = lot.ID;
            row.ReceiptLine.QuarantineInventoryDocumentLineId = documentLine.ID;
            row.ReceiptLine.UpdateDate = now;
            row.ReceiptLine.UpdateUserID = user;
        }
        receipt.Status = PurGoodsReceiptStatus.InQuarantine;
        receipt.QuarantineWarehouseId = warehouse.ID;
        receipt.QuarantineInventoryDocumentId = document.ID;
        receipt.QuarantineDate = now;
        receipt.QuarantineUserId = user;
        receipt.UpdateDate = now;
        receipt.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
        await CreateMissingQualityInspectionsAsync(receipt.ID, now, user, ct);
        await transaction.CommitAsync(ct);

        TempData["success"] = $"{receipt.ReceiptNumber} içindeki {rows.Count} satır {warehouse.Code} - {warehouse.Name} karantina deposuna alındı. Bu stoklar kalite onayı verilene kadar MRP tarafından kullanılmaz.";
        return RedirectToAction(nameof(MalKabulDetay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> KaliteyeGonder(int id, CancellationToken ct)
    {
        var receipt = await _context.PurGoodsReceipts.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (receipt == null) return NotFound();
        if (!receipt.QuarantineWarehouseId.HasValue || receipt.Status != PurGoodsReceiptStatus.InQuarantine)
        {
            TempData["error"] = "Yalnızca karantina deposuna alınmış mal kabuller kalite analizine gönderilebilir.";
            return RedirectToAction(nameof(MalKabulDetay), new { id });
        }
        var created = await CreateMissingQualityInspectionsAsync(receipt.ID, DateTime.Now, CurrentUser, ct);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = created > 0 ? $"{created} lot için kalite analiz kaydı oluşturuldu." : "Bu mal kabulün kalite analiz kayıtları zaten mevcut.";
        return RedirectToAction("Analizler", "KaliteYonetimi", new { goodsReceiptId = id });
    }

    private async Task<int> CreateMissingQualityInspectionsAsync(int goodsReceiptId, DateTime now, string user, CancellationToken ct)
    {
        var receipt = await _context.PurGoodsReceipts.AsNoTracking().FirstAsync(x => x.ID == goodsReceiptId, ct);
        if (!receipt.QuarantineWarehouseId.HasValue) return 0;
        var existingLineIds = await _context.PurQualityInspections.AsNoTracking()
            .Where(x => x.GoodsReceiptId == goodsReceiptId && x.IsDelete != true)
            .Select(x => x.GoodsReceiptLineId)
            .ToListAsync(ct);
        var lines = await _context.PurGoodsReceiptLines.AsNoTracking()
            .Where(x => x.GoodsReceiptId == goodsReceiptId && x.IsDelete != true && x.QuarantineStockLotId.HasValue && !existingLineIds.Contains(x.ID))
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);
        var materialIds = lines.Select(x => x.MaterialId).Distinct().ToList();
        var today = now.Date;
        var activeSets = await _context.PrdMaterialSpecificationSets.AsNoTracking()
            .Where(x => materialIds.Contains(x.MaterialId) && x.Status == PrdSpecificationSetStatus.Active && x.IsDelete != true &&
                        (!x.ValidFrom.HasValue || x.ValidFrom <= today) && (!x.ValidTo.HasValue || x.ValidTo >= today))
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(ct);
        var activeSetByMaterial = activeSets.GroupBy(x => x.MaterialId).ToDictionary(x => x.Key, x => x.First());
        var createdInspections = new List<PurQualityInspection>();
        foreach (var line in lines)
        {
            var inspection = new PurQualityInspection
            {
                InspectionNumber = $"KA-{goodsReceiptId}-{line.ID}-{now:yyyyMMddHHmmssfff}",
                GoodsReceiptId = goodsReceiptId,
                GoodsReceiptLineId = line.ID,
                MaterialId = line.MaterialId,
                StockLotId = line.QuarantineStockLotId!.Value,
                WarehouseId = receipt.QuarantineWarehouseId.Value,
                SpecificationSetId = activeSetByMaterial.TryGetValue(line.MaterialId, out var specificationSet) ? specificationSet.ID : null,
                Status = PrdQualityControlStatus.Pending,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            };
            _context.PurQualityInspections.Add(inspection);
            createdInspections.Add(inspection);
        }
        if (lines.Count > 0)
        {
            await _context.SaveChangesAsync(ct);
            var setIds = createdInspections.Where(x => x.SpecificationSetId.HasValue).Select(x => x.SpecificationSetId!.Value).Distinct().ToList();
            var specificationItems = await _context.PrdMaterialSpecificationItems.AsNoTracking().Where(x => setIds.Contains(x.SpecificationSetId) && x.IsDelete != true).ToListAsync(ct);
            foreach (var inspection in createdInspections.Where(x => x.SpecificationSetId.HasValue))
            {
                foreach (var item in specificationItems.Where(x => x.SpecificationSetId == inspection.SpecificationSetId))
                {
                    _context.PurQualityInspectionSpecificationResults.Add(new PurQualityInspectionSpecificationResult
                    {
                        QualityInspectionId = inspection.ID,
                        SpecificationSetId = inspection.SpecificationSetId!.Value,
                        SpecificationItemId = item.ID,
                        Status = PrdSpecificationResultStatus.Pending,
                        IsActive = true,
                        IsDelete = false,
                        CreateDate = now,
                        CreateUserID = user
                    });
                }
            }
            await _context.SaveChangesAsync(ct);
        }
        return lines.Count;
    }

    private sealed record ValidGoodsReceiptLine(GoodsReceiptFormLineVM Input, PurPurchaseOrderLine OrderLine, decimal Quantity);

    private async Task<List<ValidGoodsReceiptLine>> ValidateGoodsReceiptFormAsync(GoodsReceiptFormVM model, int? currentReceiptId, CancellationToken ct)
    {
        model.Lines ??= [];
        model.DispatchNumber = model.DispatchNumber?.Trim() ?? string.Empty;
        if (model.ReceiptDate == default) ModelState.AddModelError(nameof(model.ReceiptDate), "Mal kabul tarihi zorunludur.");
        if (string.IsNullOrWhiteSpace(model.DispatchNumber)) ModelState.AddModelError(nameof(model.DispatchNumber), "İrsaliye numarası zorunludur.");
        if (model.DispatchDate.HasValue && model.DispatchDate.Value.Date > model.ReceiptDate.Date)
            ModelState.AddModelError(nameof(model.DispatchDate), "İrsaliye tarihi mal kabul tarihinden sonra olamaz.");
        if (model.InvoiceDate.HasValue && string.IsNullOrWhiteSpace(model.InvoiceNumber))
            ModelState.AddModelError(nameof(model.InvoiceNumber), "Fatura tarihi girildiğinde fatura numarası da girilmelidir.");
        if (await _context.PurGoodsReceipts.AsNoTracking().AnyAsync(x => x.PurchaseOrderId == model.PurchaseOrderId && x.DispatchNumber == model.DispatchNumber && x.IsDelete != true && (!currentReceiptId.HasValue || x.ID != currentReceiptId.Value), ct))
            ModelState.AddModelError(nameof(model.DispatchNumber), "Bu siparişte aynı irsaliye numarasıyla daha önce mal kabul yapılmış.");

        var order = await _context.PurPurchaseOrders.AsNoTracking().FirstOrDefaultAsync(x => x.ID == model.PurchaseOrderId && x.IsDelete != true, ct);
        if (order == null || order.Status == PurPurchaseOrderStatus.Cancelled)
            ModelState.AddModelError(nameof(model.PurchaseOrderId), "Açık bir satınalma siparişi bulunamadı.");

        var selected = model.Lines.Where(x => x.Include).ToList();
        if (selected.Count == 0) ModelState.AddModelError(string.Empty, "Teslim alınan en az bir sipariş satırını işaretleyiniz.");
        if (selected.GroupBy(x => x.PurchaseOrderLineId).Any(x => x.Count() > 1))
            ModelState.AddModelError(string.Empty, "Aynı sipariş satırı bu mal kabulde birden fazla kez seçilemez.");

        var orderLineIds = selected.Select(x => x.PurchaseOrderLineId).Distinct().ToList();
        var orderLines = await _context.PurPurchaseOrderLines
            .Where(x => orderLineIds.Contains(x.ID) && x.PurchaseOrderId == model.PurchaseOrderId && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .ToDictionaryAsync(x => x.ID, ct);
        var otherReceived = await (from line in _context.PurGoodsReceiptLines.AsNoTracking()
                                   join receipt in _context.PurGoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals receipt.ID
                                   where orderLineIds.Contains(line.PurchaseOrderLineId) && line.IsDelete != true && receipt.IsDelete != true &&
                                         receipt.Status != PurGoodsReceiptStatus.Cancelled && (!currentReceiptId.HasValue || receipt.ID != currentReceiptId.Value)
                                   group line by line.PurchaseOrderLineId into grouped
                                   select new { grouped.Key, Quantity = grouped.Sum(x => x.ReceivedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var materials = await _context.PrdMaterials.AsNoTracking()
            .Where(x => orderLines.Values.Select(y => y.MaterialId).Contains(x.ID))
            .ToDictionaryAsync(x => x.ID, ct);
        var result = new List<ValidGoodsReceiptLine>();
        foreach (var input in selected)
        {
            var index = model.Lines.IndexOf(input);
            if (!orderLines.TryGetValue(input.PurchaseOrderLineId, out var orderLine))
            {
                ModelState.AddModelError($"Lines[{index}].Include", "Sipariş satırı artık mal kabul için uygun değil.");
                continue;
            }
            otherReceived.TryGetValue(orderLine.ID, out var previouslyReceived);
            var remaining = Math.Max(0, orderLine.OrderedQuantity - previouslyReceived);
            if (!TryParseDecimal(input.ReceivedQuantityInput, out var quantity) || quantity <= 0 || quantity > remaining)
            {
                ModelState.AddModelError($"Lines[{index}].ReceivedQuantityInput", $"Miktar sıfırdan büyük ve kalan {remaining:0.######} miktarı aşmamalıdır.");
                continue;
            }
            if (materials.TryGetValue(orderLine.MaterialId, out var material))
            {
                if ((material.Type is PrdMaterialType.RawMaterial or PrdMaterialType.Packaging || material.RequiresLotTracking) && string.IsNullOrWhiteSpace(input.LotNumber))
                    ModelState.AddModelError($"Lines[{index}].LotNumber", "Bu malzeme için lot numarası zorunludur.");
                if (material.RequiresExpirationDate && !input.ExpirationDate.HasValue)
                    ModelState.AddModelError($"Lines[{index}].ExpirationDate", "Bu malzeme için son kullanma tarihi zorunludur.");
            }
            if (input.ProductionDate.HasValue && input.ExpirationDate.HasValue && input.ExpirationDate.Value.Date < input.ProductionDate.Value.Date)
                ModelState.AddModelError($"Lines[{index}].ExpirationDate", "Son kullanma tarihi üretim tarihinden önce olamaz.");
            result.Add(new ValidGoodsReceiptLine(input, orderLine, quantity));
        }

        model.FreightCurrencyCode = model.FreightCurrencyCode?.Trim().ToUpperInvariant() ?? "TRY";
        if (!string.IsNullOrWhiteSpace(model.ActualFreightAmountInput))
        {
            if (!TryParseDecimal(model.ActualFreightAmountInput, out var freightAmount) || freightAmount < 0)
                ModelState.AddModelError(nameof(model.ActualFreightAmountInput), "Gerçekleşen nakliye tutarı sıfır veya daha büyük olmalıdır.");
            if (!TryParseDecimal(model.ActualFreightVatRateInput, out var freightVat) || freightVat < 0 || freightVat > 100)
                ModelState.AddModelError(nameof(model.ActualFreightVatRateInput), "Nakliye KDV oranı 0 ile 100 arasında olmalıdır.");
            if (!new[] { "TRY", "USD", "EUR", "GBP" }.Contains(model.FreightCurrencyCode))
                ModelState.AddModelError(nameof(model.FreightCurrencyCode), "Geçerli bir nakliye para birimi seçiniz.");
            if (!TryParseDecimal(model.FreightExchangeRateInput, out var freightRate) || freightRate <= 0)
                ModelState.AddModelError(nameof(model.FreightExchangeRateInput), "Nakliye döviz kuru sıfırdan büyük olmalıdır.");
            if (!model.FreightExchangeRateDate.HasValue)
                ModelState.AddModelError(nameof(model.FreightExchangeRateDate), "Nakliye kur tarihi zorunludur.");
        }
        return result;
    }

    private async Task<bool> FillGoodsReceiptFormAsync(GoodsReceiptFormVM model, CancellationToken ct)
    {
        var header = await (from order in _context.PurPurchaseOrders.AsNoTracking()
                            join supplier in _context.PurSuppliers.AsNoTracking() on order.SupplierId equals supplier.ID
                            where order.ID == model.PurchaseOrderId && order.IsDelete != true
                            select new { order.OrderNumber, order.Status, supplier.Code, supplier.Name, order.CarrierName, order.TrackingNumber }).FirstOrDefaultAsync(ct);
        if (header == null) return false;
        model.OrderNumber = header.OrderNumber;
        model.SupplierCode = header.Code;
        model.SupplierName = header.Name;
        if (!model.Id.HasValue)
        {
            model.CarrierName ??= header.CarrierName;
            model.TrackingNumber ??= header.TrackingNumber;
        }

        var currentReceiptId = model.Id;
        var received = await (from line in _context.PurGoodsReceiptLines.AsNoTracking()
                              join receipt in _context.PurGoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals receipt.ID
                              where receipt.PurchaseOrderId == model.PurchaseOrderId && receipt.IsDelete != true && line.IsDelete != true &&
                                    receipt.Status != PurGoodsReceiptStatus.Cancelled && (!currentReceiptId.HasValue || receipt.ID != currentReceiptId.Value)
                              group line by line.PurchaseOrderLineId into grouped
                              select new { grouped.Key, Quantity = grouped.Sum(x => x.ReceivedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var metadata = await (from line in _context.PurPurchaseOrderLines.AsNoTracking()
                              join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                              join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                              where line.PurchaseOrderId == model.PurchaseOrderId && line.IsDelete != true && line.Status != PurPurchaseOrderLineStatus.Cancelled
                              orderby line.Sequence
                              select new
                              {
                                  Line = line,
                                  material.Code,
                                  material.Name,
                                  Unit = unit.Name,
                                  RequiresLotTracking = material.Type == PrdMaterialType.RawMaterial || material.Type == PrdMaterialType.Packaging || material.RequiresLotTracking,
                                  material.RequiresExpirationDate
                              }).ToListAsync(ct);
        var posted = model.Lines.GroupBy(x => x.PurchaseOrderLineId).ToDictionary(x => x.Key, x => x.First());
        var rebuilt = new List<GoodsReceiptFormLineVM>();
        foreach (var item in metadata)
        {
            received.TryGetValue(item.Line.ID, out var previous);
            if (!posted.TryGetValue(item.Line.ID, out var line))
                line = new GoodsReceiptFormLineVM { PurchaseOrderLineId = item.Line.ID };
            line.MaterialCode = item.Code;
            line.MaterialName = item.Name;
            line.Unit = item.Unit;
            line.OrderedQuantity = item.Line.OrderedQuantity;
            line.PreviouslyReceivedQuantity = previous;
            line.RemainingQuantity = Math.Max(0, item.Line.OrderedQuantity - previous);
            line.RequiresLotTracking = item.RequiresLotTracking;
            line.RequiresExpirationDate = item.RequiresExpirationDate;
            rebuilt.Add(line);
        }
        model.Lines = rebuilt;
        await FillGoodsReceiptCurrencyOptionsAsync(model);
        return true;
    }

    private async Task FillGoodsReceiptCurrencyOptionsAsync(GoodsReceiptFormVM model)
    {
        model.Currencies = new[] { "TRY", "USD", "EUR", "GBP" }
            .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x, Text = x })
            .ToList();
        model.CurrentRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["TRY"] = 1m };
        model.CurrentRateDate = DateTime.Today;
        try
        {
            var rates = await _tcmbService.DovizKuruGetir();
            if (TryParseDecimal(rates.UsdSatis, out var usdRate) && usdRate > 0) model.CurrentRates["USD"] = usdRate;
            if (TryParseDecimal(rates.EurSatis, out var eurRate) && eurRate > 0) model.CurrentRates["EUR"] = eurRate;
            if (rates.Tarih != default) model.CurrentRateDate = rates.Tarih.Date;
        }
        catch
        {
            model.RateLoadWarning = "TCMB kurları şu anda alınamadı. TRY dışındaki gerçekleşen nakliye için kuru manuel girebilirsiniz.";
        }
    }

    private async Task SaveGoodsReceiptAsync(PurGoodsReceipt receipt, GoodsReceiptFormVM model, List<ValidGoodsReceiptLine> validLines, bool isEdit, CancellationToken ct)
    {
        var now = DateTime.Now;
        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        receipt.ReceiptDate = model.ReceiptDate.Date;
        receipt.DispatchNumber = model.DispatchNumber.Trim();
        receipt.DispatchDate = model.DispatchDate?.Date;
        receipt.InvoiceNumber = Clean(model.InvoiceNumber);
        receipt.InvoiceDate = model.InvoiceDate?.Date;
        receipt.CarrierName = Clean(model.CarrierName);
        receipt.VehiclePlate = Clean(model.VehiclePlate);
        receipt.TrackingNumber = Clean(model.TrackingNumber);
        receipt.Notes = Clean(model.Notes);
        if (string.IsNullOrWhiteSpace(model.ActualFreightAmountInput))
        {
            receipt.ActualFreightAmount = null;
            receipt.ActualFreightVatRate = null;
            receipt.FreightCurrencyCode = "TRY";
            receipt.FreightExchangeRate = 1m;
            receipt.FreightExchangeRateDate = model.ReceiptDate.Date;
            receipt.FreightExchangeRateSource = "Sabit";
        }
        else
        {
            TryParseDecimal(model.ActualFreightAmountInput, out var freightAmount);
            TryParseDecimal(model.ActualFreightVatRateInput, out var freightVat);
            TryParseDecimal(model.FreightExchangeRateInput, out var freightRate);
            receipt.ActualFreightAmount = freightAmount;
            receipt.ActualFreightVatRate = freightVat;
            receipt.FreightCurrencyCode = model.FreightCurrencyCode;
            receipt.FreightExchangeRate = model.FreightCurrencyCode == "TRY" ? 1m : freightRate;
            receipt.FreightExchangeRateDate = model.FreightExchangeRateDate?.Date;
            receipt.FreightExchangeRateSource = model.FreightCurrencyCode == "TRY" ? "Sabit" :
                (string.Equals(model.FreightExchangeRateSource, "TCMB", StringComparison.OrdinalIgnoreCase) ? "TCMB" : "Manuel");
        }
        if (isEdit)
        {
            receipt.UpdateDate = now;
            receipt.UpdateUserID = CurrentUser;
            var oldLines = await _context.PurGoodsReceiptLines.Where(x => x.GoodsReceiptId == receipt.ID && x.IsDelete != true).ToListAsync(ct);
            foreach (var oldLine in oldLines) SoftDelete(oldLine, now, CurrentUser);
        }
        else
        {
            _context.PurGoodsReceipts.Add(receipt);
            await _context.SaveChangesAsync(ct);
        }

        var sequence = 0;
        foreach (var item in validLines)
        {
            _context.PurGoodsReceiptLines.Add(new PurGoodsReceiptLine
            {
                GoodsReceiptId = receipt.ID,
                PurchaseOrderLineId = item.OrderLine.ID,
                Sequence = ++sequence,
                MaterialId = item.OrderLine.MaterialId,
                UnitId = item.OrderLine.UnitId,
                ReceivedQuantity = item.Quantity,
                LotNumber = Clean(item.Input.LotNumber),
                ProductionDate = item.Input.ProductionDate?.Date,
                ExpirationDate = item.Input.ExpirationDate?.Date,
                Notes = Clean(item.Input.Notes),
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = CurrentUser
            });
        }
        await _context.SaveChangesAsync(ct);
        await RecalculatePurchaseOrderReceiptStateAsync(receipt.PurchaseOrderId, now, CurrentUser, ct);
        await transaction.CommitAsync(ct);
    }

    private async Task RecalculatePurchaseOrderReceiptStateAsync(int orderId, DateTime now, string user, CancellationToken ct)
    {
        var order = await _context.PurPurchaseOrders.FirstAsync(x => x.ID == orderId, ct);
        var orderLines = await _context.PurPurchaseOrderLines.Where(x => x.PurchaseOrderId == orderId && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled).ToListAsync(ct);
        var received = await (from line in _context.PurGoodsReceiptLines.AsNoTracking()
                              join receipt in _context.PurGoodsReceipts.AsNoTracking() on line.GoodsReceiptId equals receipt.ID
                              where receipt.PurchaseOrderId == orderId && receipt.IsDelete != true && receipt.Status != PurGoodsReceiptStatus.Cancelled && line.IsDelete != true
                              group line by line.PurchaseOrderLineId into grouped
                              select new { grouped.Key, Quantity = grouped.Sum(x => x.ReceivedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        foreach (var line in orderLines)
        {
            line.ReceivedQuantity = Math.Min(line.OrderedQuantity, received.GetValueOrDefault(line.ID));
            line.Status = line.ReceivedQuantity >= line.OrderedQuantity
                ? PurPurchaseOrderLineStatus.Received
                : line.ReceivedQuantity > 0 ? PurPurchaseOrderLineStatus.PartiallyReceived : PurPurchaseOrderLineStatus.Open;
            line.UpdateDate = now;
            line.UpdateUserID = user;
        }
        order.Status = orderLines.Count > 0 && orderLines.All(x => x.Status == PurPurchaseOrderLineStatus.Received)
            ? PurPurchaseOrderStatus.Received
            : orderLines.Any(x => x.ReceivedQuantity > 0) ? PurPurchaseOrderStatus.PartiallyReceived : PurPurchaseOrderStatus.Open;
        order.UpdateDate = now;
        order.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
    }

    private string CurrentUser => User.Identity?.Name ?? "system";

    private Task<bool> HasPostedGoodsReceiptForOrderAsync(int orderId, CancellationToken ct) =>
        _context.PurGoodsReceipts.AsNoTracking().AnyAsync(x => x.PurchaseOrderId == orderId && x.IsDelete != true && x.QuarantineInventoryDocumentId.HasValue, ct);

    private Task<bool> HasPostedGoodsReceiptForQuotationAsync(int quotationId, CancellationToken ct) =>
        (from receipt in _context.PurGoodsReceipts.AsNoTracking()
         join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
         where order.SourceQuotationId == quotationId && order.IsDelete != true && receipt.IsDelete != true && receipt.QuarantineInventoryDocumentId.HasValue
         select receipt.ID).AnyAsync(ct);

    private Task<bool> HasPostedGoodsReceiptForRequestAsync(int requestId, CancellationToken ct) =>
        (from receipt in _context.PurGoodsReceipts.AsNoTracking()
         join order in _context.PurPurchaseOrders.AsNoTracking() on receipt.PurchaseOrderId equals order.ID
         join quotation in _context.PurSupplierQuotations.AsNoTracking() on order.SourceQuotationId equals quotation.ID
         where quotation.PurchaseRequestId == requestId && quotation.IsDelete != true && order.IsDelete != true && receipt.IsDelete != true && receipt.QuarantineInventoryDocumentId.HasValue
         select receipt.ID).AnyAsync(ct);
    private bool IsAdmin => User.IsInRole("Admin");
    private bool CanApproveRequests => User.IsInRole("Admin") || User.HasClaim("Authorize", "TalepKabul");
    private bool CanApproveQuotations => User.IsInRole("Admin") || User.HasClaim("Authorize", "TeklifKabul");
    private bool CanEditRequest(PurPurchaseRequest request) => IsAdmin;
    private bool CanEditQuotation(PurSupplierQuotation quotation) => IsAdmin;
    private bool CanSubmitRequest(PurPurchaseRequest request) => IsAdmin || string.Equals(request.RequestedUserId, CurrentUser, StringComparison.OrdinalIgnoreCase);
    private bool CanSubmitQuotation(PurSupplierQuotation quotation) => IsAdmin || string.Equals(quotation.CreateUserID, CurrentUser, StringComparison.OrdinalIgnoreCase);
    private IActionResult RedirectAfterQuotationDecision(QuotationLineDecisionVM input) =>
        input.ReturnToComparison && input.RequestId > 0
            ? RedirectToAction(nameof(TeklifKarsilastir), new { requestId = input.RequestId })
            : RedirectToAction(nameof(TeklifOnay), new { id = input.QuotationId });

    private async Task<List<(PurchaseRequestFormLineVM Input, int UnitId, decimal Quantity)>> ValidateFormAsync(PurchaseRequestFormVM model, CancellationToken ct)
    {
        model.Lines ??= [];
        model.Lines = model.Lines.Where(x => x.MaterialId > 0 || !string.IsNullOrWhiteSpace(x.RequestedQuantityInput) || !string.IsNullOrWhiteSpace(x.Reason)).ToList();
        if (model.RequestDate == default) ModelState.AddModelError(nameof(model.RequestDate), "Talep tarihi zorunludur.");
        if (model.NeededDate.HasValue && model.NeededDate.Value.Date < model.RequestDate.Date) ModelState.AddModelError(nameof(model.NeededDate), "Genel ihtiyaç tarihi talep tarihinden önce olamaz.");
        if (model.Lines.Count == 0) ModelState.AddModelError(string.Empty, "En az bir malzeme eklemelisiniz.");
        if (model.Lines.Where(x => x.MaterialId > 0).GroupBy(x => x.MaterialId).Any(x => x.Count() > 1)) ModelState.AddModelError(string.Empty, "Aynı malzeme talebe birden fazla kez eklenemez.");

        var materialIds = model.Lines.Where(x => x.MaterialId > 0).Select(x => x.MaterialId).Distinct().ToList();
        var materials = await _context.PrdMaterials.AsNoTracking().Where(x => materialIds.Contains(x.ID) && x.IsActive != false && x.IsDelete != true).Select(x => new { x.ID, x.UnitId }).ToDictionaryAsync(x => x.ID, ct);
        var result = new List<(PurchaseRequestFormLineVM Input, int UnitId, decimal Quantity)>();
        for (var index = 0; index < model.Lines.Count; index++)
        {
            var line = model.Lines[index];
            if (!materials.TryGetValue(line.MaterialId, out var material))
            {
                ModelState.AddModelError($"Lines[{index}].MaterialId", "Geçerli ve aktif bir malzeme seçiniz.");
                continue;
            }
            if (!TryParseDecimal(line.RequestedQuantityInput, out var quantity) || quantity <= 0)
            {
                ModelState.AddModelError($"Lines[{index}].RequestedQuantityInput", "Talep miktarı sıfırdan büyük olmalıdır.");
                continue;
            }
            var neededDate = line.NeededDate ?? model.NeededDate;
            if (neededDate.HasValue && neededDate.Value.Date < model.RequestDate.Date)
            {
                ModelState.AddModelError($"Lines[{index}].NeededDate", "İhtiyaç tarihi talep tarihinden önce olamaz.");
            }
            result.Add((line, material.UnitId, quantity));
        }
        return result;
    }

    private async Task FillMaterialOptionsAsync(PurchaseRequestFormVM model, CancellationToken ct)
    {
        model.MaterialOptions = await (from material in _context.PrdMaterials.AsNoTracking()
                                       join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                                       where material.IsActive != false && material.IsDelete != true
                                       orderby material.Code
                                       select new PurchaseMaterialOptionVM { Id = material.ID, Code = material.Code, Name = material.Name, UnitId = unit.ID, Unit = unit.Name }).ToListAsync(ct);
        if (model.Lines.Count == 0) model.Lines.Add(new PurchaseRequestFormLineVM { NeededDate = model.NeededDate });
    }

    private async Task<PurchaseRequestDetailVM?> BuildDetailAsync(int id, CancellationToken ct)
    {
        var model = await _context.PurPurchaseRequests.AsNoTracking().Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new PurchaseRequestDetailVM
            {
                Id = x.ID,
                RequestNumber = x.RequestNumber,
                RequestDate = x.RequestDate,
                NeededDate = x.NeededDate,
                RequestedUserId = x.RequestedUserId,
                Priority = x.Priority,
                Status = x.Status,
                SubmittedDate = x.SubmittedDate,
                SubmittedUserId = x.SubmittedUserId,
                Notes = x.Notes
            }).FirstOrDefaultAsync(ct);
        if (model == null) return null;
        model.CanEdit = IsAdmin;
        model.CanSubmit = IsAdmin || string.Equals(model.RequestedUserId, CurrentUser, StringComparison.OrdinalIgnoreCase);
        model.CanApprove = CanApproveRequests;
        model.Lines = await (from line in _context.PurPurchaseRequestLines.AsNoTracking()
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where line.PurchaseRequestId == id && line.IsDelete != true
                             orderby line.Sequence
                             select new PurchaseRequestDetailLineVM
                             {
                                 Id = line.ID,
                                 Sequence = line.Sequence,
                                 MaterialCode = material.Code,
                                 MaterialName = material.Name,
                                 RequestedQuantity = line.RequestedQuantity,
                                 ApprovedQuantity = line.ApprovedQuantity,
                                 Unit = unit.Name,
                                 NeededDate = line.NeededDate,
                                 Status = line.Status,
                                 Source = line.Source,
                                 Reason = line.Reason,
                                 ApprovedDate = line.ApprovedDate,
                                 ApprovedUserId = line.ApprovedUserId,
                                 ApprovalNote = line.ApprovalNote
                             }).ToListAsync(ct);
        return model;
    }

    private async Task RecalculateRequestStatusAsync(int requestId, DateTime now, string user, CancellationToken ct)
    {
        var request = await _context.PurPurchaseRequests.FirstAsync(x => x.ID == requestId, ct);
        var statuses = await _context.PurPurchaseRequestLines.AsNoTracking().Where(x => x.PurchaseRequestId == requestId && x.IsDelete != true).Select(x => x.Status).ToListAsync(ct);
        if (statuses.Any(x => x == PurPurchaseRequestLineStatus.PendingApproval)) request.Status = PurPurchaseRequestStatus.PendingApproval;
        else if (statuses.Count > 0 && statuses.All(x => x == PurPurchaseRequestLineStatus.Approved)) request.Status = PurPurchaseRequestStatus.Approved;
        else if (statuses.Any(x => x == PurPurchaseRequestLineStatus.Approved)) request.Status = PurPurchaseRequestStatus.PartiallyApproved;
        else request.Status = PurPurchaseRequestStatus.Rejected;
        request.UpdateDate = now;
        request.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
    }

    private static PurPurchaseRequestLine CreateLine(int requestId, int sequence, PurchaseRequestFormLineVM input, int unitId, decimal quantity, DateTime now, string user) => new()
    {
        PurchaseRequestId = requestId,
        Sequence = sequence,
        MaterialId = input.MaterialId,
        UnitId = unitId,
        RequestedQuantity = quantity,
        ApprovedQuantity = 0,
        NeededDate = input.NeededDate?.Date,
        Status = PurPurchaseRequestLineStatus.Draft,
        Source = PurPurchaseRequestSource.Manual,
        Reason = input.Reason?.Trim(),
        IsActive = true,
        IsDelete = false,
        CreateDate = now,
        CreateUserID = user
    };

    private static PurRequestApprovalHistory CreateHistory(int requestId, PurPurchaseRequestLine line, PurRequestApprovalAction action, PurPurchaseRequestLineStatus previousStatus, PurPurchaseRequestLineStatus newStatus, decimal previousApprovedQuantity, string? note, DateTime now, string user) => new()
    {
        PurchaseRequestId = requestId,
        PurchaseRequestLineId = line.ID,
        Action = action,
        PreviousStatus = previousStatus,
        NewStatus = newStatus,
        RequestedQuantity = line.RequestedQuantity,
        PreviousApprovedQuantity = previousApprovedQuantity,
        ApprovedQuantity = line.ApprovedQuantity,
        Note = note?.Trim(),
        ActionDate = now,
        ActionUserId = user,
        IsActive = true,
        IsDelete = false,
        CreateDate = now,
        CreateUserID = user
    };

    private async Task ValidateSupplierAsync(SupplierFormVM model, CancellationToken ct)
    {
        model.Code = model.Code?.Trim() ?? string.Empty;
        model.Name = model.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(model.Code)) ModelState.AddModelError(nameof(model.Code), "Tedarikçi kodu zorunludur.");
        if (string.IsNullOrWhiteSpace(model.Name)) ModelState.AddModelError(nameof(model.Name), "Tedarikçi adı zorunludur.");
        var normalizedCode = model.Code.ToUpperInvariant();
        if (await _context.PurSuppliers.AsNoTracking().AnyAsync(x => x.ID != model.Id && x.IsDelete != true && x.Code == normalizedCode, ct))
            ModelState.AddModelError(nameof(model.Code), "Bu tedarikçi kodu zaten kullanılıyor.");
    }

    private async Task<SupplierQuotationFormVM?> BuildQuotationFormAsync(int requestId, PurSupplierQuotation? quotation, CancellationToken ct)
    {
        var request = await _context.PurPurchaseRequests.AsNoTracking().FirstOrDefaultAsync(x => x.ID == requestId && x.IsDelete != true, ct);
        if (request == null) return null;
        var model = new SupplierQuotationFormVM
        {
            Id = quotation?.ID ?? 0, PurchaseRequestId = request.ID, RequestNumber = request.RequestNumber,
            QuotationNumber = quotation?.QuotationNumber ?? string.Empty, SupplierId = quotation?.SupplierId ?? 0,
            Status = quotation?.Status ?? PurSupplierQuotationStatus.Draft,
            SupplierQuotationNumber = quotation?.SupplierQuotationNumber, QuotationDate = quotation?.QuotationDate ?? DateTime.Today,
            ValidUntil = quotation?.ValidUntil, CurrencyCode = quotation?.CurrencyCode ?? "TRY",
            ExchangeRateInput = quotation?.ExchangeRate.ToString("0.######", CultureInfo.InvariantCulture) ?? "1",
            ExchangeRateDate = quotation?.ExchangeRateDate ?? DateTime.Today,
            ExchangeRateSource = quotation?.ExchangeRateSource ?? "Sabit", PaymentTerms = quotation?.PaymentTerms,
            DeliveryTerms = quotation?.DeliveryTerms, LeadTimeDays = quotation?.LeadTimeDays, Notes = quotation?.Notes
        };
        var requestLines = await _context.PurPurchaseRequestLines.AsNoTracking()
            .Where(x => x.PurchaseRequestId == requestId && x.IsDelete != true &&
                        (x.Status == PurPurchaseRequestLineStatus.Approved ||
                         x.Status == PurPurchaseRequestLineStatus.InQuotation ||
                         x.Status == PurPurchaseRequestLineStatus.Ordered))
            .OrderBy(x => x.Sequence)
            .ToListAsync(ct);
        var requestLineIds = requestLines.Select(x => x.ID).ToList();
        var materialIds = requestLines.Select(x => x.MaterialId).Distinct().ToList();
        var unitIds = requestLines.Select(x => x.UnitId).Distinct().ToList();

        var materials = await _context.PrdMaterials.AsNoTracking()
            .Where(x => materialIds.Contains(x.ID))
            .Select(x => new { x.ID, x.Code, x.Name })
            .ToDictionaryAsync(x => x.ID, ct);
        var units = await _context.PrdUnits.AsNoTracking()
            .Where(x => unitIds.Contains(x.ID))
            .Select(x => new { x.ID, x.Name })
            .ToDictionaryAsync(x => x.ID, ct);
        var existing = quotation == null
            ? new Dictionary<int, PurSupplierQuotationLine>()
            : await _context.PurSupplierQuotationLines.AsNoTracking()
                .Where(x => x.SupplierQuotationId == quotation.ID && x.IsDelete != true)
                .ToDictionaryAsync(x => x.PurchaseRequestLineId, ct);
        var ownOrderIds = quotation == null
            ? new List<int>()
            : await _context.PurPurchaseOrders.AsNoTracking()
                .Where(x => x.SourceQuotationId == quotation.ID && x.IsDelete != true)
                .Select(x => x.ID)
                .ToListAsync(ct);
        var ordered = await _context.PurPurchaseOrderLines.AsNoTracking()
            .Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) &&
                        x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled &&
                        !ownOrderIds.Contains(x.PurchaseOrderId))
            .GroupBy(x => x.PurchaseRequestLineId)
            .Select(x => new { x.Key, Quantity = x.Sum(y => y.OrderedQuantity) })
            .ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);

        foreach (var line in requestLines)
        {
            if (!materials.TryGetValue(line.MaterialId, out var material) || !units.TryGetValue(line.UnitId, out var unit)) continue;
            existing.TryGetValue(line.ID, out var old);
            ordered.TryGetValue(line.ID, out var orderedQuantity);
            var remaining = Math.Max(0, line.ApprovedQuantity - orderedQuantity);
            if (remaining <= 0 && old == null) continue;
            model.Lines.Add(new SupplierQuotationFormLineVM
            {
                Id = old?.ID ?? 0, PurchaseRequestLineId = line.ID, Include = old != null,
                MaterialCode = material.Code, MaterialName = material.Name, Unit = unit.Name,
                ApprovedRequestQuantity = line.ApprovedQuantity, OrderedQuantity = orderedQuantity, RemainingQuantity = remaining,
                OfferedQuantityInput = (old?.OfferedQuantity ?? remaining).ToString("0.######", CultureInfo.InvariantCulture),
                UnitPriceInput = old?.UnitPrice.ToString("0.######", CultureInfo.InvariantCulture) ?? string.Empty,
                DiscountRateInput = (old?.DiscountRate ?? 0).ToString("0.######", CultureInfo.InvariantCulture),
                VatRateInput = (old?.VatRate ?? 20).ToString("0.######", CultureInfo.InvariantCulture),
                DeliveryDate = old?.DeliveryDate ?? line.NeededDate, Notes = old?.Notes
            });
        }
        await FillQuotationOptionsAsync(model, ct);
        return model;
    }

    private async Task RefillQuotationFormAsync(SupplierQuotationFormVM model, CancellationToken ct)
    {
        model.RequestNumber = await _context.PurPurchaseRequests.AsNoTracking().Where(x => x.ID == model.PurchaseRequestId).Select(x => x.RequestNumber).FirstOrDefaultAsync(ct) ?? string.Empty;
        var posted = model.Lines.ToDictionary(x => x.PurchaseRequestLineId);
        var reference = await BuildQuotationFormAsync(model.PurchaseRequestId, null, ct);
        if (reference != null)
        {
            foreach (var line in model.Lines)
            {
                var source = reference.Lines.FirstOrDefault(x => x.PurchaseRequestLineId == line.PurchaseRequestLineId);
                if (source == null) continue;
                line.MaterialCode = source.MaterialCode; line.MaterialName = source.MaterialName; line.Unit = source.Unit;
                line.ApprovedRequestQuantity = source.ApprovedRequestQuantity; line.OrderedQuantity = source.OrderedQuantity; line.RemainingQuantity = source.RemainingQuantity;
            }
        }
        await FillQuotationOptionsAsync(model, ct);
    }

    private async Task FillQuotationOptionsAsync(SupplierQuotationFormVM model, CancellationToken ct)
    {
        model.Suppliers = await _context.PurSuppliers.AsNoTracking().Where(x => x.IsDelete != true && x.IsActive != false)
            .OrderBy(x => x.Code).Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x.ID.ToString(), Text = x.Code + " - " + x.Name }).ToListAsync(ct);
        model.Currencies = new[] { "TRY", "USD", "EUR", "GBP" }.Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x, Text = x }).ToList();
        model.CurrentRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["TRY"] = 1m };
        model.CurrentRateDate = DateTime.Today;
        try
        {
            var rates = await _tcmbService.DovizKuruGetir();
            if (TryParseDecimal(rates.UsdSatis, out var usdRate) && usdRate > 0) model.CurrentRates["USD"] = usdRate;
            if (TryParseDecimal(rates.EurSatis, out var eurRate) && eurRate > 0) model.CurrentRates["EUR"] = eurRate;
            if (rates.Tarih != default) model.CurrentRateDate = rates.Tarih.Date;
        }
        catch
        {
            model.RateLoadWarning = "TCMB kurları şu anda alınamadı. TRY dışındaki teklifler için kuru manuel girebilirsiniz.";
        }
    }

    private async Task FillPurchaseOrderTransportationOptionsAsync(PurchaseOrderTransportationFormVM model, CancellationToken ct)
    {
        model.TransportationTypes = Enum.GetValues<PurTransportationType>()
            .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = ((int)x).ToString(), Text = x.ToTurkish() })
            .ToList();
        model.FreightPaymentTypes = Enum.GetValues<PurFreightPaymentType>()
            .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = ((int)x).ToString(), Text = x.ToTurkish() })
            .ToList();
        model.Warehouses = await _context.PrdWarehouses.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive != false)
            .OrderBy(x => x.Code)
            .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = x.ID.ToString(),
                Text = x.Code + " - " + x.Name
            }).ToListAsync(ct);
        model.Currencies = new[] { "TRY", "USD", "EUR", "GBP" }
            .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem { Value = x, Text = x })
            .ToList();
        model.CurrentRates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["TRY"] = 1m };
        model.CurrentRateDate = DateTime.Today;
        try
        {
            var rates = await _tcmbService.DovizKuruGetir();
            if (TryParseDecimal(rates.UsdSatis, out var usdRate) && usdRate > 0) model.CurrentRates["USD"] = usdRate;
            if (TryParseDecimal(rates.EurSatis, out var eurRate) && eurRate > 0) model.CurrentRates["EUR"] = eurRate;
            if (rates.Tarih != default) model.CurrentRateDate = rates.Tarih.Date;
        }
        catch
        {
            model.RateLoadWarning = "TCMB kurları şu anda alınamadı. TRY dışındaki nakliye bedeli için kuru manuel girebilirsiniz.";
        }
    }

    private sealed record ValidQuotationLine(SupplierQuotationFormLineVM Input, PurPurchaseRequestLine RequestLine, decimal Quantity, decimal UnitPrice, decimal DiscountRate, decimal VatRate);

    private async Task<List<ValidQuotationLine>> ValidateQuotationFormAsync(SupplierQuotationFormVM model, CancellationToken ct)
    {
        model.Lines ??= [];
        model.CurrencyCode = model.CurrencyCode?.Trim().ToUpperInvariant() ?? string.Empty;
        if (!await _context.PurSuppliers.AsNoTracking().AnyAsync(x => x.ID == model.SupplierId && x.IsDelete != true && x.IsActive != false, ct))
            ModelState.AddModelError(nameof(model.SupplierId), "Aktif bir tedarikçi seçiniz.");
        if (!new[] { "TRY", "USD", "EUR", "GBP" }.Contains(model.CurrencyCode))
            ModelState.AddModelError(nameof(model.CurrencyCode), "Geçerli bir para birimi seçiniz.");
        if (!TryParseDecimal(model.ExchangeRateInput, out var exchangeRate) || exchangeRate <= 0)
        {
            ModelState.AddModelError(nameof(model.ExchangeRateInput), "Döviz kuru sıfırdan büyük olmalıdır.");
        }
        else if (model.CurrencyCode == "TRY")
        {
            exchangeRate = 1m;
            model.ExchangeRateInput = "1";
            model.ExchangeRateSource = "Sabit";
        }
        else
        {
            model.ExchangeRateInput = exchangeRate.ToString("0.######", CultureInfo.InvariantCulture);
            model.ExchangeRateSource = string.Equals(model.ExchangeRateSource, "TCMB", StringComparison.OrdinalIgnoreCase) ? "TCMB" : "Manuel";
        }
        if (!model.ExchangeRateDate.HasValue)
            ModelState.AddModelError(nameof(model.ExchangeRateDate), "Kur tarihi zorunludur.");
        if (model.QuotationDate == default) ModelState.AddModelError(nameof(model.QuotationDate), "Teklif tarihi zorunludur.");
        if (model.ValidUntil.HasValue && model.ValidUntil.Value.Date < model.QuotationDate.Date)
            ModelState.AddModelError(nameof(model.ValidUntil), "Geçerlilik tarihi teklif tarihinden önce olamaz.");
        if (model.LeadTimeDays < 0) ModelState.AddModelError(nameof(model.LeadTimeDays), "Termin süresi negatif olamaz.");
        var selected = model.Lines.Where(x => x.Include).ToList();
        if (selected.Count == 0) ModelState.AddModelError(string.Empty, "Teklife en az bir talep satırı ekleyiniz.");
        var requestLineIds = selected.Select(x => x.PurchaseRequestLineId).Distinct().ToList();
        var requestLines = await _context.PurPurchaseRequestLines.Where(x => requestLineIds.Contains(x.ID) && x.PurchaseRequestId == model.PurchaseRequestId && x.IsDelete != true).ToDictionaryAsync(x => x.ID, ct);
        var ownOrderIds = model.Id <= 0
            ? new List<int>()
            : await _context.PurPurchaseOrders.AsNoTracking()
                .Where(x => x.SourceQuotationId == model.Id && x.IsDelete != true)
                .Select(x => x.ID)
                .ToListAsync(ct);
        var ordered = await _context.PurPurchaseOrderLines.AsNoTracking().Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled && !ownOrderIds.Contains(x.PurchaseOrderId))
            .GroupBy(x => x.PurchaseRequestLineId).Select(x => new { x.Key, Quantity = x.Sum(y => y.OrderedQuantity) }).ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var result = new List<ValidQuotationLine>();
        foreach (var input in selected)
        {
            var index = model.Lines.IndexOf(input);
            if (!requestLines.TryGetValue(input.PurchaseRequestLineId, out var requestLine))
            {
                ModelState.AddModelError($"Lines[{index}].Include", "Talep satırı artık teklif için uygun değil.");
                continue;
            }
            ordered.TryGetValue(requestLine.ID, out var orderedQuantity);
            var remaining = Math.Max(0, requestLine.ApprovedQuantity - orderedQuantity);
            if (!TryParseDecimal(input.OfferedQuantityInput, out var quantity) || quantity <= 0 || quantity > remaining)
            {
                ModelState.AddModelError($"Lines[{index}].OfferedQuantityInput", $"Miktar sıfırdan büyük ve kalan {remaining:0.######} miktarı aşmamalıdır.");
                continue;
            }
            if (!TryParseDecimal(input.UnitPriceInput, out var unitPrice) || unitPrice < 0)
            {
                ModelState.AddModelError($"Lines[{index}].UnitPriceInput", "Geçerli bir birim fiyat giriniz.");
                continue;
            }
            if (!TryParseDecimal(input.DiscountRateInput, out var discount) || discount < 0 || discount > 100)
            {
                ModelState.AddModelError($"Lines[{index}].DiscountRateInput", "İskonto oranı 0 ile 100 arasında olmalıdır.");
                continue;
            }
            if (!TryParseDecimal(input.VatRateInput, out var vat) || vat < 0 || vat > 100)
            {
                ModelState.AddModelError($"Lines[{index}].VatRateInput", "KDV oranı 0 ile 100 arasında olmalıdır.");
                continue;
            }
            result.Add(new ValidQuotationLine(input, requestLine, quantity, unitPrice, discount, vat));
        }
        return result;
    }

    private static PurSupplierQuotationLine CreateQuotationLine(int quotationId, int sequence, ValidQuotationLine item, DateTime now, string user)
    {
        var netUnitPrice = item.UnitPrice * (1 - item.DiscountRate / 100m);
        var netTotal = netUnitPrice * item.Quantity;
        var taxTotal = netTotal * item.VatRate / 100m;
        return new PurSupplierQuotationLine
        {
            SupplierQuotationId = quotationId, PurchaseRequestLineId = item.RequestLine.ID, Sequence = sequence,
            MaterialId = item.RequestLine.MaterialId, UnitId = item.RequestLine.UnitId, OfferedQuantity = item.Quantity,
            ApprovedQuantity = 0, UnitPrice = item.UnitPrice, DiscountRate = item.DiscountRate, NetUnitPrice = netUnitPrice,
            VatRate = item.VatRate, NetTotal = netTotal, TaxTotal = taxTotal, GrandTotal = netTotal + taxTotal,
            DeliveryDate = item.Input.DeliveryDate?.Date, Status = PurSupplierQuotationLineStatus.Draft, Notes = Clean(item.Input.Notes),
            IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = user
        };
    }

    private async Task<SupplierQuotationDetailVM?> BuildQuotationDetailAsync(int id, CancellationToken ct)
    {
        var model = await (from quotation in _context.PurSupplierQuotations.AsNoTracking()
                           join request in _context.PurPurchaseRequests.AsNoTracking() on quotation.PurchaseRequestId equals request.ID
                           join supplier in _context.PurSuppliers.AsNoTracking() on quotation.SupplierId equals supplier.ID
                           where quotation.ID == id && quotation.IsDelete != true
                           select new SupplierQuotationDetailVM
                           {
                               Id = quotation.ID, QuotationNumber = quotation.QuotationNumber, RequestId = request.ID, RequestNumber = request.RequestNumber,
                               SupplierId = supplier.ID, SupplierCode = supplier.Code, SupplierName = supplier.Name,
                               SupplierQuotationNumber = quotation.SupplierQuotationNumber, QuotationDate = quotation.QuotationDate,
                                ValidUntil = quotation.ValidUntil, CurrencyCode = quotation.CurrencyCode,
                                ExchangeRate = quotation.ExchangeRate, ExchangeRateDate = quotation.ExchangeRateDate,
                                ExchangeRateSource = quotation.ExchangeRateSource, PaymentTerms = quotation.PaymentTerms,
                               DeliveryTerms = quotation.DeliveryTerms, LeadTimeDays = quotation.LeadTimeDays, Status = quotation.Status,
                               NetTotal = quotation.NetTotal, TaxTotal = quotation.TaxTotal, GrandTotal = quotation.GrandTotal, Notes = quotation.Notes,
                               PurchaseOrderId = _context.PurPurchaseOrders.Where(x => x.SourceQuotationId == quotation.ID && x.IsDelete != true).Select(x => (int?)x.ID).FirstOrDefault()
                           }).FirstOrDefaultAsync(ct);
        if (model == null) return null;
        var orderedByRequest = await _context.PurPurchaseOrderLines.AsNoTracking().Where(x => x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .GroupBy(x => x.PurchaseRequestLineId).Select(x => new { x.Key, Quantity = x.Sum(y => y.OrderedQuantity) }).ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        model.Lines = await (from line in _context.PurSupplierQuotationLines.AsNoTracking()
                             join requestLine in _context.PurPurchaseRequestLines.AsNoTracking() on line.PurchaseRequestLineId equals requestLine.ID
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where line.SupplierQuotationId == id && line.IsDelete != true
                             orderby line.Sequence
                             select new SupplierQuotationDetailLineVM
                             {
                                 Id = line.ID, PurchaseRequestLineId = line.PurchaseRequestLineId, Sequence = line.Sequence,
                                 MaterialCode = material.Code, MaterialName = material.Name, Unit = unit.Name,
                                 RequestApprovedQuantity = requestLine.ApprovedQuantity, OfferedQuantity = line.OfferedQuantity,
                                 ApprovedQuantity = line.ApprovedQuantity, UnitPrice = line.UnitPrice, DiscountRate = line.DiscountRate,
                                 NetUnitPrice = line.NetUnitPrice, VatRate = line.VatRate, NetTotal = line.NetTotal,
                                 TaxTotal = line.TaxTotal, GrandTotal = line.GrandTotal, DeliveryDate = line.DeliveryDate,
                                 Status = line.Status, ApprovalNote = line.ApprovalNote, Notes = line.Notes
                             }).ToListAsync(ct);
        foreach (var line in model.Lines)
        {
            orderedByRequest.TryGetValue(line.PurchaseRequestLineId, out var ordered);
            line.AlreadyOrderedQuantity = ordered;
            line.RemainingRequestQuantity = Math.Max(0, line.RequestApprovedQuantity - ordered);
        }
        var quotationEntity = await _context.PurSupplierQuotations.AsNoTracking().FirstAsync(x => x.ID == id, ct);
        model.CanEdit = CanEditQuotation(quotationEntity);
        model.CanSubmit = CanSubmitQuotation(quotationEntity);
        model.CanApprove = CanApproveQuotations;
        return model;
    }

    private async Task RecalculateQuotationTotalsAsync(int quotationId, DateTime now, string user, CancellationToken ct)
    {
        var quotation = await _context.PurSupplierQuotations.FirstAsync(x => x.ID == quotationId, ct);
        var totals = await _context.PurSupplierQuotationLines.AsNoTracking().Where(x => x.SupplierQuotationId == quotationId && x.IsDelete != true)
            .GroupBy(x => x.SupplierQuotationId).Select(x => new { Net = x.Sum(y => y.NetTotal), Tax = x.Sum(y => y.TaxTotal), Grand = x.Sum(y => y.GrandTotal) }).FirstOrDefaultAsync(ct);
        quotation.NetTotal = totals?.Net ?? 0; quotation.TaxTotal = totals?.Tax ?? 0; quotation.GrandTotal = totals?.Grand ?? 0;
        quotation.UpdateDate = now; quotation.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
    }

    private async Task RecalculatePurchaseOrderTotalsAsync(int orderId, DateTime now, string user, CancellationToken ct)
    {
        var order = await _context.PurPurchaseOrders.FirstAsync(x => x.ID == orderId, ct);
        var totals = await _context.PurPurchaseOrderLines.AsNoTracking().Where(x => x.PurchaseOrderId == orderId && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .GroupBy(x => x.PurchaseOrderId).Select(x => new { Net = x.Sum(y => y.NetTotal), Tax = x.Sum(y => y.TaxTotal), Grand = x.Sum(y => y.GrandTotal) }).FirstOrDefaultAsync(ct);
        order.NetTotal = totals?.Net ?? 0; order.TaxTotal = totals?.Tax ?? 0; order.GrandTotal = totals?.Grand ?? 0;
        order.UpdateDate = now; order.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
    }

    private async Task RecalculateQuotationStatusAsync(int quotationId, DateTime now, string user, CancellationToken ct)
    {
        var quotation = await _context.PurSupplierQuotations.FirstAsync(x => x.ID == quotationId, ct);
        var statuses = await _context.PurSupplierQuotationLines.AsNoTracking().Where(x => x.SupplierQuotationId == quotationId && x.IsDelete != true).Select(x => x.Status).ToListAsync(ct);
        if (statuses.Any(x => x == PurSupplierQuotationLineStatus.PendingApproval))
            quotation.Status = statuses.Any(x => x == PurSupplierQuotationLineStatus.Ordered || x == PurSupplierQuotationLineStatus.Rejected || x == PurSupplierQuotationLineStatus.NotSelected) ? PurSupplierQuotationStatus.PartiallyApproved : PurSupplierQuotationStatus.PendingApproval;
        else if (statuses.Any(x => x == PurSupplierQuotationLineStatus.Ordered)) quotation.Status = PurSupplierQuotationStatus.ConvertedToOrder;
        else if (statuses.Count > 0 && statuses.All(x => x == PurSupplierQuotationLineStatus.NotSelected)) quotation.Status = PurSupplierQuotationStatus.NotSelected;
        else if (statuses.Count > 0 && statuses.All(x => x == PurSupplierQuotationLineStatus.Rejected)) quotation.Status = PurSupplierQuotationStatus.Rejected;
        quotation.UpdateDate = now; quotation.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
    }

    private async Task RecalculateRequestAfterOrdersAsync(int requestId, DateTime now, string user, CancellationToken ct)
    {
        var request = await _context.PurPurchaseRequests.FirstAsync(x => x.ID == requestId, ct);
        var lines = await _context.PurPurchaseRequestLines.Where(x => x.PurchaseRequestId == requestId && x.IsDelete != true).ToListAsync(ct);
        var ordered = await _context.PurPurchaseOrderLines.AsNoTracking().Where(x => lines.Select(l => l.ID).Contains(x.PurchaseRequestLineId) && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .GroupBy(x => x.PurchaseRequestLineId).Select(x => new { x.Key, Quantity = x.Sum(y => y.OrderedQuantity) }).ToDictionaryAsync(x => x.Key, x => x.Quantity, ct);
        var quotedRequestLineIds = await _context.PurSupplierQuotationLines.AsNoTracking()
            .Where(x => lines.Select(l => l.ID).Contains(x.PurchaseRequestLineId) && x.IsDelete != true)
            .Select(x => x.PurchaseRequestLineId)
            .Distinct()
            .ToListAsync(ct);
        var quotedRequestLineIdSet = quotedRequestLineIds.ToHashSet();
        foreach (var line in lines.Where(x => x.Status != PurPurchaseRequestLineStatus.Rejected && x.Status != PurPurchaseRequestLineStatus.Cancelled))
        {
            ordered.TryGetValue(line.ID, out var quantity);
            if (line.ApprovedQuantity > 0 && quantity >= line.ApprovedQuantity) line.Status = PurPurchaseRequestLineStatus.Ordered;
            else if (line.ApprovedQuantity > 0 && quotedRequestLineIdSet.Contains(line.ID)) line.Status = PurPurchaseRequestLineStatus.InQuotation;
            else if (line.ApprovedQuantity > 0) line.Status = PurPurchaseRequestLineStatus.Approved;
            line.UpdateDate = now; line.UpdateUserID = user;
        }
        var actionable = lines.Where(x => x.Status != PurPurchaseRequestLineStatus.Rejected && x.Status != PurPurchaseRequestLineStatus.Cancelled).ToList();
        if (actionable.Count > 0 && actionable.All(x => x.Status == PurPurchaseRequestLineStatus.Ordered))
            request.Status = PurPurchaseRequestStatus.Completed;
        else if (actionable.Any(x => x.Status == PurPurchaseRequestLineStatus.InQuotation || x.Status == PurPurchaseRequestLineStatus.Ordered))
            request.Status = PurPurchaseRequestStatus.InQuotation;
        else if (actionable.Count > 0 && actionable.All(x => x.Status == PurPurchaseRequestLineStatus.Approved))
            request.Status = PurPurchaseRequestStatus.Approved;
        else if (actionable.Any(x => x.Status == PurPurchaseRequestLineStatus.Approved))
            request.Status = PurPurchaseRequestStatus.PartiallyApproved;
        else
            request.Status = PurPurchaseRequestStatus.Rejected;
        request.UpdateDate = now; request.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<HashSet<int>> ReconcileQuotationAlternativesAsync(int requestId, DateTime now, string user, CancellationToken ct)
    {
        const string closedNote = "Talep miktarı başka bir teklif üzerinden tamamen siparişe dönüştürüldüğü için otomatik kapatıldı.";
        const string reopenedNote = "Sipariş miktarı değiştiği için teklif yeniden değerlendirmeye açıldı.";

        var requestLines = await _context.PurPurchaseRequestLines
            .Where(x => x.PurchaseRequestId == requestId && x.IsDelete != true)
            .Select(x => new { x.ID, x.ApprovedQuantity })
            .ToListAsync(ct);
        if (requestLines.Count == 0) return [];

        var requestLineIds = requestLines.Select(x => x.ID).ToList();
        var orderedLines = await _context.PurPurchaseOrderLines.AsNoTracking()
            .Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
            .Select(x => new { x.PurchaseRequestLineId, x.SupplierQuotationLineId, x.OrderedQuantity })
            .ToListAsync(ct);
        var orderedQuantities = orderedLines.GroupBy(x => x.PurchaseRequestLineId).ToDictionary(x => x.Key, x => x.Sum(y => y.OrderedQuantity));
        var orderedQuotationLineIds = orderedLines.Select(x => x.SupplierQuotationLineId).ToHashSet();

        var candidates = await _context.PurSupplierQuotationLines
            .Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) && x.IsDelete != true &&
                        (x.Status == PurSupplierQuotationLineStatus.PendingApproval || x.Status == PurSupplierQuotationLineStatus.NotSelected))
            .ToListAsync(ct);
        var affectedQuotationIds = new HashSet<int>();

        foreach (var requestLine in requestLines)
        {
            var orderedQuantity = orderedQuantities.GetValueOrDefault(requestLine.ID);
            var isSatisfied = requestLine.ApprovedQuantity > 0 && orderedQuantity >= requestLine.ApprovedQuantity;
            foreach (var candidate in candidates.Where(x => x.PurchaseRequestLineId == requestLine.ID && !orderedQuotationLineIds.Contains(x.ID)))
            {
                if (isSatisfied && candidate.Status == PurSupplierQuotationLineStatus.PendingApproval)
                {
                    var previous = candidate.Status;
                    var previousQuantity = candidate.ApprovedQuantity;
                    candidate.Status = PurSupplierQuotationLineStatus.NotSelected;
                    candidate.ApprovedQuantity = 0;
                    candidate.ApprovedDate = now;
                    candidate.ApprovedUserId = user;
                    candidate.ApprovalNote = closedNote;
                    candidate.UpdateDate = now;
                    candidate.UpdateUserID = user;
                    affectedQuotationIds.Add(candidate.SupplierQuotationId);
                    _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(candidate.SupplierQuotationId, candidate,
                        PurQuotationApprovalAction.Rejected, previous, candidate.Status, previousQuantity, closedNote, now, user));
                }
                else if (!isSatisfied && candidate.Status == PurSupplierQuotationLineStatus.NotSelected)
                {
                    var previous = candidate.Status;
                    var previousQuantity = candidate.ApprovedQuantity;
                    candidate.Status = PurSupplierQuotationLineStatus.PendingApproval;
                    candidate.ApprovedDate = null;
                    candidate.ApprovedUserId = null;
                    candidate.ApprovalNote = reopenedNote;
                    candidate.UpdateDate = now;
                    candidate.UpdateUserID = user;
                    affectedQuotationIds.Add(candidate.SupplierQuotationId);
                    _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(candidate.SupplierQuotationId, candidate,
                        PurQuotationApprovalAction.Submitted, previous, candidate.Status, previousQuantity, reopenedNote, now, user));
                }
            }
        }

        if (affectedQuotationIds.Count == 0) return affectedQuotationIds;
        await _context.SaveChangesAsync(ct);
        foreach (var quotationId in affectedQuotationIds)
            await RecalculateQuotationStatusAsync(quotationId, now, user, ct);
        return affectedQuotationIds;
    }

    private async Task RollbackRequestToDraftAsync(PurPurchaseRequest request, DateTime now, string user, CancellationToken ct)
    {
        var quotations = await _context.PurSupplierQuotations
            .Where(x => x.PurchaseRequestId == request.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var quotation in quotations)
            await DeleteQuotationWorkflowAsync(quotation, now, user, ct);

        var requestLines = await _context.PurPurchaseRequestLines
            .Where(x => x.PurchaseRequestId == request.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var line in requestLines)
        {
            line.Status = PurPurchaseRequestLineStatus.Draft;
            line.ApprovedQuantity = 0;
            line.ApprovedDate = null;
            line.ApprovedUserId = null;
            line.ApprovalNote = null;
            line.UpdateDate = now;
            line.UpdateUserID = user;
        }

        request.Status = PurPurchaseRequestStatus.Draft;
        request.SubmittedDate = null;
        request.SubmittedUserId = null;
        request.UpdateDate = now;
        request.UpdateUserID = user;
    }

    private async Task DeleteRequestWorkflowAsync(PurPurchaseRequest request, DateTime now, string user, CancellationToken ct)
    {
        await RollbackRequestToDraftAsync(request, now, user, ct);
        var requestLines = await _context.PurPurchaseRequestLines
            .Where(x => x.PurchaseRequestId == request.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var line in requestLines) SoftDelete(line, now, user);
        SoftDelete(request, now, user);
    }

    private async Task RollbackQuotationToDraftAsync(PurSupplierQuotation quotation, DateTime now, string user, CancellationToken ct)
    {
        var orders = await _context.PurPurchaseOrders
            .Where(x => x.SourceQuotationId == quotation.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var order in orders)
            await SoftDeleteOrderAsync(order, now, user, ct);

        quotation.Status = PurSupplierQuotationStatus.Draft;
        quotation.SubmittedDate = null;
        quotation.SubmittedUserId = null;
        quotation.UpdateDate = now;
        quotation.UpdateUserID = user;
    }

    private async Task DeleteQuotationWorkflowAsync(PurSupplierQuotation quotation, DateTime now, string user, CancellationToken ct)
    {
        var orders = await _context.PurPurchaseOrders
            .Where(x => x.SourceQuotationId == quotation.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var order in orders)
            await SoftDeleteOrderAsync(order, now, user, ct);

        var quotationLines = await _context.PurSupplierQuotationLines
            .Where(x => x.SupplierQuotationId == quotation.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var line in quotationLines) SoftDelete(line, now, user);
        SoftDelete(quotation, now, user);
    }

    private async Task DeleteOrderAndReopenQuotationAsync(PurPurchaseOrder order, DateTime now, string user, CancellationToken ct)
    {
        const string reopenNote = "Bağlı satınalma siparişi admin tarafından silindiği için teklif satırı yeniden değerlendirmeye açıldı.";
        var orderLines = await _context.PurPurchaseOrderLines
            .Where(x => x.PurchaseOrderId == order.ID && x.IsDelete != true)
            .ToListAsync(ct);
        var quotationLineIds = orderLines.Select(x => x.SupplierQuotationLineId).ToList();
        var quotationLines = await _context.PurSupplierQuotationLines
            .Where(x => quotationLineIds.Contains(x.ID) && x.IsDelete != true)
            .ToListAsync(ct);

        foreach (var line in quotationLines)
        {
            var previous = line.Status;
            var previousQuantity = line.ApprovedQuantity;
            line.Status = PurSupplierQuotationLineStatus.PendingApproval;
            line.ApprovedQuantity = 0;
            line.ApprovedDate = null;
            line.ApprovedUserId = null;
            line.ApprovalNote = reopenNote;
            line.UpdateDate = now;
            line.UpdateUserID = user;
            _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(line.SupplierQuotationId, line,
                PurQuotationApprovalAction.Submitted, previous, line.Status, previousQuantity, reopenNote, now, user));
        }

        await SoftDeleteGoodsReceiptsForOrderAsync(order.ID, now, user, ct);
        foreach (var line in orderLines) SoftDelete(line, now, user);
        SoftDelete(order, now, user);
    }

    private async Task SoftDeleteOrderAsync(PurPurchaseOrder order, DateTime now, string user, CancellationToken ct)
    {
        await SoftDeleteGoodsReceiptsForOrderAsync(order.ID, now, user, ct);
        var orderLines = await _context.PurPurchaseOrderLines
            .Where(x => x.PurchaseOrderId == order.ID && x.IsDelete != true)
            .ToListAsync(ct);
        foreach (var line in orderLines) SoftDelete(line, now, user);
        SoftDelete(order, now, user);
    }

    private async Task SoftDeleteGoodsReceiptsForOrderAsync(int orderId, DateTime now, string user, CancellationToken ct)
    {
        var receipts = await _context.PurGoodsReceipts.Where(x => x.PurchaseOrderId == orderId && x.IsDelete != true).ToListAsync(ct);
        if (receipts.Count == 0) return;
        var receiptIds = receipts.Select(x => x.ID).ToList();
        var lines = await _context.PurGoodsReceiptLines.Where(x => receiptIds.Contains(x.GoodsReceiptId) && x.IsDelete != true).ToListAsync(ct);
        foreach (var line in lines) SoftDelete(line, now, user);
        foreach (var receipt in receipts) SoftDelete(receipt, now, user);
    }

    private static void SoftDelete(BaseEntity entity, DateTime now, string user)
    {
        entity.IsDelete = true;
        entity.IsActive = false;
        entity.DeleteDate = now;
        entity.DeleteUserID = user;
    }

    private static PurQuotationApprovalHistory CreateQuotationHistory(int quotationId, PurSupplierQuotationLine line, PurQuotationApprovalAction action, PurSupplierQuotationLineStatus previousStatus, PurSupplierQuotationLineStatus newStatus, decimal previousApprovedQuantity, string? note, DateTime now, string user) => new()
    {
        SupplierQuotationId = quotationId, SupplierQuotationLineId = line.ID, Action = action,
        PreviousStatus = previousStatus, NewStatus = newStatus, OfferedQuantity = line.OfferedQuantity,
        PreviousApprovedQuantity = previousApprovedQuantity, ApprovedQuantity = line.ApprovedQuantity,
        NetUnitPrice = line.NetUnitPrice, Note = Clean(note), ActionDate = now, ActionUserId = user,
        IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = user
    };

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        value = 0;
        if (string.IsNullOrWhiteSpace(text)) return false;
        var normalized = text.Trim().Replace(" ", string.Empty);
        if (normalized.Contains(',') && normalized.Contains('.'))
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.') ? normalized.Replace(".", string.Empty).Replace(',', '.') : normalized.Replace(",", string.Empty);
        else if (normalized.Contains(','))
            normalized = normalized.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
