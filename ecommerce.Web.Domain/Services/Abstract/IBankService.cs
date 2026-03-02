using ecommerce.Core.Utils.ResultSet;
using ecommerce.Web.Domain.Dtos.Bank;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IBankService
{
    Task<IActionResult<List<BankListDto>>> GetActiveBanksAsync();
    Task<IActionResult<List<BankCardListDto>>> GetBankCardsAsync(int bankId);
    Task<IActionResult<List<BankInstallmentListDto>>> GetBankInstallmentsAsync(int cardId);
}
