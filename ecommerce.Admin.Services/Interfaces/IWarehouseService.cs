using ecommerce.Admin.Domain.Dtos.WarehouseDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IWarehouseService
    {
        Task<IActionResult<Paging<List<WarehouseListDto>>>> GetWarehouses(PageSetting pager);
        Task<IActionResult<List<WarehouseListDto>>> GetAllWarehouses();
        Task<IActionResult<Empty>> UpsertWarehouse(AuditWrapDto<WarehouseUpsertDto> model);
        Task<IActionResult<Empty>> DeleteWarehouse(AuditWrapDto<WarehouseDeleteDto> model);
        Task<IActionResult<WarehouseUpsertDto>> GetWarehouseById(int Id);
        Task<IActionResult<List<WarehouseListDto>>> GetWarehousesByBranchId(int branchId);
    }
}
