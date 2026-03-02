using Microsoft.AspNetCore.ResponseCompression;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Polly;
using Polly.Extensions.Http;
using AutoMapper.EquivalencyExpression;
using Blazored.LocalStorage;
using Blazored.Modal;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Helpers;
using ecommerce.Core.Identity;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Concreate;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.Domain.Shared.Services;
using ecommerce.Core.Rules;
using ecommerce.Domain.Shared.Middleware;
using ecommerce.Domain.Shared.Options;
using ecommerce.EFCore.Context;
using ecommerce.Web.Components;
using ecommerce.Web.Domain.Email;
using ecommerce.Web.Domain.MiddleWares;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Web.Domain.Services.Concreate;
using ecommerce.Web.Utility;
using ecommerce.Web.Validators;
using Elasticsearch.Net;
using FluentValidation;
using Hangfire;
using Hangfire.Common;
using Hangfire.PostgreSql;
using I18NPortable;
using I18NPortable.JsonReader;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Nest;
using Nest.JsonNetSerializer;
using Radzen;
using StackExchange.Redis;
using Telecom.Address.Abstract;
using Telecom.Address.Concreate;
using Telecom.Address.Options;
using EmailService = ecommerce.Domain.Shared.Emailing.EmailService;
using EmailTemplateService = ecommerce.Domain.Shared.Services.EmailTemplateService;
using ICookieManager = ecommerce.Web.Domain.Services.Abstract.ICookieManager;
using IEmailService = ecommerce.Domain.Shared.Emailing.IEmailService;
using IEmailTemplateService = ecommerce.Domain.Shared.Services.IEmailTemplateService;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Concreate;
using ecommerce.Virtual.Pos.Providers;
using ecommerce.Payments;
using ecommerce.Payments.Providers;
using Policy = Polly.Policy;
using WebOptimizer;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Exceptions;
using ecommerce.Cargo.Sendeo;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Services;
using ecommerce.Domain.Shared.Options;
using Remar.Abstract;
using Remar.Options;
using Remar.Concreate; // Assuming implementation is here, need to check if Concreate or Concrete
using Dega.Abstract;
using Dega.Options;
using Dega.Concreate; // Assuming implementation is here
using OtoIsmail.Concreate; // Just in case, but Shared might handle it


var builder = WebApplication.CreateBuilder(args);
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Configure Serilog
builder.Host.UseSerilog((context, services, configuration) => {
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "Web")
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(); // Always write to console for dev visibility

    var elasticUri = context.Configuration["ElasticSearch:Uri"];
    if (!string.IsNullOrEmpty(elasticUri))
    {
        configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
        {
            AutoRegisterTemplate = true,
            IndexFormat = $"ecommerce-logs-web-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-{{0:yyyy.MM}}",
            ModifyConnectionSettings = x => x.BasicAuthentication(
                context.Configuration["ElasticSearch:Username"], 
                context.Configuration["ElasticSearch:Password"]),
            NumberOfShards = 2,
            NumberOfReplicas = 1
        });
    }
});


builder.Services.Configure<IdentityOptions>(options =>
    {
        options.User.AllowedUserNameCharacters =
            "1234567890abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
        options.User.RequireUniqueEmail = true;

        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
    }
);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();


builder.Services.AddBlazoredModal();
builder.Services.AddScoped<ICookieManager, CookieManager>();

builder.Services.AddScoped<AppStateManager>();
builder.Services.AddScoped<IUserManager, UserManager>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ISellerProductService, SellerProductService>(); // Multi-index strategy
builder.Services.AddScoped<IUserCarService, UserCarService>();
builder.Services.AddScoped<IStaticPageServices, StaticPageServices>();
builder.Services.AddScoped<IBrandService, BrandService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
builder.Services.AddScoped<IFrequentlyAskedQuestionService, FrequentlyAskedQuestionService>();
builder.Services.AddScoped<ICommonManager, CommonManager>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddScoped<IUserOrderService, UserOrderService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.IManufacturerCacheService, ecommerce.Web.Domain.Services.ManufacturerCacheService>();
builder.Services.AddScoped<IDiscountService, DiscountService>();
builder.Services.AddScoped<IGoogleMerchantService, GoogleMerchantService>();
builder.Services.AddScoped<ISearchSynonymService, SearchSynonymService>();

// Dot Integration Services
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IBankService, ecommerce.Web.Domain.Services.Concreate.BankService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IDotIntegrationService, ecommerce.Web.Domain.Services.Concreate.DotIntegrationService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IDotVehicleDataService, ecommerce.Web.Domain.Services.Concreate.DotVehicleDataService>();

// VIN Elasticsearch Service - Araç bazlı ürün eşleştirme için

builder.Services.AddTransient<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddTransient<ICartService, RedisCartService>();

// Copied from Admin for Stock API availability
// OtoIsmail
builder.Services.Configure<OtoIsmailOptions>(builder.Configuration.GetSection("OtoIsmail"));
builder.Services.AddHttpClient<IOtoIsmailService, ecommerce.Domain.Shared.Services.OtoIsmailService>(); // Shared imp
builder.Services.AddScoped<IRealTimeStockProvider>(sp => (ecommerce.Domain.Shared.Services.OtoIsmailService)sp.GetRequiredService<IOtoIsmailService>());

// Remar
builder.Services.Configure<RemarApiOptions>(builder.Configuration.GetSection("RemarApiOptions"));
builder.Services.AddHttpClient<IRemarApiService, RemarApiService>();
builder.Services.AddScoped<IRealTimeStockProvider>(sp => (RemarApiService)sp.GetRequiredService<IRemarApiService>());

// Dega
builder.Services.Configure<DegaApiOptions>(builder.Configuration.GetSection("DegaApiOptions"));
builder.Services.AddHttpClient<IDegaService, DegaService>();
builder.Services.AddScoped<IRealTimeStockProvider>(sp => (DegaService)sp.GetRequiredService<IDegaService>());
builder.Services.AddScoped<IRealTimeStockResolver, ecommerce.Domain.Shared.Services.RealTimeStockResolver>();
builder.Services.AddScoped<ecommerce.Web.Domain.Email.IEmailTemplateService, ecommerce.Web.Domain.Email.EmailTemplateService>();
builder.Services.AddTransient<IContactEmailService, ContactEmailService>();
builder.Services.AddScoped<IHangfireJobManager, HangfireJobManager>();
builder.Services.AddScoped<FileHelper>();
builder.Services.AddScoped<ecommerce.Core.Interfaces.ITenantProvider, ecommerce.Web.Domain.Services.Concreate.WebTenantProvider>();

builder.Services.AddSendeoCargo(options => builder.Configuration.GetSection("Cargo:Sendeo").Bind(options));

// Payment Services
// Swapped to CPPaymentProviderFactory from ecommerce.Payments
builder.Services.AddScoped<IPaymentProviderFactory, CPPaymentProviderFactory>();
builder.Services.AddTransient<CPNestPayProvider>(); 
builder.Services.AddTransient<NestPayPaymentProvider>();
builder.Services.AddTransient<DenizbankPaymentProvider>();
builder.Services.AddTransient<FinansbankPaymentProvider>();
builder.Services.AddTransient<GarantiPaymentProvider>();
builder.Services.AddTransient<KuveytTurkPaymentProvider>();
builder.Services.AddTransient<VakifbankPaymentProvider>();
builder.Services.AddTransient<PosnetPaymentProvider>();

// Rules engine + memory cache for OrderManager dependencies
builder.Services.AddMemoryCache();
builder.Services.AddRules(options =>
{
    // Register field definition providers so scopes like "Discount" are available
    options.Providers.Add<ecommerce.Domain.Shared.Rules.Providers.DiscountFieldDefinitionProvider>();
    options.Providers.Add<ecommerce.Domain.Shared.Rules.Providers.PopupFieldDefinitionProvider>();
}, assemblyNames: new[] { "ecommerce.Domain.Shared" });
// Decorate OrderManager so GetShoppingCart reads from Redis
builder.Services.AddScoped<OrderManager>();
builder.Services.AddScoped<IOrderManager>(sp => new RedisOrderManagerDecorator(
    sp.GetRequiredService<OrderManager>(),
    sp.GetRequiredService<IConnectionMultiplexer>(),
    sp.GetRequiredService<IUnitOfWork<ApplicationDbContext>>(),

    sp.GetRequiredService<IHttpContextAccessor>(),
    sp.GetRequiredService<IConfiguration>(),
    sp.GetRequiredService<ecommerce.Core.Interfaces.ITenantProvider>(),
    sp.GetRequiredService<ecommerce.Core.Identity.CurrentUser>()
));

builder.Services.AddScoped<IUnitOfWork<ApplicationDbContext>, UnitOfWork<ApplicationDbContext>>();
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var rawCs = builder.Configuration.GetConnectionString("ApplicationDbContext");
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
    options.EnableThreadSafetyChecks(false);
    options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
    options.UseNpgsql(finalConnectionString,
        o =>
        {
            o.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery)
             .MigrationsAssembly("ecommerce.EFCore");
            o.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(2),errorCodesToAdd:null);
            o.CommandTimeout(180);
        });
});


builder.Services.AddScoped<AuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider => provider.GetRequiredService<AuthStateProvider>());
builder.Services.AddHttpContextAccessor();
builder.Services.AddAuthorization();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ContextMenuService>();
builder.Services.AddValidatorsFromAssemblyContaining<UserValidator>();
// builder.Services.AddHangfireServer(); // Disabled to prevent Web server from processing Admin jobs


// Health Checks for infrastructure monitoring
builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("ApplicationDbContext")!,
        name: "postgresql",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy,
        tags: new[] { "db", "ready" })
    .AddRedis(
        redisConnectionString: $"{builder.Configuration["Redis:Host"]}:{builder.Configuration["Redis:Port"]},password={builder.Configuration["Redis:Password"]},ssl=false,abortConnect=false",
        name: "redis",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "cache", "ready" })
    .AddElasticsearch(
        elasticsearchUri: builder.Configuration["ElasticSearch:Uri"]!,
        name: "elasticsearch",
        failureStatus: Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Degraded,
        tags: new[] { "search", "ready" });


builder.Services.AddHangfire(config =>
    {
        config.UsePostgreSqlStorage(builder.Configuration.GetConnectionString("ApplicationDbContext"));
        config.UseFilter(new AutomaticRetryAttribute { Attempts = 3, LogEvents = true });
        config.UseFilter(new RetainSuccessJobsAttribute(100)); // Keep success jobs for 100 days
        config.Use(new HangfireJobFilterAttributeFilterProvider(), filterProvider =>
            {
                var existingProvider =
                    JobFilterProviders.Providers.FirstOrDefault(p => p is JobFilterAttributeFilterProvider);
                if (existingProvider != null)
                {
                    JobFilterProviders.Providers.Remove(existingProvider);
                }

                JobFilterProviders.Providers.Add(filterProvider);
            }
        );
    }
);
builder.Services.AddIdentity<User, ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<User>, WebUserClaimsPrincipalFactory>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = ".ecommerce.auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(2);
    options.SlidingExpiration = true;
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
});
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddBlazoredLocalStorage(config =>
    {
        config.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
        config.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        config.JsonSerializerOptions.IgnoreReadOnlyProperties = true;
        config.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        config.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        config.JsonSerializerOptions.WriteIndented = false;
    }
);

// bind Redis options
builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));
//redis DI
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var options = sp.GetRequiredService<IOptions<RedisOptions>>().Value;
    var config = new ConfigurationOptions
    {
        EndPoints = { $"{options.Host}:{options.Port}" },
        Password = options.Password,
        DefaultDatabase = options.DefaultDatabase,
        Ssl = options.Ssl,
        ConnectTimeout = options.ConnectTimeout,
        SyncTimeout = options.SyncTimeout,
        AsyncTimeout = options.AsyncTimeout,
        AbortOnConnectFail = false
    };
    return ConnectionMultiplexer.Connect(config);
});
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// ELK DI


//adress api dl
builder.Services.Configure<ecommerce.Web.Domain.Dtos.Options.AddressApiOptions>(
    builder.Configuration.GetSection("AddressApiOptions"));
            
// adres di service inject
builder.Services.AddScoped<ecommerce.Web.Domain.Services.IAddressService, ecommerce.Web.Domain.Services.AddressService>();

builder.Services.AddHttpClient<ecommerce.Web.Domain.Services.IAddressService, ecommerce.Web.Domain.Services.AddressService>(client =>
{
    client.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout
    client.DefaultRequestHeaders.Add("User-Agent", "AddressService/1.0");
    client.DefaultRequestHeaders.Add("Connection", "keep-alive");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 10,
    UseCookies = false
})
.AddPolicyHandler(GetRetryPolicy());

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"Retry {retryCount} after {timespan} seconds");
            });
}

builder.Services.Configure<ElasticSearchOptions>(builder.Configuration.GetSection("ElasticSearch"));

builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;

    var pool = new SingleNodeConnectionPool(new Uri(options.Uri));
    var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default)
        .BasicAuthentication(options.Username, options.Password)
        .EnableDebugMode()
        .DefaultFieldNameInferrer(p => p); 

    return new ElasticClient(settings);
});

builder.Services.AddScoped<IElasticSearchService, ElasticSearchService>();

builder.Services.AddScoped<IBannerService, BannerService>();


builder.Services.AddSingleton<II18N>((_) =>
{
    var i18n = I18N.Current
        .AddLocaleReader(new JsonKvpReader(), ".json")
        .SetNotFoundSymbol("$")
        .SetFallbackLocale("tr-TR");

    i18n.Init(typeof(Program).Assembly);

    return i18n;
});
CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("tr-TR");
CultureInfo.CurrentCulture = new CultureInfo("tr-TR");
CultureInfo.CurrentUICulture = new CultureInfo("tr-TR");
Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
Thread.CurrentThread.CurrentUICulture = new CultureInfo("tr-TR");
Audit.Core.Configuration.AuditDisabled = true;


var cdnOptions = builder.Configuration.GetSection("Cdn").Get<CdnOptions>();
builder.Services.AddSingleton(cdnOptions);


builder.Services.AddAutoMapper(cfg =>
    {
        cfg.AddMaps("ecommerce.Admin.Domain");
        cfg.AddMaps("ecommerce.Web.Domain");
        cfg.AddCollectionMappers();
    }
);
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "image/svg+xml",
        "application/octet-stream"
    });
});

// Output Caching for performance optimization
builder.Services.AddOutputCache(options =>
{
    // Base policy: 10 seconds default
    options.AddBasePolicy(builder => 
        builder.Expire(TimeSpan.FromSeconds(10)));
    
    // Static pages: 1 hour cache
    options.AddPolicy("StaticPages", builder => 
        builder.Expire(TimeSpan.FromHours(1))
               .Tag("static"));
    
    // Product listings: 5 minutes, vary by query parameters
    options.AddPolicy("ProductListing", builder => 
        builder.Expire(TimeSpan.FromMinutes(5))
               .SetVaryByQuery("page", "sort", "category", "brand", "search")
               .Tag("products"));
    
    // Product detail: 10 minutes, vary by route
    options.AddPolicy("ProductDetail", builder => 
        builder.Expire(TimeSpan.FromMinutes(10))
               .SetVaryByRouteValue("id")
               .Tag("products"));
    
    // No cache for user-specific content
    options.AddPolicy("NoCache", builder => 
        builder.NoCache());
});

// WebOptimizer Bundling & Minification
builder.Services.AddWebOptimizer(pipeline =>
{
    // CSS Bundle - Vendors
    pipeline.AddCssBundle("/css/vendors.bundle.css",
        "assets/css/vendors/bootstrap.css",
        "assets/css/animate.min.css",
        "assets/css/vendors/ion.rangeSlider.min.css",
        "assets/css/font-awesome.min.css");

    // CSS Bundle - App
    pipeline.AddCssBundle("/css/app.bundle.css",
        "assets/css/font-style.css",
        "assets/css/header-search-fix.css",
        "assets/css/car-selection.css",
        "assets/css/brand-tooltip.css",
        "assets/css/style.css",
        "assets/css/custom.css",
        "assets/css/advanced-search.css",
        "assets/css/cart-cargo-selector.css",
        "assets/css/cargo-detail-modal.css");

    // JS Bundle - Vendors
    pipeline.AddJavaScriptBundle("/js/vendors.bundle.js",
        "assets/js/jquery-3.6.0.min.js",
        "assets/js/jquery-ui.min.js",
        "assets/js/bootstrap/bootstrap-notify.min.js",
        "assets/js/bootstrap/bootstrap.bundle.min.js",
        "assets/js/feather/feather.min.js",
        "assets/js/feather/feather-icon.js",
        "assets/js/lazysizes.min.js",
        "assets/js/slick/slick.js",
        "assets/js/slick/slick-animation.min.js",
        "assets/js/slick/custom_slick.js",
        "assets/js/slick/slick-init.js",
        "assets/js/sweetalert2.all.min.js",
        "assets/js/crypto-js.min.js",
        "assets/js/confetti.browser.min.js");

    // JS Bundle - App
    pipeline.AddJavaScriptBundle("/js/app.bundle.js",
        "assets/js/car-brand-logos.js",
        "assets/js/manufacturer-slider.js",
        "assets/js/MainLoad.js",
        "assets/js/sticky-cart-bottom.js",
        "assets/js/auto-height.js",
        "assets/js/timer1.js",
        "assets/js/fly-cart.js",
        "assets/js/quantity-2.js",
        "assets/js/ion.rangeSlider.min.js",
        "assets/js/filter-sidebar.js",
        "assets/js/wow.min.js",
        "assets/js/custom-wow.js",
        "assets/js/script.js",
        "assets/js/theme-setting.js",
        "assets/js/cookie.js",
        "assets/js/scroll-passive.js",
        "js/auth.js",
        "js/MenuCustom.js",
        "js/infinite-scroll.js",
        "assets/js/infinite-scroll.js");
});

var app = builder.Build();


app.UseRequestLocalization("tr-TR");
app.UseWebOptimizer();



// Uzantısız URL'leri .html dosyalarına yönlendir (App Store için gizlilik/iletişim sayfaları)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path != null && !Path.HasExtension(path))
    {
        var htmlPath = Path.Combine(app.Environment.WebRootPath, path.TrimStart('/') + ".html");
        if (File.Exists(htmlPath))
        {
            context.Request.Path = path + ".html";
        }
    }
    await next();
});

app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = r =>
    {
        var path = r.File.PhysicalPath;
        if (path.EndsWith(".gif") || path.EndsWith(".css") || path.EndsWith(".js") ||
            path.EndsWith(".json") || path.EndsWith(".jpg") || path.EndsWith(".png") ||
            path.EndsWith(".svg") || path.EndsWith(".webp") || path.EndsWith(".mp4") ||
            path.EndsWith(".ico") || path.EndsWith(".mjs") || path.EndsWith(".jpeg") ||
            path.EndsWith(".avi") || path.EndsWith(".woff2") || path.EndsWith(".woff"))
        {
            var maxAge = TimeSpan.FromDays(365); // 1 yıl
            r.Context.Response.Headers["Cache-Control"] = $"public,max-age={maxAge.TotalSeconds:0},immutable";
            r.Context.Response.Headers["Expires"] = DateTime.UtcNow.Add(maxAge).ToString("R");
        }
    }
});
app.UseResponseCompression(); // Always enabled for optimal performance
app.UseOutputCache(); // Output caching for performance



// Moved up

app.Use(async (context, next) =>
    {
        context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
        await next();
    }
);


if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);

    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<ecommerce.Domain.Shared.Middleware.LogContextMiddleware>();
app.UseMiddleware<ecommerce.Web.Domain.MiddleWares.OnlineUserTrackingMiddleware>();
app.UseMiddleware<CurrentUserMiddleware>();
// app.UseMiddleware<ExceptionLoggingMiddleware>();


app.UseAntiforgery();

app.MapControllers();

// Health Check Endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            totalDuration = report.TotalDuration,
            entries = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                duration = e.Value.Duration,
                description = e.Value.Description,
                tags = e.Value.Tags
            })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            entries = report.Entries.Select(e => new { name = e.Key, status = e.Value.Status.ToString() })
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false // Just returns 200 OK if app is running
});


app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();



_ = Task.Run(async () =>
{
    try
    {
        await Task.Delay(2000);
        using var scope = app.Services.CreateScope();
        var cacheService = scope.ServiceProvider.GetRequiredService<ecommerce.Web.Domain.Services.IManufacturerCacheService>();
        await cacheService.WarmupCacheAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Cache warmup failed: {ex.Message}");
    }
});

app.Run();