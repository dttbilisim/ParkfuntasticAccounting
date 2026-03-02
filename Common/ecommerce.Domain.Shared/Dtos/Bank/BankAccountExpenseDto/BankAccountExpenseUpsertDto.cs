namespace ecommerce.Domain.Shared.Dtos.Bank.BankAccountExpenseDto;

public class BankAccountExpenseUpsertDto
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public int MainExpenseId { get; set; }
    public int? SubExpenseId { get; set; }
}


