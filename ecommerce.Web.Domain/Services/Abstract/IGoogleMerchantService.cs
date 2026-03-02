using ecommerce.Web.Domain.Dtos;

namespace ecommerce.Web.Domain.Services.Abstract;

/// <summary>
/// Google Merchant Center ürün feed'leri oluşturmak için servis
/// </summary>
public interface IGoogleMerchantService
{
    /// <summary>
    /// Google Merchant feed için tüm aktif ürünleri getir
    /// </summary>
    /// <param name="maxProducts">Döndürülecek maksimum ürün sayısı (varsayılan 50000)</param>
    /// <returns>Google Merchant formatında ürün listesi</returns>
    Task<List<GoogleMerchantProductDto>> GetProductsForFeedAsync(int maxProducts = 50000);
}
