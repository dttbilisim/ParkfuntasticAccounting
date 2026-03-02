using ecommerce.Core.Utils.ResultSet;
using ecommerce.Web.Domain.Dtos.Order;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface ICheckoutService
{
    Task<IActionResult<CheckoutResultDto>> Checkout(CheckoutRequestDto request);
    Task<IActionResult<Empty>> OrderDelete(int? userId = null); 
    Task<IActionResult<Empty>> DeleteFailedOrders(List<string> orderNumbers); 
}