namespace ecommerce.Web.Domain.Dtos.Order;

public class UserOrderHistoryDto
{
    public List<SellerOrderGroupDto> SellerGroups { get; set; } = new();
    public int TotalCount { get; set; } // Toplam sipariş sayısı
    public int CurrentPage { get; set; } // Mevcut sayfa
    public int PageSize { get; set; } // Sayfa başına sipariş
    public Dictionary<string, int> StatusCounts { get; set; } = new(); // Statü bazlı sayılar

    public List<OrderDto> GetAllOrders()
    {
        return SellerGroups
            .SelectMany(g => g.Orders.Select(o => { o.SellerName = g.SellerName; o.SellerId = g.SellerId; return o; }))
            .OrderByDescending(o => o.CreatedDate)
            .ToList();
    }
}

public class SellerOrderGroupDto
{
    public int SellerId { get; set; }
    public string SellerName { get; set; }
    public List<OrderDto> Orders { get; set; } = new();
}

public class OrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public string OrderStatus { get; set; }
    public DateTime CreatedDate { get; set; }
    public decimal ProductTotal { get; set; }        // Ara Toplam (ürünler)
    public decimal? DiscountTotal { get; set; }      // Toplam İndirim
    public decimal CargoPrice { get; set; }          // Kargo Ücreti
    public decimal OrderTotal { get; set; }          // Sipariş Toplamı (ürünler + kargo - indirim)
    public decimal GrandTotal { get; set; }          // Genel Toplam (ödenen)
    public string? CargoTrackNumber { get; set; }
    public string? CargoTrackUrl { get; set; }
    public string CargoName { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }
    public bool PaymentStatus { get; set; }
    public int SellerId { get; set; }
    public string SellerName { get; set; }
    public ecommerce.Core.Entities.Authentication.UserAddress? DeliveryAddress { get; set; }
    
    // Bank & Payment Information
    public ecommerce.Core.Entities.Bank? Bank { get; set; }
    public ecommerce.Core.Entities.BankCard? BankCard { get; set; }
    public string? PaymentId { get; set; }
    public string? PaymentToken { get; set; }
    public int? Installment { get; set; }
    public string? CardBinNumber { get; set; }
    public string? CardType { get; set; }
    public ecommerce.Core.Utils.OrderStatusType OrderStatusType { get; set; }
    
    /// <summary>Kurye teslimat seçildiyse atanmış kurye id.</summary>
    public int? CourierId { get; set; }
    /// <summary>Kurye adı soyadı (teslimat ekranı için).</summary>
    public string? CourierName { get; set; }
    /// <summary>Kurye plakası (teslimat ekranı için).</summary>
    public string? CourierLicensePlate { get; set; }
    /// <summary>Kurye araç tipi (0=Motosiklet, 1=Bisiklet, 2=Otomobil, 3=Kamyonet).</summary>
    public int? CourierVehicleType { get; set; }
    /// <summary>Teslimat tipi: 0=Kargo, 1=Kurye.</summary>
    public int? DeliveryOptionType { get; set; }
    /// <summary>Kurye ile tahmini teslimat süresi (dakika).</summary>
    public int? EstimatedCourierDeliveryMinutes { get; set; }
    
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int BrandId { get; set; }
    public string ProductName { get; set; }
    public string ProductImage { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal? DiscountAmount { get; set; }
    
    // Cargo information (Migrated from Order level)
    public string? CargoTrackNumber { get; set; }
    public string? CargoTrackUrl { get; set; }
    public string? CargoExternalId { get; set; }
    public DateTime? ShipmentDate { get; set; }
    
    // Product Image specifics
    public string? DocumentUrl { get; set; }
    public string? ProductFileGuid { get; set; }
}

/// <summary>
/// Ürün bazlı geçmiş alışveriş kaydı — ürün arama listesinde "geçmiş alışverişlerim" için.
/// </summary>
public class ProductPurchaseHistoryItemDto
{
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderCreatedDate { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public ecommerce.Core.Utils.OrderStatusType OrderStatusType { get; set; }
    public string SellerName { get; set; } = string.Empty;
}