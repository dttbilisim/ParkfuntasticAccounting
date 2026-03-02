using ecommerce.Admin.Domain.Dtos.DiscountDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ecommerce.Admin.Services.Concreate;

public class DiscountCacheService : IDiscountCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IDiscountService _discountService;
    private const string CACHE_KEY = "active_discounts_with_products";
    private static readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromMinutes(5);

    public DiscountCacheService(IMemoryCache cache, IDiscountService discountService)
    {
        _cache = cache;
        _discountService = discountService;
    }

    public async Task<List<DiscountWithProductsDto>> GetActiveDiscountsAsync()
    {
        if (_cache.TryGetValue(CACHE_KEY, out List<DiscountWithProductsDto>? cachedDiscounts) && cachedDiscounts != null)
        {
            return cachedDiscounts;
        }

        try
        {
            var result = await _discountService.GetActiveDiscountsWithProductsAsync();
            if (result.Ok && result.Result != null)
            {
                var discounts = result.Result;
                
                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CACHE_EXPIRATION,
                    Priority = CacheItemPriority.Normal
                };

                _cache.Set(CACHE_KEY, discounts, cacheOptions);
                
                return discounts;
            }
        }
        catch
        {
            // Sessiz hata — cache miss durumunda boş liste dön
        }

        return new List<DiscountWithProductsDto>();
    }

    public void InvalidateCache()
    {
        _cache.Remove(CACHE_KEY);
    }
}
