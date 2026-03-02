using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ICargoService
    {
        public Task<IActionResult<Paging<IQueryable<CargoListDto>>>> GetCargoes(PageSetting pager);
        public Task<IActionResult<List<CargoListDto>>> GetCargoes();
        Task<IActionResult<int>> UpsertCargo(AuditWrapDto<CargoUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCargo(AuditWrapDto<CargoDeleteDto> model);
        Task<IActionResult<CargoUpsertDto>> GetCargoById(int Id);
    }
}
