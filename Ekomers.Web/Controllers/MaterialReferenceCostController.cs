using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class MaterialReferenceCostController : Controller
{
    private const string ReferenceCostEditPolicy = "ReferansMaliyetDuzenle";
    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logoContext;

    public MaterialReferenceCostController(ApplicationDbContext context, LogoContext logoContext)
    {
        _context = context;
        _logoContext = logoContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, string status = "missing", CancellationToken ct = default)
    {
        ViewBag.Modul = "YeniUretim";
        search = Clean(search) ?? string.Empty;
        status = NormalizeStatus(status);

        var usages = await (
            from item in _context.PrdRecipeItems.AsNoTracking()
            join version in _context.PrdRecipeVersions.AsNoTracking() on item.RecipeVersionId equals version.ID
            where item.IsDelete != true && version.IsDelete != true
            select new { item.MaterialId, version.RecipeId })
            .Distinct()
            .ToListAsync(ct);

        var usageByMaterial = usages.GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Select(y => y.RecipeId).Distinct().Count());
        var materialIds = usageByMaterial.Keys.ToList();

        var materials = materialIds.Count == 0
            ? []
            : await (
                from material in _context.PrdMaterials.AsNoTracking()
                join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                where materialIds.Contains(material.ID) && material.IsDelete != true
                select new
                {
                    material.ID,
                    material.Code,
                    material.Name,
                    material.Type,
                    UnitCode = unit.Code,
                    UnitName = unit.Name
                }).ToListAsync(ct);

        var stockRows = materialIds.Count == 0
            ? []
            : await (
                from movement in _context.PrdStockMovements.AsNoTracking()
                join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID
                where materialIds.Contains(movement.MaterialId) && movement.IsDelete != true &&
                      movement.Direction == PrdStockDirection.In &&
                      movement.MovementType != PrdStockMovementType.Transfer && movement.UnitCost > 0
                select new
                {
                    movement.ID,
                    movement.MaterialId,
                    movement.UnitCost,
                    movement.MovementDate,
                    UnitCode = unit.Code,
                    UnitName = unit.Name
                }).ToListAsync(ct);
        var stockByMaterial = stockRows.GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.MovementDate).ThenByDescending(y => y.ID).First());

        var today = DateTime.Today;
        var referenceRows = materialIds.Count == 0
            ? []
            : await (
                from cost in _context.PrdMaterialReferenceCosts.AsNoTracking()
                join unit in _context.PrdUnits.AsNoTracking() on cost.UnitId equals unit.ID
                where materialIds.Contains(cost.MaterialId) && cost.IsDelete != true &&
                      cost.ValidFrom <= today && (cost.ValidTo == null || cost.ValidTo >= today)
                select new
                {
                    cost.MaterialId,
                    cost.VersionNumber,
                    cost.UnitCost,
                    cost.CurrencyCode,
                    cost.ExchangeRate,
                    cost.UnitCostTry,
                    cost.ValidFrom,
                    cost.Source,
                    UnitCode = unit.Code,
                    UnitName = unit.Name
                }).ToListAsync(ct);
        var referenceByMaterial = referenceRows.GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.OrderByDescending(y => y.ValidFrom).ThenByDescending(y => y.VersionNumber).First());

        var allRows = materials.Select(material =>
        {
            stockByMaterial.TryGetValue(material.ID, out var stock);
            referenceByMaterial.TryGetValue(material.ID, out var reference);
            return new MaterialReferenceCostListRowVM
            {
                MaterialId = material.ID,
                MaterialCode = material.Code,
                MaterialName = material.Name,
                MaterialType = material.Type.ToTurkish(),
                BaseUnit = ProductionUnitNormalizer.DisplayName(material.UnitCode, material.UnitName),
                RecipeCount = usageByMaterial.GetValueOrDefault(material.ID),
                HasStockCost = stock != null,
                StockUnitCostTry = stock?.UnitCost,
                StockCostUnit = stock == null ? null : ProductionUnitNormalizer.DisplayName(stock.UnitCode, stock.UnitName),
                StockCostDate = stock?.MovementDate,
                ReferenceVersion = reference?.VersionNumber,
                ReferenceUnitCost = reference?.UnitCost,
                ReferenceCurrencyCode = reference?.CurrencyCode,
                ReferenceExchangeRate = reference?.ExchangeRate,
                ReferenceUnitCostTry = reference?.UnitCostTry,
                ReferenceUnit = reference == null ? null : ProductionUnitNormalizer.DisplayName(reference.UnitCode, reference.UnitName),
                ReferenceValidFrom = reference?.ValidFrom,
                ReferenceSource = reference == null ? null : reference.Source.ToTurkish()
            };
        }).OrderBy(x => x.MaterialCode).ToList();

        var model = new MaterialReferenceCostListVM
        {
            Search = search,
            Status = status,
            MissingCount = allRows.Count(x => !x.HasStockCost && !x.ReferenceUnitCostTry.HasValue),
            ReferenceCount = allRows.Count(x => !x.HasStockCost && x.ReferenceUnitCostTry.HasValue),
            StockCount = allRows.Count(x => x.HasStockCost)
        };

        IEnumerable<MaterialReferenceCostListRowVM> filtered = allRows;
        if (!string.IsNullOrWhiteSpace(search))
        {
            filtered = filtered.Where(x => x.MaterialCode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                           x.MaterialName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }
        filtered = status switch
        {
            "missing" => filtered.Where(x => !x.HasStockCost && !x.ReferenceUnitCostTry.HasValue),
            "reference" => filtered.Where(x => !x.HasStockCost && x.ReferenceUnitCostTry.HasValue),
            "stock" => filtered.Where(x => x.HasStockCost),
            _ => filtered
        };
        model.Rows = filtered.ToList();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Yeni(int materialId, string? returnUrl = null, CancellationToken ct = default)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await BuildEditModelAsync(materialId, returnUrl, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(MaterialReferenceCostEditVM input, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var materialExists = await _context.PrdMaterials.AnyAsync(x => x.ID == input.MaterialId && x.IsDelete != true, ct);
        var unitExists = await _context.PrdUnits.AnyAsync(x => x.ID == input.UnitId && x.IsDelete != true && x.IsActive != false, ct);
        if (!materialExists || !unitExists)
        {
            TempData["error"] = "Seçilen malzeme veya birim bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var currency = (input.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (currency.Length != 3 || !currency.All(char.IsLetter) ||
            !TryParseDecimal(input.UnitCost, out var unitCost) || unitCost <= 0 ||
            !TryParseDecimal(input.ExchangeRate, out var exchangeRate) || exchangeRate <= 0 ||
            input.ValidFrom == default || !Enum.IsDefined(input.Source))
        {
            ModelState.AddModelError(string.Empty, "Birim maliyet, para birimi, kur ve başlangıç tarihi alanlarını geçerli giriniz.");
            var invalidModel = await BuildEditModelAsync(input.MaterialId, input.ReturnUrl, ct, input);
            return invalidModel == null ? NotFound() : View(invalidModel);
        }
        if (currency == "TRY") exchangeRate = 1m;

        var validFrom = input.ValidFrom.Date;
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var latest = await _context.PrdMaterialReferenceCosts
            .Where(x => x.MaterialId == input.MaterialId && x.IsDelete != true)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.ID)
            .FirstOrDefaultAsync(ct);
        if (latest != null && validFrom <= latest.ValidFrom.Date)
        {
            await transaction.RollbackAsync(ct);
            ModelState.AddModelError(nameof(input.ValidFrom), $"Yeni versiyonun başlangıcı son versiyon tarihinden ({latest.ValidFrom:dd.MM.yyyy}) sonra olmalıdır.");
            var invalidModel = await BuildEditModelAsync(input.MaterialId, input.ReturnUrl, ct, input);
            return invalidModel == null ? NotFound() : View(invalidModel);
        }

        var now = DateTime.Now;
        var user = User.Identity?.Name;
        if (latest != null)
        {
            latest.ValidTo = validFrom.AddDays(-1);
            latest.IsActive = false;
            latest.UpdateDate = now;
            latest.UpdateUserID = user;
        }
        _context.PrdMaterialReferenceCosts.Add(new PrdMaterialReferenceCost
        {
            MaterialId = input.MaterialId,
            VersionNumber = (latest?.VersionNumber ?? 0) + 1,
            UnitId = input.UnitId,
            UnitCost = unitCost,
            CurrencyCode = currency,
            ExchangeRate = exchangeRate,
            UnitCostTry = unitCost * exchangeRate,
            ValidFrom = validFrom,
            Source = input.Source,
            Notes = Clean(input.Notes),
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        });
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = "Stok miktarını değiştirmeyen yeni referans maliyet versiyonu kaydedildi.";

        if (!string.IsNullOrWhiteSpace(input.ReturnUrl) && Url.IsLocalUrl(input.ReturnUrl))
            return LocalRedirect(input.ReturnUrl);
        return RedirectToAction(nameof(Detay), new { materialId = input.MaterialId });
    }

    [HttpGet]
    [Authorize(Policy = ReferenceCostEditPolicy)]
    public async Task<IActionResult> Duzenle(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var cost = await _context.PrdMaterialReferenceCosts.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (cost == null) return NotFound();

        var model = await BuildEditModelAsync(cost.MaterialId, null, ct);
        if (model == null) return NotFound();
        ApplyExistingCost(model, cost);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Policy = ReferenceCostEditPolicy)]
    public async Task<IActionResult> Duzenle(MaterialReferenceCostEditVM input, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var cost = await _context.PrdMaterialReferenceCosts
            .FirstOrDefaultAsync(x => x.ID == input.CostId && x.IsDelete != true, ct);
        if (cost == null) return NotFound();

        var unitExists = await _context.PrdUnits.AnyAsync(x =>
            x.ID == input.UnitId && x.IsDelete != true && x.IsActive != false, ct);
        var currency = (input.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        if (!unitExists || currency.Length != 3 || !currency.All(char.IsLetter) ||
            !TryParseDecimal(input.UnitCost, out var unitCost) || unitCost <= 0 ||
            !TryParseDecimal(input.ExchangeRate, out var exchangeRate) || exchangeRate <= 0 ||
            input.ValidFrom == default || !Enum.IsDefined(input.Source))
        {
            ModelState.AddModelError(string.Empty, "Birim maliyet, maliyet birimi, para birimi, kur ve başlangıç tarihi alanlarını geçerli giriniz.");
            var invalidModel = await BuildEditModelAsync(cost.MaterialId, null, ct, input);
            if (invalidModel == null) return NotFound();
            invalidModel.CostId = cost.ID;
            invalidModel.VersionNumber = cost.VersionNumber;
            return View(invalidModel);
        }
        if (currency == "TRY") exchangeRate = 1m;

        var validFrom = input.ValidFrom.Date;
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var previous = await _context.PrdMaterialReferenceCosts
            .Where(x => x.MaterialId == cost.MaterialId && x.IsDelete != true &&
                        x.VersionNumber < cost.VersionNumber)
            .OrderByDescending(x => x.VersionNumber)
            .ThenByDescending(x => x.ID)
            .FirstOrDefaultAsync(ct);
        var next = await _context.PrdMaterialReferenceCosts
            .Where(x => x.MaterialId == cost.MaterialId && x.IsDelete != true &&
                        x.VersionNumber > cost.VersionNumber)
            .OrderBy(x => x.VersionNumber)
            .ThenBy(x => x.ID)
            .FirstOrDefaultAsync(ct);

        if ((previous != null && validFrom <= previous.ValidFrom.Date) ||
            (next != null && validFrom >= next.ValidFrom.Date))
        {
            await transaction.RollbackAsync(ct);
            var rangeMessage = previous != null && next != null
                ? $"Başlangıç tarihi {previous.ValidFrom:dd.MM.yyyy} ile {next.ValidFrom:dd.MM.yyyy} arasında olmalıdır."
                : previous != null
                    ? $"Başlangıç tarihi önceki versiyon tarihinden ({previous.ValidFrom:dd.MM.yyyy}) sonra olmalıdır."
                    : $"Başlangıç tarihi sonraki versiyon tarihinden ({next!.ValidFrom:dd.MM.yyyy}) önce olmalıdır.";
            ModelState.AddModelError(nameof(input.ValidFrom), rangeMessage);
            var invalidModel = await BuildEditModelAsync(cost.MaterialId, null, ct, input);
            if (invalidModel == null) return NotFound();
            invalidModel.CostId = cost.ID;
            invalidModel.VersionNumber = cost.VersionNumber;
            return View(invalidModel);
        }

        var now = DateTime.Now;
        var user = User.Identity?.Name;
        cost.UnitId = input.UnitId;
        cost.UnitCost = unitCost;
        cost.CurrencyCode = currency;
        cost.ExchangeRate = exchangeRate;
        cost.UnitCostTry = unitCost * exchangeRate;
        cost.ValidFrom = validFrom;
        cost.ValidTo = next == null ? null : next.ValidFrom.Date.AddDays(-1);
        cost.Source = input.Source;
        cost.Notes = Clean(input.Notes);
        cost.IsActive = next == null;
        cost.UpdateDate = now;
        cost.UpdateUserID = user;
        if (previous != null)
        {
            previous.ValidTo = validFrom.AddDays(-1);
            previous.IsActive = false;
            previous.UpdateDate = now;
            previous.UpdateUserID = user;
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"Referans maliyet v{cost.VersionNumber} güncellendi.";
        return RedirectToAction(nameof(Detay), new { materialId = cost.MaterialId });
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int materialId, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var material = await (
            from item in _context.PrdMaterials.AsNoTracking()
            join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID
            where item.ID == materialId && item.IsDelete != true
            select new
            {
                item.ID,
                MaterialCode = item.Code,
                MaterialName = item.Name,
                item.Type,
                UnitCode = unit.Code,
                UnitName = unit.Name
            })
            .FirstOrDefaultAsync(ct);
        if (material == null) return NotFound();

        var versions = await (
            from cost in _context.PrdMaterialReferenceCosts.AsNoTracking()
            join unit in _context.PrdUnits.AsNoTracking() on cost.UnitId equals unit.ID
            join batch0 in _context.PrdMaterialReferenceCostImportBatches.AsNoTracking() on cost.ImportBatchId equals (int?)batch0.ID into batchJoin
            from batch in batchJoin.DefaultIfEmpty()
            where cost.MaterialId == materialId && cost.IsDelete != true
            orderby cost.VersionNumber descending
            select new MaterialReferenceCostHistoryRowVM
            {
                Id = cost.ID,
                VersionNumber = cost.VersionNumber,
                UnitCost = cost.UnitCost,
                CurrencyCode = cost.CurrencyCode,
                ExchangeRate = cost.ExchangeRate,
                UnitCostTry = cost.UnitCostTry,
                Unit = ProductionUnitNormalizer.DisplayName(unit.Code, unit.Name),
                ValidFrom = cost.ValidFrom,
                ValidTo = cost.ValidTo,
                Source = cost.Source.ToTurkish(),
                Notes = cost.Notes,
                CreateDate = cost.CreateDate,
                CreatedUser = cost.CreateUserID ?? "-",
                UpdateDate = cost.UpdateDate,
                UpdatedUser = cost.UpdateUserID
                ,ImportBatchId = cost.ImportBatchId
                ,ImportBatchNumber = batch == null ? null : batch.BatchNumber
                ,SourceSheet = cost.SourceSheet
                ,SourceRow = cost.SourceRow
            }).ToListAsync(ct);

        return View(new MaterialReferenceCostDetailVM
        {
            MaterialId = material.ID,
            MaterialCode = material.MaterialCode,
            MaterialName = material.MaterialName,
            MaterialType = material.Type.ToTurkish(),
            BaseUnit = ProductionUnitNormalizer.DisplayName(material.UnitCode, material.UnitName),
            Versions = versions
        });
    }

    [HttpGet]
    public IActionResult ExcelAktar()
    {
        ViewBag.Modul = "YeniUretim";
        return View(new MaterialReferenceCostImportUploadVM());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestFormLimits(MultipartBodyLengthLimit = 10 * 1024 * 1024)]
    public async Task<IActionResult> ExcelOnizle(IFormFile? file, MaterialReferenceCostImportUploadVM input, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        if (file == null || file.Length == 0 || file.Length > 10 * 1024 * 1024 ||
            !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "En fazla 10 MB boyutunda bir .xlsx dosyası seçiniz.");
            return View("ExcelAktar", input);
        }
        if (input.ValidFrom == default)
        {
            ModelState.AddModelError(nameof(input.ValidFrom), "Geçerlilik başlangıcı zorunludur.");
            return View("ExcelAktar", input);
        }

        try
        {
            await using var stream = file.OpenReadStream();
            var model = await BuildImportPreviewAsync(stream, Path.GetFileName(file.FileName), input.ValidFrom.Date, Clean(input.Notes), ct);
            model.Payload = JsonSerializer.Serialize(model.Rows);
            return View(model);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("ExcelAktar", input);
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [RequestFormLimits(ValueLengthLimit = 2 * 1024 * 1024)]
    public async Task<IActionResult> ExcelKaydet(MaterialReferenceCostImportSaveVM input, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        List<MaterialReferenceCostImportPreviewRowVM>? postedRows;
        try
        {
            postedRows = JsonSerializer.Deserialize<List<MaterialReferenceCostImportPreviewRowVM>>(input.Payload);
        }
        catch (JsonException)
        {
            postedRows = null;
        }

        if (postedRows == null || input.ValidFrom == default)
        {
            TempData["error"] = "Aktarım ön izlemesi okunamadı. Dosyayı yeniden seçiniz.";
            return RedirectToAction(nameof(ExcelAktar));
        }

        var selected = postedRows
            .Where(x => x.Selected && x.CanImport && x.MaterialId > 0 && x.UnitId > 0 && x.UnitCostTry > 0)
            .GroupBy(x => x.MaterialId)
            .Select(x => x.First())
            .ToList();
        if (selected.Count == 0)
        {
            TempData["error"] = "Kaydedilecek en az bir geçerli satır seçiniz.";
            return RedirectToAction(nameof(ExcelAktar));
        }

        var materialIds = selected.Select(x => x.MaterialId).ToList();
        var unitIds = selected.Select(x => x.UnitId).Distinct().ToList();
        var validMaterialIds = await _context.PrdMaterials.AsNoTracking()
            .Where(x => materialIds.Contains(x.ID) && x.IsDelete != true).Select(x => x.ID).ToListAsync(ct);
        var validUnitIds = await _context.PrdUnits.AsNoTracking()
            .Where(x => unitIds.Contains(x.ID) && x.IsDelete != true && x.IsActive != false).Select(x => x.ID).ToListAsync(ct);
        selected = selected.Where(x => validMaterialIds.Contains(x.MaterialId) && validUnitIds.Contains(x.UnitId)).ToList();
        if (selected.Count == 0)
        {
            TempData["error"] = "Seçilen malzeme veya birimler artık geçerli değil.";
            return RedirectToAction(nameof(ExcelAktar));
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.Now;
        var user = User.Identity?.Name;
        var batch = new PrdMaterialReferenceCostImportBatch
        {
            BatchNumber = $"RMA-{now:yyyyMMddHHmmssfff}",
            FileName = Path.GetFileName(input.FileName),
            ValidFrom = input.ValidFrom.Date,
            ReviewCount = postedRows.Count(x => x.CanImport && x.HasConflict),
            SkippedCount = postedRows.Count(x => !x.CanImport),
            Notes = Clean(input.Notes),
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        };
        _context.PrdMaterialReferenceCostImportBatches.Add(batch);
        await _context.SaveChangesAsync(ct);

        var imported = 0;
        var dateRejected = 0;
        foreach (var row in selected)
        {
            var latest = await _context.PrdMaterialReferenceCosts
                .Where(x => x.MaterialId == row.MaterialId && x.IsDelete != true)
                .OrderByDescending(x => x.VersionNumber).ThenByDescending(x => x.ID)
                .FirstOrDefaultAsync(ct);
            if (latest != null && input.ValidFrom.Date <= latest.ValidFrom.Date)
            {
                dateRejected++;
                continue;
            }
            if (latest != null)
            {
                latest.ValidTo = input.ValidFrom.Date.AddDays(-1);
                latest.IsActive = false;
                latest.UpdateDate = now;
                latest.UpdateUserID = user;
            }
            _context.PrdMaterialReferenceCosts.Add(new PrdMaterialReferenceCost
            {
                MaterialId = row.MaterialId,
                VersionNumber = (latest?.VersionNumber ?? 0) + 1,
                UnitId = row.UnitId,
                UnitCost = decimal.Round(row.UnitCostTry, 6),
                CurrencyCode = "TRY",
                ExchangeRate = 1m,
                UnitCostTry = decimal.Round(row.UnitCostTry, 6),
                ValidFrom = input.ValidFrom.Date,
                Source = PrdMaterialReferenceCostSource.ExcelImport,
                ImportBatchId = batch.ID,
                SourceSheet = Clean(row.SourceSheet),
                SourceRow = row.SourceRow > 0 ? row.SourceRow : null,
                Notes = string.IsNullOrWhiteSpace(input.Notes) ? $"{batch.BatchNumber} toplu aktarımı" : input.Notes,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            });
            imported++;
        }
        batch.ImportedCount = imported;
        batch.SkippedCount += dateRejected;
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        TempData[imported > 0 ? "success" : "error"] = imported > 0
            ? $"{batch.BatchNumber}: {imported} maliyet versiyonu kaydedildi. {dateRejected} satır tarih sırası nedeniyle atlandı."
            : "Hiçbir kayıt eklenemedi. Geçerlilik tarihini mevcut sürümlerden sonraki bir tarih seçerek tekrar deneyiniz.";
        return RedirectToAction(nameof(TopluAktarimDetay), new { id = batch.ID });
    }

    [HttpGet]
    public async Task<IActionResult> TopluAktarimlar(CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var rows = await _context.PrdMaterialReferenceCostImportBatches.AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderByDescending(x => x.CreateDate).ThenByDescending(x => x.ID)
            .Select(x => new MaterialReferenceCostImportBatchListRowVM
            {
                Id = x.ID, BatchNumber = x.BatchNumber, FileName = x.FileName, ValidFrom = x.ValidFrom,
                ImportedCount = x.ImportedCount, ReviewCount = x.ReviewCount, SkippedCount = x.SkippedCount,
                CreateDate = x.CreateDate, CreatedUser = x.CreateUserID ?? "-", Notes = x.Notes
            }).ToListAsync(ct);
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> TopluAktarimDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await _context.PrdMaterialReferenceCostImportBatches.AsNoTracking()
            .Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new MaterialReferenceCostImportBatchDetailVM
            {
                Id = x.ID, BatchNumber = x.BatchNumber, FileName = x.FileName, ValidFrom = x.ValidFrom,
                ImportedCount = x.ImportedCount, ReviewCount = x.ReviewCount, SkippedCount = x.SkippedCount,
                CreateDate = x.CreateDate, CreatedUser = x.CreateUserID ?? "-", Notes = x.Notes
            }).FirstOrDefaultAsync(ct);
        if (model == null) return NotFound();
        model.Rows = await (
            from cost in _context.PrdMaterialReferenceCosts.AsNoTracking()
            join material in _context.PrdMaterials.AsNoTracking() on cost.MaterialId equals material.ID
            join unit in _context.PrdUnits.AsNoTracking() on cost.UnitId equals unit.ID
            where cost.ImportBatchId == id && cost.IsDelete != true
            orderby material.Code
            select new MaterialReferenceCostImportBatchDetailRowVM
            {
                CostId = cost.ID, MaterialId = material.ID, MaterialCode = material.Code, MaterialName = material.Name,
                VersionNumber = cost.VersionNumber, UnitCostTry = cost.UnitCostTry,
                Unit = ProductionUnitNormalizer.DisplayName(unit.Code, unit.Name), SourceSheet = cost.SourceSheet ?? "-",
                SourceRow = cost.SourceRow, ValidFrom = cost.ValidFrom, ValidTo = cost.ValidTo
            }).ToListAsync(ct);
        return View(model);
    }

    private async Task<MaterialReferenceCostImportPreviewVM> BuildImportPreviewAsync(
        Stream stream, string fileName, DateTime validFrom, string? notes, CancellationToken ct)
    {
        using var workbook = new XLWorkbook(stream);
        var rawSheet = workbook.Worksheets.FirstOrDefault(x => NormalizeComparable(x.Name) == "FORMULLER");
        var packagingSheet = workbook.Worksheets.FirstOrDefault(x => NormalizeComparable(x.Name) == "MAMULMALIYET");
        if (rawSheet == null || packagingSheet == null)
            throw new InvalidDataException("Dosyada 'Formüller' ve 'Mamül Maliyet' çalışma sayfaları birlikte bulunmalıdır.");

        var materials = await _context.PrdMaterials.AsNoTracking()
            .Where(x => x.IsDelete != true)
            .Select(x => new ImportMaterial(x.ID, x.Code, x.LogoCode, x.Name, x.Type, x.UnitId))
            .ToListAsync(ct);
        var materialByCode = new Dictionary<string, ImportMaterial>(StringComparer.OrdinalIgnoreCase);
        foreach (var material in materials)
        {
            if (!string.IsNullOrWhiteSpace(material.Code)) materialByCode.TryAdd(NormalizeCode(material.Code), material);
            if (!string.IsNullOrWhiteSpace(material.LogoCode)) materialByCode.TryAdd(NormalizeCode(material.LogoCode), material);
        }

        var units = await _context.PrdUnits.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive != false)
            .Select(x => new ImportUnit(x.ID, x.Code, x.Name)).ToListAsync(ct);
        var unitById = units.ToDictionary(x => x.Id);
        var kilogramUnit = units.FirstOrDefault(x => ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name) == "KG");

        var latestRecipes = await (
            from version in _context.PrdRecipeVersions.AsNoTracking()
            join recipe in _context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID
            join product in _context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID
            where version.IsDelete != true && recipe.IsDelete != true && product.IsDelete != true
            select new ImportRecipe(version.ID, version.RecipeId, version.VersionNumber, version.BaseQuantity,
                recipe.Name, product.Name)).ToListAsync(ct);
        latestRecipes = latestRecipes.GroupBy(x => x.RecipeId)
            .Select(x => x.OrderByDescending(y => y.VersionNumber).ThenByDescending(y => y.VersionId).First())
            .ToList();
        var recipeIds = latestRecipes.Select(x => x.VersionId).ToList();
        var recipeItems = recipeIds.Count == 0 ? [] : await _context.PrdRecipeItems.AsNoTracking()
            .Where(x => recipeIds.Contains(x.RecipeVersionId) && x.IsDelete != true)
            .Select(x => new ImportRecipeItem(x.RecipeVersionId, x.MaterialId, x.Quantity, x.PlannedWasteRate, x.UnitId))
            .ToListAsync(ct);

        var candidates = new List<ImportCandidate>();
        foreach (var row in rawSheet.RowsUsed())
        {
            var rowNumber = row.RowNumber();
            var sourceCode = rawSheet.Cell(rowNumber, 3).GetString().Trim();
            var code = NormalizeCode(sourceCode);
            if (string.IsNullOrWhiteSpace(code) || code == "STOKKODU") continue;
            var excelValue = ReadDecimal(rawSheet.Cell(rowNumber, 11));
            if (!excelValue.HasValue || excelValue <= 0) continue;
            var name = rawSheet.Cell(rowNumber, 4).GetString().Trim();
            if (!materialByCode.TryGetValue(code, out var material))
            {
                candidates.Add(ImportCandidate.Missing(sourceCode, name, rawSheet.Name, rowNumber, excelValue.Value,
                    "Malzeme kodu uygulama kataloğunda bulunamadı."));
                continue;
            }
            if (kilogramUnit == null)
            {
                candidates.Add(ImportCandidate.Missing(code, material.Name, rawSheet.Name, rowNumber, excelValue.Value,
                    "Kilogram birimi tanımlı değil."));
                continue;
            }
            candidates.Add(ImportCandidate.Ready(material, kilogramUnit, excelValue.Value, excelValue.Value,
                rawSheet.Name, rowNumber, null));
        }

        foreach (var block in ReadPackagingBlocks(packagingSheet))
        {
            foreach (var source in block.Rows)
            {
                var code = NormalizeCode(source.Code);
                if (!materialByCode.TryGetValue(code, out var material))
                {
                    candidates.Add(ImportCandidate.Missing(source.Code, source.Name, packagingSheet.Name, source.RowNumber,
                        source.ExcelValueTry, "Malzeme kodu uygulama kataloğunda bulunamadı.", block.ProductName));
                    continue;
                }

                var normalizedProduct = NormalizeComparable(block.ProductName);
                var matchingRecipes = latestRecipes.Where(x =>
                        NormalizeComparable(x.RecipeName) == normalizedProduct || NormalizeComparable(x.ProductName) == normalizedProduct)
                    .ToList();
                if (matchingRecipes.Count == 0)
                {
                    matchingRecipes = latestRecipes.Where(x =>
                        NormalizeComparable(x.RecipeName).Contains(normalizedProduct, StringComparison.Ordinal) ||
                        NormalizeComparable(x.ProductName).Contains(normalizedProduct, StringComparison.Ordinal) ||
                        normalizedProduct.Contains(NormalizeComparable(x.RecipeName), StringComparison.Ordinal) ||
                        normalizedProduct.Contains(NormalizeComparable(x.ProductName), StringComparison.Ordinal)).ToList();
                }
                var matches = matchingRecipes.Select(recipe => new
                    {
                        Recipe = recipe,
                        Items = recipeItems.Where(x => x.RecipeVersionId == recipe.VersionId && x.MaterialId == material.Id).ToList()
                    })
                    .Where(x => x.Items.Count > 0)
                    .ToList();
                if (matches.Count != 1)
                {
                    candidates.Add(ImportCandidate.Missing(code, material.Name, packagingSheet.Name, source.RowNumber,
                        source.ExcelValueTry, matches.Count == 0
                            ? $"'{block.ProductName}' reçetesiyle bu ambalaj kalemi eşleştirilemedi."
                            : $"'{block.ProductName}' için birden fazla reçete eşleşti.", block.ProductName));
                    continue;
                }
                var itemUnits = matches[0].Items.Select(x => x.UnitId).Distinct().ToList();
                var usage = matches[0].Items.Sum(x => x.Quantity * (1m + x.WasteRate / 100m));
                usage = matches[0].Recipe.BaseQuantity > 0 ? usage / matches[0].Recipe.BaseQuantity : 0m;
                if (itemUnits.Count != 1 || usage <= 0 || !unitById.TryGetValue(itemUnits[0], out var itemUnit))
                {
                    candidates.Add(ImportCandidate.Missing(code, material.Name, packagingSheet.Name, source.RowNumber,
                        source.ExcelValueTry, "Reçetedeki kullanım miktarı veya birimi maliyete çevrilemedi.", block.ProductName));
                    continue;
                }
                var unitCost = source.ExcelValueTry / usage;
                candidates.Add(ImportCandidate.Ready(material, itemUnit, unitCost, source.ExcelValueTry,
                    packagingSheet.Name, source.RowNumber, block.ProductName));
            }
        }

        var rows = new List<MaterialReferenceCostImportPreviewRowVM>();
        foreach (var missing in candidates.Where(x => !x.CanImport)
                     .GroupBy(x => new { x.MaterialCode, x.SourceSheet, x.Message, x.ProductName }).Select(x => x.First()))
        {
            rows.Add(ToPreviewRow(missing, false, false, missing.Message));
        }
        foreach (var group in candidates.Where(x => x.CanImport).GroupBy(x => new { x.MaterialId, x.UnitId }))
        {
            var values = group.Select(x => decimal.Round(x.UnitCostTry, 6)).Distinct().OrderBy(x => x).ToList();
            var first = group.First();
            var conflict = values.Count > 1;
            var message = conflict
                ? "Çakışan maliyetler: " + string.Join("; ", values.Select(x => x.ToString("N6", CultureInfo.GetCultureInfo("tr-TR"))))
                : group.Count() > 1 ? $"{group.Count()} Excel satırında aynı maliyet bulundu." : "Aktarıma hazır.";
            rows.Add(ToPreviewRow(first with { UnitCostTry = values[0] }, !conflict, conflict, message));
        }

        rows = rows.OrderByDescending(x => x.Selected).ThenBy(x => x.MaterialCode).ThenBy(x => x.SourceSheet).ToList();
        return new MaterialReferenceCostImportPreviewVM
        {
            FileName = fileName,
            ValidFrom = validFrom,
            Notes = notes,
            Rows = rows
        };
    }

    private static List<PackagingBlock> ReadPackagingBlocks(IXLWorksheet sheet)
    {
        var result = new List<PackagingBlock>();
        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 0;
        var headers = Enumerable.Range(1, lastRow)
            .Where(row => NormalizeComparable(sheet.Cell(row, 3).GetString()) == "STOKKODU")
            .ToList();
        for (var index = 0; index < headers.Count; index++)
        {
            var start = headers[index] + 1;
            var end = index + 1 < headers.Count ? headers[index + 1] - 1 : lastRow;
            var productName = Enumerable.Range(start, Math.Max(0, end - start + 1))
                .Where(row => NormalizeComparable(sheet.Cell(row, 1).GetString()) == "URUNADI")
                .Select(row => sheet.Cell(row, 2).GetString().Trim()).FirstOrDefault() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(productName)) continue;
            var rows = new List<PackagingSourceRow>();
            for (var row = start; row <= end; row++)
            {
                var code = sheet.Cell(row, 3).GetString().Trim();
                var value = ReadDecimal(sheet.Cell(row, 5));
                if (string.IsNullOrWhiteSpace(code) || !value.HasValue || value <= 0) continue;
                rows.Add(new PackagingSourceRow(row, code, sheet.Cell(row, 4).GetString().Trim(), value.Value));
            }
            if (rows.Count > 0) result.Add(new PackagingBlock(productName, rows));
        }
        return result;
    }

    private static MaterialReferenceCostImportPreviewRowVM ToPreviewRow(
        ImportCandidate source, bool selected, bool conflict, string message) => new()
    {
        Selected = selected,
        MaterialId = source.MaterialId,
        MaterialCode = source.MaterialCode,
        MaterialName = source.MaterialName,
        MaterialType = source.MaterialType,
        UnitId = source.UnitId,
        Unit = source.Unit,
        UnitCostTry = source.UnitCostTry,
        ExcelValueTry = source.ExcelValueTry,
        SourceSheet = source.SourceSheet,
        SourceRow = source.SourceRow,
        ProductName = source.ProductName,
        CanImport = source.CanImport,
        HasConflict = conflict,
        Message = message
    };

    private static decimal? ReadDecimal(IXLCell cell)
    {
        var value = cell.HasFormula ? cell.CachedValue : cell.Value;
        if (value.IsNumber) return Convert.ToDecimal(value.GetNumber(), CultureInfo.InvariantCulture);
        var text = value.ToString(CultureInfo.InvariantCulture).Trim();
        return decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var invariant) ? invariant :
            decimal.TryParse(text, NumberStyles.Any, CultureInfo.GetCultureInfo("tr-TR"), out var turkish) ? turkish : null;
    }

    private static string NormalizeCode(string? value) => NormalizeComparable(value);

    private static string NormalizeComparable(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Trim().ToUpperInvariant().Replace('İ', 'I').Normalize(NormalizationForm.FormD);
        return string.Concat(normalized.Where(x => CharUnicodeInfo.GetUnicodeCategory(x) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(x)));
    }

    private sealed record ImportMaterial(int Id, string Code, string? LogoCode, string Name, PrdMaterialType Type, int UnitId);
    private sealed record ImportUnit(int Id, string Code, string Name);
    private sealed record ImportRecipe(int VersionId, int RecipeId, int VersionNumber, decimal BaseQuantity, string RecipeName, string ProductName);
    private sealed record ImportRecipeItem(int RecipeVersionId, int MaterialId, decimal Quantity, decimal WasteRate, int UnitId);
    private sealed record PackagingSourceRow(int RowNumber, string Code, string Name, decimal ExcelValueTry);
    private sealed record PackagingBlock(string ProductName, List<PackagingSourceRow> Rows);
    private sealed record ImportCandidate(
        bool CanImport, int MaterialId, string MaterialCode, string MaterialName, string MaterialType,
        int UnitId, string Unit, decimal UnitCostTry, decimal ExcelValueTry, string SourceSheet,
        int SourceRow, string? ProductName, string Message)
    {
        public static ImportCandidate Ready(ImportMaterial material, ImportUnit unit, decimal unitCost, decimal excelValue,
            string sheet, int row, string? productName) => new(true, material.Id, material.Code, material.Name,
            material.Type.ToTurkish(), unit.Id, ProductionUnitNormalizer.DisplayName(unit.Code, unit.Name),
            decimal.Round(unitCost, 6), excelValue, sheet, row, productName, string.Empty);

        public static ImportCandidate Missing(string code, string name, string sheet, int row, decimal excelValue,
            string message, string? productName = null) => new(false, 0, code, name, "-", 0, "-", 0m,
            excelValue, sheet, row, productName, message);
    }

    private async Task<MaterialReferenceCostEditVM?> BuildEditModelAsync(
        int materialId, string? returnUrl, CancellationToken ct, MaterialReferenceCostEditVM? posted = null)
    {
        var material = await (
            from item in _context.PrdMaterials.AsNoTracking()
            join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID
            where item.ID == materialId && item.IsDelete != true
            select new { item.ID, item.Code, item.LogoCode, item.Name, item.Type, item.UnitId, UnitCode = unit.Code, UnitName = unit.Name })
            .FirstOrDefaultAsync(ct);
        if (material == null) return null;

        var units = await _context.PrdUnits.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive != false)
            .OrderBy(x => x.Name)
            .ToListAsync(ct);
        var current = await (
            from cost in _context.PrdMaterialReferenceCosts.AsNoTracking()
            join unit in _context.PrdUnits.AsNoTracking() on cost.UnitId equals unit.ID
            where cost.MaterialId == materialId && cost.IsDelete != true
            orderby cost.VersionNumber descending
            select new MaterialReferenceCostHistoryRowVM
            {
                Id = cost.ID,
                VersionNumber = cost.VersionNumber,
                UnitCost = cost.UnitCost,
                CurrencyCode = cost.CurrencyCode,
                ExchangeRate = cost.ExchangeRate,
                UnitCostTry = cost.UnitCostTry,
                Unit = ProductionUnitNormalizer.DisplayName(unit.Code, unit.Name),
                ValidFrom = cost.ValidFrom,
                ValidTo = cost.ValidTo,
                Source = cost.Source.ToTurkish(),
                Notes = cost.Notes,
                CreateDate = cost.CreateDate,
                CreatedUser = cost.CreateUserID ?? "-"
            }).FirstOrDefaultAsync(ct);

        var model = posted ?? new MaterialReferenceCostEditVM
        {
            MaterialId = material.ID,
            UnitId = current == null ? material.UnitId : 0,
            ValidFrom = current == null ? DateTime.Today :
                (current.ValidFrom.Date.AddDays(1) > DateTime.Today ? current.ValidFrom.Date.AddDays(1) : DateTime.Today),
            ReturnUrl = returnUrl
        };
        if (posted == null && current != null)
        {
            var lastUnitId = await _context.PrdMaterialReferenceCosts.AsNoTracking()
                .Where(x => x.ID == current.Id).Select(x => x.UnitId).FirstAsync(ct);
            model.UnitId = lastUnitId;
            model.UnitCost = current.UnitCost.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"));
            model.CurrencyCode = current.CurrencyCode;
            model.ExchangeRate = current.ExchangeRate.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"));
        }
        model.MaterialCode = material.Code;
        model.MaterialName = material.Name;
        model.MaterialType = material.Type.ToTurkish();
        model.BaseUnit = ProductionUnitNormalizer.DisplayName(material.UnitCode, material.UnitName);
        model.CurrentVersion = current;
        model.Units = units
            .GroupBy(x => ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name), StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => string.Equals(x.Code, group.Key, StringComparison.OrdinalIgnoreCase)).ThenBy(x => x.ID).First())
            .Select(x => new SelectListItem(
                ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name) + " - " + ProductionUnitNormalizer.DisplayName(x.Code, x.Name),
                x.ID.ToString(), x.ID == model.UnitId))
            .OrderBy(x => x.Text)
            .ToList();
        try
        {
            model.LogoPurchasePrices = await LoadLogoPurchasePricesAsync(
                string.IsNullOrWhiteSpace(material.LogoCode) ? material.Code : material.LogoCode,
                units.Select(x => new ImportUnit(x.ID, x.Code, x.Name)).ToList(), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            model.LogoPurchasePriceError = "Logo satınalma kayıtları okunamadı: " + ex.Message;
        }
        return model;
    }

    private async Task<List<LogoMaterialPurchasePriceVM>> LoadLogoPurchasePricesAsync(
        string materialCode, IReadOnlyCollection<ImportUnit> productionUnits, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (100)
                [Tarihi],
                [Fatura Numarası],
                [Cari Hesap Kodu],
                [Cari Hesap Unvanı],
                [Miktar],
                [Birim],
                [Birim Fiyat],
                [İşlem Döviz Türü],
                [kur],
                [SatIr Matrahı]
            FROM [dbo].[FATURA_DOKUMU_100] WITH (NOLOCK)
            WHERE [TUR] = N'Satınalma'
              AND [Fatura Türü] = N'Satınalma Faturası'
              AND [Fatura İptal Durumu] = N'İptal Edilmemiş'
              AND [Hizmet Kodu] = @materialCode
              AND [Miktar] > 0
            ORDER BY [Tarihi] DESC, [Fatura Numarası] DESC;
            """;

        var connection = _logoContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(ct);
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandTimeout = 15;
            var parameter = command.CreateParameter();
            parameter.ParameterName = "@materialCode";
            parameter.DbType = DbType.String;
            parameter.Value = materialCode.Trim();
            command.Parameters.Add(parameter);

            var result = new List<LogoMaterialPurchasePriceVM>();
            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var quantity = ReadLogoDecimal(reader, "Miktar");
                var lineNetTry = ReadLogoDecimal(reader, "SatIr Matrahı");
                var logoUnit = ReadLogoString(reader, "Birim");
                var canonicalUnit = ProductionUnitNormalizer.CanonicalCode(logoUnit, logoUnit);
                var productionUnit = productionUnits.FirstOrDefault(x =>
                    ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name) == canonicalUnit);
                var currency = ReadLogoString(reader, "İşlem Döviz Türü").ToUpperInvariant() switch
                {
                    "TL" => "TRY",
                    "EURO" => "EUR",
                    var value => value
                };
                result.Add(new LogoMaterialPurchasePriceVM
                {
                    InvoiceDate = ReadLogoDateTime(reader, "Tarihi"),
                    InvoiceNumber = ReadLogoString(reader, "Fatura Numarası"),
                    SupplierCode = ReadLogoString(reader, "Cari Hesap Kodu"),
                    SupplierName = ReadLogoString(reader, "Cari Hesap Unvanı"),
                    Quantity = quantity,
                    Unit = string.IsNullOrWhiteSpace(canonicalUnit)
                        ? logoUnit
                        : ProductionUnitNormalizer.DisplayName(canonicalUnit, logoUnit),
                    LogoUnitPrice = ReadLogoDecimal(reader, "Birim Fiyat"),
                    CurrencyCode = currency,
                    ExchangeRate = ReadLogoDecimal(reader, "kur"),
                    NetLineAmountTry = lineNetTry,
                    NetUnitPriceTry = quantity == 0 ? 0 : decimal.Abs(lineNetTry / quantity),
                    ProductionUnitId = productionUnit?.Id
                });
            }
            return result;
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    private static string ReadLogoString(System.Data.Common.DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        return value == DBNull.Value ? string.Empty : Convert.ToString(value, CultureInfo.CurrentCulture)?.Trim() ?? string.Empty;
    }

    private static decimal ReadLogoDecimal(System.Data.Common.DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        return value == DBNull.Value ? 0m : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static DateTime ReadLogoDateTime(System.Data.Common.DbDataReader reader, string name)
    {
        var value = reader.GetValue(reader.GetOrdinal(name));
        return value == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    private static string NormalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "reference" => "reference",
        "stock" => "stock",
        "all" => "all",
        _ => "missing"
    };

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        text = (text ?? string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out value) ||
               decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ApplyExistingCost(MaterialReferenceCostEditVM model, PrdMaterialReferenceCost cost)
    {
        model.CostId = cost.ID;
        model.VersionNumber = cost.VersionNumber;
        model.MaterialId = cost.MaterialId;
        model.UnitId = cost.UnitId;
        model.UnitCost = cost.UnitCost.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"));
        model.CurrencyCode = cost.CurrencyCode;
        model.ExchangeRate = cost.ExchangeRate.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"));
        model.ValidFrom = cost.ValidFrom;
        model.Source = cost.Source;
        model.Notes = cost.Notes;
        foreach (var unit in model.Units) unit.Selected = unit.Value == cost.UnitId.ToString();
    }
}
