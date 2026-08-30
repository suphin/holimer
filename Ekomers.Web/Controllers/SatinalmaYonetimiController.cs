using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Services;
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

    [HttpGet]
    public async Task<IActionResult> TalepDuzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var request = await _context.PurPurchaseRequests.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (!CanEditRequest(request)) return Forbid();
        if (request.Status != PurPurchaseRequestStatus.Draft)
        {
            TempData["error"] = "Yalnızca taslak talepler düzenlenebilir.";
            return RedirectToAction(nameof(TalepDetay), new { id });
        }

        var model = new PurchaseRequestFormVM
        {
            Id = request.ID,
            RequestNumber = request.RequestNumber,
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TalepDuzenle(PurchaseRequestFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var request = await _context.PurPurchaseRequests.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (!CanEditRequest(request)) return Forbid();
        if (request.Status != PurPurchaseRequestStatus.Draft)
        {
            TempData["error"] = "Yalnızca taslak talepler düzenlenebilir.";
            return RedirectToAction(nameof(TalepDetay), new { id = model.Id });
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
        TempData["success"] = $"{request.RequestNumber} numaralı taslak güncellendi.";
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> OnayaGonder(int id, CancellationToken ct)
    {
        var request = await _context.PurPurchaseRequests.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (request == null) return NotFound();
        if (!CanEditRequest(request)) return Forbid();
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

    [HttpGet]
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

    [HttpPost, ValidateAntiForgeryToken]
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

    [HttpGet]
    public async Task<IActionResult> TeklifDuzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var quotation = await _context.PurSupplierQuotations.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (!CanEditQuotation(quotation)) return Forbid();
        if (quotation.Status != PurSupplierQuotationStatus.Draft)
        {
            TempData["error"] = "Yalnızca taslak teklifler düzenlenebilir.";
            return RedirectToAction(nameof(TeklifDetay), new { id });
        }
        var model = await BuildQuotationFormAsync(quotation.PurchaseRequestId, quotation, ct);
        return View("TeklifFormu", model!);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeklifDuzenle(SupplierQuotationFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var quotation = await _context.PurSupplierQuotations.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (!CanEditQuotation(quotation)) return Forbid();
        if (quotation.Status != PurSupplierQuotationStatus.Draft)
        {
            TempData["error"] = "Yalnızca taslak teklifler düzenlenebilir.";
            return RedirectToAction(nameof(TeklifDetay), new { id = model.Id });
        }
        model.PurchaseRequestId = quotation.PurchaseRequestId;
        var validLines = await ValidateQuotationFormAsync(model, ct);
        if (!ModelState.IsValid)
        {
            model.QuotationNumber = quotation.QuotationNumber;
            await RefillQuotationFormAsync(model, ct);
            return View("TeklifFormu", model);
        }
        var now = DateTime.Now;
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
        TempData["success"] = $"{quotation.QuotationNumber} güncellendi.";
        return RedirectToAction(nameof(TeklifDetay), new { id = quotation.ID });
    }

    [HttpGet]
    public async Task<IActionResult> TeklifDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniSatinalma";
        var model = await BuildQuotationDetailAsync(id, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> TeklifiOnayaGonder(int id, CancellationToken ct)
    {
        var quotation = await _context.PurSupplierQuotations.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (quotation == null) return NotFound();
        if (!CanEditQuotation(quotation)) return Forbid();
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
            var order = await _context.PurPurchaseOrders.FirstOrDefaultAsync(x => x.SourceQuotationId == quotation.ID && x.IsDelete != true, ct);
            if (order == null)
            {
                order = new PurPurchaseOrder
                {
                    OrderNumber = $"SS-{now:yyyyMMddHHmmssfff}", SupplierId = quotation.SupplierId, SourceQuotationId = quotation.ID,
                    OrderDate = now.Date, CurrencyCode = quotation.CurrencyCode, ExchangeRate = quotation.ExchangeRate,
                    ExchangeRateDate = quotation.ExchangeRateDate, ExchangeRateSource = quotation.ExchangeRateSource,
                    Status = PurPurchaseOrderStatus.Open,
                    PaymentTerms = quotation.PaymentTerms, DeliveryTerms = quotation.DeliveryTerms, Notes = quotation.Notes,
                    IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser
                };
                _context.PurPurchaseOrders.Add(order);
                await _context.SaveChangesAsync(ct);
            }
            var sequence = await _context.PurPurchaseOrderLines.CountAsync(x => x.PurchaseOrderId == order.ID && x.IsDelete != true, ct) + 1;
            var netTotal = line.NetUnitPrice * approvedQuantity;
            var taxTotal = netTotal * line.VatRate / 100m;
            _context.PurPurchaseOrderLines.Add(new PurPurchaseOrderLine
            {
                PurchaseOrderId = order.ID, SupplierQuotationLineId = line.ID, PurchaseRequestLineId = line.PurchaseRequestLineId,
                Sequence = sequence, MaterialId = line.MaterialId, UnitId = line.UnitId, OrderedQuantity = approvedQuantity,
                ReceivedQuantity = 0, UnitPrice = line.UnitPrice, DiscountRate = line.DiscountRate, NetUnitPrice = line.NetUnitPrice,
                VatRate = line.VatRate, NetTotal = netTotal, TaxTotal = taxTotal, GrandTotal = netTotal + taxTotal,
                RequestedDeliveryDate = line.DeliveryDate, Status = PurPurchaseOrderLineStatus.Open, Notes = line.Notes,
                IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser
            });
            await _context.SaveChangesAsync(ct);
            await RecalculatePurchaseOrderTotalsAsync(order.ID, now, CurrentUser, ct);

            if (alreadyOrdered + approvedQuantity >= requestLine.ApprovedQuantity)
            {
                var alternatives = await _context.PurSupplierQuotationLines
                    .Where(x => x.PurchaseRequestLineId == requestLine.ID && x.ID != line.ID &&
                                x.Status == PurSupplierQuotationLineStatus.PendingApproval && x.IsDelete != true)
                    .ToListAsync(ct);
                foreach (var alternative in alternatives)
                {
                    var alternativePrevious = alternative.Status;
                    var alternativePreviousQuantity = alternative.ApprovedQuantity;
                    alternative.Status = PurSupplierQuotationLineStatus.Rejected;
                    alternative.ApprovedQuantity = 0;
                    alternative.ApprovedDate = now;
                    alternative.ApprovedUserId = CurrentUser;
                    alternative.ApprovalNote = $"Talep miktarı {quotation.QuotationNumber} numaralı teklif üzerinden siparişe dönüştürüldüğü için otomatik kapatıldı.";
                    alternative.UpdateDate = now;
                    alternative.UpdateUserID = CurrentUser;
                    affectedQuotationIds.Add(alternative.SupplierQuotationId);
                    _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(
                        alternative.SupplierQuotationId, alternative, PurQuotationApprovalAction.Rejected,
                        alternativePrevious, alternative.Status, alternativePreviousQuantity,
                        alternative.ApprovalNote, now, CurrentUser));
                }
            }
        }
        line.ApprovedDate = now; line.ApprovedUserId = CurrentUser; line.ApprovalNote = Clean(input.Note); line.UpdateDate = now; line.UpdateUserID = CurrentUser;
        _context.PurQuotationApprovalHistories.Add(CreateQuotationHistory(quotation.ID, line, input.Decision, previous, line.Status, previousQuantity, input.Note, now, CurrentUser));
        await _context.SaveChangesAsync(ct);
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
        return View(model);
    }

    private string CurrentUser => User.Identity?.Name ?? "system";
    private bool CanApproveRequests => User.IsInRole("Admin") || User.HasClaim("Authorize", "TalepKabul");
    private bool CanApproveQuotations => User.IsInRole("Admin") || User.HasClaim("Authorize", "TeklifKabul");
    private bool CanEditRequest(PurPurchaseRequest request) => User.IsInRole("Admin") || string.Equals(request.RequestedUserId, CurrentUser, StringComparison.OrdinalIgnoreCase);
    private bool CanEditQuotation(PurSupplierQuotation quotation) => User.IsInRole("Admin") || string.Equals(quotation.CreateUserID, CurrentUser, StringComparison.OrdinalIgnoreCase);
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
        model.CanEdit = User.IsInRole("Admin") || string.Equals(model.RequestedUserId, CurrentUser, StringComparison.OrdinalIgnoreCase);
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
        var ordered = await _context.PurPurchaseOrderLines.AsNoTracking()
            .Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) &&
                        x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
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
        var ordered = await _context.PurPurchaseOrderLines.AsNoTracking().Where(x => requestLineIds.Contains(x.PurchaseRequestLineId) && x.IsDelete != true && x.Status != PurPurchaseOrderLineStatus.Cancelled)
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
            quotation.Status = statuses.Any(x => x == PurSupplierQuotationLineStatus.Ordered || x == PurSupplierQuotationLineStatus.Rejected) ? PurSupplierQuotationStatus.PartiallyApproved : PurSupplierQuotationStatus.PendingApproval;
        else if (statuses.Any(x => x == PurSupplierQuotationLineStatus.Ordered)) quotation.Status = PurSupplierQuotationStatus.ConvertedToOrder;
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
        foreach (var line in lines.Where(x => x.Status != PurPurchaseRequestLineStatus.Rejected && x.Status != PurPurchaseRequestLineStatus.Cancelled))
        {
            ordered.TryGetValue(line.ID, out var quantity);
            if (line.ApprovedQuantity > 0 && quantity >= line.ApprovedQuantity) line.Status = PurPurchaseRequestLineStatus.Ordered;
            else if (line.ApprovedQuantity > 0) line.Status = PurPurchaseRequestLineStatus.InQuotation;
            line.UpdateDate = now; line.UpdateUserID = user;
        }
        var actionable = lines.Where(x => x.Status != PurPurchaseRequestLineStatus.Rejected && x.Status != PurPurchaseRequestLineStatus.Cancelled).ToList();
        request.Status = actionable.Count > 0 && actionable.All(x => x.Status == PurPurchaseRequestLineStatus.Ordered) ? PurPurchaseRequestStatus.Completed : PurPurchaseRequestStatus.InQuotation;
        request.UpdateDate = now; request.UpdateUserID = user;
        await _context.SaveChangesAsync(ct);
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
