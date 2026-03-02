namespace ecommerce.Web.Domain.Dtos.Order;

public class CardPaymentRequest
{
    public int? BankId { get; set; }
    public string? CardHolderName { get; set; }
    public string? CardNumber { get; set; }
    public string? ExpMonth { get; set; }
    public string? ExpYear { get; set; }
    public string? Cvv { get; set; }
    public int? BankCardId { get; set; }
    public int? InstallmentId { get; set; }
}
