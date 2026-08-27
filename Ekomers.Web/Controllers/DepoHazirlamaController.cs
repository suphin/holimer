using Ekomers.Data;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = "AdminOrStok")]
public sealed class DepoHazirlamaController : Controller
{
    private readonly ApplicationDbContext _context;

    public DepoHazirlamaController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Modul = "Stok";

        var model = await (
            from emir in _context.DepoHazirlamaEmri.AsNoTracking()
            join uretim in _context.Uretim.AsNoTracking() on emir.UretimID equals uretim.ID
            join urun in _context.Malzeme.AsNoTracking() on uretim.UrunID equals urun.ID into urunler
            from urun in urunler.DefaultIfEmpty()
            join kaynak in _context.MalzemeDepo.AsNoTracking() on emir.KaynakDepoID equals kaynak.ID into kaynakDepolar
            from kaynak in kaynakDepolar.DefaultIfEmpty()
            join hedef in _context.MalzemeDepo.AsNoTracking() on emir.HedefDepoID equals hedef.ID into hedefDepolar
            from hedef in hedefDepolar.DefaultIfEmpty()
            where emir.IsDelete != true
            orderby emir.TalepTarihi descending, emir.ID descending
            select new DepoHazirlamaEmriListeVM
            {
                ID = emir.ID,
                UretimID = uretim.ID,
                UretimEmriNo = uretim.UretimEmriNo,
                UrunKod = urun != null ? urun.Kod : null,
                UrunAd = urun != null ? urun.Ad : null,
                KaynakDepo = kaynak != null ? kaynak.Ad : null,
                HedefDepo = hedef != null ? hedef.Ad : null,
                Durum = emir.Durum,
                TalepTarihi = emir.TalepTarihi,
                PlanlananUretimTarihi = uretim.PlanlananUretimTarihi,
                KalemSayisi = _context.DepoHazirlamaKalem.Count(x =>
                    x.DepoHazirlamaEmriID == emir.ID && x.IsDelete != true)
            }).ToListAsync(cancellationToken);

        return View(model);
    }
}
