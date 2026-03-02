using ecommerce.Core.Utils;
using ecommerce.Web.Domain.Dtos.Order;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IUserOrderService
{
    Task<UserOrderHistoryDto> GetUserOrderHistoryAsync(OrderStatusType? orderStatus = null, int page = 1, int pageSize = 10);
    Task<(bool Success, string Message)> CancelOrder(int orderId, string description);
    /// <summary>Giriş yapan kullanıcının belirli bir ürün için geçmiş alışveriş kayıtlarını döner.</summary>
    Task<List<ProductPurchaseHistoryItemDto>> GetProductPurchaseHistoryAsync(int productId);
    /// <summary>Giriş yapan kullanıcının daha önce sipariş verdiği ürün ID'lerini döner (liste için badge göstermek üzere).</summary>
    Task<List<int>> GetPurchasedProductIdsAsync(IEnumerable<int> productIds);
}