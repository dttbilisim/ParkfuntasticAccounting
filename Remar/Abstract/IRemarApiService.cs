using Remar.Dtos;
namespace Remar.Abstract;
public interface IRemarApiService
{
    Task<string> GetTokenAsync();
    Task<List<RemarProductDto>> GetProductsAsync(string token, RemarProductListRequestDto request);
    Task<RemarAddToCartResponseDto?> AddToCartAsync(List<RemarCartItemDto> items);
    Task<CustomOrderResponseDto?> CreateOrderAsync(CustomOrderRequestDto request);
}