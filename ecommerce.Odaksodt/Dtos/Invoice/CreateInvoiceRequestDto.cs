namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Fatura oluşturma request DTO
/// </summary>
public class CreateInvoiceRequestDto
{
    /// <summary>
    /// Fatura tipi (SATIS, IADE, vb.)
    /// </summary>
    public string InvoiceType { get; set; } = string.Empty;

    /// <summary>
    /// Fatura senaryosu (TEMELFATURA, TICARIFATURA, vb.)
    /// </summary>
    public string InvoiceScenario { get; set; } = string.Empty;

    /// <summary>
    /// Fatura numarası
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Fatura tarihi
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Para birimi (TRY, USD, EUR, vb.)
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Alıcı bilgileri
    /// </summary>
    public InvoiceCustomerDto Customer { get; set; } = new();

    /// <summary>
    /// Fatura kalemleri
    /// </summary>
    public List<InvoiceLineDto> Lines { get; set; } = new();

    /// <summary>
    /// Notlar
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Referans numarası (kendi sisteminizdeki ID)
    /// </summary>
    public string? ReferenceNumber { get; set; }
}
