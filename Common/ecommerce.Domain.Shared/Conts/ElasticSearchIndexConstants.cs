namespace ecommerce.Domain.Shared.Conts;
public class ElasticSearchIndexConstants{
    public const string BannerItems = "banner_items";
    
    // Legacy (deprecated - kullanma!)
    public const string Products = "product_index"; 
    
    // Multi-Index Strategy (NEW - kullan!)
    public const string SellerProducts = "sellerproduct_index";
    public const string Images = "image_index";
    public const string Categories = "category_index";
    public const string Brands = "brand_index";
    public const string Companies = "companies";
}
