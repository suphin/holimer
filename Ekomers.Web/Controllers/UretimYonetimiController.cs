using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrUretim")]
public sealed class UretimYonetimiController : Controller
{
    public IActionResult Dashboard() => ModulSayfasi("Üretim Paneli", "Üretim sürecinin genel görünümü bu ekranda yer alacak.");
    public IActionResult Planlama() => ModulSayfasi("Üretim Planlama", "Reçeteden üretim planı oluşturma ekranı hazırlanıyor.");
    public IActionResult Emirler() => ModulSayfasi("Üretim Emirleri", "Onaylanan üretim emirleri bu ekranda yönetilecek.");
    public IActionResult DepoHazirlama() => ModulSayfasi("Depo Hazırlama Emirleri", "Yeni Prd depo hazırlama görevleri bu ekranda yönetilecek.");
    public IActionResult Gerceklesen() => ModulSayfasi("Gerçekleşen Üretim", "Gerçek tüketim, iade, fire ve çıktı kayıtları bu ekranda yönetilecek.");
    public IActionResult Izlenebilirlik() => ModulSayfasi("Lot ve Parti İzlenebilirliği", "Hammadde lotundan mamul partisine izleme bu ekranda yapılacak.");
    public IActionResult Raporlar() => ModulSayfasi("Üretim Raporları", "Planlanan ve gerçekleşen üretim karşılaştırmaları burada yer alacak.");

    private IActionResult ModulSayfasi(string baslik, string aciklama)
    {
        ViewBag.Modul = "YeniUretim";
        ViewBag.Baslik = baslik;
        ViewBag.Aciklama = aciklama;
        return View("ModulSayfasi");
    }
}
