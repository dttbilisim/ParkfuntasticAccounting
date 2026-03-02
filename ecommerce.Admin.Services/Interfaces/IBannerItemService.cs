using ecommerce.Admin.Domain.Dtos.BannerItemDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IBannerItemService
    {
        public Task<IActionResult<Paging<IQueryable<BannerItemListDto>>>> GetBannerItems(PageSetting pager);
        public Task<IActionResult<List<BannerItemListDto>>> GetBannerItems();        
        Task<IActionResult<Empty>> UpsertBannerItem(AuditWrapDto<BannerItemUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBannerItem(AuditWrapDto<BannerItemDeleteDto> model);
        Task<IActionResult<BannerItemUpsertDto>> GetBannerItemById(int BannerItemId);
        Task<int> GetBannerItemLastCount(int bannerId);
    }
}
