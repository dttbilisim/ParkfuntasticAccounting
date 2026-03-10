namespace ecommerce.Web.Domain.Dtos.Cart;

/// <summary>
/// Paket ürün bileşeni - sepette paket içeriğini göstermek için.
/// </summary>
public class CartPackageItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }

    /// <summary>Paket içinde bu ürünün miktarı (varsayılan 1).</summary>
    public int Quantity { get; set; } = 1;
}
