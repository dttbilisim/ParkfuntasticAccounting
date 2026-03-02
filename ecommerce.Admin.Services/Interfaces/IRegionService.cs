using ecommerce.Admin.Domain.Dtos.RegionDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IRegionService
    {
        public Task<IActionResult<Paging<IQueryable<RegionListDto>>>> GetRegions(PageSetting pager);
        public Task<IActionResult<List<RegionListDto>>> GetRegions();
        public Task<IActionResult<Empty>> UpsertRegion(AuditWrapDto<RegionUpsertDto> model);
        public Task<IActionResult<Empty>> DeleteRegion(AuditWrapDto<RegionDeleteDto> model);
        public Task<IActionResult<RegionUpsertDto>> GetRegionById(int regionId);
    }
}



