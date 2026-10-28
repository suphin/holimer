using System.Data;
using System.Globalization;
using System.Text.Json;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class HammaddeSarfController(ApplicationDbContext context, RawMaterialConsumptionService consumption, PurchasingInventoryReversalService reversal) : Controller
{
    private string Actor => User.Identity?.Name ?? "Kullanıcı";
    private IQueryable<PrdInventoryDocument> Documents => context.PrdInventoryDocuments.Where(x => x.IsDelete != true &&
        x.Type == PrdInventoryDocumentType.RawMaterialConsumption && x.SourceDocumentType == RawMaterialConsumptionService.SourceType);

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await Documents.AsNoTracking().OrderByDescending(x => x.DocumentDate).ThenByDescending(x => x.ID)
            .Select(x => new RawMaterialConsumptionListVM
            {
                Id = x.ID, Number = x.DocumentNumber, Date = x.DocumentDate, Status = x.Status, CreatedBy = x.CreateUserID, Notes = x.Notes,
                TotalCost = x.TotalCost, LineCount = context.PrdInventoryDocumentLines.Count(l => l.InventoryDocumentId == x.ID && l.IsDelete != true),
                Warehouse = context.PrdWarehouses.Where(w => w.ID == x.SourceWarehouseId).Select(w => w.Code + " - " + w.Name).FirstOrDefault() ?? "-"
            }).Take(1000).ToListAsync(ct);
        return View(model);
    }

    [HttpGet, Authorize(Policy = "HammaddeSarfYonet")]
    public async Task<IActionResult> Yeni(int? warehouseId, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = new RawMaterialConsumptionCreateVM { WarehouseId = warehouseId };
        await FillFormAsync(model, ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "HammaddeSarfYonet")]
    public async Task<IActionResult> Yeni(RawMaterialConsumptionCreateVM model, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        model.Notes = model.Notes?.Trim();
        if (model.DocumentDate.Date > DateTime.Today || model.DocumentDate.Year < 2000) ModelState.AddModelError(nameof(model.DocumentDate), "Geçerli ve gelecekte olmayan bir belge tarihi seçiniz.");
        if (string.IsNullOrWhiteSpace(model.Notes) || model.Notes.Length < 5 || model.Notes.Length > 500) ModelState.AddModelError(nameof(model.Notes), "Sarf açıklaması 5-500 karakter olmalıdır.");
        if (!model.WarehouseId.HasValue) ModelState.AddModelError(nameof(model.WarehouseId), "Kaynak depo seçiniz.");
        if (!ModelState.IsValid) { await FillFormAsync(model, ct); return View(model); }
        try
        {
            var prepared = await consumption.PrepareLinesAsync(model.WarehouseId!.Value, model.Lines, ct);
            await using var tx = await context.Database.BeginTransactionAsync(ct);
            var now = DateTime.Now;
            var document = new PrdInventoryDocument
            {
                DocumentNumber = $"HS-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..8]}", Type = PrdInventoryDocumentType.RawMaterialConsumption,
                Status = PrdInventoryDocumentStatus.Draft, DocumentDate = model.DocumentDate.Date, SourceWarehouseId = model.WarehouseId,
                SourceDocumentType = RawMaterialConsumptionService.SourceType, CurrencyCode = "TRY", ExchangeRate = 1,
                Notes = model.Notes, TotalCost = prepared.Sum(x => x.TotalCost), IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = Actor
            };
            context.PrdInventoryDocuments.Add(document);
            await context.SaveChangesAsync(ct);
            foreach (var line in prepared) { line.InventoryDocumentId = document.ID; line.CreateDate = now; line.CreateUserID = Actor; }
            context.PrdInventoryDocumentLines.AddRange(prepared);
            Audit("TaslakOlustur", document, null);
            await context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            TempData["success"] = "Sarf taslağı oluşturuldu. Henüz stok düşmedi; detay ekranından Stoktan Düş işlemini uygulayınız.";
            return RedirectToAction(nameof(Detay), new { id = document.ID });
        }
        catch (InvalidOperationException e) { context.ChangeTracker.Clear(); ModelState.AddModelError(string.Empty, e.Message); await FillFormAsync(model, ct); return View(model); }
        catch (OverflowException) { context.ChangeTracker.Clear(); ModelState.AddModelError(string.Empty, "Dönüştürülmüş miktar veya maliyet desteklenen aralığı aşıyor."); await FillFormAsync(model, ct); return View(model); }
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await Documents.AsNoTracking().Where(x => x.ID == id).Select(x => new RawMaterialConsumptionDetailVM
        {
            Id = x.ID, Number = x.DocumentNumber, Date = x.DocumentDate, Status = x.Status, CreatedBy = x.CreateUserID, Notes = x.Notes,
            TotalCost = x.TotalCost, PostingDate = x.PostingDate, PostedBy = x.PostedUserId, ReversalDocumentId = x.ReversalDocumentId,
            Warehouse = context.PrdWarehouses.Where(w => w.ID == x.SourceWarehouseId).Select(w => w.Code + " - " + w.Name).FirstOrDefault() ?? "-"
        }).FirstOrDefaultAsync(ct);
        if (model == null) return NotFound();
        var rows = await (from line in context.PrdInventoryDocumentLines.AsNoTracking()
                          join material in context.PrdMaterials.AsNoTracking() on line.MaterialId equals material.ID
                          join unit in context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID
                          where line.InventoryDocumentId == id && line.IsDelete != true orderby line.Sequence
                          select new { Line = line, material.Code, material.Name, UnitCode = unit.Code, UnitName = unit.Name }).ToListAsync(ct);
        model.Lines = rows.Select(x => new RawMaterialConsumptionDetailLineVM
        {
            Sequence = x.Line.Sequence, MaterialCode = x.Code, MaterialName = x.Name, Unit = ProductionUnitNormalizer.DisplayName(x.UnitCode, x.UnitName),
            LotNumber = x.Line.LotNumber, ProductionDate = x.Line.ProductionDate, ExpirationDate = x.Line.ExpirationDate,
            Quantity = x.Line.Quantity, UnitCost = x.Line.UnitCost, TotalCost = x.Line.TotalCost, Notes = x.Line.Notes
        }).ToList();
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "HammaddeSarfYonet")]
    public async Task<IActionResult> StoktanDus(int id, bool confirm, CancellationToken ct)
    {
        if (!confirm) { TempData["error"] = "Stoktan düşme işlemini onaylayınız."; return RedirectToAction(nameof(Detay), new { id }); }
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await LoadForUpdateAsync(id, ct);
        if (document == null) return NotFound();
        if (document.Status != PrdInventoryDocumentStatus.Draft) { TempData["error"] = "Yalnızca taslak fiş stoktan düşülebilir; bu fiş daha önce işlenmiş veya kapatılmış."; return RedirectToAction(nameof(Detay), new { id }); }
        try
        {
            if (!document.SourceWarehouseId.HasValue) throw new InvalidOperationException("Kaynak depo bulunamadı.");
            if (document.DocumentDate.Date > DateTime.Today) throw new InvalidOperationException("Gelecek tarihli fiş stoktan düşülemez.");
            // Serialize manual consumption in the same warehouse before reading its balances.
            if (context.Database.IsSqlServer())
                await context.Database.ExecuteSqlInterpolatedAsync($"SELECT [ID] FROM [dbo].[PrdWarehouse] WITH (UPDLOCK,HOLDLOCK) WHERE [ID] = {document.SourceWarehouseId.Value}", ct);
            var lines = await context.PrdInventoryDocumentLines.Where(x => x.InventoryDocumentId == id && x.IsDelete != true).OrderBy(x => x.Sequence).ToListAsync(ct);
            var prepared = await consumption.PrepareLinesAsync(document.SourceWarehouseId.Value, lines.Select(x => new RawMaterialConsumptionInputVM
            { StockLotId = x.SourceStockLotId, Quantity = x.Quantity.ToString(CultureInfo.InvariantCulture), Notes = x.Notes }).ToList(), ct);
            var converter = await ProductionStockUnitConverter.LoadAsync(context, lines.Select(x => x.MaterialId).Distinct().ToList(), ct);
            var now = DateTime.Now;
            for (var i = 0; i < lines.Count; i++)
            {
                var line = lines[i]; var current = prepared[i];
                if (line.MaterialId != current.MaterialId || !converter.IsBaseUnitEquivalent(line.MaterialId, line.UnitId))
                    throw new InvalidOperationException("Taslak oluşturulduktan sonra malzeme/birim bilgisi değişmiş. Taslağı iptal edip yeniden hazırlayınız.");
                line.UnitId = current.UnitId; line.UnitCost = current.UnitCost; line.OriginalUnitCost = current.OriginalUnitCost; line.TotalCost = current.TotalCost;
                line.LotNumber = current.LotNumber; line.ProductionDate = current.ProductionDate; line.ExpirationDate = current.ExpirationDate;
                line.UpdateDate = now; line.UpdateUserID = Actor;
                context.PrdStockMovements.Add(new PrdStockMovement
                {
                    InventoryDocumentId = id, InventoryDocumentLineId = line.ID, MaterialId = line.MaterialId, WarehouseId = document.SourceWarehouseId.Value,
                    StockLotId = line.SourceStockLotId, Direction = PrdStockDirection.Out, MovementType = PrdStockMovementType.RawMaterialConsumption,
                    Quantity = line.Quantity, UnitId = line.UnitId, UnitCost = line.UnitCost, OriginalUnitCost = line.OriginalUnitCost, TotalCost = line.TotalCost,
                    CostSource = PrdStockCostSource.Manual, CurrencyCode = "TRY", ExchangeRate = 1, MovementDate = document.DocumentDate,
                    DocumentNumber = document.DocumentNumber, DocumentType = PrdStockDocumentType.InventoryDocument, DocumentId = id,
                    Description = document.Notes, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = Actor
                });
            }
            document.Status = PrdInventoryDocumentStatus.Posted; document.PostingDate = now; document.PostedUserId = Actor;
            document.TotalCost = lines.Sum(x => x.TotalCost); document.UpdateDate = now; document.UpdateUserID = Actor;
            Audit(nameof(StoktanDus), document, null);
            await context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = "Hammaddeler seçilen depo ve lotların stoklarından düşüldü. Üretim emri veya Logo fişi oluşturulmadı.";
        }
        catch (InvalidOperationException e) { await tx.RollbackAsync(ct); context.ChangeTracker.Clear(); TempData["error"] = e.Message; }
        catch (OverflowException) { await tx.RollbackAsync(ct); context.ChangeTracker.Clear(); TempData["error"] = "Dönüştürülmüş miktar veya maliyet desteklenen aralığı aşıyor; stok düşülmedi."; }
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "HammaddeSarfYonet")]
    public async Task<IActionResult> TaslagiIptalEt(int id, CancellationToken ct)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await LoadForUpdateAsync(id, ct);
        if (document == null) return NotFound();
        if (document.Status != PrdInventoryDocumentStatus.Draft) { TempData["error"] = "Yalnızca taslak fiş iptal edilebilir. İşlenmiş fiş için geri alma işlemini kullanınız."; return RedirectToAction(nameof(Detay), new { id }); }
        document.Status = PrdInventoryDocumentStatus.Cancelled; document.UpdateDate = DateTime.Now; document.UpdateUserID = Actor;
        Audit(nameof(TaslagiIptalEt), document, null);
        await context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
        TempData["success"] = "Taslak iptal edildi; stok hareketi oluşmadı.";
        return RedirectToAction(nameof(Detay), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "HammaddeSarfGeriAl")]
    public async Task<IActionResult> GeriAl(int id, string? reason, bool confirm, CancellationToken ct)
    {
        reason = reason?.Trim();
        if (!confirm || string.IsNullOrWhiteSpace(reason) || reason.Length < 5 || reason.Length > 500)
        { TempData["error"] = "5-500 karakterlik geri alma gerekçesi girip stoğun geri ekleneceğini onaylayınız."; return RedirectToAction(nameof(Detay), new { id }); }
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var document = await LoadForUpdateAsync(id, ct);
        if (document == null) return NotFound();
        try
        {
            var reverse = await reversal.ReversePostedDocumentAsync(document, reason, Actor, ct);
            Audit(nameof(GeriAl), document, reason);
            await context.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            TempData["success"] = $"Sarf geri alındı; miktar ve maliyet aynı lotlara geri eklendi. Ters belge: {reverse.DocumentNumber}";
        }
        catch (InvalidOperationException e) { await tx.RollbackAsync(ct); TempData["error"] = e.Message; }
        return RedirectToAction(nameof(Detay), new { id });
    }

    private async Task<PrdInventoryDocument?> LoadForUpdateAsync(int id, CancellationToken ct)
    {
        var query = context.Database.IsSqlServer()
            ? context.PrdInventoryDocuments.FromSqlInterpolated($"SELECT * FROM [dbo].[PrdInventoryDocument] WITH (UPDLOCK,HOLDLOCK) WHERE [ID] = {id}")
            : context.PrdInventoryDocuments.AsQueryable();
        return await query.FirstOrDefaultAsync(x => x.IsDelete != true && x.Type == PrdInventoryDocumentType.RawMaterialConsumption &&
            x.SourceDocumentType == RawMaterialConsumptionService.SourceType, ct);
    }

    private async Task FillFormAsync(RawMaterialConsumptionCreateVM model, CancellationToken ct)
    {
        model.Warehouses = await context.PrdWarehouses.AsNoTracking().Where(x => x.IsDelete != true && x.IsActive != false && x.Type != PrdWarehouseType.Quarantine && x.Type != PrdWarehouseType.Scrap)
            .OrderBy(x => x.Code).Select(x => new SelectListItem(x.Code + " - " + x.Name, x.ID.ToString())).ToListAsync(ct);
        if (model.WarehouseId.HasValue && model.Warehouses.Any(x => x.Value == model.WarehouseId.Value.ToString())) model.Lots = await consumption.LoadLotsAsync(model.WarehouseId.Value, ct);
        while (model.Lines.Count < 5) model.Lines.Add(new());
    }

    private void Audit(string action, PrdInventoryDocument document, string? reason) => context.UserActivityLog.Add(new UserActivityLog
    {
        DateTime = DateTime.Now, UserName = Actor.Length > 100 ? Actor[..100] : Actor, ControllerName = nameof(HammaddeSarfController), ActionName = action,
        Parameters = JsonSerializer.Serialize(new { document.ID, document.DocumentNumber, document.SourceWarehouseId, document.TotalCost, Reason = reason }), Info = "ManualRawMaterialConsumption"
    });
}
