using ecommerce.Domain.Shared.Dtos.Bank.BankDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankParameterDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCardDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardInstallmentDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankCreditCardPrefixDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Domain.Shared.Abstract
{
    public interface IBankService
    {
        Task<IActionResult<Paging<List<BankListDto>>>> GetBanks(PageSetting pager);
        Task<IActionResult<BankUpsertDto>> GetBankById(int id);
        Task<IActionResult<int>> UpsertBank(AuditWrapDto<BankUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBank(AuditWrapDto<BankDeleteDto> model);

        Task<IActionResult<Paging<List<BankParameterListDto>>>> GetBankParameters(PageSetting pager, int? bankId = null);
        Task<IActionResult<BankParameterUpsertDto>> GetBankParameterById(int id);
        Task<IActionResult<int>> UpsertBankParameter(AuditWrapDto<BankParameterUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBankParameter(AuditWrapDto<BankParameterDeleteDto> model);

        Task<IActionResult<Paging<List<BankCardListDto>>>> GetBankCards(PageSetting pager, int? bankId = null);
        Task<IActionResult<BankCardUpsertDto>> GetBankCardById(int id);
        Task<IActionResult<int>> UpsertBankCard(AuditWrapDto<BankCardUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBankCard(AuditWrapDto<BankCardDeleteDto> model);

        Task<IActionResult<Paging<List<BankCreditCardInstallmentListDto>>>> GetBankCreditCardInstallments(PageSetting pager, int? creditCardId = null);
        Task<IActionResult<BankCreditCardInstallmentUpsertDto>> GetBankCreditCardInstallmentById(int id);
        Task<IActionResult<int>> UpsertBankCreditCardInstallment(AuditWrapDto<BankCreditCardInstallmentUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBankCreditCardInstallment(AuditWrapDto<BankCreditCardInstallmentDeleteDto> model);

        Task<IActionResult<Paging<List<BankCreditCardPrefixListDto>>>> GetBankCreditCardPrefixes(PageSetting pager, int? creditCardId = null);
        Task<IActionResult<BankCreditCardPrefixUpsertDto>> GetBankCreditCardPrefixById(int id);
        Task<IActionResult<int>> UpsertBankCreditCardPrefix(AuditWrapDto<BankCreditCardPrefixUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBankCreditCardPrefix(AuditWrapDto<BankCreditCardPrefixDeleteDto> model);
    }
}