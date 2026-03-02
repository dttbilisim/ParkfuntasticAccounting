namespace ecommerce.Admin.Domain.Dtos.PaymentCollection;

/// <summary>
/// Plasiyer ödeme alma sonuç DTO'su
/// </summary>
public class CollectPaymentResultDto
{
    /// <summary>İşlem başarılı mı</summary>
    public bool Success { get; set; }

    /// <summary>Sonuç mesajı</summary>
    public string? Message { get; set; }

    /// <summary>Oluşturulan transaction ID</summary>
    public int? TransactionId { get; set; }

    /// <summary>3D Secure HTML formu (sanal POS için)</summary>
    public string? CheckoutFormContent { get; set; }

    /// <summary>3D Secure iframe URL'i</summary>
    public string? IframeUrl { get; set; }
}
