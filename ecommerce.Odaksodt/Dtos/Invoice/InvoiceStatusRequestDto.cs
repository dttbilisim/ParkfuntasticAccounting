namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Fatura durum sorgulama request DTO
/// </summary>
public class InvoiceStatusRequestDto
{
    /// <summary>
    /// ETTN listesi
    /// </summary>
    public List<string>? EttnList { get; set; }

    /// <summary>
    /// Referans numarası listesi
    /// </summary>
    public List<string>? RefNoList { get; set; }
}
