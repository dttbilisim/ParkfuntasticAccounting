using System.Reflection;
using AutoMapper.EquivalencyExpression;
using Polly;
using Polly.Extensions.Http;
using BasbugOto.Abstract;
using BasbugOto.Concreate;
using BasbugOto.Options;
using CurrencyAuto.Abstract;
using CurrencyAuto.Concreate;
using Dega.Abstract;
using Dega.Concreate;
using Dega.Options;
using Dot.Integration.Extensions;
using Dot.Integration.Services;
using ecommerce.Admin.ConfigureValidators;
using ecommerce.Admin.Domain.BackgroundServices;
using ecommerce.Admin.Domain.Concreate;
using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Domain.Report;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Helpers.Concretes;
using ecommerce.Admin.Helpers.Interfaces;
using ecommerce.Admin.Helpers;
using Castle.DynamicProxy;
using ecommerce.Admin.Services;
using ecommerce.Admin.Services.Concreate;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Interfaces;
using ecommerce.Payments;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Cargo.Mng;
using ecommerce.Cargo.Sendeo;
using ecommerce.Cargo.Yurtici;
using ecommerce.Core.Attributes;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Helpers;
using ecommerce.Core.Identity;
using ecommerce.Core.Rules;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Concreate;
using ecommerce.Domain.Shared.Dtos.Options;
using ecommerce.Domain.Shared.ElasticSearch.Abstract;
using ecommerce.Domain.Shared.ElasticSearch.Concreate;
using ecommerce.Domain.Shared.Emailing;
using ecommerce.Domain.Shared.Rules.Providers;
using ecommerce.Domain.Shared.Services;
using ecommerce.EFCore.Context;
using ecommerce.Iyzico.Payment.Concreate;
using ecommerce.Iyzico.Payment.Interface;

using FluentValidation;
using Hangfire;
using Hangfire.Common;
using Hangfire.PostgreSql;
using ImageDownload.Abstract;
using ImageDownload.Concreate;
using ImageDownload.Options;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Server.Circuits;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OtoIsmail.Abstract;
using OtoIsmail.Concreate;
using ecommerce.Domain.Shared.Options;
using Otokoc.Abstract;
using Otokoc.Concreate;
using Otokoc.Options;
using Radzen;
using Remar.Abstract;
using Remar.Concreate;
using Remar.Options;
using RestSharp;
using StackExchange.Redis;
using Telecom.Address.Abstract;
using Telecom.Address.Concreate;
using Telecom.Address.Options;
using EmailTemplateService = ecommerce.Admin.Domain.Concreate.EmailTemplateService;
using IEmailTemplateService = ecommerce.Admin.Domain.Interfaces.IEmailTemplateService;
using Nest;
using Nest.JsonNetSerializer;
using Elasticsearch.Net;

namespace ecommerce.Admin.AppStart{
    public static class ConfigureServices{
        public static void ConfigureAppServices(WebApplicationBuilder builder){
            builder.Services.AddScoped<ICategoryService, CategoryService>();
            builder.Services.AddScoped<IProductService, ProductService>();
            builder.Services.AddScoped<IBrandService, BrandService>();
            builder.Services.AddScoped<ITaxService, TaxService>();
            builder.Services.AddScoped<IProductTypeService, ProductTypeService>();
            builder.Services.AddScoped<IMembershipService, MembershipService>();
            builder.Services.AddScoped<ICityService, CityService>();
            builder.Services.AddScoped<ITownService, TownService>();
            builder.Services.AddScoped<ICompanyService, CompanyService>();
            builder.Services.AddScoped<IActiveArticlesService, ActiveArticlesService>();
            builder.Services.AddScoped<IProductActiveArticleService, ProductActiveArticleService>();
            builder.Services.AddScoped<IScaleUnitService, ScaleUnitService>();
            builder.Services.AddScoped<IPriceListService, PriceListService>();
            builder.Services.AddScoped<ICurrencyAdminService, CurrencyAdminService>();
            builder.Services.AddScoped<IProductActiveArticleService, ProductActiveArticleService>();
            builder.Services.AddScoped<IProductImageService, ProductImageService>();
            builder.Services.AddScoped<ICompanyRateService, CompanyRateService>();
            builder.Services.AddScoped<ITierService, TierService>();
            builder.Services.AddScoped<IInvoiceTypeService, InvoiceTypeService>();
            builder.Services.AddScoped<IInvoiceService, InvoiceService>();
            builder.Services.AddScoped<ICollectionReceiptService, CollectionReceiptService>();
            builder.Services.AddScoped<ICustomerAccountTransactionService, CustomerAccountTransactionService>();
            builder.Services.AddScoped<ICashRegisterMovementService, CashRegisterMovementService>();
            builder.Services.AddScoped<ICashRegisterService, CashRegisterService>();
            builder.Services.AddScoped<ICheckService, CheckService>();
            builder.Services.AddScoped<ICourierApplicationService, CourierApplicationService>();
            builder.Services.AddScoped<ICourierService, CourierService>();
            builder.Services.AddScoped<ICourierDeliveryService, CourierDeliveryService>();
            builder.Services.AddScoped<ICourierNotificationService, ecommerce.EP.Services.Concreate.CourierNotificationService>();
            builder.Services.AddScoped<ICourierLocationService, CourierLocationService>();
            builder.Services.AddScoped<IBankBranchService, BankBranchService>();
            builder.Services.AddScoped<ICorporationService, CorporationService>();
            builder.Services.AddScoped<IBranchService, BranchService>();
            builder.Services.AddScoped<IUserBranchService, UserBranchService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<ITenantProvider, TenantProvider>();
            builder.Services.AddScoped<ecommerce.Admin.Domain.Services.IRoleBasedFilterService, ecommerce.Admin.Domain.Services.RoleBasedFilterService>();
            builder.Services.AddScoped<BranchSwitcherService>();
            builder.Services.AddScoped<IExpenseDefinitionService, ExpenseDefinitionService>();
            builder.Services.AddScoped<IUnitService, UnitService>();
            builder.Services.AddScoped<IProductUnitService, ProductUnitService>();
            builder.Services.AddScoped<ISaleOptionsService, SaleOptionsService>();
            builder.Services.AddScoped<IPcPosService, PcPosService>();
            builder.Services.AddScoped<ISalesPersonService, SalesPersonService>();
            builder.Services.AddScoped<IRegionService, RegionService>();
            builder.Services.AddScoped<IProductCategoryService, ProductCategoryService>();
            builder.Services.AddScoped<IAppSettingService, AppSettingService>();
            builder.Services.AddScoped<IOrderService, OrderService>();
            builder.Services.AddHttpClient<IOtoIsmailService, OtoIsmailService>();
            builder.Services.AddScoped<IRealTimeStockProvider>(sp => (OtoIsmailService)sp.GetRequiredService<IOtoIsmailService>());
            builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.IRealTimeStockResolver, ecommerce.Admin.Domain.Concreate.RealTimeStockResolver>();
            builder.Services.AddScoped<IOrderItemService, OrderItemService>();
            builder.Services.AddScoped<IDiscountService, DiscountService>();
            builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ICargoService, ecommerce.Admin.Domain.Concreate.CargoService>();
            builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ICargoCreationService, ecommerce.Admin.Domain.Concreate.CargoCreationService>();
            builder.Services.AddScoped<ecommerce.Admin.Domain.Interfaces.ICargoPropertyService, ecommerce.Admin.Domain.Concreate.CargoPropertyService>();
            builder.Services.AddScoped<ICompanyCargoService, CompanyCargoService>();
            builder.Services.AddScoped<IProductTierService, ProductTierService>();
            builder.Services.AddScoped<IPaymentTypeService, PaymentTypeService>();
            builder.Services.AddScoped<IPharmacyTypeService, PharmacyTypeService>();
            builder.Services.AddScoped<INotificationEventService, NotificationEventService>();
            builder.Services.AddScoped<INotificationTypeService, NotificationTypeService>();
            // builder.Services.AddScoped<IElasticSearchService, ElasticSearchManager>();
            builder.Services.AddScoped<IElasticSearchConfigration, ElasticSearchConfigration>();
     

            // builder.Services.AddScoped<IBackgroundJobServices, BackgroundJobServices>();
            builder.Services.AddScoped<IPaymentService, PaymentService>();
            builder.Services.AddScoped<IIdentityUserService, IdentityUserService>();
            builder.Services.AddScoped<IRoleMenuService, RoleMenuService>();
            builder.Services.AddScoped<IRoleService, RoleService>();
            builder.Services.AddScoped<IMenuService, MenuService>();
            builder.Services.AddScoped<IUserMenuService, UserMenuService>();
            builder.Services.AddScoped<IEmailSender, EmailSender>();
            builder.Services.AddScoped<ecommerce.Domain.Shared.Emailing.EmailSendJob>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<ILogService, LogService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ISellerService, SellerService>();
            builder.Services.AddScoped<ISellerAddressService, SellerAddressService>();
            builder.Services.AddScoped<ISellerItemService, SellerItemService>();
            
            // VIN Service - PostgreSQL vin_get_models() kullanarak araç bazlı ürün eşleştirme
            builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IVinService, ecommerce.Admin.Services.Services.VinService>();
            builder.Services.AddScoped<IBankService, BankService>();
            builder.Services.AddScoped<IBankAccountDefinitionService, BankAccountDefinitionService>();
            // Payment Provider (required by CheckoutService, even though Admin doesn't use payment)
            builder.Services.AddScoped<IPaymentProviderFactory, CPPaymentProviderFactory>();
            builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.ICheckoutService, ecommerce.Web.Domain.Services.Concreate.CheckoutService>();
            builder.Services.AddProxiedScoped<ICustomerService, CustomerService>(); // AOP interceptor enabled
            builder.Services.AddValidatorsFromAssemblies(new[]{typeof(Validations).Assembly, Assembly.Load("ecommerce.Admin.Domain")}, filter:result => result.ValidatorType.GetCustomAttribute<DisableFluentValidatorRegistrationAttribute>() == null);
            builder.Services.AddScoped(typeof(IRadzenPagerService<>), typeof(RadzenPagerService<>));
            builder.Services.AddScoped<IStaticPageService, StaticPageService>();
            builder.Services.AddSingleton<FileHelper>();
            builder.Services.AddScoped<IFileService, FileService>();
            builder.Services.AddScoped<IFrequentlyAskedQuestionService, FrequentlyAskedQuestionService>();
            builder.Services.AddScoped<ISurveyService, SurveyService>();
            builder.Services.AddScoped<IEditorialContentService, EditorialContentService>();
            builder.Services.AddScoped<ISupportLineService, SupportLineService>();
            builder.Services.AddScoped<IPopupService, PopupService>();
            // Order Manager (DB tabanlı — Redis kullanılmıyor)
            builder.Services.AddScoped<ecommerce.Domain.Shared.Services.IOrderManager, ecommerce.Domain.Shared.Services.OrderManager>();
            builder.Services.AddScoped<IBannerService, BannerService>();
            builder.Services.AddScoped<IBannerItemService, BannerItemService>();
            builder.Services.AddScoped<IBannerSubItemService, BannerSubItemService>();
            // Remove RestSharp RestClient DI to avoid ambiguous constructor activation
         
            builder.Services.AddScoped<IProductGroupCodeService, ProductGroupCodeService>();
            builder.Services.AddScoped<IDapperService, DapperService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IEducationService, EducationService>();
            builder.Services.AddScoped<IEducationCalendarService, EducationCalendarService>();

            builder.Services.AddScoped<IOnlineMeetService, OnlineMeetService>();
            builder.Services.AddScoped<IZoomService, ZoomService>();
            
            builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            builder.Services.AddTransient<ecommerce.Domain.Shared.Services.IEmailTemplateService, ecommerce.Domain.Shared.Services.EmailTemplateService>();

            // Warehouse Management Services
            builder.Services.AddScoped<IWarehouseService, WarehouseService>();
            builder.Services.AddScoped<IWarehouseShelfService, WarehouseShelfService>();
            builder.Services.AddScoped<IProductStockService, ProductStockService>();

            builder.Services.AddScoped<JwtTokenGenerator>();
            
            builder.Services.AddScoped<IAdminProductSearchService, AdminProductSearchDbService>();
            builder.Services.AddScoped<ecommerce.Domain.Shared.Abstract.ISearchSynonymService, ecommerce.Domain.Shared.Services.SearchSynonymService>();
            builder.Services.AddScoped<ISearchSynonymAdminService, SearchSynonymAdminService>();
            builder.Services.AddScoped<IAppSettingService, AppSettingService>();
            
            // ElasticSearch Service (required by SellerProductService)
            builder.Services.AddScoped<ecommerce.Domain.Shared.Abstract.IElasticSearchService, ecommerce.Domain.Shared.Concreate.ElasticSearchService>();
            
            // UserCar Service (required by SellerProductService)
            builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IUserCarService, ecommerce.Web.Domain.Services.Concreate.UserCarService>();
            
            // RealTimeStockResolver for Web services (RedisCartService expects this namespace)
            builder.Services.AddScoped<ecommerce.Domain.Shared.Abstract.IRealTimeStockResolver, ecommerce.Domain.Shared.Services.RealTimeStockResolver>();
            
            // Cart State Service (UI State Management)
            builder.Services.AddScoped<ICartStateService, ecommerce.Admin.Services.Concreate.CartStateService>();
            
            // Seller Product Service (required by RedisCartService)
            builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.ISellerProductService, ecommerce.Web.Domain.Services.Concreate.SellerProductService>();
            
            // Cart Service for B2B customers (DB tabanlı — Redis kullanılmıyor)
            builder.Services.AddTransient<ecommerce.Web.Domain.Services.Abstract.ICartService, ecommerce.Web.Domain.Services.Concreate.CartService>();
            
            // Bank Service for payment processing
            builder.Services.AddScoped<ecommerce.Web.Domain.Services.Abstract.IBankService, ecommerce.Web.Domain.Services.Concreate.BankService>();

            
            // AOP / Interception Configuration
            builder.Services.AddSingleton<IProxyGenerator, ProxyGenerator>();
            builder.Services.AddScoped<IInterceptor, PermissionInterceptor>();
            // Note: Services needing permission checks must be registered with AddProxiedScoped via ConfigureServicesExtensions
            
            // Dynamic Permission Service
            builder.Services.AddScoped<ecommerce.Admin.Domain.Services.IPermissionService, ecommerce.Admin.Domain.Services.PermissionService>();
        }
        public static void Configure(WebApplicationBuilder builder){
            builder.Services.AddMemoryCache();
            
            // Dashboard Cache Service
            builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IDashboardCacheService, ecommerce.Admin.Services.Concreate.DashboardCacheService>();
            
            // Discount Cache Service
            builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IDiscountCacheService, ecommerce.Admin.Services.Concreate.DiscountCacheService>();
            
            // Push Notification Sayfa Servisi
            builder.Services.AddScoped<ecommerce.Admin.Services.Interfaces.IPushNotificationPageService, ecommerce.Admin.Services.Concreate.PushNotificationPageService>();
            
            // Push Notification Gönderim Job'ı — Development ortamında NullHangfireJobManager senkron çalıştırır
            builder.Services.AddScoped<ecommerce.Admin.Jobs.SendPushNotificationJob>();
            builder.Services.AddSingleton(sp => sp.GetRequiredService<ILoggerFactory>().CreateLogger("DefaultLogger"));
            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<HelperGeneral>();
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddScoped<ContextMenuService>();
            builder.Services.AddTransient<ComponentDebouncer>();
            // DbContext is configured in Program.cs with advanced options (NoTracking, retries, Multiplexing=false)
            // Avoid re-registering here to prevent overriding those options.
            builder.Services.AddHttpClient("ecommerce.Admin").AddHeaderPropagation(o => o.Headers.Add("Cookie"));
            builder.Services.AddHeaderPropagation(o => o.Headers.Add("Cookie"));
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = "auth_cookie"; // İsteğe bağlı isim
                    options.AccessDeniedPath = "/error-403"; // Yetki yoksa yönlendirilecek yer
                    options.Cookie.HttpOnly = true;
                    options.SlidingExpiration = true;
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                });
            builder.Services.AddAuthorization();
            builder.Services.AddScoped<CurrentUser>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Scoped<CircuitHandler, AuthenticationCircuitHandler>());
            builder.Services.AddScoped<AuthenticationService>();
            builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(
                    options => { options.User.RequireUniqueEmail = true; }
                )
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddClaimsPrincipalFactory<UserClaimsPrincipalFactory>()
                .AddErrorDescriber<CustomIdentityErrorDescriber>();
               
            builder.Services.AddUnitOfWork<ApplicationDbContext>();
            builder.Services.AddAutoMapper(cfg => {
                    cfg.AddMaps("ecommerce.Admin.Domain", "ecommerce.Web.Domain");
                    cfg.AddCollectionMappers();
                }
            );
            builder.Services.AddRules(options => {
                    options.Providers.Add<PopupFieldDefinitionProvider>();
                    options.Providers.Add<DiscountFieldDefinitionProvider>();
                }, new[]{"ecommerce.Domain.Shared"}
            );
            if (!builder.Environment.IsDevelopment())
            {
                builder.Services.AddBackgroundJobs();
                builder.Services.AddHangfire(config => {
                        // Stabilize Npgsql/Hangfire connection
                        var connStr = builder.Configuration.GetConnectionString("ApplicationDbContext");
                        if (!string.IsNullOrEmpty(connStr) && !connStr.Contains("Multiplexing", StringComparison.OrdinalIgnoreCase))
                        {
                            connStr += ";Multiplexing=false;KeepAlive=30;Timeout=180;Command Timeout=180";
                        }
                        config.UsePostgreSqlStorage(connStr, new PostgreSqlStorageOptions
                        {
                            QueuePollInterval = TimeSpan.FromSeconds(10),
                            InvisibilityTimeout = TimeSpan.FromMinutes(5),
                            DistributedLockTimeout = TimeSpan.FromMinutes(5),
                            UseNativeDatabaseTransactions = true
                        });
                        config.UseFilter(new AutomaticRetryAttribute{Attempts = 3, LogEvents = true});
                        config.UseFilter(new RetainSuccessJobsAttribute(100)); // Keep success jobs for 100 days
                        config.Use(new HangfireJobFilterAttributeFilterProvider(), filterProvider => {
                                var existingProvider = JobFilterProviders.Providers.FirstOrDefault(p => p is JobFilterAttributeFilterProvider);
                                if(existingProvider != null){
                                    JobFilterProviders.Providers.Remove(existingProvider);
                                }
                                JobFilterProviders.Providers.Add(filterProvider);
                            }
                        );
                    }
                );
                
                builder.Services.AddHangfireServer(options =>
                {
                    options.WorkerCount = 1; // Reduce to 1 for stability
                    options.Queues = new[] { "admin", "default" };
                });
            }
            else 
            {
                builder.Services.AddNullBackgroundJobs();
            }
            builder.Services.AddHttpClient();
            builder.Services.AddTransient<ImageProcessingService>();
            
            // CDN adresini okumak için — sunucuda Cdn bölümü yoksa varsayılan kullan (500.30 önlenir)
            var cdnOptions = builder.Configuration.GetSection("Cdn").Get<CdnOptions>()
                ?? new CdnOptions
                {
                    BaseUrl = "https://cdn.yedeksen.com/images/",
                    BrandImagesUrl = "https://cdn.yedeksen.com/images/BrandImages/",
                    CategoryImagesUrl = "https://cdn.yedeksen.com/images/CategoryImages/"
                };
            builder.Services.AddSingleton(cdnOptions);
            
            //kategori olusturma ML AI ile
           // builder.Services.AddScoped<ICategoryPredictorService, CategoryPredictorService>();
            
            // builder.Services.AddMngCargo(options => builder.Configuration.GetSection("Cargo:Mng").Bind(options));
            builder.Services.AddSendeoCargo(options => builder.Configuration.GetSection("Cargo:Sendeo").Bind(options));
            // builder.Services.AddYurticiCargo(options => builder.Configuration.GetSection("Cargo:Yurtici").Bind(options));
            
            // DAT Integration (SilverDAT API)
            builder.Services.AddDatIntegration(builder.Configuration);
            builder.Services.AddScoped<DatBulkSyncService>();
            
            
            // oto ismail icin servis DI
            builder.Services.Configure<OtoIsmailOptions>(builder.Configuration.GetSection("OtoIsmail"));
            builder.Services.AddHttpClient<ITokenService, TokenService>();
            builder.Services.AddHttpClient<IApiClient, ApiClient>();
            
            // Remar Oto icin servis DI
            builder.Services.Configure<RemarApiOptions>(builder.Configuration.GetSection("RemarApiOptions"));
            builder.Services.AddHttpClient<IRemarApiService, RemarApiService>();
            builder.Services.AddScoped<IRealTimeStockProvider>(sp => (RemarApiService)sp.GetRequiredService<IRemarApiService>());
            
            // basbug oto icin DI
            builder.Services.Configure<BasbugOptions>(builder.Configuration.GetSection("Basbug"));
            builder.Services.AddHttpClient<IBasbugApiService, BasbugApiService>();
            builder.Services.AddScoped<IRealTimeStockProvider, BasbugStockService>();
            
            // Dega Oto icin servis DI
            builder.Services.Configure<DegaApiOptions>(builder.Configuration.GetSection("DegaApiOptions"));
            builder.Services.AddHttpClient<IDegaService, DegaService>();
            builder.Services.AddScoped<IRealTimeStockProvider>(sp => (DegaService)sp.GetRequiredService<IDegaService>());
            
            // Otokoc icin servis DI
            builder.Services.Configure<OtokocOptions>(
                builder.Configuration.GetSection(OtokocOptions.SectionName));

            builder.Services.AddScoped<IOtokocService, OtokocService>();
            
            
            builder.Services.Configure<GoogleImageFetcherOptions>(builder.Configuration.GetSection("GoogleImageFetcher"));
            builder.Services.AddScoped<IGoogleImageFetcher, GoogleImageFetcher>();
            builder.Services.AddScoped<IImageStorageService, ImageStorageService>();
            
            //adress api dl
            builder.Services.Configure<AddressApiOptions>(
                builder.Configuration.GetSection("AddressApiOptions"));
            
            // adres di service inject
            builder.Services.AddScoped<IAddressSyncService, AddressSyncService>();

            builder.Services.AddHttpClient<IAddressApiService, AddressApiService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5); // 5 minute timeout
                client.DefaultRequestHeaders.Add("User-Agent", "AddressSyncService/1.0");
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
                return Polly.Policy
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
            
            //merkez bankasi doviz cekmesi icin DI
            
            builder.Services.AddHttpClient<ICurrencyService, CurrencyService>();
        
            builder.Services.AddHttpClient();
            
            //redis
            
            builder.Services.Configure<RedisOptions>(builder.Configuration.GetSection("Redis"));

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
                    AbortOnConnectFail = false
                };
                return ConnectionMultiplexer.Connect(config);
            });

            builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
            builder.Services.AddScoped<ISearchAnalyticsService, SearchAnalyticsService>();
            
            
            ConfigureAppServices(builder);
            
            // ElasticSearch Configuration (from Web)
            builder.Services.Configure<ElasticSearchOptions>(builder.Configuration.GetSection("ElasticSearch"));
            builder.Services.AddSingleton<IElasticClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<ElasticSearchOptions>>().Value;
                var pool = new SingleNodeConnectionPool(new Uri(options.Uri));
                var settings = new ConnectionSettings(pool, sourceSerializer: JsonNetSerializer.Default)
                    .BasicAuthentication(options.Username, options.Password)
                    .EnableDebugMode()
                    .DefaultFieldNameInferrer(p => p)
                    .RequestTimeout(TimeSpan.FromSeconds(60));  // Global timeout: 60 saniye
                return new ElasticClient(settings);
            });

            // Expo Push Bildirim Servisi — Admin panelden bildirim gönderimi için
            builder.Services.AddHttpClient<ecommerce.EP.Services.Abstract.IExpoPushService, ecommerce.EP.Services.Concreate.ExpoPushService>(client =>
            {
                client.BaseAddress = new Uri("https://exp.host/--/api/v2/push/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            })
            .AddPolicyHandler(HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

            // Web Push Bildirim Servisi — VAPID ile tarayıcı bildirimleri
            builder.Services.AddSingleton<ecommerce.EP.Services.Abstract.IWebPushService, ecommerce.EP.Services.Concreate.WebPushService>();

            Audit.Core.Configuration.AuditDisabled = true;
        }
    }
}
