using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.EFCore.Context;
using ecommerce.EFCore.UnitOfWork;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using ecommerce.Virtual.Pos.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection; // Required for GetRequiredService
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Domain.Shared.Emailing;

// Force Restart
namespace ecommerce.Web.Controllers;

[Route("checkout")]
public class PaymentCallbackController : Controller
{
    private readonly IPaymentProviderFactory _paymentProviderFactory;
    private readonly IUnitOfWork<ApplicationDbContext> _uow;
    private readonly IServiceProvider _serviceProvider;
    private readonly ICheckoutService _checkoutService;
    private readonly IEmailService _emailService;

    public PaymentCallbackController(IPaymentProviderFactory paymentProviderFactory, IUnitOfWork<ApplicationDbContext> uow, IServiceProvider serviceProvider, ICheckoutService checkoutService, IEmailService emailService)
    {
        _paymentProviderFactory = paymentProviderFactory;
        _uow = uow;
        _serviceProvider = serviceProvider;
        _checkoutService = checkoutService;
        _emailService = emailService;
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback(IFormCollection form)
    {
        try
        {
            // Senior Solution: Use User ID to get PaymentToken from Redis
            // This is reliable, bypasses URL stripping, and avoids database scanning
            
            string paymentToken = "";
            List<Orders> orders = null;
            
            var redisService = _serviceProvider.GetRequiredService<ecommerce.Domain.Shared.Abstract.IRedisCacheService>();
            string cacheKey = null;

            // 1. Try to recover session via User ID (Preferred)
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdStr))
            {
                cacheKey = $"PendingPayment_{userIdStr}";
                paymentToken = await redisService.GetAsync<string>(cacheKey);
            }

            var orderRepo = _uow.GetRepository<Orders>();

            // 2. If Session/Redis failed, Fallback to Token in URL or POST Body
            // The 'token' in URL (and 'oid' in POST) corresponds to the 'PaymentToken' field in Orders table.
            if (string.IsNullOrEmpty(paymentToken))
            {
                string tokenToSearch = null;
                
                if (Request.Query.ContainsKey("token"))
                {
                    tokenToSearch = Request.Query["token"].ToString();
                }
                
                // Fallback to POST body 'oid' if URL token is missing
                if (string.IsNullOrEmpty(tokenToSearch) && form.ContainsKey("oid"))
                {
                    tokenToSearch = form["oid"].ToString();
                }

                if (!string.IsNullOrEmpty(tokenToSearch))
                {
                     // CRITICAL: Set paymentToken BEFORE searching
                     // This ensures we can verify payment even if order is still being created
                     paymentToken = tokenToSearch;
                     
                     // Search by PaymentToken (which maps to 'oid' sent to bank)
                     orders = await orderRepo.GetAll(predicate: o => o.PaymentToken == tokenToSearch)
                                 .Include(o => o.Bank).ThenInclude(b => b.Parameters)
                                 .ToListAsync();
                }
            }
            else 
            {
                // Session verify success
                orders = await orderRepo.GetAll(predicate: o => o.PaymentToken == paymentToken)
                    .Include(o => o.Bank).ThenInclude(b => b.Parameters)
                    .ToListAsync();
            }

            // 3. Final Check
            if (orders == null || !orders.Any())
            {
                 // Only return error if BOTH methods failed
                 return ReturnModalResponse(false, "Order not found or payment session expired. Please check order history.");
            }

            var firstOrder = orders.First();
            if (firstOrder.Bank == null || !Enum.TryParse(firstOrder.Bank.SystemName, out ecommerce.Core.Utils.BankNames bankNameEnum))
            {
                return ReturnModalResponse(false, "Bank information invalid");
            }

            // 4. Verify Payment with Bank
            var provider = _paymentProviderFactory.Create(bankNameEnum);
            var bankParams = firstOrder.Bank.Parameters.ToDictionary(k => k.Key, v => v.Value);

            var verifyRequest = new VerifyGatewayRequest
            {
                BankParameters = bankParams
            };
            
            // Important: We inject the known 'oid' (PaymentToken) into the form data 
            // so the library can calculate the HASH correctly if it needs it.
            var modifiedForm = new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>();
            foreach(var key in form.Keys) modifiedForm[key] = form[key];
            
            // DEBUG: Log RAW form from bank BEFORE any injection
            try 
            {
                var debugPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                var sbRaw = new System.Text.StringBuilder();
                sbRaw.AppendLine($"\n--- RAW BANK CALLBACK ({DateTime.Now}) ---");
                foreach (var k in form.Keys)
                {
                    sbRaw.AppendLine($"{k}: {form[k]}");
                }
                sbRaw.AppendLine("--------------------------\n");
                System.IO.File.AppendAllText(debugPath, sbRaw.ToString());
            } catch {}
            
            if (!modifiedForm.ContainsKey("oid")) modifiedForm["oid"] = paymentToken;
            
            // --- GENERIC HIDDEN INPUT SCRAPER ---
            // Senior Fix: Scrape ALL hidden inputs to ensure we don't miss mdStatus, procReturnCode, etc.
            if (modifiedForm.ContainsKey("HTMLContent"))
            {
                var htmlContent = modifiedForm["HTMLContent"].ToString();
                // Regex to find name and value attributes in input tags
                var matches = System.Text.RegularExpressions.Regex.Matches(htmlContent, 
                    @"<input[^>]*name=[""']?([^""'\s>]+)[""']?[^>]*value=[""']?([^""']*)[""']?", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (match.Groups.Count >= 3)
                    {
                        var key = match.Groups[1].Value;
                        var val = match.Groups[2].Value;
                        // Only add if not already present (Form data takes precedence if valid)
                        if (!modifiedForm.ContainsKey(key))
                        {
                            modifiedForm[key] = val;
                        }
                    }
                }
            }
            
            // SENIOR FIX: Inject Missing Transaction Parameters for Hash Verification
            // 3D Secure Hash verification often requires the original transaction Amount, Currency, and Installment
            // If the bank does not return them in the callback, we must supply them from the trusted Order record.
            if (!modifiedForm.ContainsKey("amount"))
            {
                 var totalAmount = orders.Sum(x => x.GrandTotal);
                 modifiedForm["amount"] = totalAmount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture); 
            }
            if (!modifiedForm.ContainsKey("currency"))
            {
                 // Default to TRY (949) if not stored. Ideally store currency code in Order.
                 modifiedForm["currency"] = "949"; 
            }
            if (!modifiedForm.ContainsKey("installment"))
            {
                 modifiedForm["installment"] = "1"; // Default single shot
                 // If order has installment info, use it.
            }

            
            // Inject ClientId if missing (Required for Hash)
            if (!modifiedForm.ContainsKey("clientId"))
            {
                var clientId = bankParams.FirstOrDefault(x => x.Key.Equals("ClientId", StringComparison.OrdinalIgnoreCase) || x.Key.Equals("MerchantID", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(clientId))
                {
                    modifiedForm["clientId"] = clientId;
                }
            }
            
            // Inject StoreKey (Some libraries require it in the dictionary for hash calculation)
            if (!modifiedForm.ContainsKey("storeKey"))
            {
                var val = bankParams.FirstOrDefault(x => x.Key.Equals("storeKey", StringComparison.OrdinalIgnoreCase)).Value;
                if (!string.IsNullOrEmpty(val)) modifiedForm["storeKey"] = val;
            }
            
            // Inject StoreType
             if (!modifiedForm.ContainsKey("storetype"))
            {
                 var val = bankParams.FirstOrDefault(x => x.Key.Equals("storeType", StringComparison.OrdinalIgnoreCase)).Value;
                 modifiedForm["storetype"] = val ?? "3D_PAY";
            }

            // Inject OkUrl/FailUrl (Library might need them)
            var constructedCallbackUrl = $"https://yedeksen.com/checkout/callback?token={paymentToken}";
            if (!modifiedForm.ContainsKey("okUrl")) modifiedForm["okUrl"] = constructedCallbackUrl;
            if (!modifiedForm.ContainsKey("failUrl")) modifiedForm["failUrl"] = constructedCallbackUrl;
            
            // --- DEBUG LOGGING START ---
            try 
            {
                var debugPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                var sbLog = new System.Text.StringBuilder();
                sbLog.AppendLine($"\n--- CALLBACK RECEIVED ({DateTime.Now}) ---");
                foreach (var k in modifiedForm.Keys)
                {
                    sbLog.AppendLine($"{k}: {modifiedForm[k]}");
                }
                sbLog.AppendLine("--------------------------\n");
                System.IO.File.AppendAllText(debugPath, sbLog.ToString());
            } catch {}
            // --- DEBUG LOGGING END ---

            // --- PRE-VALIDATION: Check for Complete Response ---
            // If bank returns only HASH/rnd without success indicators, it means the payment FAILED
            // CP.VPOS library cannot handle this and throws generic errors
            // We detect this early and provide clear feedback
            
            var mdStatus = modifiedForm.ContainsKey("mdStatus") ? modifiedForm["mdStatus"].ToString() : "";
            var procReturnCode = modifiedForm.ContainsKey("procReturnCode") ? modifiedForm["procReturnCode"].ToString() : "";
            var response = modifiedForm.ContainsKey("Response") ? modifiedForm["Response"].ToString() : "";
            var errMsg = modifiedForm.ContainsKey("ErrMsg") ? modifiedForm["ErrMsg"].ToString() : "";
            
            // CRITICAL: If mdStatus is missing or not "1" (Success), the 3D Auth FAILED
            if (string.IsNullOrEmpty(mdStatus))
            {
                var detail = !string.IsNullOrEmpty(errMsg) ? errMsg : (!string.IsNullOrEmpty(response) ? response : "Banka eksik yanıt döndü.");
                return await FailAndCleanup(orders, $"3D doğrulama başarısız. Banka yanıtı eksik (mdStatus yok). Detay: {detail}");
            }
            
            if (mdStatus != "1")
            {
                var failReason = !string.IsNullOrEmpty(errMsg) ? errMsg : "3D doğrulama başarısız";
                return await FailAndCleanup(orders, $"3D doğrulama reddedildi (mdStatus={mdStatus}). Sebep: {failReason}");
            }
            
            // If procReturnCode exists and is not "00" (Success), payment FAILED
            if (!string.IsNullOrEmpty(procReturnCode) && procReturnCode != "00")
            {
                var failReason = !string.IsNullOrEmpty(errMsg) ? errMsg : "Ödeme işlemi reddedildi";
                return await FailAndCleanup(orders, $"Ödeme başarısız (Kod: {procReturnCode}). Sebep: {failReason}");
            }

            var formCollection = new FormCollection(modifiedForm);

            var result = await provider.VerifyGateway(verifyRequest, null, formCollection);
            
            if (result.Success)
            {
                // Payment Successful
                foreach (var order in orders)
                {
                    // DETACH BANK to prevent "Identity Resolution" conflicts on Update
                    // We only want to update the Order logic here.
                    order.Bank = null; 
                    
                    order.PaymentStatus = true;
                    order.PaymentId = result.TransactionId ?? result.ReferenceNumber ?? Guid.NewGuid().ToString("N");
                    order.OrderStatusType = ecommerce.Core.Utils.OrderStatusType.OrderNew;
                    orderRepo.Update(order);
                }
                
                // CLEAR USER CART
                if (int.TryParse(userIdStr, out var uId))
                {
                    try
                    {
                        var cartRepo = _uow.GetRepository<ecommerce.Core.Entities.CartItem>();
                        var cartItems = cartRepo.GetAll(predicate: x => x.UserId == uId).ToList();
                        if (cartItems.Any())
                        {
                            foreach(var item in cartItems) cartRepo.Delete(item);

                        }
                    }
                    catch (Exception)
                    {
                         // Fail silently
                    }
                }

                await _uow.SaveChangesAsync();
                
                // Send order confirmation email to customer
                try
                {
                    var orderNumbers = orders.Select(o => o.OrderNumber).ToList();
                    var orderListWithDetails = await orderRepo.GetAll(
                        predicate: o => orderNumbers.Contains(o.OrderNumber))
                        .Include(o => o.User)
                        .Include(o => o.UserAddress)
                            .ThenInclude(a => a.City)
                        .Include(o => o.Seller)
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.ProductImages)
                        .ToListAsync();
                    
                    if (orderListWithDetails != null && orderListWithDetails.Any())
                    {
                        await _emailService.SendOrderPlacedCustomerEmail(orderListWithDetails);

                        // Trigger Seller Emails
                        foreach (var oDetail in orderListWithDetails)
                        {
                            await _emailService.SendOrderPlacedSellerEmail(oDetail);
                        }
                    }
                }
                catch (Exception emailEx)
                {
                    // Log email error but don't fail the payment
                    Console.WriteLine($"Email send error: {emailEx.Message}");
                }
                
                // Clear Redis Key
                if (string.IsNullOrEmpty(cacheKey))
                {
                    // Try to recover User ID from order to clear the pending payment key
                    var oUserId = orders.FirstOrDefault()?.CompanyId; // CompanyId is FK to User
                    if (oUserId != null && oUserId > 0)
                    {
                         cacheKey = $"PendingPayment_{oUserId}";
                    }
                }

                if (!string.IsNullOrEmpty(cacheKey))
                {
                    await redisService.RemoveAsync(cacheKey);
                }

                var firstSuccessOrderNumber = orders.FirstOrDefault()?.OrderNumber;
                // MODAL SUCCESS RESPONSE
                return ReturnModalResponse(true, firstSuccessOrderNumber ?? "0");
            }
            else
            {
                 // Payment Failed
                  return await FailAndCleanup(orders, result.ErrorMessage ?? "Payment verification failed");
            }
        }
        catch (Exception ex)
        {
             var errorDetails = $"Error: {ex.Message} | StackTrace: {ex.StackTrace?.Substring(0, Math.Min(200, ex.StackTrace?.Length ?? 0))}";
             Console.WriteLine($"Payment Error: {errorDetails}");
             return ReturnModalResponse(false, "Sistem hatası oluştu: " + ex.Message);
        }
    }

    private IActionResult ReturnModalResponse(bool success, string message)
    {
        var status = success ? "success" : "error";
        // Escape message for JS
        var safeMessage = message.Replace("'", "\\'").Replace("\n", " ");
        
        var script = $@"
            <html>
            <body style='background:#f8f9fa; display:flex; justify-content:center; align-items:center; height:100vh; font-family:sans-serif; flex-direction:column;'>
                <h3 style='color:{(success ? "green" : "red")}'>{(success ? "Ödeme Başarılı!" : "Ödeme Başarısız")}</h3>
                <p>Yönlendiriliyorsunuz...</p>
                <script>
                    setTimeout(function() {{
                        window.parent.postMessage({{ type: 'paymentResult', status: '{status}', message: '{safeMessage}' }}, '*');
                    }}, 1000);
                </script>
            </body>
            </html>";
        return Content(script, "text/html");
    }

    private async Task<IActionResult> FailAndCleanup(List<ecommerce.Core.Entities.Orders>? orders, string message)
    {
        if (orders != null && orders.Any())
        {
            await _checkoutService.DeleteFailedOrders(orders.Select(x => x.OrderNumber).ToList());
        }
        return ReturnModalResponse(false, message);
    }

    // Helper for early failures
    private IActionResult Fail(string message) => ReturnModalResponse(false, message);
}
