namespace ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto;

public class BankAccountListDto
{
    public int Id { get; set; }
    public int? BankId { get; set; }
    public string? BankName { get; set; }
    public string SystemCode { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public int? CurrencyId { get; set; }
    public string? CurrencyName { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string Iban { get; set; } = string.Empty;
    public bool Active { get; set; }
}


