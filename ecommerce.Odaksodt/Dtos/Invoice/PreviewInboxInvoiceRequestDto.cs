namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Gelen fatura önizleme request DTO'su
/// </summary>
public class PreviewInboxInvoiceRequestDto
{
    public string Ettn { get; set; } = "";
    public string? RefNo { get; set; }
    public string? FileType { get; set; }
    public string? CompanyIdentifier { get; set; }
}
