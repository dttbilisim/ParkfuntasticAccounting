using ecommerce.Admin.Domain.Dtos.BannerSubItemDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IBannerSubItemService
    {
        public Task<IActionResult<Paging<List<BannerSubItemListDto>>>> GetBannerSubItems(PageSetting pager, int banneritemId);
        public Task<IActionResult<List<BannerSubItemListDto>>> GetBannerSubItems();

        Task<IActionResult<Empty>> UpsertBannerSubItem(AuditWrapDto<BannerSubItemUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBannerSubItem(AuditWrapDto<BannerSubItemDeleteDto> model);
        Task<IActionResult<BannerSubItemUpsertDto>> GetBannerSubItemById(int Id);
    }
}
