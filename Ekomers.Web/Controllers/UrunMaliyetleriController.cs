using System.Data;
using Ekomers.Data;
using Ekomers.Models.Entity.Profitability;
using Ekomers.Models.LogoDb;
using Ekomers.Models.ViewModels.Profitability;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Ekomers.Web.Controllers;

[Authorize(Roles = "Admin,Yönetici")]
public sealed class UrunMaliyetleriController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly LogoContext _logoContext;

    public UrunMaliyetleriController(ApplicationDbContext context, LogoContext logoContext)
    {
        _context = context;
        _logoContext = logoContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, CancellationToken ct)
    {
        ViewBag.Modul = "Rapor";
        search = Clean(search);

        var query = _context.RptProductCostVersions.AsNoTracking()
            .Where(x => x.IsDelete != true);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.ProductCode, $"%{search}%") ||
                EF.Functions.Like(x.ProductName, $"%{search}%"));
        }

        var versions = await query
            .OrderBy(x => x.ProductCode)
            .ThenByDescending(x => x.VersionNumber)
            .ToListAsync(ct);

        var model = new ProductCostIndexVM
        {
            Search = search,
            Products = versions
                .GroupBy(x => new { x.LogoMaterialRef, x.ProductCode, x.ProductName })
                .Select(group =>
                {
                    var latest = group.OrderByDescending(x => x.VersionNumber).First();
                    return new ProductCostSummaryVM
                    {
                        LogoMaterialRef = group.Key.LogoMaterialRef,
                        ProductCode = group.Key.ProductCode,
                        ProductName = group.Key.ProductName,
                        VersionCount = group.Count(),
                        LatestVersionNumber = latest.VersionNumber,
                        ValidFrom = latest.ValidFrom,
                        UnitCode = latest.UnitCode,
                        TotalUnitCostTry = latest.TotalUnitCostTry
                    };
                })
                .OrderBy(x => x.ProductCode)
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Yeni(int? logoMaterialRef, CancellationToken ct)
    {
        ViewBag.Modul = "Rapor";
        var model = new ProductCostFormVM();
        await PopulateFormAsync(model, logoMaterialRef, ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Yeni(ProductCostFormVM model, CancellationToken ct)
    {
        ViewBag.Modul = "Rapor";
        model.CurrencyCode = (model.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        model.UnitCode = (model.UnitCode ?? string.Empty).Trim();
        model.ChangeReason = (model.ChangeReason ?? string.Empty).Trim();
        model.ValidFrom = model.ValidFrom.Date;

        if (model.CurrencyCode == "TRY")
        {
            model.ExchangeRate = 1m;
        }

        if (model.ValidFrom == default)
        {
            ModelState.AddModelError(nameof(model.ValidFrom), "Geçerlilik başlangıcını giriniz.");
        }

        var product = await _logoContext.Items.AsNoTracking()
            .Where(x => x.LOGICALREF == model.LogoMaterialRef && x.ACTIVE == 0)
            .Select(x => new { x.LOGICALREF, x.CODE, x.NAME })
            .FirstOrDefaultAsync(ct);
        if (product == null)
        {
            ModelState.AddModelError(nameof(model.LogoMaterialRef), "Seçilen aktif Logo ürünü bulunamadı.");
        }

        var latest = await _context.RptProductCostVersions
            .Where(x => x.LogoMaterialRef == model.LogoMaterialRef && x.IsDelete != true)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);
        if (latest != null && model.ValidFrom <= latest.ValidFrom.Date)
        {
            ModelState.AddModelError(nameof(model.ValidFrom),
                $"Yeni versiyon tarihi son versiyon tarihinden ({latest.ValidFrom:dd.MM.yyyy}) sonra olmalıdır.");
        }

        if (!ModelState.IsValid || product == null)
        {
            await PopulateFormAsync(model, model.LogoMaterialRef > 0 ? model.LogoMaterialRef : null, ct);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        latest = await _context.RptProductCostVersions
            .Where(x => x.LogoMaterialRef == model.LogoMaterialRef && x.IsDelete != true)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);
        if (latest != null && model.ValidFrom <= latest.ValidFrom.Date)
        {
            await transaction.RollbackAsync(ct);
            ModelState.AddModelError(nameof(model.ValidFrom), "Bu ürün için daha yeni veya aynı tarihli bir maliyet versiyonu bulunuyor.");
            await PopulateFormAsync(model, model.LogoMaterialRef, ct);
            return View(model);
        }

        var now = DateTime.Now;
        if (latest != null)
        {
            latest.Status = RptProductCostVersionStatus.Closed;
            latest.ValidTo = model.ValidFrom.AddDays(-1);
            latest.UpdateDate = now;
            latest.UpdateUserID = CurrentUser;
        }

        var totalUnitCost = model.MaterialCost + model.LaborCost + model.FreightCost +
                            model.OverheadCost + model.OtherCost;
        var version = new RptProductCostVersion
        {
            LogoMaterialRef = product.LOGICALREF,
            ProductCode = product.CODE?.Trim() ?? string.Empty,
            ProductName = product.NAME?.Trim() ?? string.Empty,
            VersionNumber = (latest?.VersionNumber ?? 0) + 1,
            Status = RptProductCostVersionStatus.Active,
            ValidFrom = model.ValidFrom,
            ValidTo = null,
            UnitCode = model.UnitCode,
            MaterialCost = model.MaterialCost,
            LaborCost = model.LaborCost,
            FreightCost = model.FreightCost,
            OverheadCost = model.OverheadCost,
            OtherCost = model.OtherCost,
            TotalUnitCost = totalUnitCost,
            CurrencyCode = model.CurrencyCode,
            ExchangeRate = model.ExchangeRate,
            TotalUnitCostTry = totalUnitCost * model.ExchangeRate,
            Source = "Manual",
            ChangeReason = model.ChangeReason,
            ApprovedDate = now,
            ApprovedUserId = CurrentUser,
            IsActive = true,
            IsDelete = false,
            CreateDate = now,
            CreateUserID = CurrentUser
        };
        _context.RptProductCostVersions.Add(version);
        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        TempData["success"] = $"{version.ProductCode} için v{version.VersionNumber} maliyet versiyonu oluşturuldu.";
        return RedirectToAction(nameof(Gecmis), new { logoMaterialRef = version.LogoMaterialRef });
    }

    [HttpGet]
    public async Task<IActionResult> Toplu(
        string? search,
        string statusFilter = "all",
        string? codePrefixes = null,
        CancellationToken ct = default)
    {
        ViewBag.Modul = "Rapor";
        var model = new ProductCostBulkVM
        {
            Search = Clean(search),
            StatusFilter = NormalizeStatusFilter(statusFilter),
            CodePrefixes = Request.Query.ContainsKey(nameof(codePrefixes))
                ? codePrefixes ?? string.Empty
                : "152MM,153TG",
            ValidFrom = DateTime.Today
        };
        await PopulateBulkRowsAsync(model, ct);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toplu(ProductCostBulkVM model, CancellationToken ct)
    {
        ViewBag.Modul = "Rapor";
        model.CurrencyCode = (model.CurrencyCode ?? string.Empty).Trim().ToUpperInvariant();
        model.ChangeReason = (model.ChangeReason ?? string.Empty).Trim();
        model.CodePrefixes = (model.CodePrefixes ?? string.Empty).Trim();
        model.ValidFrom = model.ValidFrom.Date;
        model.StatusFilter = NormalizeStatusFilter(model.StatusFilter);
        if (model.CurrencyCode == "TRY")
        {
            model.ExchangeRate = 1m;
        }

        if (model.ValidFrom == default)
        {
            ModelState.AddModelError(nameof(model.ValidFrom), "Geçerlilik başlangıcını giriniz.");
        }
        if (model.CurrencyCode is not ("TRY" or "USD" or "EUR" or "GBP"))
        {
            ModelState.AddModelError(nameof(model.CurrencyCode), "Desteklenen bir para birimi seçiniz.");
        }
        var allowedPrefixes = ParseCodePrefixes(model.CodePrefixes);
        if (allowedPrefixes.Count == 0)
        {
            ModelState.AddModelError(nameof(model.CodePrefixes), "En az bir ürün kodu ön eki giriniz.");
        }

        var changedRows = model.Rows
            .Select((row, index) => new { Row = row, Index = index })
            .Where(x => IsChanged(x.Row))
            .ToList();
        if (changedRows.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "En az bir ürün için yeni maliyet giriniz.");
        }

        var parsedCosts = new Dictionary<int, BulkCostValues>();
        foreach (var item in changedRows)
        {
            var row = item.Row;
            var key = $"Rows[{item.Index}].NewUnitCost";
            if (row.LogoMaterialRef <= 0)
            {
                ModelState.AddModelError(key, "Ürün bilgisi geçersiz.");
                continue;
            }
            if (parsedCosts.ContainsKey(row.LogoMaterialRef))
            {
                ModelState.AddModelError(key, "Bu ürün listede birden fazla kez bulunuyor.");
                continue;
            }

            var components = new[]
            {
                row.MaterialCost, row.LaborCost, row.FreightCost, row.OverheadCost, row.OtherCost
            };
            if (components.Any(x => x < 0) || row.NewUnitCost < 0)
            {
                ModelState.AddModelError(key, "Maliyet değerleri negatif olamaz.");
                continue;
            }

            var hasDetails = components.Any(x => x.HasValue);
            decimal material;
            decimal labor;
            decimal freight;
            decimal overhead;
            decimal other;
            decimal total;
            string source;
            if (hasDetails)
            {
                material = row.MaterialCost ?? 0m;
                labor = row.LaborCost ?? 0m;
                freight = row.FreightCost ?? 0m;
                overhead = row.OverheadCost ?? 0m;
                other = row.OtherCost ?? 0m;
                total = material + labor + freight + overhead + other;
                source = "BulkDetailed";
                if (row.NewUnitCost.HasValue && Math.Abs(row.NewUnitCost.Value - total) > 0.000001m)
                {
                    ModelState.AddModelError(key, "Yeni maliyet, detay kalemlerinin toplamıyla aynı olmalıdır.");
                    continue;
                }
            }
            else
            {
                if (!row.NewUnitCost.HasValue)
                {
                    ModelState.AddModelError(key, "Seçilen ürün için yeni maliyet giriniz.");
                    continue;
                }
                // Şemada toplam maliyet için ayrı bir bileşen bulunmadığından hızlı giriş,
                // Source alanıyla işaretlenerek ana maliyet bileşeninde saklanır.
                material = row.NewUnitCost.Value;
                labor = freight = overhead = other = 0m;
                total = row.NewUnitCost.Value;
                source = "BulkTotal";
            }

            if (total <= 0)
            {
                ModelState.AddModelError(key, "Toplam maliyet sıfırdan büyük olmalıdır.");
                continue;
            }
            row.NewUnitCost = total;
            parsedCosts[row.LogoMaterialRef] = new BulkCostValues(
                material, labor, freight, overhead, other, total, source);
        }

        var materialRefs = parsedCosts.Keys.ToList();
        var logoProducts = await _logoContext.Items.AsNoTracking()
            .Where(x => materialRefs.Contains(x.LOGICALREF) && x.ACTIVE == 0)
            .Select(x => new { x.LOGICALREF, x.CODE, x.NAME })
            .ToListAsync(ct);
        var logoByRef = logoProducts.ToDictionary(x => x.LOGICALREF);
        foreach (var item in changedRows.Where(x => !logoByRef.ContainsKey(x.Row.LogoMaterialRef)))
        {
            ModelState.AddModelError($"Rows[{item.Index}].NewUnitCost", "Aktif Logo ürünü bulunamadı.");
        }
        foreach (var item in changedRows.Where(x =>
                     logoByRef.TryGetValue(x.Row.LogoMaterialRef, out var product) &&
                     !allowedPrefixes.Any(prefix => product.CODE.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))))
        {
            ModelState.AddModelError($"Rows[{item.Index}].NewUnitCost",
                "Ürün kodu izin verilen ön eklerden biriyle başlamıyor.");
        }

        var latestByRef = (await _context.RptProductCostVersions.AsNoTracking()
                .Where(x => materialRefs.Contains(x.LogoMaterialRef) && x.IsDelete != true)
                .OrderByDescending(x => x.VersionNumber)
                .ToListAsync(ct))
            .GroupBy(x => x.LogoMaterialRef)
            .ToDictionary(x => x.Key, x => x.First());
        foreach (var item in changedRows)
        {
            if (latestByRef.TryGetValue(item.Row.LogoMaterialRef, out var latest) &&
                model.ValidFrom <= latest.ValidFrom.Date)
            {
                ModelState.AddModelError($"Rows[{item.Index}].NewUnitCost",
                    $"Başlangıç tarihi son versiyon tarihinden ({latest.ValidFrom:dd.MM.yyyy}) sonra olmalıdır.");
            }
        }

        if (!ModelState.IsValid)
        {
            await RehydrateBulkRowsAsync(model, ct);
            return View(model);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var trackedVersions = await _context.RptProductCostVersions
            .Where(x => materialRefs.Contains(x.LogoMaterialRef) && x.IsDelete != true)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(ct);
        var trackedLatestByRef = trackedVersions
            .GroupBy(x => x.LogoMaterialRef)
            .ToDictionary(x => x.Key, x => x.First());
        var conflict = trackedLatestByRef.Values.FirstOrDefault(x => model.ValidFrom <= x.ValidFrom.Date);
        if (conflict != null)
        {
            await transaction.RollbackAsync(ct);
            ModelState.AddModelError(string.Empty,
                $"{conflict.ProductCode} için aynı veya daha yeni tarihli bir maliyet versiyonu oluşturulmuş. Listeyi yenileyiniz.");
            await RehydrateBulkRowsAsync(model, ct);
            return View(model);
        }

        var now = DateTime.Now;
        var unitByCode = await ReadUnitMapAsync(ct);
        foreach (var materialRef in materialRefs)
        {
            var product = logoByRef[materialRef];
            var productCode = product.CODE?.Trim() ?? string.Empty;
            var values = parsedCosts[materialRef];
            trackedLatestByRef.TryGetValue(materialRef, out var latest);
            if (latest != null)
            {
                latest.Status = RptProductCostVersionStatus.Closed;
                latest.ValidTo = model.ValidFrom.AddDays(-1);
                latest.UpdateDate = now;
                latest.UpdateUserID = CurrentUser;
            }

            _context.RptProductCostVersions.Add(new RptProductCostVersion
            {
                LogoMaterialRef = materialRef,
                ProductCode = productCode,
                ProductName = product.NAME?.Trim() ?? string.Empty,
                VersionNumber = (latest?.VersionNumber ?? 0) + 1,
                Status = RptProductCostVersionStatus.Active,
                ValidFrom = model.ValidFrom,
                UnitCode = unitByCode.TryGetValue(productCode, out var unit) ? unit : "Adet",
                MaterialCost = values.Material,
                LaborCost = values.Labor,
                FreightCost = values.Freight,
                OverheadCost = values.Overhead,
                OtherCost = values.Other,
                TotalUnitCost = values.Total,
                CurrencyCode = model.CurrencyCode,
                ExchangeRate = model.ExchangeRate,
                TotalUnitCostTry = values.Total * model.ExchangeRate,
                Source = values.Source,
                ChangeReason = model.ChangeReason,
                ApprovedDate = now,
                ApprovedUserId = CurrentUser,
                IsActive = true,
                IsDelete = false,
                CreateDate = now,
                CreateUserID = CurrentUser
            });
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        TempData["success"] = $"{materialRefs.Count} ürün için yeni maliyet versiyonu oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Gecmis(int logoMaterialRef, CancellationToken ct)
    {
        ViewBag.Modul = "Rapor";
        var versions = await _context.RptProductCostVersions.AsNoTracking()
            .Where(x => x.LogoMaterialRef == logoMaterialRef && x.IsDelete != true)
            .OrderByDescending(x => x.VersionNumber)
            .ToListAsync(ct);
        if (versions.Count == 0)
        {
            return NotFound();
        }

        var latest = versions[0];
        var model = new ProductCostHistoryVM
        {
            LogoMaterialRef = logoMaterialRef,
            ProductCode = latest.ProductCode,
            ProductName = latest.ProductName,
            Versions = versions.Select(x => new ProductCostHistoryRowVM
            {
                Id = x.ID,
                VersionNumber = x.VersionNumber,
                Status = StatusText(x.Status),
                ValidFrom = x.ValidFrom,
                ValidTo = x.ValidTo,
                UnitCode = x.UnitCode,
                MaterialCost = x.MaterialCost,
                LaborCost = x.LaborCost,
                FreightCost = x.FreightCost,
                OverheadCost = x.OverheadCost,
                OtherCost = x.OtherCost,
                TotalUnitCost = x.TotalUnitCost,
                CurrencyCode = x.CurrencyCode,
                ExchangeRate = x.ExchangeRate,
                TotalUnitCostTry = x.TotalUnitCostTry,
                Source = x.Source,
                ChangeReason = x.ChangeReason,
                CreateDate = x.CreateDate,
                CreateUser = x.CreateUserID
            }).ToList()
        };
        return View(model);
    }

    private async Task PopulateFormAsync(
        ProductCostFormVM model,
        int? logoMaterialRef,
        CancellationToken ct)
    {
        var unitByLogoCode = await (
            from material in _context.PrdMaterials.AsNoTracking()
            join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
            where material.IsDelete != true && material.LogoCode != null
            select new { material.LogoCode, Unit = unit.Name })
            .ToListAsync(ct);
        var units = unitByLogoCode
            .Where(x => !string.IsNullOrWhiteSpace(x.LogoCode))
            .GroupBy(x => x.LogoCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Unit, StringComparer.OrdinalIgnoreCase);

        var logoProducts = await _logoContext.Items.AsNoTracking()
            .Where(x => x.ACTIVE == 0 && x.CODE != null && x.CODE != "")
            .OrderBy(x => x.CODE)
            .Select(x => new { x.LOGICALREF, x.CODE, x.NAME })
            .ToListAsync(ct);
        model.Products = logoProducts.Select(x => new ProductCostProductOptionVM
        {
            LogoMaterialRef = x.LOGICALREF,
            ProductCode = x.CODE.Trim(),
            ProductName = x.NAME == null ? string.Empty : x.NAME.Trim(),
            UnitCode = units.TryGetValue(x.CODE.Trim(), out var unit) ? unit : "Adet"
        }).ToList();

        if (!logoMaterialRef.HasValue || logoMaterialRef.Value <= 0)
        {
            return;
        }

        model.LogoMaterialRef = logoMaterialRef.Value;
        var selected = model.Products.FirstOrDefault(x => x.LogoMaterialRef == logoMaterialRef.Value);
        if (selected != null)
        {
            model.ProductCode = selected.ProductCode;
            model.ProductName = selected.ProductName;
            if (string.IsNullOrWhiteSpace(model.UnitCode) || model.UnitCode == "Adet")
            {
                model.UnitCode = selected.UnitCode;
            }
        }

        var previous = await _context.RptProductCostVersions.AsNoTracking()
            .Where(x => x.LogoMaterialRef == logoMaterialRef.Value && x.IsDelete != true)
            .OrderByDescending(x => x.VersionNumber)
            .FirstOrDefaultAsync(ct);
        if (previous == null)
        {
            return;
        }

        model.PreviousVersion = new ProductCostSummaryVM
        {
            LogoMaterialRef = previous.LogoMaterialRef,
            ProductCode = previous.ProductCode,
            ProductName = previous.ProductName,
            VersionCount = await _context.RptProductCostVersions.CountAsync(
                x => x.LogoMaterialRef == logoMaterialRef.Value && x.IsDelete != true, ct),
            LatestVersionNumber = previous.VersionNumber,
            ValidFrom = previous.ValidFrom,
            UnitCode = previous.UnitCode,
            TotalUnitCostTry = previous.TotalUnitCostTry
        };

        if (!Request.HasFormContentType)
        {
            model.ValidFrom = DateTime.Today > previous.ValidFrom.Date
                ? DateTime.Today
                : previous.ValidFrom.Date.AddDays(1);
            model.UnitCode = previous.UnitCode;
            model.MaterialCost = previous.MaterialCost;
            model.LaborCost = previous.LaborCost;
            model.FreightCost = previous.FreightCost;
            model.OverheadCost = previous.OverheadCost;
            model.OtherCost = previous.OtherCost;
            model.CurrencyCode = previous.CurrencyCode;
            model.ExchangeRate = previous.ExchangeRate;
        }
    }

    private async Task PopulateBulkRowsAsync(ProductCostBulkVM model, CancellationToken ct)
    {
        var prefixes = ParseCodePrefixes(model.CodePrefixes);
        if (prefixes.Count == 0)
        {
            model.Rows = [];
            model.TotalProductCount = 0;
            return;
        }

        var logoQuery = _logoContext.Items.AsNoTracking()
            .Where(x => x.ACTIVE == 0 && x.CODE != null && x.CODE != "");
        logoQuery = logoQuery.Where(BuildCodePrefixExpression(prefixes));
        if (!string.IsNullOrWhiteSpace(model.Search))
        {
            var search = model.Search;
            logoQuery = logoQuery.Where(x =>
                x.CODE.Contains(search) || (x.NAME != null && x.NAME.Contains(search)));
        }

        var logoProducts = await logoQuery
            .OrderBy(x => x.CODE)
            .Select(x => new { x.LOGICALREF, x.CODE, x.NAME })
            .ToListAsync(ct);
        var latestByRef = (await _context.RptProductCostVersions.AsNoTracking()
                .Where(x => x.IsDelete != true)
                .OrderByDescending(x => x.VersionNumber)
                .ToListAsync(ct))
            .GroupBy(x => x.LogoMaterialRef)
            .ToDictionary(x => x.Key, x => x.First());
        var unitByCode = await ReadUnitMapAsync(ct);

        var rows = logoProducts.Select(product =>
        {
            latestByRef.TryGetValue(product.LOGICALREF, out var latest);
            var code = product.CODE.Trim();
            return new ProductCostBulkRowVM
            {
                LogoMaterialRef = product.LOGICALREF,
                ProductCode = code,
                ProductName = product.NAME?.Trim() ?? string.Empty,
                UnitCode = unitByCode.TryGetValue(code, out var unit) ? unit : "Adet",
                CurrentVersionNumber = latest?.VersionNumber,
                CurrentValidFrom = latest?.ValidFrom,
                CurrentUnitCostTry = latest?.TotalUnitCostTry
            };
        });
        rows = model.StatusFilter switch
        {
            "missing" => rows.Where(x => !x.CurrentVersionNumber.HasValue),
            "defined" => rows.Where(x => x.CurrentVersionNumber.HasValue),
            _ => rows
        };

        var filteredRows = rows.ToList();
        model.TotalProductCount = filteredRows.Count;
        model.IsTruncated = filteredRows.Count > 500;
        model.Rows = filteredRows.Take(500).ToList();
    }

    private async Task RehydrateBulkRowsAsync(ProductCostBulkVM model, CancellationToken ct)
    {
        var refs = model.Rows.Select(x => x.LogoMaterialRef).Where(x => x > 0).Distinct().ToList();
        var logoProducts = await _logoContext.Items.AsNoTracking()
            .Where(x => refs.Contains(x.LOGICALREF))
            .Select(x => new { x.LOGICALREF, x.CODE, x.NAME })
            .ToDictionaryAsync(x => x.LOGICALREF, ct);
        var latestByRef = (await _context.RptProductCostVersions.AsNoTracking()
                .Where(x => refs.Contains(x.LogoMaterialRef) && x.IsDelete != true)
                .OrderByDescending(x => x.VersionNumber)
                .ToListAsync(ct))
            .GroupBy(x => x.LogoMaterialRef)
            .ToDictionary(x => x.Key, x => x.First());
        var unitByCode = await ReadUnitMapAsync(ct);
        foreach (var row in model.Rows)
        {
            if (logoProducts.TryGetValue(row.LogoMaterialRef, out var product))
            {
                row.ProductCode = product.CODE?.Trim() ?? string.Empty;
                row.ProductName = product.NAME?.Trim() ?? string.Empty;
                row.UnitCode = unitByCode.TryGetValue(row.ProductCode, out var unit) ? unit : "Adet";
            }
            if (latestByRef.TryGetValue(row.LogoMaterialRef, out var latest))
            {
                row.CurrentVersionNumber = latest.VersionNumber;
                row.CurrentValidFrom = latest.ValidFrom;
                row.CurrentUnitCostTry = latest.TotalUnitCostTry;
            }
        }
        model.TotalProductCount = model.Rows.Count;
    }

    private async Task<Dictionary<string, string>> ReadUnitMapAsync(CancellationToken ct)
    {
        var rows = await (
                from material in _context.PrdMaterials.AsNoTracking()
                join unit in _context.PrdUnits.AsNoTracking() on material.UnitId equals unit.ID
                where material.IsDelete != true && material.LogoCode != null
                select new { material.LogoCode, Unit = unit.Name })
            .ToListAsync(ct);
        return rows
            .Where(x => !string.IsNullOrWhiteSpace(x.LogoCode))
            .GroupBy(x => x.LogoCode!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First().Unit, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsChanged(ProductCostBulkRowVM row) =>
        row.Selected || row.NewUnitCost.HasValue || row.MaterialCost.HasValue ||
        row.LaborCost.HasValue || row.FreightCost.HasValue || row.OverheadCost.HasValue ||
        row.OtherCost.HasValue;

    private static string NormalizeStatusFilter(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "missing" => "missing",
        "defined" => "defined",
        _ => "all"
    };

    private static IReadOnlyList<string> ParseCodePrefixes(string? value) =>
        (value ?? string.Empty)
        .Split([',', ';', '\r', '\n', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.ToUpperInvariant())
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToList();

    private static Expression<Func<LG_100_ITEMS, bool>> BuildCodePrefixExpression(
        IReadOnlyList<string> prefixes)
    {
        var product = Expression.Parameter(typeof(LG_100_ITEMS), "product");
        var code = Expression.Property(product, nameof(LG_100_ITEMS.CODE));
        Expression body = Expression.Constant(false);
        foreach (var prefix in prefixes)
        {
            var startsWith = Expression.Call(
                code,
                nameof(string.StartsWith),
                Type.EmptyTypes,
                Expression.Constant(prefix));
            body = Expression.OrElse(body, startsWith);
        }
        return Expression.Lambda<Func<LG_100_ITEMS, bool>>(body, product);
    }

    private string CurrentUser => User.Identity?.Name ?? "system";

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string StatusText(RptProductCostVersionStatus status) => status switch
    {
        RptProductCostVersionStatus.Active => "Aktif",
        RptProductCostVersionStatus.Closed => "Kapanmış",
        _ => "Taslak"
    };

    private sealed record BulkCostValues(
        decimal Material,
        decimal Labor,
        decimal Freight,
        decimal Overhead,
        decimal Other,
        decimal Total,
        string Source);
}
