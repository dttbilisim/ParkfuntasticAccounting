using ecommerce.Admin.Domain.Dtos.UnitDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface IUnitService
{
    Task<IActionResult<Paging<IQueryable<UnitListDto>>>> GetUnits(PageSetting pager);
    Task<IActionResult<List<UnitListDto>>> GetUnits();
    Task<IActionResult<UnitUpsertDto>> GetUnitById(int id);
    Task<IActionResult<Empty>> UpsertUnit(AuditWrapDto<UnitUpsertDto> model);
    Task<IActionResult<Empty>> DeleteUnit(AuditWrapDto<UnitDeleteDto> model);
}
