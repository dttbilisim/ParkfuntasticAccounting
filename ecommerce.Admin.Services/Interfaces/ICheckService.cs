using ecommerce.Admin.Domain.Dtos.CheckDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICheckService
    {
        Task<IActionResult<Paging<List<CheckListDto>>>> GetPaged(
            PageSetting pager,
            int? bankId = null,
            int? customerId = null,
            CheckStatus? checkStatus = null,
            DateTime? dueDateStart = null,
            DateTime? dueDateEnd = null);

        Task<IActionResult<CheckUpsertDto>> GetById(int id);
        Task<IActionResult<int>> Create(AuditWrapDto<CheckUpsertDto> model);
        Task<IActionResult<Empty>> Update(AuditWrapDto<CheckUpsertDto> model);
        Task<IActionResult<Empty>> Delete(AuditWrapDto<CheckDeleteDto> model);
    }

    public interface IBankBranchService
    {
        /// <summary>Dropdown için: isteğe bağlı banka ve il filtresi (tenant bağımsız master data)</summary>
        Task<IActionResult<List<BankBranchListDto>>> GetList(int? bankId = null, int? cityId = null);
    }
}
