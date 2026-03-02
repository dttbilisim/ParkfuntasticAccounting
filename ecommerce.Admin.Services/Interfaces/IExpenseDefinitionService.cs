using ecommerce.Admin.Domain.Dtos.ExpenseDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IExpenseDefinitionService
    {
        Task<IActionResult<Paging<IQueryable<ExpenseDefinitionListDto>>>> GetMainExpenses(PageSetting pager, ExpenseOperationType operationType);
        Task<IActionResult<List<ExpenseDefinitionListDto>>> GetMainExpenses(ExpenseOperationType operationType);
        Task<IActionResult<List<ExpenseDefinitionListDto>>> GetSubExpenses(int parentId);
        Task<IActionResult<ExpenseDefinitionUpsertDto>> GetExpenseById(int id);
        Task<IActionResult<Empty>> UpsertExpense(AuditWrapDto<ExpenseDefinitionUpsertDto> model);
        Task<IActionResult<Empty>> DeleteExpense(AuditWrapDto<ExpenseDeleteDto> model);
    }
}


