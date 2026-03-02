using ecommerce.Domain.Shared.Dtos.Bank.BankAccountDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountExpenseDto;
using ecommerce.Domain.Shared.Dtos.Bank.BankAccountInstallmentDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface IBankAccountDefinitionService
{
    // Banka hesapları
    Task<IActionResult<Paging<List<BankAccountListDto>>>> GetBankAccounts(PageSetting pager);
    Task<IActionResult<BankAccountUpsertDto>> GetBankAccountById(int id);
    Task<IActionResult<Empty>> UpsertBankAccount(AuditWrapDto<BankAccountUpsertDto> model);
    Task<IActionResult<Empty>> DeleteBankAccount(AuditWrapDto<BankAccountDeleteDto> model);

    // Gider bağlantıları
    Task<IActionResult<List<BankAccountExpenseListDto>>> GetBankAccountExpenses(int bankAccountId);
    Task<IActionResult<Empty>> UpsertBankAccountExpense(AuditWrapDto<BankAccountExpenseUpsertDto> model);
    Task<IActionResult<Empty>> DeleteBankAccountExpense(AuditWrapDto<BankAccountExpenseDeleteDto> model);

    // Taksit seçenekleri
    Task<IActionResult<List<BankAccountInstallmentListDto>>> GetBankAccountInstallments(int bankAccountId);
    Task<IActionResult<Empty>> UpsertBankAccountInstallment(AuditWrapDto<BankAccountInstallmentUpsertDto> model);
    Task<IActionResult<Empty>> DeleteBankAccountInstallment(AuditWrapDto<BankAccountInstallmentDeleteDto> model);
}


