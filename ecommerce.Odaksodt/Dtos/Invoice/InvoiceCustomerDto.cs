namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Fatura müşteri bilgileri DTO
/// </summary>
public class InvoiceCustomerDto
{
    /// <summary>
    /// Vergi kimlik numarası (VKN) veya TC kimlik numarası
    /// </summary>
    public string TaxNumber { get; set; } = string.Empty;

    /// <summary>
    /// Müşteri unvanı
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Vergi dairesi
    /// </summary>
    public string? TaxOffice { get; set; }

    /// <summary>
    /// Adres
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Şehir
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// İlçe
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// Ülke
    /// </summary>
    public string Country { get; set; } = "Türkiye";

    /// <summary>
    /// E-posta
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Telefon
    /// </summary>
    public string? Phone { get; set; }
}
