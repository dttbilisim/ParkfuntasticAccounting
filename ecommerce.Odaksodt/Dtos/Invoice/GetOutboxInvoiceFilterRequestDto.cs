namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Giden fatura filtreli liste request DTO'su
/// </summary>
public class GetOutboxInvoiceFilterRequestDto
{
    public string? DocNo { get; set; }
    public string? AccountName { get; set; }
    public string? Identifier { get; set; }
    public List<string>? Profile { get; set; }
    public List<string>? InvoiceType { get; set; }
    public string? Ettn { get; set; }
    public bool? IsRead { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsDetail { get; set; } = true;
    public List<string>? RefNoList { get; set; }
    public int PageCount { get; set; }
    public int PageSize { get; set; } = 50;
    public string? CompanyIdentifier { get; set; }
}
