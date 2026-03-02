using System.Diagnostics.CodeAnalysis;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Entities;
using ecommerce.Core.Extensions;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils;
namespace ecommerce.Domain.Shared.Emailing;
[SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
[SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract")]
public class EmailService : IEmailService{
    private readonly IConfiguration _configuration;
    private readonly IHangfireJobManager _hangfireJobManager;
    private readonly FileHelper _fileHelper;
    private readonly EmailSendJob _emailSendJob;

    public EmailService(IConfiguration configuration, IHangfireJobManager hangfireJobManager, FileHelper fileHelper, EmailSendJob emailSendJob){
        _configuration = configuration;
        _hangfireJobManager = hangfireJobManager;
        _fileHelper = fileHelper;
        _emailSendJob = emailSendJob;
    }
    public async Task SendNewUserEmail(string fullname, string email, string activationToken)
    {
        var activationUrl = $"{_configuration["App:BaseUrl"]}/activate-account?token={activationToken}&email={Uri.EscapeDataString(email)}";

        var tokens = new EmailTokenDictionary
        {
            { "FullName", fullname },
            { "UserName", email },
            { "Url", activationUrl },
            { "VerifyButton", "Hesabı Aktifleştir" },
            { "SupportEmail", "destek@bicops.tr" },
        };

        AddCommonTokens(tokens);

        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.NewUser,
            Subject = "Hesabınız Başarıyla Oluşturuldu",
            Tokens = tokens,
            ToAddress = email,
            ToName = fullname
        }, queue: "admin");
    }

    /// <inheritdoc />
    public async Task SendNewSubUserEmail(string fullname, string email, string generatedPassword)
    {
        var loginUrl = _configuration["App:BaseUrl"]?.TrimEnd('/') ?? "https://bicops.tr";

        var tokens = new EmailTokenDictionary
        {
            { "FullName", fullname },
            { "UserName", email },
            { "GeneratedPassword", generatedPassword },
            { "Url", loginUrl },
            { "VerifyButton", "Giriş Yap" },
            { "SupportEmail", "destek@bicops.tr" },
        };

        AddCommonTokens(tokens);

        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.NewUser,
            Subject = "Hesabınız Oluşturuldu — Giriş Bilgileriniz",
            Tokens = tokens,
            ToAddress = email,
            ToName = fullname
        }, queue: "admin");
    }
   public async Task SendNewUserTokenEmail(string fullname, string email, string resetToken)
   {
       var appsetting = _configuration.GetSection("EmailSetting");
   
       var baseUrl = appsetting["DevUrl"]; 
       var url = $"{baseUrl}?token={resetToken}&email={email}";
   
       var tokens = new EmailTokenDictionary
       {
           {"FullName", fullname},
           {"Url", url},
           {"VerifyButton", "Şifreyi Sıfırla"},
           {"LogoUrl", "https://yedeksek.com/assets/images/logo.png"}, 
           {"ResetImageUrl", "https://yedeksen.com/assets/images/logo/yedeksen.png"}
       };
   
       AddCommonTokens(tokens); 
   
       await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
       {
           TemplateName = EmailTemplateType.PasswordReset,
           Subject = "Yeni Parola Oluşturma Onayı",
           Tokens = tokens,
           ToAddress = email,
           ToName = fullname
       }, queue: "admin");
   }
   public async Task SendEmailVerificationCodeEmail(string fullname, string email, string code)
   {
       var tokens = new EmailTokenDictionary
       {
           {"FullName", fullname},
           {"Url", code},
           {"VerifyButton", $"Doğrulama Kodunuz: {code}"},
           {"LogoUrl", "https://yedeksen.com/assets/images/logo/yedeksen.png"},
           {"ResetImageUrl", "https://yedeksen.com/assets/images/logo/yedeksen.png"}
       };

       AddCommonTokens(tokens);

       await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
       {
           TemplateName = EmailTemplateType.NewUser,
           Subject = $"ParPazar E-posta Doğrulama Kodunuz: {code}",
           Tokens = tokens,
           ToAddress = email,
           ToName = fullname
       }, queue: "admin");
   }

   public async Task SendDeliveryVerificationCodeEmail(string fullname, string email, string orderNumber, string code)
   {
       var tokens = new EmailTokenDictionary
       {
           {"FullName", fullname},
           {"Url", code},
           {"Code", code},
           {"VerifyButton", $"Teslimat Doğrulama Kodunuz: {code}"},
           {"OrderNumber", orderNumber},
           {"LogoUrl", "https://yedeksen.com/assets/images/logo/yedeksen.png"},
           {"ResetImageUrl", "https://yedeksen.com/assets/images/logo/yedeksen.png"}
       };
       AddCommonTokens(tokens);
       await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
       {
           TemplateName = EmailTemplateType.DeliveryVerificationCode,
           Subject = $"Sipariş {orderNumber} — Teslimat Doğrulama Kodunuz: {code}",
           Tokens = tokens,
           ToAddress = email,
           ToName = fullname
       }, queue: "admin");
   }

    public async Task SendPasswordResetEmail(string fullname, string email, string token){
        var appsetting = _configuration.GetSection("EmailSetting");
        var url = appsetting["ForgetPassword"] + token;
        var tokens = new EmailTokenDictionary{{"FullName", fullname},{"Url", url}};
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.PasswordReset,
                Subject = "Şifre Sıfırlama",
                Tokens = tokens,
                ToAddress = email,
                ToName = fullname
            }, queue: "admin"
        );
    }
    public async Task SendOrderPlacedSellerEmail(Orders order)
    {
        if (order == null || order.Seller == null) return;

        var sellerName = order.Seller.Name;
        var baseUrl = _configuration["Cdn:BaseUrl"]?.TrimEnd('/') + "/";
        var logoUrl = "https://yedeksen.com/assets/images/logo/yedeksen.png";
        var adminUrl = _configuration["EmailSetting:AdminUrl"] ?? _configuration["App:AdminUrl"];
        var orderDetailsUrl = $"{adminUrl?.TrimEnd('/')}/orders/detail/{order.Id}";
        
        // Prepare delivery address
        var deliveryAddress = $"{order.UserAddress?.FullName ?? order.UserFullName ?? ""}\n" +
                             $"{order.UserAddress?.Address ?? ""}\n" +
                             $"{order.UserAddress?.City?.Name ?? ""}\n" +
                             $"Tel: {order.UserAddress?.PhoneNumber ?? order.UserPhoneNumber ?? ""}";

        // Prepare products list directly for the seller (Flat structure)
        var products = new List<Dictionary<string, object>>();
        if (order.OrderItems != null)
        {
            foreach (var item in order.OrderItems)
            {
                var imageFile = item.ProductImages?.FirstOrDefault()?.FileGuid ?? 
                                item.Product?.ProductImage?.FirstOrDefault()?.FileGuid;
                
                var productImageUrl = string.IsNullOrWhiteSpace(imageFile) 
                    ? "https://placehold.co/200x200/f8f9fa/0DA487?text=Urun+Resmi+Yok" 
                    : $"{baseUrl}{imageFile}";
                                
                products.Add(new Dictionary<string, object> {
                    { "ProductImage", productImageUrl },
                    { "ProductName", item.ProductName ?? item.Product?.Name ?? "İsimsiz Ürün" },
                    { "ProductCode", item.Product?.Barcode ?? "" },
                    { "Quantity", item.Quantity },
                    { "UnitPrice", $"₺{item.Price:N2}" },
                    { "TotalPrice", $"₺{item.TotalPrice:N2}" }
                });
            }
        }

        Console.WriteLine($"[EmailService] Sending seller email for {order.OrderNumber}. Items: {products.Count}");

        var tokens = new EmailTokenDictionary
        {
            {"LogoUrl", logoUrl},
            {"FullName", sellerName},
            {"OrderNumber", order.OrderNumber},
            {"OrderDate", order.CreatedDate.ToString("dd.MM.yyyy HH:mm")},
            {"Subtotal", $"₺{order.ProductTotal:N2}"},
            {"TotalShipping", $"₺{order.CargoPrice:N2}"},
            {"Discount", order.DiscountTotal > 0 ? $"₺{order.DiscountTotal:N2}" : null},
            {"GrandTotal", $"₺{order.GrandTotal:N2}"},
            {"DeliveryAddress", deliveryAddress},
            {"OrderDetailsUrl", orderDetailsUrl},
            {"Products", products}, 
            {"SellerTotal", $"₺{order.ProductTotal:N2}"},
            {"ShippingCost", order.CargoPrice > 0 ? $"₺{order.CargoPrice:N2}" : null}
        };

        AddCommonTokens(tokens);

        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.OrderNotificationSeller,
            Subject = $"Yeni Sipariş Bildirimi - {order.OrderNumber}",
            Tokens = tokens,
            ToAddress = order.Seller.Email,
            ToName = sellerName
        }, queue: "admin");
    }

    public async Task SendOrderPlacedCustomerEmail(List<Orders> orders)
    {
        if (orders == null || !orders.Any()) return;
        var firstOrder = orders.First();
        
        if (firstOrder.User == null && firstOrder.ApplicationUser == null) return;
        
        var customerName = firstOrder.UserFullName;
        var orderNumbers = string.Join(", ", orders.Select(o => o.OrderNumber));
        var baseUrl = _configuration["Cdn:BaseUrl"]?.TrimEnd('/') + "/";
        var logoUrl = "https://yedeksen.com/assets/images/logo/yedeksen.png";
        var frontendUrl = _configuration["App:BaseUrl"]?.TrimEnd('/');
        var orderDetailsUrl = $"{frontendUrl}/user-dashboard?orderNumber={firstOrder.OrderNumber}#pills-order";
        
        // Prepare delivery address
        var deliveryAddress = $"{firstOrder.UserAddress?.FullName ?? firstOrder.UserFullName}\n" +
                             $"{firstOrder.UserAddress?.Address ?? ""}\n" +
                             $"{firstOrder.UserAddress?.City?.Name ?? ""}\n" +
                             $"Tel: {firstOrder.UserAddress?.PhoneNumber ?? firstOrder.UserPhoneNumber}";

        var sellersList = new List<Dictionary<string, object>>();
        foreach (var order in orders)
        {
            var sellerName = order.Seller?.Name ?? "Mağaza";
            var products = new List<Dictionary<string, object>>();
            
            if (order.OrderItems != null)
            {
                foreach (var item in order.OrderItems)
                {
                    var imageFile = item.ProductImages?.FirstOrDefault()?.FileGuid ?? 
                                    item.Product?.ProductImage?.FirstOrDefault()?.FileGuid;

                    
                    var productImageUrl = string.IsNullOrWhiteSpace(imageFile) 
                        ? "https://placehold.co/200x200/f8f9fa/0DA487?text=Urun+Resmi+Yok" 
                        : $"{baseUrl}{imageFile}";

                    products.Add(new Dictionary<string, object> {
                        { "ProductImage", productImageUrl },
                        { "ProductName", item.ProductName ?? item.Product?.Name ?? "İsimsiz Ürün" },
                        { "ProductCode", item.Product?.Barcode ?? "" },
                        { "Quantity", item.Quantity },
                        { "UnitPrice", $"₺{item.Price:N2}" },
                        { "TotalPrice", $"₺{item.TotalPrice:N2}" }
                    });
                }
            }

            sellersList.Add(new Dictionary<string, object> {
                {"SellerName", sellerName},
                {"SellerTotal", $"₺{order.ProductTotal:N2}"},
                {"ShippingCost", order.CargoPrice > 0 ? $"₺{order.CargoPrice:N2}" : null},
                {"Products", products}
            });
        }

        var tokens = new EmailTokenDictionary
        {
            {"LogoUrl", logoUrl},
            {"FullName", customerName},
            {"OrderNumber", orderNumbers},
            {"OrderDate", firstOrder.CreatedDate.ToString("dd.MM.yyyy HH:mm")},
            {"Subtotal", $"₺{orders.Sum(o => o.ProductTotal):N2}"},
            {"TotalShipping", $"₺{orders.Sum(o => o.CargoPrice):N2}"},
            {"Discount", orders.Sum(o => o.DiscountTotal) > 0 ? $"₺{orders.Sum(o => o.DiscountTotal):N2}" : null},
            {"GrandTotal", $"₺{orders.Sum(o => o.GrandTotal):N2}"},
            {"DeliveryAddress", deliveryAddress},
            {"OrderDetailsUrl", orderDetailsUrl},
            {"Sellers", sellersList}
        };

        AddCommonTokens(tokens);

        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.OrderNotificationCustomer,
            Subject = $"Siparişiniz Alındı - {orderNumbers}",
            Tokens = tokens,
            ToAddress = firstOrder.UserEmail,
            ToName = customerName
        }, queue: "admin");
    }
    public async Task SendOrderCancelRequestSellerEmail(Orders order){
        if(order.Seller == null){
            return;
        }
        var sellerName = order.Seller.Name;
        var url = order.CargoTrackUrl ?? _fileHelper.GetEmailFileUrl("incoming-order");
        var returnOrCancelDate = order.ReturnOrCancelDate.HasValue ? DateTime.SpecifyKind(order.ReturnOrCancelDate.Value, DateTimeKind.Utc).ToLocalTime() : DateTime.Now;
        var tokens = new EmailTokenDictionary{
            {"FullName", sellerName},
            {"Url", url},
            {"OrderNumber", order.OrderNumber},
            {"OrderTotal", order.OrderTotal},
            {"OrderDate", order.CreatedDate},
            {"OrderAddress", order.Seller.Address},
            {"OrderCancelDate", returnOrCancelDate},
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.OrderNotification,
                Subject = "Sipariş İptal Bildirimi",
                Tokens = tokens,
                ToAddress = order.Seller.Email,
                ToName = sellerName
            }, queue: "admin"
        );
    }
    public async Task SendOrderCancelRequestCustomerEmail(Orders order){
        if(order.User == null && order.ApplicationUser == null){
            return;
        }
        var customerName = order.UserFullName;
        var url = _fileHelper.GetEmailFileUrl("incoming-order");
        var returnOrCancelDate = order.ReturnOrCancelDate.HasValue ? DateTime.SpecifyKind(order.ReturnOrCancelDate.Value, DateTimeKind.Utc).ToLocalTime() : DateTime.Now;
        var tokens = new EmailTokenDictionary{
            {"FullName", customerName},
            {"Url", url},
            {"OrderNumber", order.OrderNumber},
            {"OrderTotal", order.OrderTotal},
            {"OrderDate", order.CreatedDate},
            {"OrderCancelDate", returnOrCancelDate},
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.OrderNotification,
                Subject = "İade Talebiniz Alındı",
                Tokens = tokens,
                ToAddress = order.UserEmail,
                ToName = customerName
            }, queue: "admin"
        );
    }
    public async Task SendOrderShippedEmail(Orders order){
        // Geçici olarak kapatıldı — kargo mail servisi
        return;
        if(order.User == null && order.ApplicationUser == null){
            return;
        }
        var customerName = order.UserFullName;
        var url = order.CargoTrackUrl ?? _fileHelper.GetEmailFileUrl("incoming-order");
        var tokens = new EmailTokenDictionary{
            {"FullName", customerName},
            {"Url", url},
            {"OrderNumber", order.OrderNumber},
            {"OrderTotal", order.OrderTotal},
            {"OrderDate", order.CreatedDate},
        
            {"OrderCargoName", order.Cargo?.Name},
            {"OrderCargoTrackNumber", order.CargoTrackNumber},
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.OrderNotification,
                Subject = "Paketiniz Kargoya Verildi",
                Tokens = tokens,
                ToAddress = order.UserEmail,
                ToName = customerName
            }, queue: "admin"
        );
    }
    public async Task SendOrderSuccessEmail(Orders order){
        // Geçici olarak kapatıldı — OrderSuccess mail servisi
        return;
        if(order.User == null && order.ApplicationUser == null){
            return;
        }
        var customerName = order.UserFullName;
        var url = order.CargoTrackUrl ?? _fileHelper.GetEmailFileUrl("incoming-order");
        var tokens = new EmailTokenDictionary{
            {"FullName", customerName},
            {"Url", url},
            {"OrderNumber", order.OrderNumber},
            {"OrderTotal", order.OrderTotal},
            {"OrderDate", order.CreatedDate},
            {"OrderCargoName", order.Cargo?.Name},
            {"OrderCargoTrackNumber", order.CargoTrackNumber},
            {"ShipmentDate", order.ShipmentDate},
            {"DeliveryTo", order.DeliveryTo}
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.OrderNotification,
                Subject = "Siparişiniz Teslim Edilmiştir.",
                Tokens = tokens,
                ToAddress = order.UserEmail,
                ToName = customerName
            }, queue: "admin"
        );
    }
    public async Task SendOrderCancelledCustomerEmail(Orders order)
    {
        if (order == null || (order.User == null && order.ApplicationUser == null)) return;

        var customerName = order.UserFullName;
        var baseUrl = _configuration["Cdn:BaseUrl"]?.TrimEnd('/') + "/";
        var logoUrl = "https://yedeksen.com/assets/images/logo/yedeksen.png";
        var frontendUrl = _configuration["App:BaseUrl"]?.TrimEnd('/');
        var orderDetailsUrl = $"{frontendUrl}/user-dashboard?orderNumber={order.OrderNumber}#pills-order";

        // Prepare products list
        var products = new List<Dictionary<string, object>>();
        if (order.OrderItems != null)
        {
            foreach (var item in order.OrderItems)
            {
                var imageFile = item.ProductImages?.FirstOrDefault()?.FileGuid ?? 
                                item.Product?.ProductImage?.FirstOrDefault()?.FileGuid;
                
                var productImageUrl = string.IsNullOrWhiteSpace(imageFile) 
                    ? "https://placehold.co/200x200/f8f9fa/0DA487?text=Urun+Resmi+Yok" 
                    : $"{baseUrl}{imageFile}";
                                
                products.Add(new Dictionary<string, object> {
                    { "ProductImage", productImageUrl },
                    { "ProductName", item.ProductName ?? item.Product?.Name ?? "İsimsiz Ürün" },
                    { "ProductCode", item.Product?.Barcode ?? "" },
                    { "Quantity", item.Quantity },
                    { "UnitPrice", $"₺{item.Price:N2}" },
                    { "TotalPrice", $"₺{item.TotalPrice:N2}" }
                });
            }
        }

        var tokens = new EmailTokenDictionary
        {
            {"LogoUrl", logoUrl},
            {"FullName", customerName},
            {"OrderNumber", order.OrderNumber},
            {"OrderDate", order.CreatedDate.ToString("dd.MM.yyyy HH:mm")},
            {"CancelledDate", DateTime.Now.ToString("dd.MM.yyyy HH:mm")},
            {"GrandTotal", $"₺{order.GrandTotal:N2}"},
            {"OrderDetailsUrl", orderDetailsUrl},
            {"Products", products}
        };

        AddCommonTokens(tokens);

        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.OrderCancelledCustomer,
            Subject = $"Siparişiniz İptal Edildi - {order.OrderNumber}",
            Tokens = tokens,
            ToAddress = order.UserEmail,
            ToName = customerName
        }, queue: "admin");
    }

    public async Task SendOrderCancelledSellerEmail(Orders order)
    {
        if (order == null || order.Seller == null) return;

        var sellerName = order.Seller.Name;
        var baseUrl = _configuration["Cdn:BaseUrl"]?.TrimEnd('/') + "/";
        var logoUrl = "https://yedeksen.com/assets/images/logo/yedeksen.png";
        var adminUrl = _configuration["EmailSetting:AdminUrl"] ?? _configuration["App:AdminUrl"];
        var orderDetailsUrl = $"{adminUrl?.TrimEnd('/')}/orders/detail/{order.Id}";

        // Prepare products list
        var products = new List<Dictionary<string, object>>();
        if (order.OrderItems != null)
        {
            foreach (var item in order.OrderItems)
            {
                var imageFile = item.ProductImages?.FirstOrDefault()?.FileGuid ?? 
                                item.Product?.ProductImage?.FirstOrDefault()?.FileGuid;
                
                var productImageUrl = string.IsNullOrWhiteSpace(imageFile) 
                    ? "https://placehold.co/200x200/f8f9fa/0DA487?text=Urun+Resmi+Yok" 
                    : $"{baseUrl}{imageFile}";
                                
                products.Add(new Dictionary<string, object> {
                    { "ProductImage", productImageUrl },
                    { "ProductName", item.ProductName ?? item.Product?.Name ?? "İsimsiz Ürün" },
                    { "ProductCode", item.Product?.Barcode ?? "" },
                    { "Quantity", item.Quantity },
                    { "UnitPrice", $"₺{item.Price:N2}" },
                    { "TotalPrice", $"₺{item.TotalPrice:N2}" }
                });
            }
        }

        var tokens = new EmailTokenDictionary
        {
            {"LogoUrl", logoUrl},
            {"FullName", sellerName},
            {"OrderNumber", order.OrderNumber},
            {"OrderDate", order.CreatedDate.ToString("dd.MM.yyyy HH:mm")},
            {"CancelledDate", DateTime.Now.ToString("dd.MM.yyyy HH:mm")},
            {"SellerTotal", $"₺{order.ProductTotal:N2}"},
            {"OrderDetailsUrl", orderDetailsUrl},
            {"Products", products}
        };

        AddCommonTokens(tokens);

        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.OrderCancelledSeller,
            Subject = $"Sipariş İptal Edildi - {order.OrderNumber}",
            Tokens = tokens,
            ToAddress = order.Seller.Email,
            ToName = sellerName
        }, queue: "admin");
    }
    public async Task SendSupportRequestSystemEmail(SupportLine supportLine, string ? topic = null){
        var appsetting = _configuration.GetSection("EmailSetting");
        var toAddress = appsetting["SystemEmailAddress"];
        var bccAddresses = appsetting["SystemBccEmailAddresses"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if(string.IsNullOrEmpty(toAddress)){
            return;
        }
        var url = _fileHelper.GetEmailFileAdminUrl("supportline");
        var tokens = new EmailTokenDictionary{
            {"Url", url},
            {"SupportLineType", supportLine.SupportLineType.GetDisplayName()},
            {"FirstName", supportLine.FirstName},
            {"LastName", supportLine.LastName},
            {"Email", supportLine.Email},
            {"PhoneNumber", supportLine.PhoneNumber},
            {"Topic", topic},
            {"Description", supportLine.Description},
            {"CreatedDate", supportLine.CreatedDate},
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.NewSupport,
                Subject = "Yedeksen | Yeni Destek Talebi Alındı | {{String.Format CreatedDate \"g\"}}",
                Tokens = tokens,
                ToAddress = toAddress,
                ToName = toAddress,
                Bcc = bccAddresses
            }, queue: "admin"
        );
    }
    public async Task SendOrderCancelRequestSystemEmail(Orders order){
        var appsetting = _configuration.GetSection("EmailSetting");
        var toAddress = appsetting["SystemEmailAddress"];
        var bccAddresses = appsetting["SystemBccEmailAddresses"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if(string.IsNullOrEmpty(toAddress)){
            return;
        }
        var url = _fileHelper.GetEmailFileAdminUrl("orders");
        var sellerName = order.Seller.Name;
        var companyName = order.Seller.Name;
        var returnOrCancelDate = order.ReturnOrCancelDate.HasValue ? DateTime.SpecifyKind(order.ReturnOrCancelDate.Value, DateTimeKind.Utc).ToLocalTime() : DateTime.Now;
        var tokens = new EmailTokenDictionary{
            {"Url", url},
            {"OrderNumber", order.OrderNumber},
            {"OrderTotal", order.OrderTotal},
            {"OrderDate", order.CreatedDate},
            {"OrderCancelDate", returnOrCancelDate},
            {"SellerName", sellerName},
            {"CompanyName", companyName},
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.OrderNotification,
                Subject = "Yedeksen | Sipariş İptal Talebi Alındı | {{String.Format OrderCancelDate \"g\"}}",
                Tokens = tokens,
                ToAddress = toAddress,
                ToName = toAddress,
                Bcc = bccAddresses
            }, queue: "admin"
        );
    }
    public async Task SendOrderPlacedSystemEmail(Orders order){
        var appsetting = _configuration.GetSection("EmailSetting");
        var toAddress = appsetting["SystemEmailAddress"];
        var bccAddresses = appsetting["SystemBccEmailAddresses"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if(string.IsNullOrEmpty(toAddress)){
            return;
        }
        var url = _fileHelper.GetEmailFileAdminUrl("orders");
        var sellerName = order.Seller.Name;
        var companyName = order.UserFullName;
        var tokens = new EmailTokenDictionary{
            {"Url", url},
            {"OrderNumber", order.OrderNumber},
            {"OrderTotal", order.OrderTotal},
            {"OrderDate", order.CreatedDate},
            {"SellerName", sellerName},
            {"CompanyName", companyName},
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.OrderNotification,
                Subject = "Parpazar | Yeni Sipariş Alındı | {{String.Format OrderDate \"g\"}}",
                Tokens = tokens,
                ToAddress = toAddress,
                ToName = toAddress,
                Bcc = bccAddresses
            }, queue: "admin"
        );
    }
    public async Task SendNewUserSystemEmail(string fullname, string email, DateTime registerDate){
        var appsetting = _configuration.GetSection("EmailSetting");
        var toAddress = appsetting["SystemEmailAddress"];
        var bccAddresses = appsetting["SystemBccEmailAddresses"]?.Split(',', StringSplitOptions.RemoveEmptyEntries);
        if(string.IsNullOrEmpty(toAddress)){
            return;
        }
        var url = _fileHelper.GetEmailFileAdminUrl("membership-list");
        var tokens = new EmailTokenDictionary{{"Url", url},{"FullName", fullname},{"UserName", email},{"RegisterDate", registerDate}};
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.NewUser,
                Subject = "Parpazar | Yeni Kullanıcı Kayıt Oldu | {{String.Format RegisterDate \"g\"}}",
                Tokens = tokens,
                ToAddress = toAddress,
                ToName = toAddress,
                Bcc = bccAddresses
            }, queue: "admin"
        );
    }
    public async Task UserBirthDateEmail(string firstname, string lastname, string email){
        var tokens = new EmailTokenDictionary{
            {"Firstname", firstname},
            {"Lastname", lastname},
            {"FullName", $"{firstname} {lastname}"},
            {"Url", _fileHelper.GetEmailFileUrl("login")},
            {"SiteUrl", "https://Parpazar.com"},
            {"SupportUrl","https://Parpazar.com/support-line?t=contact"}
        };
        AddCommonTokens(tokens);
        await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                TemplateName = EmailTemplateType.UserBirthDate,
                Subject = "Nice Yıllara",
                Tokens = tokens,
                ToAddress = email,
                ToName = $"{firstname} {lastname}"
            }, queue: "admin"
        );
    }
    public async Task ZoomMeetCreate(string fullname, string subject,string text, DateTime meetDate,int duration, string email){
        try
        {
            var tokens = new EmailTokenDictionary{
                {"FullName", fullname},
                {"Subject", subject},
                {"MeetDate", meetDate.ToString("dd.MM.yyyy HH:mm")},
                {"SiteUrl", "https://Parpazar.com"},
                {"Description",text},
                {"MeetHour",$"{duration.ToString()} Dakika"}
            };
            AddCommonTokens(tokens);
            await _hangfireJobManager.EnqueueAsync<EmailSendJob>(new EmailSendJobArgs{
                    TemplateName = EmailTemplateType.NewUser,
                    Subject = subject,
                    Tokens = tokens,
                    ToAddress = email,
                    ToName = fullname
                }, queue: "admin"
            );
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task SendPaymentReceiptEmail(string toEmail, string toName, string customerName, string customerCode, DateTime transactionDate, string? description, decimal amount, string? paymentTypeName, byte[]? pdfAttachment = null, string? pdfFileName = null, string? makbuzNo = null)
    {
        if (string.IsNullOrWhiteSpace(toEmail)) return;

        var logoUrl = _configuration["Company:LogoUrl"] ?? "https://yedeksen.com/assets/images/logo/yedeksen.png";
        var companyName = _configuration["Company:CompanyName"] ?? _configuration["AppSettings:CompanyName"] ?? "Bicops";
        var companyVat = _configuration["Company:CompanyVat"] ?? _configuration["AppSettings:CompanyVat"] ?? "";
        var companyVatName = _configuration["Company:CompanyVatName"] ?? _configuration["AppSettings:CompanyVatName"] ?? "";
        var companyAddress = _configuration["Company:CompanyAddress"] ?? _configuration["AppSettings:CompanyAddress"] ?? "";

        var tokens = new EmailTokenDictionary
        {
            { "LogoUrl", logoUrl },
            { "CompanyName", companyName },
            { "CompanyVat", companyVat },
            { "CompanyVatName", companyVatName },
            { "CompanyAddress", companyAddress },
            { "FullName", toName },
            { "CustomerName", customerName },
            { "CustomerCode", customerCode },
            { "TransactionDate", transactionDate.ToString("dd.MM.yyyy HH:mm") },
            { "Description", description ?? "Tahsilat" },
            { "Amount", $"₺{amount:N2}" },
            { "PaymentTypeName", paymentTypeName ?? "Nakit/Kredi Kartı" },
            { "MakbuzNo", makbuzNo ?? "" }
        };

        AddCommonTokens(tokens);

        var attachments = (System.Collections.Generic.IList<ecommerce.Core.Models.NameValue<string?, byte[]>>?)null;
        if (pdfAttachment != null && pdfAttachment.Length > 0 && !string.IsNullOrWhiteSpace(pdfFileName))
        {
            attachments = new List<ecommerce.Core.Models.NameValue<string?, byte[]>>
            {
                new ecommerce.Core.Models.NameValue<string?, byte[]>(pdfFileName, pdfAttachment)
            };
        }

        var args = new EmailSendJobArgs
        {
            TemplateName = EmailTemplateType.PaymentReceipt,
            Subject = $"Tahsilat Makbuzu - {transactionDate:dd.MM.yyyy}",
            Tokens = tokens,
            ToAddress = toEmail,
            ToName = toName,
            Attachments = attachments
        };
        await _emailSendJob.ExecuteAsync(args);
    }

    private void AddCommonTokens(EmailTokenDictionary tokens){
        tokens.TryAdd("SiteUrl", _fileHelper.GetEmailFileUrl("/"));
        tokens.TryAdd("SupportUrl", _fileHelper.GetEmailFileUrl("support-line?t=contact"));
        tokens.TryAdd("FacebookUrl", "https://www.facebook.com/Parpazar");
        tokens.TryAdd("InstagramUrl", "https://www.instagram.com/Parpazar/");
        tokens.TryAdd("LinkedinUrl", "https://www.linkedin.com/company/Parpazar/");
    }
}
