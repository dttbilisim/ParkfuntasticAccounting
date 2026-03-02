using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Domain.Shared.Emailing;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Mobil 3D Secure ödeme callback controller'ı.
/// Banka, ödeme sonucunu bu endpoint'e POST eder.
/// WebView'da yakalanabilecek HTML response döner.
/// </summary>
[Route("api/Cart")]
public class MobilePaymentCallbackController : Controller
{
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly IUnitOfWork<ApplicationDbContext> _uow;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICheckoutService _checkoutService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MobilePaymentCallbackController> _logger;

    public MobilePaymentCallbackController(
        IPaymentProviderFactory paymentProviderFactory,
        IUnitOfWork<ApplicationDbContext> uow,
        IServiceProvider serviceProvider,
        ICheckoutService checkoutService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<MobilePaymentCallbackController> logger)
    {
        _paymentProviderFactory = paymentProviderFactory;
        _uow = uow;
        _serviceProvider = serviceProvider;
        _checkoutService = checkoutService;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Banka 3D Secure callback'i — mobil WebView'dan yakalanır.
    /// </summary>
    [HttpPost("payment-callback")]
    public async Task<IActionResult> Callback(IFormCollection form)
    {
        _logger.LogInformation("=== Mobil Payment Callback STARTED ({Time}) ===", DateTime.Now);

        try
        {
            string paymentToken = "";
            List<Orders>? orders = null;

            var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
            string? cacheKey = null;

            // 1. User ID üzerinden Redis'ten token al
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                cacheKey = $"PendingPayment_{userIdStr}";
                paymentToken = await redisService.GetAsync<string>(cacheKey) ?? string.Empty;
            }

            var orderRepo = _uow.GetRepository<Orders>();

            // 2. Redis'te yoksa URL token veya POST body 'oid' kullan
            if (string.IsNullOrEmpty(paymentToken))
            {
                string? tokenToSearch = null;

                if (Request.Query.ContainsKey("token"))
                    tokenToSearch = Request.Query["token"].ToString()?.Trim();

                if (string.IsNullOrEmpty(tokenToSearch) && form.ContainsKey("oid"))
                    tokenToSearch = form["oid"].ToString()?.Trim();

                if (!string.IsNullOrEmpty(tokenToSearch))
                {
                    paymentToken = tokenToSearch;
                    var tokenUpper = tokenToSearch.ToUpper();
                    orders = await orderRepo.GetAll(predicate: o => o.PaymentToken == tokenToSearch || o.PaymentToken == tokenUpper)
                        .Include(o => o.Bank).ThenInclude(b => b!.Parameters)
                        .ToListAsync();
                }
            }
            else
            {
                orders = await orderRepo.GetAll(predicate: o => o.PaymentToken == paymentToken)
                    .Include(o => o.Bank).ThenInclude(b => b!.Parameters)
                    .ToListAsync();
            }

            // 3. Sipariş bulunamadı kontrolü
            if (orders == null || !orders.Any())
            {
                _logger.LogWarning("Mobil callback: Sipariş bulunamadı. Token: {Token}", paymentToken);
                return ReturnMobileResponse(false, "Sipariş bulunamadı veya ödeme oturumu sona erdi.");
            }

            var firstOrder = orders.First();
            if (firstOrder.Bank == null || !Enum.TryParse(firstOrder.Bank.SystemName, out ecommerce.Core.Utils.BankNames bankNameEnum))
            {
                return ReturnMobileResponse(false, "Banka bilgisi geçersiz");
            }

            // 4. Banka ile ödeme doğrulama
            var provider = _paymentProviderFactory.Create(bankNameEnum);
            var bankParams = firstOrder.Bank.Parameters.ToDictionary(k => k.Key, v => v.Value);

            var verifyRequest = new VerifyGatewayRequest { BankParameters = bankParams };

            // Form verilerini kopyala ve eksik alanları enjekte et
            var modifiedForm = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            foreach (var key in form.Keys) modifiedForm[key] = form[key];

            if (!modifiedForm.ContainsKey("oid")) modifiedForm["oid"] = paymentToken;

            // Hidden input scraper — HTMLContent varsa
            if (modifiedForm.ContainsKey("HTMLContent"))
            {
                var htmlContent = modifiedForm["HTMLContent"].ToString();
                var matches = System.Text.RegularExpressions.Regex.Matches(htmlContent,
                    @"<input[^>]*name=[""']?([^""'\s>]+)[""']?[^>]*value=[""']?([^""']*)[""']?",
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var k = match.Groups[1].Value;
                        var v = match.Groups[2].Value;
                        if (!modifiedForm.ContainsKey(k)) modifiedForm[k] = v;
                    }
                }
            }

            // Eksik transaction parametrelerini enjekte et
            if (!modifiedForm.ContainsKey("amount"))
            {
                var totalAmount = orders.Sum(x => x.GrandTotal);
                modifiedForm["amount"] = totalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (!modifiedForm.ContainsKey("currency")) modifiedForm["currency"] = "949";
            if (!modifiedForm.ContainsKey("installment")) modifiedForm["installment"] = "1";
            if (!modifiedForm.ContainsKey("clientId"))
            {
                var clientId = bankParams.FirstOrDefault(x => x.Key.Equals("ClientId", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("MerchantID", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(clientId)) modifiedForm["clientId"] = clientId;
            }
            if (!modifiedForm.ContainsKey("storeKey"))
            {
                var val = bankParams.FirstOrDefault(x => x.Key.Equals("storeKey", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(val)) modifiedForm["storeKey"] = val;
            }
            if (!modifiedForm.ContainsKey("storetype"))
            {
                var val = bankParams.FirstOrDefault(x => x.Key.Equals("storeType", StringComparison.OrdinalIgnoreCase)).Value;
                modifiedForm["storetype"] = val ?? "3D_PAY";
            }

            // Callback URL'leri enjekte et
            var apiBaseUrl = _configuration["AppSettings:ApiBaseUrl"] ?? _configuration["AppSettings:AdminBaseUrl"];
            if (string.IsNullOrEmpty(apiBaseUrl))
                apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var callbackUrl = $"{apiBaseUrl.TrimEnd('/')}/api/Cart/payment-callback?token={paymentToken}";
            if (!modifiedForm.ContainsKey("okUrl")) modifiedForm["okUrl"] = callbackUrl;
            if (!modifiedForm.ContainsKey("failUrl")) modifiedForm["failUrl"] = callbackUrl;

            // Pre-validation: mdStatus kontrolü
            var mdStatus = modifiedForm.ContainsKey("mdStatus") ? modifiedForm["mdStatus"].ToString() : "";
            var errMsg = modifiedForm.ContainsKey("ErrMsg") ? modifiedForm["ErrMsg"].ToString() : "";

            if (string.IsNullOrEmpty(mdStatus))
            {
                var detail = !string.IsNullOrEmpty(errMsg) ? errMsg : "Banka eksik yanıt döndü.";
                return await FailAndCleanup(orders, $"3D doğrulama başarısız: {detail}");
            }
            if (mdStatus != "1")
            {
                var failReason = !string.IsNullOrEmpty(errMsg) ? errMsg : "3D doğrulama başarısız";
                return await FailAndCleanup(orders, $"3D doğrulama reddedildi (mdStatus={mdStatus}): {failReason}");
            }

            var procReturnCode = modifiedForm.ContainsKey("procReturnCode") ? modifiedForm["procReturnCode"].ToString() : "";
            if (!string.IsNullOrEmpty(procReturnCode) && procReturnCode != "00")
            {
                var failReason = !string.IsNullOrEmpty(errMsg) ? errMsg : "Ödeme işlemi reddedildi";
                return await FailAndCleanup(orders, $"Ödeme başarısız (Kod: {procReturnCode}): {failReason}");
            }

            var formCollection = new FormCollection(modifiedForm);
            var result = await provider.VerifyGateway(verifyRequest, null!, formCollection);

            if (result.Success)
            {
                // Ödeme başarılı — siparişleri onayla
                foreach (var order in orders)
                {
                    order.Bank = null; // Detach — identity resolution çakışmasını önle
                    order.PaymentStatus = true;
                    order.PaymentId = result.TransactionId ?? result.ReferenceNumber ?? Guid.NewGuid().ToString("N");
                    order.OrderStatusType = ecommerce.Core.Utils.OrderStatusType.OrderNew;
                    orderRepo.Update(order);
                }

                // Sepeti temizle
                if (int.TryParse(userIdStr, out var uId))
                {
                    try
                    {
                        var orderManager = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Services.IOrderManager>();
                        await orderManager.ClearShoppingCart(uId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Mobil callback: Sepet temizleme hatası");
                    }
                }

                await _uow.SaveChangesAsync();

                // E-posta gönder (fire-and-forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var orderNumbers = orders.Select(o => o.OrderNumber).ToList();
                        var orderListWithDetails = await orderRepo.GetAll(
                            predicate: o => orderNumbers.Contains(o.OrderNumber))
                            .Include(o => o.ApplicationUser)
                            .Include(o => o.UserAddress).ThenInclude(a => a!.City)
                            .Include(o => o.Seller)
                            .Include(o => o.OrderItems).ThenInclude(oi => oi.Product)
                            .Include(o => o.OrderItems).ThenInclude(oi => oi.ProductImages)
                            .AsSplitQuery()
                            .ToListAsync();

                        if (orderListWithDetails?.Any() == true)
                        {
                            await _emailService.SendOrderPlacedCustomerEmail(orderListWithDetails);
                            foreach (var oDetail in orderListWithDetails)
                                await _emailService.SendOrderPlacedSellerEmail(oDetail);
                        }
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogError(emailEx, "Mobil callback: E-posta gönderim hatası");
                    }
                });

                // Redis temizle
                if (string.IsNullOrEmpty(cacheKey))
                {
                    var oUserId = orders.FirstOrDefault()?.CompanyId;
                    if (oUserId != null && oUserId > 0) cacheKey = $"PendingPayment_{oUserId}";
                }
                if (!string.IsNullOrEmpty(cacheKey)) await redisService.RemoveAsync(cacheKey);

                var firstOrderNumber = orders.FirstOrDefault()?.OrderNumber;
                _logger.LogInformation("Mobil ödeme başarılı. Sipariş: {OrderNumber}", firstOrderNumber);
                return ReturnMobileResponse(true, firstOrderNumber ?? "0");
            }
            else
            {
                return await FailAndCleanup(orders, result.ErrorMessage ?? "Ödeme doğrulaması başarısız");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobil payment callback hatası");
            return ReturnMobileResponse(false, "Sistem hatası: " + ex.Message);
        }
    }

    /// <summary>
    /// Mobil ödeme sonucu döner.
    /// Eğer istek "source=mobile-browser" parametresi içeriyorsa deep link ile uygulamaya yönlendirir.
    /// Aksi halde eski WebView yaklaşımı ile HTML response döner.
    /// </summary>
    private IActionResult ReturnMobileResponse(bool success, string message)
    {
        var safeMessage = message.Replace("'", "\\'").Replace("\n", " ");

        // Mobil tarayıcıdan gelen istek mi kontrol et — deep link ile uygulamaya dön
        var source = Request.Query.ContainsKey("source") ? Request.Query["source"].ToString() : "";
        if (source == "mobile-browser")
        {
            // Deep link ile uygulamaya yönlendir
            var deepLinkUrl = success
                ? $"exp+bicops://payment-callback?status=success&message={Uri.EscapeDataString(message)}"
                : $"exp+bicops://payment-callback?status=fail&message={Uri.EscapeDataString(message)}";

            var html = $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'></head>
<body style='background:#f8f9fa; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; flex-direction:column; text-align:center;'>
    <h3 style='color:{(success ? "#10b981" : "#ef4444")}'>{(success ? "Ödeme Başarılı!" : "Ödeme Başarısız")}</h3>
    <p style='color:#666;'>{(success ? "Uygulamaya yönlendiriliyorsunuz..." : safeMessage)}</p>
    <script>
        // Deep link ile uygulamaya dön
        window.location.href = '{deepLinkUrl}';
        // Fallback — 2 saniye sonra tekrar dene (bazı tarayıcılar ilk denemede engelleyebilir)
        setTimeout(function() {{ window.location.href = '{deepLinkUrl}'; }}, 2000);
    </script>
    <br/>
    <a href='{deepLinkUrl}' style='color:#2196F3; font-size:16px; margin-top:20px;'>Uygulamaya dön</a>
</body>
</html>";
            return Content(html, "text/html");
        }

        // Eski WebView yaklaşımı — URL değişikliği ile yakalanacak redirect
        var redirectUrl = success
            ? $"/payment/success?orderNumber={Uri.EscapeDataString(safeMessage)}&PaymentResult=true"
            : $"/payment/fail?error={Uri.EscapeDataString(safeMessage)}&PaymentResult=false";

        var webViewHtml = $@"
            <html>
            <body style='background:#f8f9fa; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; flex-direction:column;'>
                <h3 style='color:{(success ? "#10b981" : "#ef4444")}'>{(success ? "Ödeme Başarılı!" : "Ödeme Başarısız")}</h3>
                <p>{(success ? "Yönlendiriliyorsunuz..." : safeMessage)}</p>
                <script>
                    setTimeout(function() {{
                        window.location.href = '{redirectUrl}';
                    }}, 1500);
                </script>
            </body>
            </html>";
        return Content(webViewHtml, "text/html");
    }

    private async Task<IActionResult> FailAndCleanup(List<Orders>? orders, string message)
    {
        if (orders != null && orders.Any())
        {
            await _checkoutService.DeleteFailedOrders(orders.Select(x => x.OrderNumber).ToList());
        }
        return ReturnMobileResponse(false, message);
    }

    /// <summary>
    /// Mobil ödeme formu kaydet — checkout sonrası HTML form'u Redis'e kaydeder.
    /// Kısa ömürlü token döner, WebView bu token ile GET yaparak formu açar.
    /// [Authorize] ile korunur — sadece giriş yapmış kullanıcılar form kaydedebilir.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpPost("payment-form")]
    public async Task<IActionResult> StorePaymentForm([FromBody] StorePaymentFormRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.HtmlContent))
            return BadRequest("HTML içeriği boş olamaz.");

        var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
        // Tahmin edilemez tek kullanımlık token
        var formToken = Guid.NewGuid().ToString("N");
        var cacheKey = $"MobilePaymentForm_{formToken}";
        // 5 dakika TTL — kısa ömürlü, tek kullanımlık
        await redisService.SetAsync(cacheKey, request.HtmlContent, TimeSpan.FromMinutes(5));

        _logger.LogInformation("Mobil ödeme formu kaydedildi. Token: {Token}", formToken);
        return Ok(new { token = formToken });
    }

    /// <summary>
    /// Mobil ödeme formu getir — WebView bu endpoint'i GET ile açar.
    /// Token tek kullanımlık: ilk erişimde Redis'ten silinir.
    /// [AllowAnonymous] çünkü WebView JWT gönderemez, güvenlik token ile sağlanır.
    /// </summary>
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    [HttpGet("payment-form/{token}")]
    public async Task<IActionResult> GetPaymentForm(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return NotFound();

        var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
        var cacheKey = $"MobilePaymentForm_{token}";
        var htmlContent = await redisService.GetAsync<string>(cacheKey);

        if (string.IsNullOrEmpty(htmlContent))
        {
            _logger.LogWarning("Mobil ödeme formu bulunamadı veya süresi dolmuş. Token: {Token}", token);
            return Content("<html><body><h3>Form süresi dolmuş veya geçersiz.</h3></body></html>", "text/html");
        }

        // Tek kullanımlık — Redis'ten sil (replay attack önlemi)
        await redisService.RemoveAsync(cacheKey);

        // Banka'dan gelen HTML tam bir sayfa mı (<html> tag'i var mı) kontrol et
        var trimmed = htmlContent.TrimStart();
        var isFullHtmlPage = trimmed.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
                          || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase);

        string fullHtml;
        if (isFullHtmlPage)
        {
            // Tam HTML sayfası — olduğu gibi serve et, iç içe <html> oluşmasın
            // Sadece auto-submit script'i yoksa ekle
            var hasAutoSubmit = System.Text.RegularExpressions.Regex.IsMatch(
                htmlContent, @"\.submit\s*\(\s*\)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            
            if (!hasAutoSubmit)
            {
                // </body> veya </html> öncesine auto-submit script ekle
                var insertScript = @"<script>(function(){var f=document.getElementById('PaymentForm')||document.forms[0];if(f&&f.submit){setTimeout(function(){f.submit();},300);}})();</script>";
                if (htmlContent.Contains("</body>", StringComparison.OrdinalIgnoreCase))
                    fullHtml = htmlContent.Replace("</body>", insertScript + "</body>");
                else if (htmlContent.Contains("</html>", StringComparison.OrdinalIgnoreCase))
                    fullHtml = htmlContent.Replace("</html>", insertScript + "</html>");
                else
                    fullHtml = htmlContent + insertScript;
            }
            else
            {
                fullHtml = htmlContent;
            }
        }
        else
        {
            // Form snippet'i — tüm script tag'lerini kaldır (attribute'lu olanlar dahil) ve sarmalayıcı HTML oluştur
            var cleanedHtml = System.Text.RegularExpressions.Regex.Replace(
                htmlContent, @"<script[^>]*>[\s\S]*?</script>", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            fullHtml = $@"<!DOCTYPE html>
<html>
<head>
<meta charset=""utf-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<style>body{{background:#f8f9fa;display:flex;justify-content:center;align-items:center;height:100vh;font-family:sans-serif;}}p{{color:#666;font-size:16px;}}</style>
</head>
<body>
<p>Banka sayfasına yönlendiriliyorsunuz...</p>
{cleanedHtml}
<script>
(function(){{
  var f = document.getElementById('PaymentForm') || document.forms[0];
  if (f && f.submit) {{ setTimeout(function(){{ f.submit(); }}, 300); }}
}})();
</script>
</body>
</html>";
        }

        _logger.LogInformation("Mobil ödeme formu serve edildi. Token: {Token}", token);
        return Content(fullHtml, "text/html");
    }
    /// <summary>
    /// Plasiyer tahsilat 3D Secure callback'i — banka ödeme sonucunu bu endpoint'e POST eder.
    /// Sipariş oluşturmaz, sadece CustomerAccountTransaction (Credit) kaydı oluşturur.
    /// </summary>
    [HttpPost("payment-collection-callback")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public async Task<IActionResult> PaymentCollectionCallback(IFormCollection form)
    {
        _logger.LogInformation("=== Plasiyer Tahsilat Callback STARTED ({Time}) ===", DateTime.Now);

        try
        {
            // 1. Token'ı al — URL query veya POST body'den
            string? paymentToken = null;

            if (Request.Query.ContainsKey("token"))
                paymentToken = Request.Query["token"].ToString()?.Trim();

            if (string.IsNullOrEmpty(paymentToken) && form.ContainsKey("oid"))
                paymentToken = form["oid"].ToString()?.Trim();

            if (string.IsNullOrEmpty(paymentToken))
            {
                _logger.LogWarning("Plasiyer tahsilat callback: Token bulunamadı.");
                return ReturnMobileResponse(false, "Ödeme oturumu bulunamadı.");
            }

            // 2. Redis'ten tahsilat bilgilerini al
            var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
            var cacheKey = $"PaymentCollection_{paymentToken}";
            var cacheData = await redisService.GetAsync<ecommerce.Admin.Services.Concreate.PaymentCollectionCacheData>(cacheKey);

            if (cacheData == null)
            {
                _logger.LogWarning("Plasiyer tahsilat callback: Cache verisi bulunamadı. Token: {Token}", paymentToken);
                return ReturnMobileResponse(false, "Ödeme oturumu sona erdi veya geçersiz.");
            }

            // 3. Bank entity'sini çek
            var bankRepo = _uow.GetRepository<Bank>();
            var bank = await bankRepo.GetAll(predicate: b => b.Id == cacheData.BankId)
                .Include(b => b.Parameters)
                .FirstOrDefaultAsync();

            if (bank == null || !Enum.TryParse(bank.SystemName, out ecommerce.Core.Utils.BankNames bankNameEnum))
            {
                return ReturnMobileResponse(false, "Banka bilgisi geçersiz.");
            }

            // 4. Banka ile ödeme doğrulama
            var provider = _paymentProviderFactory.Create(bankNameEnum);
            var bankParams = bank.Parameters.ToDictionary(k => k.Key, v => v.Value);
            var verifyRequest = new VerifyGatewayRequest { BankParameters = bankParams };

            // Form verilerini kopyala ve eksik alanları enjekte et
            var modifiedForm = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            foreach (var key in form.Keys) modifiedForm[key] = form[key];

            if (!modifiedForm.ContainsKey("oid")) modifiedForm["oid"] = paymentToken;

            // Eksik parametreleri enjekte et
            if (!modifiedForm.ContainsKey("amount"))
                modifiedForm["amount"] = cacheData.TotalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            if (!modifiedForm.ContainsKey("currency")) modifiedForm["currency"] = "949";
            if (!modifiedForm.ContainsKey("installment")) modifiedForm["installment"] = cacheData.InstallmentCount.ToString();
            if (!modifiedForm.ContainsKey("clientId"))
            {
                var clientId = bankParams.FirstOrDefault(x => x.Key.Equals("ClientId", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("MerchantID", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(clientId)) modifiedForm["clientId"] = clientId;
            }
            if (!modifiedForm.ContainsKey("storeKey"))
            {
                var val = bankParams.FirstOrDefault(x => x.Key.Equals("storeKey", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(val)) modifiedForm["storeKey"] = val;
            }
            if (!modifiedForm.ContainsKey("storetype"))
            {
                var val = bankParams.FirstOrDefault(x => x.Key.Equals("storeType", StringComparison.OrdinalIgnoreCase)).Value;
                modifiedForm["storetype"] = val ?? "3D_PAY";
            }

            // Callback URL'leri enjekte et
            var apiBaseUrl = _configuration["AppSettings:ApiBaseUrl"] ?? _configuration["AppSettings:AdminBaseUrl"];
            if (string.IsNullOrEmpty(apiBaseUrl))
                apiBaseUrl = $"{Request.Scheme}://{Request.Host}";
            var callbackUrl = $"{apiBaseUrl.TrimEnd('/')}/api/Cart/payment-collection-callback?token={paymentToken}";
            if (!modifiedForm.ContainsKey("okUrl")) modifiedForm["okUrl"] = callbackUrl;
            if (!modifiedForm.ContainsKey("failUrl")) modifiedForm["failUrl"] = callbackUrl;

            // Pre-validation: mdStatus kontrolü
            var mdStatus = modifiedForm.ContainsKey("mdStatus") ? modifiedForm["mdStatus"].ToString() : "";
            var errMsg = modifiedForm.ContainsKey("ErrMsg") ? modifiedForm["ErrMsg"].ToString() : "";

            if (string.IsNullOrEmpty(mdStatus))
            {
                var detail = !string.IsNullOrEmpty(errMsg) ? errMsg : "Banka eksik yanıt döndü.";
                await redisService.RemoveAsync(cacheKey);
                return ReturnMobileResponse(false, $"3D doğrulama başarısız: {detail}");
            }
            if (mdStatus != "1")
            {
                var failReason = !string.IsNullOrEmpty(errMsg) ? errMsg : "3D doğrulama başarısız";
                await redisService.RemoveAsync(cacheKey);
                return ReturnMobileResponse(false, $"3D doğrulama reddedildi (mdStatus={mdStatus}): {failReason}");
            }

            var procReturnCode = modifiedForm.ContainsKey("procReturnCode") ? modifiedForm["procReturnCode"].ToString() : "";
            if (!string.IsNullOrEmpty(procReturnCode) && procReturnCode != "00")
            {
                var failReason = !string.IsNullOrEmpty(errMsg) ? errMsg : "Ödeme işlemi reddedildi";
                await redisService.RemoveAsync(cacheKey);
                return ReturnMobileResponse(false, $"Ödeme başarısız (Kod: {procReturnCode}): {failReason}");
            }

            var formCollection = new FormCollection(modifiedForm);
            var verifyResult = await provider.VerifyGateway(verifyRequest, null!, formCollection);

            if (!verifyResult.Success)
            {
                await redisService.RemoveAsync(cacheKey);
                return ReturnMobileResponse(false, verifyResult.ErrorMessage ?? "Ödeme doğrulaması başarısız.");
            }

            // 5. Ödeme başarılı — CustomerAccountTransaction oluştur
            _logger.LogInformation("Plasiyer tahsilat 3D Secure doğrulandı. Token: {Token}, CustomerId: {CustomerId}, Amount: {Amount}",
                paymentToken, cacheData.CustomerId, cacheData.Amount);

            var transactionService = _serviceProvider.GetRequiredService<ecommerce.Admin.Services.Interfaces.ICustomerAccountTransactionService>();
            var referenceNo = verifyResult.TransactionId ?? verifyResult.ReferenceNumber ?? paymentToken;

            if (cacheData.OrderIds != null && cacheData.OrderIds.Any())
            {
                // Sipariş bağlamı var — her sipariş için ayrı transaction
                var orderRepo = _uow.GetRepository<Orders>();
                foreach (var orderId in cacheData.OrderIds)
                {
                    var order = await orderRepo.GetAll(predicate: o => o.Id == orderId, disableTracking: true)
                        .Select(o => new { o.OrderNumber, o.GrandTotal })
                        .FirstOrDefaultAsync();

                    var orderNumber = order?.OrderNumber ?? orderId.ToString();
                    var orderAmount = order?.GrandTotal > 0 ? order.GrandTotal : cacheData.Amount / cacheData.OrderIds.Count;

                    var txDto = new ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto.CustomerAccountTransactionUpsertDto
                    {
                        CustomerId = cacheData.CustomerId,
                        OrderId = orderId,
                        TransactionType = ecommerce.Core.Utils.CustomerAccountTransactionType.Credit,
                        Amount = orderAmount,
                        TransactionDate = DateTime.Now,
                        Description = $"Plasiyer kredi kartı tahsilat (3D Secure) — Sipariş: {orderNumber}",
                        PaymentTypeId = ecommerce.Core.Utils.PaymentType.CreditCart,
                        CashRegisterId = null,
                        ReferenceNo = referenceNo
                    };

                    var auditWrap = new ecommerce.Core.Helpers.AuditWrapDto<ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto.CustomerAccountTransactionUpsertDto>
                    {
                        UserId = cacheData.UserId,
                        Dto = txDto
                    };

                    var txResult = await transactionService.CreateTransaction(auditWrap);
                    if (!txResult.Ok)
                    {
                        _logger.LogError("Plasiyer tahsilat transaction oluşturulamadı. OrderId: {OrderId}, Hata: {Error}",
                            orderId, txResult.Metadata?.Message);
                    }
                }
            }
            else
            {
                // Sipariş bağlamı yok — serbest ödeme
                var txDto = new ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto.CustomerAccountTransactionUpsertDto
                {
                    CustomerId = cacheData.CustomerId,
                    OrderId = null,
                    TransactionType = ecommerce.Core.Utils.CustomerAccountTransactionType.Credit,
                    Amount = cacheData.Amount,
                    TransactionDate = DateTime.Now,
                    Description = $"Plasiyer kredi kartı tahsilat (3D Secure) — Serbest ödeme",
                    PaymentTypeId = ecommerce.Core.Utils.PaymentType.CreditCart,
                    CashRegisterId = null,
                    ReferenceNo = referenceNo
                };

                var auditWrap = new ecommerce.Core.Helpers.AuditWrapDto<ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto.CustomerAccountTransactionUpsertDto>
                {
                    UserId = cacheData.UserId,
                    Dto = txDto
                };

                var txResult = await transactionService.CreateTransaction(auditWrap);
                if (!txResult.Ok)
                {
                    _logger.LogError("Plasiyer tahsilat serbest ödeme transaction oluşturulamadı. Hata: {Error}",
                        txResult.Metadata?.Message);
                    await redisService.RemoveAsync(cacheKey);
                    return ReturnMobileResponse(false, "Ödeme doğrulandı ancak kayıt oluşturulamadı.");
                }
            }

            // 6. Redis temizle
            await redisService.RemoveAsync(cacheKey);

            _logger.LogInformation("Plasiyer tahsilat başarılı. Token: {Token}, CustomerId: {CustomerId}, Amount: {Amount}",
                paymentToken, cacheData.CustomerId, cacheData.Amount);

            return ReturnMobileResponse(true, "Ödeme başarıyla tamamlandı.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plasiyer tahsilat callback hatası");
            return ReturnMobileResponse(false, "Sistem hatası: " + ex.Message);
        }
    }

}

/// <summary>
/// Mobil ödeme formu kaydetme isteği
/// </summary>
public class StorePaymentFormRequest
{
    public string? HtmlContent { get; set; }
}
