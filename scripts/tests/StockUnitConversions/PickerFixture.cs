using Ekomers.Models.ViewModels.Production;
using Ekomers.Models.ViewModels.Purchasing;
using System.Security.Claims;
using Ekomers.Models.Enums;
using Ekomers.Web.Controllers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.FileProviders;

// Read-only synthetic form preview: never creates a DbContext or connects to live inventory.
static class PickerFixture
{
    public static async Task Run(Func<HttpContext,string,string,object,Task<string>> render)
    {
        var root=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Ekomers.Web","wwwroot")))root=root.Parent;
        if(root==null)throw new DirectoryNotFoundException("Run from the ECommerce repository.");
        var builder=WebApplication.CreateBuilder();builder.Logging.ClearProviders();builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Services.AddControllersWithViews().AddApplicationPart(typeof(HammaddeSarfController).Assembly);
        builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
        await using var app=builder.Build();
        app.UseDeveloperExceptionPage();
        app.UseStaticFiles(new StaticFileOptions{FileProvider=new PhysicalFileProvider(Path.Combine(root.FullName,"Ekomers.Web","wwwroot"))});
        RawMaterialConsumptionCreateVM Model(bool selected=false,bool blocked=false)=>new(){WarehouseId=1,Notes="Sadece test verisi",Warehouses=[new SelectListItem("TEST · Ana depo","1"),new SelectListItem("TEST · Üretim depo","2")],
            Lots=[new(){StockLotId=11,MaterialId=1,MaterialCode="150HM.TEST.001",MaterialName="YEŞİL ŞİŞE HAMMADDESİ",LotNumber="LOT-A",Unit="Kilogram",UnitId=2,PhysicalQuantity=12.5m,BlockReason=blocked?"Test: rezervasyon kontrolü":null},
                new(){StockLotId=12,MaterialId=2,MaterialCode="150HM.TEST.002",MaterialName="VİTAMİN C",LotNumber="LOT-B",Unit="Adet",UnitId=1,PhysicalQuantity=900},
                new(){StockLotId=13,MaterialId=1,MaterialCode="150HM.TEST.001",MaterialName="YEŞİL ŞİŞE HAMMADDESİ",LotNumber="LOT-C",Unit="Kilogram",UnitId=2,PhysicalQuantity=45}],
            Lines=[new(){StockLotId=selected?11:null,Quantity=selected?"3,25":null,Notes=selected?"Korunacak not":null},new()]};
        async Task<IResult> Page(HttpContext http,RawMaterialConsumptionCreateVM model)
        {
            var body=await render(http,"HammaddeSarf","Yeni",model);
            return Results.Content("<!doctype html><html lang='tr'><head><meta charset='utf-8'><title>TEST · Hammadde sarf seçimi</title><link rel='stylesheet' href='/assets1/plugins/global/plugins.bundle.css'><link rel='stylesheet' href='/assets1/css/style.bundle.css'></head><body style='padding:24px;background:#f4f8f6'><div class='alert alert-warning'>İZOLE TEST · Canlı stoklara bağlı değildir.</div>"+body+"<script src='/assets1/plugins/global/plugins.bundle.js'></script><script src='/js/production/raw-material-consumption.js'></script></body></html>","text/html; charset=utf-8");
        }
        app.MapGet("/picker",(Delegate)new Func<HttpContext,Task<IResult>>(http=>Page(http,Model(http.Request.Query.ContainsKey("selected"),http.Request.Query.ContainsKey("blocked")))));
        app.MapGet("/suppliers",(Delegate)new Func<HttpContext,Task<IResult>>(async http=>
        {
            http.User=new ClaimsPrincipal(new ClaimsIdentity(http.Request.Query.ContainsKey("reader")?Array.Empty<Claim>():[new Claim(ClaimTypes.Role,"Admin")],"Fixture"));
            var suppliers=new List<SupplierListVM>{new(){Id=1,Code="320.TEST.01",Name="TEST · Kimya ve Ambalaj San. Tic. Ltd. Şti.",ContactName="Test Yetkilisi",Phone="TEST",QuotationCount=2,IsActive=true},new(){Id=2,Code="320.TEST.02",Name="TEST · İlaç San. ve Tic. A.Ş.",Email="test@example.invalid",QuotationCount=1,IsActive=false}};
            var body=await render(http,"SatinalmaYonetimi","Tedarikciler",suppliers);
            return Results.Content("<!doctype html><html lang='tr'><head><meta charset='utf-8'><title>TEST · Tedarikçi butonları</title><link rel='stylesheet' href='/assets1/plugins/global/plugins.bundle.css'><link rel='stylesheet' href='/assets1/css/style.bundle.css'><link rel='stylesheet' href='/css/custom.css'><link rel='stylesheet' href='/css/portal-modern.css'></head><body class='portal-modern' style='padding:24px'><div class='alert alert-warning'>İZOLE TEST · Canlı tedarikçilere bağlı değildir.</div>"+body+"</body></html>","text/html; charset=utf-8");
        }));
        InventoryDocumentCreateVM TransferModel(bool selected=false,bool blocked=false)=>new(){Type=PrdInventoryDocumentType.WarehouseTransfer,SourceWarehouseId=1,TargetWarehouseId=2,Warehouses=Model().Warehouses,
            SourceLots=Model().Lots.Where(x=>!blocked||x.StockLotId!=11).Select(x=>new SelectListItem($"{x.MaterialCode} - {x.MaterialName} | Lot: {x.LotNumber} | Kullanılabilir: {x.PhysicalQuantity} {x.Unit} | Maliyet: 10 ₺",x.StockLotId.ToString())).ToList(),
            Lines=[new(){SourceStockLotId=selected?11:null,Quantity=selected?"3,25":null,Notes=selected?"Korunacak transfer notu":null},new()]};
        async Task<IResult> TransferPage(HttpContext http,InventoryDocumentCreateVM model)
        {
            var body=await render(http,"ProductionInventory","Yeni",model);
            return Results.Content("<!doctype html><html lang='tr'><head><meta charset='utf-8'><title>TEST · Transfer seçimi</title><link rel='stylesheet' href='/assets1/plugins/global/plugins.bundle.css'><link rel='stylesheet' href='/assets1/css/style.bundle.css'></head><body style='padding:24px;background:#f4f8f6'><div class='alert alert-warning'>İZOLE TEST · Canlı stoklara bağlı değildir.</div>"+body+"<script src='/assets1/plugins/global/plugins.bundle.js'></script><script src='/js/select2.js'></script><script src='/js/production/warehouse-transfer.js'></script></body></html>","text/html; charset=utf-8");
        }
        app.MapGet("/transfer-picker",(Delegate)new Func<HttpContext,Task<IResult>>(http=>TransferPage(http,TransferModel(http.Request.Query.ContainsKey("selected"),http.Request.Query.ContainsKey("blocked")))));
        app.MapGet("/ProductionInventory/Yeni",(Delegate)new Func<HttpContext,Task<IResult>>(http=>
        {
            var model=TransferModel();
            model.SourceWarehouseId=int.TryParse(http.Request.Query["SourceWarehouseId"],out var source)?source:null;
            model.TargetWarehouseId=int.TryParse(http.Request.Query["TargetWarehouseId"],out var target)?target:null;
            return TransferPage(http,model);
        }));
        app.MapPost("/ProductionInventory/Yeni",async (HttpContext http)=>
        {
            var data=await http.Request.ReadFormAsync();var model=TransferModel();model.Lines=[];
            for(var i=0;i<100;i++)if(data.ContainsKey($"Lines[{i}].SourceStockLotId"))model.Lines.Add(new(){SourceStockLotId=int.TryParse(data[$"Lines[{i}].SourceStockLotId"],out var id)?id:null,Quantity=data[$"Lines[{i}].Quantity"],Notes=data[$"Lines[{i}].Notes"]});
            return await TransferPage(http,model);
        });
        app.MapPost("/HammaddeSarf/Yeni",async (HttpContext http)=>
        {
            var data=await http.Request.ReadFormAsync();var model=Model();model.Lines=[];
            for(var i=0;i<100;i++)if(data.ContainsKey($"Lines[{i}].StockLotId"))model.Lines.Add(new(){StockLotId=int.TryParse(data[$"Lines[{i}].StockLotId"],out var id)?id:null,Quantity=data[$"Lines[{i}].Quantity"],Notes=data[$"Lines[{i}].Notes"]});
            return await Page(http,model);
        });
        app.MapDefaultControllerRoute();await app.StartAsync();
        Console.WriteLine("Picker fixture PID="+Environment.ProcessId+" URL="+app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single()+"/picker");
        using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(15));
        try{await app.WaitForShutdownAsync(timeout.Token);}catch(OperationCanceledException){}finally{await app.StopAsync();}
    }
}
