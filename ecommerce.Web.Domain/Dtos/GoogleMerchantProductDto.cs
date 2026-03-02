namespace ecommerce.Web.Domain.Dtos;

/// <summary>
/// DTO representing a Google Merchant Center product feed item
/// </summary>
public class GoogleMerchantProductDto
{
    /// <summary>
    /// Unique product identifier (SellerItemId)
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Product title (max 150 characters)
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Product description (max 5000 characters)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Product detail page URL
    /// </summary>
    public string Link { get; set; } = string.Empty;

    /// <summary>
    /// Main product image URL
    /// </summary>
    public string ImageLink { get; set; } = string.Empty;

    /// <summary>
    /// Price with currency (e.g., "5304.66 TRY")
    /// </summary>
    public string Price { get; set; } = string.Empty;

    /// <summary>
    /// Stock availability: "in stock" or "out of stock"
    /// </summary>
    public string Availability { get; set; } = string.Empty;

    /// <summary>
    /// Product condition: "new", "refurbished", or "used"
    /// </summary>
    public string Condition { get; set; } = "new";

    /// <summary>
    /// Brand name
    /// </summary>
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// GTIN/EAN/UPC barcode (optional)
    /// </summary>
    public string? Gtin { get; set; }
}
