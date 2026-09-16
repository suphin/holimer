using Ekomers.Data;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Ekomers.Web.Controllers;

[Authorize(Policy="UretimSiparisGoruntule")]
public sealed class UretimSiparisleriController : Controller
{
    private const string PlanningSessionKey="PrdProductionPlanningList";
    private readonly ApplicationDbContext _context;

    public UretimSiparisleriController(ApplicationDbContext context) => _context=context;

    [HttpGet]
    public async Task<IActionResult> Index(string? search,PrdCustomerOrderStatus? status,CancellationToken ct)
    {
        ViewBag.Modul="UretimSiparis";ViewBag.Search=search;ViewBag.Status=status;
        var query=_context.PrdCustomerOrders.AsNoTracking().Where(x=>x.IsDelete!=true);
        if(status.HasValue)query=query.Where(x=>x.Status==status.Value);
        if(!string.IsNullOrWhiteSpace(search))
        {
            var term=search.Trim();query=query.Where(x=>x.OrderNumber.Contains(term)||(x.ExternalOrderNumber!=null&&x.ExternalOrderNumber.Contains(term))||x.CustomerName.Contains(term)||(x.CustomerCode!=null&&x.CustomerCode.Contains(term)));
        }
        var rows=await query.OrderByDescending(x=>x.OrderDate).ThenByDescending(x=>x.ID).Select(x=>new ProductionCustomerOrderListVM
        {
            Id=x.ID,OrderNumber=x.OrderNumber,ExternalOrderNumber=x.ExternalOrderNumber,CustomerName=x.CustomerName,OrderDate=x.OrderDate,RequestedDeliveryDate=x.RequestedDeliveryDate,Priority=x.Priority,Status=x.Status,
            LineCount=_context.PrdCustomerOrderLines.Count(l=>l.CustomerOrderId==x.ID&&l.IsDelete!=true),
            OrderedQuantity=_context.PrdCustomerOrderLines.Where(l=>l.CustomerOrderId==x.ID&&l.IsDelete!=true).Sum(l=>(decimal?)l.OrderedQuantity-l.CancelledQuantity)??0,
            PlannedQuantity=(from l in _context.PrdCustomerOrderLines where l.CustomerOrderId==x.ID&&l.IsDelete!=true join a in _context.PrdProductionPlanOrderAllocations.Where(a=>a.IsDelete!=true) on l.ID equals a.CustomerOrderLineId select (decimal?)a.PlannedQuantity).Sum()??0
        }).ToListAsync(ct);
        return View(rows);
    }

    [HttpGet,Authorize(Policy="UretimSiparisOlustur")]
    public async Task<IActionResult> Yeni(CancellationToken ct)
    {
        ViewBag.Modul="UretimSiparis";var model=new ProductionCustomerOrderFormVM();await FillProductsAsync(model,ct);return View("Form",model);
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="UretimSiparisOlustur")]
    public async Task<IActionResult> Yeni(ProductionCustomerOrderFormVM model,CancellationToken ct)
    {
        ViewBag.Modul="UretimSiparis";var validLines=await ValidateAndResolveLinesAsync(model,ct);
        if(!ModelState.IsValid){await FillProductsAsync(model,ct);return View("Form",model);}
        var now=DateTime.Now;var order=new PrdCustomerOrder
        {
            OrderNumber=$"SIP-{now:yyyyMMddHHmmssfff}-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",ExternalOrderNumber=Clean(model.ExternalOrderNumber),CustomerCode=Clean(model.CustomerCode),CustomerName=model.CustomerName.Trim(),OrderDate=model.OrderDate.Date,RequestedDeliveryDate=model.RequestedDeliveryDate.Date,Priority=model.Priority,Status=PrdCustomerOrderStatus.Draft,Notes=Clean(model.Notes),IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=User.Identity?.Name
        };
        await using var transaction=await _context.Database.BeginTransactionAsync(ct);_context.PrdCustomerOrders.Add(order);await _context.SaveChangesAsync(ct);
        var sequence=1;foreach(var line in validLines)_context.PrdCustomerOrderLines.Add(new PrdCustomerOrderLine{CustomerOrderId=order.ID,Sequence=sequence++,ProductMaterialId=line.ProductMaterialId,UnitId=line.UnitId,OrderedQuantity=line.Quantity,RequestedDeliveryDate=line.RequestedDeliveryDate,Notes=line.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=User.Identity?.Name});
        await _context.SaveChangesAsync(ct);await transaction.CommitAsync(ct);TempData["success"]=$"{order.OrderNumber} numaralı sipariş taslak olarak oluşturuldu.";return RedirectToAction(nameof(Detay),new{id=order.ID});
    }

    [HttpGet]
    public async Task<IActionResult> Detay(int id,CancellationToken ct)
    {
        ViewBag.Modul="UretimSiparis";var order=await _context.PrdCustomerOrders.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(order==null)return NotFound();
        var model=new ProductionCustomerOrderDetailVM{Id=order.ID,OrderNumber=order.OrderNumber,ExternalOrderNumber=order.ExternalOrderNumber,CustomerCode=order.CustomerCode,CustomerName=order.CustomerName,OrderDate=order.OrderDate,RequestedDeliveryDate=order.RequestedDeliveryDate,Priority=order.Priority,Status=order.Status,Notes=order.Notes};
        model.Lines=await(from line in _context.PrdCustomerOrderLines.AsNoTracking() join product in _context.PrdMaterials.AsNoTracking() on line.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID where line.CustomerOrderId==id&&line.IsDelete!=true orderby line.Sequence select new ProductionCustomerOrderDetailLineVM{Id=line.ID,ProductCode=product.Code,ProductName=product.Name,OrderedQuantity=line.OrderedQuantity-line.CancelledQuantity,PlannedQuantity=_context.PrdProductionPlanOrderAllocations.Where(a=>a.CustomerOrderLineId==line.ID&&a.IsDelete!=true).Sum(a=>(decimal?)a.PlannedQuantity)??0,Unit=unit.Name,RequestedDeliveryDate=line.RequestedDeliveryDate,Notes=line.Notes}).ToListAsync(ct);
        return View(model);
    }

    [HttpGet,Authorize(Policy="UretimSiparisDuzenle")]
    public async Task<IActionResult> Duzenle(int id,CancellationToken ct)
    {
        ViewBag.Modul="UretimSiparis";var order=await _context.PrdCustomerOrders.AsNoTracking().FirstOrDefaultAsync(x=>x.ID==id&&x.Status==PrdCustomerOrderStatus.Draft&&x.IsDelete!=true,ct);if(order==null){TempData["error"]="Yalnızca taslak sipariş düzenlenebilir.";return RedirectToAction(nameof(Detay),new{id});}
        var model=new ProductionCustomerOrderFormVM{Id=order.ID,OrderNumber=order.OrderNumber,ExternalOrderNumber=order.ExternalOrderNumber,CustomerCode=order.CustomerCode,CustomerName=order.CustomerName,OrderDate=order.OrderDate,RequestedDeliveryDate=order.RequestedDeliveryDate,Priority=order.Priority,Notes=order.Notes,Lines=await _context.PrdCustomerOrderLines.AsNoTracking().Where(x=>x.CustomerOrderId==id&&x.IsDelete!=true).OrderBy(x=>x.Sequence).Select(x=>new ProductionCustomerOrderLineFormVM{Id=x.ID,ProductMaterialId=x.ProductMaterialId,Quantity=x.OrderedQuantity,RequestedDeliveryDate=x.RequestedDeliveryDate,Notes=x.Notes}).ToListAsync(ct)};
        await FillProductsAsync(model,ct);return View("Form",model);
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="UretimSiparisDuzenle")]
    public async Task<IActionResult> Duzenle(ProductionCustomerOrderFormVM model,CancellationToken ct)
    {
        if(!model.Id.HasValue)return BadRequest();ViewBag.Modul="UretimSiparis";var order=await _context.PrdCustomerOrders.FirstOrDefaultAsync(x=>x.ID==model.Id&&x.Status==PrdCustomerOrderStatus.Draft&&x.IsDelete!=true,ct);if(order==null){TempData["error"]="Yalnızca taslak sipariş düzenlenebilir.";return RedirectToAction(nameof(Index));}
        var validLines=await ValidateAndResolveLinesAsync(model,ct);if(!ModelState.IsValid){model.OrderNumber=order.OrderNumber;await FillProductsAsync(model,ct);return View("Form",model);}
        var now=DateTime.Now;var actor=User.Identity?.Name;order.ExternalOrderNumber=Clean(model.ExternalOrderNumber);order.CustomerCode=Clean(model.CustomerCode);order.CustomerName=model.CustomerName.Trim();order.OrderDate=model.OrderDate.Date;order.RequestedDeliveryDate=model.RequestedDeliveryDate.Date;order.Priority=model.Priority;order.Notes=Clean(model.Notes);order.UpdateDate=now;order.UpdateUserID=actor;
        var oldLines=await _context.PrdCustomerOrderLines.Where(x=>x.CustomerOrderId==order.ID&&x.IsDelete!=true).ToListAsync(ct);foreach(var old in oldLines){old.IsDelete=true;old.IsActive=false;old.DeleteDate=now;old.DeleteUserID=actor;}
        var sequence=1;foreach(var line in validLines)_context.PrdCustomerOrderLines.Add(new PrdCustomerOrderLine{CustomerOrderId=order.ID,Sequence=sequence++,ProductMaterialId=line.ProductMaterialId,UnitId=line.UnitId,OrderedQuantity=line.Quantity,RequestedDeliveryDate=line.RequestedDeliveryDate,Notes=line.Notes,IsActive=true,IsDelete=false,CreateDate=now,CreateUserID=actor});
        await _context.SaveChangesAsync(ct);TempData["success"]="Sipariş güncellendi.";return RedirectToAction(nameof(Detay),new{id=order.ID});
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="UretimSiparisOnayla")]
    public async Task<IActionResult> PlanlamayaHazirla(int id,CancellationToken ct)
    {
        var order=await _context.PrdCustomerOrders.FirstOrDefaultAsync(x=>x.ID==id&&x.Status==PrdCustomerOrderStatus.Draft&&x.IsDelete!=true,ct);if(order==null){TempData["error"]="Planlamaya gönderilebilecek taslak sipariş bulunamadı.";return RedirectToAction(nameof(Detay),new{id});}
        if(!await _context.PrdCustomerOrderLines.AnyAsync(x=>x.CustomerOrderId==id&&x.IsDelete!=true&&x.OrderedQuantity>x.CancelledQuantity,ct)){TempData["error"]="Siparişte planlanabilir satır yok.";return RedirectToAction(nameof(Detay),new{id});}
        order.Status=PrdCustomerOrderStatus.ReadyForPlanning;order.SubmittedDate=DateTime.Now;order.SubmittedUserId=User.Identity?.Name;order.UpdateDate=DateTime.Now;order.UpdateUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);TempData["success"]="Sipariş üretim planlama havuzuna gönderildi.";return RedirectToAction(nameof(Detay),new{id});
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="UretimSiparisIptal")]
    public async Task<IActionResult> Iptal(int id,string? reason,CancellationToken ct)
    {
        var order=await _context.PrdCustomerOrders.FirstOrDefaultAsync(x=>x.ID==id&&x.IsDelete!=true,ct);if(order==null)return NotFound();
        if(string.IsNullOrWhiteSpace(reason)||reason.Trim().Length<5){TempData["error"]="İptal nedeni en az 5 karakter olmalıdır.";return RedirectToAction(nameof(Detay),new{id});}
        var hasPlan=await(from line in _context.PrdCustomerOrderLines where line.CustomerOrderId==id&&line.IsDelete!=true join allocation in _context.PrdProductionPlanOrderAllocations.Where(x=>x.IsDelete!=true) on line.ID equals allocation.CustomerOrderLineId select allocation.ID).AnyAsync(ct);if(hasPlan){TempData["error"]="Üretim planına bağlanmış sipariş doğrudan iptal edilemez; önce ilgili üretim planı geri alınmalıdır.";return RedirectToAction(nameof(Detay),new{id});}
        order.Status=PrdCustomerOrderStatus.Cancelled;order.IsActive=false;order.CancelledDate=DateTime.Now;order.CancelledUserId=User.Identity?.Name;order.CancellationReason=reason.Trim();order.UpdateDate=DateTime.Now;order.UpdateUserID=User.Identity?.Name;await _context.SaveChangesAsync(ct);TempData["success"]="Sipariş iptal edildi.";return RedirectToAction(nameof(Detay),new{id});
    }

    [HttpGet,Authorize(Policy="UretimSiparisPlanla")]
    public async Task<IActionResult> PlanlamaHavuzu(string? search,CancellationToken ct)
    {
        ViewBag.Modul="UretimSiparis";var orderRows=await(from line in _context.PrdCustomerOrderLines.AsNoTracking() join order in _context.PrdCustomerOrders.AsNoTracking() on line.CustomerOrderId equals order.ID join product in _context.PrdMaterials.AsNoTracking() on line.ProductMaterialId equals product.ID join unit in _context.PrdUnits.AsNoTracking() on line.UnitId equals unit.ID where line.IsDelete!=true&&order.IsDelete!=true&&(order.Status==PrdCustomerOrderStatus.ReadyForPlanning||order.Status==PrdCustomerOrderStatus.PartiallyPlanned) select new{Line=line,Order=order,Product=product,Unit=unit}).ToListAsync(ct);
        if(!string.IsNullOrWhiteSpace(search)){var term=search.Trim();orderRows=orderRows.Where(x=>x.Order.OrderNumber.Contains(term,StringComparison.OrdinalIgnoreCase)||x.Order.CustomerName.Contains(term,StringComparison.OrdinalIgnoreCase)||x.Product.Code.Contains(term,StringComparison.OrdinalIgnoreCase)||x.Product.Name.Contains(term,StringComparison.OrdinalIgnoreCase)).ToList();}
        var lineIds=orderRows.Select(x=>x.Line.ID).ToList();var allocations=await _context.PrdProductionPlanOrderAllocations.AsNoTracking().Where(x=>lineIds.Contains(x.CustomerOrderLineId)&&x.IsDelete!=true).GroupBy(x=>x.CustomerOrderLineId).Select(x=>new{LineId=x.Key,Quantity=x.Sum(a=>a.PlannedQuantity)}).ToDictionaryAsync(x=>x.LineId,x=>x.Quantity,ct);
        var productIds=orderRows.Select(x=>x.Product.ID).Distinct().ToList();var recipes=await(from version in _context.PrdRecipeVersions.AsNoTracking() join recipe in _context.PrdRecipes.AsNoTracking() on version.RecipeId equals recipe.ID where productIds.Contains(recipe.ProductMaterialId)&&version.Status==PrdRecipeStatus.Active&&version.IsDelete!=true&&recipe.IsDelete!=true orderby version.VersionNumber descending select new{recipe.ProductMaterialId,VersionId=version.ID}).ToListAsync(ct);var recipeByProduct=recipes.GroupBy(x=>x.ProductMaterialId).ToDictionary(x=>x.Key,x=>x.First().VersionId);
        var warehouseIds=await _context.PrdWarehouses.AsNoTracking().Where(x=>x.Type==PrdWarehouseType.FinishedProduct&&x.IsDelete!=true&&x.IsActive!=false).Select(x=>x.ID).ToListAsync(ct);var stocks=await _context.PrdStockMovements.AsNoTracking().Where(x=>productIds.Contains(x.MaterialId)&&warehouseIds.Contains(x.WarehouseId)&&x.IsDelete!=true).GroupBy(x=>new{x.MaterialId,x.UnitId}).Select(x=>new{x.Key.MaterialId,x.Key.UnitId,Quantity=x.Sum(m=>m.Direction==PrdStockDirection.In?m.Quantity:-m.Quantity)}).ToListAsync(ct);
        var model=new ProductionOrderPlanningPoolVM{Search=search,Lines=orderRows.Where(x=>recipeByProduct.ContainsKey(x.Product.ID)).Select(x=>{var planned=allocations.GetValueOrDefault(x.Line.ID);return new ProductionOrderPlanningPoolLineVM{OrderLineId=x.Line.ID,OrderId=x.Order.ID,OrderNumber=x.Order.OrderNumber,CustomerName=x.Order.CustomerName,RequestedDeliveryDate=x.Line.RequestedDeliveryDate??x.Order.RequestedDeliveryDate,Priority=x.Order.Priority,RecipeVersionId=recipeByProduct[x.Product.ID],ProductCode=x.Product.Code,ProductName=x.Product.Name,OrderedQuantity=x.Line.OrderedQuantity-x.Line.CancelledQuantity,PlannedQuantity=planned,RemainingQuantity=Math.Max(0,x.Line.OrderedQuantity-x.Line.CancelledQuantity-planned),CurrentFinishedStock=stocks.Where(s=>s.MaterialId==x.Product.ID&&s.UnitId==x.Line.UnitId).Sum(s=>s.Quantity),Unit=x.Unit.Name};}).Where(x=>x.RemainingQuantity>0).OrderBy(x=>x.RequestedDeliveryDate).ThenByDescending(x=>x.Priority).ToList()};return View(model);
    }

    [HttpPost,ValidateAntiForgeryToken,Authorize(Policy="UretimSiparisPlanla")]
    public async Task<IActionResult> PlanlamayaAktar(ProductionOrderPlanningSelectionVM model,CancellationToken ct)
    {
        var selected=model.Lines.Where(x=>x.Selected&&x.OrderLineId>0&&x.Quantity>0).ToList();if(selected.Count==0){TempData["error"]="Planlamaya aktarmak için en az bir sipariş satırı ve miktar seçiniz.";return RedirectToAction(nameof(PlanlamaHavuzu));}
        var ids=selected.Select(x=>x.OrderLineId).Distinct().ToList();var rows=await(from line in _context.PrdCustomerOrderLines.AsNoTracking() join order in _context.PrdCustomerOrders.AsNoTracking() on line.CustomerOrderId equals order.ID join recipe in _context.PrdRecipes.AsNoTracking() on line.ProductMaterialId equals recipe.ProductMaterialId join version in _context.PrdRecipeVersions.AsNoTracking() on recipe.ID equals version.RecipeId where ids.Contains(line.ID)&&line.IsDelete!=true&&order.IsDelete!=true&&(order.Status==PrdCustomerOrderStatus.ReadyForPlanning||order.Status==PrdCustomerOrderStatus.PartiallyPlanned)&&version.Status==PrdRecipeStatus.Active&&version.IsDelete!=true orderby version.VersionNumber descending select new{Line=line,VersionId=version.ID}).ToListAsync(ct);
        var resolved=rows.GroupBy(x=>x.Line.ID).ToDictionary(x=>x.Key,x=>x.First());var allocated=await _context.PrdProductionPlanOrderAllocations.AsNoTracking().Where(x=>ids.Contains(x.CustomerOrderLineId)&&x.IsDelete!=true).GroupBy(x=>x.CustomerOrderLineId).Select(x=>new{Id=x.Key,Quantity=x.Sum(a=>a.PlannedQuantity)}).ToDictionaryAsync(x=>x.Id,x=>x.Quantity,ct);
        var list=ReadPlanningList();foreach(var selection in selected)
        {
            if(!resolved.TryGetValue(selection.OrderLineId,out var row)){TempData["error"]="Seçilen sipariş satırlarından biri artık planlanabilir değil veya aktif reçetesi yok.";return RedirectToAction(nameof(PlanlamaHavuzu));}
            var alreadyInSession=list.SelectMany(x=>x.OrderSources).Where(x=>x.CustomerOrderLineId==selection.OrderLineId).Sum(x=>x.Quantity);var remaining=row.Line.OrderedQuantity-row.Line.CancelledQuantity-allocated.GetValueOrDefault(row.Line.ID)-alreadyInSession;if(selection.Quantity>remaining){TempData["error"]=$"Bir satır için girilen miktar kalan {remaining:0.######} miktarını aşıyor.";return RedirectToAction(nameof(PlanlamaHavuzu));}
            var planItem=list.FirstOrDefault(x=>x.RecipeVersionId==row.VersionId);if(planItem==null){planItem=new ProductionPlanningSessionItem{RecipeVersionId=row.VersionId};list.Add(planItem);}planItem.Quantity+=selection.Quantity;var source=planItem.OrderSources.FirstOrDefault(x=>x.CustomerOrderLineId==row.Line.ID);if(source==null)planItem.OrderSources.Add(new ProductionPlanningOrderSourceSessionItem{CustomerOrderLineId=row.Line.ID,Quantity=selection.Quantity});else source.Quantity+=selection.Quantity;
        }
        SavePlanningList(list);TempData["success"]=$"{selected.Count} sipariş satırı üretim planlama listesine aktarıldı.";return RedirectToAction("Planlama","UretimYonetimi");
    }

    private async Task<List<ResolvedOrderLine>> ValidateAndResolveLinesAsync(ProductionCustomerOrderFormVM model,CancellationToken ct)
    {
        model.CustomerName=model.CustomerName?.Trim()??string.Empty;if(model.RequestedDeliveryDate.Date<model.OrderDate.Date)ModelState.AddModelError(nameof(model.RequestedDeliveryDate),"Teslim tarihi sipariş tarihinden önce olamaz.");
        model.Lines=model.Lines.Where(x=>x.ProductMaterialId>0||x.Quantity!=0||!string.IsNullOrWhiteSpace(x.Notes)).ToList();if(model.Lines.Count==0)ModelState.AddModelError(nameof(model.Lines),"En az bir ürün satırı ekleyiniz.");if(model.Lines.Any(x=>x.ProductMaterialId<=0))ModelState.AddModelError(nameof(model.Lines),"Tüm sipariş satırlarında ürün seçilmelidir.");if(model.Lines.Any(x=>x.Quantity<=0))ModelState.AddModelError(nameof(model.Lines),"Tüm satır miktarları sıfırdan büyük olmalıdır.");if(model.Lines.GroupBy(x=>x.ProductMaterialId).Any(x=>x.Key>0&&x.Count()>1))ModelState.AddModelError(nameof(model.Lines),"Aynı ürün siparişte bir kez yer alabilir.");
        var ids=model.Lines.Select(x=>x.ProductMaterialId).Where(x=>x>0).Distinct().ToList();
        var products=await _context.PrdMaterials.AsNoTracking()
            .Where(product=>ids.Contains(product.ID)&&product.IsDelete!=true&&product.IsActive!=false&&
                (product.Type==PrdMaterialType.FinishedProduct||_context.PrdRecipes.Any(recipe=>recipe.ProductMaterialId==product.ID&&recipe.IsDelete!=true)))
            .Select(product=>new{product.ID,product.UnitId})
            .ToDictionaryAsync(x=>x.ID,x=>x.UnitId,ct);
        if(ids.Any(id=>!products.ContainsKey(id)))ModelState.AddModelError(nameof(model.Lines),"Seçilen ürünlerden biri aktif mamul kartı veya reçete ürünü değil.");
        return model.Lines.Where(x=>products.ContainsKey(x.ProductMaterialId)).Select(x=>new ResolvedOrderLine(x.ProductMaterialId,products[x.ProductMaterialId],x.Quantity,x.RequestedDeliveryDate?.Date,Clean(x.Notes))).ToList();
    }

    private async Task FillProductsAsync(ProductionCustomerOrderFormVM model,CancellationToken ct)
    {
        model.Products=await _context.PrdMaterials.AsNoTracking()
            .Where(product=>product.IsDelete!=true&&product.IsActive!=false&&
                (product.Type==PrdMaterialType.FinishedProduct||_context.PrdRecipes.Any(recipe=>recipe.ProductMaterialId==product.ID&&recipe.IsDelete!=true)))
            .OrderBy(product=>product.Code)
            .Select(product=>new SelectListItem{Value=product.ID.ToString(),Text=product.Code+" - "+product.Name})
            .ToListAsync(ct);
    }

    private List<ProductionPlanningSessionItem> ReadPlanningList(){var json=HttpContext.Session.GetString(PlanningSessionKey);return string.IsNullOrWhiteSpace(json)?[]:JsonSerializer.Deserialize<List<ProductionPlanningSessionItem>>(json)??[];}
    private void SavePlanningList(List<ProductionPlanningSessionItem> list)=>HttpContext.Session.SetString(PlanningSessionKey,JsonSerializer.Serialize(list));
    private static string? Clean(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
    private sealed record ResolvedOrderLine(int ProductMaterialId,int UnitId,decimal Quantity,DateTime? RequestedDeliveryDate,string? Notes);
}
