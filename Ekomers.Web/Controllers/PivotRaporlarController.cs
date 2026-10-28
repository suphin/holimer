using System.Text.Json;
using Ekomers.Data;
using Ekomers.Data.Services.Reports;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels.Raporlar;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = ReportPermissions.PivotRaporlar)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PivotRaporlarController(ApplicationDbContext context, ILogoPivotService source, ILogger<PivotRaporlarController> logger) : Controller
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private void Module() => ViewBag.Modul = "Rapor";
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        Module();
        return View(await context.RptPivotReports.AsNoTracking().Where(r => r.IsActive).OrderBy(r => r.Name).ToListAsync(ct));
    }
    [HttpGet, Authorize(Policy = ReportPermissions.PivotRaporlarYonet)]
    public async Task<IActionResult> Yonetim(CancellationToken ct)
    {
        Module(); return View(await context.RptPivotReports.AsNoTracking().OrderBy(r => r.Name).ToListAsync(ct));
    }
    [HttpGet, Authorize(Policy = ReportPermissions.PivotRaporlarYonet)]
    public async Task<IActionResult> Duzenle(int id, CancellationToken ct)
    {
        Module();
        if (id == 0) return View(new PivotEditVM());
        var report = await context.RptPivotReports.AsNoTracking().SingleOrDefaultAsync(r => r.ID == id, ct);
        return report == null ? NotFound() : View(new PivotEditVM { Id = report.ID, Name = report.Name, SchemaName = report.SchemaName, ViewName = report.ViewName, IsActive = report.IsActive, SettingsJson = report.SettingsJson, Version = Convert.ToBase64String(report.Version) });
    }
    [HttpPost, ValidateAntiForgeryToken, Authorize(Policy = ReportPermissions.PivotRaporlarYonet)]
    [RequestSizeLimit(65536)]
    public async Task<IActionResult> Duzenle(PivotEditVM model, CancellationToken ct)
    {
        Module();
        if (!ModelState.IsValid) return View(model);
        try
        {
            var settings = JsonSerializer.Deserialize<PivotSettingsVM>(model.SettingsJson, JsonOptions) ?? throw new ArgumentException("Alanları ve varsayılan düzeni seçiniz.");
            var fields = await source.ColumnsAsync(model.SchemaName, model.ViewName, ct);
            LogoPivotService.Validate(settings, settings.Layout, fields);
            var report = model.Id == 0 ? new RptPivotReport() : await context.RptPivotReports.SingleOrDefaultAsync(r => r.ID == model.Id, ct);
            if (report == null) return NotFound();
            if (model.Id != 0)
            {
                byte[] version;
                try { version = Convert.FromBase64String(model.Version ?? ""); } catch (FormatException) { throw new ArgumentException("Kayıt sürümü geçersiz; sayfayı yenileyiniz."); }
                if (version.Length != 8) throw new ArgumentException("Kayıt sürümü geçersiz; sayfayı yenileyiniz.");
                context.Entry(report).Property(r => r.Version).OriginalValue = version;
            }
            report.Name = model.Name.Trim(); report.SchemaName = model.SchemaName; report.ViewName = model.ViewName;
            report.SettingsJson = JsonSerializer.Serialize(settings); report.IsActive = model.IsActive;
            report.UpdatedAt = DateTime.UtcNow; report.UpdatedBy = (User.Identity?.Name ?? "")[..Math.Min(256, User.Identity?.Name?.Length ?? 0)];
            if (model.Id == 0) context.RptPivotReports.Add(report);
            await context.SaveChangesAsync(ct);
            TempData["PivotSuccess"] = "Rapor tanımı kaydedildi.";
            return RedirectToAction(nameof(Yonetim));
        }
        catch (DbUpdateConcurrencyException) { ModelState.AddModelError("", "Başka bir yönetici bu kaydı değiştirdi. Kaydetmeden önce sayfayı yenileyiniz."); }
        catch (Exception ex) when (ex is ArgumentException or JsonException) { ModelState.AddModelError("", ex is JsonException ? "Rapor düzeni geçersiz. Alanları yeniden seçiniz." : ex.Message); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Pivot rapor tanımı kaydedilemedi {Id}", model.Id); ModelState.AddModelError("", "Rapor kaydedilemedi. Logo bağlantısını ve portal migration durumunu kontrol ediniz."); }
        return View(model);
    }
    [HttpGet, Authorize(Policy = ReportPermissions.PivotRaporlarYonet)]
    public async Task<IActionResult> Alanlar(string schema, string view, CancellationToken ct)
    {
        try { return Json(await source.ColumnsAsync(schema, view, ct)); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) when (ex is not OperationCanceledException) { logger.LogError(ex, "Pivot view metadata okunamadı"); return StatusCode(503, new { error = "View alanları okunamadı. Logo bağlantısını ve view yetkisini kontrol ediniz." }); }
    }
    [HttpGet]
    public async Task<IActionResult> Rapor(int id, CancellationToken ct)
    {
        Module();
        var report = await context.RptPivotReports.AsNoTracking().SingleOrDefaultAsync(r => r.ID == id && r.IsActive, ct);
        if (report == null) return NotFound();
        var page = new PivotPageVM { Id = id, Name = report.Name, Source = report.SchemaName + "." + report.ViewName };
        try
        {
            page.Settings = JsonSerializer.Deserialize<PivotSettingsVM>(report.SettingsJson, JsonOptions) ?? throw new ArgumentException("Rapor tanımı geçersiz.");
            page.Columns = LogoPivotService.Validate(page.Settings, page.Settings.Layout, await source.ColumnsAsync(report.SchemaName, report.ViewName, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { logger.LogError(ex, "Pivot rapor alanları okunamadı {Id}", id); page.Error = "Rapor alanları okunamadı veya kaynak view değişti. Yönetici tanımını kontrol ediniz."; }
        return View(page);
    }
    [HttpPost, ValidateAntiForgeryToken, RequestSizeLimit(32768)]
    public async Task<IActionResult> Veri(int id, [FromBody] PivotLayoutVM layout, CancellationToken ct)
    {
        if (!ModelState.IsValid || layout == null) return BadRequest(new { error = "Pivot seçimleri geçersiz." });
        var report = await context.RptPivotReports.AsNoTracking().SingleOrDefaultAsync(r => r.ID == id && r.IsActive, ct);
        if (report == null) return NotFound(new { error = "Aktif rapor bulunamadı." });
        try
        {
            var settings = JsonSerializer.Deserialize<PivotSettingsVM>(report.SettingsJson, JsonOptions) ?? throw new ArgumentException("Rapor tanımı geçersiz.");
            return Json(await source.RunAsync(report.SchemaName, report.ViewName, settings, layout, ct));
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex) when (ex is not OperationCanceledException)
        { logger.LogError(ex, "Pivot verileri okunamadı {Id}", id); return StatusCode(503, new { error = "Pivot hesaplanamadı. Bağlantıyı kontrol ediniz veya filtreleri daraltınız. Eksik toplam gösterilmedi." }); }
    }
}
