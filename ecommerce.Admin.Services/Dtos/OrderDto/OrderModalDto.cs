using ecommerce.Core.Entities;
using ecommerce.Core.Utils;

namespace ecommerce.Admin.Domain.Dtos.OrderDto
{
    /// <summary>Paket ürün alt kalemi (düzenlenebilir miktar).</summary>
    public class OrderPackageItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
    }

    /// <summary>Sipariş modalında ürün güncellemesi için DTO (miktar, fiyat).</summary>
    public class OrderItemUpdateDto
    {
        public int OrderItemId { get; set; }
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal? DiscountAmount { get; set; }
        /// <summary>Paket ürün mü?</summary>
        public bool IsPackageProduct { get; set; }
        /// <summary>Paket ürünler için sipariş/ziyaret tarihi.</summary>
        public DateTime? ShipmentDate { get; set; }
        /// <summary>Paket ürünler için alt ürünler (miktarlar düzenlenebilir).</summary>
        public List<OrderPackageItemDto> PackageProductItems { get; set; } = new();
    }

    /// <summary>Sipariş modal detayı (OrderPendingModal, OrderApprovedModal için).</summary>
    public class OrderDetailModalDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public OrderStatusType OrderStatusType { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CustomerName { get; set; }
        public string? Voucher { get; set; }
        public string? GuideName { get; set; }
        public decimal GrandTotal { get; set; }
        public List<OrderItemUpdateDto> Items { get; set; } = new();
        /// <summary>Plasiyer ise miktar/fiyat düzenlenebilir.</summary>
        public bool IsPlasiyer { get; set; }
    }
}
