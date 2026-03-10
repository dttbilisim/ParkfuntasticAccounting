using System.Text;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Interfaces;
using ecommerce.EFCore.Context;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Concreate;
using ecommerce.Domain.Shared.Services;
using ecommerce.Core.Identity;
using ecommerce.Admin.EFCore.UnitOfWork;
// using ecommerce.Admin.Domain; // Removed
// using ecommerce.Admin.Domain.Services; // Removed
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Admin.Services.Concreate;
using StackExchange.Redis;
using ecommerce.Core.Rules;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Domain.Shared.Rules.Providers;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Concreate;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.Odaksodt.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;
using Polly;
using Nest;
using Nest.JsonNetSerializer;
using Elasticsearch.Net;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Serilog;
using Serilog.Sinks.Elasticsearch;
using Serilog.Exceptions;
using ecommerce.EP.Configuration;
using Audit.Core;
using Minio;

// EF Core Audit — EP (Mobile API) audit kullanmıyor; tamamen kapat (log/sink tetiklenmesin)
Audit.Core.Configuration.AuditDisabled = true;

// PostgreSQL DateTime uyumluluğu
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Audit'ü config'den de kapat (appsettings "Audit:Disabled": true ile override edilebilir)
var auditDisabled = builder.Configuration.GetValue<bool>("Audit:Disabled", true);
Audit.Core.Configuration.AuditDisabled = auditDisabled;

// Serilog Yapılandırması
builder.Host.UseSerilog((context, services, configuration) => {
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "Mobile")
        .Enrich.WithExceptionDetails()
        .WriteTo.Console(); // Console'a her zaman yaz

    var elasticUri = context.Configuration["ElasticSearch:Uri"];
    if (!string.IsNullOrEmpty(elasticUri))
    {
        configuration.WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticUri))
        {
            AutoRegisterTemplate = true,
            IndexFormat = $"ecommerce-logs-mobile-{context.HostingEnvironment.EnvironmentName?.ToLower().Replace(".", "-")}-{{0:yyyy.MM}}",
            ModifyConnectionSettings = x => x.BasicAuthentication(
                context.Configuration["ElasticSearch:Username"], 
                context.Configuration["ElasticSearch:Password"]),
            NumberOfShards = 2,
            NumberOfReplicas = 1,
            // ILM Policy ile 30 gün sonra otomatik silinir
            TemplateName = "ecommerce-logs-mobile-template",
            AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
            OverwriteTemplate = false,
            RegisterTemplateFailure = RegisterTemplateRecovery.IndexAnyway
        });
    }
});

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddMemoryCache();

// Response compression — JSON/API yanıtları için performans (Brotli > Gzip)
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
});

// Rate limiting — brute force ve DoS koruması, kullanıcıyı kilitlemeden
builder.Services.AddApiRateLimiting(builder.Configuration);

// CORS Policy — Sadece belirli origin'lerden gelen istekleri kabul et
builder.Services.AddCors(options =>
{
    options.AddPolicy("MobileAppPolicy", policy =>
    {
        // Development: Tüm origin'lere izin ver (test için)
        // Production: Sadece domain'inizi ekleyin
        if (builder.Environment.IsDevelopment())
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            // Production: Sadece kendi domain'iniz
            policy.WithOrigins("https://yedeksen.com", "https://api.yedeksen.com","http://192.168.1.15:5274/api")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});
builder.Services.AddAutoMapper(typeof(ecommerce.Web.Domain.AutoMapperProfile));
builder.Services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultLogger"));
builder.Services.AddSingleton<ecommerce.Core.Helpers.FileHelper>();

// MinIO: Ayarlar dolu ise kurye belgeleri MinIO'ya yüklenir; boş ise mevcut dosya servisi kullanılır (yapı bozulmaz).
builder.Services.Configure<ecommerce.EP.Configuration.MinioOptions>(builder.Configuration.GetSection(ecommerce.EP.Configuration.MinioOptions.SectionName));
builder.Services.Configure<ecommerce.EP.Configuration.EpBranchOptions>(builder.Configuration.GetSection(ecommerce.EP.Configuration.EpBranchOptions.SectionName));
var minioOptions = builder.Configuration.GetSection(ecommerce.EP.Configuration.MinioOptions.SectionName).Get<ecommerce.EP.Configuration.MinioOptions>() ?? new ecommerce.EP.Configuration.MinioOptions();
if (minioOptions.IsConfigured)
{
    builder.Services.AddSingleton<Minio.IMinioClient>(sp =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        var section = config.GetSection(ecommerce.EP.Configuration.MinioOptions.SectionName);
        var endpoint = section.GetValue<string>("Endpoint") ?? "";
        var port = section.GetValue<int>("Port", 9000);
        var useSsl = section.GetValue<bool>("UseSSL");
        var accessKey = section.GetValue<string>("AccessKey") ?? "";
        var secretKey = section.GetValue<string>("SecretKey") ?? "";
        var loggerFactory = sp.GetService<ILoggerFactory>();
        loggerFactory?.CreateLogger("MinIO").LogInformation("MinIO client: {Endpoint}:{Port} (S3 API portu; Console 9040 kullanılırsa Broken pipe olur)", endpoint, port);
        return new MinioClient().WithEndpoint(endpoint, port).WithCredentials(accessKey, secretKey).WithSSL(useSsl).Build();
    });
    builder.Services.AddScoped<ecommerce.EP.Services.CourierDocumentUploadService>();
    builder.Services.AddScoped<ecommerce.EP.Services.ICourierDocumentUploadService, ecommerce.EP.Services.MinioCourierDocumentUploadService>();
    builder.Services.AddScoped<ecommerce.EP.Services.ICourierDocumentUrlProvider, ecommerce.EP.Services.MinioCourierDocumentUrlProvider>();
}
else
{
    builder.Services.AddScoped<ecommerce.EP.Services.ICourierDocumentUploadService, ecommerce.EP.Services.CourierDocumentUploadService>();
    builder.Services.AddScoped<ecommerce.EP.Services.ICourierDocumentUrlProvider, ecommerce.EP.Services.FileHelperCourierDocumentUrlProvider>();
}

// Swagger Configuration
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { 
        Title = "ecommerce EP API", 
        Version = "v1",
        Description = "Mobile Application API" 
    });

    // Add JWT Authorization support in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // Set the comments path for the Swagger JSON and UI.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

// DbContext — ApplicationDbContext constructor'da scoped ITenantProvider kullanıldığı için
// AddDbContextPool kullanılamaz (pool root provider ile context oluşturur, scoped servis çözülemez).
// AddDbContext ile her request kendi scope'unda context alır, ITenantProvider çözülür.
// Npgsql: MinPoolSize/MaxPoolSize ortama göre appsettings.{Environment}.json ile override edilebilir (cold start için MinPoolSize önerilir).
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ApplicationDbContext") ?? "";
    var npgsqlMin = builder.Configuration.GetValue<int?>("Npgsql:MinPoolSize");
    var npgsqlMax = builder.Configuration.GetValue<int?>("Npgsql:MaxPoolSize");
    if (npgsqlMin.HasValue || npgsqlMax.HasValue)
    {
        var csb = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
        if (npgsqlMin.HasValue) csb.MinPoolSize = npgsqlMin.Value;
        if (npgsqlMax.HasValue) csb.MaxPoolSize = npgsqlMax.Value;
        connectionString = csb.ToString();
    }
    options.UseNpgsql(connectionString, o =>
    {
        o.MigrationsAssembly("ecommerce.EFCore");
    });
});

// CourierService / CourierApplicationService ayrı context için IDbContextFactory kullanır (ITenantProvider scoped olduğu için özel factory).
builder.Services.AddSingleton<IDbContextFactory<ApplicationDbContext>, ecommerce.EP.Services.EpApplicationDbContextFactory>();

// Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options => {
    options.User.RequireUniqueEmail = true;
    
    // Basit şifre politikası (kullanıcı dostu)
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // Hesap kilitleme — esnek: kullanıcıları gereksiz kilitlemez, yine de brute force’a karşı korur
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(3);
    options.Lockout.MaxFailedAccessAttempts = 10;
    options.Lockout.AllowedForNewUsers = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? "Default_Strong_Key_For_Development_Only_123456");

builder.Services.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => {
    options.TokenValidationParameters = new TokenValidationParameters {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "ecommerce.EP",
        ValidAudience = jwtSettings["Audience"] ?? "ecommerce.App",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Elasticsearch
builder.Services.AddSingleton<IElasticClient>(sp =>
{
    var elasticSettings = builder.Configuration.GetSection("ElasticSearch");
    var uri = elasticSettings["Uri"] ?? "http://localhost:9200";
    var pool = new SingleNodeConnectionPool(new Uri(uri));
    var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default)
        .BasicAuthentication(elasticSettings["Username"], elasticSettings["Password"])
        .DefaultFieldNameInferrer(p => p);
    return new ElasticClient(settings);
});

// Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisSettings = builder.Configuration.GetSection("Redis");
    var options = new ConfigurationOptions
    {
        EndPoints = { { redisSettings["Host"] ?? "localhost", int.Parse(redisSettings["Port"] ?? "6379") } },
        Password = redisSettings["Password"],
        DefaultDatabase = int.Parse(redisSettings["DefaultDatabase"] ?? "0"),
        Ssl = bool.Parse(redisSettings["Ssl"] ?? "false"),
        ConnectTimeout = int.Parse(redisSettings["ConnectTimeout"] ?? "5000"),
        AbortOnConnectFail = false,
        KeepAlive = 30, // Redis bağlantısını canlı tut — 30 saniyede bir heartbeat
        ConnectRetry = 3, // Bağlantı koptuğunda 3 kez dene
        ReconnectRetryPolicy = new ExponentialRetry(5000), // Üstel geri çekilme ile yeniden bağlan
    };
    return ConnectionMultiplexer.Connect(options);
});

// Infrastructure Services
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<CurrentUser>();
builder.Services.AddScoped<ecommerce.Admin.Services.Concreate.TenantProvider>();
builder.Services.AddScoped<ITenantProvider, ecommerce.EP.Services.EpTenantProvider>();

// Domain Services
builder.Services.AddUnitOfWork<ApplicationDbContext>();
// Admin Services - Ortak Katman (Tüm servisler Admin.Services'den)
builder.Services.AddScoped<ecommerce.Admin.Domain.Services.IRoleBasedFilterService, ecommerce.Admin.Domain.Services.RoleBasedFilterService>();
builder.Services.AddScoped<ecommerce.Admin.Domain.Services.IPermissionService, ecommerce.Admin.Domain.Services.PermissionService>();
builder.Services.AddScoped<IAdminProductSearchService, AdminProductSearchDbService>();
builder.Services.AddScoped<ISearchSynonymService, SearchSynonymService>();

// Location Services - Kayıt formu için il/ilçe servisleri
builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ICityService, ecommerce.Admin.Domain.Concreate.CityService>();
builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ITownService, ecommerce.Admin.Domain.Concreate.TownService>();

// SellerService - Admin.Domain'den (AdminProductSearchService için gerekli)
builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ISellerService, ecommerce.Admin.Domain.Concreate.SellerService>();

// VinService - Admin.Services'den (Ortak Katman)
// sasisorgulama.com scraping için HttpClient kaydı
builder.Services.AddHttpClient("SasiSorgulama", client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IVinService, ecommerce.Admin.Services.Services.VinService>();

// ManufacturerElasticService - Web.Domain'den (Garaj marka/model listeleme için)
builder.Services.AddScoped<ecommerce.Web.Domain.Services.IManufacturerElasticService, ecommerce.Web.Domain.Services.ManufacturerElasticService>();

// DotIntegrationService - Web.Domain'den (Garaj araç tipi/kasa/motor/ek özellik listeleme için)
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IDotIntegrationService, ecommerce.Web.Domain.Services.Concreate.DotIntegrationService>();

// RecentSearchService - Admin.Services'den (Ortak Katman)
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IRecentSearchService, ecommerce.Admin.Services.Concreate.RecentSearchService>();

// CustomerAccountTransactionService - Admin.Services'den (Cari hesap işlemleri için)
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICustomerAccountTransactionService, ecommerce.Admin.Services.Concreate.CustomerAccountTransactionService>();

// DiscountService - Admin.Services'den (Kampanyalar için)
builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.IDiscountService, ecommerce.Admin.Domain.Concreate.DiscountService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IDiscountCacheService, ecommerce.Admin.Services.Concreate.DiscountCacheService>();

builder.Services.AddScoped<ecommerce.Domain.Shared.Abstract.IRedisCacheService, ecommerce.Domain.Shared.Concreate.RedisCacheService>();
builder.Services.AddScoped<ecommerce.Domain.Shared.Abstract.IElasticSearchService, ecommerce.Domain.Shared.Concreate.ElasticSearchService>();
// UserCarService & SellerProductService - RedisCartService için gerekli
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IUserCarService, ecommerce.Web.Domain.Services.Concreate.UserCarService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.ISellerProductService, ecommerce.Web.Domain.Services.Concreate.SellerProductService>();

// OrderManager — Redis decorator ile sarmalıyoruz (Web projesiyle aynı yapı)
builder.Services.AddScoped<OrderManager>();
builder.Services.AddScoped<IOrderManager>(sp =>
{
    var inner = sp.GetRequiredService<OrderManager>();
    var redis = sp.GetRequiredService<IConnectionMultiplexer>();
    var context = sp.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    var tenantProvider = sp.GetRequiredService<ITenantProvider>();
    var currentUser = sp.GetRequiredService<CurrentUser>();
    return new ecommerce.Web.Domain.Services.Concreate.RedisOrderManagerDecorator(
        inner, redis, context, httpContextAccessor, configuration, tenantProvider, currentUser);
});

// CartService — Redis tabanlı (Web projesiyle aynı yapı, sepet Redis'ten okunuyor)
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.ICartService, ecommerce.Web.Domain.Services.Concreate.RedisCartService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.ICheckoutService, ecommerce.Web.Domain.Services.Concreate.CheckoutService>();
builder.Services.AddScoped<ecommerce.Domain.Shared.Abstract.IRealTimeStockResolver, ecommerce.Domain.Shared.Services.RealTimeStockResolver>();

// Rules & Fields
builder.Services.AddRules(options => {
    options.Providers.Add<PopupFieldDefinitionProvider>();
    options.Providers.Add<DiscountFieldDefinitionProvider>();
}, new[] { "ecommerce.Domain.Shared" });

// Background Jobs (Satisfy dependencies for EmailService etc.)
builder.Services.AddNullBackgroundJobs();

// E-posta gönderimi — EmailService job ile kuyruğa alır; job çalışması için aşağıdakiler gerekli
builder.Services.AddScoped<ecommerce.Domain.Shared.Emailing.IEmailSender, ecommerce.Domain.Shared.Emailing.EmailSender>();
builder.Services.AddScoped<ecommerce.Domain.Shared.Services.IEmailTemplateService, ecommerce.Domain.Shared.Services.EmailTemplateService>();
builder.Services.AddScoped<ecommerce.Domain.Shared.Emailing.EmailSendJob>();

// Mobil API Servisleri - Web.Domain'den (Mobil API için)
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IBankService, ecommerce.Web.Domain.Services.Concreate.BankService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.ICategoryService, ecommerce.Web.Domain.Services.Concreate.CategoryService>();
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IBrandService, ecommerce.Web.Domain.Services.Concreate.BrandService>();
builder.Services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IPaymentReceiptPdfService, ecommerce.Admin.Services.Concreate.PaymentReceiptPdfService>();

// Uygulama başlangıcında bağlantı havuzlarını ısıt (cold start önleme)
builder.Services.AddHostedService<ecommerce.EP.Services.WarmupService>();

// Token temizleme servisi — 30 günden eski token'ları pasif olarak işaretler
builder.Services.AddScoped<ecommerce.EP.Services.Abstract.ITokenCleanupService, ecommerce.EP.Services.Concreate.TokenCleanupService>();
builder.Services.AddHostedService<ecommerce.EP.Services.TokenCleanupBackgroundService>();

// Expo Push Bildirim Servisi — Polly retry policy ile (3 deneme, üstel geri çekilme)
builder.Services.AddHttpClient<ecommerce.EP.Services.Abstract.IExpoPushService, ecommerce.EP.Services.Concreate.ExpoPushService>(client =>
{
    client.BaseAddress = new Uri("https://exp.host/--/api/v2/push/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(3, retryAttempt =>
    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Web Push Bildirim Servisi — VAPID ile tarayıcı bildirimleri
builder.Services.AddSingleton<ecommerce.EP.Services.Abstract.IWebPushService, ecommerce.EP.Services.Concreate.WebPushService>();

// Ödeme Provider'ları — IHttpClientFactory bağımlılığı için AddHttpClient gerekli
builder.Services.AddHttpClient();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.NestPayPaymentProvider>();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.DenizbankPaymentProvider>();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.FinansbankPaymentProvider>();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.GarantiPaymentProvider>();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.KuveytTurkPaymentProvider>();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.VakifbankPaymentProvider>();
builder.Services.AddTransient<ecommerce.Virtual.Pos.Providers.PosnetPaymentProvider>();

// Sipariş Servisleri
builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IUserOrderService, ecommerce.Web.Domain.Services.Concreate.UserOrderService>();

// Plasiyer Servisleri
builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ISalesPersonService, ecommerce.Admin.Domain.Concreate.SalesPersonService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICollectionReceiptService, ecommerce.Admin.Services.Concreate.CollectionReceiptService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IPaymentCollectionService, ecommerce.Admin.Services.Concreate.PaymentCollectionService>();
builder.Services.AddScoped(typeof(ecommerce.Admin.Domain.Interfaces.IRadzenPagerService<>), typeof(ecommerce.Admin.Domain.Concreate.RadzenPagerService<>));
builder.Services.AddAutoMapper(typeof(ecommerce.Admin.Domain.AutoMapperProfile));

// Kurye modülü (Kuryem Olur musun?) — başvuru, teslimat seçenekleri, kurye siparişleri
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICourierApplicationService, ecommerce.Admin.Services.Concreate.CourierApplicationService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICourierService, ecommerce.Admin.Services.Concreate.CourierService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICourierDeliveryService, ecommerce.Admin.Services.Concreate.CourierDeliveryService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICourierLocationService, ecommerce.Admin.Services.Concreate.CourierLocationService>();
builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.ICourierNotificationService, ecommerce.EP.Services.Concreate.CourierNotificationService>();

// Odaksoft E-Fatura entegrasyonu
builder.Services.AddOdaksoftServices(builder.Configuration);

var app = builder.Build();

// Audit'ü pipeline öncesi tekrar kapat (servis kayıtları sırasında açılmış olmasın)
Audit.Core.Configuration.AuditDisabled = true;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI(c => {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "ecommerce EP API v1");
        c.RoutePrefix = "swagger"; // Access at /swagger
    });
}

// Security Headers — XSS, Clickjacking, MIME sniffing koruması
app.Use(async (context, next) =>
{
    // HSTS — HTTPS zorunlu (production)
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    
    // Clickjacking koruması — iframe'de açılmasını engelle
    context.Response.Headers["X-Frame-Options"] = "DENY";
    
    // MIME sniffing koruması — tarayıcının content-type'ı değiştirmesini engelle
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    
    // XSS koruması (eski tarayıcılar için)
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    
    // Referrer policy — hassas bilgilerin URL'de sızmasını engelle
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    
    await next();
});

// Response compression — JSON yanıtları sıkıştır (performans)
app.UseResponseCompression();

app.UseHttpsRedirection();

// CORS middleware
app.UseCors("MobileAppPolicy");

// Rate limiter — CORS'tan sonra, Authentication'dan önce
app.UseRateLimiter();

// Serilog HTTP Request Logging - Tüm HTTP isteklerini logla
app.UseSerilogRequestLogging(options =>
{
    options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
        diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        
        // X-Forwarded-For check for proxies
        if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
        {
            diagnosticContext.Set("ClientIp", httpContext.Request.Headers["X-Forwarded-For"].ToString());
        }
        
        // User bilgileri
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            diagnosticContext.Set("Username", httpContext.User.Identity.Name);
            diagnosticContext.Set("UserId", httpContext.User.FindFirst("sub")?.Value ?? 
                                           httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);
        }
    };
});

app.UseAuthentication();
app.UseAuthorization();

// HTTP Request/Response Logging Middleware
app.UseMiddleware<ecommerce.Domain.Shared.Middleware.LogContextMiddleware>();

// CurrentUser claim'lerini zenginleştir (ActiveBranchId, ActiveCorporationId vb.)
app.UseMiddleware<ecommerce.Domain.Shared.Middleware.CurrentUserMiddleware>();

app.MapControllers();

// Graceful shutdown - uygulama kapanırken açık bağlantıları temiz kapat
app.Lifetime.ApplicationStopping.Register(() =>
{
    Log.Information("API kapatılıyor, açık bağlantılar temizleniyor...");
});
app.Lifetime.ApplicationStopped.Register(() =>
{
    Log.Information("API kapatıldı.");
    Log.CloseAndFlush();
});

await app.RunAsync();
return 0;