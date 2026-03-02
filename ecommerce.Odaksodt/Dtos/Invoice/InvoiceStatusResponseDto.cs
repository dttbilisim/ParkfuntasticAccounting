namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Fatura durum sorgulama response DTO
/// </summary>
public class InvoiceStatusResponseDto
{
    /// <summary>
    /// Başarılı mı?
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Fatura durumları
    /// </summary>
    public List<InvoiceStatusItemDto> Invoices { get; set; } = new();

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Fatura durum item DTO
/// </summary>
public class InvoiceStatusItemDto
{
    /// <summary>
    /// ETTN
    /// </summary>
    public string Ettn { get; set; } = string.Empty;

    /// <summary>
    /// Fatura numarası
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Durum (TASLAK, GONDERILDI, ONAYLANDI, REDDEDILDI, vb.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Durum açıklaması
    /// </summary>
    public string? StatusDescription { get; set; }

    /// <summary>
    /// GİB durumu
    /// </summary>
    public string? GibStatus { get; set; }
}
