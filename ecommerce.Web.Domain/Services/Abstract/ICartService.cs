using ecommerce.Core.Entities;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Banners;
using ecommerce.Web.Domain.Dtos.Cart;
namespace ecommerce.Web.Domain.Services.Abstract;
public interface ICartService{
    Task<IActionResult<CartDto>> CreateCartItem(CartItemUpsertDto req);
    Task<IActionResult<CartDto>> GetCart(CartCustomerSavedPreferences? preferences = null);
    Task<IActionResult<CartDto>> CartItemRemove(int Id);
    Task<IActionResult<CartDto>> ClearCart();
    Task<IActionResult<CartDto>> PassiveCartItemBySellerId(int sellerId, bool status);
    Task<IActionResult<CartDto>> PassiveCartItemByProductSellerItemId(int productSellerItemId, bool status);
}
