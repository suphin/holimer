using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Ekomers.Data;
using Ekomers.Web.Controllers;
using Ekomers.Web.Services;
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

static class FasonPreview
{
    public static async Task Run(ApplicationDbContext db,FasonStockService _,bool serve)
    {
        var root=new DirectoryInfo(Directory.GetCurrentDirectory());while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Ekomers.Web","wwwroot")))root=root.Parent;
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        var connection=db.Database.GetConnectionString();builder.Services.AddScoped<ApplicationDbContext>(_=>new FasonContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options));builder.Services.AddScoped<FasonStockService>();
        builder.Services.AddControllersWithViews(o=>o.Filters.Add<PreviewViewFilter>()).AddApplicationPart(typeof(FasonHammaddeController).Assembly);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();builder.Services.AddAuthentication("Fixture").AddScheme<AuthenticationSchemeOptions,FixtureAdmin>("Fixture",_=>{});
        builder.Services.AddAuthorization(o=>{foreach(var name in new[]{"AdminOrUretim","FasonHammaddeYonet","FasonHammaddeGeriAl"})o.AddPolicy(name,p=>p.RequireRole("Admin"));});
        await using var app=builder.Build();app.UseDeveloperExceptionPage();app.UseStaticFiles(new StaticFileOptions{FileProvider=new PhysicalFileProvider(Path.Combine(root!.FullName,"Ekomers.Web","wwwroot"))});
        app.Use(async (http,next)=>{if(!http.Request.Path.StartsWithSegments("/FasonHammadde")&&http.Request.Path!="/fixture-stop"){http.Response.StatusCode=404;return;}await next();});
        app.UseAuthentication();app.UseAuthorization();app.MapGet("/fixture-stop",(IHostApplicationLifetime lifetime)=>{lifetime.StopApplication();return "Isolated preview stopped.";});app.MapDefaultControllerRoute();await app.StartAsync();
        var address=app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();using var client=new HttpClient();
        foreach(var path in new[]{"Index","Depolar","YeniDepo","YeniGiris","Stoklar","Gecmis"}){var response=await client.GetAsync(address+"/FasonHammadde/"+path);if(response.StatusCode!=HttpStatusCode.OK)throw new Exception("FAIL Razor preview "+path+": "+await response.Content.ReadAsStringAsync());Console.WriteLine("PASS Compiled MVC view: "+path);}
        var forged=await client.PostAsync(address+"/FasonHammadde/StogaAl",new FormUrlEncodedContent(new[]{new KeyValuePair<string,string>("id","1")}));if(forged.StatusCode!=HttpStatusCode.BadRequest)throw new Exception("FAIL Missing antiforgery token was accepted");Console.WriteLine("PASS MVC rejects stock posting without antiforgery token");
        if(serve){Console.WriteLine("Fason fixture PID="+Environment.ProcessId+" URL="+address+"/FasonHammadde");using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(15));try{await app.WaitForShutdownAsync(timeout.Token);}catch(OperationCanceledException){}}
        await app.StopAsync();
    }
}
sealed class FixtureAdmin(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()=>Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.Name,"TEST Admin"),new Claim(ClaimTypes.Role,"Admin")},Scheme.Name)),Scheme.Name)));
}
sealed class PreviewViewFilter(IRazorViewEngine engine,ITempDataDictionaryFactory tempData):IAsyncResultFilter
{
    public async Task OnResultExecutionAsync(ResultExecutingContext context,ResultExecutionDelegate next)
    {
        if(context.Result is ViewResult result)
        {
            var name=result.ViewName??context.RouteData.Values["action"]!.ToString();var view=engine.GetView(null,"/Views/FasonHammadde/"+name+".cshtml",false);if(!view.Success)throw new Exception("View not found: "+name);using var writer=new StringWriter();
            await view.View.RenderAsync(new ViewContext(context,view.View,result.ViewData,tempData.GetTempData(context.HttpContext),writer,new HtmlHelperOptions()));
            var messages=tempData.GetTempData(context.HttpContext);var notice=(messages["success"]??messages["error"])?.ToString();
            context.Result=new ContentResult{ContentType="text/html; charset=utf-8",Content="<!doctype html><html lang='tr'><head><meta charset='utf-8'><title>TEST · Fason Stok</title><link rel='stylesheet' href='/assets1/plugins/global/plugins.bundle.css'><link rel='stylesheet' href='/assets1/css/style.bundle.css'><link rel='stylesheet' href='/css/portal-modern.css'></head><body class='portal-modern' style='padding:24px'><div class='alert alert-warning'>İZOLE TEST · Müşteri ve fabrika stokları canlı verilere bağlı değildir.</div>"+(notice==null?"":"<div class='alert alert-info'>"+WebUtility.HtmlEncode(notice)+"</div>")+writer+"<script src='/assets1/plugins/global/plugins.bundle.js'></script><script src='/js/select2.js'></script><script src='/js/production/fason-receipt.js'></script></body></html>"};
        }
        if(context.Result is ContentResult preview&&preview.Content!=null)preview.Content=preview.Content.Replace("style='padding:24px'","style='padding:24px;display:block;height:auto;min-height:100vh'");
        await next();
    }
}
