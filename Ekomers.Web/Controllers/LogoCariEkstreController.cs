using Ekomers.Data.Services.Reports;
using Ekomers.Models.ViewModels.Raporlar;
using Ekomers.Web.Infrastructure.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Data.SqlClient;

namespace Ekomers.Web.Controllers;

[Authorize(Policy = ReportPermissions.LogoCariEkstre)]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class LogoCariEkstreController(ILogoCustomerStatementService reports,ILogger<LogoCariEkstreController> logger) : Controller
{
    public override Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ViewBag.Modul="Rapor";
        return base.OnActionExecutionAsync(context,next);
    }
    [HttpGet]
    public async Task<IActionResult> Index(int firmNumber=0,int periodNumber=0,DateTime? startDate=null,DateTime? endDate=null,string? search=null,bool includePassive=false,bool onlyBalance=false,int page=1,CancellationToken ct=default)
    {
        var model=new LogoCariRaporVM { FirmNumber=firmNumber,PeriodNumber=periodNumber,StartDate=startDate,EndDate=endDate,Search=search?.Trim() ?? "",IncludePassive=includePassive,OnlyBalance=onlyBalance,Page=page };
        if(!ModelState.IsValid || page is <1 or >1000000 || model.Search.Length>150) return BadRequest("Filtre veya sayfa bilgisi geçersiz.");
        try
        {
            model.Companies=await reports.CompaniesAsync(ct);
            if(model.Companies.Count==0) { model.Error="Logo'da raporlanacak firma bulunamadı.";return View(model); }
            if(firmNumber==0) firmNumber=model.Companies[0].Number;
            model.FirmNumber=firmNumber;
            if(!model.Companies.Any(x=>x.Number==firmNumber)) return NotFound();
            model.Periods=await reports.PeriodsAsync(firmNumber,ct);
            if(periodNumber==0 && model.Periods.Count>0) model.PeriodNumber=model.Periods[0].Number;
            model.Scope=await reports.ScopeAsync(firmNumber,periodNumber,startDate,endDate,ct);
            var data=await reports.AccountsAsync(model.Scope,model.Search,includePassive,onlyBalance,page,ct);
            data.Companies=model.Companies;data.Periods=model.Periods;return View(data);
        }
        catch(ArgumentException ex) { model.Error=ex.Message;return View(model); }
        catch(KeyNotFoundException ex) { model.Error=ex.Message;return View(model); }
        catch(SqlException ex) { logger.LogError(ex,"Logo cari hesap raporu okunamadı; firma {Firm}, dönem {Period}",firmNumber,periodNumber);model.Error="Logo cari hesap verileri okunamadı. Bağlantıyı ve seçilen firmanın dönem tablolarını kontrol ediniz.";return View(model); }
    }
    [HttpGet]
    public async Task<IActionResult> Ekstre(int firmNumber,int periodNumber,int clientRef,DateTime startDate,DateTime endDate,int page=1,CancellationToken ct=default)
    {
        if(!ValidRequest(firmNumber,periodNumber,clientRef,startDate,endDate,page)) return BadRequest();
        try
        {
            var scope=await reports.ScopeAsync(firmNumber,periodNumber,startDate,endDate,ct);
            var model=await reports.StatementAsync(scope,clientRef,page,ct);
            Response.Headers["X-Logo-Cari-Partial"]="Ekstre";return PartialView("_Ekstre",model);
        }
        catch(ArgumentException) { return BadRequest(); }
        catch(KeyNotFoundException) { return NotFound(); }
        catch(SqlException ex) { logger.LogError(ex,"Logo cari ekstre okunamadı; firma {Firm}, cari {Client}",firmNumber,clientRef);return StatusCode(503); }
    }
    [HttpGet]
    public async Task<IActionResult> BelgeDetay(int firmNumber,int periodNumber,int clientRef,int movementRef,DateTime startDate,DateTime endDate,int page=1,CancellationToken ct=default)
    {
        if(movementRef<=0 || !ValidRequest(firmNumber,periodNumber,clientRef,startDate,endDate,page)) return BadRequest();
        try
        {
            var scope=await reports.ScopeAsync(firmNumber,periodNumber,startDate,endDate,ct);
            var model=await reports.DocumentAsync(scope,clientRef,movementRef,page,ct);
            Response.Headers["X-Logo-Cari-Partial"]="Belge";return PartialView("_Belge",model);
        }
        catch(ArgumentException) { return BadRequest(); }
        catch(KeyNotFoundException) { return NotFound(); }
        catch(SqlException ex) { logger.LogError(ex,"Logo kaynak belge okunamadı; firma {Firm}, cari {Client}, hareket {Movement}",firmNumber,clientRef,movementRef);return StatusCode(503); }
    }
    private bool ValidRequest(int firm,int period,int client,DateTime start,DateTime end,int page) =>
        ModelState.IsValid && firm is >=1 and <=999 && period is >=1 and <=99 && client>0 && page is >=1 and <=1000000 && start>=new DateTime(1900,1,1) && end>=start && end<DateTime.MaxValue.Date;
}
