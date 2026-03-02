namespace ecommerce.Admin.Domain.Dtos.CustomerAccountTransactionDto;

/// <summary>
/// Tahsilat makbuzu e-postası için gerekli veriler.
/// </summary>
public class PaymentReceiptDto
{
    /// <summary>
    /// Plasiyer bazlı otomatik tahsilat makbuz numarası (TM-{SalesPersonId}-{Year}-{Seq}).
    /// </summary>
    public string? MakbuzNo { get; set; }
    public string CustomerEmail { get; set; } = null!;
    public string CustomerName { get; set; } = null!;
    public string CustomerCode { get; set; } = null!;
    public DateTime TransactionDate { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string? PaymentTypeName { get; set; }
}
