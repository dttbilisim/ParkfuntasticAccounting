using ecommerce.Admin.Domain.Dtos.WarehouseShelfDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IWarehouseShelfService
    {
        Task<IActionResult<Paging<List<WarehouseShelfListDto>>>> GetShelves(PageSetting pager);
        Task<IActionResult<List<WarehouseShelfListDto>>> GetShelvesByWarehouse(int warehouseId);
        Task<IActionResult<Empty>> UpsertShelf(AuditWrapDto<WarehouseShelfUpsertDto> model);
        Task<IActionResult<Empty>> DeleteShelf(AuditWrapDto<WarehouseShelfDeleteDto> model);
        Task<IActionResult<WarehouseShelfUpsertDto>> GetShelfById(int Id);
        Task<IActionResult<Empty>> BatchCreateShelves(AuditWrapDto<WarehouseShelfBatchCreateDto> model);
        Task<IActionResult<Empty>> BatchDeleteShelves(AuditWrapDto<WarehouseShelfBatchDeleteDto> model);
    }
}
