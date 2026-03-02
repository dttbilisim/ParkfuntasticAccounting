using System.Runtime.InteropServices;
using ecommerce.Core.BackgroundJobs;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Domain.Shared.Services;
using HandlebarsDotNet;
using HandlebarsDotNet.Helpers;
using HandlebarsDotNet.Helpers.Enums;
using System.Dynamic;
using System.Collections;
using Microsoft.Extensions.Logging;

namespace ecommerce.Domain.Shared.Emailing;

public class EmailSendJob : IAsyncBackgroundJob<EmailSendJobArgs>
{
    private readonly IEmailSender _emailSender;
    private readonly FileHelper _fileHelper;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly ILogger<EmailSendJob> _logger;

    public EmailSendJob(
        IEmailSender emailSender,
        FileHelper fileHelper,
        IEmailTemplateService emailTemplateService,
        ILogger<EmailSendJob> logger)
    {
        _emailSender = emailSender;
        _fileHelper = fileHelper;
        _emailTemplateService = emailTemplateService;
        _logger = logger;
    }

    public async Task ExecuteAsync(EmailSendJobArgs args)
    {
        var nestedTokens = args.Tokens?.ConvertToNestedDictionary();
        var renderTokens = ConvertToPlainObject(nestedTokens);

        var emailTemplateContent = await _emailTemplateService.GetEmailTemplate(args.TemplateName);
        string subject;
        string rawBody;

        // Tahsilat makbuzu: her zaman varsayılan HTML, token'lar appsettings (şirket) + cari/tarih/tutar ile doldurulur
        if (args.TemplateName == EmailTemplateType.PaymentReceipt)
        {
            subject = args.Subject ?? "Tahsilat Makbuzu";
            rawBody = BuildDefaultPaymentReceiptHtml(args.Tokens);
        }
        // Kullanıcı kayıt / hesap aktifleştirme: Bicops tasarımı (mor başlık, BICOPS, destek@bicops.tr)
        else if (args.TemplateName == EmailTemplateType.NewUser)
        {
            subject = args.Subject ?? "Hesabınız Başarıyla Oluşturuldu";
            rawBody = BuildDefaultNewUserHtml(args.Tokens);
        }
        // Teslimat doğrulama kodu: Admin'de Bicops template varsa onu kullan, yoksa varsayılan HTML
        else if (args.TemplateName == EmailTemplateType.DeliveryVerificationCode)
        {
            subject = args.Subject ?? "Teslimat Doğrulama Kodu";
            if (emailTemplateContent?.Result != null && !string.IsNullOrWhiteSpace(emailTemplateContent.Result.Description))
            {
                var handlebarsContext = Handlebars.Create();
                handlebarsContext.Configuration.TextEncoder = new HtmlEncoder();
                HandlebarsHelpers.Register(handlebarsContext, Category.String, Category.DateTime, Category.Math, Category.Url, Category.Boolean);
                var bodyTemplate = handlebarsContext.Compile(emailTemplateContent.Result.Description);
                rawBody = bodyTemplate(renderTokens);
            }
            else
            {
                rawBody = BuildDefaultDeliveryVerificationHtml(args.Tokens);
            }
        }
        else if (emailTemplateContent?.Result != null && !string.IsNullOrWhiteSpace(emailTemplateContent.Result.Description))
        {
            var handlebarsContext = Handlebars.Create();
            handlebarsContext.Configuration.TextEncoder = new HtmlEncoder();
            HandlebarsHelpers.Register(handlebarsContext, Category.String, Category.DateTime, Category.Math, Category.Url, Category.Boolean);
            var subjectTemplate = handlebarsContext.Compile(args.Subject ?? string.Empty);
            var bodyTemplate = handlebarsContext.Compile(emailTemplateContent.Result.Description);
            subject = subjectTemplate(renderTokens);
            rawBody = bodyTemplate(renderTokens);
        }
        else
        {
            _logger.LogWarning("[EmailSendJob] Template {TemplateName} not found.", args.TemplateName);
            return;
        }

        var body = HtmlHelper.ModifyEmailContentImages(_fileHelper, rawBody);
        var markupBody = HtmlHelper.MarkAsHtml(body)?.Value ?? string.Empty;

        var attachments = new List<NameValue<string?, byte[]>>();
        if (args.Attachments?.Any() == true)
        {
            attachments.AddRange(args.Attachments);
        }

        _logger.LogInformation("[EmailSendJob] Sending receipt email to: {ToAddress}, subject: {Subject}", args.ToAddress, subject);
        try
        {
            await _emailSender.SendEmailAsync(
                subject,
                markupBody,
                args.ToAddress,
                args.ToName,
                args.FromAddress,
                args.FromName,
                args.ReplyToAddress,
                args.ReplyToName,
                args.Bcc,
                args.Cc,
                attachments,
                args.Headers
            );
            _logger.LogInformation("[EmailSendJob] Receipt email sent successfully to: {ToAddress}", args.ToAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EmailSendJob] SMTP send failed to {ToAddress}: {Message}", args.ToAddress, ex.Message);
            throw;
        }
    }

    private static string BuildDefaultPaymentReceiptHtml(object? tokens)
    {
        var d = ToStringDict(tokens);
        // Şirket bilgileri appsettings (Company:*) ile doldurulur
        var companyName = d.GetValueOrDefault("CompanyName") ?? "Şirket";
        var companyAddress = d.GetValueOrDefault("CompanyAddress") ?? "";
        var companyVatName = d.GetValueOrDefault("CompanyVatName") ?? "";
        var companyVat = d.GetValueOrDefault("CompanyVat") ?? "";
        var makbuzNo = d.GetValueOrDefault("MakbuzNo") ?? "";
        var customerName = d.GetValueOrDefault("CustomerName") ?? "";
        var customerCode = d.GetValueOrDefault("CustomerCode") ?? "";
        var transactionDate = d.GetValueOrDefault("TransactionDate") ?? "";
        var amount = d.GetValueOrDefault("Amount") ?? "0,00 ₺";
        var description = d.GetValueOrDefault("Description") ?? "Tahsilat";
        var paymentTypeName = d.GetValueOrDefault("PaymentTypeName") ?? "Nakit/Kredi Kartı";
        var companyBlock = string.IsNullOrWhiteSpace(companyAddress) && string.IsNullOrWhiteSpace(companyVatName) && string.IsNullOrWhiteSpace(companyVat)
            ? ""
            : $"<div class=\"footer\">{companyName}{(!string.IsNullOrWhiteSpace(companyAddress) ? $" — {companyAddress}" : "")}{(!string.IsNullOrWhiteSpace(companyVatName) ? $" — {companyVatName}: {companyVat}" : "")}</div>";
        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""/><style>body{{font-family:Segoe UI,Arial,sans-serif;padding:20px;color:#333;}} 
h1{{color:#1a5276;font-size:18px;}} .row{{margin:8px 0;}} .label{{font-weight:bold;}} .footer{{margin-top:24px;font-size:12px;color:#666;}}</style></head>
<body>
<h1>{companyName} — Tahsilat Makbuzu</h1>
{(!string.IsNullOrWhiteSpace(makbuzNo) ? $"<div class=\"row\"><span class=\"label\">Makbuz No:</span> {makbuzNo}</div>" : "")}
<div class=""row""><span class=""label"">Cari Unvan:</span> {customerName}</div>
<div class=""row""><span class=""label"">Cari Kodu:</span> {customerCode}</div>
<div class=""row""><span class=""label"">İşlem Tarihi:</span> {transactionDate}</div>
<div class=""row""><span class=""label"">Ödeme Türü:</span> {paymentTypeName}</div>
<div class=""row""><span class=""label"">Açıklama:</span> {description}</div>
<div class=""row""><span class=""label"">Tahsil Edilen Tutar:</span> <strong>{amount}</strong></div>
{companyBlock}
<div class=""footer"">Bu e-posta otomatik olarak gönderilmiştir.</div>
</body></html>";
    }

    private static string BuildDefaultDeliveryVerificationHtml(object? tokens)
    {
        var d = ToStringDict(tokens);
        var fullName = d.GetValueOrDefault("FullName") ?? "Müşteri";
        var orderNumber = d.GetValueOrDefault("OrderNumber") ?? "";
        var code = d.GetValueOrDefault("Code") ?? d.GetValueOrDefault("Url") ?? "";
        var logoUrl = d.GetValueOrDefault("LogoUrl") ?? "https://yedeksen.com/assets/images/logo/yedeksen.png";
        return $@"<!DOCTYPE html><html><head><meta charset=""utf-8""/><style>body{{font-family:Segoe UI,Arial,sans-serif;padding:20px;color:#333;}} 
h1{{color:#1a5276;font-size:18px;}} .code-box{{background:#27ae60;color:#fff;padding:16px 24px;border-radius:8px;font-size:24px;font-weight:bold;margin:16px 0;display:inline-block;}} 
.footer{{margin-top:24px;font-size:12px;color:#666;}}</style></head>
<body>
<img src=""{logoUrl}"" alt=""Logo"" style=""max-width:120px;margin-bottom:16px;""/>
<h1>Merhaba {fullName},</h1>
<p>Sipariş <strong>{orderNumber}</strong> teslim edilmek üzeredir. Kurye teslimatı onaylamak için aşağıdaki 4 haneli kodu kurye ile paylaşınız:</p>
<div class=""code-box"">Teslimat Doğrulama Kodunuz: {code}</div>
<p>Bu kod 15 dakika geçerlidir.</p>
<div class=""footer"">Bu e-posta otomatik olarak gönderilmiştir.</div>
</body></html>";
    }

    /// <summary>Bicops tasarımı: mor başlık, BICOPS markası. Aktivasyon linki veya (alt kullanıcı) üretilen parola ile giriş bilgisi.</summary>
    private static string BuildDefaultNewUserHtml(object? tokens)
    {
        var d = ToStringDict(tokens);
        var fullName = d.GetValueOrDefault("FullName") ?? "Kullanıcı";
        var url = d.GetValueOrDefault("Url") ?? "#";
        var verifyButton = d.GetValueOrDefault("VerifyButton") ?? "Hesabı Aktifleştir";
        var supportEmail = d.GetValueOrDefault("SupportEmail") ?? "destek@bicops.tr";
        var generatedPassword = d.GetValueOrDefault("GeneratedPassword");
        var userName = d.GetValueOrDefault("UserName") ?? "";
        const string primaryColor = "#6A1B9A";
        const string primaryLight = "#9C27B0";

        var isSubUser = !string.IsNullOrEmpty(generatedPassword);
        var headerSub = isSubUser ? "Hesabınız Oluşturuldu — Giriş Bilgileriniz" : "Hesabınız Başarıyla Oluşturuldu";

        string bodyBlock;
        if (isSubUser)
        {
            bodyBlock = $@"<p class=""greeting"">{fullName}, hoş geldiniz!</p>
      <p class=""text"">Hesabınız oluşturuldu. Giriş yapmak için aşağıdaki bilgileri kullanabilirsiniz. İlk girişten sonra parolanızı değiştirmeniz önerilir.</p>
      <div style=""background:#f5f5f5; border-radius:10px; padding:16px; margin:16px 0;"">
        <p style=""margin:0 0 8px; font-size:14px; color:#5A5A5A;""><strong>E-posta:</strong> {userName}</p>
        <p style=""margin:0; font-size:14px; color:#5A5A5A;""><strong>Geçici parola:</strong> <code style=""background:#e0e0e0; padding:4px 8px; border-radius:6px; font-size:15px;"">{generatedPassword}</code></p>
      </div>
      <p style=""margin: 8px 0 24px;""><a href=""{url}"" class=""btn"">{verifyButton}</a></p>";
        }
        else
        {
            bodyBlock = $@"<p class=""greeting"">{fullName}, hoş geldiniz!</p>
      <p class=""text"">Hesabınız başarıyla oluşturuldu. Aşağıdaki butona tıklayarak hesabınızı aktif edebilirsiniz.</p>
      <p style=""margin: 8px 0 24px;""><a href=""{url}"" class=""btn"">{verifyButton}</a></p>";
        }

        return $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8""/>
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0""/>
  <style>
    body {{ font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif; margin: 0; padding: 0; background: #f5f5f5; color: #2C2C2C; }}
    .wrap {{ max-width: 520px; margin: 0 auto; background: #fff; }}
    .header {{ background: linear-gradient(135deg, {primaryColor} 0%, {primaryLight} 100%); padding: 28px 24px; text-align: center; }}
    .brand {{ font-size: 26px; font-weight: 800; color: #fff; letter-spacing: 2px; margin: 0; }}
    .brand-sub {{ font-size: 12px; color: rgba(255,255,255,0.85); margin-top: 4px; }}
    .body {{ padding: 28px 24px; }}
    .greeting {{ font-size: 18px; font-weight: 600; color: #2C2C2C; margin-bottom: 12px; }}
    .text {{ font-size: 15px; line-height: 1.5; color: #5A5A5A; margin-bottom: 24px; }}
    .btn {{ display: inline-block; background: {primaryLight}; color: #fff !important; text-decoration: none; padding: 14px 32px; border-radius: 10px; font-size: 16px; font-weight: 700; }}
    .footer {{ padding: 20px 24px; font-size: 13px; color: #8E8E93; border-top: 1px solid #eee; }}
    .footer a {{ color: {primaryLight}; }}
  </style>
</head>
<body>
  <div class=""wrap"">
    <div class=""header"">
      <p class=""brand"">BICOPS</p>
      <p class=""brand-sub"">{headerSub}</p>
    </div>
    <div class=""body"">
      {bodyBlock}
    </div>
    <div class=""footer"">Herhangi bir sorunuz varsa <a href=""mailto:{supportEmail}"">{supportEmail}</a> adresine yazabilirsiniz.</div>
  </div>
</body>
</html>";
    }

    private static Dictionary<string, string> ToStringDict(object? obj)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (obj == null) return result;
        if (obj is IDictionary dict)
        {
            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key?.ToString();
                if (string.IsNullOrEmpty(key)) continue;
                result[key] = entry.Value?.ToString() ?? "";
            }
        }
        return result;
    }

    private object? ConvertToPlainObject(object? obj)
    {
        if (obj == null) return null;

        // If it's already an IDictionary (like EmailTokenDictionary or NestedDictionary)
        if (obj is IDictionary dict)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object?>)expando;
            foreach (DictionaryEntry entry in dict)
            {
                var key = entry.Key.ToString() ?? string.Empty;
                expandoDict[key] = ConvertToPlainObject(entry.Value);
            }
            return expando;
        }

        // Handle enumerables (lists, arrays) but skip strings
        if (obj is IEnumerable enumerable && !(obj is string))
        {
            var list = new List<object?>();
            foreach (var item in enumerable)
            {
                list.Add(ConvertToPlainObject(item));
            }
            return list;
        }

        // Basic types (strings, numbers, etc.)
        // Also handle anonymous objects by reflecting properties if needed, 
        // but since we usually pass dictionaries or anonymous objects that get boxed, we check if it's a "class"
        var type = obj.GetType();
        if (!type.IsPrimitive && !(obj is string) && type.IsClass)
        {
            var expando = new ExpandoObject();
            var expandoDict = (IDictionary<string, object?>)expando;
            foreach (var prop in type.GetProperties())
            {
                expandoDict[prop.Name] = ConvertToPlainObject(prop.GetValue(obj));
            }
            return expando;
        }

        return obj;
    }
}