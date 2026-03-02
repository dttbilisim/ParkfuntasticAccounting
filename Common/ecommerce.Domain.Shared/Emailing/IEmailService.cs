using ecommerce.Core.Entities;

namespace ecommerce.Domain.Shared.Emailing;

public interface IEmailService
{
    Task SendNewUserEmail(string fullname, string email, string activationToken);

    /// <summary>Alt kullanıcı (örn. kurye alt kullanıcısı): otomatik üretilen parola e-postada gönderilir, aktivasyon linki yok.</summary>
    Task SendNewSubUserEmail(string fullname, string email, string generatedPassword);

    Task SendNewUserTokenEmail(string fullname, string email, string token);

    Task SendEmailVerificationCodeEmail(string fullname, string email, string code);

    /// <summary>Kurye teslimat doğrulama kodu — müşteriye 4 haneli kod gönderir.</summary>
    Task SendDeliveryVerificationCodeEmail(string fullname, string email, string orderNumber, string code);

    Task SendPasswordResetEmail(string fullname, string email, string token);

    Task SendOrderPlacedSellerEmail(Orders order);

    Task SendOrderPlacedCustomerEmail(List<Orders> orders);

    Task SendOrderCancelRequestSellerEmail(Orders order);

    Task SendOrderCancelRequestCustomerEmail(Orders order);

    Task SendOrderShippedEmail(Orders order);
    Task SendOrderSuccessEmail(Orders order);

    Task SendOrderCancelledCustomerEmail(Orders order);
    Task SendOrderCancelledSellerEmail(Orders order);

    Task SendSupportRequestSystemEmail(SupportLine supportLine, string? topic = null);

    Task SendOrderCancelRequestSystemEmail(Orders order);

    Task SendOrderPlacedSystemEmail(Orders order);

    Task SendNewUserSystemEmail(string fullname, string email, DateTime registerDate);
    
    Task UserBirthDateEmail(string firstname, string lastname,string email);

    Task ZoomMeetCreate(string fullname, string subject,string text, DateTime meetDate,int duration,string email);

    /// <summary>
    /// Cariye tahsilat makbuzu e-postası gönderir (nakit/kredi kartı tahsilatları için).
    /// PDF eki verilirse e-postaya eklenir. makbuzNo plasiyer bazlı otomatik numara (TM-x-yyyy-nnnnn).
    /// </summary>
    Task SendPaymentReceiptEmail(string toEmail, string toName, string customerName, string customerCode, DateTime transactionDate, string? description, decimal amount, string? paymentTypeName, byte[]? pdfAttachment = null, string? pdfFileName = null, string? makbuzNo = null);

}
