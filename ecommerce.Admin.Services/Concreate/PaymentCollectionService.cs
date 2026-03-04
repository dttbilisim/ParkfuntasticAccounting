using ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;
using ecommerce.Admin.Domain.Dtos.PaymentCollection;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Accounting;
using ecommerce.Core.Helpers;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate;

/// <summary>
/// Plasiyer ödeme alma servisi — nakit ve sanal POS ödeme işlemlerini yönetir
/// </summary>
public class PaymentCollectionService : IPaymentCollectionService
{
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly ICustomerAccountTransactionService _transactionService;
    private readonly ICollectionReceiptService _collectionReceiptService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentCollectionService> _logger;

    public PaymentCollectionService(
        IUnitOfWork<ApplicationDbContext> context,
        ICustomerAccountTransactionService transactionService,
        ICollectionReceiptService collectionReceiptService,
        ITenantProvider tenantProvider,
        IPaymentProviderFactory paymentProviderFactory,
        IConfiguration configuration,
        IHttpContextAccessor httpContextAccessor,
        IServiceProvider serviceProvider,
        ILogger<PaymentCollectionService> logger)
    {
        _context = context;
        _transactionService = transactionService;
        _collectionReceiptService = collectionReceiptService;
        _tenantProvider = tenantProvider;
        _paymentProviderFactory = paymentProviderFactory;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Carinin faturalaşmamış siparişlerini getirir — plasiyer yetki kontrolü dahil
    /// </summary>
    public async Task<IActionResult<List<UnfacturedOrderMobileDto>>> GetUnfacturedOrdersByCustomer(int customerId, int salesPersonId)
    {
        var result = new IActionResult<List<UnfacturedOrderMobileDto>> { Result = new List<UnfacturedOrderMobileDto>() };

        try
        {
            // Plasiyer-cari yetki kontrolü
            var isLinked = await _context.DbContext.CustomerPlasiyers
                .AsNoTracking()
                .AnyAsync(cp => cp.SalesPersonId == salesPersonId && cp.CustomerId == customerId);

            if (!isLinked)
            {
                result.AddError("Bu cariye erişim yetkiniz bulunmamaktadır.");
                return result;
            }

            // Cariye bağlı kullanıcı ID'lerini bul
            var customerUserIds = await _context.GetRepository<ecommerce.Core.Entities.Authentication.ApplicationUser>()
                .GetAll(predicate: u => u.CustomerId == customerId, disableTracking: true)
                .Select(u => u.Id)
                .ToListAsync();

            if (!customerUserIds.Any())
            {
                _logger.LogWarning("Cariye bağlı kullanıcı bulunamadı. CustomerId: {CustomerId}", customerId);
                return result; // Boş liste dön
            }

            // Faturalaşmamış siparişleri çek (InvoiceId == null ve iptal edilmemiş)
            var orders = await _context.GetRepository<Orders>()
                .GetAll(
                    predicate: o => customerUserIds.Contains(o.CompanyId)
                        && o.InvoiceId == null
                        && o.OrderStatusType != OrderStatusType.OrderCanceled,
                    include: q => q.Include(o => o.OrderItems),
                    ignoreQueryFilters: true
                )
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();

            // BranchId filtreleme
            var currentBranchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : 0;
            if (currentBranchId > 0)
            {
                orders = orders.Where(o => o.BranchId == currentBranchId || o.BranchId == null).ToList();
            }

            result.Result = orders.Select(o => new UnfacturedOrderMobileDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CreatedDate = o.CreatedDate,
                GrandTotal = o.GrandTotal,
                OrderStatusType = o.OrderStatusType,
                ItemCount = o.OrderItems?.Count ?? 0
            }).ToList();

            _logger.LogInformation("Faturalaşmamış siparişler getirildi. CustomerId: {CustomerId}, SalesPersonId: {SalesPersonId}, Adet: {Count}",
                customerId, salesPersonId, result.Result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Faturalaşmamış siparişler getirilirken hata. CustomerId: {CustomerId}", customerId);
            result.AddSystemError("Siparişler yüklenirken bir hata oluştu.");
        }

        return result;
    }

    /// <summary>
    /// Nakit ödeme alır — CustomerAccountTransaction tablosuna Credit kaydı oluşturur
    /// </summary>
    public async Task<IActionResult<CollectPaymentResultDto>> CollectCashPayment(CollectPaymentRequestDto request, int salesPersonId, int userId)
    {
        var result = new IActionResult<CollectPaymentResultDto> { Result = new CollectPaymentResultDto() };

        try
        {
            // Validasyon
            var validationError = ValidateRequest(request);
            if (validationError != null)
            {
                result.AddError(validationError);
                return result;
            }

            // Nakit ödeme için kasa ID zorunlu
            if (!request.CashRegisterId.HasValue || request.CashRegisterId.Value <= 0)
            {
                result.AddError("Nakit ödeme için kasa seçimi zorunludur.");
                return result;
            }

            // Plasiyer-cari yetki kontrolü
            var isLinked = await _context.DbContext.CustomerPlasiyers
                .AsNoTracking()
                .AnyAsync(cp => cp.SalesPersonId == salesPersonId && cp.CustomerId == request.CustomerId);

            if (!isLinked)
            {
                result.AddError("Bu cariye erişim yetkiniz bulunmamaktadır.");
                return result;
            }

            // Siparişlerin faturalaşmamış olduğunu doğrula (sipariş varsa)
            if (request.OrderIds != null && request.OrderIds.Any())
            {
                var unfacturedCheck = await ValidateOrdersUnfactured(request.OrderIds, request.CustomerId);
                if (unfacturedCheck != null)
                {
                    result.AddError(unfacturedCheck);
                    return result;
                }
            }

            int? transactionIdForReceipt = null;

            // Sipariş bağlamı varsa her sipariş için ayrı transaction, yoksa tek transaction
            if (request.OrderIds != null && request.OrderIds.Any())
            {
                // Sipariş numaralarını al (ReferenceNo için)
                var orderNumbers = await GetOrderNumbers(request.OrderIds);

                // Her sipariş için ayrı transaction oluştur
                foreach (var orderId in request.OrderIds)
                {
                    var orderNumber = orderNumbers.GetValueOrDefault(orderId, orderId.ToString());
                    var orderAmount = await GetOrderGrandTotal(orderId);

                    var transactionDto = new CustomerAccountTransactionUpsertDto
                    {
                        CustomerId = request.CustomerId,
                        OrderId = orderId,
                        TransactionType = CustomerAccountTransactionType.Credit,
                        Amount = orderAmount > 0 ? orderAmount : request.Amount / request.OrderIds.Count,
                        TransactionDate = DateTime.Now,
                        Description = $"Plasiyer nakit tahsilat — Sipariş: {orderNumber}",
                        PaymentTypeId = null, // Nakit ödeme
                        CashRegisterId = request.CashRegisterId,
                        ReferenceNo = orderNumber
                    };

                    var auditWrap = new AuditWrapDto<CustomerAccountTransactionUpsertDto>
                    {
                        UserId = userId,
                        Dto = transactionDto
                    };

                    var txResult = await _transactionService.CreateTransaction(auditWrap);
                    if (!txResult.Ok)
                    {
                        var errorMsg = txResult.Metadata?.Message ?? "Ödeme kaydı oluşturulamadı.";
                        _logger.LogError("Nakit ödeme kaydı başarısız. OrderId: {OrderId}, Hata: {Error}", orderId, errorMsg);
                        result.AddError(errorMsg);
                        return result;
                    }
                    var createdTxId = txResult.Result;
                    if (createdTxId > 0)
                    {
                        transactionIdForReceipt = createdTxId;
                        var branchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : (int?)null;
                        var receiptResult = await _collectionReceiptService.CreateReceiptAsync(createdTxId, request.CustomerId, salesPersonId, branchId, userId);
                        if (receiptResult.Ok && !string.IsNullOrEmpty(receiptResult.Result))
                        {
                            _logger.LogInformation("Tahsilat makbuzu oluşturuldu. MakbuzNo: {MakbuzNo}, TransactionId: {TransactionId}", receiptResult.Result, createdTxId);
                            await CreateTahsilatCashRegisterMovementAsync(request.CashRegisterId!.Value, request.CustomerId, salesPersonId, orderAmount > 0 ? orderAmount : request.Amount / request.OrderIds!.Count, receiptResult.Result, createdTxId, branchId ?? 0, userId);
                        }
                    }
                }
            }
            else
            {
                // Sipariş bağlamı yok — direkt tutar üzerinden tek transaction
                var transactionDto = new CustomerAccountTransactionUpsertDto
                {
                    CustomerId = request.CustomerId,
                    OrderId = null,
                    TransactionType = CustomerAccountTransactionType.Credit,
                    Amount = request.Amount,
                    TransactionDate = DateTime.Now,
                    Description = $"Plasiyer nakit tahsilat — Serbest ödeme",
                    PaymentTypeId = null,
                    CashRegisterId = request.CashRegisterId,
                    ReferenceNo = null
                };

                var auditWrap = new AuditWrapDto<CustomerAccountTransactionUpsertDto>
                {
                    UserId = userId,
                    Dto = transactionDto
                };

                var txResult = await _transactionService.CreateTransaction(auditWrap);
                if (!txResult.Ok)
                {
                    var errorMsg = txResult.Metadata?.Message ?? "Ödeme kaydı oluşturulamadı.";
                    _logger.LogError("Nakit serbest ödeme kaydı başarısız. Hata: {Error}", errorMsg);
                    result.AddError(errorMsg);
                    return result;
                }
                var createdTxId = txResult.Result;
                if (createdTxId > 0)
                {
                    transactionIdForReceipt = createdTxId;
                    var branchId = _tenantProvider.IsMultiTenantEnabled ? _tenantProvider.GetCurrentBranchId() : (int?)null;
                    var receiptResult = await _collectionReceiptService.CreateReceiptAsync(createdTxId, request.CustomerId, salesPersonId, branchId, userId);
                    if (receiptResult.Ok && !string.IsNullOrEmpty(receiptResult.Result))
                    {
                        _logger.LogInformation("Tahsilat makbuzu oluşturuldu. MakbuzNo: {MakbuzNo}, TransactionId: {TransactionId}", receiptResult.Result, createdTxId);
                        await CreateTahsilatCashRegisterMovementAsync(request.CashRegisterId!.Value, request.CustomerId, salesPersonId, request.Amount, receiptResult.Result, createdTxId, branchId ?? 0, userId);
                    }
                }
            }

            result.Result = new CollectPaymentResultDto
            {
                Success = true,
                Message = "Nakit ödeme başarıyla kaydedildi.",
                TransactionId = transactionIdForReceipt
            };

            _logger.LogInformation("Nakit ödeme başarılı. CustomerId: {CustomerId}, SalesPersonId: {SalesPersonId}, Tutar: {Amount}, SiparişSayısı: {OrderCount}",
                request.CustomerId, salesPersonId, request.Amount, request.OrderIds?.Count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nakit ödeme işlemi sırasında hata. CustomerId: {CustomerId}", request.CustomerId);
            result.AddSystemError("Ödeme işlemi sırasında bir hata oluştu.");
        }

        return result;
    }

    /// <summary>
    /// Sanal POS (kredi kartı) ödeme alır — şimdilik sadece placeholder, 
    /// CheckoutService'deki PaymentProviderFactory akışı entegre edilecek
    /// </summary>
    public async Task<IActionResult<CollectPaymentResultDto>> CollectCardPayment(CollectPaymentRequestDto request, int salesPersonId, int userId)
    {
        var result = new IActionResult<CollectPaymentResultDto> { Result = new CollectPaymentResultDto() };

        try
        {
            // Validasyon
            var validationError = ValidateRequest(request);
            if (validationError != null)
            {
                result.AddError(validationError);
                return result;
            }

            // Kart bilgisi zorunlu
            if (request.CardPayment == null || !request.CardPayment.BankId.HasValue)
            {
                result.AddError("Kredi kartı ödemesi için banka ve kart bilgileri gereklidir.");
                return result;
            }

            // Plasiyer-cari yetki kontrolü
            var isLinked = await _context.DbContext.CustomerPlasiyers
                .AsNoTracking()
                .AnyAsync(cp => cp.SalesPersonId == salesPersonId && cp.CustomerId == request.CustomerId);

            if (!isLinked)
            {
                result.AddError("Bu cariye erişim yetkiniz bulunmamaktadır.");
                return result;
            }

            // Siparişlerin faturalaşmamış olduğunu doğrula
            if (request.OrderIds != null && request.OrderIds.Any())
            {
                var unfacturedCheck = await ValidateOrdersUnfactured(request.OrderIds, request.CustomerId);
                if (unfacturedCheck != null)
                {
                    result.AddError(unfacturedCheck);
                    return result;
                }
            }

            // 1. Bank entity'sini çek (Parameters dahil)
            var bankRepo = _context.GetRepository<Bank>();
            var bankQuery = bankRepo.GetAll(predicate: b => b.Id == request.CardPayment.BankId.Value);
            var bank = await bankQuery
                .Include(b => b.Parameters)
                .FirstOrDefaultAsync();

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

            // 2. PaymentProviderFactory ile provider oluştur
            var paymentProvider = _paymentProviderFactory.Create(bankNameEnum);

            // 3. Taksit hesapla
            decimal installmentRate = 0;
            int installmentCount = 1;

            if (request.CardPayment.InstallmentId.HasValue)
            {
                var installmentRepo = _context.GetRepository<BankCreditCardInstallment>();
                var installment = await installmentRepo
                    .GetAll(predicate: x => x.Id == request.CardPayment.InstallmentId.Value)
                    .FirstOrDefaultAsync();
                if (installment != null)
                {
                    installmentRate = installment.InstallmentRate;
                    installmentCount = installment.Installment;
                }
            }

            // 4. Toplam tutarı hesapla (taksit komisyonu dahil)
            var totalAmount = request.Amount * (1 + (installmentRate / 100));

            // 5. Benzersiz PaymentToken oluştur
            var timeSpan = (DateTime.Now - new DateTime(2020, 1, 1)).TotalSeconds;
            var paymentToken = $"PC{((int)timeSpan).ToString("X")}{new Random().Next(10, 99)}";

            // 6. Redis'e token bilgilerini kaydet — callback'te kullanılacak
            var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
            var cacheData = new PaymentCollectionCacheData
            {
                PaymentToken = paymentToken,
                CustomerId = request.CustomerId,
                SalesPersonId = salesPersonId,
                UserId = userId,
                Amount = request.Amount,
                TotalAmount = totalAmount,
                InstallmentCount = installmentCount,
                InstallmentRate = installmentRate,
                BankId = request.CardPayment.BankId.Value,
                OrderIds = request.OrderIds
            };
            var cacheKey = $"PaymentCollection_{paymentToken}";
            await redisService.SetAsync(cacheKey, cacheData, TimeSpan.FromMinutes(15));

            _logger.LogInformation("Plasiyer tahsilat 3D Secure başlatılıyor. Token: {Token}, CustomerId: {CustomerId}, Amount: {Amount}, TotalAmount: {TotalAmount}",
                paymentToken, request.CustomerId, request.Amount, totalAmount);

            // 7. IP adresi al
            var ipAddress = _httpContextAccessor.HttpContext?.Request?.Headers["X-Forwarded-For"].FirstOrDefault();
            if (string.IsNullOrEmpty(ipAddress))
                ipAddress = _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

            // Banka özel IP'leri reddeder — geçerli bir public IP kullan
            if (string.IsNullOrEmpty(ipAddress) || ipAddress == "::1" || ipAddress.StartsWith("127.") ||
                ipAddress.StartsWith("192.168.") || ipAddress.StartsWith("10.") || ipAddress.StartsWith("172."))
            {
                ipAddress = "88.255.145.240";
            }

            // 8. Callback URL oluştur — EP API'deki plasiyer tahsilat callback endpoint'i
            // Önce konfigürasyondan, yoksa mevcut HTTP isteğinin base URL'inden al
            var apiBaseUrl = _configuration["AppSettings:ApiBaseUrl"];
            if (string.IsNullOrEmpty(apiBaseUrl))
                apiBaseUrl = _configuration["AppSettings:AdminBaseUrl"];
            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                // Konfigürasyon yoksa mevcut HTTP isteğinden base URL oluştur
                var httpRequest = _httpContextAccessor.HttpContext?.Request;
                if (httpRequest != null)
                    apiBaseUrl = $"{httpRequest.Scheme}://{httpRequest.Host}";
                else
                    apiBaseUrl = "https://api.yedeksen.com";
            }
            var callbackUrl = new Uri($"{apiBaseUrl.TrimEnd('/')}/api/Cart/payment-collection-callback?token={paymentToken}&source=mobile-browser");

            // 9. Kart bilgilerini parse et — mobil'den boş gelebilir, banka 3D formunda girilecek
            int.TryParse(request.CardPayment.ExpMonth, out var expMonth);
            int.TryParse(request.CardPayment.ExpYear, out var expYear);

            // 10. PaymentGatewayRequest hazırla
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
                CallbackUrl = callbackUrl,
                CustomerIpAddress = ipAddress,
                BankName = bankNameEnum,
                BankParameters = bank.Parameters.ToDictionary(k => k.Key, v => v.Value),
                CurrencyIsoCode = "949",
                LanguageIsoCode = "tr"
            };

            // 11. 3D Secure Gateway isteği gönder
            var paymentResult = await paymentProvider.ThreeDGatewayRequest(gatewayRequest);

            if (!paymentResult.Success)
            {
                result.AddError($"Ödeme hatası ({bank.Name}): {paymentResult.ErrorMessage}");
                return result;
            }

            // 12. Banka'dan dönen HTML form content'i response'a koy
            string? htmlContent = null;

            if (paymentResult.HtmlContent)
            {
                htmlContent = paymentResult.HtmlFormContent;
            }
            else if (paymentResult.Parameters != null && paymentResult.Parameters.ContainsKey("HTMLContent"))
            {
                htmlContent = paymentResult.Parameters["HTMLContent"].ToString();
            }
            else if (paymentResult.Parameters != null && paymentResult.Parameters.Any())
            {
                // Fallback: parametreleri form olarak sar
                var targetUrl = paymentResult.GatewayUrl?.ToString() ?? "";
                var sb = new System.Text.StringBuilder();
                sb.Append($"<form id='PaymentForm' action='{targetUrl}' method='post'>");
                foreach (var param in paymentResult.Parameters)
                {
                    sb.Append($"<input type='hidden' name='{param.Key}' value='{param.Value}' />");
                }
                sb.Append("</form><script>document.getElementById('PaymentForm').submit();</script>");
                htmlContent = sb.ToString();
            }

            if (string.IsNullOrEmpty(htmlContent))
            {
                result.AddError("Banka 3D Secure formu alınamadı.");
                return result;
            }

            // Localhost URL'lerini temizle
            htmlContent = htmlContent.Replace("http://localhost:5100", apiBaseUrl.TrimEnd('/'));
            htmlContent = htmlContent.Replace("https://localhost:5100", apiBaseUrl.TrimEnd('/'));

            result.Result = new CollectPaymentResultDto
            {
                Success = true,
                Message = "3D Secure formu hazır.",
                CheckoutFormContent = htmlContent
            };

            _logger.LogInformation("Plasiyer tahsilat 3D Secure formu oluşturuldu. Token: {Token}, CustomerId: {CustomerId}, Banka: {BankName}",
                paymentToken, request.CustomerId, bank.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kart ödeme işlemi sırasında hata. CustomerId: {CustomerId}", request.CustomerId);
            result.AddSystemError("Ödeme işlemi sırasında bir hata oluştu.");
        }

        return result;
    }

    #region Private Yardımcı Metodlar

    /// <summary>
    /// Tahsilat (TH) işlemi için CashRegisterMovement kaydı oluşturur.
    /// Dokümana göre: tCashTransaction tablosuna "TH" satırı atılmalı.
    /// </summary>
    private async Task CreateTahsilatCashRegisterMovementAsync(int cashRegisterId, int customerId, int salesPersonId, decimal amount, string transCode, int customerAccountTransactionId, int branchId, int userId)
    {
        try
        {
            var cashRegister = await _context.GetRepository<CashRegister>()
                .GetAll(predicate: x => x.Id == cashRegisterId, include: q => q.Include(x => x.Currency))
                .FirstOrDefaultAsync();

            if (cashRegister == null)
            {
                _logger.LogWarning("Tahsilat için kasa bulunamadı. CashRegisterId: {CashRegisterId}", cashRegisterId);
                return;
            }

            var movement = new CashRegisterMovement
            {
                CashRegisterId = cashRegisterId,
                MovementType = CashRegisterMovementType.In,
                ProcessType = CashRegisterMovementProcessType.TH,
                TransCode = transCode,
                CustomerId = customerId,
                SalesPersonId = salesPersonId,
                CustomerAccountTransactionId = customerAccountTransactionId,
                PaymentTypeId = null,
                CurrencyId = cashRegister.CurrencyId,
                Amount = amount,
                TransactionDate = DateTime.Now,
                Description = $"Tahsilat makbuzu — {transCode}",
                BranchId = branchId,
                Status = (int)EntityStatus.Active,
                CreatedDate = DateTime.Now,
                CreatedId = userId
            };

            _context.GetRepository<CashRegisterMovement>().Insert(movement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Tahsilat kasa hareketi oluşturuldu. ProcessType=TH, TransCode={TransCode}, Amount={Amount}", transCode, amount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tahsilat kasa hareketi oluşturulurken hata. CustomerId: {CustomerId}, Amount: {Amount}", customerId, amount);
        }
    }

    /// <summary>
    /// Ödeme isteğinin temel validasyonunu yapar
    /// </summary>
    private string? ValidateRequest(CollectPaymentRequestDto request)
    {
        if (request.CustomerId <= 0)
            return "Geçersiz müşteri ID.";

        if (request.Amount <= 0)
            return "Ödeme tutarı sıfırdan büyük olmalıdır.";

        return null;
    }

    /// <summary>
    /// Seçilen siparişlerin faturalaşmamış olduğunu doğrular
    /// </summary>
    private async Task<string?> ValidateOrdersUnfactured(List<int> orderIds, int customerId)
    {
        // Cariye bağlı kullanıcı ID'lerini bul
        var customerUserIds = await _context.GetRepository<ecommerce.Core.Entities.Authentication.ApplicationUser>()
            .GetAll(predicate: u => u.CustomerId == customerId, disableTracking: true)
            .Select(u => u.Id)
            .ToListAsync();

        var orders = await _context.GetRepository<Orders>()
            .GetAll(
                predicate: o => orderIds.Contains(o.Id) && customerUserIds.Contains(o.CompanyId),
                disableTracking: true,
                ignoreQueryFilters: true
            )
            .ToListAsync();

        // Tüm sipariş ID'leri bulunmalı
        if (orders.Count != orderIds.Count)
            return "Seçilen siparişlerden bazıları bulunamadı veya bu cariye ait değil.";

        // Hiçbiri faturalaşmış olmamalı
        var facturedOrders = orders.Where(o => o.InvoiceId != null).ToList();
        if (facturedOrders.Any())
            return "Seçilen siparişlerden bazıları zaten faturalaşmıştır.";

        return null;
    }

    /// <summary>
    /// Sipariş ID'lerinden sipariş numaralarını Dictionary olarak döndürür
    /// </summary>
    private async Task<Dictionary<int, string>> GetOrderNumbers(List<int> orderIds)
    {
        var orders = await _context.GetRepository<Orders>()
            .GetAll(
                predicate: o => orderIds.Contains(o.Id),
                disableTracking: true,
                ignoreQueryFilters: true
            )
            .Select(o => new { o.Id, o.OrderNumber })
            .ToListAsync();

        return orders.ToDictionary(o => o.Id, o => o.OrderNumber ?? o.Id.ToString());
    }

    /// <summary>
    /// Siparişin GrandTotal değerini döndürür
    /// </summary>
    private async Task<decimal> GetOrderGrandTotal(int orderId)
    {
        var order = await _context.GetRepository<Orders>()
            .GetAll(
                predicate: o => o.Id == orderId,
                disableTracking: true,
                ignoreQueryFilters: true
            )
            .Select(o => new { o.GrandTotal })
            .FirstOrDefaultAsync();

        return order?.GrandTotal ?? 0;
    }

    #endregion
}

/// <summary>
/// Plasiyer tahsilat callback bilgilerini Redis'te saklamak için kullanılan veri sınıfı
/// </summary>
public class PaymentCollectionCacheData
{
    public string PaymentToken { get; set; } = "";
    public int CustomerId { get; set; }
    public int SalesPersonId { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public decimal InstallmentRate { get; set; }
    public int BankId { get; set; }
    public List<int>? OrderIds { get; set; }
}
