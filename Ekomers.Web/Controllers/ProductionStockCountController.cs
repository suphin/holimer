using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Globalization;
using System.Text.Json;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class ProductionStockCountController : Controller
{
    private readonly ApplicationDbContext _context;

    public ProductionStockCountController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? warehouseId,
        int? sourceDocumentId = null,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "YeniUretim";

        var warehouses = await _context.PrdWarehouses
            .AsNoTracking()
            .Where(x => x.IsActive != false && x.IsDelete != true)
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Code)
            .Select(x => new { x.ID, x.Code, x.Name, x.Type })
            .ToListAsync(ct);

        PrdInventoryDocument? sourceDocument = null;
        List<int>? correctionMaterialIds = null;
        if (sourceDocumentId.HasValue)
        {
            sourceDocument = await _context.PrdInventoryDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ID == sourceDocumentId.Value &&
                    x.Type == PrdInventoryDocumentType.Adjustment &&
                    x.Status == PrdInventoryDocumentStatus.Posted &&
                    (x.SourceDocumentType == "WarehouseCount" || x.SourceDocumentType == "WarehouseCountCorrection") &&
                    x.IsDelete != true,
                    ct);

            if (sourceDocument == null || (!sourceDocument.SourceWarehouseId.HasValue && !sourceDocument.TargetWarehouseId.HasValue))
            {
                TempData["error"] = "Düzeltilecek depo sayımı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            warehouseId = sourceDocument.TargetWarehouseId ?? sourceDocument.SourceWarehouseId;
            correctionMaterialIds = await _context.PrdInventoryDocumentLines
                .AsNoTracking()
                .Where(x => x.InventoryDocumentId == sourceDocument.ID && x.IsDelete != true)
                .Select(x => x.MaterialId)
                .Distinct()
                .ToListAsync(ct);
        }

        warehouseId ??= warehouses.FirstOrDefault()?.ID;
        var model = new WarehouseCountVM
        {
            SourceDocumentId = sourceDocument?.ID,
            SourceDocumentNumber = sourceDocument?.DocumentNumber,
            WarehouseId = warehouseId ?? 0,
            Warehouses = warehouses
                .Select(x => new SelectListItem(
                    $"{x.Code} - {x.Name} ({x.Type.ToTurkish()})",
                    x.ID.ToString(),
                    x.ID == warehouseId))
                .ToList()
        };

        if (!warehouseId.HasValue)
            return View(model);

        var sessionRows = ReadCountList(warehouseId.Value, sourceDocument?.ID);
        if (sessionRows == null)
        {
            sessionRows = [];
            if (correctionMaterialIds is { Count: > 0 })
            {
                var correctionMaterials = await _context.PrdMaterials
                    .AsNoTracking()
                    .Where(x => correctionMaterialIds.Contains(x.ID) && x.IsActive != false && x.IsDelete != true)
                    .OrderBy(x => x.Code)
                    .Select(x => new { x.ID, x.Code })
                    .ToListAsync(ct);
                var correctionBalances = await CurrentBalancesAsync(warehouseId.Value, correctionMaterialIds, ct);
                sessionRows = correctionMaterials.Select(x =>
                {
                    var current = correctionBalances.GetValueOrDefault(x.ID);
                    return new WarehouseCountSessionItem
                    {
                        MaterialId = x.ID,
                        MaterialCode = x.Code,
                        SnapshotQuantity = current.ToString("0.######", CultureInfo.InvariantCulture),
                        CountedQuantity = current.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"))
                    };
                }).ToList();
            }
            SaveCountList(warehouseId.Value, sourceDocument?.ID, sessionRows);
        }

        model.Rows = await BuildRowsAsync(warehouseId.Value, sessionRows, ct);
        model.TotalCount = model.Rows.Count;

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SearchMaterials(
        string? term,
        int page = 1,
        int? sourceDocumentId = null,
        CancellationToken ct = default)
    {
        const int pageSize = 30;
        page = Math.Max(1, page);
        term = term?.Trim();
        var query = _context.PrdMaterials
            .AsNoTracking()
            .Where(x => x.IsActive != false && x.IsDelete != true);
        if (sourceDocumentId.HasValue)
            query = query.Where(x => _context.PrdInventoryDocumentLines.Any(line =>
                line.InventoryDocumentId == sourceDocumentId.Value &&
                line.MaterialId == x.ID &&
                line.IsDelete != true));
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.Code.Contains(term) || x.Name.Contains(term));

        var materials = await query
            .OrderBy(x => x.Code)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(x => new { x.ID, x.Code, x.Name })
            .ToListAsync(ct);
        return Json(new
        {
            results = materials.Take(pageSize).Select(x => new { id = x.ID, text = $"{x.Code} - {x.Name}" }),
            pagination = new { more = materials.Count > pageSize }
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(
        int warehouseId,
        int materialId,
        string? countedQuantity,
        int? sourceDocumentId,
        CancellationToken ct)
    {
        if (!await IsActiveWarehouseAsync(warehouseId, ct))
        {
            TempData["error"] = "Geçerli ve aktif bir depo seçiniz.";
            return RedirectToAction(nameof(Index));
        }
        if (!TryParseDecimal(countedQuantity, out var counted) || counted < 0)
        {
            TempData["error"] = "Sayım stoku sıfır veya sıfırdan büyük bir sayı olmalıdır.";
            return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
        }

        var material = await _context.PrdMaterials
            .AsNoTracking()
            .Where(x => x.ID == materialId && x.IsActive != false && x.IsDelete != true)
            .Select(x => new { x.ID, x.Code })
            .FirstOrDefaultAsync(ct);
        if (material == null)
        {
            TempData["error"] = "Seçilen ürün bulunamadı veya aktif değil.";
            return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
        }
        if (sourceDocumentId.HasValue && !await _context.PrdInventoryDocumentLines.AsNoTracking().AnyAsync(x =>
                x.InventoryDocumentId == sourceDocumentId.Value &&
                x.MaterialId == material.ID &&
                x.IsDelete != true,
                ct))
        {
            TempData["error"] = "Sayım düzeltmesine yalnızca önceki sayım belgesindeki ürünler eklenebilir.";
            return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
        }

        var rows = ReadCountList(warehouseId, sourceDocumentId) ?? [];
        var existing = rows.FirstOrDefault(x => x.MaterialId == material.ID);
        if (existing == null)
        {
            var balances = await CurrentBalancesAsync(warehouseId, [material.ID], ct);
            rows.Add(new WarehouseCountSessionItem
            {
                MaterialId = material.ID,
                MaterialCode = material.Code,
                SnapshotQuantity = balances.GetValueOrDefault(material.ID).ToString("0.######", CultureInfo.InvariantCulture),
                CountedQuantity = counted.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"))
            });
            TempData["success"] = $"{material.Code} sayım listesine eklendi.";
        }
        else
        {
            existing.CountedQuantity = counted.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"));
            TempData["success"] = $"{material.Code} sayım değeri güncellendi.";
        }
        SaveCountList(warehouseId, sourceDocumentId, rows);
        return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateItem(WarehouseCountVM model, int materialId)
    {
        var postedRow = model.Rows.LastOrDefault(x => x.MaterialId == materialId);
        if (postedRow == null || !TryParseDecimal(postedRow.CountedQuantity, out var counted) || counted < 0)
        {
            TempData["error"] = "Sayım stoku sıfır veya sıfırdan büyük bir sayı olmalıdır.";
            return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId });
        }
        var rows = ReadCountList(model.WarehouseId, model.SourceDocumentId) ?? [];
        var row = rows.FirstOrDefault(x => x.MaterialId == materialId);
        if (row == null)
            TempData["error"] = "Güncellenecek ürün geçici sayım listesinde bulunamadı.";
        else
        {
            row.CountedQuantity = counted.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR"));
            SaveCountList(model.WarehouseId, model.SourceDocumentId, rows);
            TempData["success"] = $"{row.MaterialCode} sayım değeri güncellendi.";
        }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult RemoveItem(int warehouseId, int materialId, int? sourceDocumentId)
    {
        var rows = ReadCountList(warehouseId, sourceDocumentId) ?? [];
        var row = rows.FirstOrDefault(x => x.MaterialId == materialId);
        if (row != null)
        {
            rows.Remove(row);
            SaveCountList(warehouseId, sourceDocumentId, rows);
            TempData["success"] = $"{row.MaterialCode} sayım listesinden kaldırıldı.";
        }
        return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Clear(int warehouseId, int? sourceDocumentId)
    {
        SaveCountList(warehouseId, sourceDocumentId, []);
        TempData["success"] = "Geçici sayım listesi temizlendi.";
        return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(WarehouseCountVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var warehouse = await _context.PrdWarehouses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ID == model.WarehouseId && x.IsActive != false && x.IsDelete != true, ct);

        if (warehouse == null)
            return CountError(model, "Geçerli ve aktif bir depo seçiniz.");

        PrdInventoryDocument? sourceDocument = null;
        if (model.SourceDocumentId.HasValue)
        {
            sourceDocument = await _context.PrdInventoryDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ID == model.SourceDocumentId.Value &&
                    x.Type == PrdInventoryDocumentType.Adjustment &&
                    x.Status == PrdInventoryDocumentStatus.Posted &&
                    (x.SourceDocumentType == "WarehouseCount" || x.SourceDocumentType == "WarehouseCountCorrection") &&
                    x.IsDelete != true,
                    ct);

            var sourceWarehouseId = sourceDocument?.TargetWarehouseId ?? sourceDocument?.SourceWarehouseId;
            if (sourceDocument == null || sourceWarehouseId != warehouse.ID)
                return CountError(model, "Düzeltilecek sayım belgesi bu depoya ait değil veya artık kullanılamıyor.");
        }

        if (model.CountDate.Date > DateTime.Today)
            return CountError(model, "Sayım tarihi gelecekte olamaz.");

        var sessionRows = ReadCountList(model.WarehouseId, model.SourceDocumentId) ?? [];
        var postedQuantities = model.Rows
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.Last().CountedQuantity);
        foreach (var sessionRow in sessionRows)
        {
            if (postedQuantities.TryGetValue(sessionRow.MaterialId, out var postedQuantity))
                sessionRow.CountedQuantity = postedQuantity?.Trim() ?? string.Empty;
        }
        SaveCountList(model.WarehouseId, model.SourceDocumentId, sessionRows);
        var enteredRows = sessionRows
            .Where(x => !string.IsNullOrWhiteSpace(x.CountedQuantity))
            .Select(x => new WarehouseCountRowVM
            {
                MaterialId = x.MaterialId,
                MaterialCode = x.MaterialCode,
                SnapshotQuantity = x.SnapshotQuantity,
                CountedQuantity = x.CountedQuantity
            })
            .ToList();

        if (enteredRows.Count == 0)
            return CountError(model, "En az bir ürün için sayım stoku giriniz.");

        if (enteredRows.GroupBy(x => x.MaterialId).Any(x => x.Count() > 1))
            return CountError(model, "Aynı ürün sayım listesinde birden fazla kez gönderilemez.");

        var countedValues = new Dictionary<int, decimal>();
        var snapshotValues = new Dictionary<int, decimal>();
        foreach (var row in enteredRows)
        {
            if (!TryParseDecimal(row.CountedQuantity, out var counted) || counted < 0)
                return CountError(model, $"{row.MaterialCode} için sayım stoku geçersiz.");
            if (!TryParseDecimal(row.SnapshotQuantity, out var snapshot))
                return CountError(model, $"{row.MaterialCode} için ekran stok bilgisi doğrulanamadı.");
            countedValues[row.MaterialId] = counted;
            snapshotValues[row.MaterialId] = snapshot;
        }

        var materialIds = enteredRows.Select(x => x.MaterialId).Distinct().ToList();
        var materials = await _context.PrdMaterials
            .AsNoTracking()
            .Where(x => materialIds.Contains(x.ID) && x.IsActive != false && x.IsDelete != true)
            .ToDictionaryAsync(x => x.ID, ct);

        if (materials.Count != materialIds.Count)
            return CountError(model, "Sayımı girilen ürünlerden biri artık aktif değil veya bulunamadı.");

        try
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

            var stockMovements = await _context.PrdStockMovements
                .AsNoTracking()
                .Where(x => x.WarehouseId == warehouse.ID && materialIds.Contains(x.MaterialId) && x.IsDelete != true)
                .Select(x => new
                {
                    x.MaterialId,
                    x.StockLotId,
                    x.Direction,
                    x.Quantity,
                    x.TotalCost,
                    x.UnitCost,
                    x.MovementDate,
                    x.ID
                })
                .ToListAsync(ct);

            var currentByMaterial = stockMovements
                .GroupBy(x => x.MaterialId)
                .ToDictionary(
                    x => x.Key,
                    x => x.Sum(y => y.Direction == PrdStockDirection.In ? y.Quantity : -y.Quantity));

            foreach (var materialId in materialIds)
            {
                var current = currentByMaterial.GetValueOrDefault(materialId);
                if (current != snapshotValues[materialId])
                    throw new InvalidOperationException(
                        $"{materials[materialId].Code} stok miktarı ekran açıldıktan sonra değişti. Sayım listesini yenileyip tekrar deneyiniz.");
            }

            var differences = materialIds
                .Select(id => new
                {
                    Material = materials[id],
                    Current = currentByMaterial.GetValueOrDefault(id),
                    Counted = countedValues[id],
                    Difference = countedValues[id] - currentByMaterial.GetValueOrDefault(id)
                })
                .Where(x => x.Difference != 0)
                .ToList();

            if (differences.Count == 0)
            {
                await transaction.RollbackAsync(ct);
                HttpContext.Session.Remove(CountSessionKey(model.WarehouseId, model.SourceDocumentId));
                TempData["success"] = "Girilen sayım stokları mevcut stoklarla aynı; stok hareketi oluşturulmadı.";
                return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId });
            }

            var now = DateTime.Now;
            var user = User.Identity?.Name;
            var documentNumber = $"SAY-{now:yyyyMMddHHmmssfff}";
            var document = new PrdInventoryDocument
            {
                DocumentNumber = documentNumber,
                Type = PrdInventoryDocumentType.Adjustment,
                Status = PrdInventoryDocumentStatus.Posted,
                DocumentDate = model.CountDate.Date,
                PostingDate = now,
                PostedUserId = user,
                SourceWarehouseId = warehouse.ID,
                TargetWarehouseId = warehouse.ID,
                CurrencyCode = "TRY",
                ExchangeRate = 1,
                SourceDocumentType = sourceDocument == null ? "WarehouseCount" : "WarehouseCountCorrection",
                SourceDocumentId = sourceDocument?.ID,
                Notes = string.IsNullOrWhiteSpace(model.Notes)
                    ? sourceDocument == null
                        ? $"{warehouse.Code} depo sayımı"
                        : $"{sourceDocument.DocumentNumber} sayım düzeltmesi"
                    : model.Notes.Trim(),
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            };
            _context.PrdInventoryDocuments.Add(document);
            await _context.SaveChangesAsync(ct);

            var lotIds = stockMovements.Where(x => x.StockLotId.HasValue).Select(x => x.StockLotId!.Value).Distinct().ToList();
            var lots = await _context.PrdStockLots
                .Where(x => lotIds.Contains(x.ID) && x.WarehouseId == warehouse.ID && x.IsDelete != true)
                .ToDictionaryAsync(x => x.ID, ct);

            var lotBalances = stockMovements
                .Where(x => x.StockLotId.HasValue && lots.ContainsKey(x.StockLotId.Value))
                .GroupBy(x => new { x.MaterialId, StockLotId = x.StockLotId!.Value })
                .Select(x => new LotBalance(
                    x.Key.MaterialId,
                    x.Key.StockLotId,
                    x.Sum(y => y.Direction == PrdStockDirection.In ? y.Quantity : -y.Quantity),
                    x.Sum(y => y.Direction == PrdStockDirection.In ? y.TotalCost : -y.TotalCost)))
                .Where(x => x.Quantity > 0)
                .ToList();

            var lineMovements = new List<PendingMovement>();
            var sequence = 0;

            foreach (var difference in differences)
            {
                var materialMovements = stockMovements.Where(x => x.MaterialId == difference.Material.ID).ToList();
                var currentValue = materialMovements.Sum(x => x.Direction == PrdStockDirection.In ? x.TotalCost : -x.TotalCost);
                var fallbackUnitCost = difference.Current > 0 && currentValue > 0
                    ? currentValue / difference.Current
                    : materialMovements
                        .Where(x => x.UnitCost > 0)
                        .OrderByDescending(x => x.MovementDate)
                        .ThenByDescending(x => x.ID)
                        .Select(x => x.UnitCost)
                        .FirstOrDefault();
                fallbackUnitCost = Math.Max(0, fallbackUnitCost);

                if (difference.Difference < 0)
                {
                    var remaining = -difference.Difference;
                    var sourceLots = lotBalances
                        .Where(x => x.MaterialId == difference.Material.ID)
                        .OrderBy(x => lots[x.StockLotId].ExpirationDate ?? DateTime.MaxValue)
                        .ThenBy(x => lots[x.StockLotId].ID)
                        .ToList();

                    foreach (var balance in sourceLots)
                    {
                        if (remaining <= 0) break;
                        var quantity = Math.Min(remaining, balance.Quantity);
                        var unitCost = balance.Value > 0 ? balance.Value / balance.Quantity : fallbackUnitCost;
                        var note = $"Sayım eksiği | Sistem: {difference.Current:0.######} | Sayım: {difference.Counted:0.######}";
                        var line = CreateLine(document.ID, ++sequence, difference.Material, lots[balance.StockLotId], quantity, unitCost, note, now, user, source: true);
                        lineMovements.Add(new PendingMovement(line, warehouse.ID, balance.StockLotId, PrdStockDirection.Out, PrdStockMovementType.InventoryShortage));
                        remaining -= quantity;
                    }

                    if (remaining > 0)
                        throw new InvalidOperationException($"{difference.Material.Code} sayım eksiği lot stoklarına dağıtılamadı. Lot hareketlerini kontrol ediniz.");
                }
                else
                {
                    var targetBalance = lotBalances
                        .Where(x => x.MaterialId == difference.Material.ID)
                        .OrderByDescending(x => lots[x.StockLotId].ExpirationDate ?? DateTime.MinValue)
                        .ThenByDescending(x => lots[x.StockLotId].ID)
                        .FirstOrDefault();

                    PrdStockLot targetLot;
                    if (targetBalance != null)
                    {
                        targetLot = lots[targetBalance.StockLotId];
                    }
                    else
                    {
                        targetLot = new PrdStockLot
                        {
                            MaterialId = difference.Material.ID,
                            WarehouseId = warehouse.ID,
                            LotNumber = $"SAYIM-FARK-{now:yyyyMMddHHmmssfff}-{difference.Material.ID}",
                            IsActive = true,
                            IsDelete = false,
                            CreateDate = now,
                            CreateUserID = user
                        };
                        _context.PrdStockLots.Add(targetLot);
                        await _context.SaveChangesAsync(ct);
                    }

                    var surplusUnitCost = targetBalance != null && targetBalance.Value > 0
                        ? targetBalance.Value / targetBalance.Quantity
                        : fallbackUnitCost;
                    var note = $"Sayım fazlası | Sistem: {difference.Current:0.######} | Sayım: {difference.Counted:0.######}";
                    var line = CreateLine(document.ID, ++sequence, difference.Material, targetLot, difference.Difference, surplusUnitCost, note, now, user, source: false);
                    lineMovements.Add(new PendingMovement(line, warehouse.ID, targetLot.ID, PrdStockDirection.In, PrdStockMovementType.InventorySurplus));
                }
            }

            _context.PrdInventoryDocumentLines.AddRange(lineMovements.Select(x => x.Line));
            await _context.SaveChangesAsync(ct);

            foreach (var item in lineMovements)
            {
                _context.PrdStockMovements.Add(new PrdStockMovement
                {
                    InventoryDocumentId = document.ID,
                    InventoryDocumentLineId = item.Line.ID,
                    MaterialId = item.Line.MaterialId,
                    WarehouseId = item.WarehouseId,
                    StockLotId = item.StockLotId,
                    Direction = item.Direction,
                    MovementType = item.MovementType,
                    Quantity = item.Line.Quantity,
                    UnitId = item.Line.UnitId,
                    OriginalUnitCost = item.Line.UnitCost,
                    CurrencyCode = "TRY",
                    ExchangeRate = 1,
                    UnitCost = item.Line.UnitCost,
                    TotalCost = item.Line.TotalCost,
                    CostSource = PrdStockCostSource.Adjustment,
                    MovementDate = model.CountDate.Date,
                    DocumentNumber = document.DocumentNumber,
                    DocumentType = PrdStockDocumentType.InventoryDocument,
                    DocumentId = document.ID,
                    Description = item.Line.Notes,
                    IsActive = true,
                    IsDelete = false,
                    CreateDate = now,
                    CreateUserID = user
                });
            }

            document.TotalCost = lineMovements.Sum(x => x.Line.TotalCost);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            HttpContext.Session.Remove(CountSessionKey(model.WarehouseId, model.SourceDocumentId));

            TempData["success"] = sourceDocument == null
                ? $"{document.DocumentNumber} numaralı sayım belgesi işlendi. {differences.Count} ürünün toplam stoku sayım değerine getirildi."
                : $"{document.DocumentNumber} numaralı sayım düzeltmesi işlendi. Önceki {sourceDocument.DocumentNumber} belgesi geçmişte korunmuştur.";
            return RedirectToAction("Detay", "ProductionInventory", new { id = document.ID });
        }
        catch (InvalidOperationException ex)
        {
            return CountError(model, ex.Message);
        }
    }

    private async Task<List<WarehouseCountRowVM>> BuildRowsAsync(
        int warehouseId,
        IReadOnlyCollection<WarehouseCountSessionItem> sessionRows,
        CancellationToken ct)
    {
        if (sessionRows.Count == 0) return [];

        var materialIds = sessionRows.Select(x => x.MaterialId).Distinct().ToList();
        var materials = await (
            from material in _context.PrdMaterials.AsNoTracking()
            join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
            where materialIds.Contains(material.ID) && material.IsActive != false && material.IsDelete != true
            select new
            {
                material.ID,
                material.Code,
                material.Name,
                material.Type,
                material.CriticalQuantity,
                Unit = unit.Name
            })
            .ToDictionaryAsync(x => x.ID, ct);

        var balances = await CurrentBalancesAsync(warehouseId, materialIds, ct);
        var reservations = await _context.PrdStockReservations
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId &&
                        materialIds.Contains(x.MaterialId) &&
                        x.IsDelete != true &&
                        (x.Status == PrdReservationStatus.Active || x.Status == PrdReservationStatus.PartiallyUsed))
            .GroupBy(x => x.MaterialId)
            .Select(group => new
            {
                MaterialId = group.Key,
                Quantity = group.Sum(x => x.ReservedQuantity - x.UsedQuantity - x.ReleasedQuantity)
            })
            .ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, ct);

        var rows = new List<WarehouseCountRowVM>();
        foreach (var sessionRow in sessionRows)
        {
            if (!materials.TryGetValue(sessionRow.MaterialId, out var material)) continue;
            var current = balances.GetValueOrDefault(material.ID);
            var snapshot = TryParseDecimal(sessionRow.SnapshotQuantity, out var parsedSnapshot)
                ? parsedSnapshot
                : current;
            rows.Add(new WarehouseCountRowVM
            {
                MaterialId = material.ID,
                MaterialCode = material.Code,
                MaterialName = material.Name,
                MaterialType = material.Type.ToTurkish(),
                Unit = material.Unit,
                CurrentQuantity = current,
                ReservedQuantity = reservations.GetValueOrDefault(material.ID),
                CriticalQuantity = material.CriticalQuantity,
                SnapshotCurrentQuantity = snapshot,
                SnapshotQuantity = snapshot.ToString("0.######", CultureInfo.InvariantCulture),
                CountedQuantity = sessionRow.CountedQuantity
            });
        }
        return rows;
    }

    private async Task<Dictionary<int, decimal>> CurrentBalancesAsync(
        int warehouseId,
        IReadOnlyCollection<int> materialIds,
        CancellationToken ct)
    {
        if (materialIds.Count == 0) return [];
        return await _context.PrdStockMovements
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && materialIds.Contains(x.MaterialId) && x.IsDelete != true)
            .GroupBy(x => x.MaterialId)
            .Select(group => new
            {
                MaterialId = group.Key,
                Quantity = group.Sum(x => x.Direction == PrdStockDirection.In ? x.Quantity : -x.Quantity)
            })
            .ToDictionaryAsync(x => x.MaterialId, x => x.Quantity, ct);
    }

    private Task<bool> IsActiveWarehouseAsync(int warehouseId, CancellationToken ct) =>
        _context.PrdWarehouses.AsNoTracking().AnyAsync(
            x => x.ID == warehouseId && x.IsActive != false && x.IsDelete != true,
            ct);

    private List<WarehouseCountSessionItem>? ReadCountList(int warehouseId, int? sourceDocumentId)
    {
        var json = HttpContext.Session.GetString(CountSessionKey(warehouseId, sourceDocumentId));
        if (json == null) return null;
        try
        {
            return JsonSerializer.Deserialize<List<WarehouseCountSessionItem>>(json) ?? [];
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(CountSessionKey(warehouseId, sourceDocumentId));
            return null;
        }
    }

    private void SaveCountList(int warehouseId, int? sourceDocumentId, List<WarehouseCountSessionItem> rows) =>
        HttpContext.Session.SetString(
            CountSessionKey(warehouseId, sourceDocumentId),
            JsonSerializer.Serialize(rows));

    private static string CountSessionKey(int warehouseId, int? sourceDocumentId) =>
        $"ProductionStockCount:Draft:{warehouseId}:{sourceDocumentId?.ToString(CultureInfo.InvariantCulture) ?? "New"}";

    private IActionResult CountError(WarehouseCountVM model, string message)
    {
        TempData["error"] = message;
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId });
    }

    private static PrdInventoryDocumentLine CreateLine(
        int documentId,
        int sequence,
        PrdMaterial material,
        PrdStockLot lot,
        decimal quantity,
        decimal unitCost,
        string notes,
        DateTime now,
        string? user,
        bool source)
    {
        return new PrdInventoryDocumentLine
        {
            InventoryDocumentId = documentId,
            Sequence = sequence,
            MaterialId = material.ID,
            UnitId = material.UnitId,
            SourceStockLotId = source ? lot.ID : null,
            TargetStockLotId = source ? null : lot.ID,
            LotNumber = lot.LotNumber,
            ProductionDate = lot.ProductionDate,
            ExpirationDate = lot.ExpirationDate,
            Quantity = quantity,
            OriginalUnitCost = unitCost,
            CurrencyCode = "TRY",
            ExchangeRate = 1,
            UnitCost = unitCost,
            TotalCost = quantity * unitCost,
            CostSource = PrdStockCostSource.Adjustment,
            Notes = notes,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        };
    }

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim().Replace(" ", string.Empty);
        if (normalized.Contains(',') && normalized.Contains('.'))
            normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.')
                ? normalized.Replace(".", string.Empty).Replace(',', '.')
                : normalized.Replace(",", string.Empty);
        else if (normalized.Contains(','))
            normalized = normalized.Replace(',', '.');
        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }

    private sealed record LotBalance(int MaterialId, int StockLotId, decimal Quantity, decimal Value);
    private sealed record PendingMovement(
        PrdInventoryDocumentLine Line,
        int WarehouseId,
        int StockLotId,
        PrdStockDirection Direction,
        PrdStockMovementType MovementType);

    public sealed class WarehouseCountSessionItem
    {
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string SnapshotQuantity { get; set; } = string.Empty;
        public string CountedQuantity { get; set; } = string.Empty;
    }
}
