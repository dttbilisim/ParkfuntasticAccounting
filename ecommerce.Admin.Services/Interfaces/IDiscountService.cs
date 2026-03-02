using ecommerce.Admin.Domain.Dtos.DiscountDto;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IDiscountService
    {
        public Task<IActionResult<List<DiscountListDto>>> GetDiscounts();

        public Task<IActionResult<Paging<List<DiscountListDto>>>> GetDiscounts(PageSetting pager);

        Task<IActionResult<Empty>> UpsertDiscount(DiscountUpsertDto dto);

        Task<IActionResult<Empty>> DeleteDiscount(DiscountDeleteDto dto);

        Task<IActionResult<DiscountUpsertDto>> GetDiscountById(int Id);

        Task<string> GenerateCouponCode();

        Task<IActionResult<List<DiscountWithProductsDto>>> GetActiveDiscountsWithProductsAsync();
    }
}