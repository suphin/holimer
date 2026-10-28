using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Ekomers.Data.Services;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

public sealed partial class ProductionStockCountController
{
    private const decimal MaximumCount = 999999999999.999999m;
    private string CountOwner => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? throw new InvalidOperationException("Kullanıcı kimliği bulunamadı.");
    private string CountActor => User.Identity?.Name ?? CountOwner;

    private async Task<PrdWarehouseCountDraft> GetOrCreateCountDraftAsync(int warehouseId, int? sourceId, IReadOnlyCollection<int>? correctionIds, CancellationToken ct)
    {
        var existing = await _context.PrdWarehouseCountDrafts.AsNoTracking().FirstOrDefaultAsync(x => x.OwnerUserId == CountOwner && x.WarehouseId == warehouseId &&
            x.SourceDocumentId == sourceId && x.Status == PrdWarehouseCountDraftStatus.Draft && x.IsDelete != true, ct);
        if (existing != null) return existing;
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        if (_context.Database.IsSqlServer())
            await _context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdWarehouse] WITH(UPDLOCK,HOLDLOCK) WHERE [ID]={warehouseId}", ct);
        existing = await _context.PrdWarehouseCountDrafts.FirstOrDefaultAsync(x => x.OwnerUserId == CountOwner && x.WarehouseId == warehouseId &&
            x.SourceDocumentId == sourceId && x.Status == PrdWarehouseCountDraftStatus.Draft && x.IsDelete != true, ct);
        if (existing != null) { await tx.CommitAsync(ct); return existing; }
        var now = DateTime.Now;
        var draft = new PrdWarehouseCountDraft { OwnerUserId = CountOwner, WarehouseId = warehouseId, SourceDocumentId = sourceId, CountDate = DateTime.Today,
            Status = PrdWarehouseCountDraftStatus.Draft, Revision = 1, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CountActor };
        _context.PrdWarehouseCountDrafts.Add(draft);
        await _context.SaveChangesAsync(ct);
        var legacy = ReadCountList(warehouseId, sourceId);
        var ids = legacy?.Select(x => x.MaterialId).Distinct().ToList() ?? correctionIds?.ToList() ?? [];
        var materials = await (from material in _context.PrdMaterials.AsNoTracking()
                               join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                               where ids.Contains(material.ID) && material.IsDelete != true && material.IsActive != false
                               select new { Material = material, Unit = unit }).ToListAsync(ct);
        var balances = await CurrentBalancesAsync(warehouseId, ids, ct);
        foreach (var item in materials)
        {
            var oldRows = legacy?.Where(x => x.MaterialId == item.Material.ID).ToList();
            if (oldRows is not { Count: > 0 }) oldRows = [new WarehouseCountSessionItem { MaterialId = item.Material.ID }];
            var snapshot = oldRows.Select(x => x.SnapshotQuantity).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            var baseline = TryParseDecimal(snapshot, out var oldBaseline) ? oldBaseline : balances.GetValueOrDefault(item.Material.ID);
            foreach (var row in oldRows)
                _context.PrdWarehouseCountEntries.Add(new PrdWarehouseCountEntry
                {
                    DraftId = draft.ID, RequestId = Guid.NewGuid(), MaterialId = item.Material.ID, UnitId = item.Unit.ID,
                    MaterialCode = item.Material.Code, MaterialName = item.Material.Name, UnitName = ProductionUnitNormalizer.DisplayName(item.Unit.Code, item.Unit.Name),
                    SnapshotQuantity = baseline, CountedQuantity = TryCount(row.CountedQuantity, out var counted) ? counted : null,
                    Notes = legacy == null ? null : "Eski oturum listesinden aktarıldı (eldeki son değer).",
                    IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CountActor
                });
        }
        CountAudit("TaslakOlustur", draft, new { Imported = legacy != null });
        await _context.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        HttpContext.Features.Get<ISessionFeature>()?.Session?.Remove(CountSessionKey(warehouseId, sourceId));
        return draft;
    }

    private async Task<PrdWarehouseCountDraft> LoadCountDraftAsync(int id, int warehouseId, int? sourceId, CancellationToken ct)
    {
        var query = _context.Database.IsSqlServer()
            ? _context.PrdWarehouseCountDrafts.FromSqlInterpolated($"SELECT * FROM [dbo].[PrdWarehouseCountDraft] WITH(UPDLOCK,HOLDLOCK) WHERE [ID]={id}")
            : _context.PrdWarehouseCountDrafts.AsQueryable();
        return await query.FirstOrDefaultAsync(x => x.ID == id && x.OwnerUserId == CountOwner && x.WarehouseId == warehouseId &&
            x.SourceDocumentId == sourceId && x.IsDelete != true, ct) ?? throw new InvalidOperationException("Sayım taslağı bulunamadı veya bu kullanıcı/depo için kullanılamıyor. Sayfayı yeniden açınız.");
    }

    private static void EnsureCountRevision(PrdWarehouseCountDraft draft, int revision)
    {
        if (draft.Status != PrdWarehouseCountDraftStatus.Draft) throw new InvalidOperationException("Tamamlanmış veya iptal edilmiş sayım değiştirilemez.");
        if (draft.Revision != revision) throw new InvalidOperationException("Sayım başka bir sekmede veya işlemde değişti. Sayfayı yenileyiniz; mevcut kayıtlarınız korunmuştur.");
    }

    private void TouchCountDraft(PrdWarehouseCountDraft draft)
    { draft.Revision++; draft.UpdateDate = DateTime.Now; draft.UpdateUserID = CountActor; }

    private void CountAudit(string action, PrdWarehouseCountDraft draft, object? data = null)
    {
        var json = JsonSerializer.Serialize(new { DraftId = draft.ID, draft.WarehouseId, draft.Revision, Data = data });
        _context.UserActivityLog.Add(new UserActivityLog { DateTime = DateTime.Now, UserName = CountActor.Length > 100 ? CountActor[..100] : CountActor,
            ControllerName = nameof(ProductionStockCountController), ActionName = action, Info = "PersistentWarehouseCount", Parameters = json.Length > 4096 ? json[..4096] : json });
    }

    private static bool TryCount(string? text, out decimal quantity) => TryParseDecimal(text, out quantity) && quantity >= 0 && quantity <= MaximumCount && decimal.Round(quantity, 6) == quantity;

    private static void CheckCountTotals(IEnumerable<PrdWarehouseCountEntry> entries)
    {
        if (entries.Where(x => x.IsDelete != true).GroupBy(x => x.MaterialId).Any(g => g.Sum(x => x.CountedQuantity ?? 0) > MaximumCount))
            throw new InvalidOperationException("Ürün toplamı desteklenen sayım miktarını aşıyor.");
    }

    private static void DeleteCountEntry(PrdWarehouseCountEntry entry, string actor)
    { entry.IsDelete = true; entry.IsActive = false; entry.DeleteDate = DateTime.Now; entry.DeleteUserID = actor; entry.UpdateDate = DateTime.Now; entry.UpdateUserID = actor; }

    private async Task ValidateCountUnitsAsync(IReadOnlyCollection<PrdWarehouseCountEntry> entries, int warehouseId, CancellationToken ct)
    {
        var materialIds = entries.Select(x => x.MaterialId).Distinct().ToList();
        var materialUnits = await _context.PrdMaterials.AsNoTracking().Where(x => materialIds.Contains(x.ID) && x.IsActive != false && x.IsDelete != true)
            .Select(x => new { x.ID, x.UnitId }).ToDictionaryAsync(x => x.ID, x => x.UnitId, ct);
        var movements = await _context.PrdStockMovements.AsNoTracking().Where(x => x.WarehouseId == warehouseId && materialIds.Contains(x.MaterialId) && x.IsDelete != true)
            .Select(x => new { x.MaterialId, x.UnitId }).Distinct().ToListAsync(ct);
        var converter = await ProductionStockUnitConverter.LoadAsync(_context, materialIds, ct);
        foreach (var group in entries.GroupBy(x => x.MaterialId))
        {
            if (!materialUnits.ContainsKey(group.Key))
                throw new InvalidOperationException($"{group.First().MaterialCode}: aktif malzeme veya stok birimi bulunamadı.");
            converter.BaseUnitName(group.Key);
            // Do not silently reinterpret numbers already counted in a different base unit.
            if (group.Any(x => !converter.IsBaseUnitEquivalent(group.Key, x.UnitId)))
                throw new InvalidOperationException($"{group.First().MaterialCode}: malzemenin ana birimi sayım başladıktan sonra değişmiş. Bu ürünü taslaktan kaldırıp yeni ana biriminde yeniden sayınız.");
            foreach (var movement in movements.Where(x => x.MaterialId == group.Key)) converter.Factor(group.Key, movement.UnitId);
        }
    }
}
