using ecommerce.Admin.Domain.Dtos.CargoPropertyDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ICargoPropertyService
    {
        public Task<IActionResult<Paging<IQueryable<CargoPropertyListDto>>>> GetCargoProperties(PageSetting pager);
        public Task<IActionResult<List<CargoPropertyListDto>>> GetCargoProperties(int cargoId);
        Task<IActionResult<Empty>> UpsertCargoProperty(AuditWrapDto<CargoPropertyUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCargoProperty(AuditWrapDto<CargoPropertyDeleteDto> model);
        Task<IActionResult<CargoPropertyUpsertDto>> GetCargoPropertyById(int Id);
    }
}
