using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Category;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface ICategoryService
{
    Task<IActionResult<List<CategoryElasticDto>>> GetAllAsync();
    Task<IActionResult<List<CategoryElasticDto>>> GetAllWithIsMainPageAsync();
    Task<IActionResult<List<CategoryElasticDto>>> GetCatehoryWithById(int categoryId);
    Task<IActionResult<List<CategoryElasticDto>>> GetAllCategoryFooter();
}