using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Brand;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IBrandService
{
    /// <param name="minProductCount">When set, only brands with at least this many products are returned (e.g. 10 for homepage).</param>
    /// <param name="inStockOnly">When true, only brands that have at least one product with stock &gt; 0 are returned.</param>
    Task<IActionResult<List<BrandElasticDto>>> GetAllAsync(int? minProductCount = null, bool inStockOnly = false);
    Task<IActionResult<List<BrandElasticDto>>> SearchAsync(string keyword);
}