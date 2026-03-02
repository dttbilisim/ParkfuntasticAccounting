namespace ecommerce.Admin.Services.Dtos.Mobile.Bank;

public class BankListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
}

public class BankCardListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int BankId { get; set; }
}

public class BankInstallmentListDto
{
    public int Id { get; set; }
    public int Installment { get; set; }
    public decimal InstallmentRate { get; set; }
    public int CreditCardId { get; set; }
}
