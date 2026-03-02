using ecommerce.Web.Domain.Dtos.Bank;

namespace ecommerce.Web.ViewModels;

public class BankInstallmentViewModel
{
    public int BankId { get; set; }
    public string BankName { get; set; }
    public string BankLogo { get; set; }
    public List<CardInstallmentViewModel> Cards { get; set; } = new();
}

public class CardInstallmentViewModel
{
    public int CardId { get; set; }
    public string CardName { get; set; }
    public List<BankInstallmentListDto> Installments { get; set; } = new();
}
