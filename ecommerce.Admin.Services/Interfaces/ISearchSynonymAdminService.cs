using ecommerce.Admin.Domain.Dtos.SearchSynonymDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ISearchSynonymAdminService
    {
        Task<IActionResult<Paging<List<SearchSynonymListDto>>>> GetSynonyms(PageSetting pager);
        Task<IActionResult<SearchSynonymUpsertDto>> GetSynonymById(int id);
        Task<IActionResult<Empty>> UpsertSynonym(AuditWrapDto<SearchSynonymUpsertDto> model);
        Task<IActionResult<Empty>> DeleteSynonym(AuditWrapDto<SearchSynonymDeleteDto> model);
    }
}
