namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Gelen fatura listesi response DTO'su
/// </summary>
public class GetInboxInvoiceResponseDto
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public string? ExceptionMessage { get; set; }
    public List<InboxInvoiceItemDto>? Data { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Gelen fatura satır bilgisi
/// </summary>
public class InboxInvoiceItemDto
{
    public string? DocNo { get; set; }
    public string? Ettn { get; set; }
    public string? AccountName { get; set; }
    public string? Identifier { get; set; }
    public string? DocStatus { get; set; }
    public DateTime? DocDate { get; set; }
    public DateTime? CreateDate { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal TaxExclusiveAmount { get; set; }
    public decimal CurrencyRate { get; set; }
    public string? Profile { get; set; }
    public string? InvoiceType { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsRead { get; set; }
    public string? RefNo { get; set; }
}
