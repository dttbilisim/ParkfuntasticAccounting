namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Gelen fatura listesi request DTO'su
/// </summary>
public class GetInboxInvoiceRequestDto
{
    public bool IsDetail { get; set; } = true;
    public int PageCount { get; set; }
    public int PageSize { get; set; } = 50;
    public string? CompanyIdentifier { get; set; }
}
