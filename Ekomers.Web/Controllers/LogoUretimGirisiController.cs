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
public sealed class LogoUretimGirisiController : Controller
{
    private const string ViewPolicy = "LogoUretimGirisiGoruntule";
    private const string ManagePolicy = "LogoUretimGirisiYonet";

    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logoContext;
    private readonly LogoRestSettingsProvider _settingsProvider;
    private readonly LogoRestApiClient _logoClient;

    public LogoUretimGirisiController(
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
        var model = await _context.PrdLogoProductionReceipts
            .AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderByDescending(x => x.DocumentDate)
            .ThenByDescending(x => x.ID)
            .Select(x => new LogoProductionReceiptListVM
            {
                Id = x.ID,
                DocumentNumber = x.DocumentNumber,
                LogoSlipNumber = x.LogoSlipNumber,
                LogoAssignedSlipNumber = x.LogoAssignedSlipNumber,
                DocumentDate = x.DocumentDate,
                WarehouseNumber = x.WarehouseNumber,
                LineCount = _context.PrdLogoProductionReceiptLines.Count(l => l.ReceiptId == x.ID && l.IsDelete != true),
                TotalQuantity = _context.PrdLogoProductionReceiptLines
                    .Where(l => l.ReceiptId == x.ID && l.IsDelete != true)
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
        return View("Form", new LogoProductionReceiptCreateVM
        {
            DocumentDate = DateTime.Today,
            Lines = [new()]
        });
    }

    [HttpGet, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Duzenle(int id, CancellationToken cancellationToken)
    {
        SetModule();
        var receipt = await _context.PrdLogoProductionReceipts
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (receipt == null)
            return NotFound();
        if (receipt.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed))
        {
            TempData["error"] = "Yalnızca taslak veya aktarım hatası olan fiş değiştirilebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (!string.IsNullOrWhiteSpace(receipt.LogoReference))
        {
            TempData["error"] = "Logo tarafında bu portal kaydına bağlı bir fiş referansı bulunuyor. Logo'daki hatalı fişi silin, ardından admin olarak 'Yeniden Aktarıma Aç' işlemini kullanın.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var lineRows = await (from line in _context.PrdLogoProductionReceiptLines.AsNoTracking()
                              join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                              join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                              where line.ReceiptId == id && line.IsDelete != true
                              orderby line.Sequence
                              select new
                              {
                                  line.MaterialId,
                                  line.Quantity,
                                  line.LotNumber,
                                  line.ProductionDate,
                                  line.ExpirationDate,
                                  line.Description,
                                  MaterialText = material.Code + " - " + material.Name,
                                  UnitText = unit.Code + " - " + unit.Name
                              }).ToListAsync(cancellationToken);

        var trCulture = CultureInfo.GetCultureInfo("tr-TR");
        var lines = lineRows.Select(line => new LogoProductionReceiptCreateLineVM
        {
            MaterialId = line.MaterialId,
            Quantity = line.Quantity.ToString("0.######", trCulture),
            LotNumber = line.LotNumber,
            ProductionDate = line.ProductionDate,
            ExpirationDate = line.ExpirationDate,
            Description = line.Description,
            MaterialText = line.MaterialText,
            UnitText = line.UnitText
        }).ToList();

        return View("Form", new LogoProductionReceiptCreateVM
        {
            Id = receipt.ID,
            DocumentDate = receipt.DocumentDate,
            LogoSlipNumber = receipt.LogoSlipNumber,
            WarehouseNumber = receipt.WarehouseNumber,
            DivisionNumber = receipt.DivisionNumber,
            DepartmentNumber = receipt.DepartmentNumber,
            FactoryNumber = receipt.FactoryNumber,
            Notes = receipt.Notes,
            Lines = lines.Count == 0 ? [new()] : lines
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Kaydet(LogoProductionReceiptCreateVM model, CancellationToken cancellationToken)
    {
        SetModule();
        var activeLines = model.Lines.Where(x => x.MaterialId.HasValue).ToList();
        if (activeLines.Count == 0)
            ModelState.AddModelError(string.Empty, "En az bir ürün ve miktar giriniz.");

        var duplicateMaterialIds = activeLines
            .GroupBy(x => x.MaterialId!.Value)
            .Where(x => x.Count() > 1)
            .Select(x => x.Key)
            .ToList();
        if (duplicateMaterialIds.Count > 0)
            ModelState.AddModelError(string.Empty, "Aynı ürün fişe birden fazla kez eklenemez.");

        var materialIds = activeLines.Select(x => x.MaterialId!.Value).Distinct().ToList();
        var materialRows = await (from material in _context.PrdMaterials.AsNoTracking()
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
                                      material.Type,
                                      material.RequiresLotTracking,
                                      material.RequiresExpirationDate,
                                      UnitCode = unit.Code,
                                      UnitName = unit.Name
                                  }).ToListAsync(cancellationToken);

        var recipeCandidateIds = materialRows
            .Where(x => x.Type != PrdMaterialType.FinishedProduct && x.Type != PrdMaterialType.SemiFinished)
            .Select(x => x.ID)
            .ToList();
        var recipeProductIds = recipeCandidateIds.Count == 0
            ? []
            : await _context.PrdRecipes.AsNoTracking()
                .Where(x => recipeCandidateIds.Contains(x.ProductMaterialId) && x.IsDelete != true)
                .Select(x => x.ProductMaterialId)
                .Distinct()
                .ToListAsync(cancellationToken);
        var recipeProductIdSet = recipeProductIds.ToHashSet();
        var materials = materialRows
            .Where(x => x.Type == PrdMaterialType.FinishedProduct
                        || x.Type == PrdMaterialType.SemiFinished
                        || recipeProductIdSet.Contains(x.ID))
            .ToDictionary(x => x.ID);

        var now = DateTime.Now;
        var user = User.Identity?.Name;
        var preparedLines = new List<PrdLogoProductionReceiptLine>();
        for (var index = 0; index < activeLines.Count; index++)
        {
            var input = activeLines[index];
            if (!input.MaterialId.HasValue || !materials.TryGetValue(input.MaterialId.Value, out var material))
            {
                ModelState.AddModelError(string.Empty, $"{index + 1}. satırdaki mamul veya yarı mamul bulunamadı.");
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
                ModelState.AddModelError(string.Empty, $"{material.Code} için Logo'ya gönderilecek birim kodu bulunmuyor.");
                continue;
            }
            if (!TryParseDecimal(input.Quantity, out var quantity) || quantity <= 0)
            {
                ModelState.AddModelError(string.Empty, $"{material.Code} için miktar sıfırdan büyük olmalıdır.");
                continue;
            }

            var lotNumber = input.LotNumber?.Trim();
            if ((material.RequiresLotTracking || input.ProductionDate.HasValue || input.ExpirationDate.HasValue)
                && string.IsNullOrWhiteSpace(lotNumber))
            {
                ModelState.AddModelError($"Lines[{index}].LotNumber", $"{material.Code} için parti numarası zorunludur.");
            }
            if ((material.RequiresLotTracking || !string.IsNullOrWhiteSpace(lotNumber))
                && !input.ProductionDate.HasValue)
            {
                ModelState.AddModelError($"Lines[{index}].ProductionDate", $"{material.Code} için üretim tarihi zorunludur.");
            }
            if (material.RequiresExpirationDate && !input.ExpirationDate.HasValue)
            {
                ModelState.AddModelError($"Lines[{index}].ExpirationDate", $"{material.Code} için son kullanma tarihi zorunludur.");
            }
            if (input.ProductionDate.HasValue && input.ExpirationDate.HasValue
                && input.ExpirationDate.Value.Date < input.ProductionDate.Value.Date)
            {
                ModelState.AddModelError($"Lines[{index}].ExpirationDate", $"{material.Code} için son kullanma tarihi üretim tarihinden önce olamaz.");
            }

            preparedLines.Add(new PrdLogoProductionReceiptLine
            {
                Sequence = preparedLines.Count + 1,
                MaterialId = material.ID,
                UnitId = material.UnitId,
                Quantity = quantity,
                LotNumber = string.IsNullOrWhiteSpace(lotNumber) ? null : lotNumber,
                ProductionDate = input.ProductionDate?.Date,
                ExpirationDate = input.ExpirationDate?.Date,
                Description = input.Description?.Trim(),
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            });
        }

        if (!ModelState.IsValid)
        {
            model.Lines = activeLines.Count == 0 ? [new()] : activeLines;
            return View("Form", model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        PrdLogoProductionReceipt receipt;
        if (model.Id.HasValue)
        {
            receipt = await _context.PrdLogoProductionReceipts
                .FirstOrDefaultAsync(x => x.ID == model.Id.Value && x.IsDelete != true, cancellationToken)
                ?? throw new InvalidOperationException("Düzenlenecek üretim giriş fişi bulunamadı.");
            if (receipt.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed))
            {
                TempData["error"] = "Yalnızca taslak veya aktarım hatası olan fiş değiştirilebilir.";
                return RedirectToAction(nameof(Detay), new { id = receipt.ID });
            }

            var oldLines = await _context.PrdLogoProductionReceiptLines
                .Where(x => x.ReceiptId == receipt.ID)
                .ToListAsync(cancellationToken);
            _context.PrdLogoProductionReceiptLines.RemoveRange(oldLines);
            receipt.Status = PrdLogoTransferStatus.Draft;
            receipt.LastError = null;
            receipt.UpdateDate = now;
            receipt.UpdateUserID = user;
        }
        else
        {
            receipt = new PrdLogoProductionReceipt
            {
                DocumentNumber = $"LUG-{now:yyyyMMddHHmmssfff}",
                Status = PrdLogoTransferStatus.Draft,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            };
            _context.PrdLogoProductionReceipts.Add(receipt);
        }

        receipt.DocumentDate = model.DocumentDate.Date;
        receipt.LogoSlipNumber = string.IsNullOrWhiteSpace(model.LogoSlipNumber) ? null : model.LogoSlipNumber.Trim();
        receipt.WarehouseNumber = model.WarehouseNumber;
        receipt.DivisionNumber = model.DivisionNumber;
        receipt.DepartmentNumber = model.DepartmentNumber;
        receipt.FactoryNumber = model.FactoryNumber;
        receipt.Notes = model.Notes?.Trim();

        await _context.SaveChangesAsync(cancellationToken);
        foreach (var line in preparedLines)
            line.ReceiptId = receipt.ID;
        _context.PrdLogoProductionReceiptLines.AddRange(preparedLines);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["success"] = $"{receipt.DocumentNumber} numaralı Logo üretim giriş taslağı kaydedildi.";
        return RedirectToAction(nameof(Detay), new { id = receipt.ID });
    }

    [HttpGet, Authorize(Policy = ViewPolicy)]
    public async Task<IActionResult> Detay(int id, CancellationToken cancellationToken)
    {
        SetModule();
        var model = await _context.PrdLogoProductionReceipts.AsNoTracking()
            .Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new LogoProductionReceiptDetailVM
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
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (model == null)
            return NotFound();

        model.Lines = await (from line in _context.PrdLogoProductionReceiptLines.AsNoTracking()
                             join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                             where line.ReceiptId == id && line.IsDelete != true
                             orderby line.Sequence
                             select new LogoProductionReceiptDetailLineVM
                             {
                                 Sequence = line.Sequence,
                                 MaterialCode = material.Code,
                                 MaterialName = material.Name,
                                 LogoCode = material.LogoCode ?? material.Code,
                                 Quantity = line.Quantity,
                                 UnitCode = unit.Code,
                                 UnitName = unit.Name,
                                 LotNumber = line.LotNumber,
                                 ProductionDate = line.ProductionDate,
                                 ExpirationDate = line.ExpirationDate,
                                 Description = line.Description
                             }).ToListAsync(cancellationToken);
        model.LineCount = model.Lines.Count;
        model.TotalQuantity = model.Lines.Sum(x => x.Quantity);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Gonder(int id, CancellationToken cancellationToken)
    {
        var receipt = await _context.PrdLogoProductionReceipts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (receipt == null)
            return NotFound();
        if (receipt.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed))
        {
            TempData["error"] = receipt.Status == PrdLogoTransferStatus.Sent
                ? "Bu fiş daha önce Logo'ya aktarılmış; ikinci kez gönderilemez."
                : "Fiş şu anda gönderime uygun durumda değil.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (!string.IsNullOrWhiteSpace(receipt.LogoReference))
        {
            TempData["error"] = "Logo tarafında bu portal kaydına bağlı bir fiş referansı bulunuyor. Logo'daki hatalı fişi silin, ardından admin olarak 'Yeniden Aktarıma Aç' işlemini kullanın.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var settings = await _settingsProvider.GetAsync(cancellationToken);
        if (!settings.Success || settings.Options == null)
        {
            TempData["error"] = settings.Message;
            return RedirectToAction(nameof(Detay), new { id });
        }

        var lines = await (from line in _context.PrdLogoProductionReceiptLines.AsNoTracking()
                           join material in _context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                           join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                           where line.ReceiptId == id && line.IsDelete != true
                           orderby line.Sequence
                           select new
                           {
                               line.Sequence,
                               Code = material.LogoCode ?? material.Code,
                               line.Quantity,
                               UnitCode = unit.Code,
                               line.LotNumber,
                               line.ProductionDate,
                               line.ExpirationDate,
                               line.Description
                           }).ToListAsync(cancellationToken);
        if (lines.Count == 0 || lines.Any(x => string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.UnitCode)))
        {
            TempData["error"] = "Fişte Logo kodu ve birimi geçerli en az bir ürün bulunmalıdır.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var materialCodes = lines
            .Select(x => x.Code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var logoMaterialRows = await _logoContext.Items
            .AsNoTracking()
            .Where(x => x.ACTIVE == 0 && materialCodes.Contains(x.CODE))
            .Select(x => new { x.CODE, x.LOGICALREF })
            .ToListAsync(cancellationToken);
        var logoMaterialReferences = logoMaterialRows
            .GroupBy(x => x.CODE.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().LOGICALREF, StringComparer.OrdinalIgnoreCase);
        var missingLogoCodes = materialCodes
            .Where(code => !logoMaterialReferences.ContainsKey(code))
            .ToList();
        if (missingLogoCodes.Count > 0)
        {
            TempData["error"] = "Logo'da aktif malzeme kartı bulunamadı: " + string.Join(", ", missingLogoCodes);
            return RedirectToAction(nameof(Detay), new { id });
        }
        if (lines.Any(x => !string.IsNullOrWhiteSpace(x.LotNumber) && !x.ProductionDate.HasValue))
        {
            TempData["error"] = "Parti numarası bulunan tüm ürün satırlarında üretim tarihi olmalıdır.";
            return RedirectToAction(nameof(Detay), new { id });
        }
        if (lines.Any(x => x.ProductionDate.HasValue && x.ExpirationDate.HasValue
                           && x.ExpirationDate.Value.Date < x.ProductionDate.Value.Date))
        {
            TempData["error"] = "Son kullanma tarihi üretim tarihinden önce olan ürün satırı Logo'ya gönderilemez.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var now = DateTime.Now;
        var claimed = await _context.PrdLogoProductionReceipts
            .Where(x => x.ID == id
                        && x.LogoReference == null
                        && (x.Status == PrdLogoTransferStatus.Draft || x.Status == PrdLogoTransferStatus.Failed))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.Status, PrdLogoTransferStatus.Sending)
                .SetProperty(x => x.LastAttemptDate, now)
                .SetProperty(x => x.AttemptCount, x => x.AttemptCount + 1)
                .SetProperty(x => x.UpdateDate, now)
                .SetProperty(x => x.UpdateUserID, User.Identity!.Name), cancellationToken);
        if (claimed == 0)
        {
            TempData["error"] = "Fiş başka bir işlem tarafından gönderiliyor veya durumu değişti.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var payload = new LogoProductionEntrySlipPayload
        {
            Group = 3,
            Number = string.IsNullOrWhiteSpace(receipt.LogoSlipNumber) ? "~" : receipt.LogoSlipNumber,
            Date = receipt.DocumentDate.Date,
            WarehouseNumber = receipt.WarehouseNumber,
            WarehouseCostGroup = receipt.WarehouseNumber,
            DivisionNumber = receipt.DivisionNumber,
            DepartmentNumber = receipt.DepartmentNumber,
            FactoryNumber = receipt.FactoryNumber,
            Notes = receipt.Notes,
            Transactions = new LogoRestItemCollection<LogoProductionEntrySlipLinePayload>
            {
                Items = lines.Select(x => new LogoProductionEntrySlipLinePayload
                {
                    MaterialCode = x.Code,
                    MaterialReference = logoMaterialReferences[x.Code.Trim()],
                    LineType = 0,
                    LineNumber = x.Sequence,
                    Quantity = x.Quantity,
                    UnitCode = x.UnitCode,
                    WarehouseNumber = receipt.WarehouseNumber,
                    WarehouseCostGroup = receipt.WarehouseNumber,
                    Description = x.Description
                }).ToList()
            }
        };

        LogoRestTransferResult result;
        try
        {
            result = await _logoClient.SendProductionEntrySlipAsync(settings.Options, payload, cancellationToken);
        }
        catch (Exception ex)
        {
            result = new LogoRestTransferResult(false, "Beklenmeyen Logo aktarım hatası: " + ex.Message);
        }

        var trackedReceipt = await _context.PrdLogoProductionReceipts.FirstAsync(x => x.ID == id, cancellationToken);
        trackedReceipt.Status = result.Success ? PrdLogoTransferStatus.Sent : PrdLogoTransferStatus.Failed;
        trackedReceipt.SentDate = result.Success ? DateTime.Now : null;
        trackedReceipt.LogoReference = result.LogoReference;
        trackedReceipt.LogoAssignedSlipNumber = result.LogoSlipNumber;
        trackedReceipt.LastError = result.Success ? null : result.Message;
        trackedReceipt.UpdateDate = DateTime.Now;
        trackedReceipt.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);

        TempData[result.Success ? "success" : "error"] = result.Message;
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> Iptal(int id, CancellationToken cancellationToken)
    {
        var receipt = await _context.PrdLogoProductionReceipts
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (receipt == null)
            return NotFound();
        if (receipt.Status is not (PrdLogoTransferStatus.Draft or PrdLogoTransferStatus.Failed))
        {
            TempData["error"] = "Yalnızca gönderilmemiş taslak veya hatalı fiş iptal edilebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (!string.IsNullOrWhiteSpace(receipt.LogoReference))
        {
            TempData["error"] = "Logo tarafında fiş referansı bulunan kayıt iptal edilemez. Önce Logo'daki fişi silin ve admin olarak yeniden aktarıma açın.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        receipt.Status = PrdLogoTransferStatus.Cancelled;
        receipt.IsActive = false;
        receipt.UpdateDate = DateTime.Now;
        receipt.UpdateUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(cancellationToken);
        TempData["success"] = "Üretim giriş fişi taslağı iptal edildi.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> Sil(int id, CancellationToken cancellationToken)
    {
        var receipt = await _context.PrdLogoProductionReceipts
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
        if (receipt == null)
            return NotFound();

        if (receipt.Status != PrdLogoTransferStatus.Cancelled)
        {
            TempData["error"] = "Yalnızca iptal edilmiş üretim giriş fişleri silinebilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        if (!string.IsNullOrWhiteSpace(receipt.LogoReference))
        {
            TempData["error"] = "Logo referansı bulunan bir fiş portal kaydından silinemez.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        var now = DateTime.Now;
        var actor = User.Identity?.Name ?? "Admin";
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        await _context.PrdLogoProductionReceiptLines
            .Where(x => x.ReceiptId == id && x.IsDelete != true)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(x => x.IsDelete, true)
                .SetProperty(x => x.IsActive, false)
                .SetProperty(x => x.DeleteDate, now)
                .SetProperty(x => x.DeleteUserID, actor), cancellationToken);

        receipt.IsDelete = true;
        receipt.IsActive = false;
        receipt.DeleteDate = now;
        receipt.DeleteUserID = actor;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        TempData["success"] = $"{receipt.DocumentNumber} numaralı iptal edilmiş fiş silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> YenidenAktarimaAc(int id, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var updated = await _context.PrdLogoProductionReceipts
            .Where(x => x.ID == id
                        && x.IsDelete != true
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
                .SetProperty(x => x.UpdateUserID, User.Identity!.Name), cancellationToken);

        if (updated == 0)
        {
            var exists = await _context.PrdLogoProductionReceipts
                .AnyAsync(x => x.ID == id && x.IsDelete != true, cancellationToken);
            if (!exists)
                return NotFound();

            TempData["error"] = "Yalnızca Logo'da karşılığı bulunan aktarılmış veya hatalı bir fiş yeniden aktarıma açılabilir.";
            return RedirectToAction(nameof(Detay), new { id });
        }

        TempData["success"] = "Fiş taslağa döndürüldü. Gerekirse düzenleyip Logo'ya yeniden gönderebilirsiniz.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpGet, Authorize(Policy = ManagePolicy)]
    public async Task<IActionResult> UrunAra(string? q, int page = 1, CancellationToken cancellationToken = default)
    {
        const int pageSize = 20;
        page = Math.Max(1, page);
        q = q?.Trim();
        var query = from material in _context.PrdMaterials.AsNoTracking()
                    join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                    where material.IsActive != false && material.IsDelete != true
                          && (material.Type == PrdMaterialType.FinishedProduct
                              || material.Type == PrdMaterialType.SemiFinished
                              || _context.PrdRecipes.Any(recipe => recipe.ProductMaterialId == material.ID && recipe.IsDelete != true))
                    select new { material, unit };
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(x => x.material.Code.Contains(q) || x.material.Name.Contains(q) || (x.material.LogoCode != null && x.material.LogoCode.Contains(q)));

        var rows = await query.OrderBy(x => x.material.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(x => new
            {
                id = x.material.ID,
                text = x.material.Code + " - " + x.material.Name,
                logoCode = x.material.LogoCode ?? x.material.Code,
                unit = x.unit.Code + " - " + x.unit.Name
            }).ToListAsync(cancellationToken);

        return Json(new
        {
            results = rows.Take(pageSize),
            pagination = new { more = rows.Count > pageSize }
        });
    }

    private void SetModule() => ViewBag.Modul = "LogoFisleri";

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out result)
               || decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);
    }
}
