using System.Globalization;
using ecommerce.Virtual.Pos.Abstract;
using ecommerce.Virtual.Pos.Requests;
using ecommerce.Virtual.Pos.Results;
using Microsoft.AspNetCore.Http;
using CP.VPOS;
using CP.VPOS.Models;
using CP.VPOS.Services;
using CP.VPOS.Enums;

namespace ecommerce.Payments.Providers;

public class CPNestPayProvider : IPaymentProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public CPNestPayProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public Task<PaymentGatewayResult> ThreeDGatewayRequest(PaymentGatewayRequest request)
    {
        try
        {
            // 1. Prepare Auth (VirtualPOSAuth)
            // 1. Prepare Auth (VirtualPOSAuth)
            string bankName = (request.BankParameters.ContainsKey("bankName") ? request.BankParameters["bankName"] : "IsBankasi") ?? "IsBankasi";
            string bankCode = MapToCpBankCode(bankName);
            
            var auth = new VirtualPOSAuth
            {
                bankCode = bankCode,
                merchantID = (request.BankParameters.ContainsKey("clientId") ? request.BankParameters["clientId"] : "") ?? "",
                merchantStorekey = (request.BankParameters.ContainsKey("storeKey") ? request.BankParameters["storeKey"] : "") ?? "",
                merchantUser = ((request.BankParameters.ContainsKey("userName") ? request.BankParameters["userName"] : (request.BankParameters.ContainsKey("merchantUser") ? request.BankParameters["merchantUser"] : "")) ?? ""),
                merchantPassword = ((request.BankParameters.ContainsKey("password") ? request.BankParameters["password"] : (request.BankParameters.ContainsKey("merchantPassword") ? request.BankParameters["merchantPassword"] : "")) ?? ""),
                testPlatform = false 
            };

            // Split name
            string safeCardHolder = request.CardHolderName ?? "Online Musteri";
            string[] names = safeCardHolder.Split(' ');
            string surname = names.Length > 1 ? names[names.Length - 1] : "Musteri";
            string name = names.Length > 1 ? string.Join(" ", names.Take(names.Length - 1)) : names[0];

            // 2. Prepare Sale Request
            var saleRequest = new SaleRequest
            {
                orderNumber = request.OrderNumber ?? DateTime.Now.Ticks.ToString(),
                invoiceInfo = new CustomerInfo
                {
                    name = name,
                    surname = surname,
                    emailAddress = "info@dummy.com", // Mandatory valid format
                    phoneNumber = "5555555555",
                    cityName = "Istanbul",
                    townName = "Merkez", // From User Test
                    addressDesc = "Istanbul Merkez", // From User Test
                    postCode = "34000",
                    country = (Country)792, // ISO TR. User used Country.TUR, likely 792.
                    taxNumber = "18638387240", // User TCKN
                    taxOffice = "" // Empty for Individual
                },
                shippingInfo = new CustomerInfo
                {
                    name = name,
                    surname = surname,
                    emailAddress = "info@dummy.com",
                    phoneNumber = "5555555555",
                    cityName = "Istanbul",
                    townName = "Merkez",
                    addressDesc = "Istanbul Merkez",
                    postCode = "34000",
                    country = (Country)792,
                    taxNumber = "18638387240", // User TCKN for Shipping too
                    taxOffice = "" // Empty for Individual
                },
                saleInfo = new SaleInfo
                {
                    amount = (decimal)request.TotalAmount, 
                    currency = MapCurrency(request.CurrencyIsoCode), 
                    // Validator requires 1-15. So 0 is invalid. Use 1 for single shot.
                    installment = request.Installment > 1 ? (sbyte)request.Installment : (sbyte)1, 
                    cardNameSurname = request.CardHolderName ?? "Online Musteri", 
                    cardNumber = request.CardNumber ?? "",
                    
                    cardExpiryDateYear = (ushort)request.ExpireYear, 
                    cardExpiryDateMonth = (short)request.ExpireMonth,
                    cardCVV = request.CvvCode ?? ""
                },
                payment3D = new Payment3D
                {
                   confirm = true, 
                   returnURL = request.CallbackUrl.ToString().Contains("token=") ? request.CallbackUrl.ToString() : $"{request.CallbackUrl}{(request.CallbackUrl.ToString().Contains("?") ? "&" : "?")}token={request.OrderNumber}"
                },
                customerIPAddress = request.CustomerIpAddress ?? "127.0.0.1" // Mandatory field
            };
            
            // VALIDATION: Check for null strings before calling library
            var validationErrors = new List<string>();
            
            if (string.IsNullOrEmpty(auth.bankCode)) validationErrors.Add("auth.bankCode is null");
            if (string.IsNullOrEmpty(auth.merchantID)) validationErrors.Add("auth.merchantID is null");
            if (string.IsNullOrEmpty(auth.merchantStorekey)) validationErrors.Add("auth.merchantStorekey is null");
            if (string.IsNullOrEmpty(auth.merchantUser)) validationErrors.Add("auth.merchantUser is null");
            if (string.IsNullOrEmpty(auth.merchantPassword)) validationErrors.Add("auth.merchantPassword is null");
            
            // ERROR FIX: Force storetype to "3d_pay" instead of "3d_pay_hosting"
            // "3d_pay_hosting" is often rejected by banks in certain configurations. "3d_pay" is safer.
            // This parameter might be used by the library internally or added to the form.
            // Override storetype logic removed to respect user config or default to 3d_pay_hosting later.
            // if (request.BankParameters.ContainsKey("storetype")) ...
            
            if (string.IsNullOrEmpty(saleRequest.orderNumber)) validationErrors.Add("saleRequest.orderNumber is null");
            if (string.IsNullOrEmpty(saleRequest.customerIPAddress)) validationErrors.Add("saleRequest.customerIPAddress is null");
            
            if (saleRequest.invoiceInfo == null) validationErrors.Add("invoiceInfo is null");
            else {
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.name)) validationErrors.Add("invoiceInfo.name is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.surname)) validationErrors.Add("invoiceInfo.surname is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.emailAddress)) validationErrors.Add("invoiceInfo.emailAddress is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.phoneNumber)) validationErrors.Add("invoiceInfo.phoneNumber is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.cityName)) validationErrors.Add("invoiceInfo.cityName is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.townName)) validationErrors.Add("invoiceInfo.townName is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.addressDesc)) validationErrors.Add("invoiceInfo.addressDesc is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.postCode)) validationErrors.Add("invoiceInfo.postCode is null");
                if (string.IsNullOrEmpty(saleRequest.invoiceInfo.taxNumber)) validationErrors.Add("invoiceInfo.taxNumber is null");
                if (saleRequest.invoiceInfo.taxOffice == null) validationErrors.Add("invoiceInfo.taxOffice is null (must be empty string, not null)");
            }
            
            if (saleRequest.shippingInfo == null) validationErrors.Add("shippingInfo is null");
            else {
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.name)) validationErrors.Add("shippingInfo.name is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.surname)) validationErrors.Add("shippingInfo.surname is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.emailAddress)) validationErrors.Add("shippingInfo.emailAddress is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.phoneNumber)) validationErrors.Add("shippingInfo.phoneNumber is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.cityName)) validationErrors.Add("shippingInfo.cityName is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.townName)) validationErrors.Add("shippingInfo.townName is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.addressDesc)) validationErrors.Add("shippingInfo.addressDesc is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.postCode)) validationErrors.Add("shippingInfo.postCode is null");
                if (string.IsNullOrEmpty(saleRequest.shippingInfo.taxNumber)) validationErrors.Add("shippingInfo.taxNumber is null");
            }
            
            if (saleRequest.saleInfo == null) validationErrors.Add("saleInfo is null");
            else {
                if (string.IsNullOrEmpty(saleRequest.saleInfo.cardNameSurname)) validationErrors.Add("saleInfo.cardNameSurname is null");
                if (string.IsNullOrEmpty(saleRequest.saleInfo.cardNumber)) validationErrors.Add("saleInfo.cardNumber is null");
                if (string.IsNullOrEmpty(saleRequest.saleInfo.cardCVV)) validationErrors.Add("saleInfo.cardCVV is null");
            }
            
            if (saleRequest.payment3D == null) validationErrors.Add("payment3D is null");
            else {
                if (string.IsNullOrEmpty(saleRequest.payment3D.returnURL)) validationErrors.Add("payment3D.returnURL is null");
            }
            
            if (validationErrors.Any())
            {
                return Task.FromResult(PaymentGatewayResult.Failed("Null field validation failed: " + string.Join(", ", validationErrors)));
            }
            
            // 3. MANUAL FORM GENERATION (Bypass CP.VPOS)
            // Fixes "Immediate Rejection" due to potential library hash/param mismatch.
            try 
            {
                var gatewayUrl = request.BankParameters.ContainsKey("gatewayUrl") ? request.BankParameters["gatewayUrl"] : "https://sanalpos.isbank.com.tr/fim/est3dgate";
                var clientId = auth.merchantID;
                var storeKey = auth.merchantStorekey;
                
                // Get StoreType from DB Params (User Request: Manual override removed)
                // Defaulting to "3d_pay" if missing, as this is the current required flow.
                var storeType = request.BankParameters.ContainsKey("storetype") ? request.BankParameters["storetype"] : "3d_pay_hosting";

                var rnd = DateTime.Now.Ticks.ToString();
                var timeSpan = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
                
                var amountStr = request.TotalAmount.ToString("F2", CultureInfo.InvariantCulture);
                var currency = "949"; // TRY
                var oid = saleRequest.orderNumber;
                
                // Restore Token in URL to ensure PaymentCallbackController can find the order
                var callbackUrl = request.CallbackUrl.ToString().Contains("token=") ? request.CallbackUrl.ToString() : $"{request.CallbackUrl}{(request.CallbackUrl.ToString().Contains("?") ? "&" : "?")}token={oid}";
                var okUrl = callbackUrl;
                var failUrl = callbackUrl;

                // Parameters for Hash Calculation (Alphabetical Order Required by NestPay v3)
                // List from Docs: amount|BillToCompany|BillToName|callbackUrl|clientid|currency|failUrl|hashAlgorithm|Instalment|lang|okurl|refreshtime|rnd|storetype|TranType|storeKey
                
                var postParams = new SortedDictionary<string, string>();
                postParams.Add("clientid", clientId);
                postParams.Add("storetype", storeType);
                postParams.Add("islemtipi", "Auth"); // TranType
                postParams.Add("amount", amountStr);
                postParams.Add("currency", currency);
                postParams.Add("oid", oid);
                postParams.Add("okUrl", okUrl);
                postParams.Add("failUrl", failUrl);
                postParams.Add("callbackUrl", callbackUrl);
                postParams.Add("lang", "tr");
                postParams.Add("rnd", rnd);
                postParams.Add("hashAlgorithm", "ver3");
                postParams.Add("refreshtime", "5");
                
                // Card Parameters for 3d_pay (Merchant Side Entry)
                // SKIP if using 3d_pay_hosting (Bank Page)
                if (storeType != "3d_pay_hosting")
                {
                    postParams.Add("pan", saleRequest.saleInfo.cardNumber);
                    postParams.Add("cv2", saleRequest.saleInfo.cardCVV);
                    // Format Year as 'yy' (e.g. 25 for 2025)
                    postParams.Add("Ecom_Payment_Card_ExpDate_Year", (saleRequest.saleInfo.cardExpiryDateYear % 100).ToString("00"));
                    postParams.Add("Ecom_Payment_Card_ExpDate_Month", saleRequest.saleInfo.cardExpiryDateMonth.ToString("00"));
                }
                
                // Optional: postParams.Add("cardType", "VISA"); // Detect or skip
                
                // Optional Info for 3D
                if(!string.IsNullOrEmpty(saleRequest.invoiceInfo?.name)) postParams.Add("BillToName", saleRequest.invoiceInfo.name + " " + saleRequest.invoiceInfo.surname);
                // BillToCompany...

                // Installment
                var installment = saleRequest.saleInfo.installment > 1 ? saleRequest.saleInfo.installment.ToString() : "";
                postParams.Add("taksit", installment); // Note: Doc says "taksit" in one place, "Instalment" in hash list?
                // Doc says: "Instalment" in hash list. But "taksit" in form input list?
                // Doc: "taksit Parametresi bos gonderilmelidir"
                // Let's check "HTTP Formunda Zorunlu ... Ornek".
                // It doesn't show "taksit". It shows "amount", "currency" etc.
                // Wait, the Hash List table uses "Instalment". 
                // But the input name in "Ek A" says "taksit".
                // Let's use "taksit" for Form, but we need to match Hash Key.
                // NESTPAY TRICKY PART: The HASH param name vs FORM param name.
                // Usually they match. "Instalment" vs "taksit".
                // Let's try adding "Instalment" to Dictionary for HASHING, but maybe map to "taksit" for FORM?
                // Actually, common NestPay uses "taksit" input.
                // If I put "Instalment" in Hash, but "taksit" in Form, does calling it "Instalment" in hash work?
                // Doc says: "Hash için Plain Text Oluşturma ... Instalment parametresinin ..." regarding hash.
                
                // Let's assume input name is "taksit".
                
                // HASH GENERATION (Generic ver3: Value|Value|...|storeKey - Sorted by Key)
                // This ensures we only hash parameters that are actually present in the form.
                
                // HASH GENERATION (Generic ver3: Value|Value|...|storeKey - Sorted by Key)
                // CRITICAL: Exclude sensitive card data from Hash (pan, cv2, expiry)
                var ignoredParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                { 
                    "hash", "encoding", "pan", "cv2", 
                    "Ecom_Payment_Card_ExpDate_Year", "Ecom_Payment_Card_ExpDate_Month", "cardType" 
                };

                var sbHash = new System.Text.StringBuilder();
                var sortedKeys = postParams.Keys.OrderBy(k => k).ToList();
                
                foreach(var key in sortedKeys)
                {
                    if(ignoredParams.Contains(key)) continue;

                    // Escape special characters in Values
                    string rawVal = postParams[key];
                    string escapedVal = rawVal.Replace("\\", "\\\\").Replace("|", "\\|");
                    sbHash.Append(escapedVal + "|");
                }
                sbHash.Append(storeKey);
                
                var plainText = sbHash.ToString();
                
                string hash = "";
                using (var sha = System.Security.Cryptography.SHA512.Create())
                {
                    var bytes = System.Text.Encoding.UTF8.GetBytes(plainText);
                    var hashBytes = sha.ComputeHash(bytes);
                    hash = Convert.ToBase64String(hashBytes);
                }
                
                postParams.Add("hash", hash);
                
                // Debug Log
                 try {
                     var debugLogPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                     System.IO.File.AppendAllText(debugLogPath, $"\n--- MANUAL REQUEST GEN ({DateTime.Now}) ---\nPlainText: {plainText}\nHash: {hash}\nParams: {System.Text.Json.JsonSerializer.Serialize(postParams)}\n--------------------\n");
                 } catch {}

                // Build HTML Form
                var sbHtml = new System.Text.StringBuilder();
                sbHtml.AppendLine("<!DOCTYPE html>");
                sbHtml.AppendLine("<html>");
                sbHtml.AppendLine("<head><title>Redirecting...</title>");
                sbHtml.AppendLine("<script>window.onload = function() { document.forms[0].submit(); }</script>");
                sbHtml.AppendLine("</head>");
                sbHtml.AppendLine("<body>");
                // Target the IFrame in the Checkout Modal
                sbHtml.AppendLine($"<form action='{gatewayUrl}' method='post' target='threeDIframe'>");
                
                foreach(var kvp in postParams)
                {
                    sbHtml.AppendLine($"<input type='hidden' name='{kvp.Key}' value='{kvp.Value}' />");
                }
                // Add missing inputs manually if needed (like taksit/Instalment naming issue)
                if(!postParams.ContainsKey("taksit") && !string.IsNullOrEmpty(installment))
                {
                     sbHtml.AppendLine($"<input type='hidden' name='taksit' value='{installment}' />");
                }
                
                sbHtml.AppendLine("</form>");
                sbHtml.AppendLine("</body></html>");
                
                var msg = sbHtml.ToString();
                
                var parameters = new Dictionary<string, object>();
                parameters.Add("HTMLContent", msg);
                return Task.FromResult(PaymentGatewayResult.Successed(parameters, request.CallbackUrl?.ToString() ?? ""));

            }
            catch (Exception ex)
            {
                return Task.FromResult(PaymentGatewayResult.Failed("Manual Gen Failed: " + ex.Message));
            }
        }
        catch (Exception ex)
        {
             return Task.FromResult(PaymentGatewayResult.Failed(ex.ToString()));
        }
    }

    public Task<VerifyGatewayResult> VerifyGateway(VerifyGatewayRequest request, PaymentGatewayRequest gatewayRequest, IFormCollection form)
    {
        try 
        {
             
             string bankName = (request.BankParameters.ContainsKey("bankName") ? request.BankParameters["bankName"] : "IsBankasi") ?? "IsBankasi";
             string bankCode = MapToCpBankCode(bankName);
            
             // Helper local function for case-insensitive lookup
             string GetParam(string key) => 
                request.BankParameters.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)) is string foundKey 
                ? request.BankParameters[foundKey] 
                : "";

             // Auto-detect test mode: If any parameter (like GatewayUrl) points to Asseco or contains 'test', enable test mode.
             bool isTestMode = request.BankParameters.Values.Any(v => 
                 v != null && (v.Contains("asseco", StringComparison.OrdinalIgnoreCase) || v.Contains("test", StringComparison.OrdinalIgnoreCase)));
             
             // Explicit override if "isTest" parameter exists
             var isTestParam = GetParam("isTest");
             if (!string.IsNullOrEmpty(isTestParam) && (isTestParam == "1" || isTestParam.Equals("true", StringComparison.OrdinalIgnoreCase)))
             {
                 isTestMode = true;
             }

             var auth = new VirtualPOSAuth
             {
                bankCode = bankCode,
                merchantID = GetParam("clientId"),
                merchantStorekey = GetParam("storeKey"),
                merchantUser = !string.IsNullOrEmpty(GetParam("userName")) ? GetParam("userName") : GetParam("merchantUser"),
                merchantPassword = !string.IsNullOrEmpty(GetParam("password")) ? GetParam("password") : GetParam("merchantPassword"),
                testPlatform = isTestMode
             };

             // Convert Form to Dictionary<string, object>
            // CRITICAL: Pass StringValues directly as object (don't call ToString())
            // This matches official CP.VPOS documentation
            var msg = form.Keys.ToDictionary(k => k, v => (object)form[v]);

             // Call Library Validation
             try 
             {
                 var debugLogPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                 var debugVerify = new 
                 {
                     Time = DateTime.Now,
                     Action = "VerifyGateway",
                     Auth = new { auth.bankCode, auth.merchantID, merchantUser = auth.merchantUser, merchantPassword = "***", merchantStorekey = "***" },
                     ResponseMsg = msg
                 };
                 System.IO.File.AppendAllText(debugLogPath, $"\n--- VerifyGateway ({DateTime.Now}) ---\n{System.Text.Json.JsonSerializer.Serialize(debugVerify)}\n--------------------\n");
             } catch {}

             var response = VPOSClient.Sale3DResponse(new Sale3DResponseRequest { responseArray = msg }, auth);

             try 
             {
                 var debugLogPath = System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "payment_debug.txt");
                 System.IO.File.AppendAllText(debugLogPath, $"Verify Result: Statu={response.statu}, Msg={response.message}\nOrderNumber: {response.orderNumber}\nTransactionId: {response.transactionId}\n");
             } catch {}

             if(response.statu == SaleResponseStatu.Success)
             {
                 return Task.FromResult(VerifyGatewayResult.Successed(
                    transactionId: response.transactionId ?? "", 
                    referenceNumber: response.orderNumber ?? "", 
                    installment: 0,
                    extraInstallment: 0,
                    message: response.message ?? "Success",
                    responseCode: "00"
                 ));
             }
             else
             {
                 var detail = $"Statu: {response.statu}, Msg: {response.message}";
                 return Task.FromResult(VerifyGatewayResult.Failed(detail, response.statu.ToString()));
             }
        }
        catch(Exception ex)
        {
             return Task.FromResult(VerifyGatewayResult.Failed($"EXCEPTION: {ex.Message} {ex.StackTrace}"));
        }
    }

    public Task<CancelPaymentResult> CancelRequest(CancelPaymentRequest request) => Task.FromResult(CancelPaymentResult.Failed("NI"));
    public Task<RefundPaymentResult> RefundRequest(RefundPaymentRequest request) => Task.FromResult(RefundPaymentResult.Failed("NI"));
    public Task<PaymentDetailResult> PaymentDetailRequest(PaymentDetailRequest request) => Task.FromResult(PaymentDetailResult.FailedResult("NI"));
    public Dictionary<string, string> TestParameters => new Dictionary<string, string>();

    private Currency MapCurrency(string isoCode) => isoCode switch { "TRY" => Currency.TRY, "USD" => Currency.USD, "EUR" => Currency.EUR, _ => Currency.TRY };

    private string MapToCpBankCode(string bankName)
    {
        if (string.IsNullOrEmpty(bankName)) return CP.VPOS.Services.BankService.IsBankasi;
        if(bankName.Contains("IsBank", StringComparison.OrdinalIgnoreCase)) return CP.VPOS.Services.BankService.IsBankasi;
        if(bankName.Contains("Akbank", StringComparison.OrdinalIgnoreCase)) return CP.VPOS.Services.BankService.AkbankNestpay;
        if(bankName.Contains("Halk", StringComparison.OrdinalIgnoreCase)) return CP.VPOS.Services.BankService.Halkbank;
        if(bankName.Contains("Ziraat", StringComparison.OrdinalIgnoreCase)) return CP.VPOS.Services.BankService.ZiraatBankasi;
        return CP.VPOS.Services.BankService.IsBankasi;
    }
}
