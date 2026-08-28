using Ekomers.Data;
using Ekomers.Models.Entity.Purchasing;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Purchasing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrPurchasing")]
public sealed class SatinalmaYonetimiController : Controller
{
    private readonly ApplicationDbContext _context;

    public SatinalmaYonetimiController(ApplicationDbContext context) => _context = context;

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

    private string CurrentUser => User.Identity?.Name ?? "system";
    private bool CanApproveRequests => User.IsInRole("Admin") || User.HasClaim("Authorize", "TalepKabul");
    private bool CanEditRequest(PurPurchaseRequest request) => User.IsInRole("Admin") || string.Equals(request.RequestedUserId, CurrentUser, StringComparison.OrdinalIgnoreCase);

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
