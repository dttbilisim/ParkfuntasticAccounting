namespace ecommerce.Domain.Shared.Dtos.Bank.BankAccountInstallmentDto;

public class BankAccountInstallmentListDto
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public int Installment { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal Amount { get; set; }
    public string Note { get; set; } = string.Empty;
    public bool Active { get; set; }
}


