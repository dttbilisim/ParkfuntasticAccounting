using ecommerce.Core.Entities;

namespace ecommerce.Web.Domain.Dtos.Bank;

public class BankInstallmentListDto
{
    public int Id { get; set; }
    public int Installment { get; set; }
    public decimal InstallmentRate { get; set; }
    public int CreditCardId { get; set; }
}
