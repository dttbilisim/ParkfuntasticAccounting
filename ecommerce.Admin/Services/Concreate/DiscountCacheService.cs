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
    private static readonly TimeSpan CACHE_EXPIRATION = TimeSpan.FromMinutes(5); // 5 dakika cache (kampanyalar sık değişmez)

    public DiscountCacheService(IMemoryCache cache, IDiscountService discountService)
    {
        _cache = cache;
        _discountService = discountService;
    }

    public async Task<List<DiscountWithProductsDto>> GetActiveDiscountsAsync()
    {
        // Try to get from cache
        if (_cache.TryGetValue(CACHE_KEY, out List<DiscountWithProductsDto>? cachedDiscounts) && cachedDiscounts != null)
        {
            return cachedDiscounts;
        }

        // Cache miss - load and cache
        try
        {
            var result = await _discountService.GetActiveDiscountsWithProductsAsync();
            if (result.Ok && result.Result != null)
            {
                var discounts = result.Result;
                
                // Cache with absolute expiration
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
            // Silent fail
        }

        return new List<DiscountWithProductsDto>();
    }

    public void InvalidateCache()
    {
        _cache.Remove(CACHE_KEY);
    }
}
