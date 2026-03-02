using ecommerce.Admin.Domain.Dtos.BannerDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IBannerService
    {
        public Task<IActionResult<Paging<List<BannerListDto>>>> GetBanners(PageSetting pager);
        public Task<IActionResult<List<BannerListDto>>> GetBanners();
        Task<int> GetBannerLastCount(BannerType bannertypeId);
        Task<IActionResult<Empty>> UpsertBanner(AuditWrapDto<BannerUpsertDto> model);
        Task<IActionResult<Empty>> DeleteBanner(AuditWrapDto<BannerDeleteDto> model);
        Task<IActionResult<BannerUpsertDto>> GetBannerById(int Id);
    }
}
