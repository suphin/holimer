using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class ReceteMaliyetleriController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReceteMaliyetleriController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        return View(await BuildCalculationModelAsync(ct));
    }

    private async Task<RecipeCostCalculationVM> BuildCalculationModelAsync(CancellationToken ct, DateTime? calculationDate = null, IReadOnlyCollection<int>? requestedVersionIds = null)
    {
        var effectiveCalculationDate = calculationDate ?? DateTime.Now;
        var recipeRows = await (
            from version in _context.PrdRecipeVersions.AsNoTracking()
            join recipe in _context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID
            join product in _context.PrdMaterials.AsNoTracking() on recipe.ProductMaterialId equals product.ID
            join unit in _context.PrdUnits.AsNoTracking() on version.UnitId equals unit.ID
            where version.IsDelete != true && recipe.IsDelete != true && product.IsDelete != true
            select new
            {
                Version = version,
                RecipeCode = recipe.Code,
                ProductCode = product.Code,
                ProductName = product.Name,
                UnitCode = unit.Code,
                UnitName = unit.Name
            })
            .ToListAsync(ct);

        if (requestedVersionIds is { Count: > 0 })
        {
            recipeRows = recipeRows.Where(x => requestedVersionIds.Contains(x.Version.ID)).OrderBy(x => x.ProductCode).ToList();
        }
        else
        {
            // Her reçeteyi bir kez göster; durumundan bağımsız olarak son versiyonu maliyetlendirmeye al.
            recipeRows = recipeRows
                .GroupBy(x => x.Version.RecipeId)
                .Select(x => x.OrderByDescending(y => y.Version.VersionNumber).ThenByDescending(y => y.Version.ID).First())
                .OrderBy(x => x.ProductCode)
                .ToList();
        }

        var versionIds = recipeRows.Select(x => x.Version.ID).ToList();
        var items = versionIds.Count == 0
            ? []
            : await (
                from item in _context.PrdRecipeItems.AsNoTracking()
                join material in _context.PrdMaterials.AsNoTracking() on item.MaterialId equals material.ID
                join unit in _context.PrdUnits.AsNoTracking() on item.UnitId equals unit.ID
                where versionIds.Contains(item.RecipeVersionId) && item.IsDelete != true && material.IsDelete != true
                select new RecipeItemCostSource
                {
                    RecipeVersionId = item.RecipeVersionId,
                    MaterialId = material.ID,
                    MaterialCode = material.Code,
                    MaterialName = material.Name,
                    MaterialType = material.Type,
                    Quantity = item.Quantity,
                    PlannedWasteRate = item.PlannedWasteRate,
                    UnitId = unit.ID,
                    UnitCode = unit.Code,
                    UnitName = unit.Name
                })
                .ToListAsync(ct);

        var componentCodes = items.Select(x => x.MaterialCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var movements = componentCodes.Count == 0
            ? []
            : await (
                from movement in _context.PrdStockMovements.AsNoTracking()
                join material in _context.PrdMaterials.AsNoTracking() on movement.MaterialId equals material.ID
                join unit in _context.PrdUnits.AsNoTracking() on movement.UnitId equals unit.ID
                where componentCodes.Contains(material.Code) && movement.IsDelete != true && material.IsDelete != true &&
                       movement.Direction == PrdStockDirection.In && movement.MovementType != PrdStockMovementType.Transfer &&
                       movement.UnitCost > 0 && movement.MovementDate <= effectiveCalculationDate
                select new StockCostSource
                {
                    Id = movement.ID,
                    MaterialCode = material.Code,
                    UnitId = movement.UnitId,
                    UnitCode = unit.Code,
                    UnitName = unit.Name,
                    UnitCost = movement.UnitCost,
                    MovementDate = movement.MovementDate
                })
                .ToListAsync(ct);

        var stockCosts = BuildStockCosts(movements);
        var componentIds = items.Select(x => x.MaterialId).Distinct().ToList();
        var referenceVersions = componentIds.Count == 0
            ? []
            : await (
                from cost in _context.PrdMaterialReferenceCosts.AsNoTracking()
                join unit in _context.PrdUnits.AsNoTracking() on cost.UnitId equals unit.ID
                where componentIds.Contains(cost.MaterialId) && cost.IsDelete != true &&
                       cost.UnitCostTry > 0 && cost.ValidFrom <= effectiveCalculationDate.Date &&
                       (cost.ValidTo == null || cost.ValidTo >= effectiveCalculationDate.Date)
                orderby cost.ValidFrom descending, cost.VersionNumber descending
                select new ReferenceCostSource
                {
                    MaterialId = cost.MaterialId,
                    UnitId = cost.UnitId,
                    UnitCode = unit.Code,
                    UnitName = unit.Name,
                    UnitCostTry = cost.UnitCostTry,
                    VersionNumber = cost.VersionNumber,
                    ValidFrom = cost.ValidFrom
                }).ToListAsync(ct);
        var referenceCosts = referenceVersions
            .GroupBy(x => x.MaterialId)
            .ToDictionary(x => x.Key, x => x.First());
        var fallbackVersions = componentCodes.Count == 0
            ? []
            : await _context.RptProductCostVersions.AsNoTracking()
                .Where(x => componentCodes.Contains(x.ProductCode) && x.IsDelete != true &&
                             x.TotalUnitCostTry > 0 &&
                             x.ValidFrom <= effectiveCalculationDate.Date &&
                             (x.ValidTo == null || x.ValidTo >= effectiveCalculationDate.Date))
                .OrderByDescending(x => x.ValidFrom)
                .ThenByDescending(x => x.VersionNumber)
                .ToListAsync(ct);
        var fallbackCosts = fallbackVersions
            .GroupBy(x => x.ProductCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
        var conversionRules = await (
            from conversion in _context.PrdUnitConversions.AsNoTracking()
            join fromUnit in _context.PrdUnits.AsNoTracking() on conversion.FromUnitId equals fromUnit.ID
            join toUnit in _context.PrdUnits.AsNoTracking() on conversion.ToUnitId equals toUnit.ID
            where conversion.IsDelete != true && conversion.IsActive != false && conversion.Factor > 0
            select new UnitConversionRule
            {
                MaterialId = conversion.MaterialId,
                FromUnitId = conversion.FromUnitId,
                FromCode = fromUnit.Code,
                FromName = fromUnit.Name,
                ToUnitId = conversion.ToUnitId,
                ToCode = toUnit.Code,
                ToName = toUnit.Name,
                Factor = conversion.Factor
            }).ToListAsync(ct);

        var model = new RecipeCostCalculationVM
        {
            CalculationDate = effectiveCalculationDate,
            ScenarioName = $"Maliyet Çalışması {effectiveCalculationDate:dd.MM.yyyy HH:mm}"
        };
        foreach (var recipe in recipeRows)
        {
            var row = new RecipeCostCalculationRowVM
            {
                RecipeVersionId = recipe.Version.ID,
                RecipeCode = recipe.RecipeCode,
                VersionNumber = recipe.Version.VersionNumber,
                RecipeStatus = recipe.Version.Status,
                ProductCode = recipe.ProductCode,
                ProductName = recipe.ProductName,
                ProductionUnit = ProductionUnitNormalizer.DisplayName(recipe.UnitCode, recipe.UnitName),
                BaseQuantity = recipe.Version.BaseQuantity
            };

            foreach (var item in items.Where(x => x.RecipeVersionId == recipe.Version.ID))
            {
                var isPackaging = IsPackaging(item.MaterialType, item.MaterialCode);
                if (isPackaging) row.PackagingCount++; else row.RawMaterialCount++;

                decimal? itemCost = null;
                var resolvedSource = RecipeCostSource.None;
                var costQuantity = item.Quantity * (1m + item.PlannedWasteRate / 100m);
                decimal? pricedQuantity = null;
                decimal? selectedUnitCost = null;
                var recipeUnit = ProductionUnitNormalizer.DisplayName(item.UnitCode, item.UnitName);
                var costUnit = string.Empty;
                var costSource = string.Empty;
                var costSourceDetail = string.Empty;
                var missingReason = string.Empty;
                if (stockCosts.TryGetValue(item.MaterialCode, out var stockCost))
                {
                    if (TryConvertQuantity(costQuantity, item.MaterialId, conversionRules,
                        item.UnitId, item.UnitCode, item.UnitName,
                        stockCost.UnitId, stockCost.UnitCode, stockCost.UnitName, out var stockQuantity))
                    {
                        itemCost = stockQuantity * stockCost.UnitCost;
                        pricedQuantity = stockQuantity;
                        selectedUnitCost = stockCost.UnitCost;
                        costUnit = ProductionUnitNormalizer.DisplayName(stockCost.UnitCode, stockCost.UnitName);
                        costSource = "Gerçek stok girişi";
                        costSourceDetail = $"{stockCost.MovementDate:dd.MM.yyyy} tarihli son maliyetli depo girişi";
                        resolvedSource = RecipeCostSource.Stock;
                    }
                    else
                    {
                        missingReason = $"Reçete birimi ({ProductionUnitNormalizer.DisplayName(item.UnitCode, item.UnitName)}) ile son depo giriş birimi ({ProductionUnitNormalizer.DisplayName(stockCost.UnitCode, stockCost.UnitName)}) dönüştürülemiyor.";
                    }
                }
                else
                {
                    missingReason = "Transfer hariç, birim maliyeti sıfırdan büyük bir depo giriş kaydı bulunamadı.";
                }

                if (!itemCost.HasValue && referenceCosts.TryGetValue(item.MaterialId, out var referenceCost))
                {
                    if (TryConvertQuantity(costQuantity, item.MaterialId, conversionRules,
                            item.UnitId, item.UnitCode, item.UnitName,
                            referenceCost.UnitId, referenceCost.UnitCode, referenceCost.UnitName, out var referenceQuantity))
                    {
                        itemCost = referenceQuantity * referenceCost.UnitCostTry;
                        pricedQuantity = referenceQuantity;
                        selectedUnitCost = referenceCost.UnitCostTry;
                        costUnit = ProductionUnitNormalizer.DisplayName(referenceCost.UnitCode, referenceCost.UnitName);
                        costSource = $"Referans maliyet v{referenceCost.VersionNumber}";
                        costSourceDetail = $"{referenceCost.ValidFrom:dd.MM.yyyy} tarihinden itibaren geçerli";
                        resolvedSource = RecipeCostSource.Reference;
                    }
                    else
                    {
                        missingReason += $" Referans maliyet v{referenceCost.VersionNumber} birimi ({ProductionUnitNormalizer.DisplayName(referenceCost.UnitCode, referenceCost.UnitName)}) reçete birimine dönüştürülemiyor.";
                    }
                }

                if (!itemCost.HasValue && fallbackCosts.TryGetValue(item.MaterialCode, out var fallback))
                {
                    if (TryConvertQuantity(costQuantity, item.MaterialId, conversionRules,
                             item.UnitId, item.UnitCode, item.UnitName,
                             null, fallback.UnitCode, fallback.UnitCode, out var fallbackQuantity))
                    {
                        itemCost = fallbackQuantity * fallback.TotalUnitCostTry;
                        pricedQuantity = fallbackQuantity;
                        selectedUnitCost = fallback.TotalUnitCostTry;
                        costUnit = ProductionUnitNormalizer.DisplayName(fallback.UnitCode, fallback.UnitCode);
                        costSource = $"Eski maliyet kartı v{fallback.VersionNumber}";
                        costSourceDetail = $"{fallback.ValidFrom:dd.MM.yyyy} tarihinden itibaren geçerli";
                        resolvedSource = RecipeCostSource.Legacy;
                    }
                    else
                    {
                        missingReason += $" Güncel maliyet kartının birimi ({fallback.UnitCode}) de reçete birimine dönüştürülemiyor.";
                    }
                }

                if (!itemCost.HasValue)
                {
                    row.CostItems.Add(new RecipeCostItemDetailVM
                    {
                        MaterialId = item.MaterialId,
                        MaterialCode = item.MaterialCode,
                        MaterialName = item.MaterialName,
                        MaterialType = isPackaging ? "Ambalaj" : "Hammadde / diğer",
                        RecipeQuantity = item.Quantity,
                        PlannedWasteRate = item.PlannedWasteRate,
                        CostQuantity = costQuantity,
                        RecipeUnit = recipeUnit,
                        ItemCost = 0m,
                        ProductUnitCostContribution = 0m,
                        CostSource = "Eksik maliyet",
                        IncludedInCalculation = false,
                        Reason = missingReason
                    });
                    row.MissingCosts.Add(new RecipeMissingCostVM
                    {
                        MaterialId = item.MaterialId,
                        MaterialCode = item.MaterialCode,
                        MaterialName = item.MaterialName,
                        MaterialType = isPackaging ? "Ambalaj" : "Hammadde / diğer",
                        RecipeQuantity = item.Quantity,
                        PlannedWasteRate = item.PlannedWasteRate,
                        CostQuantity = costQuantity,
                        Unit = recipeUnit,
                        Reason = missingReason
                    });
                    continue;
                }

                if (resolvedSource == RecipeCostSource.Stock) row.StockCostItemCount++;
                else if (resolvedSource == RecipeCostSource.Reference) row.ReferenceCostItemCount++;
                else if (resolvedSource == RecipeCostSource.Legacy) row.LegacyCostItemCount++;

                var unitCost = recipe.Version.BaseQuantity > 0 ? itemCost.Value / recipe.Version.BaseQuantity : 0m;
                row.CostItems.Add(new RecipeCostItemDetailVM
                {
                    MaterialId = item.MaterialId,
                    MaterialCode = item.MaterialCode,
                    MaterialName = item.MaterialName,
                    MaterialType = isPackaging ? "Ambalaj" : "Hammadde / diğer",
                    RecipeQuantity = item.Quantity,
                    PlannedWasteRate = item.PlannedWasteRate,
                    CostQuantity = costQuantity,
                    RecipeUnit = recipeUnit,
                    PricedQuantity = pricedQuantity,
                    CostUnit = costUnit,
                    UnitCostTry = selectedUnitCost,
                    ItemCost = itemCost.Value,
                    ProductUnitCostContribution = unitCost,
                    CostSource = costSource,
                    CostSourceDetail = costSourceDetail,
                    IncludedInCalculation = true
                });
                if (isPackaging) row.PackagingUnitCost += unitCost;
                else row.RawMaterialUnitCost += unitCost;
            }

            model.Rows.Add(row);
        }

        return model;
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Kaydet(RecipeCostScenarioSaveVM input, CancellationToken ct)
    {
        var name = Clean(input.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["error"] = "Maliyet çalışması için bir ad giriniz.";
            return RedirectToAction(nameof(Index));
        }
        if (!TryParseOptionalDecimal(input.FixedCost, out var fixedCost) || fixedCost < 0 ||
            !TryParseOptionalDecimal(input.VariableCost, out var variableCost) || variableCost < 0 ||
            !TryParseOptionalDecimal(input.DefaultProductionQuantity, out var defaultQuantity) || defaultQuantity < 0)
        {
            TempData["error"] = "Üretim miktarı ve gider alanları geçerli, sıfırdan küçük olmayan sayılar olmalıdır.";
            return RedirectToAction(nameof(Index));
        }

        var quantities = new Dictionary<int, decimal>();
        foreach (var line in input.Lines)
        {
            if (!TryParseOptionalDecimal(line.ProductionQuantity, out var quantity) || quantity < 0)
            {
                TempData["error"] = "Ürün üretim miktarlarından biri geçersiz.";
                return RedirectToAction(nameof(Index));
            }
            if (quantity > 0) quantities[line.RecipeVersionId] = quantity;
        }
        if (quantities.Count == 0)
        {
            TempData["error"] = "Kaydetmek için en az bir ürünün üretim miktarını giriniz.";
            return RedirectToAction(nameof(Index));
        }

        var calculation = await BuildCalculationModelAsync(ct);
        var selectedRows = calculation.Rows.Where(x => quantities.ContainsKey(x.RecipeVersionId)).ToList();
        var totalQuantity = selectedRows.Sum(x => quantities[x.RecipeVersionId]);
        if (selectedRows.Count == 0 || totalQuantity <= 0)
        {
            TempData["error"] = "Seçilen reçeteler artık bulunamadı.";
            return RedirectToAction(nameof(Index));
        }

        var now = DateTime.Now;
        var user = User.Identity?.Name;
        var scenario = new PrdRecipeCostScenario
        {
            ScenarioNumber = $"RM-{now:yyyyMMddHHmmssfff}",
            Name = name,
            CalculationDate = now,
            DefaultProductionQuantity = defaultQuantity,
            FixedCost = fixedCost,
            VariableCost = variableCost,
            TotalProductionQuantity = totalQuantity,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = user
        };

        await using var transaction = await _context.Database.BeginTransactionAsync(ct);
        _context.PrdRecipeCostScenarios.Add(scenario);
        await _context.SaveChangesAsync(ct);
        foreach (var row in selectedRows)
        {
            var quantity = quantities[row.RecipeVersionId];
            var ratio = quantity / totalQuantity;
            var recipeMaterialCost = row.RecipeUnitCost * quantity;
            var variableShare = variableCost * ratio;
            var fixedShare = fixedCost * ratio;
            var totalCost = recipeMaterialCost + variableShare + fixedShare;
            _context.PrdRecipeCostScenarioLines.Add(new PrdRecipeCostScenarioLine
            {
                ScenarioId = scenario.ID,
                RecipeVersionId = row.RecipeVersionId,
                ProductCode = row.ProductCode,
                ProductName = row.ProductName,
                ProductionUnit = row.ProductionUnit,
                ProductionQuantity = quantity,
                RawMaterialUnitCost = row.RawMaterialUnitCost,
                PackagingUnitCost = row.PackagingUnitCost,
                RecipeMaterialCost = recipeMaterialCost,
                VariableCostShare = variableShare,
                FixedCostShare = fixedShare,
                TotalCost = totalCost,
                TotalUnitCost = totalCost / quantity,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            });
            scenario.TotalRecipeCost += recipeMaterialCost;
            scenario.TotalCost += totalCost;
        }
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{scenario.ScenarioNumber} numaralı maliyet çalışması kaydedildi.";
        return RedirectToAction(nameof(KayitDetay), new { id = scenario.ID });
    }

    [HttpGet]
    public async Task<IActionResult> KayitliHesaplar(CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var rows = await _context.PrdRecipeCostScenarios.AsNoTracking()
            .Where(x => x.IsDelete != true)
            .OrderByDescending(x => x.CalculationDate)
            .Select(x => new RecipeCostScenarioListRowVM
            {
                Id = x.ID,
                ScenarioNumber = x.ScenarioNumber,
                Name = x.Name,
                CalculationDate = x.CalculationDate,
                TotalProductionQuantity = x.TotalProductionQuantity,
                TotalRecipeCost = x.TotalRecipeCost,
                FixedCost = x.FixedCost,
                VariableCost = x.VariableCost,
                TotalCost = x.TotalCost,
                LineCount = _context.PrdRecipeCostScenarioLines.Count(y => y.ScenarioId == x.ID && y.IsDelete != true),
                CreatedUser = x.CreateUserID ?? "-"
            }).ToListAsync(ct);
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> KayitDetay(int id, CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        var model = await BuildScenarioDetailAsync(id, ct);
        return model == null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> KayitExcel(int id, CancellationToken ct)
    {
        var model = await BuildScenarioDetailAsync(id, ct);
        if (model == null) return NotFound();

        var versionIds = model.Lines.Select(x => x.RecipeVersionId).Distinct().ToArray();
        var calculation = await BuildCalculationModelAsync(ct, model.CalculationDate, versionIds);
        var calculatedRows = calculation.Rows.ToDictionary(x => x.RecipeVersionId);

        using var workbook = new XLWorkbook();
        workbook.Properties.Title = model.Name;
        workbook.Properties.Subject = "Reçete maliyet çalışması ve malzeme maliyetleri";

        var summarySheet = workbook.Worksheets.Add("Çalışma Özeti");
        summarySheet.ShowGridLines = false;
        summarySheet.Cell("A2").Value = model.Name;
        summarySheet.Range("A2:D2").Merge();
        summarySheet.Range("A2:D2").Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#1F4E78"));
        summarySheet.Range("A3:D3").Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        summarySheet.Range("A3:D3").Style.Border.BottomBorderColor = XLColor.FromHtml("#B4C6E7");
        var summaryValues = new (string Label, object Value)[]
        {
            ("Çalışma numarası", model.ScenarioNumber),
            ("Hesaplama tarihi", model.CalculationDate),
            ("Kaydeden", model.CreatedUser),
            ("Ürün sayısı", model.LineCount),
            ("Varsayılan üretim miktarı", model.DefaultProductionQuantity),
            ("Toplam üretim miktarı", model.TotalProductionQuantity),
            ("Reçete maliyeti (TRY)", model.TotalRecipeCost),
            ("Değişken gider (TRY)", model.VariableCost),
            ("Sabit gider (TRY)", model.FixedCost),
            ("Toplam gider (TRY)", model.TotalCost)
        };
        var summaryRow = 5;
        foreach (var entry in summaryValues)
        {
            summarySheet.Cell(summaryRow, 1).Value = entry.Label;
            summarySheet.Cell(summaryRow, 2).Value = XLCellValue.FromObject(entry.Value);
            summaryRow++;
        }
        summarySheet.Range(5, 1, summaryRow - 1, 1).Style.Font.SetBold();
        summarySheet.Range(5, 1, summaryRow - 1, 1).Style.Fill.SetBackgroundColor(XLColor.FromHtml("#D9EAF7"));
        summarySheet.Cell("B6").Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
        summarySheet.Range("B9:B10").Style.NumberFormat.Format = "#,##0.######";
        summarySheet.Range("B11:B14").Style.NumberFormat.Format = "#,##0.00";
        summarySheet.Cell("A16").Value = "Reçete malzeme maliyetleri, kayıtlı reçete versiyonu ve çalışmanın hesaplama tarihindeki uygun maliyet kaynakları esas alınarak hazırlanır.";
        summarySheet.Range("A16:D17").Merge().Style.Alignment.SetWrapText().Font.SetItalic().Font.SetFontColor(XLColor.FromHtml("#595959"));
        summarySheet.Columns("A:B").AdjustToContents();

        var productSheet = workbook.Worksheets.Add("Ürün Maliyetleri");
        productSheet.ShowGridLines = false;
        var productHeaders = new[] { "Ürün Kodu", "Ürün Adı", "Üretim Miktarı", "Üretim Birimi", "Hammadde Birim Maliyeti (TRY)", "Ambalaj Birim Maliyeti (TRY)", "Birim Reçete Maliyeti (TRY)", "Reçete Toplamı (TRY)", "Değişken Gider Payı (TRY)", "Sabit Gider Payı (TRY)", "Toplam Gider (TRY)", "Toplam Birim Maliyet (TRY)" };
        for (var column = 0; column < productHeaders.Length; column++) productSheet.Cell(1, column + 1).Value = productHeaders[column];
        StyleExcelHeader(productSheet.Range(1, 1, 1, productHeaders.Length));
        var productRow = 1;
        foreach (var line in model.Lines)
        {
            productRow++;
            productSheet.Cell(productRow, 1).Value = line.ProductCode;
            productSheet.Cell(productRow, 2).Value = line.ProductName;
            productSheet.Cell(productRow, 3).Value = line.ProductionQuantity;
            productSheet.Cell(productRow, 4).Value = line.ProductionUnit;
            productSheet.Cell(productRow, 5).Value = line.RawMaterialUnitCost;
            productSheet.Cell(productRow, 6).Value = line.PackagingUnitCost;
            productSheet.Cell(productRow, 7).Value = line.RawMaterialUnitCost + line.PackagingUnitCost;
            productSheet.Cell(productRow, 8).Value = line.RecipeMaterialCost;
            productSheet.Cell(productRow, 9).Value = line.VariableCostShare;
            productSheet.Cell(productRow, 10).Value = line.FixedCostShare;
            productSheet.Cell(productRow, 11).Value = line.TotalCost;
            productSheet.Cell(productRow, 12).Value = line.TotalUnitCost;
        }
        if (productRow > 1)
        {
            productSheet.Range(2, 3, productRow, 3).Style.NumberFormat.Format = "#,##0.######";
            productSheet.Range(2, 5, productRow, 7).Style.NumberFormat.Format = "#,##0.0000";
            productSheet.Range(2, 8, productRow, 12).Style.NumberFormat.Format = "#,##0.00";
            productSheet.Range(1, 1, productRow, productHeaders.Length).CreateTable();
        }
        productSheet.SheetView.FreezeRows(1);
        FitExcelColumns(productSheet);

        var materialSheet = workbook.Worksheets.Add("Reçete Malzemeleri");
        materialSheet.ShowGridLines = false;
        var materialHeaders = new[] { "Ürün Kodu", "Ürün Adı", "Reçete", "Versiyon", "Üretim Miktarı", "Üretim Birimi", "Malzeme Kodu", "Malzeme Adı", "Tür", "Reçete Miktarı", "Reçete Birimi", "Planlanan Fire (%)", "Maliyete Esas Miktar", "Maliyet Birimi", "Kullanılan Birim Fiyat (TRY)", "Birim Ürüne Maliyet (TRY)", "Toplam Üretime Maliyet (TRY)", "Maliyet Kaynağı", "Kaynak Detayı", "Hesaba Dahil", "Eksik Maliyet Nedeni" };
        for (var column = 0; column < materialHeaders.Length; column++) materialSheet.Cell(1, column + 1).Value = materialHeaders[column];
        StyleExcelHeader(materialSheet.Range(1, 1, 1, materialHeaders.Length));
        var materialRow = 1;
        foreach (var savedLine in model.Lines)
        {
            if (!calculatedRows.TryGetValue(savedLine.RecipeVersionId, out var recipeRow))
            {
                materialRow++;
                materialSheet.Cell(materialRow, 1).Value = savedLine.ProductCode;
                materialSheet.Cell(materialRow, 2).Value = savedLine.ProductName;
                materialSheet.Cell(materialRow, 5).Value = savedLine.ProductionQuantity;
                materialSheet.Cell(materialRow, 6).Value = savedLine.ProductionUnit;
                materialSheet.Cell(materialRow, 20).Value = "Hayır";
                materialSheet.Cell(materialRow, 21).Value = "Kayıtlı reçete versiyonu veya reçete kalemleri artık bulunamadı.";
                continue;
            }

            if (recipeRow.CostItems.Count == 0)
            {
                materialRow++;
                materialSheet.Cell(materialRow, 1).Value = savedLine.ProductCode;
                materialSheet.Cell(materialRow, 2).Value = savedLine.ProductName;
                materialSheet.Cell(materialRow, 3).Value = recipeRow.RecipeCode;
                materialSheet.Cell(materialRow, 4).Value = recipeRow.VersionNumber;
                materialSheet.Cell(materialRow, 5).Value = savedLine.ProductionQuantity;
                materialSheet.Cell(materialRow, 6).Value = savedLine.ProductionUnit;
                materialSheet.Cell(materialRow, 20).Value = "Hayır";
                materialSheet.Cell(materialRow, 21).Value = "Reçete versiyonunda maliyetlendirilecek malzeme kalemi bulunamadı.";
                continue;
            }

            foreach (var item in recipeRow.CostItems)
            {
                materialRow++;
                materialSheet.Cell(materialRow, 1).Value = savedLine.ProductCode;
                materialSheet.Cell(materialRow, 2).Value = savedLine.ProductName;
                materialSheet.Cell(materialRow, 3).Value = recipeRow.RecipeCode;
                materialSheet.Cell(materialRow, 4).Value = recipeRow.VersionNumber;
                materialSheet.Cell(materialRow, 5).Value = savedLine.ProductionQuantity;
                materialSheet.Cell(materialRow, 6).Value = savedLine.ProductionUnit;
                materialSheet.Cell(materialRow, 7).Value = item.MaterialCode;
                materialSheet.Cell(materialRow, 8).Value = item.MaterialName;
                materialSheet.Cell(materialRow, 9).Value = item.MaterialType;
                materialSheet.Cell(materialRow, 10).Value = item.RecipeQuantity;
                materialSheet.Cell(materialRow, 11).Value = item.RecipeUnit;
                materialSheet.Cell(materialRow, 12).Value = item.PlannedWasteRate;
                materialSheet.Cell(materialRow, 13).Value = item.PricedQuantity ?? item.CostQuantity;
                materialSheet.Cell(materialRow, 14).Value = string.IsNullOrWhiteSpace(item.CostUnit) ? item.RecipeUnit : item.CostUnit;
                if (item.UnitCostTry.HasValue) materialSheet.Cell(materialRow, 15).Value = item.UnitCostTry.Value;
                materialSheet.Cell(materialRow, 16).Value = item.ProductUnitCostContribution;
                materialSheet.Cell(materialRow, 17).Value = item.ProductUnitCostContribution * savedLine.ProductionQuantity;
                materialSheet.Cell(materialRow, 18).Value = item.CostSource;
                materialSheet.Cell(materialRow, 19).Value = item.CostSourceDetail;
                materialSheet.Cell(materialRow, 20).Value = item.IncludedInCalculation ? "Evet" : "Hayır";
                materialSheet.Cell(materialRow, 21).Value = item.Reason ?? string.Empty;
                if (!item.IncludedInCalculation) materialSheet.Range(materialRow, 1, materialRow, materialHeaders.Length).Style.Fill.SetBackgroundColor(XLColor.LightPink);
            }
        }
        if (materialRow > 1)
        {
            materialSheet.Range(2, 5, materialRow, 5).Style.NumberFormat.Format = "#,##0.######";
            materialSheet.Range(2, 10, materialRow, 10).Style.NumberFormat.Format = "#,##0.######";
            materialSheet.Range(2, 12, materialRow, 12).Style.NumberFormat.Format = "0.00";
            materialSheet.Range(2, 13, materialRow, 13).Style.NumberFormat.Format = "#,##0.######";
            materialSheet.Range(2, 15, materialRow, 17).Style.NumberFormat.Format = "#,##0.000000";
            materialSheet.Range(1, 1, materialRow, materialHeaders.Length).CreateTable();
        }
        materialSheet.SheetView.FreezeRows(1);
        materialSheet.SheetView.FreezeColumns(2);
        FitExcelColumns(materialSheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"recete-maliyet-{model.ScenarioNumber}.xlsx");
    }

    private async Task<RecipeCostScenarioDetailVM?> BuildScenarioDetailAsync(int id, CancellationToken ct)
    {
        var model = await _context.PrdRecipeCostScenarios.AsNoTracking()
            .Where(x => x.ID == id && x.IsDelete != true)
            .Select(x => new RecipeCostScenarioDetailVM
            {
                Id = x.ID,
                ScenarioNumber = x.ScenarioNumber,
                Name = x.Name,
                CalculationDate = x.CalculationDate,
                DefaultProductionQuantity = x.DefaultProductionQuantity,
                TotalProductionQuantity = x.TotalProductionQuantity,
                TotalRecipeCost = x.TotalRecipeCost,
                FixedCost = x.FixedCost,
                VariableCost = x.VariableCost,
                TotalCost = x.TotalCost,
                CreatedUser = x.CreateUserID ?? "-"
            }).FirstOrDefaultAsync(ct);
        if (model == null) return null;
        model.Lines = await _context.PrdRecipeCostScenarioLines.AsNoTracking()
            .Where(x => x.ScenarioId == id && x.IsDelete != true)
            .OrderBy(x => x.ProductCode)
            .Select(x => new RecipeCostScenarioDetailLineVM
            {
                RecipeVersionId = x.RecipeVersionId,
                ProductCode = x.ProductCode,
                ProductName = x.ProductName,
                ProductionUnit = x.ProductionUnit,
                ProductionQuantity = x.ProductionQuantity,
                RawMaterialUnitCost = x.RawMaterialUnitCost,
                PackagingUnitCost = x.PackagingUnitCost,
                RecipeMaterialCost = x.RecipeMaterialCost,
                VariableCostShare = x.VariableCostShare,
                FixedCostShare = x.FixedCostShare,
                TotalCost = x.TotalCost,
                TotalUnitCost = x.TotalUnitCost
            }).ToListAsync(ct);
        model.LineCount = model.Lines.Count;
        return model;
    }

    private static void StyleExcelHeader(IXLRange range)
    {
        range.Style.Font.SetBold().Font.SetFontColor(XLColor.White);
        range.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#1F4E78"));
        range.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center).Alignment.SetVertical(XLAlignmentVerticalValues.Center);
    }

    private static void FitExcelColumns(IXLWorksheet worksheet)
    {
        worksheet.ColumnsUsed().AdjustToContents();
        foreach (var column in worksheet.ColumnsUsed())
        {
            if (column.Width > 45) column.Width = 45;
        }
        var used = worksheet.RangeUsed();
        if (used != null)
        {
            used.Style.Font.FontName = "Arial";
            used.Style.Font.FontSize = 10;
            used.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        }
    }

    [HttpGet]
    public async Task<IActionResult> BirimDonusumleri(CancellationToken ct)
    {
        ViewBag.Modul = "YeniUretim";
        return View(await BuildConversionModelAsync(ct));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BirimDonusumEkle(ProductionUnitConversionManagementVM input, CancellationToken ct)
    {
        if (input.FromUnitId <= 0 || input.ToUnitId <= 0 || input.FromUnitId == input.ToUnitId)
        {
            TempData["error"] = "Kaynak ve hedef için birbirinden farklı iki birim seçiniz.";
            return RedirectToAction(nameof(BirimDonusumleri));
        }
        if (!TryParseDecimal(input.Factor, out var factor) || factor <= 0)
        {
            TempData["error"] = "Dönüşüm katsayısı sıfırdan büyük olmalıdır.";
            return RedirectToAction(nameof(BirimDonusumleri));
        }

        var unitsExist = await _context.PrdUnits.CountAsync(x =>
            (x.ID == input.FromUnitId || x.ID == input.ToUnitId) && x.IsDelete != true && x.IsActive != false, ct) == 2;
        var materialExists = !input.MaterialId.HasValue || await _context.PrdMaterials.AnyAsync(x =>
            x.ID == input.MaterialId.Value && x.IsDelete != true && x.IsActive != false, ct);
        if (!unitsExist || !materialExists)
        {
            TempData["error"] = "Seçilen malzeme veya birim bulunamadı.";
            return RedirectToAction(nameof(BirimDonusumleri));
        }

        var existing = await _context.PrdUnitConversions.FirstOrDefaultAsync(x =>
            x.MaterialId == input.MaterialId && x.FromUnitId == input.FromUnitId && x.ToUnitId == input.ToUnitId, ct);
        var now = DateTime.Now;
        var user = User.Identity?.Name;
        if (existing == null)
        {
            _context.PrdUnitConversions.Add(new PrdUnitConversion
            {
                MaterialId = input.MaterialId,
                FromUnitId = input.FromUnitId,
                ToUnitId = input.ToUnitId,
                Factor = factor,
                Description = Clean(input.Description),
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = user
            });
        }
        else
        {
            existing.Factor = factor;
            existing.Description = Clean(input.Description);
            existing.IsActive = true;
            existing.IsDelete = false;
            existing.DeleteDate = null;
            existing.DeleteUserID = null;
            existing.UpdateDate = now;
            existing.UpdateUserID = user;
        }
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Birim dönüşümü kaydedildi. Ters yöndeki hesap otomatik uygulanır.";
        return RedirectToAction(nameof(BirimDonusumleri));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> BirimDonusumSil(int id, CancellationToken ct)
    {
        var conversion = await _context.PrdUnitConversions.FirstOrDefaultAsync(x => x.ID == id && x.IsDelete != true, ct);
        if (conversion == null) return NotFound();
        conversion.IsActive = false;
        conversion.IsDelete = true;
        conversion.DeleteDate = DateTime.Now;
        conversion.DeleteUserID = User.Identity?.Name;
        await _context.SaveChangesAsync(ct);
        TempData["success"] = "Birim dönüşümü kaldırıldı.";
        return RedirectToAction(nameof(BirimDonusumleri));
    }

    private async Task<ProductionUnitConversionManagementVM> BuildConversionModelAsync(CancellationToken ct)
    {
        var units = await _context.PrdUnits.AsNoTracking()
            .Where(x => x.IsDelete != true && x.IsActive != false)
            .ToListAsync(ct);
        var model = new ProductionUnitConversionManagementVM
        {
            Units = units
                .GroupBy(x => ProductionUnitNormalizer.CanonicalCode(x.Code, x.Name), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var unit = group
                        .OrderByDescending(x => string.Equals(x.Code, group.Key, StringComparison.OrdinalIgnoreCase))
                        .ThenBy(x => x.ID)
                        .First();
                    var name = ProductionUnitNormalizer.DisplayName(group.Key, unit.Name);
                    return new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(group.Key + " - " + name, unit.ID.ToString());
                })
                .OrderBy(x => x.Text)
                .ToList(),
            Materials = await _context.PrdMaterials.AsNoTracking()
                .Where(x => x.IsDelete != true && x.IsActive != false)
                .OrderBy(x => x.Code)
                .Select(x => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(x.Code + " - " + x.Name, x.ID.ToString()))
                .ToListAsync(ct)
        };
        var conversionRows = await (
            from conversion in _context.PrdUnitConversions.AsNoTracking()
            join fromUnit in _context.PrdUnits.AsNoTracking() on conversion.FromUnitId equals fromUnit.ID
            join toUnit in _context.PrdUnits.AsNoTracking() on conversion.ToUnitId equals toUnit.ID
            join material0 in _context.PrdMaterials.AsNoTracking() on conversion.MaterialId equals (int?)material0.ID into materialJoin
            from material in materialJoin.DefaultIfEmpty()
            where conversion.IsDelete != true
            orderby material.Code, fromUnit.Code, toUnit.Code
            select new
            {
                Id = conversion.ID,
                Material = material == null ? "Tüm malzemeler" : material.Code + " - " + material.Name,
                FromUnitCode = fromUnit.Code,
                FromUnitName = fromUnit.Name,
                ToUnitCode = toUnit.Code,
                ToUnitName = toUnit.Name,
                Factor = conversion.Factor,
                Description = conversion.Description
            }).ToListAsync(ct);
        model.Rows = conversionRows.Select(x => new ProductionUnitConversionRowVM
        {
            Id = x.Id,
            Material = x.Material,
            FromUnit = ProductionUnitNormalizer.CanonicalCode(x.FromUnitCode, x.FromUnitName) + " - " + ProductionUnitNormalizer.DisplayName(x.FromUnitCode, x.FromUnitName),
            ToUnit = ProductionUnitNormalizer.CanonicalCode(x.ToUnitCode, x.ToUnitName) + " - " + ProductionUnitNormalizer.DisplayName(x.ToUnitCode, x.ToUnitName),
            Factor = x.Factor,
            Description = x.Description
        }).ToList();
        return model;
    }

    private static Dictionary<string, MaterialUnitCost> BuildStockCosts(List<StockCostSource> movements)
    {
        var result = new Dictionary<string, MaterialUnitCost>(StringComparer.OrdinalIgnoreCase);
        foreach (var materialGroup in movements.GroupBy(x => x.MaterialCode, StringComparer.OrdinalIgnoreCase))
        {
            // Plan maliyeti güncel kalsın: transfer hariç son maliyetli depo girişini kullan.
            var latestIn = materialGroup
                .OrderByDescending(x => x.MovementDate)
                .ThenByDescending(x => x.Id)
                .FirstOrDefault();
            if (latestIn != null)
            {
                result[materialGroup.Key] = new MaterialUnitCost(
                    latestIn.UnitId, latestIn.UnitCode, latestIn.UnitName, latestIn.UnitCost, latestIn.MovementDate);
            }
        }
        return result;
    }

    private static bool IsPackaging(PrdMaterialType materialType, string materialCode)
    {
        if (materialType == PrdMaterialType.Packaging) return true;
        var code = (materialCode ?? string.Empty).Trim().ToUpperInvariant();
        return code.StartsWith("150HM.02.", StringComparison.Ordinal) ||
               code.StartsWith("150AM.", StringComparison.Ordinal);
    }

    private static bool TryConvertQuantity(
        decimal quantity,
        int materialId,
        IReadOnlyList<UnitConversionRule> conversionRules,
        int? sourceUnitId, string? sourceCode, string? sourceName,
        int? targetUnitId, string? targetCode, string? targetName,
        out decimal converted)
    {
        converted = 0m;
        if (sourceUnitId.HasValue && targetUnitId.HasValue && sourceUnitId == targetUnitId)
        {
            converted = quantity;
            return true;
        }

        var source = ResolveUnit(sourceCode, sourceName);
        var target = ResolveUnit(targetCode, targetName);
        if (source != null && target != null && source.Value.Family == target.Value.Family)
        {
            converted = quantity * source.Value.Factor / target.Value.Factor;
            return true;
        }

        var rules = conversionRules
            .Where(x => x.MaterialId == materialId || x.MaterialId == null)
            .OrderByDescending(x => x.MaterialId.HasValue)
            .ToList();
        var direct = rules.FirstOrDefault(x =>
            UnitMatches(sourceUnitId, sourceCode, sourceName, x.FromUnitId, x.FromCode, x.FromName) &&
            UnitMatches(targetUnitId, targetCode, targetName, x.ToUnitId, x.ToCode, x.ToName));
        if (direct != null)
        {
            converted = quantity * direct.Factor;
            return true;
        }
        var reverse = rules.FirstOrDefault(x =>
            UnitMatches(sourceUnitId, sourceCode, sourceName, x.ToUnitId, x.ToCode, x.ToName) &&
            UnitMatches(targetUnitId, targetCode, targetName, x.FromUnitId, x.FromCode, x.FromName));
        if (reverse != null)
        {
            converted = quantity / reverse.Factor;
            return true;
        }
        return false;
    }

    private static bool UnitMatches(int? leftId, string? leftCode, string? leftName, int rightId, string? rightCode, string? rightName)
    {
        if (leftId.HasValue && leftId.Value == rightId) return true;
        return string.Equals(
            ProductionUnitNormalizer.CanonicalCode(leftCode, leftName),
            ProductionUnitNormalizer.CanonicalCode(rightCode, rightName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static (string Family, decimal Factor)? ResolveUnit(string? code, string? name)
    {
        var canonical = ProductionUnitNormalizer.CanonicalCode(code, name);
        return canonical switch
        {
            "KG" => ("MASS", 1000m),
            "GRAM" => ("MASS", 1m),
            "LITRE" => ("VOLUME", 1000m),
            "ML" => ("VOLUME", 1m),
            "ADET" => ("COUNT", 1m),
            "KOLI" => ("PACKAGE:KOLI", 1m),
            "PAKET" => ("PACKAGE:PAKET", 1m),
            _ when !string.IsNullOrWhiteSpace(canonical) => ($"EXACT:{canonical}", 1m),
            _ => null
        };
    }

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        text = (text ?? string.Empty).Trim();
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.GetCultureInfo("tr-TR"), out value) ||
               decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryParseOptionalDecimal(string? text, out decimal value)
    {
        if (string.IsNullOrWhiteSpace(text)) { value = 0m; return true; }
        return TryParseDecimal(text, out value);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class RecipeItemCostSource
    {
        public int RecipeVersionId { get; set; }
        public int MaterialId { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public string MaterialName { get; set; } = string.Empty;
        public PrdMaterialType MaterialType { get; set; }
        public decimal Quantity { get; set; }
        public decimal PlannedWasteRate { get; set; }
        public int UnitId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
    }

    private sealed class StockCostSource
    {
        public int Id { get; set; }
        public string MaterialCode { get; set; } = string.Empty;
        public int UnitId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal UnitCost { get; set; }
        public DateTime MovementDate { get; set; }
    }

    private sealed class ReferenceCostSource
    {
        public int MaterialId { get; set; }
        public int UnitId { get; set; }
        public string UnitCode { get; set; } = string.Empty;
        public string UnitName { get; set; } = string.Empty;
        public decimal UnitCostTry { get; set; }
        public int VersionNumber { get; set; }
        public DateTime ValidFrom { get; set; }
    }

    private enum RecipeCostSource { None, Stock, Reference, Legacy }

    private sealed record MaterialUnitCost(
        int UnitId,
        string UnitCode,
        string UnitName,
        decimal UnitCost,
        DateTime MovementDate);

    private sealed class UnitConversionRule
    {
        public int? MaterialId { get; set; }
        public int FromUnitId { get; set; }
        public string FromCode { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public int ToUnitId { get; set; }
        public string ToCode { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public decimal Factor { get; set; }
    }
}
