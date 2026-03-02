namespace ecommerce.Domain.Shared.Dtos.Bank.BankAccountExpenseDto;

public class BankAccountExpenseListDto
{
    public int Id { get; set; }
    public int BankAccountId { get; set; }
    public int MainExpenseId { get; set; }
    public string MainExpenseName { get; set; } = string.Empty;
    public int? SubExpenseId { get; set; }
    public string? SubExpenseName { get; set; }
}


