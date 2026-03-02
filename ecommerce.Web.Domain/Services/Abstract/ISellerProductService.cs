using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Dtos;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface ISellerProductService
{
    Task<IActionResult<List<SellerProductViewModel>>> GetAllAsync(int page = 1, int pageSize = 20);
    Task<IActionResult<Paging<List<SellerProductViewModel>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter);
    Task<IActionResult<List<SellerProductViewModel>>> SearchAsync(string keyword, bool onlyInStock = false);
    Task<IActionResult<SellerProductViewModel>> GetByIdAsync(int id);
    Task<IActionResult<List<SellerProductViewModel>>> GetByIdsAsync(List<int> ids);
    Task<IActionResult<List<SellerProductViewModel>>> GetByBrandIdAsync(int brandId);
    Task AttachCompatibilityMetadataAsync(List<SellerProductViewModel>? viewModels);
    Task AttachCompatibilityMetadataAsync(List<SellerProductViewModel>? viewModels, IEnumerable<string>? allowedSubModelKeys);
    Task<IActionResult<List<ecommerce.Web.Domain.Dtos.BaseModelDto>>> GetCompatibleModelsAsync(int productId);
    Task<IActionResult<List<SellerProductViewModel>>> GetByOemCodesAsync(List<string> oemCodes);
}

