using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Banners;
namespace ecommerce.Web.Domain.Services.Abstract;
public interface IBannerService{
    Task<IActionResult<List<BannerItemDto>>> GetAllAsync(BannerType bannerType);
}
