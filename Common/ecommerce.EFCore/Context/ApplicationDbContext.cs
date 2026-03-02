using Audit.EntityFramework;
using ecommerce.Core.ApiEntity;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Entities.ApiEntity;
using ecommerce.Core.Entities.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ecommerce.Core.Entities.Warehouse;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Interfaces;
namespace ecommerce.EFCore.Context
{
    public class ApplicationDbContext : AuditIdentityDbContext<ApplicationUser, ApplicationRole, int, IdentityUserClaim<int>
        , IdentityUserRole<int>, IdentityUserLogin<int>, IdentityRoleClaim<int>, IdentityUserToken<int>>
    {
        private readonly ITenantProvider _tenantProvider;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantProvider tenantProvider) : base(options)
        {
            _tenantProvider = tenantProvider;
        }

        public virtual DbSet<Customer> Customers { get; set; } = null!;

        #region [Just Admin]
        public virtual DbSet<Menu> Menus { get; set; } = null!;
        public virtual DbSet<RoleMenu> RoleMenus { get; set; } = null!;
        public virtual DbSet<UserMenu> UserMenus { get; set; } = null!;

        #endregion
        public virtual DbSet<ApplicationUser> AspNetUsers { get; set; } = null!;
        public virtual DbSet<Product> Product { get; set; }
        public virtual DbSet<ProductTier> ProductTiers { get; set; }

        public virtual DbSet<Category> Category { get; set; }
        public virtual DbSet<Brand> Brand { get; set; }
        public virtual DbSet<ProductType> ProductType { get; set; }
        public virtual DbSet<ProductCategories> ProductCategories { get; set; }
        public virtual DbSet<Tax> Tax { get; set; }
      
        public virtual DbSet<City> City { get; set; }
        public virtual DbSet<Town> Town { get; set; }
        
      
        public virtual DbSet<Neighboor> Neighboors{ get; set; }
        public virtual DbSet<Street> Street { get; set; } 
        public virtual DbSet<Building> Buildings { get; set; }
        public virtual DbSet<Home> Homes{ get; set; }
        public virtual DbSet<AddressInf> AddressInfos { get; set; }
        public virtual DbSet<DatProcessedLog> DatProcessedLogs{get;set;}


        public virtual DbSet<Membership> Membership { get; set; }
        
        public virtual DbSet<MembershipActivation> MembershipActivation { get; set; }

        public virtual DbSet<Company> Company { get; set; }
        public virtual DbSet<CompanyCargo> CompanyCargoes { get; set; }
        public virtual DbSet<CompanyRate> CompanyRate { get; set; }
        public virtual DbSet<Tier> Tiers { get; set; }
        public virtual DbSet<InvoiceTypeDefinition> InvoiceTypes { get; set; }
        public virtual DbSet<PcPosDefinition> PcPosDefinitions { get; set; }
        public virtual DbSet<SalesPerson> SalesPersons { get; set; }
        public DbSet<CustomerBranch> CustomerBranches { get; set; }
        public virtual DbSet<Region> Regions { get; set; }
        public virtual DbSet<CustomerPlasiyer> CustomerPlasiyers { get; set; }
        public virtual DbSet<Month> Months { get; set; }
        public virtual DbSet<CustomerWorkPlan> CustomerWorkPlans { get; set; }
        public virtual DbSet<PaymentType> PaymentTypes { get; set; }


        public virtual DbSet<ActiveArticle> ActiveArticles { get; set; }
        public virtual DbSet<ScaleUnit> ScaleUnits { get; set; }
        public virtual DbSet<ProductActiveArticleItem> ProductActiveArticleItems { get; set; }
        public virtual DbSet<ProductPriceHistory> ProductPriceHistories { get; set; }
        public virtual DbSet<ProductImage> ProductImages { get; set; }

        public virtual DbSet<AppSettings> AppSettings { get; set; }
        public virtual DbSet<ProductSellerItem> ProductSellerItems { get; set; }

        public virtual DbSet<Discount> Discounts { get; set; }
        public virtual DbSet<DiscountCompanyCoupon> DiscountCouponCompanies { get; set; }

        public virtual DbSet<Cargo> Cargoes { get; set; }

        public virtual DbSet<CargoProperty> CargoProperties { get; set; }
        public virtual DbSet<NotificationType> NotificationTypes { get; set; }
        public virtual DbSet<NotificationEvent> NotificationEvents { get; set; }
        public virtual DbSet<CartItem> CartItems { get; set; }
        public virtual DbSet<CreditCard> CreditCards { get; set; }
        public virtual DbSet<Orders> Orders { get; set; }
        public virtual DbSet<OrderItems> OrderItems { get; set; }
        public virtual DbSet<OrderAppliedDiscount> OrderAppliedDiscounts { get; set; } = null!;

        /// <summary>Kuryem Olur musun: Onaylanmış kuryeler.</summary>
        public virtual DbSet<Courier> Couriers { get; set; } = null!;
        public virtual DbSet<CourierApplication> CourierApplications { get; set; } = null!;
        public virtual DbSet<CourierServiceArea> CourierServiceAreas { get; set; } = null!;
        public virtual DbSet<CourierVehicle> CourierVehicles { get; set; } = null!;
        public virtual DbSet<CourierLocation> CourierLocations { get; set; } = null!;

        public virtual DbSet<StaticPage> AboutUs { get; set; }


        public virtual DbSet<FrequentlyAskedQuestion> FrequentlyAskedQuestions { get; set; }

        public virtual DbSet<Survey> Surveys { get; set; }
        public virtual DbSet<SurveyOption> SurveyOptions { get; set; }
        public virtual DbSet<SurveyAnswer> SurveyAnswers { get; set; }
        public virtual DbSet<PointTransaction> PointTransactions { get; set; }

        public virtual DbSet<EditorialContent> EditorialContents { get; set; }

        public virtual DbSet<SupportLine> SupportLines{get;set;}
        public virtual DbSet<Notification> Notifications{get;set;}

        public virtual DbSet<Popup> Popups { get; set; }
        public virtual DbSet<MyFavorites> MyFavorites { get; set; }
        public virtual DbSet<PaymentTemp> PaymentTemps{get;set;}

        public virtual DbSet<Banner>Banners { get; set; }
        public virtual DbSet<BannerItem> BannerItems { get; set; }
        public virtual DbSet<BannerSubItem> BannerSubItems { get; set; }
       
        public virtual DbSet<Weather> Weathers{get;set;}
        public virtual DbSet<PharmacyData> PharmacyDatas{get;set;}
        public virtual DbSet<ProductGroupCode> ProductGroupCodes{get;set;}
        public virtual DbSet<Appointment> Appointments{get;set;}
        public virtual DbSet<EducationCalendar> EducationCalendars{get;set;}
        public virtual DbSet<CurrencyData> CurrencyData{get;set;}
        public virtual DbSet<ReportStorage>ReportStorages {get;set;}
        public virtual DbSet<CompanyLogger> CompanyLogger{get;set;}
        public virtual DbSet<SearchLogger> SearchLogger{get;set;}
        public virtual DbSet<ProductTransaction> ProductTransaction{get;set;}
        public virtual DbSet<EducationCategory> EducationCategories{get;set;}
        public virtual DbSet<Education> Educations{get;set;}
        public virtual DbSet<EducationItems> EducationItems{get;set;}
        public virtual DbSet<EducationImages> EducationImages{get;set;}

        public virtual DbSet<CompanyDocument> CompanyDocuments{get;set;}
        public virtual DbSet<DocumentFile> Documents{get;set;}
        public virtual DbSet<CompanyWareHouse> CompanyWareHouses{get;set;}
        public virtual DbSet<OrderInvoice> OrderInvoices{get;set;}
        public virtual DbSet<ApiUser> ApiUser{get;set;}
        public virtual DbSet<ApiCompany> ApiCompany{get;set;}
        public virtual DbSet<CompanyInterview> CompanyInterviews{get;set;}
        public virtual DbSet<ProductOnline> ProductOnlines{get;set;}
        public virtual DbSet<AdvertOther> AdvertOthers{get;set;}
        public virtual DbSet<OnlineMeetCalender> OnlineMeetCalenders{get;set;}
        public virtual DbSet<OnlineMeetCalendarPharmacy> OnlineMeetCalendarPharmacies{get;set;}
        public virtual DbSet<CompanyMeet> CompanyMeets{get;set;}
        public virtual DbSet<ProductOtoIsmail> ProductOtoIsmails{get;set;}
        public virtual DbSet<ProductRemar> ProductRemars{get;set;}
        public virtual DbSet<ProductBasbug> ProductBasbugs{get;set;}
        public virtual DbSet<ProductDega> ProductDegas{get;set;}
        public virtual DbSet<ProductOtokoc> ProductOtokocs{get;set;}
        
        public virtual DbSet<EmailTemplates> EmailTemplates{get;set;}
        public virtual DbSet<OemToCars> OemToCars{get;set;}
        
        
        
        //new web user
        
        public virtual DbSet<User> Users{get;set;}
        public virtual DbSet<UserAddress> UserAddresses{get;set;}
        
        public virtual DbSet<Seller> Sellers{get;set;}
        public virtual DbSet<SellerItem> SellerItems {get;set;}
        public virtual DbSet<SellerAddress> SellerAddresses {get;set;}
        
        // Cookie
        public DbSet<GlobalExceptionLog> GlobalExceptionLogs{get;set;}
        
        //car object
        
        public DbSet<CarBrand> CarBrands { get; set; }
        public DbSet<CarModel> CarModels { get; set; }
        public DbSet<CarEngine> CarEngines { get; set; }
        public DbSet<CarFuelType> CarFuelTypes { get; set; }
        public DbSet<CarGearbox> CarGearboxes { get; set; }
        public DbSet<CarVehicle> CarVehicles { get; set; }
        public DbSet<CarYear> CarYears { get; set; }
        public DbSet<CarVin> CarVins { get; set; }
        public DbSet<CarOriginalNumber> CarOriginalNumbers { get; set; }
        public DbSet<CarSpec> CarSpecs { get; set; }
        public DbSet<CarSpecOriginalNumber> CarSpecOriginalNumbers { get; set; }
        public DbSet<UserCars> UserCars{get;set;}
        public DbSet<Currency> Currencies{get;set;}
        public DbSet<SearchSynonym> SearchSynonyms { get; set; }

        // Price Lists
        public DbSet<PriceList> PriceLists { get; set; }
        public DbSet<PriceListItem> PriceListItems { get; set; }

        // Invoices
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<InvoiceItem> InvoiceItems { get; set; }

        // Expense Definitions
        public DbSet<ExpenseDefinition> ExpenseDefinitions { get; set; }

        // Units
        public DbSet<Unit> Units { get; set; }
        public DbSet<ProductUnit> ProductUnits { get; set; }

        // DAT Integration Entities
        public DbSet<DotVehicleType> DotVehicleTypes { get; set; }
        public DbSet<DotVehicleData> DotVehicleData { get; set; }
        public DbSet<DotPart> DotParts { get; set; }
        public DbSet<DotTokenCache> DotTokenCaches { get; set; }
        public DbSet<DotManufacturer> DotManufacturers { get; set; }
        public DbSet<DotBaseModel> DotBaseModels { get; set; }
        public DbSet<DotSubModel> DotSubModels { get; set; }
        public DbSet<DotCompiledCode> DotCompiledCodes { get; set; }
        public DbSet<DotConstructionPeriod> DotConstructionPeriods { get; set; }
        public DbSet<DotOption> DotOptions { get; set; }
        public DbSet<DotEngineOption> DotEngineOptions { get; set; }
        public DbSet<DotCarBodyOption> DotCarBodyOptions { get; set; }
        public DbSet<DotVehicleImage> DotVehicleImages { get; set; }
        public DbSet<DatData> DatDatas{get;set;}
        public DbSet<VinExternalScrapedData> VinExternalScrapedData { get; set; }

        public DbSet<ProductOemDetail> ProductOemDetails{get;set;}
        
        // banks
        public DbSet<Bank> Banks { get; set; }
        public DbSet<BankParameter> BankParameters { get; set; }
        public DbSet<BankCard> BankCards { get; set; }
        public DbSet<BankCreditCardPrefix> BankCreditCardPrefixs { get; set; }
        public DbSet<BankCreditCardInstallment> BankCreditCardInstallments { get; set; }
        // bank account definitions (admin module)
        public DbSet<BankAccount> BankAccounts { get; set; }
        public DbSet<BankAccountExpense> BankAccountExpenses { get; set; }
        public DbSet<BankAccountInstallment> BankAccountInstallments { get; set; }
        // payments
        public DbSet<BankPaymentTransaction> BankPaymentTransactions { get; set; }

        // Warehouse Management
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<WarehouseShelf> WarehouseShelves { get; set; }
        public DbSet<ProductStock> ProductStocks { get; set; }
        public DbSet<StockTransferLog> StockTransferLogs { get; set; }

        // Accounting
        public DbSet<CashRegister> CashRegisters { get; set; }
        public DbSet<CashRegisterMovement> CashRegisterMovements { get; set; }
        public DbSet<CustomerAccountTransaction> CustomerAccountTransactions { get; set; }
        public DbSet<CollectionReceipt> CollectionReceipts { get; set; }
        public DbSet<BankBranch> BankBranches { get; set; }
        public DbSet<Check> Checks { get; set; }

        // Hierarchical Multi-Tenancy
        public virtual DbSet<Corporation> Corporations { get; set; } = null!;
        public virtual DbSet<Branch> Branches { get; set; } = null!;
        public virtual DbSet<UserBranch> UserBranches { get; set; } = null!;
        public virtual DbSet<SalesPersonBranch> SalesPersonBranches { get; set; } = null!;

        // Push Bildirim Sistemi
        public virtual DbSet<UserPushToken> UserPushTokens { get; set; } = null!;
        public virtual DbSet<NotificationLog> NotificationLogs { get; set; } = null!;
        public virtual DbSet<UserNotification> UserNotifications { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Global Query Filters for Multi-Branch B2B System
            builder.Entity<CashRegister>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<Invoice>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<Warehouse>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<PcPosDefinition>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<Orders>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !x.BranchId.HasValue || x.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<CustomerAccountTransaction>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !x.BranchId.HasValue || x.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<CashRegisterMovement>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<CollectionReceipt>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !x.BranchId.HasValue || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);
            builder.Entity<Check>().HasQueryFilter(x => _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || x.BranchId == _tenantProvider.GetCurrentBranchId() || x.BranchId == 0);

            // Multi-Tenant Isolation
            builder.Entity<Product>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<Discount>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<Seller>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<Banner>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<SalesPerson>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<SalesPersonBranch>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            
            // Additional Multi-Tenant Isolation
            builder.Entity<Brand>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<Customer>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<PriceList>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<Tier>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<ProductType>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<Survey>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == (int?)_tenantProvider.GetCurrentBranchId() || e.BranchId == 0);

            builder.Entity<Category>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            // Banka Yönetimi: Bank ortak (HasQueryFilter yok). BankParameter, BankCard, BankCreditCardInstallment, BankCreditCardPrefix şube bazlı — listeleme/güncelleme/ekleme/silme BranchId'ye göre.
            builder.Entity<BankParameter>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<BankCard>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<BankCreditCardInstallment>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<BankCreditCardPrefix>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || !e.BranchId.HasValue || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<BankAccount>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<ExpenseDefinition>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<PaymentType>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<InvoiceTypeDefinition>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);
            builder.Entity<CustomerBranch>().HasQueryFilter(e => !_tenantProvider.IsMultiTenantEnabled || _tenantProvider.IsGlobalAdmin || _tenantProvider.IsB2BAdmin || _tenantProvider.GetCurrentBranchId() == 0 || e.BranchId == _tenantProvider.GetCurrentBranchId() || e.BranchId == 0);

            // DUAL NAVIGATION SUPPORT: Both User and ApplicationUser can be used
            // Strategy: Configure ApplicationUser FK explicitly, User navigation works via same FK
            // Web context: User navigation works (User table exists, FK matches)
            // Admin context: ApplicationUser navigation works (ApplicationUser table exists, FK matches)
            // EF Core will automatically resolve based on which entity exists in the DbSet
            
            // MyFavorites and UserCars: Only User navigation (Web only entities)
            builder.Entity<MyFavorites>().Ignore(f => f.User);
            builder.Entity<UserCars>().Ignore(u => u.User);
            
            // CartItem: Configure ApplicationUser FK explicitly (Admin context)
            // User navigation will work in Web context via same FK (UserId)
            // EF Core handles this: If User exists in DbSet, it uses User; if ApplicationUser exists, it uses ApplicationUser
            builder.Entity<CartItem>()
                .HasOne(c => c.ApplicationUser)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Note: User navigation is NOT ignored - it works in Web context via same FK
            
            // UserAddress: Configure ApplicationUser FK explicitly (Admin context)
            builder.Entity<UserAddress>()
                .HasOne(a => a.ApplicationUser)
                .WithMany(u => u.UserAddresses)
                .HasForeignKey(a => a.ApplicationUserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Note: User navigation is NOT ignored - it works in Web context via UserId FK
            
            // Orders: Configure both ApplicationUser and User navigation properties
            // CompanyId can reference either AspNetUsers (Admin/B2B) or Users (Web/Marketplace)
            // No FK constraint in database - removed via migration to allow flexibility
            // Data integrity checked at application level
            // Both navigations use the same CompanyId column but point to different tables
            builder.Entity<Orders>()
                .HasOne(o => o.ApplicationUser)
                .WithMany()
                .HasForeignKey(o => o.CompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);
            
            // User navigation also configured - EF Core will use the same CompanyId
            // No FK constraint enforced - allows CompanyId to reference either Users or AspNetUsers
            builder.Entity<Orders>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.CompanyId)
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired(false);



            builder.Entity<ApplicationUser>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<IdentityUserRole<int>>();
            builder.Entity<Category>().HasMany<ProductCategories>(s => s.Products);
            //.WithOne(g => g.Category)
            //.HasForeignKey(s => s.CategoryId);
            builder.Entity<Product>().HasMany<ProductCategories>(s => s.Categories);
            //.WithOne(g => g.Product)
            //  .HasForeignKey(s => s.ProductId);
            
            // CarModels: (Brand + Name) benzersiz
            builder.Entity<CarModel>()
                .HasIndex(x => new { x.CarBrandId, x.Name })
                .IsUnique();

            // Kuryem Olur musun: CourierServiceArea aynı (kurye + araç + bölge) tekrar eklenemesin. CourierVehicleId null eski kayıtlar için.
            builder.Entity<CourierServiceArea>()
                .HasIndex(x => new { x.CourierId, x.CourierVehicleId, x.CityId, x.TownId, x.NeighboorId })
                .IsUnique()
                .HasFilter("[NeighboorId] IS NOT NULL");
            builder.Entity<CourierServiceArea>()
                .HasIndex(x => new { x.CourierId, x.CourierVehicleId, x.CityId, x.TownId })
                .IsUnique()
                .HasFilter("[NeighboorId] IS NULL");

            // CarEngines: (Model + Name) benzersiz
            builder.Entity<CarEngine>()
                .HasIndex(x => new { x.CarModelId, x.Name })
                .IsUnique();

            // CarFuelTypes: Name benzersiz
            builder.Entity<CarFuelType>()
                .HasIndex(x => x.Name)
                .IsUnique();

            // CarGearboxes: Name benzersiz
            builder.Entity<CarGearbox>()
                .HasIndex(x => x.Name)
                .IsUnique();

            // CarVins: Code benzersiz
            builder.Entity<CarVin>()
                .HasIndex(x => x.Code)
                .IsUnique();

            // CarOriginalNumbers: Number benzersiz
            builder.Entity<CarOriginalNumber>()
                .HasIndex(x => x.Number)
                .IsUnique();

            // CarSpecs: OEM + Brand + Model + (Engine,Fuel,Gearbox,Year) unique
            builder.Entity<CarSpec>()
                .HasIndex(x => new
                {
                    x.OEM,
                    x.CarBrandId,
                    x.ModelId,
                    x.CarEngineId,
                    x.CarFuelId,
                    x.CarGearboxId,
                    x.CarYearId
                })
                .IsUnique();

            // Köprü: Composite Key
            builder.Entity<CarSpecOriginalNumber>()
                .HasKey(x => new { x.CarSpecId, x.OriginalNumberId });

            builder.Entity<CarSpecOriginalNumber>()
                .HasOne(x => x.CarSpec)
                .WithMany(s => s.OriginalNumbers)
                .HasForeignKey(x => x.CarSpecId);

            builder.Entity<CarSpecOriginalNumber>()
                .HasOne(x => x.OriginalNumber)
                .WithMany(o => o.CarSpecs)
                .HasForeignKey(x => x.OriginalNumberId);

            builder.Entity<Popup>(
                b => { b.Property(p => p.Rule).HasJsonConversion(); }
            );

            // Address Hierarchy
            builder.Entity<City>().ToTable("City");

            builder.Entity<Town>()
                .ToTable("Town");

            builder.Entity<Neighboor>()
                .ToTable("Neighboors")
                .HasOne(n => n.Town)
                .WithMany(t => t.Neighboors)
                .HasForeignKey(n => n.TownId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Street>()
                .ToTable("Streets")
                .HasOne(s => s.Neighboor)
                .WithMany(n => n.Streets)
                .HasForeignKey(s => s.NeighboorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Building>()
                .ToTable("Buildings")
                .HasOne(b => b.Street)
                .WithMany(s => s.Buildings)
                .HasForeignKey(b => b.StreetId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Home>()
                .ToTable("Homes")
                .HasOne(h => h.Building)
                .WithMany(b => b.Homes)
                .HasForeignKey(h => h.BuildingId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<AddressInf>()
                .ToTable("Addressinfos")
                .HasOne(a => a.Home)
                .WithMany(h => h.AddressInfos)
                .HasForeignKey(a => a.HomeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Discount>(
                b =>
                {
                    b.Property(c => c.AssignedEntityIds).HasJsonConversion();
                    b.Property(c => c.Rule).HasJsonConversion();
                }
            );

            builder.Entity<DiscountCompanyCoupon>(
                b =>
                {
                    b.HasOne(o => o.Discount).WithMany(o => o.CompanyCoupons).HasForeignKey(o => o.DiscountId).OnDelete(DeleteBehavior.Cascade);
                    b.HasOne(o => o.Company).WithMany().HasForeignKey(o => o.CompanyId).OnDelete(DeleteBehavior.Cascade);
                }
            );

            builder.Entity<OrderAppliedDiscount>(
                b =>
                {
                    b.HasKey(o => new { o.OrderId, o.DiscountId });

                    b.HasOne(o => o.Order).WithMany(o => o.AppliedDiscounts).HasForeignKey(o => o.OrderId).OnDelete(DeleteBehavior.Cascade);
                    b.HasOne(o => o.OrderItem).WithMany(o => o.AppliedDiscounts).HasForeignKey(o => o.OrderItemId).OnDelete(DeleteBehavior.Cascade);
                    b.HasOne(o => o.Discount).WithMany(c => c.UsedOrders).HasForeignKey(o => o.DiscountId).OnDelete(DeleteBehavior.Restrict);
                    b.HasOne(o => o.CompanyCoupon).WithMany(c => c.AppliedOrders).HasForeignKey(o => o.CompanyCouponId).OnDelete(DeleteBehavior.SetNull);
                }
            );

            // DAT Integration Entity Configurations
            builder.Entity<DotVehicleType>()
                .HasIndex(x => x.DatId)
                .IsUnique();

            builder.Entity<DotVehicle>()
                .HasIndex(x => x.DatId)
                .IsUnique();

            builder.Entity<DotPart>()
                .HasIndex(x => x.PartNumber);

            builder.Entity<DotTokenCache>()
                .HasIndex(x => x.Token);

            builder.Entity<DotManufacturer>()
                .HasIndex(x => new { x.DatKey, x.VehicleType })
                .IsUnique();

            builder.Entity<DotBaseModel>()
                .HasIndex(x => new { x.DatKey, x.VehicleType, x.ManufacturerKey })
                .IsUnique();

            builder.Entity<DotSubModel>()
                .HasIndex(x => new { x.DatKey, x.VehicleType, x.ManufacturerKey, x.BaseModelKey })
                .IsUnique();

            builder.Entity<DotCompiledCode>()
                .HasIndex(x => x.DatECode)
                .IsUnique();

            builder.Entity<DotVehicleData>()
                .HasIndex(x => x.DatECode)
                .IsUnique();

            builder.Entity<DotOption>()
                .HasIndex(x => new { x.DatKey, x.VehicleType, x.ManufacturerKey, x.BaseModelKey, x.SubModelKey, x.Classification })
                .IsUnique();

            builder.Entity<DotEngineOption>()
                .HasIndex(x => new { x.DatKey, x.VehicleType, x.ManufacturerKey, x.BaseModelKey, x.SubModelKey })
                .IsUnique();

            builder.Entity<DotCarBodyOption>()
                .HasIndex(x => new { x.DatKey, x.VehicleType, x.ManufacturerKey, x.BaseModelKey, x.SubModelKey })
                .IsUnique();

            // Warehouse Configurations
            builder.Entity<WarehouseShelf>()
                .HasIndex(x => new { x.WarehouseId, x.Code })
                .IsUnique();
            
            builder.Entity<ProductStock>()
                .HasIndex(x => new { x.ProductId, x.WarehouseShelfId })
                .IsUnique();

            // Performance Indexes
            builder.Entity<CartItem>()
                .HasIndex(x => x.UserId);

            builder.Entity<MyFavorites>()
                .HasIndex(x => new { x.UserId, x.Status });

            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Customer)
                .WithMany()
                .HasForeignKey(u => u.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<OrderItems>()
                .HasIndex(x => x.OrderId);
            
            // Brand FK constraint removed - BrandId is now a simple column without FK relationship

            builder.Entity<ProductCategories>()
                .HasIndex(x => x.ProductId);

            builder.Entity<ProductImage>()
                .HasIndex(x => x.ProductId);

            builder.Entity<ProductSellerItem>()
                .HasIndex(x => x.ProductId);

            // Unique indexes for bulk operations (prevents concurrent index creation errors)
            builder.Entity<ProductDega>()
                .HasIndex(x => x.Code)
                .IsUnique();

            builder.Entity<ProductRemar>()
                .HasIndex(x => x.Code)
                .IsUnique();

            builder.Entity<ProductBasbug>()
                .HasIndex(x => x.No)
                .IsUnique();

            builder.Entity<ProductOtoIsmail>()
                .HasIndex(x => x.NetsisStokId)
                .IsUnique();

            builder.Entity<CollectionReceipt>()
                .HasIndex(x => x.MakbuzNo)
                .IsUnique();

            // CustomerAccountTransaction: PaymentTypeId is an enum, not a foreign key to PaymentType entity
            // Explicitly configure it as a value property, not a navigation
            builder.Entity<CustomerAccountTransaction>()
                .Property(t => t.PaymentTypeId)
                .HasConversion(
                    v => v.HasValue ? (int?)v.Value : null,
                    v => v.HasValue ? (ecommerce.Core.Utils.PaymentType)v.Value : null)
                .HasColumnName("PaymentTypeId");

            // Unique indexes scoped by BranchId for multi-tenant data integrity (excluding deleted records)
            builder.Entity<Brand>()
                .HasIndex(x => new { x.BranchId, x.Name })
                .IsUnique()
                .HasFilter("\"Status\" <> 99");

            builder.Entity<Category>()
                .HasIndex(x => new { x.BranchId, x.Name })
                .IsUnique()
                .HasFilter("\"Status\" <> 99");

            builder.Entity<ProductType>()
                .HasIndex(x => new { x.BranchId, x.Name })
                .IsUnique()
                .HasFilter("\"Status\" <> 99");

            builder.Entity<Tier>()
                .HasIndex(x => new { x.BranchId, x.Name })
                .IsUnique()
                .HasFilter("\"Status\" <> 99");

            builder.Entity<Survey>()
                .HasIndex(x => new { x.BranchId, Title = x.Title })
                .IsUnique()
                .HasFilter("\"Status\" <> 99");

            // Push Bildirim Sistemi — UserPushToken konfigürasyonu
            builder.Entity<UserPushToken>(entity =>
            {
                // UserId + DeviceId benzersiz composite index (upsert davranışı için)
                entity.HasIndex(x => new { x.UserId, x.DeviceId })
                    .IsUnique();

                // Platform alanı max 10 karakter (ios, android, web)
                entity.Property(x => x.Platform)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(x => x.Token).IsRequired();
                entity.Property(x => x.DeviceId).IsRequired();

                // ApplicationUser ile ilişki
                entity.HasOne(x => x.User)
                    .WithMany(u => u.PushTokens)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Push Bildirim Sistemi — NotificationLog konfigürasyonu
            builder.Entity<NotificationLog>(entity =>
            {
                entity.Property(x => x.Title).IsRequired();
                entity.Property(x => x.Body).IsRequired();
                entity.Property(x => x.TargetAudience).IsRequired();

                // Gönderen admin ile ilişki (opsiyonel)
                entity.HasOne(x => x.SentByUser)
                    .WithMany()
                    .HasForeignKey(x => x.SentByUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                // Gönderim tarihine göre sıralama için index
                entity.HasIndex(x => x.SentAt);
            });
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        }
    }
}
