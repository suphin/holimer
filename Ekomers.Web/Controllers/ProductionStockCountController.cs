using Ekomers.Data;
using Ekomers.Data.Services;
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
public sealed partial class ProductionStockCountController : Controller
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
        int? draftId = null,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "YeniUretim";
        PrdWarehouseCountDraft? selectedDraft = null;
        if (draftId.HasValue)
        {
            selectedDraft = await _context.PrdWarehouseCountDrafts.AsNoTracking().FirstOrDefaultAsync(x => x.ID == draftId && x.OwnerUserId == CountOwner && x.IsDelete != true, ct);
            if (selectedDraft == null) return NotFound();
            warehouseId = selectedDraft.WarehouseId;
            sourceDocumentId = selectedDraft.SourceDocumentId;
        }

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
            var archived = selectedDraft != null && selectedDraft.Status != PrdWarehouseCountDraftStatus.Draft;
            sourceDocument = await _context.PrdInventoryDocuments
                .AsNoTracking()
                .FirstOrDefaultAsync(x =>
                    x.ID == sourceDocumentId.Value &&
                    (archived || (x.Type == PrdInventoryDocumentType.Adjustment &&
                    x.Status == PrdInventoryDocumentStatus.Posted &&
                    (x.SourceDocumentType == "WarehouseCount" || x.SourceDocumentType == "WarehouseCountCorrection") &&
                    x.IsDelete != true)),
                    ct);

            if (sourceDocument == null || (!sourceDocument.SourceWarehouseId.HasValue && !sourceDocument.TargetWarehouseId.HasValue))
            {
                TempData["error"] = "Düzeltilecek depo sayımı bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            warehouseId = selectedDraft?.WarehouseId ?? sourceDocument.TargetWarehouseId ?? sourceDocument.SourceWarehouseId;
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

        PrdWarehouseCountDraft draft;
        try { draft = selectedDraft ?? await GetOrCreateCountDraftAsync(warehouseId.Value, sourceDocument?.ID, correctionMaterialIds, ct); }
        catch (InvalidOperationException ex) { _context.ChangeTracker.Clear(); model.Error = ex.Message; return View(model); }
        model.DraftId = draft.ID; model.Revision = draft.Revision; model.Status = draft.Status;
        model.CountDate = draft.CountDate; model.Notes = draft.Notes; model.InventoryDocumentId = draft.InventoryDocumentId;
        var entries = await _context.PrdWarehouseCountEntries.AsNoTracking().Where(x => x.DraftId == draft.ID && x.IsDelete != true).OrderBy(x => x.ID).ToListAsync(ct);
        model.Rows = await BuildRowsAsync(warehouseId.Value, entries, ct);
        model.TotalCount = model.Rows.Count;
        model.History = await _context.PrdWarehouseCountDrafts.AsNoTracking().Where(x => x.OwnerUserId == CountOwner && x.WarehouseId == warehouseId && x.IsDelete != true)
            .OrderByDescending(x => x.ID).Take(10).Select(x => new WarehouseCountHistoryVM { Id = x.ID, CountDate = x.CountDate, Status = x.Status, InventoryDocumentId = x.InventoryDocumentId, Notes = x.Notes }).ToListAsync(ct);

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
        int draftId,
        int revision,
        Guid requestId,
        int warehouseId,
        int materialId,
        string? countedQuantity,
        string? location,
        string? entryNotes,
        int? sourceDocumentId,
        CancellationToken ct)
    {
        if (!await IsActiveWarehouseAsync(warehouseId, ct))
        {
            TempData["error"] = "Geçerli ve aktif bir depo seçiniz.";
            return RedirectToAction(nameof(Index));
        }
        if (!TryCount(countedQuantity, out var counted) || requestId == Guid.Empty || location?.Trim().Length > 100 || entryNotes?.Trim().Length > 250)
        {
            TempData["error"] = "Geçerli bir sayım miktarı giriniz (en fazla 6 ondalık). Raf/alan en fazla 100, not 250 karakter olabilir. Sayfayı yenileyerek tekrar deneyiniz.";
            return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId });
        }

        var material = await _context.PrdMaterials
            .AsNoTracking()
            .Where(x => x.ID == materialId && x.IsActive != false && x.IsDelete != true)
            .Select(x => new { x.ID, x.Code, x.Name, x.UnitId })
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

        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(draftId, warehouseId, sourceDocumentId, ct);
            if (await _context.PrdWarehouseCountEntries.AnyAsync(x => x.DraftId == draftId && x.RequestId == requestId, ct))
            {
                TempData["success"] = "Bu sayım girişi daha önce kaydedildi; ikinci kez eklenmedi.";
                return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId, draftId });
            }
            // Appending cannot overwrite existing entries; stale tabs may safely add a new request.
            // Updates, removals and posting still require the current revision.
            if (draft.Status != PrdWarehouseCountDraftStatus.Draft) throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş sayıma giriş eklenemez.");
            var entries = await _context.PrdWarehouseCountEntries.Where(x => x.DraftId == draft.ID && x.IsDelete != true).ToListAsync(ct);
            if (entries.Count >= 2000) throw new InvalidOperationException("Bir sayımda en fazla 2.000 giriş tutulabilir.");
            var unit = await _context.PrdUnits.AsNoTracking().FirstOrDefaultAsync(x => x.ID == material.UnitId && x.IsDelete != true && x.IsActive != false, ct)
                ?? throw new InvalidOperationException("Malzemenin stok birimi aktif değil.");
            var previous = entries.FirstOrDefault(x => x.MaterialId == material.ID);
            var balances = previous == null ? await CurrentBalancesAsync(warehouseId, [material.ID], ct) : null;
            var entry = new PrdWarehouseCountEntry
            {
                DraftId = draft.ID, RequestId = requestId, MaterialId = material.ID, UnitId = previous?.UnitId ?? material.UnitId,
                MaterialCode = material.Code, MaterialName = material.Name, UnitName = previous?.UnitName ?? Ekomers.Data.Services.ProductionUnitNormalizer.DisplayName(unit.Code, unit.Name),
                SnapshotQuantity = previous?.SnapshotQuantity ?? balances!.GetValueOrDefault(material.ID), CountedQuantity = counted,
                Location = location?.Trim(), Notes = entryNotes?.Trim(), IsActive = true, IsDelete = false, CreateDate = DateTime.Now, CreateUserID = CountActor
            };
            await ValidateCountUnitsAsync([entry], warehouseId, ct);
            CheckCountTotals(entries.Append(entry));
            _context.PrdWarehouseCountEntries.Add(entry); TouchCountDraft(draft);
            CountAudit(nameof(AddItem), draft, new { material.ID, Counted = counted, entry.Location, requestId });
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = $"{material.Code}: {counted:0.######} {entry.UnitName} ayrı bir sayım girişi olarak eklendi; ürün toplamı güncellendi.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { warehouseId, sourceDocumentId, draftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(WarehouseCountVM model, int entryId, string? countedQuantity, string? location, string? entryNotes, CancellationToken ct)
    {
        try
        {
            if (!TryCount(countedQuantity, out var counted) || location?.Trim().Length > 100 || entryNotes?.Trim().Length > 250) throw new InvalidOperationException("Miktar geçersiz veya raf/not bilgisi çok uzun.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct); EnsureCountRevision(draft, model.Revision);
            var entries = await _context.PrdWarehouseCountEntries.Where(x => x.DraftId == draft.ID && x.IsDelete != true).ToListAsync(ct);
            var entry = entries.FirstOrDefault(x => x.ID == entryId && x.CountedQuantity.HasValue) ?? throw new InvalidOperationException("Güncellenecek sayım girişi bulunamadı.");
            var before = new { entry.CountedQuantity, entry.Location, entry.Notes };
            entry.CountedQuantity = counted; entry.Location = location?.Trim(); entry.Notes = entryNotes?.Trim(); entry.UpdateDate = DateTime.Now; entry.UpdateUserID = CountActor;
            CheckCountTotals(entries); TouchCountDraft(draft); CountAudit(nameof(UpdateItem), draft, new { entryId, Before = before, Counted = counted, entry.Location, entry.Notes });
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = "Seçilen giriş güncellendi; ürün toplamı yeniden hesaplandı.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId, draftId = model.DraftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(WarehouseCountVM model, int entryId, CancellationToken ct)
    {
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct); EnsureCountRevision(draft, model.Revision);
            var entry = await _context.PrdWarehouseCountEntries.FirstOrDefaultAsync(x => x.ID == entryId && x.DraftId == draft.ID && x.IsDelete != true, ct)
                ?? throw new InvalidOperationException("Kaldırılacak giriş bulunamadı.");
            DeleteCountEntry(entry, CountActor); TouchCountDraft(draft); CountAudit(nameof(RemoveItem), draft, new { entryId, entry.CountedQuantity, entry.Location });
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = "Yalnızca seçilen giriş kaldırıldı; diğer girişler korunarak toplam güncellendi.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId, draftId = model.DraftId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveProduct(WarehouseCountVM model, int materialId, CancellationToken ct)
    {
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct); EnsureCountRevision(draft, model.Revision);
            var entries = await _context.PrdWarehouseCountEntries.Where(x => x.DraftId == draft.ID && x.MaterialId == materialId && x.IsDelete != true).ToListAsync(ct);
            if (entries.Count == 0) throw new InvalidOperationException("Kaldırılacak ürün bulunamadı.");
            foreach (var entry in entries) DeleteCountEntry(entry, CountActor);
            TouchCountDraft(draft); CountAudit(nameof(RemoveProduct), draft, new { materialId, EntryIds = entries.Select(x => x.ID) });
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct); TempData["success"] = "Bu ürünün tüm girişleri taslaktan kaldırıldı; diğer ürünler korunmuştur.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId, draftId = model.DraftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clear(WarehouseCountVM model, CancellationToken ct)
    {
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct); EnsureCountRevision(draft, model.Revision);
            draft.Status = PrdWarehouseCountDraftStatus.Cancelled; draft.IsActive = false; draft.CompletedAt = DateTime.Now; TouchCountDraft(draft);
            CountAudit(nameof(Clear), draft); await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = "Taslak iptal edilerek geçmişte saklandı. Yeni sayım taslağı açıldı; stok değişmedi.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); return CountError(model, e.Message); }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDraft(WarehouseCountVM model, CancellationToken ct)
    {
        try
        {
            if (model.CountDate.Year < 1900 || model.CountDate.Date > DateTime.Today || model.Notes?.Trim().Length > 500) throw new InvalidOperationException("Sayım tarihi veya notu geçersiz.");
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct); EnsureCountRevision(draft, model.Revision);
            draft.CountDate = model.CountDate.Date; draft.Notes = model.Notes?.Trim(); TouchCountDraft(draft); CountAudit(nameof(UpdateDraft), draft);
            await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct); TempData["success"] = "Sayım tarihi ve notu taslağa kaydedildi; stok değişmedi.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId, draftId = model.DraftId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshSnapshots(WarehouseCountVM model, CancellationToken ct)
    {
        try
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
            var draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct); EnsureCountRevision(draft, model.Revision);
            var entries = await _context.PrdWarehouseCountEntries.Where(x => x.DraftId == draft.ID && x.IsDelete != true).ToListAsync(ct);
            await ValidateCountUnitsAsync(entries, model.WarehouseId, ct);
            var balances = await CurrentBalancesAsync(model.WarehouseId, entries.Select(x => x.MaterialId).Distinct().ToList(), ct);
            var before = entries.GroupBy(x => x.MaterialId).Select(g => new { MaterialId = g.Key, Old = g.First().SnapshotQuantity, New = balances.GetValueOrDefault(g.Key) }).ToList();
            foreach (var entry in entries) { entry.SnapshotQuantity = balances.GetValueOrDefault(entry.MaterialId); entry.UpdateDate = DateTime.Now; entry.UpdateUserID = CountActor; }
            TouchCountDraft(draft); CountAudit(nameof(RefreshSnapshots), draft, before); await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = "Sistem stok referansları yenilendi; sayım miktarlarınız değişmedi. Farkları kontrol edip stoka işleyiniz.";
        }
        catch (InvalidOperationException e) { _context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId, draftId = model.DraftId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(WarehouseCountVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        PrdWarehouseCountDraft draft;
        try
        {
            draft = await LoadCountDraftAsync(model.DraftId, model.WarehouseId, model.SourceDocumentId, ct);
            if (draft.Status == PrdWarehouseCountDraftStatus.Posted)
                return draft.InventoryDocumentId.HasValue
                    ? RedirectToAction("Detay", "ProductionInventory", new { id = draft.InventoryDocumentId.Value })
                    : RedirectToAction(nameof(Index), new { draftId = draft.ID });
            EnsureCountRevision(draft, model.Revision);
        }
        catch (InvalidOperationException e) { return CountError(model, e.Message); }
        if (_context.Database.IsSqlServer())
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdWarehouse] WITH(UPDLOCK,HOLDLOCK) WHERE [ID]={model.WarehouseId}", ct);
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

        if (model.CountDate.Year < 1900 || model.CountDate.Date > DateTime.Today || model.Notes?.Trim().Length > 500)
            return CountError(model, "Sayım tarihi veya notu geçersiz.");
        var entries = await _context.PrdWarehouseCountEntries.AsNoTracking().Where(x => x.DraftId == draft.ID && x.IsDelete != true && x.CountedQuantity.HasValue).ToListAsync(ct);
        if (entries.GroupBy(x => x.MaterialId).Any(g => g.Select(x => x.SnapshotQuantity).Distinct().Count() != 1 || g.Sum(x => x.CountedQuantity!.Value) > MaximumCount))
            return CountError(model, "Sayım girişlerinde stok referansı veya toplam tutarsızlığı var; taslağı kontrol ediniz.");
        var enteredRows = entries.GroupBy(x => x.MaterialId).Select(group => new WarehouseCountRowVM
            {
                MaterialId = group.Key, MaterialCode = group.First().MaterialCode,
                SnapshotQuantity = group.First().SnapshotQuantity.ToString(CultureInfo.InvariantCulture),
                CountedQuantity = group.Sum(x => x.CountedQuantity!.Value).ToString(CultureInfo.InvariantCulture)
            }).ToList();

        if (enteredRows.Count == 0)
            return CountError(model, "En az bir ürün için sayım stoku giriniz.");

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
            await ValidateCountUnitsAsync(entries, warehouse.ID, ct);

            var rawStockMovements = await _context.PrdStockMovements
                .AsNoTracking()
                .Where(x => x.WarehouseId == warehouse.ID && materialIds.Contains(x.MaterialId) && x.IsDelete != true)
                .Select(x => new
                {
                    x.MaterialId,
                    x.UnitId,
                    x.StockLotId,
                    x.Direction,
                    x.Quantity,
                    x.TotalCost,
                    x.UnitCost,
                    x.MovementDate,
                    x.ID
                })
                .ToListAsync(ct);

            var converter = await ProductionStockUnitConverter.LoadAsync(_context, materialIds, ct);
            var stockMovements = rawStockMovements.Select(x => new
            {
                x.MaterialId, x.StockLotId, x.Direction, x.TotalCost, x.MovementDate, x.ID,
                Quantity = converter.Quantity(x.MaterialId, x.UnitId, x.Quantity),
                UnitCost = converter.UnitCost(x.MaterialId, x.UnitId, x.UnitCost)
            }).ToList();

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
                        $"{materials[materialId].Code} stok miktarı sayım başladıktan sonra değişti. Sistem Stok Referanslarını Yenile işlemini uygulayıp farkları kontrol ediniz; sayım girişleri korunmuştur.");
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
                draft.CountDate = model.CountDate.Date; draft.Notes = model.Notes?.Trim(); draft.Status = PrdWarehouseCountDraftStatus.Posted;
                draft.CompletedAt = DateTime.Now; draft.IsActive = false; TouchCountDraft(draft); CountAudit(nameof(Save), draft, new { NoDifference = true, Products = materialIds.Count });
                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);
                TempData["success"] = "Girilen sayım stokları mevcut stoklarla aynı; stok hareketi oluşturulmadı.";
                return RedirectToAction(nameof(Index), new { draftId = draft.ID });
            }

            var now = DateTime.Now;
            var user = User.Identity?.Name;
            var documentNumber = $"SAY-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}";
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
            draft.CountDate = model.CountDate.Date; draft.Notes = model.Notes?.Trim(); draft.Status = PrdWarehouseCountDraftStatus.Posted;
            draft.InventoryDocumentId = document.ID; draft.CompletedAt = now; draft.IsActive = false; TouchCountDraft(draft);
            CountAudit(nameof(Save), draft, new { document.ID, Products = materialIds.Count, EntryCount = entries.Count });
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            TempData["success"] = sourceDocument == null
                ? $"{document.DocumentNumber} numaralı sayım belgesi işlendi. {differences.Count} ürünün toplam stoku sayım değerine getirildi."
                : $"{document.DocumentNumber} numaralı sayım düzeltmesi işlendi. Önceki {sourceDocument.DocumentNumber} belgesi geçmişte korunmuştur.";
            return RedirectToAction("Detay", "ProductionInventory", new { id = document.ID });
        }
        catch (InvalidOperationException ex)
        {
            await transaction.RollbackAsync(ct); _context.ChangeTracker.Clear();
            return CountError(model, ex.Message);
        }
        catch (OverflowException)
        {
            await transaction.RollbackAsync(ct); _context.ChangeTracker.Clear();
            return CountError(model, "Dönüştürülmüş miktar veya maliyet desteklenen aralığı aşıyor; sayım girişleri korunmuştur.");
        }
    }

    private async Task<List<WarehouseCountRowVM>> BuildRowsAsync(
        int warehouseId,
        IReadOnlyCollection<PrdWarehouseCountEntry> sessionRows,
        CancellationToken ct)
    {
        if (sessionRows.Count == 0) return [];

        var materialIds = sessionRows.Select(x => x.MaterialId).Distinct().ToList();
        var materials = await _context.PrdMaterials.AsNoTracking()
            .Where(material => materialIds.Contains(material.ID)).ToDictionaryAsync(x => x.ID, ct);

        var resolved = await ResolveCountBalancesAsync(warehouseId, materialIds, ct);
        var balances = resolved.Values;
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
        foreach (var group in sessionRows.GroupBy(x => x.MaterialId).OrderBy(g => g.First().MaterialCode))
        {
            var sessionRow = group.First();
            // Broken master references must not hide saved physical count entries.
            var material = materials.GetValueOrDefault(sessionRow.MaterialId) ?? new PrdMaterial
                { ID = sessionRow.MaterialId, Code = sessionRow.MaterialCode, Name = sessionRow.MaterialName, UnitId = sessionRow.UnitId };
            var current = balances.GetValueOrDefault(material.ID);
            var snapshot = sessionRow.SnapshotQuantity;
            var unitError = resolved.Errors.GetValueOrDefault(material.ID);
            try
            {
                resolved.Converter.BaseUnitName(material.ID);
                if (group.Any(x => !resolved.Converter.IsBaseUnitEquivalent(material.ID, x.UnitId)))
                    unitError = $"{material.Code}: ana birim sayım başladıktan sonra değişmiş; bu ürünü kaldırıp yeni ana biriminde yeniden sayınız.";
            }
            catch (InvalidOperationException ex) { unitError = ex.Message; }
            rows.Add(new WarehouseCountRowVM
            {
                Error = unitError,
                MaterialId = material.ID,
                MaterialCode = material.Code,
                MaterialName = material.Name,
                MaterialType = material.Type.ToTurkish(),
                Unit = sessionRow.UnitName,
                CurrentQuantity = current,
                ReservedQuantity = reservations.GetValueOrDefault(material.ID),
                CriticalQuantity = material.CriticalQuantity,
                SnapshotCurrentQuantity = snapshot,
                SnapshotQuantity = snapshot.ToString("0.######", CultureInfo.InvariantCulture),
                CountedQuantity = group.Any(x => x.CountedQuantity.HasValue) ? group.Sum(x => x.CountedQuantity ?? 0).ToString("0.######", CultureInfo.GetCultureInfo("tr-TR")) : null,
                Entries = group.Where(x => x.CountedQuantity.HasValue).OrderBy(x => x.ID).Select(x => new WarehouseCountEntryVM
                { Id = x.ID, CountedQuantity = x.CountedQuantity!.Value.ToString("0.######", CultureInfo.GetCultureInfo("tr-TR")), Location = x.Location, Notes = x.Notes, User = x.CreateUserID, CreatedAt = x.CreateDate }).ToList()
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
        var resolved = await ResolveCountBalancesAsync(warehouseId, materialIds, ct);
        if (resolved.Errors.Count > 0) throw new InvalidOperationException(resolved.Errors.First().Value);
        return resolved.Values;
    }

    private async Task<(Dictionary<int,decimal> Values, Dictionary<int,string> Errors, ProductionStockUnitConverter Converter)> ResolveCountBalancesAsync(
        int warehouseId, IReadOnlyCollection<int> materialIds, CancellationToken ct)
    {
        var movements = await _context.PrdStockMovements
            .AsNoTracking()
            .Where(x => x.WarehouseId == warehouseId && materialIds.Contains(x.MaterialId) && x.IsDelete != true)
            .Select(x => new { x.MaterialId, x.UnitId, x.Direction, x.Quantity }).ToListAsync(ct);
        var converter = await ProductionStockUnitConverter.LoadAsync(_context, materialIds, ct);
        var values = new Dictionary<int,decimal>();var errors = new Dictionary<int,string>();
        foreach (var group in movements.GroupBy(x => x.MaterialId))
        {
            try
            {
                values[group.Key] = group.Sum(x => (x.Direction == PrdStockDirection.In ? 1 : -1) * converter.Quantity(x.MaterialId, x.UnitId, x.Quantity));
                if (Math.Abs(values[group.Key]) > MaximumCount) throw new InvalidOperationException("Dönüştürülmüş stok bakiyesi sayımın desteklediği miktar aralığını aşıyor.");
            }
            catch (InvalidOperationException ex) { errors[group.Key] = ex.Message; }
            catch (OverflowException) { errors[group.Key] = "Dönüştürülmüş stok toplamı desteklenen aralığı aşıyor."; }
        }
        return (values,errors,converter);
    }

    private Task<bool> IsActiveWarehouseAsync(int warehouseId, CancellationToken ct) =>
        _context.PrdWarehouses.AsNoTracking().AnyAsync(
            x => x.ID == warehouseId && x.IsActive != false && x.IsDelete != true,
            ct);

    private List<WarehouseCountSessionItem>? ReadCountList(int warehouseId, int? sourceDocumentId)
    {
        var session = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.ISessionFeature>()?.Session;
        var json = session?.GetString(CountSessionKey(warehouseId, sourceDocumentId));
        if (json == null) return null;
        try
        {
            return JsonSerializer.Deserialize<List<WarehouseCountSessionItem>>(json) ?? [];
        }
        catch (JsonException)
        {
            session?.Remove(CountSessionKey(warehouseId, sourceDocumentId));
            return null;
        }
    }

    private static string CountSessionKey(int warehouseId, int? sourceDocumentId) =>
        $"ProductionStockCount:Draft:{warehouseId}:{sourceDocumentId?.ToString(CultureInfo.InvariantCulture) ?? "New"}";

    private IActionResult CountError(WarehouseCountVM model, string message)
    {
        TempData["error"] = message;
        return RedirectToAction(nameof(Index), new { warehouseId = model.WarehouseId, sourceDocumentId = model.SourceDocumentId, draftId = model.DraftId > 0 ? (int?)model.DraftId : null });
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
