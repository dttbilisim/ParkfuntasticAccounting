namespace ecommerce.Web.Domain.Dtos.Cart;
public class CartItemUpsertDto
{
    public int ProductSellerItemId { get; set; }

    public int Quantity { get; set; }
    public string? SourceId { get; set; }
    
    /// <summary>
    /// Plasiyer rolü için seçili cari ID'si (opsiyonel)
    /// </summary>
    public int? CustomerId { get; set; }
    
    /// <summary>
    /// ProductSellerItemId bilinmediğinde ProductId ile otomatik çözümleme yapılır.
    /// Öne çıkan ürünler gibi sipariş geçmişinden gelen ürünler için kullanılır.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>Paket ürünler için voucher kodu (modalda girilir).</summary>
    public string? Voucher { get; set; }

    /// <summary>Paket ürünler için rehber/acenta ismi (modalda girilir).</summary>
    public string? GuideName { get; set; }

    /// <summary>Paket ürünler için ziyaret/etkinlik tarihi (zorunlu).</summary>
    public DateTime? VisitDate { get; set; }

    /// <summary>Paket ürünler için alt ürün miktarları (ProductId -> Quantity).</summary>
    public Dictionary<int, int>? PackageItemQuantities { get; set; }
}
