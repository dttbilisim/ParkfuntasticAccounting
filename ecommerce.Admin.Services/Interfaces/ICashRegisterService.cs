using ecommerce.Admin.Domain.Dtos.CashRegisterDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICashRegisterService
    {
        Task<IActionResult<Paging<IQueryable<CashRegisterListDto>>>> GetCashRegisters(PageSetting pager);
        Task<IActionResult<List<CashRegisterListDto>>> GetCashRegisters();
        Task<IActionResult<CashRegisterUpsertDto>> GetCashRegisterById(int id);
        Task<IActionResult<Empty>> UpsertCashRegister(AuditWrapDto<CashRegisterUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCashRegister(AuditWrapDto<CashRegisterDeleteDto> model);
    }
}
