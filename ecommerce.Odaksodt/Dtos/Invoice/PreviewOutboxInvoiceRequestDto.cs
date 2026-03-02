namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Giden fatura önizleme request DTO'su
/// </summary>
public class PreviewOutboxInvoiceRequestDto
{
    public string Ettn { get; set; } = "";
    public bool IsDefaultXslt { get; set; } = true;
    public string? CompanyIdentifier { get; set; }
}
