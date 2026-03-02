using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Dtos;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.Extensions.Configuration;
using Nest;

namespace ecommerce.Web.Domain.Services.Concreate;

/// <summary>
/// Google Merchant Center ürün feed'leri oluşturmak için servis
/// </summary>
public class GoogleMerchantService : IGoogleMerchantService
{
    private readonly IElasticSearchService _elasticSearchService;
    private readonly string _baseUrl;
    private readonly string _cdnBaseUrl;

    public GoogleMerchantService(
        IElasticSearchService elasticSearchService,
        IConfiguration configuration)
    {
        _elasticSearchService = elasticSearchService;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://yedeksen.com";
        _cdnBaseUrl = configuration["Cdn:BaseUrl"] ?? "https://cdn.yedeksen.com/images/";
    }

    /// <summary>
    /// Google Merchant feed için tüm aktif ürünleri getir
    /// </summary>
    public async Task<List<GoogleMerchantProductDto>> GetProductsForFeedAsync(int maxProducts = 50000)
    {
        var result = new List<GoogleMerchantProductDto>();
        
        try
        {
            // 1. Query active products from Elasticsearch using scroll API for large datasets
            var scrollTime = "2m"; // Scroll context timeout
            var pageSize = 1000; // Fetch 1000 products per scroll
            
            var searchResponse = await _elasticSearchService._client.SearchAsync<SellerProductElasticDto>(s => s
                .Index(ElasticSearchIndexConstants.SellerProducts)
                .Size(pageSize)
                .Scroll(scrollTime)
                .Query(q => q.Bool(b => b
                    .Must(
                        m => m.Term(t => t.Field(f => f.ProductStatus).Value(1)), // Active products
                        m => m.Term(t => t.Field(f => f.SellerStatus).Value(1))   // Active sellers
                    )
                ))
                .Sort(ss => ss.Descending(p => p.SellerModifiedDate))
            );

            if (!searchResponse.IsValid)
            {
                Console.WriteLine($"⚠️ Google Merchant Feed - Elasticsearch query failed: {searchResponse.OriginalException?.Message}");
                return result;
            }

            var scrollId = searchResponse.ScrollId;
            var products = searchResponse.Documents.ToList();
            
            // Process first batch
            var processedCount = 0;
            processedCount = await ProcessProductBatch(products, result, processedCount, maxProducts);

            // Continue scrolling for remaining products
            while (products.Any() && processedCount < maxProducts)
            {
                var scrollResponse = await _elasticSearchService._client.ScrollAsync<SellerProductElasticDto>(scrollTime, scrollId);
                
                if (!scrollResponse.IsValid || !scrollResponse.Documents.Any())
                {
                    break;
                }

                scrollId = scrollResponse.ScrollId;
                products = scrollResponse.Documents.ToList();
                
                processedCount = await ProcessProductBatch(products, result, processedCount, maxProducts);
            }

            // Clear scroll context
            if (!string.IsNullOrEmpty(scrollId))
            {
                await _elasticSearchService._client.ClearScrollAsync(c => c.ScrollId(scrollId));
            }

            Console.WriteLine($"✅ Google Merchant Feed - Generated feed for {result.Count} products");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Google Merchant Feed - Error: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// Bir ürün grubunu işle ve Google Merchant formatına çevir
    /// </summary>
    private async Task<int> ProcessProductBatch(
        List<SellerProductElasticDto> products, 
        List<GoogleMerchantProductDto> result, 
        int processedCount, 
        int maxProducts)
    {
        if (!products.Any()) return processedCount;

        // 1. Get unique IDs for joins
        var brandIds = products.Where(p => p.BrandId.HasValue).Select(p => p.BrandId!.Value).Distinct().ToList();
        var productIds = products.Select(p => p.ProductId).Distinct().ToList();

        // 2. Join with brand_index to get brand names
        var brands = await GetBrandsByIds(brandIds);
        var brandsDict = brands.Where(b => b.Id.HasValue).ToDictionary(b => b.Id!.Value, b => b);

        // 3. Join with image_index to get product images
        var images = await GetImagesByProductIds(productIds);
        var imagesDict = images.GroupBy(img => img.ProductId).ToDictionary(g => g.Key, g => g.First());

        // 4. Convert to Google Merchant format
        foreach (var product in products)
        {
            if (processedCount >= maxProducts) break;

            var merchantProduct = ConvertToGoogleMerchantProduct(product, brandsDict, imagesDict);
            if (merchantProduct != null)
            {
                result.Add(merchantProduct);
                processedCount++;
            }
        }

        return processedCount;
    }

    private GoogleMerchantProductDto? ConvertToGoogleMerchantProduct(
        SellerProductElasticDto product,
        Dictionary<int, BrandDto> brandsDict,
        Dictionary<int, ImageIndexDto> imagesDict)
    {
        try
        {
            // Build product URL
            var productId = product.SellerItemId > 0 ? product.SellerItemId : product.ProductId;
            var link = product.BrandId.HasValue
                ? $"{_baseUrl}/product-detail?productId={productId}&brandId={product.BrandId}"
                : $"{_baseUrl}/product-detail?productId={productId}";

            // Get image URL
            var imageLink = GetProductImageUrl(product, imagesDict);
            if (string.IsNullOrEmpty(imageLink))
            {
                // Skip products without images (Google requires image_link)
                return null;
            }

            // Get brand name
            var brandName = "Unknown";
            if (product.BrandId.HasValue && brandsDict.TryGetValue(product.BrandId.Value, out var brand))
            {
                brandName = brand.Name ?? "Unknown";
            }

            // Determine availability
            var availability = product.Stock > 0 ? "in stock" : "out of stock";

            // Format price
            var price = $"{product.SalePrice:F2} {product.Currency ?? "TRY"}";

            // Truncate title and description to Google's limits
            var title = TruncateString(product.ProductName ?? "Unknown Product", 150);
            var description = TruncateString(product.ProductDescription ?? product.ProductName ?? "No description", 5000);

            return new GoogleMerchantProductDto
            {
                Id = productId.ToString(),
                Title = title,
                Description = description,
                Link = link,
                ImageLink = imageLink,
                Price = price,
                Availability = availability,
                Condition = "new",
                Brand = brandName,
                Gtin = product.ProductBarcode
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error converting product {product.ProductId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// DocumentUrl veya image_index'ten ürün görsel URL'sini al
    /// </summary>
    private string? GetProductImageUrl(SellerProductElasticDto product, Dictionary<int, ImageIndexDto> imagesDict)
    {
        // Priority 1: Use DocumentUrl if available
        if (!string.IsNullOrEmpty(product.DocumentUrl))
        {
            return product.DocumentUrl;
        }

        // Priority 2: Use image from image_index
        if (imagesDict.ContainsKey(product.ProductId))
        {
            var image = imagesDict[product.ProductId];
            if (!string.IsNullOrEmpty(image.FileName))
            {
                return $"{_cdnBaseUrl}ProductImages/{image.FileName}";
            }
        }

        return null;
    }

    /// <summary>
    /// brand_index'ten ID'lere göre markaları getir
    /// </summary>
    private async Task<List<BrandDto>> GetBrandsByIds(List<int> brandIds)
    {
        if (!brandIds.Any()) return new List<BrandDto>();

        try
        {
            var response = await _elasticSearchService._client.SearchAsync<BrandDto>(s => s
                .Index(ElasticSearchIndexConstants.Brands)
                .Size(brandIds.Count)
                .Query(q => q.Terms(t => t.Field("Id").Terms(brandIds)))
            );

            return response.IsValid ? response.Documents.ToList() : new List<BrandDto>();
        }
        catch
        {
            return new List<BrandDto>();
        }
    }

    /// <summary>
    /// image_index'ten ürün ID'lerine göre görselleri getir
    /// </summary>
    private async Task<List<ImageIndexDto>> GetImagesByProductIds(List<int> productIds)
    {
        if (!productIds.Any()) return new List<ImageIndexDto>();

        try
        {
            var response = await _elasticSearchService._client.SearchAsync<ImageIndexDto>(s => s
                .Index(ElasticSearchIndexConstants.Images)
                .Size(productIds.Count)
                .Query(q => q.Terms(t => t.Field(f => f.ProductId).Terms(productIds)))
            );

            return response.IsValid ? response.Documents.ToList() : new List<ImageIndexDto>();
        }
        catch
        {
            return new List<ImageIndexDto>();
        }
    }

    /// <summary>
    /// Metni maksimum uzunluğa göre kısalt
    /// </summary>
    private string TruncateString(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text)) return text;
        if (text.Length <= maxLength) return text;
        
        return text.Substring(0, maxLength - 3) + "...";
    }
}
