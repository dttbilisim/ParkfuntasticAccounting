using ecommerce.Admin.Domain.Dtos.ScaleUnitDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IScaleUnitService
    {
        public Task<IActionResult<Paging<IQueryable<ScaleUnitListDto>>>> GetScaleUnits(PageSetting pager);
        public Task<IActionResult<List<ScaleUnitListDto>>> GetScaleUnits();
        Task<IActionResult<Empty>> UpsertScaleUnit(AuditWrapDto<ScaleUnitUpsertDto> model);
        Task<IActionResult<Empty>> DeleteScaleUnit(AuditWrapDto<ScaleUnitDeleteDto> model);
        Task<IActionResult<ScaleUnitUpsertDto>> GetScaleUnitById(int Id);
    }
}
