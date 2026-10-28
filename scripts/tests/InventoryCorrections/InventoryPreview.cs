using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Ekomers.Data;
using Ekomers.Web.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

static class InventoryPreview
{
    public static async Task Run(string connection,int documentId,bool serve)
    {
        var root=new DirectoryInfo(Directory.GetCurrentDirectory());while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Ekomers.Web","wwwroot")))root=root.Parent;
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddScoped<ApplicationDbContext>(_=>new CorrectionContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options));
        builder.Services.AddControllersWithViews(o=>o.Filters.Add<PreviewViewFilter>()).AddApplicationPart(typeof(ProductionInventoryController).Assembly);builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions,FixtureUser>("Fixture",_=>{});
        builder.Services.AddAuthorization(o=>{o.AddPolicy("AdminOrUretim",p=>p.RequireAssertion(c=>c.User.IsInRole("Admin")||c.User.HasClaim("Modul","Uretim")));o.AddPolicy("StokBelgesiSil",p=>p.RequireRole("Admin"));});
        await using var app=builder.Build();app.UseDeveloperExceptionPage();app.UseStaticFiles(new StaticFileOptions{FileProvider=new PhysicalFileProvider(Path.Combine(root!.FullName,"Ekomers.Web","wwwroot"))});app.UseAuthentication();app.UseAuthorization();
        app.MapGet("/fixture-stop",(IHostApplicationLifetime lifetime)=>{lifetime.StopApplication();return "Isolated preview stopped.";});app.MapDefaultControllerRoute();await app.StartAsync();
        var url=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();using var client=new HttpClient();
        var list=await client.GetStringAsync(url+"/ProductionInventory?productSearch=PIECES");if(!list.Contains("inventoryProductSearch")||!list.Contains("value=\"PIECES\"")||!list.Contains("1 belge bulundu"))throw new Exception("FAIL Product search form/results");Console.WriteLine("PASS Inventory list Razor preserves product filter and displays matching document count");
        if(!WebUtility.HtmlDecode(list).Contains("Eşleşen Ürün / Miktar")||!list.Contains("20 Adet"))throw new Exception("FAIL Matching product quantity and unit are missing");Console.WriteLine("PASS Inventory search Razor renders matching product quantity and unit");
        list=await client.GetStringAsync(url+"/ProductionInventory?productSearch=NO-SUCH-PRODUCT");if(!WebUtility.HtmlDecode(list).Contains("stok hareket belgesi bulunamadı."))throw new Exception("FAIL Empty product search message");Console.WriteLine("PASS Inventory list renders a specific empty-search message");
        foreach(var action in new[]{"Detay","Duzenle"}){var response=await client.GetAsync(url+"/ProductionInventory/"+action+"/"+documentId);if(response.StatusCode!=HttpStatusCode.OK)throw new Exception("FAIL compiled view "+action+": "+await response.Content.ReadAsStringAsync());Console.WriteLine("PASS Compiled MVC view: "+action);}
        using var scope=app.Services.CreateScope();var database=scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var managedId=await database.PrdInventoryDocuments.AsNoTracking().Where(x=>x.SourceDocumentType=="WarehouseCount").Select(x=>x.ID).FirstAsync();var managedLineId=await database.PrdInventoryDocumentLines.AsNoTracking().Where(x=>x.InventoryDocumentId==managedId&&x.IsDelete!=true).Select(x=>x.ID).FirstAsync();
        foreach(var target in new[]{"/ProductionInventory/SatirDuzenle/"+managedId+"?lineId="+managedLineId,"/ProductionInventory/Duzenle/"+managedId}){var managedHtml=await client.GetStringAsync(url+target);if(!managedHtml.Contains("<select")||!managedHtml.Contains("Lines_0__UnitId")||WebUtility.HtmlDecode(managedHtml).Contains("Bu kaydın birimi bağlı olduğu kaynak süreçten düzeltilmelidir."))throw new Exception("FAIL Source-managed unit selector");}Console.WriteLine("PASS Source-managed row and document forms expose Admin unit selection");
        var row=await database.PrdInventoryDocumentLines.AsNoTracking().Where(x=>x.InventoryDocumentId==documentId&&x.IsDelete!=true).OrderByDescending(x=>x.ID).FirstAsync();
        var header=await database.PrdInventoryDocuments.AsNoTracking().SingleAsync(x=>x.ID==documentId);
        var editUrl=url+"/ProductionInventory/SatirDuzenle/"+documentId+"?lineId="+row.ID;
        var editHtml=await client.GetStringAsync(editUrl);if(!editHtml.Contains("Lines_0__UnitId")||editHtml.Contains("Lines_1__"))throw new Exception("FAIL Single-row form or unit selector");Console.WriteLine("PASS Compiled single-row edit form includes unit selection and only one line");
        var token=WebUtility.HtmlDecode(Regex.Match(editHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
        var values=new Dictionary<string,string>{{"__RequestVerificationToken",token},{"Id",documentId.ToString()},{"EditLineId",row.ID.ToString()},{"DocumentNumber",header.DocumentNumber},{"Lines[0].Id",row.ID.ToString()},{"Lines[0].UnitId",row.UnitId.ToString()},{"Lines[0].LotNumber",row.LotNumber??""},{"Lines[0].Quantity",row.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture)},{"Lines[0].UnitCost",(row.OriginalUnitCost??row.UnitCost).ToString(System.Globalization.CultureInfo.InvariantCulture)},{"Lines[0].Notes","HTTP tek satır testi"},{"EditReason","İzole HTTP form doğrulaması"}};
        var posted=await client.PostAsync(url+"/ProductionInventory/SatirDuzenle",new FormUrlEncodedContent(values));var postedHtml=WebUtility.HtmlDecode(await posted.Content.ReadAsStringAsync());
        if(posted.StatusCode!=HttpStatusCode.OK||!postedHtml.Contains("satırı ve bağlı stok hareketleri güncellendi.")||!await database.PrdInventoryDocumentLines.AsNoTracking().AnyAsync(x=>x.ID==row.ID&&x.Notes=="HTTP tek satır testi"))throw new Exception("FAIL Row form post: "+Regex.Replace(postedHtml,"<[^>]+>"," ").Trim()[..Math.Min(1500,Regex.Replace(postedHtml,"<[^>]+>"," ").Trim().Length)]);Console.WriteLine("PASS Row edit submits through MVC model binding, antiforgery and persistence");
        await using(var blocker=new Microsoft.Data.SqlClient.SqlConnection(connection))
        {
            await blocker.OpenAsync();await using var locked=await blocker.BeginTransactionAsync();
            await using(var hold=blocker.CreateCommand()){hold.Transaction=(Microsoft.Data.SqlClient.SqlTransaction)locked;hold.CommandText="UPDATE dbo.PrdInventoryDocument SET Notes=Notes WHERE ID=@id";hold.Parameters.AddWithValue("@id",documentId);await hold.ExecuteNonQueryAsync();}
            values["Lines[0].Notes"]="HTTP retry: <test> & değer";
            var busy=await client.PostAsync(url+"/ProductionInventory/SatirDuzenle",new FormUrlEncodedContent(values));var busyHtml=await busy.Content.ReadAsStringAsync();
            if(busy.StatusCode!=HttpStatusCode.Conflict||!WebUtility.HtmlDecode(busyHtml).Contains("Aynı Değerlerle Tekrar Dene")||!WebUtility.HtmlDecode(busyHtml).Contains("HTTP retry: <test> & değer"))throw new Exception("FAIL Compiled correction retry form");Console.WriteLine("PASS Lock contention renders a retry form preserving submitted values");
            values["__RequestVerificationToken"]=WebUtility.HtmlDecode(Regex.Match(busyHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
            await locked.RollbackAsync();
        }
        var retryResponse=await client.PostAsync(url+"/ProductionInventory/SatirDuzenle",new FormUrlEncodedContent(values));
        if(retryResponse.StatusCode!=HttpStatusCode.OK||!await database.PrdInventoryDocumentLines.AsNoTracking().AnyAsync(x=>x.ID==row.ID&&x.Notes=="HTTP retry: <test> & değer"))throw new Exception("FAIL Retry submission");Console.WriteLine("PASS Explicit HTTP retry saves preserved values after lock release");
        client.DefaultRequestHeaders.Add("X-Fixture-Reader","true");var reader=await client.GetStringAsync(url+"/ProductionInventory/Detay/"+documentId);if(reader.Contains("deleteInventoryDetailLine-"))throw new Exception("FAIL Reader sees row deletion controls");
        if(reader.Contains("SatirDuzenle")||(await client.GetAsync(editUrl)).StatusCode!=HttpStatusCode.Forbidden)throw new Exception("FAIL Reader can open row editing");
        if((await client.PostAsync(url+"/ProductionInventory/SatirDuzenle",new FormUrlEncodedContent(values))).StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.BadRequest))throw new Exception("FAIL Reader can save row editing");Console.WriteLine("PASS Row edit controls and endpoints require Admin");
        if((await client.GetAsync(url+"/ProductionInventory/Duzenle/"+documentId)).StatusCode!=HttpStatusCode.Forbidden)throw new Exception("FAIL Reader can edit units");
        if((await client.PostAsync(url+"/ProductionInventory/SatirSil",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("id",documentId.ToString())}))).StatusCode is not (HttpStatusCode.Forbidden or HttpStatusCode.BadRequest))throw new Exception("FAIL Reader can delete rows");Console.WriteLine("PASS Read-only user has no edit/delete controls or endpoint access");
        client.DefaultRequestHeaders.Remove("X-Fixture-Reader");if((await client.PostAsync(url+"/ProductionInventory/SatirSil",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("id",documentId.ToString())}))).StatusCode!=HttpStatusCode.BadRequest)throw new Exception("FAIL Missing antiforgery token accepted");Console.WriteLine("PASS Admin row deletion requires antiforgery token");
        if((await client.PostAsync(url+"/ProductionInventory/SatirDuzenle",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("id",documentId.ToString())}))).StatusCode!=HttpStatusCode.BadRequest)throw new Exception("FAIL Missing row-edit antiforgery token accepted");Console.WriteLine("PASS Row editing requires antiforgery token");
        if(serve){Console.WriteLine("Inventory fixture PID="+Environment.ProcessId+" URL="+url+"/ProductionInventory/Detay/"+documentId);using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(15));try{await app.WaitForShutdownAsync(timeout.Token);}catch(OperationCanceledException){}}
        await app.StopAsync();
    }
}
sealed class FixtureUser(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync(){var claims=new List<Claim>{new(ClaimTypes.Name,"Test Admin"),new("Modul","Uretim")};if(!Request.Headers.ContainsKey("X-Fixture-Reader"))claims.Add(new(ClaimTypes.Role,"Admin"));return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(claims,Scheme.Name)),Scheme.Name)));}
}
sealed class PreviewViewFilter(IRazorViewEngine engine,ITempDataDictionaryFactory tempData):IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context,ResultExecutionDelegate next)
    {
        if(context.Result is ViewResult result)
        {
            var name=result.ViewName??context.RouteData.Values["action"]!.ToString();var view=engine.GetView(null,"/Views/ProductionInventory/"+name+".cshtml",false);if(!view.Success)throw new Exception("View not found "+name);using var writer=new StringWriter();
            await view.View.RenderAsync(new ViewContext(context,view.View,result.ViewData,tempData.GetTempData(context.HttpContext),writer,new HtmlHelperOptions()));var messages=tempData.GetTempData(context.HttpContext);var notice=(messages["success"]??messages["error"])?.ToString();
            context.Result=new ContentResult{ContentType="text/html; charset=utf-8",Content="<!doctype html><html lang='tr'><head><meta charset='utf-8'><title>TEST · Stok Düzeltme</title><link rel='stylesheet' href='/assets1/plugins/global/plugins.bundle.css'><link rel='stylesheet' href='/assets1/css/style.bundle.css'><link rel='stylesheet' href='/css/portal-modern.css'></head><body class='portal-modern' style='padding:24px;display:block;height:auto;min-height:100vh'><div class='alert alert-warning'>İZOLE TEST · Canlı stoklara bağlı değildir.</div>"+(notice==null?"":"<div class='alert alert-info'>"+WebUtility.HtmlEncode(notice)+"</div>")+writer+"<script src='/assets1/plugins/global/plugins.bundle.js'></script><script src='/js/select2.js'></script></body></html>"};
        }
        await next();
    }
}
