using ecommerce.Admin.Domain.Dtos.HierarchicalDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface ICorporationService
    {
        Task<IActionResult<Paging<IQueryable<CorporationListDto>>>> GetCorporations(PageSetting pager);
        Task<IActionResult<List<CorporationListDto>>> GetAllActiveCorporations();
        Task<IActionResult<CorporationUpsertDto>> GetCorporationById(int id);
        Task<IActionResult<Empty>> UpsertCorporation(AuditWrapDto<CorporationUpsertDto> model);
        Task<IActionResult<Empty>> DeleteCorporation(int id, int userId);
    }
}
