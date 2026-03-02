namespace ecommerce.Odaksodt.Dtos.Invoice;

/// <summary>
/// Giden fatura filtreli liste response DTO'su
/// </summary>
public class GetOutboxInvoiceFilterResponseDto
{
    public bool Status { get; set; }
    public string? Message { get; set; }
    public string? ExceptionMessage { get; set; }
    public List<OutboxInvoiceItemDto>? Data { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Giden fatura satır bilgisi
/// </summary>
public class OutboxInvoiceItemDto
{
    public string? DocNo { get; set; }
    public string? Ettn { get; set; }
    public string? AccountName { get; set; }
    public string? Identifier { get; set; }
    public string? DocStatus { get; set; }
    public DateTime? DocDate { get; set; }
    public DateTime? CreateDate { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Tax20 { get; set; }
    public decimal Tax18 { get; set; }
    public decimal Tax10 { get; set; }
    public decimal Tax9 { get; set; }
    public decimal Tax8 { get; set; }
    public decimal Tax1 { get; set; }
    public decimal PayableAmount { get; set; }
    public decimal TaxExclusiveAmount { get; set; }
    public decimal CurrencyRate { get; set; }
    public string? Profile { get; set; }
    public string? InvoiceType { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsRead { get; set; }
    public string? RefNo { get; set; }
}
