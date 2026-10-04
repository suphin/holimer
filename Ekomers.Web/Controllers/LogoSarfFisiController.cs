using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize]
public sealed class LogoSarfFisiController : Controller
{
    private const string ViewPolicy = "LogoSarfFisiGoruntule";
    private const string ManagePolicy = "LogoSarfFisiYonet";

    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logoContext;
    private readonly LogoRestSettingsProvider _settingsProvider;
    private readonly LogoRestApiClient _logoClient;

    public LogoSarfFisiController(
        ApplicationDbContext context,
        LogoContext logoContext,
        LogoRestSettingsProvider settingsProvider,
        LogoRestApiClient logoClient)
    {
        _context = context;
        _logoContext = logoContext;
        _settingsProvider = settingsProvider;
        _logoClient = logoClient;
    }

    [HttpGet, Authorize(Policy = ViewPolicy)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        SetModule();
        var model = await _context.PrdLogoConsumptionSlips
            .AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.ID)
            .Select(x => new LogoConsumptionSlipListVM
            {
                Id = x.ID,
                DocumentNumber = x.DocumentNumber,
                LogoSlipNumber = x.LogoSlipNumber,
                LogoAssignedSlipNumber = x.LogoAssignedSlipNumber,
                DocumentDate = x.DocumentDate,
                WarehouseNumber = x.WarehouseNumber,
                LineCount = _context.PrdLogoConsumptionSlipLines.Count(l => l.SlipId == x.ID && l.IsDelete != true),
                TotalQuantity = _context.PrdLogoConsumptionSlipLines
                    .Where(l => l.SlipId == x.ID && l.IsDelete != true)
                    .Sum(l => (decimal?)l.Quantity) ?? 0,
                Status = x.Status,
                AttemptCount = x.AttemptCount,
                SentDate = x.SentDate,
                LogoReference = x.LogoReference
            })
            .ToListAsync(cancellationToken);

        return View(model);
    }

    [HttpGet, Authorize(Policy = ManagePolicy)]
    public IActionResult Yeni()
    {
        SetModule();
        return View("Form", new LogoConsumptionSlipCreateVM
        {
            DocumentDate = DateTime.Today,
            Lines = [new()]
        });
    }

    [HttpGet, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Duzenle(int id, CancellationToken cancellationToken)
    {
        SetModule();
        var slip = await _context.PrdLogoConsumptionSlips.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (slip == null)
            return NotFound();
        if (slip.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed))
        {
            TempData["error"] = "Yalnızca taslak veya aktarım hatası olan sarf fişi değiştirilebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }
        if (!string.IsNullOrWhiteSpace(slip.LogoReference))
        {
            TempData["error"] = "Logo tarafında bu kayda bağlı bir fiş referansı bulunuyor. Logo'daki fişi silip admin olarak yeniden aktarıma açın.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var lineRows = await (from line in _context.PrdLogoConsumptionSlipLines.AsNoTracking()
                              join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                              join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                              where line.SlipId == id && line.IsDelete != true
                              orderby line.Sequence
                              select new
                              {
                                  line.MaterialId,
                                  line.Quantity,
                                  line.Description,
                                  MaterialText = material.Code + " - " + material.Name,
                                  UnitText = unit.Code + " - " + unit.Name
                              }).ToListAsync(cancellationToken);
        var culture = CultureInfo.GetCultureInfo("tr-TR");
        var lines = lineRows.Select(x => new LogoConsumptionSlipCreateLineVM
        {
            MaterialId = x.MaterialId,
            Quantity = x.Quantity.ToString("0.######", culture),
            Description = x.Description,
            MaterialText = x.MaterialText,
            UnitText = x.UnitText
        }).ToList();

        return View("Form", new LogoConsumptionSlipCreateVM
        {
            Id = slip.ID,
            DocumentDate = slip.DocumentDate,
            LogoSlipNumber = slip.LogoSlipNumber,
            WarehouseNumber = slip.WarehouseNumber,
            DivisionNumber = slip.DivisionNumber,
            DepartmentNumber = slip.DepartmentNumber,
            FactoryNumber = slip.FactoryNumber,
            Notes = slip.Notes,
            Lines = lines.Count == 0 ? [new()] : lines
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Kaydet(LogoConsumptionSlipCreateVM model, CancellationToken cancellationToken)
    {
        SetModule();
        var activeLines = model.Lines.Where(x => x.MaterialId.HasValue).ToList();
        if (activeLines.Count == 0)
            ModelState.AddModelError(string.Empty, "En az bir malzeme ve miktar giriniz.");

        var duplicateIds = activeLines.GroupBy(x => x.MaterialId!.Value)
            .Where(x => x.Count() > 1).Select(x => x.Key).ToList();
        if (duplicateIds.Count > 0)
            ModelState.AddModelError(string.Empty, "Aynı malzeme fişe birden fazla kez eklenemez.");

        var materialIds = activeLines.Select(x => x.MaterialId!.Value).Distinct().ToList();
        var materials = await (from material in _context.PrdMaterials.AsNoTracking()
                               join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                               where materialIds.Contains(material.ID)
                                     && material.IsActive != false && material.IsDelete != true
                               select new
                               {
                                   material.ID,
                                   material.Code,
                                   material.Name,
                                   material.LogoCode,
                                   material.UnitId,
                                   UnitCode = unit.Code,
                                   UnitName = unit.Name
                               }).ToDictionaryAsync(x => x.ID, cancellationToken);

        var now = DateTime.Now;
        var actor = User.Identity?.Name;
        var preparedLines = new List<PrdLogoConsumptionSlipLine>();
        for (var index = 0; index < activeLines.Count; index++)
        {
            var input = activeLines[index];
            if (!input.MaterialId.HasValue || !materials.TryGetValue(input.MaterialId.Value, out var material))
            {
                ModelState.AddModelError(string.Empty, $"{index + 1}. satırdaki sarf malzemesi bulunamadı.");
                continue;
            }

            input.MaterialText = material.Code + " - " + material.Name;
            input.UnitText = material.UnitCode + " - " + material.UnitName;
            if (string.IsNullOrWhiteSpace(material.LogoCode) && string.IsNullOrWhiteSpace(material.Code))
            {
                ModelState.AddModelError(string.Empty, $"{material.Name} için Logo malzeme kodu bulunmuyor.");
                continue;
            }
            if (string.IsNullOrWhiteSpace(material.UnitCode))
            {
                ModelState.AddModelError(string.Empty, $"{material.Code} için Logo birim kodu bulunmuyor.");
                continue;
            }
            if (!TryParseDecimal(input.Quantity, out var quantity) || quantity <= 0)
            {
                ModelState.AddModelError(string.Empty, $"{material.Code} için miktar sıfırdan büyük olmalıdır.");
                continue;
            }

            preparedLines.Add(new PrdLogoConsumptionSlipLine
            {
                Sequence = preparedLines.Count + 1,
                MaterialId = material.ID,
                UnitId = material.UnitId,
                Quantity = quantity,
                Description = input.Description?.Trim(),
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = actor
            });
        }

        if (!ModelState.IsValid)
        {
            model.Lines = activeLines.Count == 0 ? [new()] : activeLines;
            return View("Form", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        PrdLogoConsumptionSlip slip;
        if (model.Id.HasValue)
        {
            slip = await _context.PrdLogoConsumptionSlips
                .FirstOrDefaultAsync(x => x.ID == model.Id.Value && x.IsDelete != true, cancellationToken)
                ?? throw new InvalidOperationException("Düzenlenecek sarf fişi bulunamadı.");
            if (slip.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed)
                || !string.IsNullOrWhiteSpace(slip.LogoReference))
            {
                TempData["error"] = "Fiş artık düzenlemeye uygun değil.";
                return RedirectToAction(nameof(Detay), new { id = slip.ID });
            }

            var oldLines = await _context.PrdLogoConsumptionSlipLines
                .Where(x => x.SlipId == slip.ID).ToListAsync(cancellationToken);
            _context.PrdLogoConsumptionSlipLines.RemoveRange(oldLines);
            slip.Status = PrdLogoTransferStatus.Draft;
            slip.LastError = null;
            slip.UpdateDate = now;
            slip.UpdateUserID = actor;
        }
        else
        {
            slip = new PrdLogoConsumptionSlip
            {
                DocumentNumber = $"LSF-{now:yyyyMMddHHmmssfff}",
                Status = PrdLogoTransferStatus.Draft,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = actor
            };
            _context.PrdLogoConsumptionSlips.Add(slip);
        }

        slip.DocumentDate = model.DocumentDate.Date;
        slip.LogoSlipNumber = string.IsNullOrWhiteSpace(model.LogoSlipNumber) ? null : model.LogoSlipNumber.Trim();
        slip.WarehouseNumber = model.WarehouseNumber;
        slip.DivisionNumber = model.DivisionNumber;
        slip.DepartmentNumber = model.DepartmentNumber;
        slip.FactoryNumber = model.FactoryNumber;
        slip.Notes = model.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        foreach (var line in preparedLines)
            line.SlipId = slip.ID;
        _context.PrdLogoConsumptionSlipLines.AddRange(preparedLines);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["success"] = $"{slip.DocumentNumber} numaralı Logo sarf fişi taslağı kaydedildi.";
        return RedirectToAction(nameof(Detay), new { id = slip.ID });
    }

    [HttpGet, Authorize(Policy = ViewPolicy)]
    public async Task<IActionResult> Detay(int id, CancellationToken cancellationToken)
    {
        SetModule();
        var model = await _context.PrdLogoConsumptionSlips.AsNoTracking()
            .Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new LogoConsumptionSlipDetailVM
            {
                Id = x.ID,
                DocumentNumber = x.DocumentNumber,
                LogoSlipNumber = x.LogoSlipNumber,
                LogoAssignedSlipNumber = x.LogoAssignedSlipNumber,
                DocumentDate = x.DocumentDate,
                WarehouseNumber = x.WarehouseNumber,
                DivisionNumber = x.DivisionNumber,
                DepartmentNumber = x.DepartmentNumber,
                FactoryNumber = x.FactoryNumber,
                Status = x.Status,
                AttemptCount = x.AttemptCount,
                LastAttemptDate = x.LastAttemptDate,
                SentDate = x.SentDate,
                LogoReference = x.LogoReference,
                LastError = x.LastError,
                Notes = x.Notes
            }).FirstOrDefaultAsync(cancellationToken);
        if (model == null)
            return NotFound();

        model.Lines = await (from line in _context.PrdLogoConsumptionSlipLines.AsNoTracking()
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where line.SlipId == id && line.IsDelete != true
                             orderby line.Sequence
                             select new LogoConsumptionSlipDetailLineVM
                             {
                                 Sequence = line.Sequence,
                                 MaterialCode = material.Code,
                                 MaterialName = material.Name,
                                 LogoCode = material.LogoCode ?? material.Code,
                                 Quantity = line.Quantity,
                                 UnitCode = unit.Code,
                                 UnitName = unit.Name,
                                 Description = line.Description
                             }).ToListAsync(cancellationToken);
        model.LineCount = model.Lines.Count;
        model.TotalQuantity = model.Lines.Sum(x => x.Quantity);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Gonder(int id, CancellationToken cancellationToken)
    {
        var slip = await _context.PrdLogoConsumptionSlips.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (slip == null)
            return NotFound();
        if (slip.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed))
        {
            TempData["error"] = slip.Status == PrdLogoTransferStatus.Sent
                ? "Bu sarf fişi daha önce Logo'ya aktarılmış; ikinci kez gönderilemez."
                : "Fiş şu anda gönderime uygun değil.";
            return RedirectToAction(nameof(Detay), new { id });
        }
        if (!string.IsNullOrWhiteSpace(slip.LogoReference))
        {
            TempData["error"] = "Logo tarafında bu kayda bağlı fiş referansı bulunuyor. Önce Logo'daki fişi silip yeniden aktarıma açın.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var settings = await _settingsProvider.GetAsync(cancellationToken);
        if (!settings.Success || settings.Options == null)
        {
            TempData["error"] = settings.Message;
            return RedirectToAction(nameof(Detay), new { id });
        }

        var lines = await (from line in _context.PrdLogoConsumptionSlipLines.AsNoTracking()
                           join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                           join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                           where line.SlipId == id && line.IsDelete != true
                           orderby line.Sequence
                           select new
                           {
                               line.Sequence,
                               Code = material.LogoCode ?? material.Code,
                               line.Quantity,
                               UnitCode = unit.Code,
                               line.Description
                           }).ToListAsync(cancellationToken);
        if (lines.Count == 0 || lines.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.UnitCode)))
        {
            TempData["error"] = "Fişte Logo kodu ve birimi geçerli en az bir malzeme bulunmalıdır.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var materialCodes = lines.Select(x => x.Code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var logoRows = await _logoContext.Items.AsNoTracking()
            .Where(x => x.ACTIVE == 0 && materialCodes.Contains(x.CODE))
            .Select(x => new { x.CODE, x.LOGICALREF }).ToListAsync(cancellationToken);
        var logoReferences = logoRows.GroupBy(x => x.CODE.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().LOGICALREF, StringComparer.OrdinalIgnoreCase);
        var missingCodes = materialCodes.Where(x => !logoReferences.ContainsKey(x)).ToList();
        if (missingCodes.Count > 0)
        {
            TempData["error"] = "Logo'da aktif malzeme kartı bulunamadı: " + string.Join(", ", missingCodes);
            return RedirectToAction(nameof(Detay), new { id });
        }

        var now = DateTime.Now;
        var actor = User.Identity?.Name;
        var claimed = await _context.PrdLogoConsumptionSlips
            .Where(x => x.ID == id && x.LogoReference == null
                        && (x.Status == PrdLogoTransferStatus.Draft || x.Status == PrdLogoTransferStatus.Failed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PrdLogoTransferStatus.Sending)
                .SetProperty(x => x.LastAttemptDate, now)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.UpdateDate, now)
                .SetProperty(x => x.UpdateUserID, actor), cancellationToken);
        if (claimed == 0)
        {
            TempData["error"] = "Fiş başka bir işlem tarafından gönderiliyor veya durumu değişti.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var payload = new LogoProductionEntrySlipPayload
        {
            Group = 3,
            Type = 12,
            Number = string.IsNullOrWhiteSpace(slip.LogoSlipNumber) ? "~" : slip.LogoSlipNumber,
            Date = slip.DocumentDate.Date,
            WarehouseNumber = slip.WarehouseNumber,
            WarehouseCostGroup = slip.WarehouseNumber,
            DivisionNumber = slip.DivisionNumber,
            DepartmentNumber = slip.DepartmentNumber,
            FactoryNumber = slip.FactoryNumber,
            Notes = slip.Notes,
            Transactions = new LogoRestItemCollection<LogoProductionEntrySlipLinePayload>
            {
                Items = lines.Select(x => new LogoProductionEntrySlipLinePayload
                {
                    MaterialCode = x.Code,
                    MaterialReference = logoReferences[x.Code.Trim()],
                    LineType = 0,
                    LineNumber = x.Sequence,
                    Quantity = x.Quantity,
                    UnitCode = x.UnitCode,
                    WarehouseNumber = slip.WarehouseNumber,
                    WarehouseCostGroup = slip.WarehouseNumber,
                    InputOutputCode = 4,
                    Description = x.Description
                }).ToList()
            }
        };

        LogoRestTransferResult result;
        try
        {
            result = await _logoClient.SendConsumptionSlipAsync(settings.Options, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            result = new LogoRestTransferResult(false, "Beklenmeyen Logo aktarım hatası: " + ex.Message);
        }

        var tracked = await _context.PrdLogoConsumptionSlips.FirstAsync(x => x.ID == id, cancellationToken);
        tracked.Status = result.Success ? PrdLogoTransferStatus.Sent : PrdLogoTransferStatus.Failed;
        tracked.SentDate = result.Success ? DateTime.Now : null;
        tracked.LogoReference = result.LogoReference;
        tracked.LogoAssignedSlipNumber = result.LogoSlipNumber;
        tracked.LastError = result.Success ? null : result.Message;
        tracked.UpdateDate = DateTime.Now;
        tracked.UpdateUserID = actor;
        await _context.SaveChangesAsync(cancellationToken);

        TempData[result.Success ? "success" : "error"] = result.Message;
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Iptal(int id, CancellationToken cancellationToken)
    {
        var slip = await _context.PrdLogoConsumptionSlips
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (slip == null)
            return NotFound();
        if (slip.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed)
            || !string.IsNullOrWhiteSpace(slip.LogoReference))
        {
            TempData["error"] = "Yalnızca Logo referansı oluşmamış taslak veya hatalı sarf fişi iptal edilebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        slip.Status = PrdLogoTransferStatus.Cancelled;
        slip.IsActive = false;
        slip.UpdateDate = DateTime.Now;
        slip.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Sarf fişi taslağı iptal edildi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sil(int id, CancellationToken cancellationToken)
    {
        var slip = await _context.PrdLogoConsumptionSlips
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (slip == null)
            return NotFound();
        if (slip.Status != PrdLogoTransferStatus.Cancelled || !string.IsNullOrWhiteSpace(slip.LogoReference))
        {
            TempData["error"] = "Yalnızca iptal edilmiş ve Logo referansı bulunmayan sarf fişleri silinebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var now = DateTime.Now;
        var actor = User.Identity?.Name ?? "Admin";
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        await _context.PrdLogoConsumptionSlipLines.Where(x => x.SlipId == id && x.IsDelete != true)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDelete, true)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.DeleteDate, now)
                .SetProperty(x => x.DeleteUserID, actor), cancellationToken);
        slip.IsDelete = true;
        slip.IsActive = false;
        slip.DeleteDate = now;
        slip.DeleteUserID = actor;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["success"] = $"{slip.DocumentNumber} numaralı iptal edilmiş sarf fişi silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> YenidenAktarimaAc(int id, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var actor = User.Identity?.Name;
        var updated = await _context.PrdLogoConsumptionSlips
            .Where(x => x.ID == id && x.IsDelete != true
                        && (x.Status == PrdLogoTransferStatus.Sent
                            || (x.Status == PrdLogoTransferStatus.Failed && x.LogoReference != null)))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PrdLogoTransferStatus.Draft)
                .SetProperty(x => x.SentDate, (DateTime?)null)
                .SetProperty(x => x.LogoReference, (string?)null)
                .SetProperty(x => x.LogoAssignedSlipNumber, (string?)null)
                .SetProperty(x => x.LastError, (string?)null)
                .SetProperty(x => x.IsActive, true)
                .SetProperty(x => x.UpdateDate, now)
                .SetProperty(x => x.UpdateUserID, actor), cancellationToken);
        if (updated == 0)
        {
            if (!await _context.PrdLogoConsumptionSlips.AnyAsync(x => x.ID == id && x.IsDelete != true, cancellationToken))
                return NotFound();
            TempData["error"] = "Bu sarf fişi yeniden aktarıma açılamaz.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        TempData["success"] = "Sarf fişi taslağa döndürüldü. Logo'daki önceki kaydın silindiğinden emin olun.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpGet, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> MalzemeAra(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);
        q = q?.Trim();
        var query = from material in _context.PrdMaterials.AsNoTracking()
                    join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                    where material.IsActive != false && material.IsDelete != true
                    select new { material, unit };
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.material.Code.Contains(q)
                                     || x.material.Name.Contains(q)
                                     || (x.material.LogoCode != null && x.material.LogoCode.Contains(q)));

        var rows = await query.OrderBy(x => x.material.Code)
            .Skip((page - 1) * pageSize).Take(pageSize + 1)
            .Select(x => new
            {
                id = x.material.ID,
                text = x.material.Code + " - " + x.material.Name,
                logoCode = x.material.LogoCode ?? x.material.Code,
                unit = x.unit.Code + " - " + x.unit.Name
            }).ToListAsync(cancellationToken);
        return Json(new { results = rows.Take(pageSize), pagination = new { more = rows.Count > pageSize } });
    }

    private void SetModule() => ViewBag.Modul = "LogoFisleri";

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out result)
               || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
