using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.ViewModels
{
    public class UnfacturedOrderViewModel
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; }
        public DateTime CreatedDate { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } // B2B Cari adı
        public string BuyerName { get; set; }    // Alıcı adı (UserAddress.FullName)
        public decimal GrandTotal { get; set; }
        public OrderStatusType OrderStatusType { get; set; }
        public int ItemCount { get; set; }
        
        // Sipariş ürün detayları — accordion'da gösterilecek
        public List<UnfacturedOrderItemViewModel> Items { get; set; } = new();
    }

    // Sipariş satır detayı
    public class UnfacturedOrderItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
    }
}
