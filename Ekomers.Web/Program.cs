using Ekomers.Common.Services;
using Ekomers.Common.Services.IServices;
using Ekomers.Data;
using Ekomers.Data.Repository;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using Ekomers.Web.Controllers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

// Add services to the container.
//var connectionString = builder.Configuration.GetConnectionString("LocalConnection");

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");


//var maksString = builder.Configuration.GetConnectionString("NumaratajConnection");
 
builder.Services.Configure<SmsSettings>(builder.Configuration.GetSection("SmsSettings"));
builder.Services.Configure<FileSettings>(builder.Configuration.GetSection("FileSettings")); 
builder.Services.Configure<PageSettings>(builder.Configuration.GetSection("PageSettings"));
 

//builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDbContext<LogoContext>(options =>
	options.UseSqlServer(builder.Configuration.GetConnectionString("LogoConnection")));

builder.Services.AddDbContext<OrtomolekulerDernek>(options =>
	options.UseSqlite(builder.Configuration.GetConnectionString("DernekConnection")));

//builder.Services.AddDbContext<SecondDbContext>(options =>
//        options.UseSqlServer(builder.Configuration.GetConnectionString("NumaratajConnection")));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();

//builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
//    .AddEntityFrameworkStores<ApplicationDbContext>();



// Authentication ve Cookie yapılandırması
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/Home/SignIn";
            options.AccessDeniedPath = "/Home/AccessDenied";
        });

#region "role claim policy"

// role ve roleclaim
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("Edit", policy =>
		policy.RequireClaim("Authorize", "Edit"));

	options.AddPolicy("Create", policy =>
		policy.RequireClaim("Authorize", "Create"));

	options.AddPolicy("Delete", policy =>
		policy.RequireClaim("Authorize", "Delete"));

	options.AddPolicy("View", policy =>
		policy.RequireClaim("Authorize", "View"));

	options.AddPolicy("Update", policy =>
		policy.RequireClaim("Authorize", "Update"));
});
builder.Services.AddAuthorization(options =>
{
	options.AddPolicy("AdminOrFW", policy =>
	  policy.RequireAssertion(context =>
		  context.User.IsInRole("Admin") ||
		  context.User.HasClaim("Modul", "FW")));

	options.AddPolicy("AdminOrCrm", policy =>
	  policy.RequireAssertion(context =>
		  context.User.IsInRole("Admin") ||
		  context.User.HasClaim("Modul", "CRM")));

	options.AddPolicy("AdminOrUretim", policy =>
	  policy.RequireAssertion(context =>
		  context.User.IsInRole("Admin") ||
		  context.User.HasClaim("Modul", "Uretim")));

	options.AddPolicy("AdminOrMalzeme", policy =>
  policy.RequireAssertion(context =>
	  context.User.IsInRole("Admin") ||
	  context.User.HasClaim("Modul", "Malzeme")));

	options.AddPolicy("AdminOrStok", policy =>
	  policy.RequireAssertion(context =>
		  context.User.IsInRole("Admin") ||
		  context.User.HasClaim("Modul", "Stok")));

	options.AddPolicy("AdminOrSatislar", policy =>
	  policy.RequireAssertion(context =>
		  context.User.IsInRole("Admin") ||
		  context.User.HasClaim("Modul", "DestekHizmetleri")));

	options.AddPolicy("AdminOrDokuman", policy =>
	  policy.RequireAssertion(context =>
		  context.User.IsInRole("Admin") ||
		  context.User.HasClaim("DijitalSirket", "Dokuman")));

	options.AddPolicy("RaporEczane", policy =>
		policy.RequireClaim("Rapor", "Eczane"));
});




#endregion


builder.Services.Configure<DataProtectionTokenProviderOptions>(opt =>
{
    opt.TokenLifespan = TimeSpan.FromMinutes(15);
});

builder.Services.AddIdentity<Kullanici, Rol>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._+öÖçÇŞşüÜğĞıİ";
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireDigit = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;

} ).AddDefaultTokenProviders()
    .AddEntityFrameworkStores<ApplicationDbContext>();


builder.Services.ConfigureApplicationCookie(opt =>
{
    var cookieBuilder = new CookieBuilder();
    cookieBuilder.Name = "Ekomers";
    opt.Cookie = cookieBuilder;
    opt.LoginPath = new PathString("/Home/SignIn");
    opt.LogoutPath = new PathString("/Home/SignOut");
    opt.ExpireTimeSpan = TimeSpan.FromMinutes(120);
    
    opt.SlidingExpiration = true;
    opt.AccessDeniedPath = new PathString("/Home/AccessDenied");
});

 

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(120); // Oturum süresini 120 dakika olarak ayarlar
    options.Cookie.HttpOnly = true; // Güvenlik amacıyla çerezlerin sadece HTTP üzerinden erişilebilir olmasını sağlar
    options.Cookie.IsEssential = true; // Genel Veri Koruma Yönetmeliği (GDPR) gereksinimlerini karşılamak için oturum çerezini zorunlu hale getirir
});

//var config = new MapperConfiguration(cfg =>
//{

//});
//var config = new MapperConfiguration(cfg =>
//{
//	cfg.AddProfile<MappingProfile>();
//});

// Current
builder.Services.AddAutoMapper(cfg => cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzg2MTQ3MjAwIiwiaWF0IjoiMTc1NDY0NDAzNiIsImFjY291bnRfaWQiOiIwMTk4ODhlZWRkYTE3N2MyOWIxOGFlZGQyZTUzYWRkZiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazI0ZXllcHlodnB3OHY4M3c2Zno2MXExIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.uqEYp8MlwJpgaOkCGCMT551xnVMkaAtjxC-0DuxIYZcchWSKZ5TzTu9vHVKR6w9e13pdtWhpZU-0XBpsX2n-WI0Wn2dqNAqAFbmQluAd42N3mr4fVE6J7GJSMEBK1AbONOpW08p2cb_xIXpFe11JLLEw-wp9q8V2W4uY4qcbkOKVS-cK3c6mUIWKmXTwSChdkJZ_I_HPvB4NvNetP3Uvr-YzrFpFyqvgRo-tKHDpTrncNN12Ff9DSYDCwXs-3G1ExD4bBfDTXs9VF8ikcH5wPR8sJfSbcNcqEdXWnR2KTYnp18EL56f9MhgV9YPhSTULe7ghXk5ULfm7t2cXRSdWdg", typeof(Program));
builder.Services.AddAutoMapper(cfg => {
	cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxNzg2MTQ3MjAwIiwiaWF0IjoiMTc1NDY0NDAzNiIsImFjY291bnRfaWQiOiIwMTk4ODhlZWRkYTE3N2MyOWIxOGFlZGQyZTUzYWRkZiIsImN1c3RvbWVyX2lkIjoiY3RtXzAxazI0ZXllcHlodnB3OHY4M3c2Zno2MXExIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.uqEYp8MlwJpgaOkCGCMT551xnVMkaAtjxC-0DuxIYZcchWSKZ5TzTu9vHVKR6w9e13pdtWhpZU-0XBpsX2n-WI0Wn2dqNAqAFbmQluAd42N3mr4fVE6J7GJSMEBK1AbONOpW08p2cb_xIXpFe11JLLEw-wp9q8V2W4uY4qcbkOKVS-cK3c6mUIWKmXTwSChdkJZ_I_HPvB4NvNetP3Uvr-YzrFpFyqvgRo-tKHDpTrncNN12Ff9DSYDCwXs-3G1ExD4bBfDTXs9VF8ikcH5wPR8sJfSbcNcqEdXWnR2KTYnp18EL56f9MhgV9YPhSTULe7ghXk5ULfm7t2cXRSdWdg";
});
//var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

//var config = new MapperConfiguration(cfg =>
//{
//	cfg.AddProfile<MappingProfile>();
//}, loggerFactory); // ← İkinci parametre olarak LoggerFactory verilmeli

//var mapper = config.CreateMapper();

#region dependency injection 

//builder.Services.AddIdentity<IdentityUser, IdentityRole>().AddDefaultTokenProviders()
//    .AddEntityFrameworkStores<ApplicationDbContext>();

//builder.Services.AddSingleton<IEmailSender, EmailSender>();
builder.Services.AddSignalR();
//builder.Services.AddAutoMapper(typeof(MappingProfile));
builder.Services.AddRazorPages();
builder.Services.AddHttpClient(); // IHttpClientFactory'yi burada ekliyoruz
builder.Services.AddControllersWithViews()
    .AddNewtonsoftJson(options =>
{
	options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
}); ;

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
 
builder.Services.AddScoped<IUserActivityLogRepository, UserActivityLogRepository>();
//builder.Services.AddScoped<IKullaniciRepository, KullaniciRepository>();
builder.Services.AddScoped(typeof(IGeneralService<>), typeof(GeneralService<>));
 
builder.Services.AddScoped<ITanimlamalarService, TanimlamalarService>(); 
 
builder.Services.AddScoped<IFileService, FileService>();
builder.Services.AddScoped<ILoggerService, LoggerService>();
 
builder.Services.AddScoped<IStokService, StokService>(); 
builder.Services.AddScoped<IIadeService, IadeService>(); 
builder.Services.AddScoped<ISmsSender, SmsSenderTuraCell>();
builder.Services.AddScoped<IEmailSenderService, EmailSenderService>(); 
builder.Services.AddScoped<IDynamicTableService, DynamicTableService>(); 
builder.Services.AddScoped<ISehirlerService, SehirlerService>();
builder.Services.AddScoped<IVergiDairesiService, VergiDairesiService>(); 
builder.Services.AddScoped<IPortalMenuService, PortalMenuService>();
builder.Services.AddScoped<ITableMetadataService, TableMetadataService>();
builder.Services.AddScoped<ITableCacheService, TableCacheService>();
builder.Services.AddScoped(typeof(ICacheService<>), typeof(CacheService<>));


builder.Services.AddScoped<IEczaneService, EczaneService>();

builder.Services.AddScoped<ISirketlerService, SirketlerService>();
builder.Services.AddScoped<IUserShortCutFieldService, UserShortCutFieldService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IYetkilendirmeService, YetkilendirmeService>();

builder.Services.AddScoped<IMusterilerService, MusterilerService>();
builder.Services.AddScoped<IAktiviteService, AktiviteService>();
builder.Services.AddScoped<IFirsatService, FirsatService>();
builder.Services.AddScoped<ITeklifService, TeklifService>();
builder.Services.AddScoped<IMalzemeService, MalzemeService>();
builder.Services.AddScoped<ISiparisService, SiparisService>();
builder.Services.AddScoped<ISiparisIadeService, SiparisIadeService>();
builder.Services.AddScoped<ICrmService, CrmService>();
builder.Services.AddScoped<IReceteService, ReceteService>();
builder.Services.AddScoped<IUretimService, UretimService>();
builder.Services.AddScoped<ISatislarService, SatislarService>();
builder.Services.AddScoped<ISozlesmelerService, SozlesmelerService>();



builder.Services.AddScoped<ITcmbService, TcmbService>();
builder.Services.AddScoped<IMapService, MapService>();
//builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailService>(provider =>
   new EmailService(
       "suphinohutlu@afyon.bel.tr",
       "CnmDrk2012",
       "mail.afyon.bel.tr",
       587));

builder.Services.AddScoped<IUserService, UserService>();



builder.Services.AddDistributedMemoryCache();



var app = builder.Build();
#endregion




 //SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBPh8sVXJwS0d+WFBPdEBEQmFJfFdmRGNTe1h6cVNWESFaRnZdRl1iSXlSdkFkW3daeXRd");
/// SyncfusionLicenseProvider.RegisterLicense("Ngo9BigBOggjHTQxAR8/V1NAaF5cWWJCf1FpRmJGdld5fUVHYVZUTXxaS00DNHVRdkdnWX5fdnRXRmBZUkNyX0c=");
// SyncfusionLicenseProvider.RegisterLicense("Mgo+DSMBPh8sVXJwS0d+WFBPd11dXmJWd1p/THNYflR1fV9DaUwxOX1dQl9nSX9ScEVhWX1acXBXT2Y=");
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
	  name: "GES",
	  pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}"
	);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseEndpoints(endpoints =>
{
	
	_ = endpoints.MapControllerRoute(
		name: "default",
		pattern: "{controller=Chat}/{action=PrivateChat}/{id?}");
});

//app.MapAreaControllerRoute(
//    name: "cum5",
//    areaName: "cum5",
//    pattern: "cum5/{controller=GES}/{action=Index}"
//);
//app.MapAreaControllerRoute(
//	name: "ges",
//	areaName: "ges",
//	pattern: "ges/{controller=home}/{action=Index}"
//);



//app.MapControllerRoute(
//    name: "areas",
//    pattern: "{area=exists}/{controller}/{action}/{id?}");

//app.UseEndpoints(endpoints =>
//{
//	_ = endpoints.MapAreaControllerRoute(
//	  name: "Web30",
//	  pattern: "Web30/{controller=Web30}/{action=Index}/{id?}"
//	);
//});
//app.MapRazorPages();

app.Run();
