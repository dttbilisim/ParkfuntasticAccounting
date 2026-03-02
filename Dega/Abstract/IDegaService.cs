using Dega.Dtos;
namespace Dega.Abstract;
public interface IDegaService{
    Task<string> GetTokenAsync();
    Task<List<ProductResponse>> GetProductsAsync(string token, ProductListRequestDto request);
    Task<DegaAddToCartResponseDto?> AddToCartAsync(List<DegaCartItemDto> items);
    Task<CustomOrderResponseDto?> CreateOrderAsync(CustomOrderRequestDto request);
}
