using ecommerce.Admin.Domain.Dtos.TierDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface ITierService
    {
        public Task<IActionResult<Paging<IQueryable<TierListDto>>>> GetTiers(PageSetting pager);
        public Task<IActionResult<List<TierListDto>>> GetTiers();
        public Task<IActionResult<Empty>> UpsertTier(AuditWrapDto<TierUpsertDto> model);
        public Task<IActionResult<Empty>> DeleteTier(AuditWrapDto<TierDeleteDto> model);
        public Task<IActionResult<TierUpsertDto>> GetTiersById(int categoryId);
    }
}
