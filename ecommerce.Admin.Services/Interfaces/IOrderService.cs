using ecommerce.Admin.Domain.Dtos.OrderDto;
using ecommerce.Admin.Domain.Dtos.DashboardDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Core.Entities;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IOrderService
    {
        public Task<IActionResult<Paging<IQueryable<OrderListDto>>>> GetOrders(PageSetting pager);
        Task<IActionResult<Empty>> DeleteOrder(AuditWrapDto<OrderDeleteDto> model);
        Task<IActionResult<OrderUpsertDto>> GetOrderById(int Id);
        Task<List<KeyValuePair<string, int>>> OrderStatus();
        Task<IActionResult<OrderListDto>> GetOrderDetailById(int id);
        Task<IActionResult<Empty>> UpdateOrderStatus(AuditWrapDto<OrderStatusUpdateDto> model);

        // siparis faturalari icin
        Task<IActionResult<List<OrderInvoiceListDto>>> GetOrderInvoiceList(int orderId);
        Task<bool> UpsertOrderInvoice(OrderInvoiceUpsertDto input);
        Task<bool> DeleteOrderInvoice(int Id);
        
        // Dashboard methods
        Task<IActionResult<DashboardOrderSummaryDto>> GetDashboardOrderSummary();
        Task<IActionResult<List<DashboardBestSellingProductDto>>> GetBestSellingProducts(int topN);
        Task<IActionResult<List<DashboardSellerSalesDto>>> GetSalesBySeller(int topN);
        Task<IActionResult<List<DashboardChartDto>>> GetOrderStatsOverTime(int days);
        Task<Dictionary<int, string>> GetOrderItemWarehouseStocks(int orderId);
        Task<Dictionary<int, string>> GetOrderItemApiStocks(int orderId);
        Task<string?> GetProductCodeForCart(int sellerId, int sourceId);
        Task UpdateOrderItem(OrderItems item);
        
        // B2B: Get orders for current logged-in user
        Task<IActionResult<List<OrderListDto>>> GetMyOrders(int? userId = null);
        
        // B2B: Cancel order for current logged-in user
        Task<(bool Success, string Message)> CancelMyOrder(int orderId, int? userId = null);

        // Get unfactured orders (InvoiceId is null)
        Task<IActionResult<List<OrderListDto>>> GetUnfacturedOrders();

        Task<IActionResult<List<OrderListDto>>> GetOrdersByIds(List<int> ids);
        Task<IActionResult<List<OrderListDto>>> GetCustomerOrders(int customerId);
        
        // B2B: Get orders for all customers linked to this Plasiyer
        Task<IActionResult<List<OrderListDto>>> GetPlasiyerCustomersOrders(int userId);

        // Fatura oluşturma için sipariş ürünlerini doğrudan çek (Include sorunlarını bypass eder)
        Task<List<OrderItems>> GetOrderItemsDirectByOrderIds(List<int> orderIds);

        /// <summary>Seçili cari için belirli bir ürünün geçmiş alışveriş kayıtlarını döner.</summary>
        Task<List<ProductPurchaseHistoryItemDto>> GetProductPurchaseHistoryByCustomer(int customerId, int productId);

        /// <summary>Seçili carinin daha önce sipariş verdiği ürün ID'lerini döner (ürün listesinde geçmiş ikonu için).</summary>
        Task<List<int>> GetPurchasedProductIdsByCustomer(int customerId, IEnumerable<int> productIds);
    }
}
