using Ekomers.Models.ViewModels.Purchasing;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers;

[Authorize(Policy="SatinalmaGoruntule")]
public sealed class SatinalmaAramaController(PurchasingSearchService search):Controller
{
    [HttpGet("/SatinalmaYonetimi/Arama")]
    public async Task<IActionResult> Index([FromQuery] PurchasingSearchVM model,CancellationToken ct)
    {
        ViewBag.Modul="YeniSatinalma";
        model.Search=model.Search?.Trim();model.Product=model.Product?.Trim();model.Supplier=model.Supplier?.Trim();
        if(model.Run)
        {
            if(!new[]{model.Search,model.Product,model.Supplier}.Any(x=>!string.IsNullOrEmpty(x))&&!model.MaterialId.HasValue&&!model.SupplierId.HasValue)
                ModelState.AddModelError("Search","Ürün veya firma kodu/adı giriniz. Belge ve lot numarasıyla da arayabilirsiniz.");
            foreach(var (name,value) in new[]{(nameof(model.Search),model.Search),(nameof(model.Product),model.Product),(nameof(model.Supplier),model.Supplier)})
                if(!string.IsNullOrEmpty(value)&&value.Length<2)ModelState.AddModelError(name,"Arama için en az 2 karakter giriniz.");
            if(model.StartDate.HasValue&&model.EndDate.HasValue&&model.StartDate.Value.Date>model.EndDate.Value.Date)ModelState.AddModelError("EndDate","Bitiş tarihi başlangıçtan önce olamaz.");
            if(model.EndDate?.Year==9999)ModelState.AddModelError("EndDate","Geçerli bir bitiş tarihi giriniz.");
            if(model.Stage.HasValue&&!Enum.IsDefined(model.Stage.Value))ModelState.AddModelError("Stage","Geçerli bir işlem türü seçiniz.");
            if(model.MaterialId<=0||model.SupplierId<=0)ModelState.AddModelError("Search","Geçerli ürün/firma seçiniz.");
            if(ModelState.IsValid)await search.SearchAsync(model,ct);
        }
        return View(model);
    }
}
