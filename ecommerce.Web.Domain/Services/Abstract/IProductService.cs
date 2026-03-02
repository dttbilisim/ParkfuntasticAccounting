using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IProductService
{
 Task<IActionResult<List<ProductElasticDto>>> GetAllAsync();
 Task<IActionResult<ProductElasticDto>> GetByIdAsync(int id);
 Task<IActionResult<List<ProductElasticDto>>> GetByCategoryIdAsync(int categoryId);
 Task<IActionResult<Paging<List<ProductElasticDto>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter);
 Task<IActionResult<List<ProductElasticDto>>> SearchAsync(string keyword);
 Task<IActionResult<List<ProductElasticDto>>> GetByBrandIdAsync(int brandId);

}