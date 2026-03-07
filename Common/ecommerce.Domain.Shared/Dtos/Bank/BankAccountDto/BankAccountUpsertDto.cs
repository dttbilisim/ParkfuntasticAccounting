namespace ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto;

public class BankAccountUpsertDto
{
    public int Id { get; set; }
    public int? BankId { get; set; }
    public string? SystemCode { get; set; }
    public int? PaymentTypeId { get; set; }
    public int? CurrencyId { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? CardNumber { get; set; }
    public string Iban { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Active { get; set; } = true;
}


