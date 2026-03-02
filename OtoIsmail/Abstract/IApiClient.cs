using ecommerce.Core.Entities;
using OtoIsmail.Dtos;
namespace OtoIsmail.Abstract;
public interface IApiClient
{
    Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data);
    Task<TResponse?> GetAsync<TResponse>(string url);
    Task<List<BrandDto>?> GetBrandsAsync();
    Task<List<ProductOtoIsmail>?> GetProductsAsync(string brand, string tarih);
}