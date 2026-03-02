using ecommerce.Core.Utils.ResultSet;
using ecommerce.Web.Domain.Dtos;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IDiscountService
{
    Task<IActionResult<List<DiscountDto>>> GetActiveDiscountsAsync();
    Task<IActionResult<DiscountDto>> GetDiscountByIdAsync(int id);
}
