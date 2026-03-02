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
}
