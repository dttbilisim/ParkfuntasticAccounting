namespace ecommerce.Admin.Services.Interfaces;

public interface IDiscountCacheService
{
    Task<List<ecommerce.Admin.Domain.Dtos.DiscountDto.DiscountWithProductsDto>> GetActiveDiscountsAsync();
    void InvalidateCache();
}
