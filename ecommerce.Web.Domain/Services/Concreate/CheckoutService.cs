using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Services;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Web.Domain.Dtos.Order;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Domain.Shared.Emailing;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Identity;
using ecommerce.Core.Interfaces;

namespace ecommerce.Web.Domain.Services.Concreate;

public class CheckoutService:ICheckoutService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IOrderManager _orderManager;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRepository<Orders> _repository;
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;
    private readonly IEmailService _emailService;
    private readonly ILogger<CheckoutService> _logger;
    private readonly CurrentUser _currentUser;
    private readonly ITenantProvider _tenantProvider;
    
    public CheckoutService(IServiceProvider serviceProvider, IUnitOfWork<ApplicationDbContext> context, IOrderManager orderManager, IHttpContextAccessor httpContextAccessor, IPaymentProviderFactory paymentProviderFactory, Microsoft.Extensions.Configuration.IConfiguration configuration, IEmailService emailService, ILogger<CheckoutService> logger, CurrentUser currentUser, ITenantProvider tenantProvider)
    {
        _serviceProvider = serviceProvider;
        _context = context;
        _orderManager = orderManager;
        _httpContextAccessor = httpContextAccessor;
        _repository = context.GetRepository<Orders>();
        _paymentProviderFactory = paymentProviderFactory;
        _configuration = configuration;
        _emailService = emailService;
        _logger = logger;
        _currentUser = currentUser;
        _tenantProvider = tenantProvider;
    }
    
    public async Task<IActionResult<CheckoutResultDto>> Checkout(CheckoutRequestDto request)
    {
        _logger.LogInformation("=== CheckoutService.Checkout STARTED === PlatformType: {PlatformType}, UserAddressId: {UserAddressId}, HasCardPayment: {HasCardPayment}",
            request.PlatformType, request.UserAddressId, request.CardPayment != null);
        
        var userAddressId = request.UserAddressId;
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            var fail = OperationResult.CreateResult<CheckoutResultDto>();
            fail.AddError("Unauthorized");
            return fail;
        }

        // Context check: Initial setup
        var effectiveUserId = userId; // Default to current logged in owner
        var customerUserIds = new List<int>();
        var result = OperationResult.CreateResult<CheckoutResultDto>();
            try{
                // Try to find UserAddress (Web context uses UserAddress, Admin/B2B context uses ApplicationUser -> Customer -> UserAddress)
                UserAddress? currentCompany = null;
                
                // First, check if this is an ApplicationUser (Admin/B2B context)
                var applicationUser = _context.GetRepository<ApplicationUser>()
                    .GetAll(predicate: u => u.Id == userId, disableTracking: false)
                    .Include(u => u.Customer)
                    .FirstOrDefault();

                int? targetCustomerId = applicationUser?.CustomerId;
                
                // Impersonation Logic
                if (request.OnBehalfOfCustomerId.HasValue)
                {
                     // Verify if user is Plasiyer and linked to this customer
                     if (applicationUser?.SalesPersonId.HasValue == true)
                     {
                         var isLinked = await _context.DbContext.CustomerPlasiyers
                             .AsNoTracking()
                             .AnyAsync(cp => cp.SalesPersonId == applicationUser.SalesPersonId.Value && cp.CustomerId == request.OnBehalfOfCustomerId.Value);

                         if (isLinked)
                         {
                             targetCustomerId = request.OnBehalfOfCustomerId;
                         }
                         else
                         {
                             result.AddError("Bu işlem için yetkiniz bulunmamaktadır.");
                             return result;
                         }
                     }
                     else
                     {
                         // Admin or SuperUser (no SalesPersonId) - allow impersonation
                         targetCustomerId = request.OnBehalfOfCustomerId;
                     }
                }
                
                if (targetCustomerId.HasValue)
                {
                    // ApplicationUser with Customer (or Impersonating) - use Customer's UserAddresses
                    var customerId = targetCustomerId.Value;
                    
                    // Get Customer's ApplicationUser IDs (all users linked to this customer)
                    customerUserIds = _context.GetRepository<ApplicationUser>()
                        .GetAll(predicate: u => u.CustomerId == customerId, disableTracking: true)
                        .Select(u => u.Id)
                        .ToList();

                    // If impersonating or B2B, set owner to customer's first user to ensure order visibility
                    var firstCustomerUserId = customerUserIds.FirstOrDefault();
                    if (firstCustomerUserId != 0) effectiveUserId = firstCustomerUserId;
                    
                    // B2B: Adres zorunlu değil - UserAddressId null ise adres aramıyoruz
                    if (request.PlatformType == OrderPlatformType.B2B && !userAddressId.HasValue)
                    {
                        // B2B siparişlerde adres olmadan devam et
                        currentCompany = null;
                        userAddressId = null;
                    }
                    else if (userAddressId.HasValue)
                    {
                        // Validate that the selected address belongs to Customer's ApplicationUsers
                        currentCompany = _context.GetRepository<UserAddress>()
                            .GetAll(predicate: c => c.Id == userAddressId.Value && 
                                                   c.ApplicationUserId.HasValue && 
                                                   customerUserIds.Contains(c.ApplicationUserId.Value), 
                                   disableTracking: false)
                            .Include(c => c.City)
                            .Include(c => c.Town)
                            .FirstOrDefault();
                        
                        if (currentCompany == null)
                        {
                            result.AddError("Seçilen adres bu cariye ait değil.");
                            return result;
                        }
                    }
                    else
                    {
                        // No address specified - use default address or first available
                        currentCompany = _context.GetRepository<UserAddress>()
                            .GetAll(predicate: c => c.ApplicationUserId.HasValue && 
                                                   customerUserIds.Contains(c.ApplicationUserId.Value) &&
                                                   c.Status == (int)EntityStatus.Active, 
                                   disableTracking: false)
                            .Include(c => c.City)
                            .Include(c => c.Town)
                            .OrderByDescending(c => c.IsDefault)
                            .ThenBy(c => c.CreatedDate)
                            .FirstOrDefault();
                    }
                }
                else
                {
                    // EP/Mobile context — ApplicationUser without Customer (B2C gibi)
                    if (userAddressId.HasValue)
                    {
                        currentCompany = _context.GetRepository<UserAddress>()
                            .GetAll(predicate: c => c.Id == userAddressId.Value && c.ApplicationUserId == userId, 
                                   disableTracking: false)
                            .Include(c => c.City)
                            .Include(c => c.Town)
                            .FirstOrDefault();
                        
                        // Fallback: Web context (UserId) ile dene
                        if (currentCompany == null)
                        {
                            currentCompany = _context.GetRepository<UserAddress>()
                                .GetAll(predicate: c => c.Id == userAddressId.Value && c.UserId == userId, 
                                       disableTracking: false)
                                .Include(c => c.City)
                                .Include(c => c.Town)
                                .FirstOrDefault();
                        }
                    }
                    else
                    {
                        // No address specified - use default or first available
                        currentCompany = _context.GetRepository<UserAddress>()
                            .GetAll(predicate: c => c.ApplicationUserId == userId && c.Status == (int)EntityStatus.Active, 
                                   disableTracking: false)
                            .Include(c => c.City)
                            .Include(c => c.Town)
                            .OrderByDescending(c => c.IsDefault)
                            .ThenBy(c => c.CreatedDate)
                            .FirstOrDefault();
                        
                        // Fallback: Web context (UserId) ile dene
                        if (currentCompany == null)
                        {
                            currentCompany = _context.GetRepository<UserAddress>()
                                .GetAll(predicate: c => c.UserId == userId && c.Status == (int)EntityStatus.Active, 
                                       disableTracking: false)
                                .Include(c => c.City)
                                .Include(c => c.Town)
                                .OrderByDescending(c => c.IsDefault)
                                .ThenBy(c => c.CreatedDate)
                                .FirstOrDefault();
                        }
                    }
                }

                _logger.LogInformation("Checkout Context Finalized - EffectiveUserId (Owner): {EffectiveUserId}, OriginalUserId (Creator): {OriginalUserId}, TargetCustomerId: {TargetCustomerId}", 
                    effectiveUserId, userId, targetCustomerId);
                
                // B2B dışında: Adres yoksa Customer'dan oluştur (Web/EP için)
                if(currentCompany == null && targetCustomerId.HasValue && request.PlatformType != OrderPlatformType.B2B){
                    var customerRepo = _context.GetRepository<ecommerce.Core.Entities.Accounting.Customer>();
                    var customerForAddress = customerRepo.GetAll(predicate: c => c.Id == targetCustomerId.Value, disableTracking: false)
                        .Include(c => c.City)
                        .Include(c => c.Town)
                        .FirstOrDefault();
                    
                    if(customerForAddress != null){
                        // Create and persist a permanent UserAddress from Customer data
                        currentCompany = new UserAddress
                        {
                            AddressName = "Sistem Kayıtlı Adres",
                            FullName = customerForAddress.Name,
                            Email = customerForAddress.Email ?? applicationUser?.Email ?? "",
                            PhoneNumber = customerForAddress.Mobile ?? customerForAddress.Phone ?? "",
                            Address = customerForAddress.Address ?? "",
                            CityId = customerForAddress.CityId,
                            TownId = customerForAddress.TownId,
                            City = customerForAddress.City,
                            Town = customerForAddress.Town,
                            ApplicationUserId = effectiveUserId,
                            IsDefault = true,
                            Status = (int)EntityStatus.Active,
                            CreatedDate = DateTime.Now,
                            CreatedId = userId
                        };

                        _context.GetRepository<UserAddress>().Insert(currentCompany);
                        await _context.SaveChangesAsync();
                        
                        // Update the request with the new address ID
                        userAddressId = currentCompany.Id;
                    }
                }
                
                // B2B dışında adres zorunlu
                if(currentCompany == null && request.PlatformType != OrderPlatformType.B2B){
                    result.AddError("Kullanici bilgileri bulunamadı.");
                    return result;
                }
                
                // CRITICAL FIX: Always ensure userAddressId is set to currentCompany.Id (B2B'de currentCompany null olabilir)
                if (currentCompany != null && currentCompany.Id > 0)
                {
                    userAddressId = currentCompany.Id;
                }
                
                // Optimize: Load cart and preferences in parallel
                CartCustomerSavedPreferences? cartPreferences = request.CartPreferences;
                if (cartPreferences == null)
                {
                    try
                    {
                        cartPreferences = JsonConvert.DeserializeObject<CartCustomerSavedPreferences>(_httpContextAccessor.HttpContext.Request.Cookies[CartConsts.CartPreferencesStorageKey] ?? string.Empty);
                    }
                    catch { }
                }
                // CLEANUP: Önce ödenmemiş siparişleri temizle (aynı connection üzerinde paralel sorgu NpgsqlOperationInProgressException'a yol açar)
                try {
                    await OrderDelete(effectiveUserId); 
                    _context.DbContext.ChangeTracker.Clear();
                } catch(Exception ex) {
                    _logger.LogError(ex, "OrderDelete cleanup error");
                }
                
                var cartItems = await _orderManager.GetShoppingCart();
                var cartResult = await _orderManager.CalculateShoppingCart(cartItems, cartPreferences, true);
              
                if(cartResult.Sellers.All(s => s.IsAllItemsPassive)){
                    result.AddError("Sepetinizde seçili ürün bulunmamaktadır.");
                    return result;
                }
                if(cartResult.Sellers.Any(s => !s.IsAllItemsPassive && s.OrderTotal < s.MinCartTotal)){
                    result.AddError("Sepetinizdeki bazı satıcıların minimum sipariş tutarı karşılanmamaktadır.");
                    return result;
                }

                // Voucher/Rehber zorunluluğu: Sadece paket ürünler için (ParkFuntastic)
                var hasPackageProducts = cartResult.Sellers?
                    .Where(s => !s.IsAllItemsPassive)
                    .SelectMany(s => s.Items ?? new List<ecommerce.Core.Entities.CartItem>())
                    .Any(i => i.Status == (int)EntityStatus.Active && (i.Product?.IsPackageProduct ?? false)) ?? false;
                if (hasPackageProducts)
                {
                    var packageItemsWithoutDate = cartResult.Sellers?
                        .Where(s => !s.IsAllItemsPassive)
                        .SelectMany(s => s.Items ?? new List<ecommerce.Core.Entities.CartItem>())
                        .Where(i => i.Status == (int)EntityStatus.Active && (i.Product?.IsPackageProduct ?? false) && !i.VisitDate.HasValue)
                        .ToList() ?? new List<ecommerce.Core.Entities.CartItem>();
                    if (packageItemsWithoutDate.Any())
                    {
                        result.AddError("Sepetinizde ziyaret tarihi girilmemiş paket ürün bulunmaktadır. Lütfen paket ürünleri sepetten çıkarıp tekrar ekleyerek tarih seçiniz.");
                        return result;
                    }
                    if (string.IsNullOrWhiteSpace(request.Voucher))
                    {
                        result.AddError("Sepetinizde paket ürün bulunmaktadır. Siparişi tamamlamak için Voucher kodu girmeniz gerekmektedir.");
                        return result;
                    }
                    if (string.IsNullOrWhiteSpace(request.GuideName))
                    {
                        result.AddError("Sepetinizde paket ürün bulunmaktadır. Siparişi tamamlamak için Rehber veya Acenta ismi girmeniz gerekmektedir.");
                        return result;
                    }
                }

                // Uyarı kontrolü — tam liste oluşturmadan Any() ile short-circuit (büyük sepet performansı)
                var hasWarnings = cartResult.Warnings.Any()
                    || cartResult.Sellers.Any(s => !s.IsAllItemsPassive
                        && (s.Warnings.Any() || s.Items.Any(i => i.Status == (int)EntityStatus.Active && i.Warnings != null && i.Warnings.Any())));
                if (hasWarnings)
                {
                    result.AddError("Lütfen sepetinizdeki uyarıları düzelttikten sonra tekrar deneyiniz!");
                    return result;
                }
              
           
                if(cartResult.AppliedCompanyCouponCodeId.HasValue){
                    var companyCouponRepository = _context.GetRepository<DiscountCompanyCoupon>();
                    var companyCoupon = await companyCouponRepository.GetAll(
                        predicate: c => c.Id == cartResult.AppliedCompanyCouponCodeId.Value, 
                        disableTracking: false)
                        .FirstOrDefaultAsync();
                    if(companyCoupon == null){
                        result.AddError("Şirket kuponu bulunamadı.");
                        return result;
                    }
                    companyCoupon.IsUsed = true;
                    companyCouponRepository.Update(companyCoupon);
                    // Update will be saved with orders in the same transaction
                }

                // Get Customer info early to use for both requirePaymentPage and PaymentTypeId
                ecommerce.Core.Entities.Accounting.Customer? customer = null;
                if (targetCustomerId.HasValue)
                {
                    var customerRepo = _context.GetRepository<ecommerce.Core.Entities.Accounting.Customer>();
                    customer = await customerRepo.GetAll(
                        predicate: c => c.Id == targetCustomerId.Value,
                        disableTracking: true
                    ).FirstOrDefaultAsync();
                }
                
                // Validation - Check CustomerWorkingType to determine if payment is required
                bool requirePaymentPage = true; // Default: Require payment
                
                // B2B platform: Tüm siparişler cari hesap (veresiye) — banka/kart seçimi kaldırıldı
                if (request.PlatformType == OrderPlatformType.B2B && targetCustomerId.HasValue)
                {
                    requirePaymentPage = false;
                    _logger.LogInformation("B2B platform - All orders as Cari Hesap (Veresiye), RequirePayment: false");
                }
                // If ApplicationUser has Customer, check CustomerWorkingType (Marketplace/Web için)
                else if (customer != null)
                {
                    // Vadeli (2) müşteriler ödeme gerektirmez
                    // Pesin (1) müşteriler her zaman ödeme gerektirir
                    // PesinAndVadeli (3) müşteriler: cardPayment gönderilmişse ödeme gerektirir
                    if (request.CardPayment != null && request.CardPayment.BankId.HasValue)
                    {
                        requirePaymentPage = true;
                    }
                    else if (customer.CustomerWorkingType == CustomerWorkingTypeEnum.Pesin)
                    {
                        requirePaymentPage = true;
                    }
                    else
                    {
                        requirePaymentPage = false;
                    }
                    
                    _logger.LogInformation("Customer {CustomerId} - WorkingType: {WorkingType}, HasCardPayment: {HasCardPayment}, BankId: {BankId}, RequirePayment: {RequirePayment}", 
                        customer.Id, customer.CustomerWorkingType, request.CardPayment != null, request.CardPayment?.BankId, requirePaymentPage);
                }
                else
                {
                    // Web context veya B2C (Customer'ı olmayan ApplicationUser)
                    if (request.CardPayment != null && request.CardPayment.BankId.HasValue)
                    {
                        // Açıkça ödeme bilgisi gönderilmiş — ödeme sayfası göster
                        requirePaymentPage = true;
                    }
                    else if (applicationUser != null && request.CardPayment == null)
                    {
                        // B2C/Mobil kullanıcı — cardPayment göndermemiş, cari sipariş olarak kabul et
                        requirePaymentPage = false;
                        _logger.LogInformation("B2C/Mobile context - No CardPayment, treating as account order. UserId: {UserId}", userId);
                    }
                    else
                    {
                        // Web context (Marketplace) - config'e göre
                        var configValue = _configuration.GetValue<bool>("Payment:RequirePaymentPage", true);
                        requirePaymentPage = configValue;
                    }
                    _logger.LogInformation("Non-customer context - RequirePaymentPage: {RequirePayment}, HasApplicationUser: {HasAppUser}, HasCardPayment: {HasCard}", 
                        requirePaymentPage, applicationUser != null, request.CardPayment != null);
                }
                
                if (requirePaymentPage)
                {
                    if(request.CardPayment == null || !request.CardPayment.BankId.HasValue){
                         result.AddError("Lütfen banka ve kart bilgilerini eksiksiz giriniz.");
                         return result;
                    }
                    
                    // Mobil platform (3) için kart bilgisi validasyonu yapılmaz
                    // Kart bilgileri bankanın 3D Secure formunda girilecek
                    if (request.PlatformType != OrderPlatformType.Mobile)
                    {
                        if (string.IsNullOrWhiteSpace(request.CardPayment.CardHolderName) ||
                            string.IsNullOrWhiteSpace(request.CardPayment.CardNumber) ||
                            string.IsNullOrWhiteSpace(request.CardPayment.ExpMonth) ||
                            string.IsNullOrWhiteSpace(request.CardPayment.ExpYear) ||
                            string.IsNullOrWhiteSpace(request.CardPayment.Cvv))
                        {
                            result.AddError("Lütfen kart bilgilerini eksiksiz giriniz.");
                            return result;
                        }
                    }
                }

                // Installment Logic
                decimal installmentRate = 0;
                int installmentCount = 1;

                if (request.CardPayment != null && request.CardPayment.InstallmentId.HasValue)
                {
                    var installmentRepo = _context.GetRepository<BankCreditCardInstallment>();
                    var installment = await installmentRepo.GetAll(predicate: x => x.Id == request.CardPayment.InstallmentId.Value).FirstOrDefaultAsync();
                    if (installment != null)
                    {
                        installmentRate = installment.InstallmentRate;
                        installmentCount = installment.Installment;
                    }
                }
                // Determine platform type: B2B if ApplicationUser with Customer, otherwise Marketplace
                var platformType = OrderPlatformType.Marketplace; // Default: Pazaryeri
                if (targetCustomerId.HasValue)
                {
                    platformType = OrderPlatformType.B2B;
                }
                
                // Override with request parameter if provided
                if (request.PlatformType.HasValue)
                {
                    platformType = request.PlatformType.Value;
                }
                
                // Determine PaymentTypeId based on CustomerWorkingType and payment method
                ecommerce.Core.Utils.PaymentType paymentTypeId = ecommerce.Core.Utils.PaymentType.CreditCart; // Default
                if (request.PlatformType == OrderPlatformType.B2B && targetCustomerId.HasValue)
                {
                    // B2B: Tüm siparişler cari hesap (veresiye)
                    paymentTypeId = ecommerce.Core.Utils.PaymentType.CustomerBalance;
                    _logger.LogInformation("PaymentTypeId set to CustomerBalance for B2B platform (CustomerId: {CustomerId})", targetCustomerId.Value);
                }
                else if (customer != null)
                {
                    if (customer.CustomerWorkingType == CustomerWorkingTypeEnum.Pesin)
                    {
                        paymentTypeId = ecommerce.Core.Utils.PaymentType.CreditCart;
                        _logger.LogInformation("PaymentTypeId set to CreditCart for Pesin customer (CustomerId: {CustomerId})", customer.Id);
                    }
                    else if (customer.CustomerWorkingType == CustomerWorkingTypeEnum.Vadeli)
                    {
                        paymentTypeId = ecommerce.Core.Utils.PaymentType.CustomerBalance;
                        _logger.LogInformation("PaymentTypeId set to CustomerBalance for Vadeli customer (CustomerId: {CustomerId})", customer.Id);
                    }
                    else if (customer.CustomerWorkingType == CustomerWorkingTypeEnum.PesinAndVadeli)
                    {
                        paymentTypeId = (request.CardPayment != null) ? ecommerce.Core.Utils.PaymentType.CreditCart : ecommerce.Core.Utils.PaymentType.CustomerBalance;
                        _logger.LogInformation("PaymentTypeId set to {PaymentType} for PesinAndVadeli customer (CustomerId: {CustomerId}, HasCardPayment: {HasCardPayment})", 
                            paymentTypeId, customer.Id, request.CardPayment != null);
                    }
                }
                else
                {
                    paymentTypeId = (request.CardPayment != null) ? ecommerce.Core.Utils.PaymentType.CreditCart : ecommerce.Core.Utils.PaymentType.CustomerBalance;
                    _logger.LogInformation("PaymentTypeId set to {PaymentType} for non-B2B customer (HasCardPayment: {HasCardPayment})", 
                        paymentTypeId, request.CardPayment != null);
                }
                
                var createdOrders = new List<Orders>();
                // Kurye teslimat seçildiyse Bicops Express cargo kullan (Hızlı Kargo) — CargoType=1 tek ücret Cargo.Amount
                decimal? bicopsExpressCargoAmount = null;
                int? bicopsExpressCargoId = null;
                if (request.DeliveryOptionType == DeliveryOptionType.Courier)
                {
                    var bicopsCargo = await _context.DbContext.Set<ecommerce.Core.Entities.Cargo>()
                        .AsNoTracking()
                        .Where(c => c.CargoType == ecommerce.Core.Utils.CargoType.BicopsExpress && c.Status == (int)EntityStatus.Active)
                        .Select(c => new { c.Id, c.Amount })
                        .FirstOrDefaultAsync();
                    if (bicopsCargo != null)
                    {
                        bicopsExpressCargoId = bicopsCargo.Id;
                        bicopsExpressCargoAmount = bicopsCargo.Amount;
                    }
                }

                foreach(var seller in cartResult.Sellers){
                    if(seller.IsAllItemsPassive || seller.OrderTotal < seller.MinCartTotal){
                        continue;
                    }
                    // OrderManager kargo seçimini kaldırdığı için SelectedCargo null olabilir; varsayılan/ilk kargoyu kullan
                    var effectiveCargo = seller.SelectedCargo
                        ?? seller.Cargoes.FirstOrDefault(c => c.IsDefault)
                        ?? seller.Cargoes.FirstOrDefault();
                    if (effectiveCargo == null)
                    {
                        _logger.LogWarning("Satıcı {SellerId} için kargo bilgisi bulunamadı, sipariş atlanıyor.", seller.SellerId);
                        result.AddError($"Satıcı {seller.SellerName} için kargo bilgisi bulunmamaktadır.");
                        continue;
                    }
                    var sellerItems = seller.Items
                        .Where(i => i.Status == (int) EntityStatus.Active)
                        .GroupBy(x => x.ProductSellerItemId) // Deduplicate by Stock Item ID
                        .Select(g => g.First())
                        .ToList();
                    // Recalculate totals based on unique items to ensure consistency
                    var uniqueSubTotal = sellerItems.Sum(i => i.Total);
                    var uniqueOrderTotal = uniqueSubTotal + seller.CargoPrice - seller.OrderTotalDiscount;
                    
                    // B2B siparişler plasiyer onayı bekler (parkfuntastic gibi)
                    var initialOrderStatus = (platformType == OrderPlatformType.B2B || targetCustomerId.HasValue)
                        ? OrderStatusType.OrderWaitingApproval
                        : OrderStatusType.OrderNew;
                    var order = new Orders{
                        Status = (int) EntityStatus.Active,
                        CreatedDate = DateTime.Now,
                        OrderStatusType = initialOrderStatus,
                        PlatformType = platformType,
                        OrderNumber = KeyGenerator.GetUniqueKey(7),
                        CompanyId = effectiveUserId, // Order Owner (Customer)
                        CreatedId = userId,         // Order Creator (Plasiyer)
                        CustomerId = targetCustomerId, // B2B: Sipariş listesinde görünmesi için
                        BranchId = _tenantProvider.GetCurrentBranchId() > 0 ? _tenantProvider.GetCurrentBranchId() : 1, // Multi-branch support
                        SellerId = seller.SellerId,
                        UserAddressId = userAddressId,
                        PaymentTypeId = paymentTypeId,
                        DiscountTotal = seller.OrderTotalDiscount,
                        CargoId = effectiveCargo.CargoId,
                        CargoPrice = seller.CargoPrice,
                        ProductTotal = uniqueSubTotal, // Use recalculated subtotal
                        OrderTotal = uniqueOrderTotal, // Use recalculated total
                        GrandTotal = uniqueOrderTotal * (1 + (installmentRate / 100)), // Recalculate grand total
                        BankId = request.CardPayment?.BankId,
                        BankCardId = request.CardPayment?.BankCardId, 
                        Installment = installmentCount,
                        SubmerhantCommissionRate = sellerItems.Min(c => c.SubmerhantCommisionRate) ?? 0,
                        SubmerhantCommisionTotal = (uniqueSubTotal - sellerItems.Sum(c => c.SubmerhantCommision)), // Use unique subtotal
                        MerhanCommission = sellerItems.Sum(c => c.SubmerhantCommision),
                        PaymentToken = null,
                        ShipmentDate = null,
                        PaymentId = requirePaymentPage ? null : $"CustomerBalance-{Guid.NewGuid().ToString("N").Substring(0, 8)}",
                        PaymentStatus = !requirePaymentPage,
                        Voucher = request.Voucher,
                        GuideName = request.GuideName,
                        AppliedDiscounts = new List<OrderAppliedDiscount>(),
                        OrderItems = new List<OrderItems>()
                    };
                    // Kurye teslimat seçildiyse siparişe kurye ata ve CargoId + CargoPrice = BicoJET (Cargo.Amount)
                    if (request.DeliveryOptionType == DeliveryOptionType.Courier && request.CourierId.HasValue && request.CourierId.Value > 0)
                    {
                        order.CourierId = request.CourierId;
                        order.CourierDeliveryStatus = CourierDeliveryStatus.Assigned;
                        order.DeliveryOptionType = DeliveryOptionType.Courier;
                        order.EstimatedCourierDeliveryMinutes = request.EstimatedCourierDeliveryMinutes;
                        if (bicopsExpressCargoId.HasValue && bicopsExpressCargoId.Value > 0 && bicopsExpressCargoAmount.HasValue)
                        {
                            order.CargoId = bicopsExpressCargoId.Value;
                            order.CargoPrice = bicopsExpressCargoAmount.Value; // CargoType=1 tek ucret: Cargo.Amount
                        }
                    }
                    // Track added discounts per order to prevent duplicates
                    var addedDiscountIds = new HashSet<int>();
                    
                    foreach(var discount in seller.AppliedDiscounts){
                        // Prevent duplicate discounts in the same order
                        if (!addedDiscountIds.Contains(discount.Id))
                        {
                            addedDiscountIds.Add(discount.Id);
                            order.AppliedDiscounts.Add(new OrderAppliedDiscount{
                                    OrderId = 0, // Will be set after Order is saved
                                    DiscountId = discount.Id,
                                    CouponCode = cartResult.AppliedCouponCode,
                                    CompanyCouponId = cartResult.AppliedCompanyCouponCodeId,
                                    CreatedDate = order.CreatedDate,
                                    Order = order
                                }
                            );
                        }
                    }
                    
                    foreach(var cartItem in sellerItems){
                        // BrandId is no longer required - set to 0 if not available
                        var brandId = cartItem.Product?.BrandId ?? 0;
                        if(brandId <= 0) brandId = 0; // Ensure 0 if invalid
                        
                        var orderItem = new OrderItems{
                            Status = order.Status,
                            CreatedDate = order.CreatedDate,
                            CreatedId = order.CreatedId,
                            OrderId = 0, // Will be set after Order is saved
                            BrandId = brandId,
                            ProductId = cartItem.ProductId,
                            ProductName = cartItem.Product.Name,
                            Quantity = cartItem.Quantity,
                            Stock = (int)cartItem.ProductSellerItem.Stock,
                            SourceId = cartItem.ProductSellerItem.SourceId,
                            OldPrice = cartItem.UnitPriceWithoutDiscount,
                            Price = cartItem.UnitPrice,
                            TotalPrice = cartItem.Total,
                            CommissionRateId = cartItem.CommissionRateId,
                            CommissionRatePercent = cartItem.CommissionRatePercent,
                            CommissionTotal = cartItem.CommissionTotal,
                            DiscountAmount =(cartItem.DiscountAmount*cartItem.Quantity),
                            ExprationDate = DateTime.Now,
                            ShipmentDate = cartItem.Product?.IsPackageProduct == true && cartItem.VisitDate.HasValue
                                ? DateTime.SpecifyKind(cartItem.VisitDate.Value.Date, DateTimeKind.Utc)
                                : null,
                            PackageItemQuantitiesJson = cartItem.Product?.IsPackageProduct == true && cartItem.PackageItemQuantities != null && cartItem.PackageItemQuantities.Count > 0
                                ? System.Text.Json.JsonSerializer.Serialize(cartItem.PackageItemQuantities) : null,
                            MerhantCommision = cartItem.SubmerhantCommision,
                            SubmerhantCommision = ((cartItem.Quantity * cartItem.UnitPrice) - cartItem.SubmerhantCommision),
                            PaymentTransactionId = string.Empty,
                            Width = cartItem.Product.Width,
                            Length = cartItem.Product.Length,
                            Height = cartItem.Product.Height,
                            CargoDesi = cartItem.ProductDesi,
                            AppliedDiscounts = new List<OrderAppliedDiscount>(),
                            Orders = order
                        };

                        foreach(var discount in cartItem.AppliedDiscounts){
                            // Prevent duplicate discounts in the same order
                            // For item-level discounts, we'll set OrderItemId after OrderItem is saved
                            if (!addedDiscountIds.Contains(discount.Id))
                            {
                                addedDiscountIds.Add(discount.Id);
                                order.AppliedDiscounts.Add(new OrderAppliedDiscount{
                                    OrderId = 0, // Will be set after Order is saved
                                    OrderItemId = null, // Will be set after OrderItem is saved (for item-level discounts)
                                    DiscountId = discount.Id,
                                    CouponCode = cartResult.AppliedCouponCode,
                                    CompanyCouponId = cartResult.AppliedCompanyCouponCodeId,
                                    CreatedDate = order.CreatedDate,
                                    Order = order,
                                    OrderItem = orderItem // Link to OrderItem for item-level discounts
                                });
                            }
                        }
                        order.OrderItems.Add(orderItem);
                    }
                createdOrders.Add(order);
                }
                
                // B2B Sistemi için: Tüm işlemleri tek bir transaction içinde yap
                // Orders -> OrderItems -> CustomerAccountTransaction (hepsi atomic)
                // Execution Strategy ile retry desteği ve transaction uyumluluğu
                var strategy = _context.DbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    using var dbTransaction = await _context.BeginTransactionAsync();
                    try 
                    {
                        _logger.LogInformation("Transaction started for B2B checkout");
                    
                        // Store items and discounts temporarily to prevent EF Core from double-inserting them
                        // (Once via cascade on Order save, and once via manual BulkInsert)
                        var tempOrderItemsMap = new Dictionary<Orders, List<OrderItems>>();
                        var tempOrderDiscountsMap = new Dictionary<Orders, List<OrderAppliedDiscount>>();

                        // Batch insert all orders (WITHOUT children)
                        foreach(var createdOrder in createdOrders){
                            // Backup children
                            tempOrderItemsMap[createdOrder] = createdOrder.OrderItems.ToList();
                            tempOrderDiscountsMap[createdOrder] = createdOrder.AppliedDiscounts.ToList();
                            
                            // Clear navigation properties to stop EF Core Cascade Insert
                            createdOrder.OrderItems.Clear();
                            createdOrder.AppliedDiscounts.Clear();

                            _repository.Insert(createdOrder);
                        }
                        // Save Orders first to get OrderId
                        var saveResult = await _context.SaveChangesAsync();
                    
                        // Verify orders were actually saved (check if OrderIds are assigned)
                        if (createdOrders.Any(o => o.Id <= 0))
                        {
                            var lastEx = _context.LastSaveChangesResult?.Exception;
                            result.AddError($"Siparişler kaydedilemedi. {(lastEx != null ? $"Hata: {lastEx.Message}" : "Lütfen tekrar deneyiniz.")}");
                            _logger.LogError("Orders not saved. Count: {OrderCount}, SaveResult: {SaveResult}, HasException: {HasException}", createdOrders.Count, saveResult, lastEx != null);
                            if(lastEx != null) _logger.LogError(lastEx, "Save exception");
                            await dbTransaction.RollbackAsync();
                            return;
                        }
                    
                        _logger.LogInformation("Orders saved successfully. Count: {OrderCount}, First ID: {FirstOrderId}", createdOrders.Count, createdOrders[0].Id);
                        var orderItemsRepo = _context.GetRepository<OrderItems>();
                        var appliedDiscountsRepo = _context.GetRepository<OrderAppliedDiscount>();
                    
                        // Collect all items and discounts from BACKUP maps
                        var allOrderItems = new List<OrderItems>();
                        var allAppliedDiscounts = new List<OrderAppliedDiscount>();
                        // Track unique (OrderId, DiscountId) combinations to avoid duplicate key violations
                        var appliedDiscountKeys = new HashSet<(int OrderId, int DiscountId)>();
                        
                        foreach(var createdOrder in createdOrders){
                        
                        // Retrieve items from backup
                        if(!tempOrderItemsMap.TryGetValue(createdOrder, out var orderItems)) orderItems = new List<OrderItems>();
                        if(!tempOrderDiscountsMap.TryGetValue(createdOrder, out var orderDiscounts)) orderDiscounts = new List<OrderAppliedDiscount>();

                        // Set OrderId for all OrderItems
                        foreach(var item in orderItems){
                            item.OrderId = createdOrder.Id; // CRITICAL: Set OrderId after Order is saved
                            item.Id = 0; // Ensure Id is 0 for auto-increment
                            allOrderItems.Add(item);
                        }
                        
                        // First, clean up duplicates within each order's AppliedDiscounts list using (OrderId, DiscountId) key
                        // Note: OrderId is still 0 at this point, so we use DiscountId only for per-order deduplication
                        var orderDiscountKeys = new HashSet<int>();
                        var uniqueOrderDiscounts = new List<OrderAppliedDiscount>();
                        foreach(var discount in orderDiscounts){
                            if (!orderDiscountKeys.Contains(discount.DiscountId))
                            {
                                orderDiscountKeys.Add(discount.DiscountId);
                                uniqueOrderDiscounts.Add(discount);
                            }
                            else
                            {
                                _logger.LogDebug("Removed duplicate discount from order - DiscountId: {DiscountId} (before OrderId assignment)", discount.DiscountId);
                            }
                        }
                        // NOTE: Do NOT assign back to createdOrder.AppliedDiscounts to avoid re-tracking issues
                        
                        // Set OrderId for all AppliedDiscounts and remove duplicates across all orders
                        foreach(var discount in uniqueOrderDiscounts){
                            discount.OrderId = createdOrder.Id; // CRITICAL: Set OrderId after Order is saved
                            
                            // Check for duplicate (OrderId, DiscountId) combination
                            var key = (discount.OrderId, discount.DiscountId);
                            if (!appliedDiscountKeys.Contains(key))
                            {
                                appliedDiscountKeys.Add(key);
                                allAppliedDiscounts.Add(discount);
                            }
                            else
                            {
                                _logger.LogWarning("Duplicate discount detected and skipped - OrderId: {OrderId}, DiscountId: {DiscountId}", discount.OrderId, discount.DiscountId);
                            }
                        }
                    }
                    
                        // CRITICAL: Save OrderItems first to get OrderItemId, then set OrderItemId in AppliedDiscounts
                        if (allOrderItems.Any())
                        {
                            await _context.BulkInsertAsync(allOrderItems);
                            
                            // BulkInsertAsync doesn't set Ids in entities, so we need to fetch them from DB
                            var orderIds = allOrderItems.Select(oi => oi.OrderId).Distinct().ToList();
                            var savedOrderItems = await orderItemsRepo.GetAll(
                                predicate: oi => orderIds.Contains(oi.OrderId),
                                disableTracking: true
                            ).ToListAsync();
                            
                            // Create a mapping: (OrderId, ProductId, Price, Quantity) -> Id
                            // This is the best way to match items since BulkInsertAsync doesn't return Ids
                            var orderItemIdMap = new Dictionary<OrderItems, int>();
                            foreach(var item in allOrderItems)
                            {
                                // Match by OrderId, ProductId, Price, and Quantity (most unique combination)
                                var matched = savedOrderItems.FirstOrDefault(soi => 
                                    soi.OrderId == item.OrderId && 
                                    soi.ProductId == item.ProductId && 
                                    soi.Price == item.Price && 
                                    soi.Quantity == item.Quantity &&
                                    soi.TotalPrice == item.TotalPrice);
                                
                                if (matched != null)
                                {
                                    item.Id = matched.Id; // Set Id back to entity
                                    orderItemIdMap[item] = matched.Id;
                                }
                                else
                                {
                                    _logger.LogWarning("Could not match OrderItem after BulkInsert - OrderId: {OrderId}, ProductId: {ProductId}", 
                                        item.OrderId, item.ProductId);
                                }
                            }
                            
                            // Verify all items were matched
                            var unmatchedCount = allOrderItems.Count(item => item.Id <= 0);
                            if (unmatchedCount > 0)
                            {
                                result.AddError($"Sipariş kalemleri kaydedilemedi. {unmatchedCount} kalem eşleştirilemedi.");
                                _logger.LogError("OrderItems not matched after BulkInsert - UnmatchedCount: {UnmatchedCount}", unmatchedCount);
                                await dbTransaction.RollbackAsync();
                                _logger.LogWarning("Transaction rolled back - OrderItems matching failed");
                                return;
                            }
                            
                            // Set OrderItemId for discounts that have OrderItem reference
                            foreach(var discount in allAppliedDiscounts)
                            {
                                if (discount.OrderItem != null && orderItemIdMap.TryGetValue(discount.OrderItem, out var orderItemId))
                                {
                                    discount.OrderItemId = orderItemId;
                                }
                            }
                            
                            _logger.LogInformation("OrderItems saved and matched successfully. Count: {Count}", allOrderItems.Count);
                        }
                    
                    // Final safety check: Remove any remaining duplicates using DistinctBy
                    var finalAppliedDiscounts = allAppliedDiscounts
                        .GroupBy(d => new { d.OrderId, d.DiscountId })
                        .Select(g => g.First())
                        .ToList();
                    
                    // Log if duplicates were found and removed
                    if (finalAppliedDiscounts.Count < allAppliedDiscounts.Count)
                    {
                        var removedCount = allAppliedDiscounts.Count - finalAppliedDiscounts.Count;
                        _logger.LogWarning("Removed {RemovedCount} duplicate discounts from final list", removedCount);
                    }
                    
                        // Check if any of these discounts already exist in database to avoid duplicate key violations
                        if (finalAppliedDiscounts.Any())
                        {
                            var orderIds = finalAppliedDiscounts.Select(d => d.OrderId).Distinct().ToList();
                            var discountIds = finalAppliedDiscounts.Select(d => d.DiscountId).Distinct().ToList();
                            
                            var existingDiscounts = await appliedDiscountsRepo.GetAll(
                                predicate: d => orderIds.Contains(d.OrderId) && discountIds.Contains(d.DiscountId),
                                disableTracking: true
                            ).ToListAsync();
                            
                            var existingKeys = existingDiscounts
                                .Select(d => (d.OrderId, d.DiscountId))
                                .ToHashSet();
                            
                            var discountsToInsert = finalAppliedDiscounts
                                .Where(d => !existingKeys.Contains((d.OrderId, d.DiscountId)))
                                .ToList();
                            
                            if (discountsToInsert.Count < finalAppliedDiscounts.Count)
                            {
                                var skippedCount = finalAppliedDiscounts.Count - discountsToInsert.Count;
                                _logger.LogInformation("Skipped {SkippedCount} discounts that already exist in database", skippedCount);
                            }
                            
                            finalAppliedDiscounts = discountsToInsert;
                        }
                        
                        _logger.LogDebug("Total unique discounts to insert: {DiscountCount}", finalAppliedDiscounts.Count);
                        
                        // Save AppliedDiscounts (CRITICAL: This was missing!)
                        if (finalAppliedDiscounts.Any())
                        {
                            await _context.BulkInsertAsync(finalAppliedDiscounts);
                            _logger.LogInformation("AppliedDiscounts saved successfully. Count: {Count}", finalAppliedDiscounts.Count);
                        }
                        
                        var itemsSaveResult = allOrderItems.Count + finalAppliedDiscounts.Count;
                        _logger.LogInformation("Orders saved successfully. OrderIds: {OrderIds}, Items saved: {ItemsCount}, Discounts saved: {DiscountsCount}", 
                            string.Join(", ", createdOrders.Select(o => o.Id)), allOrderItems.Count, finalAppliedDiscounts.Count);
                        
                        // Sipariş aşamasında cari hesaba transaction ATILMAZ.
                        // Cari hesap borcu sadece FATURA kesildiğinde oluşturulur.
                        _logger.LogInformation("Sipariş oluşturuldu — cari hesap transaction'ı atılmadı (fatura kesildiğinde oluşturulacak). OrderIds: {OrderIds}", 
                            string.Join(", ", createdOrders.Select(o => o.Id)));
                    
                        // Tüm işlemler başarılı - Transaction commit et
                        await dbTransaction.CommitAsync();
                        _logger.LogInformation("Transaction committed successfully - All operations saved (Orders, OrderItems, CustomerAccountTransactions)");
                    }
                    catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
                {
                        // Transaction rollback - Hata durumunda tüm işlemleri geri al
                        try
                        {
                            await dbTransaction.RollbackAsync();
                            _logger.LogWarning("Transaction rolled back due to DbUpdateException");
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Error during rollback");
                        }
                        
                        // Handle FK constraint violations specifically
                        if (dbEx.InnerException is Npgsql.PostgresException pgEx && pgEx.SqlState == "23503")
                        {
                            var constraintName = pgEx.ConstraintName ?? "unknown";
                            var tableName = pgEx.TableName ?? "unknown";
                            result.AddError($"Veritabanı hatası: {tableName} tablosunda {constraintName} constraint ihlali. Lütfen ürün bilgilerini kontrol ediniz.");
                            _logger.LogError(pgEx, "FK Constraint Error - Table: {TableName}, Constraint: {ConstraintName}", tableName, constraintName);
                            return;
                        }
                        // Log and handle other database exceptions
                        result.AddError($"Veritabanı hatası: {dbEx.Message}");
                        _logger.LogError(dbEx, "DbUpdateException occurred");
                        return;
                    }
                    catch (Exception ex)
                    {
                        // Transaction rollback - Beklenmeyen hata durumunda tüm işlemleri geri al
                        try
                        {
                            await dbTransaction.RollbackAsync();
                            _logger.LogWarning("Transaction rolled back due to unexpected exception");
                        }
                        catch (Exception rollbackEx)
                        {
                            _logger.LogError(rollbackEx, "Error during rollback");
                        }
                        
                        result.AddError($"Sipariş kaydedilirken hata oluştu: {ex.Message}");
                        _logger.LogError(ex, "Exception during save");
                        return;
                    }
                    // Using statement otomatik olarak transaction'ı dispose edecek
                });
                
                // NOW make ONE payment request for ALL orders
                if (requirePaymentPage)
                {
                    if(createdOrders.Count == 0){
                    result.AddError("Sepette geçerli ürün bulunamadı.");
                    return result;
                }
                
                // 1. Get Bank
                var bankRepo = _context.GetRepository<Bank>();
                var bankQuery = bankRepo.GetAll(predicate: b => b.Id == request.CardPayment.BankId.Value);
                var bank = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.Include(bankQuery, b => b.Parameters)
                );
                if (bank == null)
                {
                    result.AddError("Seçilen banka bulunamadı.");
                    return result;
                }

                if (!Enum.TryParse(bank.SystemName, out BankNames bankNameEnum))
                {
                    result.AddError($"Banka entegrasyonu hatası: {bank.SystemName}");
                    return result;
                }

                // 2. Create Payment Provider
                var paymentProvider = _paymentProviderFactory.Create(bankNameEnum);

                // 3. Prepare Gateway Request - SINGLE payment for ALL orders
                var totalAmount = createdOrders.Sum(o => o.GrandTotal);
                var orderNumbers = string.Join(",", createdOrders.Select(o => o.OrderNumber));
                
                // Generate unique PaymentToken (Ultra-Short Hex for Bank Compatibility)
                // Example: 2F4A1B + Random (Max ~10-12 chars)
                var timeSpan = (DateTime.Now - new DateTime(2020, 1, 1)).TotalSeconds;
                var paymentToken = ((int)timeSpan).ToString("X") + new Random().Next(10, 99).ToString();
                
                // Assign PaymentToken to ALL orders and save to database
                foreach(var order in createdOrders)
                {
                    order.PaymentToken = paymentToken;
                }
                
                // CRITICAL: Use BulkUpdate to ensure PaymentToken is saved to database
                // This is necessary because the orders were created with BulkInsert
                await _context.BulkUpdateAsync(createdOrders);
                
                // Store PaymentToken in Redis using USER ID as key (Robust & Stateless)
                // This works because the callback will carry the User's Auth Cookie
                // Store PaymentToken in Redis using USER ID as key (Robust & Stateless)
                // This works because the callback will carry the User's Auth Cookie
                var userIdStr = _httpContextAccessor.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdStr))
                {
                    var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
                    var cacheKey = $"PendingPayment_{userIdStr}";
                    await redisService.SetAsync(cacheKey, paymentToken, TimeSpan.FromMinutes(15));
                }
                else
                {
                    // Fallback for Guest Users (if any): Store by Client IP or specialized logic
                    // For now assuming marketplace users are logged in
                }
                
                var ipAddress = _httpContextAccessor.HttpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
                if (string.IsNullOrEmpty(ipAddress))
                {
                    ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();
                }
                
                // Bank rejects private IPs naturally. Force a public IP if private detected.
                if (string.IsNullOrEmpty(ipAddress) || 
                    ipAddress == "::1" || 
                    ipAddress.StartsWith("127.") || 
                    ipAddress.StartsWith("192.168.") || 
                    ipAddress.StartsWith("10.") ||
                    ipAddress.StartsWith("172.")) 
                {
                    ipAddress = "88.255.145.240"; // Dummy Public IP for Bank Validation
                } 

                // Fetch User & Address Info for Bank Validation
                // Include both User and ApplicationUser to support both Web and Admin contexts
                // Get UserAddress for payment gateway - support both Web and Admin/B2B contexts
                UserAddress? userContract = null;
                
                // Check if this is an ApplicationUser with Customer
                var appUserForPayment = _context.GetRepository<ApplicationUser>()
                    .GetAll(predicate: u => u.Id == userId, disableTracking: true)
                    .FirstOrDefault();
                
                if (appUserForPayment?.CustomerId.HasValue == true)
                {
                    // ApplicationUser with Customer - use Customer's UserAddresses
                    var customerId = appUserForPayment.CustomerId.Value;
                    customerUserIds = _context.GetRepository<ApplicationUser>()
                        .GetAll(predicate: u => u.CustomerId == customerId, disableTracking: true)
                        .Select(u => u.Id)
                        .ToList();
                    
                    var userContractQuery = _context.GetRepository<UserAddress>()
                        .GetAll(disableTracking: false)
                        .Include(x => x.ApplicationUser)
                        .Include(x => x.City)
                        .Include(x => x.Town);
                    
                    if (request.UserAddressId.HasValue)
                    {
                        // Validate address belongs to Customer's ApplicationUsers
                        userContract = userContractQuery
                            .FirstOrDefault(x => x.Id == request.UserAddressId.Value && 
                                                x.ApplicationUserId.HasValue && 
                                                customerUserIds.Contains(x.ApplicationUserId.Value));
                    }
                    else
                    {
                        // Use default or first available address
                        userContract = userContractQuery
                            .Where(x => x.ApplicationUserId.HasValue && 
                                      customerUserIds.Contains(x.ApplicationUserId.Value) &&
                                      x.Status == (int)EntityStatus.Active)
                            .OrderByDescending(x => x.IsDefault)
                            .ThenBy(x => x.CreatedDate)
                            .FirstOrDefault();
                    }
                }
                else
                {
                    // Web context - regular User
                    var userContractQuery = _context.GetRepository<UserAddress>()
                        .GetAll(disableTracking: false)
                        .Include(x => x.User)
                        .Include(x => x.City)
                        .Include(x => x.Town);
                    
                    if (request.UserAddressId.HasValue)
                    {
                        userContract = userContractQuery
                            .FirstOrDefault(x => x.Id == request.UserAddressId.Value && x.UserId == userId);
                    }
                    else
                    {
                        userContract = userContractQuery
                            .Where(x => x.UserId == userId && x.Status == (int)EntityStatus.Active)
                            .OrderByDescending(x => x.IsDefault)
                            .ThenBy(x => x.CreatedDate)
                            .FirstOrDefault();
                    }
                }

                // DEBUG: Log UserAddress fetch result
                _logger.LogDebug("UserAddress fetched: {HasAddress}", userContract != null);
                if (userContract != null)
                {
                    _logger.LogDebug("UserAddress details - FullName: {FullName}, Email: {Email}, Phone: {Phone}, Address: {Address}, City: {City}, Town: {Town}, CityId: {CityId}, TownId: {TownId}", 
                        userContract.FullName, userContract.Email, userContract.PhoneNumber, userContract.Address, 
                        userContract.City?.Name ?? "NULL", userContract.Town?.Name ?? "NULL", userContract.CityId, userContract.TownId);
                }
                else
                {
                    _logger.LogWarning("UserAddress is NULL! UserAddressId: {UserAddressId}, UserId: {UserId}", request.UserAddressId, userId);
                }

                // Determine callback URL based on request context
                // If PlatformType is B2B, use admin-checkout callback on admin.yedeksen.com
                // Otherwise use web checkout callback on yedeksen.com
                string baseUrl;
                string callbackPath;
                
                if (request.PlatformType == OrderPlatformType.Mobile)
                {
                    // Mobil siparişler — callback sonucu WebView'da yakalanacak
                    callbackPath = "/api/Cart/payment-callback";
                    baseUrl = _configuration["AppSettings:ApiBaseUrl"] ?? _configuration["AppSettings:AdminBaseUrl"] ?? "https://api.yedeksen.com";
                }
                else if (request.PlatformType == OrderPlatformType.B2B)
                {
                    // B2B orders use admin.yedeksen.com
                    callbackPath = "/admin-checkout/callback";
                    baseUrl = _configuration["AppSettings:AdminBaseUrl"] ?? "https://admin.yedeksen.com";
                }
                else
                {
                    // Marketplace orders use yedeksen.com
                    callbackPath = "/checkout/callback";
                    baseUrl = _configuration["AppSettings:BaseUrl"];
                    if (string.IsNullOrEmpty(baseUrl) && _httpContextAccessor.HttpContext != null)
                    {
                        var httpRequest = _httpContextAccessor.HttpContext.Request;
                        baseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";
                    }
                    if (string.IsNullOrEmpty(baseUrl))
                    {
                        baseUrl = "https://admin.yedeksen.com"; // Fallback for web
                    }
                }
                
                var callbackUrl = new Uri($"{baseUrl}{callbackPath}?token={paymentToken}");

                // Mobil platform için kart bilgileri boş olabilir — banka 3D formunda girilecek
                // int.Parse boş string'de hata verir, güvenli parse kullan
                int.TryParse(request.CardPayment.ExpMonth, out var expMonth);
                int.TryParse(request.CardPayment.ExpYear, out var expYear);
                
                var gatewayRequest = new PaymentGatewayRequest
                {
                    CardHolderName = request.CardPayment.CardHolderName ?? "",
                    CardNumber = request.CardPayment.CardNumber ?? "",
                    ExpireMonth = expMonth,
                    ExpireYear = expYear,
                    CvvCode = request.CardPayment.Cvv ?? "",
                    TotalAmount = totalAmount,
                    Installment = installmentCount,
                    OrderNumber = paymentToken, 
                    
                    // Pass Address Info
                    InvoiceAddress = userContract,
                    ShippingAddress = userContract,
                  
                    CallbackUrl = callbackUrl,
                    CustomerIpAddress = ipAddress, // Fixed Case
         
                    BankName = bankNameEnum,
                    BankParameters = bank.Parameters.ToDictionary(k => k.Key, v => v.Value),
                    CurrencyIsoCode = "949",
                    LanguageIsoCode = "tr"
                };
                // 4. Execute 3D Request (ONCE for all orders)
                try 
                {
                    var paymentResult = await paymentProvider.ThreeDGatewayRequest(gatewayRequest);
                    if (!paymentResult.Success)
                    {
                        result.AddError($"Ödeme hatası ({bank.Name}): {paymentResult.ErrorMessage}");
                        return result;
                    }
                    
                    if(paymentResult.HtmlContent){
                        result.Result = new CheckoutResultDto{
                            CheckoutFormContent = paymentResult.HtmlFormContent,
                            OrderNumbers = createdOrders.Select(o => o.OrderNumber).ToList(),
                            OrderIds = createdOrders.Select(o => o.Id).ToList()
                        };
                        return result;
                    }
                    else if(paymentResult.Parameters != null && paymentResult.Parameters.Any())
                    {
                        try 
                        {
                            var debugPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                            System.IO.File.AppendAllText(debugPath, $"\nCheckoutService Params Keys: {string.Join(", ", paymentResult.Parameters.Keys)}\n");
                        } catch {}

                        if (paymentResult.Parameters.ContainsKey("HTMLContent"))
                        {
                            var rawHtml = paymentResult.Parameters["HTMLContent"].ToString();
                            
                          
                            try 
                            {
                                var debugPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                                System.IO.File.AppendAllText(debugPath, $"\n--- RAW HTML FROM BANK ---\n{rawHtml}\n--------------------------\n");
                            } catch {}

                            // SAFETY: Ensure no localhost remains in the raw HTML from bank/provider
                            // Replace with platform-specific base URL
                            rawHtml = rawHtml.Replace("http://localhost:5100", baseUrl.TrimEnd('/'));
                            rawHtml = rawHtml.Replace("https://localhost:5100", baseUrl.TrimEnd('/'));
                            rawHtml = rawHtml.Replace("http://localhost:5054", baseUrl.TrimEnd('/')); // Common web port
                            rawHtml = rawHtml.Replace("http://localhost:5019", baseUrl.TrimEnd('/')); // Common admin port

                            try 
                            {
                                var debugPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                                System.IO.File.AppendAllText(debugPath, "Action: Unwrapping HTMLContent directly.\n");
                            } catch {}

                            result.Result = new CheckoutResultDto{
                                CheckoutFormContent = rawHtml,
                                OrderNumbers = createdOrders.Select(o => o.OrderNumber).ToList(),
                                OrderIds = createdOrders.Select(o => o.Id).ToList()
                            };
                            return result;
                        }

                        // Debug: Why are we here?
                        try 
                        {
                            var debugPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                             System.IO.File.AppendAllText(debugPath, "Action: Wrapping Parameters in Form (Fallback).\n");
                        } catch {}

                        var targetUrl = paymentResult.GatewayUrl?.ToString() ?? ""; // Should be Bank URL
                        var sb = new System.Text.StringBuilder();
                        sb.Append($"<form id='PaymentForm' action='{targetUrl}' method='post'>");
                        foreach (var param in paymentResult.Parameters)
                        {
                            sb.Append($"<input type='hidden' name='{param.Key}' value='{param.Value}' />");
                        }
                        sb.Append("</form>");
                        sb.Append("<script>document.getElementById('PaymentForm').submit();</script>");

                        result.Result = new CheckoutResultDto{
                            CheckoutFormContent = sb.ToString(),
                            OrderNumbers = createdOrders.Select(o => o.OrderNumber).ToList(),
                            OrderIds = createdOrders.Select(o => o.Id).ToList()
                        };
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    result.AddError($"Ödeme sistemi hatası: {ex.Message}");
                    return result;
                }

                } // End if (requirePaymentPage)
                else
                {
                    // PAYMENT BYPASS MODE: E-postayı arka planda gönder; aynı DbContext eşzamanlı kullanılmasın diye yeni scope aç
                    var orderNumbersForEmail = createdOrders.Select(c => c.OrderNumber).ToList();
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var scopeContext = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                                var scopeRepo = scopeContext.GetRepository<Orders>();
                                var emailSvc = scope.ServiceProvider.GetRequiredService<IEmailService>();
                                var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<CheckoutService>>();

                                var orderListWithDetails = await scopeRepo.GetAll(
                                        predicate: o => orderNumbersForEmail.Contains(o.OrderNumber))
                                    .Include(o => o.User)
                                    .Include(o => o.ApplicationUser)
                                    .Include(o => o.UserAddress)
                                        .ThenInclude(a => a.City)
                                    .Include(o => o.Seller)
                                    .Include(o => o.OrderItems)
                                        .ThenInclude(oi => oi.Product)
                                    .Include(o => o.OrderItems)
                                        .ThenInclude(oi => oi.ProductImages)
                                    .AsSplitQuery()
                                    .ToListAsync();

                                if (orderListWithDetails != null && orderListWithDetails.Any())
                                {
                                    await emailSvc.SendOrderPlacedCustomerEmail(orderListWithDetails);
                                    foreach (var oDetail in orderListWithDetails)
                                    {
                                        await emailSvc.SendOrderPlacedSellerEmail(oDetail);
                                    }
                                    scopedLogger.LogInformation("Bypass Emails Sent for orders: {OrderNumbers}", orderNumbersForEmail);
                                }
                            }
                        }
                        catch (Exception emailEx)
                        {
                            _logger.LogError(emailEx, "Email send error in bypass");
                        }
                    });
                }

                result.Result = new CheckoutResultDto{
                    OrderNumbers = createdOrders.Select(c => c.OrderNumber).ToList(),
                    OrderIds = createdOrders.Select(c => c.Id).ToList()
                };

                // Clear cart only for non-payment (bypass) orders
                // Sepet giriş yapan kullanıcıya (userId) aittir; Plasiyer müşteri adına sipariş oluştursa bile sepet Plasiyer'indir
                await _orderManager.ClearShoppingCart(userId);
            }
            catch(Exception ex){
                 result.AddSystemError(ex.Message);
            }
            
            return result;
    }
    public async Task<IActionResult<Empty>> OrderDelete(int? targetUserId = null){
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try{
            _logger.LogInformation("OrderDelete: Invoked.");
            var principal = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("OrderDelete: Unauthorized (No User Claim).");
                rs.AddError("Unauthorized");
                return rs;
            }
            
            var cleanupUserId = targetUserId ?? userId;
            _logger.LogInformation("OrderDelete: Cleaning up (ExecuteDelete) for UserId {UserId}", cleanupUserId);

            // ExecuteDeleteAsync bypasses ChangeTracker - Solving "Tracked" issues
            
            // 1. Delete Items
            await _context.DbContext.OrderItems
                .Where(oi => oi.Orders.CompanyId == cleanupUserId && oi.Orders.PaymentStatus == false)
                .ExecuteDeleteAsync();
                
            // 2. Delete Discounts
            await _context.DbContext.Set<OrderAppliedDiscount>()
                .Where(d => d.Order.CompanyId == cleanupUserId && d.Order.PaymentStatus == false)
                .ExecuteDeleteAsync();
                
            // 3. Delete Orders
            var deletedCount = await _context.DbContext.Orders
                .Where(x => x.CompanyId == cleanupUserId && x.PaymentStatus == false)
                .ExecuteDeleteAsync();

            _logger.LogInformation("OrderDelete: Deleted {DeletedCount} orders.", deletedCount);
            rs.AddSuccess("ok");

        } catch(Exception ex){
            _logger.LogError(ex, "OrderDelete: Exception occurred");
            rs.AddSystemError(ex.ToString());
            return rs;
        }
        return rs;
    }

    public async Task<IActionResult<Empty>> DeleteFailedOrders(List<string> orderNumbers)
    {
        var rs = new IActionResult<Empty>{Result = new Empty()};
        try
        {
            if(orderNumbers == null || !orderNumbers.Any()) return rs;

            // Fetch orders to delete (No User Check needed here as it's a System call from Controller, but ideally we should check)
            // Assuming this is called by PaymentCallback which knows the context.
            var orders = _context.DbContext.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.AppliedDiscounts)
                .Where(x => orderNumbers.Contains(x.OrderNumber))
                .ToList();

            if (orders.Any())
            {
                await PerformOrderDeletion(orders);
                rs.AddSuccess("Deleted");
            }
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.Message);
        }
        return rs;
    }

    private async Task PerformOrderDeletion(List<Orders> orders)
    {
         foreach(var order in orders){
             if(order.OrderItems != null && order.OrderItems.Any())
                _context.DbContext.OrderItems.RemoveRange(order.OrderItems);
             
             if(order.AppliedDiscounts != null && order.AppliedDiscounts.Any())
                _context.DbContext.Set<OrderAppliedDiscount>().RemoveRange(order.AppliedDiscounts);

             _context.DbContext.Orders.Remove(order);
         }
         await _context.SaveChangesAsync();
    }
}