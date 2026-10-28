using System.Reflection;
using Ekomers.Data;
using Ekomers.Data.Services;
using Ekomers.Data.Migrations;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Enums;
using Ekomers.Models.ViewModels.Production;
using Ekomers.Web.Controllers;
using Ekomers.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Design;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.SqlServer.Design.Internal;

static void Check(bool value,string message){if(!value)throw new Exception("FAIL "+message);Console.WriteLine("PASS "+message);}
static async Task Reject(Func<Task> action,string message){try{await action();}catch(InvalidOperationException){Check(true,message);return;}throw new Exception("FAIL "+message);}
if(args.Contains("--scaffold"))
{
    using var design=new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FasonDesignOnly_DoNotCreate;Integrated Security=true;TrustServerCertificate=true").Options);
    var services=new ServiceCollection();services.AddEntityFrameworkDesignTimeServices();services.AddDbContextDesignTimeServices(design);new SqlServerDesignTimeServices().ConfigureDesignTimeServices(services);
    using var provider=services.BuildServiceProvider();var scaffolder=provider.GetRequiredService<IMigrationsScaffolder>();var scaffold=scaffolder.ScaffoldMigration("AddFasonConsignmentStock","Ekomers.Data");
    var old=scaffold.MigrationId;var id="20261026120000_AddFasonConsignmentStock";scaffold=new ScaffoldedMigration(scaffold.FileExtension,scaffold.PreviousMigrationId,scaffold.MigrationCode,id,scaffold.MetadataCode.Replace(old,id),scaffold.MigrationSubNamespace,scaffold.SnapshotCode,scaffold.SnapshotName,scaffold.SnapshotSubnamespace);
    var root=new DirectoryInfo(Directory.GetCurrentDirectory());while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Ekomers.Web")))root=root.Parent;
    var folder=Path.Combine(root!.FullName,"Ekomers.Data");var output=Path.Combine(folder,"Migrations");if(File.Exists(Path.Combine(output,id+".cs")))throw new InvalidOperationException("Migration already exists.");scaffolder.Save(folder,scaffold,output);Console.WriteLine("Scaffolded "+scaffold.MigrationId+" without opening a database connection.");return;
}

var database="CodexFasonStock_"+Guid.NewGuid().ToString("N");var cs=new SqlConnectionStringBuilder{DataSource="(localdb)\\MSSQLLocalDB",InitialCatalog=database,IntegratedSecurity=true,TrustServerCertificate=true};
DbContextOptions<ApplicationDbContext> Options()=>new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(cs.ConnectionString).Options;
await using var db=new FasonContext(Options());
try
{
    await db.Database.EnsureCreatedAsync();
    var migration=new AddFasonConsignmentStock();
    Check(migration.UpOperations.OfType<CreateTableOperation>().Count()==4&&migration.UpOperations.OfType<CreateTableOperation>().All(x=>x.Name.StartsWith("Fsn"))&&!migration.UpOperations.Any(x=>x is AlterColumnOperation or DropTableOperation or AddColumnOperation),"Migration creates only separate customer-stock tables without rewriting factory data");
    await db.Database.ExecuteSqlRawAsync("""
        DROP TABLE FsnStockMovement; DROP TABLE FsnReceiptLine; DROP TABLE FsnReceipt; DROP TABLE FsnWarehouse;
        CREATE TABLE AuthorizationCategory(ID int IDENTITY PRIMARY KEY,Ad nvarchar(250),Aciklama nvarchar(1000),IsActive bit,IsDelete bit,CreateDate datetime2);
        CREATE TABLE Yetkilendirme(ID int IDENTITY PRIMARY KEY,Ad nvarchar(250),Aciklama nvarchar(1000),KategoriID int,PolicyName nvarchar(100),ClaimType nvarchar(100),ClaimName nvarchar(100),IsActive bit,IsDelete bit,CreateDate datetime2,UpdateDate datetime2);
        """);
    await using(var actual=new ApplicationDbContext(Options())){
        foreach(var command in actual.GetService<IMigrationsSqlGenerator>().Generate(migration.UpOperations,actual.Model))await db.Database.ExecuteSqlRawAsync(command.CommandText);
        Check(!actual.Database.HasPendingModelChanges(),"Full production EF model matches the new migration snapshot");
    }
    Check(await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM Yetkilendirme WHERE ClaimName IN ('FasonHammaddeYonet','FasonHammaddeGeriAl') AND IsActive=1").SingleAsync()==2,"Migration registers management and reversal permissions without granting users access");
    var kg=new PrdUnit{Code="KG",Name="Kilogram",IsActive=true};var grams=new PrdUnit{Code="GRAM",Name="Gram",IsActive=true};var box=new PrdUnit{Code="KOLI",Name="Koli",IsActive=true};db.AddRange(kg,grams,box);await db.SaveChangesAsync();
    var material=new PrdMaterial{Code="FSN.TEST.01",Name="TEST Hammadde",Type=PrdMaterialType.RawMaterial,UnitId=kg.ID,RequiresLotTracking=true,RequiresExpirationDate=true,IsActive=true};db.Add(material);await db.SaveChangesAsync();db.Add(new PrdUnitConversion{MaterialId=material.ID,FromUnitId=box.ID,ToUnitId=kg.ID,Factor=20,IsActive=true});
    var a=new FsnWarehouse{Code="FAS-TEST-A",Name="A Fason Deposu",CustomerCode="MUSTERI-A",CustomerName="TEST Müşteri A",IsActive=true};var b=new FsnWarehouse{Code="FAS-TEST-B",Name="B Fason Deposu",CustomerCode="MUSTERI-B",CustomerName="TEST Müşteri B",IsActive=true};db.AddRange(a,b);await db.SaveChangesAsync();
    var factory=new PrdWarehouse{Code="FAB-TEST",Name="Fabrika",Type=PrdWarehouseType.Main,IsActive=true};db.Add(factory);await db.SaveChangesAsync();var lot=new PrdStockLot{MaterialId=material.ID,WarehouseId=factory.ID,LotNumber="SAME-LOT",IsActive=true};db.Add(lot);await db.SaveChangesAsync();db.Add(new PrdStockMovement{WarehouseId=factory.ID,MaterialId=material.ID,StockLotId=lot.ID,UnitId=kg.ID,Quantity=5,Direction=PrdStockDirection.In,MovementDate=DateTime.Today,IsActive=true});await db.SaveChangesAsync();
    FasonReceiptFormVM Form(int warehouse,string quantity,int? unit=null)=>new(){WarehouseId=warehouse,ReceiptDate=DateTime.Today,DispatchNumber="TEST-IRS",Lines=[new(){MaterialId=material.ID,UnitId=unit,Quantity=quantity,LotNumber="SAME-LOT",ProductionDate=DateTime.Today.AddDays(-2),ExpirationDate=DateTime.Today.AddYears(2),Notes="TEST giriş notu"}]};
    var service=new FasonStockService(db);var form=Form(a.ID,"2",box.ID);var id=await service.CreateAsync(form,"test");
    Check(!await db.FsnStockMovements.AnyAsync()&&await db.FsnReceipts.Where(x=>x.ID==id).Select(x=>x.Status).SingleAsync()==FsnReceiptStatus.Draft,"Draft does not affect customer or factory stock");
    Check(await service.CreateAsync(form,"test")==id&&await db.FsnReceipts.CountAsync()==1,"Repeating the same form does not create another receipt");
    var line=await db.FsnReceiptLines.SingleAsync(x=>x.ReceiptId==id);Check(line.OriginalQuantity==2&&line.OriginalUnitId==box.ID&&line.Quantity==40&&line.UnitId==kg.ID,"Product-specific unit conversion preserves entered quantity and unit");
    await service.PostAsync(id,"test");await service.PostAsync(id,"test");Check(await db.FsnStockMovements.CountAsync()==1,"Repeated approval cannot post stock twice");
    var bid=await service.CreateAsync(Form(b.ID,"1000",grams.ID),"test");await service.PostAsync(bid,"test");
    Check((await service.Stock(a.ID).SingleAsync()).Balance==40&&(await service.Stock(b.ID).SingleAsync()).Balance==1,"Same material and lot remain isolated between customer warehouses");
    Check(await service.Stock().CountAsync()==2,"Customer stock report never merges different owners");
    var factoryBalance=await ProductionStockLotReader.LoadAsync(db,factory.ID,default);Check(factoryBalance.Single().PhysicalQuantity==5&&await db.PrdWarehouses.CountAsync()==1&&await db.PrdStockMovements.CountAsync()==1,"Factory balance and warehouse choices exclude all customer-owned inventory");
    var history=await service.History(a.ID).SingleAsync();Check(history.Quantity==40&&!history.IsReversal&&history.Reason==null&&history.LineNotes=="TEST giriş notu"&&history.ProductionDate.HasValue&&history.ExpirationDate.HasValue,"Customer history retains receipt, lot dates and entered notes");
    var invalid=Form(a.ID,"1");invalid.Lines[0].ExpirationDate=null;await Reject(()=>service.CreateAsync(invalid,"test"),"Required expiry is validated before saving");
    invalid=Form(a.ID,"1");invalid.Lines[0].LotNumber=null;await Reject(()=>service.CreateAsync(invalid,"test"),"Required lot is validated before saving");
    invalid=Form(a.ID,"0");await Reject(()=>service.CreateAsync(invalid,"test"),"Zero receipt quantity is rejected");
    invalid=Form(999999,"1");await Reject(()=>service.CreateAsync(invalid,"test"),"An invalid owner warehouse cannot receive inventory");
    invalid=Form(a.ID,"1");invalid.Lines.Add(new(){Quantity="9"});await Reject(()=>service.CreateAsync(invalid,"test"),"Partially completed rows cannot silently disappear");
    var pending=await service.CreateAsync(Form(a.ID,"1",box.ID),"test");await db.PrdUnitConversions.Where(x=>x.MaterialId==material.ID).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Factor,30));
    await Reject(()=>service.PostAsync(pending,"test"),"Changing a conversion after drafting blocks posting without partial stock writes");await service.CancelAsync(pending,"test");Check(await db.FsnStockMovements.CountAsync()==2,"Cancelled and invalid drafts cannot add movements");
    await db.PrdUnitConversions.Where(x=>x.MaterialId==material.ID).ExecuteUpdateAsync(s=>s.SetProperty(x=>x.Factor,20));
    var concurrent=await service.CreateAsync(Form(a.ID,"3"),"test");
    await Task.WhenAll(Enumerable.Range(0,2).Select(async _=>{await using var other=new FasonContext(Options());await new FasonStockService(other).PostAsync(concurrent,"parallel");}));
    Check(await db.FsnStockMovements.CountAsync(x=>db.FsnReceiptLines.Where(l=>l.ReceiptId==concurrent).Select(l=>l.ID).Contains(x.ReceiptLineId))==1,"Concurrent approvals create exactly one stock entry");
    await Task.WhenAll(Enumerable.Range(0,2).Select(async _=>{await using var other=new FasonContext(Options());await new FasonStockService(other).ReverseAsync(concurrent,"Test geri alma","parallel");}));
    Check((await service.Stock(a.ID).SingleAsync()).Balance==40,"Concurrent reversals cannot deduct customer stock twice");
    await service.ReverseAsync(id,"Hatalı giriş testi","test");await service.ReverseAsync(id,"Hatalı giriş testi","test");
    Check(!await service.Stock(a.ID).AnyAsync()&&(await service.Stock(b.ID).SingleAsync()).Balance==1,"Reversing a receipt affects only its owner and keeps other owners unchanged");
    Check(await service.History(a.ID).CountAsync()==4&&await db.FsnReceipts.CountAsync()==4,"Reversal preserves original documents and signed movement history");
    await Reject(()=>service.PostAsync(id,"test"),"Reversed receipts cannot be posted again");
    await Reject(()=>service.PostAsync(999999,"test"),"Unknown document IDs never act on another owner's receipt");
    Check(typeof(FasonHammaddeController).GetCustomAttribute<AuthorizeAttribute>()?.Policy=="AdminOrUretim","Customer-stock pages require production module access");
    foreach(var method in typeof(FasonHammaddeController).GetMethods().Where(m=>m.GetCustomAttribute<HttpPostAttribute>()!=null))Check(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()!=null&&method.GetCustomAttributes<AuthorizeAttribute>().Any(x=>x.Policy is "FasonHammaddeYonet" or "FasonHammaddeGeriAl"),"POST authorization and CSRF: "+method.Name);
    await FasonPreview.Run(db,service,args.Contains("--preview"));
    await using(var actual=new ApplicationDbContext(Options()))foreach(var command in actual.GetService<IMigrationsSqlGenerator>().Generate(migration.DownOperations,actual.Model))await db.Database.ExecuteSqlRawAsync(command.CommandText);
    Check(await db.PrdStockMovements.CountAsync()==1,"Migration rollback leaves factory inventory intact");
}
finally{await db.Database.EnsureDeletedAsync();Console.WriteLine("Removed isolated database "+database);}

sealed class FasonContext(DbContextOptions<ApplicationDbContext> options):ApplicationDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder b)
    {
        var keep=new[]{typeof(FsnWarehouse),typeof(FsnReceipt),typeof(FsnReceiptLine),typeof(FsnStockMovement),typeof(PrdUnit),typeof(PrdUnitConversion),typeof(PrdMaterial),typeof(PrdWarehouse),typeof(PrdStockLot),typeof(PrdStockMovement),typeof(PrdStockReservation)};
        foreach(var t in b.Model.GetEntityTypes().Select(x=>x.ClrType).Where(x=>!keep.Contains(x)).ToList())b.Ignore(t);
        foreach(var t in keep){b.Entity(t).HasBaseType((Type?)null);b.Entity(t).ToTable(t.Name);b.Entity(t).HasKey("ID");}
        foreach(var p in b.Model.GetEntityTypes().SelectMany(x=>x.GetProperties()).Where(x=>x.ClrType==typeof(decimal)||x.ClrType==typeof(decimal?))){p.SetPrecision(18);p.SetScale(6);}
        FasonStockModel.Configure(b);
    }
}
