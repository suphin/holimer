using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class ProductionRecipeController : Controller
{
    private const string ImportSessionKey = "PrdRecipeImportRows";
    private readonly ApplicationDbContext _context;

    public ProductionRecipeController(ApplicationDbContext context) => _context = context;

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await (
            from recipe in _context.PrdRecipes.AsNoTracking()
            join version in _context.PrdRecipeVersions.AsNoTracking() on recipe.ID equals version.RecipeId
            join product in _context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID
            join unit in _context.PrdUnits.AsNoTracking() on version.UnitId equals unit.ID
            where recipe.IsDelete != true && version.IsDelete != true
            orderby recipe.Code, version.VersionNumber descending
            select new ProductionRecipeListVM
            {
                RecipeId = recipe.ID,
                RecipeVersionId = version.ID,
                Code = recipe.Code,
                Name = recipe.Name,
                Product = product.Code + " - " + product.Name,
                VersionNumber = version.VersionNumber,
                BaseQuantity = version.BaseQuantity,
                Unit = unit.Name,
                Status = version.Status,
                ItemCount = _context.PrdRecipeItems.Count(x => x.RecipeVersionId == version.ID && x.IsDelete != true),
                ValidFrom = version.ValidFrom,
                ValidTo = version.ValidTo
            }).ToListAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        var model = new ProductionRecipeCreateVM();
        await FillSelections(model, cancellationToken);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductionRecipeCreateVM model, [FromForm(Name = "BaseQuantity")] string baseQuantityText, CancellationToken cancellationToken)
    {
        ViewBag.Modul = "YeniUretim";
        if (!TryParseProductionDecimal(baseQuantityText, out var parsedBaseQuantity)) ModelState.AddModelError(nameof(model.BaseQuantity), "Baz miktar geçerli bir sayı olmalıdır.");
        else model.BaseQuantity = parsedBaseQuantity;
        model.Code = (model.Code ?? string.Empty).Trim();
        model.Name = (model.Name ?? string.Empty).Trim();

        if (model.BaseQuantity <= 0)
            ModelState.AddModelError(nameof(model.BaseQuantity), "Baz miktar sıfırdan büyük olmalıdır.");
        if (model.ValidFrom.HasValue && model.ValidTo.HasValue && model.ValidTo < model.ValidFrom)
            ModelState.AddModelError(nameof(model.ValidTo), "Bitiş tarihi başlangıç tarihinden önce olamaz.");
        if (!await _context.PrdMaterials.AnyAsync(x => x.ID == model.ProductMaterialId && x.IsDelete != true, cancellationToken))
            ModelState.AddModelError(nameof(model.ProductMaterialId), "Seçilen yeni üretim malzeme kartı bulunamadı.");
        if (!await _context.PrdUnits.AnyAsync(x => x.ID == model.UnitId && x.IsDelete != true, cancellationToken))
            ModelState.AddModelError(nameof(model.UnitId), "Seçilen üretim birimi bulunamadı.");

        var existingRecipe = await _context.PrdRecipes
            .FirstOrDefaultAsync(x => x.Code == model.Code && x.IsDelete != true, cancellationToken);
        if (existingRecipe != null && await _context.PrdRecipeVersions.AnyAsync(
                x => x.RecipeId == existingRecipe.ID && x.VersionNumber == model.VersionNumber && x.IsDelete != true,
                cancellationToken))
            ModelState.AddModelError(nameof(model.VersionNumber), "Bu reçete versiyonu zaten kayıtlı.");

        if (!ModelState.IsValid)
        {
            await FillSelections(model, cancellationToken);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var recipe = existingRecipe;
        if (recipe == null)
        {
            recipe = new PrdRecipe
            {
                Code = model.Code, Name = model.Name, ProductMaterialId = model.ProductMaterialId,
                Description = model.Description, IsActive = true, IsDelete = false,
                CreateDate = DateTime.Now, CreateUserID = User.Identity?.Name
            };
            _context.PrdRecipes.Add(recipe);
            await _context.SaveChangesAsync(cancellationToken);
        }

        _context.PrdRecipeVersions.Add(new PrdRecipeVersion
        {
            RecipeId = recipe.ID, VersionNumber = model.VersionNumber, BaseQuantity = model.BaseQuantity,
            UnitId = model.UnitId, Status = model.Status, ValidFrom = model.ValidFrom, ValidTo = model.ValidTo,
            Notes = model.Notes, IsActive = true, IsDelete = false,
            CreateDate = DateTime.Now, CreateUserID = User.Identity?.Name
        });
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        TempData["success"] = "Yeni üretim reçetesi oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await (from version in _context.PrdRecipeVersions.AsNoTracking()
                           join recipe in _context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID
                           join product in _context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID
                           where version.ID == id && version.IsDelete != true && recipe.IsDelete != true
                           select new ProductionRecipeDetailVM
                           {
                               RecipeId = recipe.ID, RecipeVersionId = version.ID, Code = recipe.Code, Name = recipe.Name,
                               Product = product.Code + " - " + product.Name, BaseQuantity = version.BaseQuantity,
                               UnitId = version.UnitId, Status = version.Status, ValidFrom = version.ValidFrom,
                               ValidTo = version.ValidTo, Description = recipe.Description, Notes = version.Notes
                           }).FirstOrDefaultAsync(ct);
        if (model == null) return NotFound();

        model.Items = await (from item in _context.PrdRecipeItems.AsNoTracking()
                             join material in _context.PrdMaterials.AsNoTracking() on item.MaterialId equals material.ID
                             join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID
                             where item.RecipeVersionId == id && item.IsDelete != true
                             orderby item.Sequence
                             select new ProductionRecipeItemVM
                             {
                                 Id=item.ID, Sequence=item.Sequence, MaterialId=material.ID, MaterialCode=material.Code,
                                 MaterialName=material.Name, MaterialType=material.Type, Quantity=item.Quantity,
                                 UnitId=unit.ID, Unit=unit.Name, PlannedWasteRate=item.PlannedWasteRate,
                                 IsRequired=item.IsRequired, AlternativeGroupCode=item.AlternativeGroupCode, Notes=item.Notes
                             }).ToListAsync(ct);
        model.Units = await _context.PrdUnits.AsNoTracking().Where(x => x.IsDelete != true && x.IsActive != false).OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.ID.ToString())).ToListAsync(ct);
        model.Materials = await _context.PrdMaterials.AsNoTracking().Where(x => x.IsDelete != true && x.IsActive != false).OrderBy(x => x.Code).Select(x => new SelectListItem(x.Code + " - " + x.Name, x.ID.ToString())).ToListAsync(ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateVersion(ProductionRecipeVersionUpdateVM model, [FromForm(Name = "BaseQuantity")] string baseQuantityText, CancellationToken ct)
    {
        if (!TryParseProductionDecimal(baseQuantityText, out var parsedBaseQuantity)) return RecipeEditError(model.RecipeVersionId, "Baz miktar geçerli bir sayı olmalıdır.");
        model.BaseQuantity=parsedBaseQuantity;
        var version = await _context.PrdRecipeVersions.FirstOrDefaultAsync(x => x.ID == model.RecipeVersionId && x.IsDelete != true, ct);
        if (version == null) return NotFound();
        if (version.Status != PrdRecipeStatus.Draft) return RecipeEditError(model.RecipeVersionId, "Yalnızca taslak reçete versiyonları değiştirilebilir.");
        if (string.IsNullOrWhiteSpace(model.Name)) return RecipeEditError(model.RecipeVersionId, "Reçete adı boş bırakılamaz.");
        if (model.BaseQuantity <= 0) return RecipeEditError(model.RecipeVersionId, "Baz miktar sıfırdan büyük olmalıdır.");
        if (model.ValidFrom.HasValue && model.ValidTo.HasValue && model.ValidTo < model.ValidFrom) return RecipeEditError(model.RecipeVersionId, "Geçerlilik bitişi başlangıçtan önce olamaz.");
        if (!await _context.PrdUnits.AnyAsync(x => x.ID == model.UnitId && x.IsDelete != true, ct)) return RecipeEditError(model.RecipeVersionId, "Birim bulunamadı.");
        if (model.Status == PrdRecipeStatus.Active && !await _context.PrdRecipeItems.AnyAsync(x => x.RecipeVersionId == model.RecipeVersionId && x.IsDelete != true, ct))
            return RecipeEditError(model.RecipeVersionId, "Malzeme kalemi bulunmayan reçete aktifleştirilemez.");
        var recipe = await _context.PrdRecipes.FirstAsync(x => x.ID == version.RecipeId, ct);
        recipe.Name = (model.Name ?? string.Empty).Trim(); recipe.Description = model.Description; recipe.UpdateDate = DateTime.Now; recipe.UpdateUserID = User.Identity?.Name;
        version.BaseQuantity=model.BaseQuantity; version.UnitId=model.UnitId; version.Status=model.Status; version.ValidFrom=model.ValidFrom; version.ValidTo=model.ValidTo; version.Notes=model.Notes; version.UpdateDate=DateTime.Now; version.UpdateUserID=User.Identity?.Name;
        if (model.Status == PrdRecipeStatus.Active)
        {
            var otherActiveVersions=await _context.PrdRecipeVersions.Where(x=>x.RecipeId==version.RecipeId&&x.ID!=version.ID&&x.Status==PrdRecipeStatus.Active&&x.IsDelete!=true).ToListAsync(ct);
            foreach(var other in otherActiveVersions){other.Status=PrdRecipeStatus.Passive;other.IsActive=false;other.UpdateDate=DateTime.Now;other.UpdateUserID=User.Identity?.Name;}
            version.IsActive=true; version.ApprovedDate = DateTime.Now; version.ApprovedUserId = User.Identity?.Name;
        }
        await _context.SaveChangesAsync(ct); TempData["success"]="Reçete bilgileri güncellendi."; return RedirectToAction(nameof(Details), new { id=model.RecipeVersionId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(ProductionRecipeItemEditVM model, [FromForm(Name = "Quantity")] string quantityText, [FromForm(Name = "PlannedWasteRate")] string wasteRateText, CancellationToken ct)
    {
        if(!TryParseProductionDecimal(quantityText,out var quantity)||!TryParseProductionDecimal(wasteRateText,out var wasteRate))return RecipeEditError(model.RecipeVersionId,"Miktar veya fire oranı geçerli bir sayı değildir.");
        model.Quantity=quantity;model.PlannedWasteRate=wasteRate;
        var version = await DraftVersion(model.RecipeVersionId, ct); if (version == null) return RecipeEditError(model.RecipeVersionId, "Reçete taslak değil veya bulunamadı.");
        if (model.Quantity <= 0 || model.PlannedWasteRate < 0 || model.PlannedWasteRate > 100) return RecipeEditError(model.RecipeVersionId, "Miktar ve fire oranı geçersiz.");
        if (await _context.PrdRecipeItems.AnyAsync(x => x.RecipeVersionId==model.RecipeVersionId && x.MaterialId==model.MaterialId && x.IsDelete!=true, ct)) return RecipeEditError(model.RecipeVersionId, "Bu malzeme reçetede zaten mevcut.");
        if (!await _context.PrdMaterials.AnyAsync(x=>x.ID==model.MaterialId && x.IsDelete!=true,ct) || !await _context.PrdUnits.AnyAsync(x=>x.ID==model.UnitId && x.IsDelete!=true,ct)) return RecipeEditError(model.RecipeVersionId,"Malzeme veya birim bulunamadı.");
        var sequence=(await _context.PrdRecipeItems.Where(x=>x.RecipeVersionId==model.RecipeVersionId && x.IsDelete!=true).MaxAsync(x=>(int?)x.Sequence,ct) ?? 0)+1;
        _context.PrdRecipeItems.Add(new PrdRecipeItem { RecipeVersionId=model.RecipeVersionId,MaterialId=model.MaterialId,Quantity=model.Quantity,UnitId=model.UnitId,PlannedWasteRate=model.PlannedWasteRate,Sequence=sequence,IsRequired=model.IsRequired,AlternativeGroupCode=model.AlternativeGroupCode,Notes=model.Notes,IsActive=true,IsDelete=false,CreateDate=DateTime.Now,CreateUserID=User.Identity?.Name });
        await _context.SaveChangesAsync(ct); TempData["success"]="Reçete kalemi eklendi."; return RedirectToAction(nameof(Details),new{id=model.RecipeVersionId});
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(ProductionRecipeItemEditVM model, [FromForm(Name = "Quantity")] string quantityText, [FromForm(Name = "PlannedWasteRate")] string wasteRateText, CancellationToken ct)
    {
        if(!TryParseProductionDecimal(quantityText,out var quantity)||!TryParseProductionDecimal(wasteRateText,out var wasteRate))return RecipeEditError(model.RecipeVersionId,"Miktar veya fire oranı geçerli bir sayı değildir.");
        model.Quantity=quantity;model.PlannedWasteRate=wasteRate;
        if (await DraftVersion(model.RecipeVersionId,ct)==null) return RecipeEditError(model.RecipeVersionId,"Reçete taslak değil.");
        var item=await _context.PrdRecipeItems.FirstOrDefaultAsync(x=>x.ID==model.ItemId && x.RecipeVersionId==model.RecipeVersionId && x.IsDelete!=true,ct); if(item==null)return NotFound();
        if(model.Quantity<=0 || model.PlannedWasteRate<0 || model.PlannedWasteRate>100)return RecipeEditError(model.RecipeVersionId,"Miktar veya fire oranı geçersiz.");
        if (!await _context.PrdUnits.AnyAsync(x => x.ID == model.UnitId && x.IsDelete != true, ct)) return RecipeEditError(model.RecipeVersionId, "Birim bulunamadı.");
        item.Quantity=model.Quantity;item.UnitId=model.UnitId;item.PlannedWasteRate=model.PlannedWasteRate;item.IsRequired=model.IsRequired;item.AlternativeGroupCode=model.AlternativeGroupCode;item.UpdateDate=DateTime.Now;item.UpdateUserID=User.Identity?.Name;
        await _context.SaveChangesAsync(ct);TempData["success"]="Reçete kalemi güncellendi.";return RedirectToAction(nameof(Details),new{id=model.RecipeVersionId});
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(int itemId,int recipeVersionId,CancellationToken ct)
    {
        if(await DraftVersion(recipeVersionId,ct)==null)return RecipeEditError(recipeVersionId,"Reçete taslak değil.");
        var item=await _context.PrdRecipeItems.FirstOrDefaultAsync(x=>x.ID==itemId && x.RecipeVersionId==recipeVersionId && x.IsDelete!=true,ct);if(item==null)return NotFound();
        item.IsDelete=true;item.DeleteDate=DateTime.Now;item.DeleteUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);TempData["success"]="Reçete kalemi silindi.";return RedirectToAction(nameof(Details),new{id=recipeVersionId});
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateNewVersion(int sourceVersionId,CancellationToken ct)
    {
        var source=await _context.PrdRecipeVersions.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==sourceVersionId&&x.IsDelete!=true,ct);
        if(source==null)return NotFound();
        var nextVersion=(await _context.PrdRecipeVersions.Where(x=>x.RecipeId==source.RecipeId&&x.IsDelete!=true).MaxAsync(x=>(int?)x.VersionNumber,ct)??0)+1;
        var now=DateTime.Now;var user=User.Identity?.Name;
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        var newVersion=new PrdRecipeVersion{RecipeId=source.RecipeId,VersionNumber=nextVersion,BaseQuantity=source.BaseQuantity,UnitId=source.UnitId,Status=PrdRecipeStatus.Draft,Notes=$"v{source.VersionNumber} versiyonundan oluşturuldu",IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user};
        _context.PrdRecipeVersions.Add(newVersion);await _context.SaveChangesAsync(ct);
        var sourceItems=await _context.PrdRecipeItems.AsNoTracking().Where(x=>x.RecipeVersionId==sourceVersionId&&x.IsDelete!=true).OrderBy(x=>x.Sequence).ToListAsync(ct);
        _context.PrdRecipeItems.AddRange(sourceItems.Select(x=>new PrdRecipeItem{RecipeVersionId=newVersion.ID,MaterialId=x.MaterialId,Quantity=x.Quantity,UnitId=x.UnitId,PlannedWasteRate=x.PlannedWasteRate,Sequence=x.Sequence,IsRequired=x.IsRequired,AlternativeGroupCode=x.AlternativeGroupCode,Notes=x.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=user}));
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
        TempData["success"]=$"v{nextVersion} taslak versiyonu oluşturuldu.";
        return RedirectToAction(nameof(Details),new{id=newVersion.ID});
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReopenAsDraft(int recipeVersionId,CancellationToken ct)
    {
        var version=await _context.PrdRecipeVersions.FirstOrDefaultAsync(x=>x.ID==recipeVersionId&&x.IsDelete!=true,ct);
        if(version==null)return NotFound();
        if(version.Status==PrdRecipeStatus.Draft)return RedirectToAction(nameof(Details),new{id=recipeVersionId});
        if(await _context.PrdProductionOrders.AnyAsync(x=>x.RecipeVersionId==recipeVersionId&&x.IsDelete!=true,ct))
            return RecipeEditError(recipeVersionId,"Bu reçete versiyonuna bağlı üretim emri bulunduğu için taslağa alınamaz. Yeni versiyon oluşturunuz.");
        version.Status=PrdRecipeStatus.Draft;version.ApprovedDate=null;version.ApprovedUserId=null;version.IsActive=true;
        version.UpdateDate=DateTime.Now;version.UpdateUserID=User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"]="Reçete versiyonu yönetici yetkisiyle yeniden taslağa alındı.";
        return RedirectToAction(nameof(Details),new{id=recipeVersionId});
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteVersion(int recipeVersionId,CancellationToken ct)
    {
        var version=await _context.PrdRecipeVersions.FirstOrDefaultAsync(x=>x.ID==recipeVersionId&&x.IsDelete!=true,ct);
        if(version==null)return NotFound();
        var recipe=await _context.PrdRecipes.FirstOrDefaultAsync(x=>x.ID==version.RecipeId&&x.IsDelete!=true,ct);
        if(recipe==null)return NotFound();
        var isUsed=await _context.PrdProductionPlans.AnyAsync(x=>x.RecipeVersionId==recipeVersionId&&x.IsDelete!=true,ct)
            ||await _context.PrdProductionOrders.AnyAsync(x=>x.RecipeVersionId==recipeVersionId&&x.IsDelete!=true,ct);
        if(isUsed){TempData["error"]="Bu reçete versiyonu üretim planı veya üretim emrinde kullanıldığı için silinemez. Gerekiyorsa pasife alınız.";return RedirectToAction(nameof(Details),new{id=recipeVersionId});}
        var now=DateTime.Now;var user=User.Identity?.Name;
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);
        var items=await _context.PrdRecipeItems.Where(x=>x.RecipeVersionId==recipeVersionId&&x.IsDelete!=true).ToListAsync(ct);
        foreach(var item in items){item.IsDelete=true;item.IsActive=false;item.DeleteDate=now;item.DeleteUserID=user;}
        version.IsDelete=true;version.IsActive=false;version.DeleteDate=now;version.DeleteUserID=user;
        var hasAnotherVersion=await _context.PrdRecipeVersions.AnyAsync(x=>x.RecipeId==recipe.ID&&x.ID!=recipeVersionId&&x.IsDelete!=true,ct);
        if(!hasAnotherVersion){recipe.IsDelete=true;recipe.IsActive=false;recipe.DeleteDate=now;recipe.DeleteUserID=user;}
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);
        TempData["success"]=hasAnotherVersion?"Seçili reçete versiyonu ve kalemleri silindi.":"Reçetenin son versiyonu ve reçete başlığı silindi.";
        return RedirectToAction(nameof(Index));
    }

    private Task<PrdRecipeVersion?> DraftVersion(int id,CancellationToken ct)=>_context.PrdRecipeVersions.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true&&x.Status==PrdRecipeStatus.Draft,ct);
    private IActionResult RecipeEditError(int id,string message){TempData["error"]=message;return RedirectToAction(nameof(Details),new{id});}
    private static bool TryParseProductionDecimal(string? value,out decimal result)
    {
        result=0;if(string.IsNullOrWhiteSpace(value))return false;
        var normalized=value.Trim().Replace(" ",string.Empty);
        if(normalized.Contains(',')&&normalized.Contains('.'))normalized=normalized.LastIndexOf(',')>normalized.LastIndexOf('.')?normalized.Replace(".",string.Empty).Replace(',','.'):normalized.Replace(",",string.Empty);
        else if(normalized.Contains(','))normalized=normalized.Replace(',','.');
        return decimal.TryParse(normalized,NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out result);
    }

    [HttpGet]
    public IActionResult Import() { ViewBag.Modul = "YeniUretim"; return View(); }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewImport(IFormFile file, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        if (file == null || file.Length == 0 || !string.Equals(Path.GetExtension(file.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase) || file.Length > 10 * 1024 * 1024)
        { ModelState.AddModelError("", "En fazla 10 MB boyutunda bir .xlsx dosyası seçiniz."); return View("Import"); }
        List<ProductionRecipeImportRow> rows;
        try { rows = ReadImportRows(file); }
        catch (Exception ex) { ModelState.AddModelError("", ex.Message); return View("Import"); }

        var allCodes = rows.SelectMany(x => new[] { x.ProductCode, x.ComponentCode }).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var materialCodes = await _context.PrdMaterials.AsNoTracking().Where(x => allCodes.Contains(x.Code)).Select(x => x.Code).ToListAsync(ct);
        var recipeCodes = rows.Select(x => "REC-" + x.ProductCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var existingRecipes = await _context.PrdRecipes.AsNoTracking().Where(x => recipeCodes.Contains(x.Code) && x.IsDelete != true).Select(x => x.Code).ToListAsync(ct);
        var missing = allCodes.Except(materialCodes, StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        HttpContext.Session.SetString(ImportSessionKey, JsonSerializer.Serialize(rows));
        return View("ImportPreview", new ProductionRecipeImportPreviewVM { RowCount = rows.Count, RecipeCount = rows.Select(x => x.ProductCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(), ComponentCount = rows.Select(x => x.ComponentCode).Distinct(StringComparer.OrdinalIgnoreCase).Count(), MissingMaterialCount = missing.Count, MissingMaterialCodes = missing, ExistingRecipeCount = existingRecipes.Count, ExistingRecipeCodes = existingRecipes, SampleRows = rows.Take(20).ToList() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmImport(CancellationToken ct)
    {
        var json = HttpContext.Session.GetString(ImportSessionKey);
        List<ProductionRecipeImportRow> rows = string.IsNullOrWhiteSpace(json)
            ? []
            : JsonSerializer.Deserialize<List<ProductionRecipeImportRow>>(json) ?? [];
        if (rows.Count == 0) { TempData["error"] = "Önizleme bulunamadı; dosyayı yeniden yükleyiniz."; return RedirectToAction(nameof(Import)); }
        var now = DateTime.Now; var userId = User.Identity?.Name;
        await using var tx = await _context.Database.BeginTransactionAsync(ct);

        var units = await _context.PrdUnits.ToListAsync(ct);
        var unitByCode = units.ToDictionary(x => Normalize(x.Code), StringComparer.OrdinalIgnoreCase); var unitAdded = 0;
        foreach (var source in rows.GroupBy(x => Normalize(x.UnitCode)).Select(x => x.First()))
            if (!unitByCode.ContainsKey(Normalize(source.UnitCode))) { var u = NewUnit(source.UnitCode, source.UnitName, now, userId); _context.PrdUnits.Add(u); unitByCode[Normalize(u.Code)] = u; unitAdded++; }
        if (!unitByCode.ContainsKey("ADET")) { var u = NewUnit("ADET", "Adet", now, userId); _context.PrdUnits.Add(u); unitByCode[u.Code] = u; unitAdded++; }
        await _context.SaveChangesAsync(ct);

        var materials = await _context.PrdMaterials.ToListAsync(ct);
        var materialByCode = materials.ToDictionary(x => Normalize(x.Code), StringComparer.OrdinalIgnoreCase); var materialAdded = 0;
        var sources = rows.SelectMany(x => new[] { new { Code=x.ProductCode, Name=x.ProductName, Product=true, Unit="ADET" }, new { Code=x.ComponentCode, Name=x.ComponentName, Product=false, Unit=x.UnitCode } }).GroupBy(x => Normalize(x.Code)).Select(x => x.First());
        foreach (var source in sources)
        {
            var code = Normalize(source.Code); if (materialByCode.ContainsKey(code)) continue;
            var m = new PrdMaterial { Code=code, Name=source.Name.Trim(), Source=PrdMaterialSource.QuickCode, Type=source.Product ? PrdMaterialType.FinishedProduct : DetectMaterialType(code), UnitId=unitByCode[Normalize(source.Unit)].ID, LogoActive=false, IsActive=true, IsDelete=false, CreateDate=now, CreateUserID=userId };
            _context.PrdMaterials.Add(m); materialByCode[code]=m; materialAdded++;
        }
        await _context.SaveChangesAsync(ct);

        var recipeByCode = (await _context.PrdRecipes.Where(x => x.IsDelete != true).ToListAsync(ct)).ToDictionary(x => Normalize(x.Code), StringComparer.OrdinalIgnoreCase);
        var recipeAdded=0; var recipeSkipped=0; var itemAdded=0;
        foreach (var group in rows.GroupBy(x => Normalize(x.ProductCode)))
        {
            var recipeCode="REC-"+group.Key; if (recipeByCode.ContainsKey(recipeCode)) { recipeSkipped++; continue; }
            var first=group.First(); var product=materialByCode[group.Key];
            var recipe=new PrdRecipe { Code=recipeCode, Name=first.ProductName.Trim(), ProductMaterialId=product.ID, IsActive=true, IsDelete=false, CreateDate=now, CreateUserID=userId };
            _context.PrdRecipes.Add(recipe); await _context.SaveChangesAsync(ct);
            var version=new PrdRecipeVersion { RecipeId=recipe.ID, VersionNumber=1, BaseQuantity=1m, UnitId=product.UnitId, Status=PrdRecipeStatus.Draft, Notes="Excel reçete aktarımı", IsActive=true, IsDelete=false, CreateDate=now, CreateUserID=userId };
            _context.PrdRecipeVersions.Add(version); await _context.SaveChangesAsync(ct);
            var sequence=1;
            foreach (var row in group) { _context.PrdRecipeItems.Add(new PrdRecipeItem { RecipeVersionId=version.ID, MaterialId=materialByCode[Normalize(row.ComponentCode)].ID, Quantity=row.Quantity, UnitId=unitByCode[Normalize(row.UnitCode)].ID, Sequence=sequence++, IsRequired=true, PlannedWasteRate=0m, IsActive=true, IsDelete=false, CreateDate=now, CreateUserID=userId }); itemAdded++; }
            recipeByCode[recipeCode]=recipe; recipeAdded++;
        }
        await _context.SaveChangesAsync(ct); await tx.CommitAsync(ct); HttpContext.Session.Remove(ImportSessionKey);
        TempData["success"]=$"{recipeAdded} reçete ve {itemAdded} kalem oluşturuldu. {recipeSkipped} mevcut reçete atlandı; {materialAdded} eksik malzeme ve {unitAdded} birim eklendi.";
        return RedirectToAction(nameof(Index));
    }

    private static List<ProductionRecipeImportRow> ReadImportRows(IFormFile file)
    {
        using var wb=new XLWorkbook(file.OpenReadStream()); var ws=wb.Worksheets.First(); var last=ws.LastRowUsed()?.RowNumber() ?? 0;
        if (last < 2 || !ws.Cell(1,1).GetString().Contains("Ana Malzmeme Kod", StringComparison.OrdinalIgnoreCase) || !ws.Cell(1,4).GetString().Contains("Alt Malzmeme Kod", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Excel sütun yapısı reçete örneğiyle uyuşmuyor.");
        var rows=new List<ProductionRecipeImportRow>(); var keys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var n=2;n<=last;n++) { var p=Normalize(ws.Cell(n,1).GetString()); var c=Normalize(ws.Cell(n,4).GetString()); var u=Normalize(ws.Cell(n,9).GetString()); if (p=="" && c=="") continue; if(p==""||c==""||u==""||!ws.Cell(n,7).TryGetValue<decimal>(out var q)||q<=0) throw new InvalidOperationException($"{n}. satırda kod, miktar veya birim geçersiz."); if(!keys.Add($"{p}|{c}|{u}")) throw new InvalidOperationException($"{n}. satırda yinelenen reçete kalemi var."); rows.Add(new() { RowNumber=n, ProductCode=p, ProductName=ws.Cell(n,2).GetString().Trim(), ComponentCode=c, ComponentName=ws.Cell(n,5).GetString().Trim(), Quantity=q, UnitCode=u, UnitName=ws.Cell(n,10).GetString().Trim() }); }
        return rows;
    }
    private static PrdUnit NewUnit(string code,string name,DateTime now,string? userId)=>new(){Code=Normalize(code),Name=string.IsNullOrWhiteSpace(name)?Normalize(code):name.Trim(),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=userId};
    private static string Normalize(string? value)=>(value??"").Trim().ToUpperInvariant();
    private static PrdMaterialType DetectMaterialType(string code)=>code.Contains("YM",StringComparison.OrdinalIgnoreCase)?PrdMaterialType.SemiFinished:code.Contains("MM",StringComparison.OrdinalIgnoreCase)?PrdMaterialType.FinishedProduct:code.Contains("HM",StringComparison.OrdinalIgnoreCase)?PrdMaterialType.RawMaterial:PrdMaterialType.Other;

    private async Task FillSelections(ProductionRecipeCreateVM model, CancellationToken cancellationToken)
    {
        model.Products = await _context.PrdMaterials.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive != false)
            .OrderBy(x => x.Code).Select(x => new SelectListItem(x.Code + " - " + x.Name, x.ID.ToString()))
            .ToListAsync(cancellationToken);
        model.Units = await _context.PrdUnits.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive != false)
            .OrderBy(x => x.Name).Select(x => new SelectListItem(x.Name, x.ID.ToString()))
            .ToListAsync(cancellationToken);
    }
}
