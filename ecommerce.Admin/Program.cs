using System;
using System.Globalization;
using System.Threading;
using BasbugOto.BackgroundServices;

using CurrencyAuto.BackgroungServices;
using Dega.BackgroundServices;
using ecommerce.Admin.AppStart;
using ecommerce.Admin.Components;
using ecommerce.Admin.Jobs;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services.Concreate;
using ecommerce.Admin.Services;
using ecommerce.Admin.Filters;
using ecommerce.Odaksodt.Extensions;
using ecommerce.Cargo.Mng;
using ecommerce.Cargo.Sendeo;
using ecommerce.Cargo.Yurtici;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Helpers;
using ecommerce.Domain.Shared.Middleware;
using ecommerce.EFCore.Context;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using OtoIsmail.BackgroundServices;
using Otokoc.BackgroundServices;
using Remar.BackgroundServices;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Exceptions;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
var builder = WebApplication.CreateBuilder(args);

// Graceful shutdown - process kapanırken socket'ların temiz kapatılmasını sağlar
// Bu olmadan Kestrel socket'ları kernel'da takılı kalıyor ve port meşgul hatası oluşuyor
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AllowSynchronousIO = false;
});
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(10);
});

// Configure Serilog — Elasticsearch yoksa/erişilemezse uygulama yine açılsın
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "Admin")
        .Enrich.WithExceptionDetails()
        .WriteTo.Console();

    var elasticUri = context.Configuration["ElasticSearch:Uri"];
    if (!string.IsNullOrEmpty(elasticUri))
    {
        try
        {
            configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
            {
                AutoRegisterTemplate = true,
                IndexFormat = $"ecommerce-logs-admin-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-{{0:yyyy.MM}}",
                ModifyConnectionSettings = x => x.BasicAuthentication(
                    context.Configuration["ElasticSearch:Username"],
                    context.Configuration["ElasticSearch:Password"]),
                NumberOfShards = 2,
                NumberOfReplicas = 1
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Serilog] Elasticsearch atlandi: {ex.Message}");
        }
    }
});

// Services
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Blazor Server Circuit Options - Uzun süren işlemler için
builder.Services.AddServerSideBlazor().AddCircuitOptions(options =>
{
    options.DetailedErrors = builder.Environment.IsDevelopment();
    options.DisconnectedCircuitMaxRetained = 100;
    options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(10);
    options.JSInteropDefaultCallTimeout = TimeSpan.FromMinutes(5);
    options.MaxBufferedUnacknowledgedRenderBatches = 20;
}).AddHubOptions(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60); // 3 dakika -> 60 saniye (kapanışta takılmayı önler)
    options.HandshakeTimeout = TimeSpan.FromSeconds(30); // 1 dakika -> 30 saniye
    options.KeepAliveInterval = TimeSpan.FromSeconds(15); // 30s -> 15s (varsayılan)
    options.MaximumReceiveMessageSize = 128 * 1024; // 128 KB
});

builder.Services.AddLocalization();

// DbContext — ConnectionString yoksa net hata (sunucuda env/appsettings kontrolü)
var rawCs = builder.Configuration.GetConnectionString("ApplicationDbContext");
if (string.IsNullOrWhiteSpace(rawCs))
    throw new InvalidOperationException(
        "ConnectionStrings:ApplicationDbContext bos. Sunucuda IIS Application Settings veya appsettings.Production.json ile ayarlayin.");

builder.Services.AddDbContext<ApplicationDbContext>(options => {
    options.EnableThreadSafetyChecks(false);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    var csb = new Npgsql.NpgsqlConnectionStringBuilder(rawCs)
    {
        KeepAlive = 30,            // send keepalive every 30s
        MaxPoolSize = 200,         // allow more concurrent conns
        Multiplexing = false       // stabilize under load
    };
    // Buffer size'ları connection string'e manuel ekle (büyük veri setleri için)
    var finalConnectionString = csb.ConnectionString;
    if (!finalConnectionString.Contains("ReadBufferSize", StringComparison.OrdinalIgnoreCase))
    {
        finalConnectionString += ";ReadBufferSize=16384;WriteBufferSize=16384";
    }
    options.UseNpgsql(finalConnectionString, o => 
    {
        o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
         .MigrationsAssembly("ecommerce.EFCore");
        o.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(2), errorCodesToAdd: null);
        o.CommandTimeout(180);
    });
});

// Ayrı bağlantı ile paged sorgular (Blazor/aynı request içinde eşzamanlı kullanımda "A command is already in progress" hatasını önler)
// EpApplicationDbContextFactory: singleton olup yalnızca IServiceProvider'a bağımlı; CreateDbContext içinde scope açıp scoped DbContextOptions alır (singleton → scoped DI hatasını önler).
builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>, ecommerce.EP.Services.EpApplicationDbContextFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8); // Consistent with auth
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register DbContext base type for DAT Integration
builder.Services.AddScoped<IOnlineUserService, OnlineUserService>();
builder.Services.AddScoped<ILogService, LogService>();
builder.Services.AddScoped<DbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddScoped<OtoIsmail.BackgroundServices.OtoIsmailBackgroundService>();
builder.Services.AddScoped<OtoIsmail.BackgroundServices.OtoIsmailFullSyncBackgroundService>();
builder.Services.AddScoped<Remar.BackgroundServices.RemarProductBackgroundService>();
builder.Services.AddScoped<BasbugOto.BackgroundServices.BasbugOtoBackgroundService>();
builder.Services.AddScoped<Dega.BackgroundServices.DegaBackgroundService>();
builder.Services.AddScoped<CurrencyAuto.BackgroungServices.CurrencyBackgroundService>();
builder.Services.AddScoped<IRecentSearchService, RecentSearchService>();
builder.Services.AddScoped<IPaymentModalService, PaymentModalService>();
builder.Services.AddScoped<ISearchFieldMatcherService, SearchFieldMatcherService>();
// Hangfire server is configured in ConfigureServices.Configure(builder)



// Services
builder.Services.AddScoped<ecommerce.Admin.Domain.Services.IPermissionService, ecommerce.Admin.Domain.Services.PermissionService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IPermissionService, ecommerce.Admin.Services.Concreate.PermissionService>();
builder.Services.AddScoped<ecommerce.Admin.Domain.Services.IRoleBasedFilterService, ecommerce.Admin.Domain.Services.RoleBasedFilterService>();

// Odaksoft E-Fatura entegrasyonu
builder.Services.AddOdaksoftServices(builder.Configuration);

ConfigureServices.Configure(builder);


CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("tr-TR");
CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");





var app = builder.Build();
app.UseRequestLocalization("tr-TR");
app.UseSession();


// Middleware (Top Level)
app.UseStatusCodePagesWithRedirects("/error-{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseBackgroundJobs();
}
// app.UseMngCargo();
//app.UseSendeoCargo();
// app.UseYurticiCargo();
if(!app.Environment.IsDevelopment()){
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
if(app.Environment.IsDevelopment() && string.IsNullOrEmpty(app.Configuration["UploadImagePath"])){
    app.UseFileServer(new FileServerOptions{FileProvider = new PhysicalFileProvider(app.Services.GetRequiredService<FileHelper>().GetUploadPath()), RequestPath = "/Files"});
}
app.UseHeaderPropagation();
app.UseRouting();

// Moved up

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
// app.UseStaticFiles(); // Removed duplicate
app.UseMiddleware<ecommerce.Domain.Shared.Middleware.LogContextMiddleware>();
app.UseMiddleware<CurrentUserMiddleware>();

// Routing
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
// app.MapBlazorHub();

    // Hangfire Dashboard - Only available in non-development environments
    if (!app.Environment.IsDevelopment())
    {
        app.UseHangfireDashboard("/hangfire", new DashboardOptions{AsyncAuthorization = new[]{new HangfireAuthorizationFilter()}, DisplayNameFunc = BackgroundJobArgsHelper.GetDashboardDisplayNameProvider()});
    }
using(var scope = app.Services.CreateScope()){
    // var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    // // oto migration acik burasi
    // await context.Database.MigrateAsync();
    if (!app.Environment.IsDevelopment())
    {
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IHangfireRecurringJobManager>();
        

        //sonra cilacak.
        // recurringJobManager.RecurAsync<CargoReturnCheckJob>(Cron.Daily(), null, null, null, "admin").Wait();
        
        
        // 1. OtoIsmail (Günlük - Tarih Bazlı: Sadece bugün değişen ürünler)
        // Günde 6 kez çalışır: 09:00, 11:00, 13:00, 15:00, 17:00, 19:00
        var otoIsmailJobId = nameof(OtoIsmailBackgroundService);
        await recurringJobManager.RemoveIfExistsAsync(otoIsmailJobId);
        await recurringJobManager.RecurAsync<OtoIsmailBackgroundService>("0 9,10,11,12,13,14,15,16,17,18,19,20 * * *", null, otoIsmailJobId, TimeZoneInfo.Local, "admin");
        
        // 1.1. OtoIsmail Full Sync (Haftalık - Tüm ürünler)
        // Her Cumartesi gece saat 04:00'de çalışır (haftada bir tam senkronizasyon)
        var otoIsmailFullSyncJobId = nameof(OtoIsmailFullSyncBackgroundService);
        await recurringJobManager.RemoveIfExistsAsync(otoIsmailFullSyncJobId);
        await recurringJobManager.RecurAsync<OtoIsmailFullSyncBackgroundService>("0 4 * * 6", null, otoIsmailFullSyncJobId, TimeZoneInfo.Local, "admin");

        // 2. RemarProduct
        var remarJobId = nameof(RemarProductBackgroundService);
        await recurringJobManager.RemoveIfExistsAsync(remarJobId);
        await recurringJobManager.RecurAsync<RemarProductBackgroundService>("0 9,10,11,12,13,14,15,16,17,18,19,20 * * *", null, remarJobId, TimeZoneInfo.Local, "admin");

        // 3. BasbugOto
        // var basbugJobId = nameof(BasbugOtoBackgroundService);
        // await recurringJobManager.RemoveIfExistsAsync(basbugJobId);
        // await recurringJobManager.RecurAsync<BasbugOtoBackgroundService>("0 9,10,11,12,13,14,15,16,17,18,19,20 * * *", null, basbugJobId, TimeZoneInfo.Local, "admin");

        // 4. Dega
        var degaJobId = nameof(DegaBackgroundService);
        await recurringJobManager.RemoveIfExistsAsync(degaJobId);
        await recurringJobManager.RecurAsync<DegaBackgroundService>("0 9,10,11,12,13,14,15,16,17,18,19,20 * * *", null, degaJobId, TimeZoneInfo.Local, "admin");
        
        var currencyJob = nameof(CurrencyBackgroundService);
        await recurringJobManager.RemoveIfExistsAsync(currencyJob);
        await recurringJobManager.RecurAsync<CurrencyBackgroundService>("0 12 * * *", null, currencyJob, TimeZoneInfo.Local, "admin");
    }
    
  
    
    //
    // 5. Otokoc
    // var otokocJobId = nameof(OtokocProductBackgoundService);
    // await recurringJobManager.RemoveIfExistsAsync(otokocJobId);
    // await recurringJobManager.RecurAsync<OtokocProductBackgoundService>("16 6,9,12,15,18 * * *", null, otokocJobId, TimeZoneInfo.Local, "default");

}
// Graceful shutdown - uygulama kapanırken açık bağlantıları temiz kapat
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("Uygulama kapatılıyor, açık bağlantılar temizleniyor...");
});
app.Lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("Uygulama kapatıldı.");
    Log.CloseAndFlush();
});

app.Run();
