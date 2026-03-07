using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.PaymentCollection;

/// <summary>
/// Plasiyer ödeme alma isteği DTO'su
/// </summary>
public class CollectPaymentRequestDto
{
    /// <summary>Cari (müşteri) ID</summary>
    public int CustomerId { get; set; }

    /// <summary>Seçilen sipariş ID'leri (opsiyonel — sipariş bağlamadan direkt ödeme alınabilir)</summary>
    public List<int>? OrderIds { get; set; }

    /// <summary>Ödeme tipi (Nakit=1, KrediKarti=2)</summary>
    public BankPaymentType PaymentType { get; set; }

    /// <summary>Ödeme tutarı</summary>
    public decimal Amount { get; set; }

    /// <summary>Kasa ID — Nakit için zorunlu (PaymentTypeId=1), Kart için zorunlu (PaymentTypeId=2). Kasa tanımlarından doğru kasa seçilmelidir.</summary>
    public int? CashRegisterId { get; set; }

    /// <summary>Sanal POS için kart bilgileri</summary>
    public CardPaymentInfo? CardPayment { get; set; }
}

/// <summary>
/// Kart ödeme bilgileri — sanal POS işlemi için gerekli alanlar
/// </summary>
public class CardPaymentInfo
{
    public int? BankId { get; set; }
    public string? CardHolderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? Cvv { get; set; }
    public int? BankCardId { get; set; }
    public int? InstallmentId { get; set; }
}
