using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrQualityOrPurchasing")]
public sealed class SpesifikasyonYonetimiController : Controller
{
    private readonly ApplicationDbContext _context;

    public SpesifikasyonYonetimiController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        ViewBag.Modul = "KaliteYonetimi";
        search = Clean(search);
        var materials = await _context.PrdMaterials.AsNoTracking()
            .Where(x => x.IsDelete != true && (string.IsNullOrEmpty(search) || EF.Functions.Like(x.Code, $"%{search}%") || EF.Functions.Like(x.Name, $"%{search}%")))
            .OrderBy(x => x.Code)
            .Select(x => new { x.ID, x.Code, x.Name, x.Type, x.QualityControlRequirement })
            .Take(500)
            .ToListAsync(ct);
        var materialIds = materials.Select(x => x.ID).ToList();
        var sets = await _context.PrdMaterialSpecificationSets.AsNoTracking()
            .Where(x => materialIds.Contains(x.MaterialId) && x.IsDelete != true)
            .Select(x => new { x.ID, x.MaterialId, x.VersionNumber, x.Status })
            .ToListAsync(ct);
        var activeSetIds = sets.Where(x => x.Status == PrdSpecificationSetStatus.Active).Select(x => x.ID).ToList();
        var itemCounts = await _context.PrdMaterialSpecificationItems.AsNoTracking()
            .Where(x => activeSetIds.Contains(x.SpecificationSetId) && x.IsDelete != true)
            .GroupBy(x => x.SpecificationSetId)
            .Select(x => new { SetId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.SetId, x => x.Count, ct);

        var model = new MaterialSpecificationIndexVM
        {
            Search = search,
            Materials = materials.Select(material =>
            {
                var materialSets = sets.Where(x => x.MaterialId == material.ID).ToList();
                var active = materialSets.Where(x => x.Status == PrdSpecificationSetStatus.Active).OrderByDescending(x => x.VersionNumber).FirstOrDefault();
                return new MaterialSpecificationMaterialVM
                {
                    MaterialId = material.ID,
                    MaterialCode = material.Code,
                    MaterialName = material.Name,
                    MaterialType = material.Type,
                    QualityRequirement = material.QualityControlRequirement,
                    VersionCount = materialSets.Count,
                    ActiveSetId = active?.ID,
                    ActiveVersion = active?.VersionNumber,
                    ActiveItemCount = active != null && itemCounts.TryGetValue(active.ID, out var count) ? count : 0
                };
            }).ToList()
        };
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int materialId, int? setId, CancellationToken ct)
    {
        ViewBag.Modul = "KaliteYonetimi";
        var model = await BuildDetailAsync(materialId, setId, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> YeniVersiyon(int materialId, int? sourceSetId, CancellationToken ct)
    {
        var material = await _context.PrdMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.ID == materialId && x.IsDelete != true, ct);
        if (material == null) return NotFound();
        var source = sourceSetId.HasValue
            ? await _context.PrdMaterialSpecificationSets.AsNoTracking().FirstOrDefaultAsync(x => x.ID == sourceSetId && x.MaterialId == materialId && x.IsDelete != true, ct)
            : null;
        var nextVersion = (await _context.PrdMaterialSpecificationSets.Where(x => x.MaterialId == materialId && x.IsDelete != true).MaxAsync(x => (int?)x.VersionNumber, ct) ?? 0) + 1;
        var now = DateTime.Now;
        var set = new PrdMaterialSpecificationSet
        {
            MaterialId = materialId,
            SpecificationCode = source?.SpecificationCode ?? $"{material.Code}-SPK",
            VersionNumber = nextVersion,
            Status = PrdSpecificationSetStatus.Draft,
            ValidFrom = now.Date,
            Notes = source == null ? null : $"v{source.VersionNumber} versiyonundan kopyalandı.",
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = CurrentUser
        };
        _context.PrdMaterialSpecificationSets.Add(set);
        await _context.SaveChangesAsync(ct);

        if (source != null)
        {
            var sourceItems = await _context.PrdMaterialSpecificationItems.AsNoTracking().Where(x => x.SpecificationSetId == source.ID && x.IsDelete != true).ToListAsync(ct);
            foreach (var item in sourceItems)
            {
                _context.PrdMaterialSpecificationItems.Add(new PrdMaterialSpecificationItem
                {
                    SpecificationSetId = set.ID, Sequence = item.Sequence, Code = item.Code, Name = item.Name,
                    DataType = item.DataType, UnitName = item.UnitName, TargetValue = item.TargetValue,
                    MinimumValue = item.MinimumValue, MaximumValue = item.MaximumValue, ExpectedText = item.ExpectedText,
                    ExpectedBoolean = item.ExpectedBoolean, AllowedValues = item.AllowedValues, TestMethod = item.TestMethod,
                    IsRequired = item.IsRequired, Criticality = item.Criticality, DecimalPlaces = item.DecimalPlaces,
                    Notes = item.Notes, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser
                });
            }
        }
        AddHistory(set.ID, null, "Versiyon Oluşturuldu", source == null ? "Yeni taslak spesifikasyon oluşturuldu." : $"v{source.VersionNumber} versiyonundan kopyalandı.", now);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"Spesifikasyon v{nextVersion} taslağı oluşturuldu.";
        return RedirectToAction(nameof(Detay), new { materialId, setId = set.ID });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> BaslikKaydet(MaterialSpecificationSetFormVM model, CancellationToken ct)
    {
        var set = await _context.PrdMaterialSpecificationSets.FirstOrDefaultAsync(x => x.ID == model.Id && x.IsDelete != true, ct);
        if (set == null) return NotFound();
        if (set.Status != PrdSpecificationSetStatus.Draft) return ImmutableSet(set.MaterialId, set.ID);
        if (model.ValidFrom.HasValue && model.ValidTo.HasValue && model.ValidTo.Value.Date < model.ValidFrom.Value.Date)
            ModelState.AddModelError(nameof(model.ValidTo), "Geçerlilik bitişi başlangıç tarihinden önce olamaz.");
        if (!ModelState.IsValid)
        {
            TempData["error"] = string.Join(" ", ModelState.Values.SelectMany(x => x.Errors).Select(x => x.ErrorMessage));
            return RedirectToAction(nameof(Detay), new { materialId = set.MaterialId, setId = set.ID });
        }
        var now = DateTime.Now;
        set.SpecificationCode = model.SpecificationCode.Trim();
        set.ValidFrom = model.ValidFrom?.Date;
        set.ValidTo = model.ValidTo?.Date;
        set.Notes = Clean(model.Notes);
        set.UpdateDate = now;
        set.UpdateUserID = CurrentUser;
        AddHistory(set.ID, null, "Başlık Güncellendi", "Taslak başlık bilgileri güncellendi.", now);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Spesifikasyon başlığı güncellendi.";
        return RedirectToAction(nameof(Detay), new { materialId = set.MaterialId, setId = set.ID });
    }

    [HttpGet, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> MaddeDuzenle(int setId, int? id, CancellationToken ct)
    {
        ViewBag.Modul = "KaliteYonetimi";
        var set = await _context.PrdMaterialSpecificationSets.AsNoTracking().FirstOrDefaultAsync(x => x.ID == setId && x.IsDelete != true, ct);
        if (set == null) return NotFound();
        if (set.Status != PrdSpecificationSetStatus.Draft) return ImmutableSet(set.MaterialId, set.ID);
        var model = new MaterialSpecificationItemFormVM { SpecificationSetId = setId };
        if (id.HasValue)
        {
            var item = await _context.PrdMaterialSpecificationItems.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.SpecificationSetId == setId && x.IsDelete != true, ct);
            if (item == null) return NotFound();
            model = ToForm(item);
        }
        else
        {
            model.Sequence = (await _context.PrdMaterialSpecificationItems.Where(x => x.SpecificationSetId == setId && x.IsDelete != true).MaxAsync(x => (int?)x.Sequence, ct) ?? 0) + 1;
        }
        ViewBag.MaterialId = set.MaterialId;
        ViewBag.VersionNumber = set.VersionNumber;
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> MaddeDuzenle(MaterialSpecificationItemFormVM model, CancellationToken ct)
    {
        var set = await _context.PrdMaterialSpecificationSets.FirstOrDefaultAsync(x => x.ID == model.SpecificationSetId && x.IsDelete != true, ct);
        if (set == null) return NotFound();
        if (set.Status != PrdSpecificationSetStatus.Draft) return ImmutableSet(set.MaterialId, set.ID);
        ValidateItem(model);
        var duplicate = await _context.PrdMaterialSpecificationItems.AnyAsync(x => x.SpecificationSetId == set.ID && x.IsDelete != true && x.ID != model.Id && (x.Code == model.Code.Trim() || x.Sequence == model.Sequence), ct);
        if (duplicate) ModelState.AddModelError(nameof(model.Code), "Aynı versiyonda spek kodu ve sıra numarası benzersiz olmalıdır.");
        if (!ModelState.IsValid)
        {
            ViewBag.Modul = "KaliteYonetimi";
            ViewBag.MaterialId = set.MaterialId;
            ViewBag.VersionNumber = set.VersionNumber;
            return View(model);
        }

        PrdMaterialSpecificationItem item;
        var now = DateTime.Now;
        var action = "Madde Eklendi";
        if (model.Id == 0)
        {
            item = new PrdMaterialSpecificationItem { SpecificationSetId = set.ID, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser };
            _context.PrdMaterialSpecificationItems.Add(item);
        }
        else
        {
            var existingItem = await _context.PrdMaterialSpecificationItems.FirstOrDefaultAsync(x => x.ID == model.Id && x.SpecificationSetId == set.ID && x.IsDelete != true, ct);
            if (existingItem == null) return NotFound();
            item = existingItem;
            item.UpdateDate = now;
            item.UpdateUserID = CurrentUser;
            action = "Madde Güncellendi";
        }
        ApplyItem(model, item);
        await _context.SaveChangesAsync(ct);
        AddHistory(set.ID, item.ID, action, $"{item.Code} - {item.Name}", now);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = action + ".";
        return RedirectToAction(nameof(Detay), new { materialId = set.MaterialId, setId = set.ID });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> MaddeSil(int id, CancellationToken ct)
    {
        var item = await _context.PrdMaterialSpecificationItems.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (item == null) return NotFound();
        var set = await _context.PrdMaterialSpecificationSets.FirstAsync(x => x.ID == item.SpecificationSetId, ct);
        if (set.Status != PrdSpecificationSetStatus.Draft) return ImmutableSet(set.MaterialId, set.ID);
        var now = DateTime.Now;
        item.IsDelete = true;
        item.IsActive = false;
        item.DeleteDate = now;
        item.DeleteUserID = CurrentUser;
        AddHistory(set.ID, item.ID, "Madde Silindi", $"{item.Code} - {item.Name}", now);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Spesifikasyon maddesi silindi.";
        return RedirectToAction(nameof(Detay), new { materialId = set.MaterialId, setId = set.ID });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = "AdminOrQuality")]
    public async Task<IActionResult> Aktiflestir(int id, CancellationToken ct)
    {
        var set = await _context.PrdMaterialSpecificationSets.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (set == null) return NotFound();
        if (set.Status != PrdSpecificationSetStatus.Draft) return ImmutableSet(set.MaterialId, set.ID);
        if (!await _context.PrdMaterialSpecificationItems.AnyAsync(x => x.SpecificationSetId == id && x.IsDelete != true, ct))
        {
            TempData["error"] = "En az bir spesifikasyon maddesi eklenmeden versiyon aktifleştirilemez.";
            return RedirectToAction(nameof(Detay), new { materialId = set.MaterialId, setId = set.ID });
        }
        var now = DateTime.Now;
        var activeSets = await _context.PrdMaterialSpecificationSets.Where(x => x.MaterialId == set.MaterialId && x.ID != set.ID && x.Status == PrdSpecificationSetStatus.Active && x.IsDelete != true).ToListAsync(ct);
        foreach (var active in activeSets)
        {
            active.Status = PrdSpecificationSetStatus.Passive;
            active.ValidTo = now.Date;
            active.UpdateDate = now;
            active.UpdateUserID = CurrentUser;
            AddHistory(active.ID, null, "Pasife Alındı", $"v{set.VersionNumber} aktifleştirildiği için pasife alındı.", now);
        }
        set.Status = PrdSpecificationSetStatus.Active;
        set.ValidFrom ??= now.Date;
        set.ApprovedDate = now;
        set.ApprovedUserId = CurrentUser;
        set.UpdateDate = now;
        set.UpdateUserID = CurrentUser;
        var material = await _context.PrdMaterials.FirstAsync(x => x.ID == set.MaterialId, ct);
        material.QualityControlRequirement = PrdQualityControlRequirement.Required;
        material.UpdateDate = now;
        material.UpdateUserID = CurrentUser;
        AddHistory(set.ID, null, "Aktifleştirildi", $"v{set.VersionNumber} kalite kontrolünde kullanılmak üzere aktifleştirildi.", now);
        await _context.SaveChangesAsync(ct);
        TempData["success"] = $"Spesifikasyon v{set.VersionNumber} aktifleştirildi.";
        return RedirectToAction(nameof(Detay), new { materialId = set.MaterialId, setId = set.ID });
    }

    private async Task<MaterialSpecificationDetailVM?> BuildDetailAsync(int materialId, int? setId, CancellationToken ct)
    {
        var material = await _context.PrdMaterials.AsNoTracking().FirstOrDefaultAsync(x => x.ID == materialId && x.IsDelete != true, ct);
        if (material == null) return null;
        var versions = await _context.PrdMaterialSpecificationSets.AsNoTracking().Where(x => x.MaterialId == materialId && x.IsDelete != true).OrderByDescending(x => x.VersionNumber).ToListAsync(ct);
        var selected = setId.HasValue ? versions.FirstOrDefault(x => x.ID == setId) : versions.FirstOrDefault(x => x.Status == PrdSpecificationSetStatus.Active) ?? versions.FirstOrDefault();
        var counts = await _context.PrdMaterialSpecificationItems.AsNoTracking().Where(x => versions.Select(v => v.ID).Contains(x.SpecificationSetId) && x.IsDelete != true).GroupBy(x => x.SpecificationSetId).Select(x => new { x.Key, Count = x.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, ct);
        var model = new MaterialSpecificationDetailVM
        {
            MaterialId = material.ID, MaterialCode = material.Code, MaterialName = material.Name, MaterialType = material.Type,
            SelectedSetId = selected?.ID,
            Versions = versions.Select(x => new MaterialSpecificationVersionVM { Id = x.ID, VersionNumber = x.VersionNumber, Status = x.Status, ValidFrom = x.ValidFrom, ValidTo = x.ValidTo, ItemCount = counts.GetValueOrDefault(x.ID) }).ToList()
        };
        if (selected == null) return model;
        model.Set = new MaterialSpecificationSetFormVM { Id = selected.ID, MaterialId = selected.MaterialId, SpecificationCode = selected.SpecificationCode, VersionNumber = selected.VersionNumber, Status = selected.Status, ValidFrom = selected.ValidFrom, ValidTo = selected.ValidTo, Notes = selected.Notes, ApprovedDate = selected.ApprovedDate, ApprovedUserId = selected.ApprovedUserId };
        model.Items = await _context.PrdMaterialSpecificationItems.AsNoTracking().Where(x => x.SpecificationSetId == selected.ID && x.IsDelete != true).OrderBy(x => x.Sequence).Select(x => new MaterialSpecificationItemVM { Id = x.ID, Sequence = x.Sequence, Code = x.Code, Name = x.Name, DataType = x.DataType, UnitName = x.UnitName, TargetValue = x.TargetValue, MinimumValue = x.MinimumValue, MaximumValue = x.MaximumValue, ExpectedText = x.ExpectedText, ExpectedBoolean = x.ExpectedBoolean, AllowedValues = x.AllowedValues, TestMethod = x.TestMethod, IsRequired = x.IsRequired, Criticality = x.Criticality, DecimalPlaces = x.DecimalPlaces, Notes = x.Notes }).ToListAsync(ct);
        model.History = await _context.PrdMaterialSpecificationHistories.AsNoTracking().Where(x => x.SpecificationSetId == selected.ID && x.IsDelete != true).OrderByDescending(x => x.ActionDate).Take(100).Select(x => new MaterialSpecificationHistoryVM { ActionDate = x.ActionDate, Action = x.Action, Description = x.Description, ActionUserId = x.ActionUserId }).ToListAsync(ct);
        return model;
    }

    private void ValidateItem(MaterialSpecificationItemFormVM model)
    {
        decimal? target = ParseOptional(model.TargetValue, nameof(model.TargetValue));
        decimal? minimum = ParseOptional(model.MinimumValue, nameof(model.MinimumValue));
        decimal? maximum = ParseOptional(model.MaximumValue, nameof(model.MaximumValue));
        if (minimum.HasValue && maximum.HasValue && minimum > maximum) ModelState.AddModelError(nameof(model.MaximumValue), "Maksimum değer minimum değerden küçük olamaz.");
        switch (model.DataType)
        {
            case PrdSpecificationDataType.Numeric when !target.HasValue && !minimum.HasValue && !maximum.HasValue:
                ModelState.AddModelError(nameof(model.TargetValue), "Sayısal spek için hedef, minimum veya maksimum değerlerden en az biri girilmelidir.");
                break;
            case PrdSpecificationDataType.Text when string.IsNullOrWhiteSpace(model.ExpectedText):
                ModelState.AddModelError(nameof(model.ExpectedText), "Metin speki için beklenen değer girilmelidir.");
                break;
            case PrdSpecificationDataType.Boolean when !model.ExpectedBoolean.HasValue:
                ModelState.AddModelError(nameof(model.ExpectedBoolean), "Beklenen uygunluk sonucu seçilmelidir.");
                break;
            case PrdSpecificationDataType.Selection when SplitAllowedValues(model.AllowedValues).Count < 2:
                ModelState.AddModelError(nameof(model.AllowedValues), "Seçenek tipi için noktalı virgülle ayrılmış en az iki değer girilmelidir.");
                break;
        }
    }

    private decimal? ParseOptional(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (TryParseDecimal(value, out var result)) return result;
        ModelState.AddModelError(field, "Geçerli bir sayı giriniz.");
        return null;
    }

    private static void ApplyItem(MaterialSpecificationItemFormVM model, PrdMaterialSpecificationItem item)
    {
        item.Sequence = model.Sequence;
        item.Code = model.Code.Trim();
        item.Name = model.Name.Trim();
        item.DataType = model.DataType;
        item.UnitName = Clean(model.UnitName);
        item.TargetValue = TryParseDecimal(model.TargetValue, out var target) ? target : null;
        item.MinimumValue = TryParseDecimal(model.MinimumValue, out var min) ? min : null;
        item.MaximumValue = TryParseDecimal(model.MaximumValue, out var max) ? max : null;
        item.ExpectedText = Clean(model.ExpectedText);
        item.ExpectedBoolean = model.ExpectedBoolean;
        item.AllowedValues = model.DataType == PrdSpecificationDataType.Selection ? string.Join(";", SplitAllowedValues(model.AllowedValues)) : Clean(model.AllowedValues);
        item.TestMethod = Clean(model.TestMethod);
        item.IsRequired = model.IsRequired;
        item.Criticality = model.Criticality;
        item.DecimalPlaces = model.DecimalPlaces;
        item.Notes = Clean(model.Notes);
    }

    private static MaterialSpecificationItemFormVM ToForm(PrdMaterialSpecificationItem item) => new()
    {
        Id = item.ID, SpecificationSetId = item.SpecificationSetId, Sequence = item.Sequence, Code = item.Code, Name = item.Name,
        DataType = item.DataType, UnitName = item.UnitName, TargetValue = Format(item.TargetValue), MinimumValue = Format(item.MinimumValue),
        MaximumValue = Format(item.MaximumValue), ExpectedText = item.ExpectedText, ExpectedBoolean = item.ExpectedBoolean,
        AllowedValues = item.AllowedValues, TestMethod = item.TestMethod, IsRequired = item.IsRequired,
        Criticality = item.Criticality, DecimalPlaces = item.DecimalPlaces, Notes = item.Notes
    };

    private IActionResult ImmutableSet(int materialId, int setId)
    {
        TempData["error"] = "Aktif veya pasif spesifikasyon değiştirilemez. Yeni bir versiyon oluşturunuz.";
        return RedirectToAction(nameof(Detay), new { materialId, setId });
    }

    private void AddHistory(int setId, int? itemId, string action, string? description, DateTime now) =>
        _context.PrdMaterialSpecificationHistories.Add(new PrdMaterialSpecificationHistory { SpecificationSetId = setId, SpecificationItemId = itemId, Action = action, Description = description, ActionDate = now, ActionUserId = CurrentUser, IsActive = true, IsDelete = false, CreateDate = now, CreateUserID = CurrentUser });

    private string CurrentUser => User.Identity?.Name ?? "system";
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string? Format(decimal? value) => value?.ToString("0.########", CultureInfo.GetCultureInfo("tr-TR"));
    private static List<string> SplitAllowedValues(string? value) => (value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

    private static bool TryParseDecimal(string? value, out decimal result)
    {
        result = 0;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var normalized = value.Trim().Replace(" ", string.Empty);
        if (normalized.Contains(',') && normalized.Contains('.')) normalized = normalized.LastIndexOf(',') > normalized.LastIndexOf('.') ? normalized.Replace(".", string.Empty).Replace(',', '.') : normalized.Replace(",", string.Empty);
        else if (normalized.Contains(',')) normalized = normalized.Replace(',', '.');
        return decimal.TryParse(normalized, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out result);
    }
}
