using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Caching.Memory;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Category;
using ecommerce.Domain.Shared.Dtos.Brand;

namespace ecommerce.EP.Controllers;

/// <summary>
/// Common data controller (Categories, Brands, etc.)
/// </summary>
[Route("api/[controller]")]
[ApiController]
[EnableRateLimiting("public")]
public class CommonController : ControllerBase
{
    private const string HomeBrandsCacheKey = "common_brands_min10";
    private const string HomeBrandsInStockCacheKey = "common_brands_min10_inStock";
    private static readonly TimeSpan HomeBrandsCacheTtl = TimeSpan.FromMinutes(10);

    private readonly ICategoryService _categoryService;
    private readonly IBrandService _brandService;
    private readonly IMemoryCache _cache;

    public CommonController(ICategoryService categoryService, IBrandService brandService, IMemoryCache cache)
    {
        _categoryService = categoryService;
        _brandService = brandService;
        _cache = cache;
    }

    /// <summary>
    /// Gets all active categories.
    /// </summary>
    [HttpGet("categories")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryElasticDto>))]
    public async Task<IActionResult> GetCategories()
    {
        var result = await _categoryService.GetAllAsync();
        if (result.Ok) return Ok(result.Result);
        return BadRequest(result.Metadata?.Message);
    }

    /// <summary>
    /// Gets main page categories.
    /// </summary>
    [HttpGet("categories/main")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryElasticDto>))]
    public async Task<IActionResult> GetMainCategories()
    {
        var result = await _categoryService.GetAllWithIsMainPageAsync();
        if (result.Ok) return Ok(result.Result);
        return BadRequest(result.Metadata?.Message);
    }

    /// <summary>
    /// Gets subcategories for a given category.
    /// </summary>
    [HttpGet("categories/{id}/sub")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<CategoryElasticDto>))]
    public async Task<IActionResult> GetSubCategories(int id)
    {
        var result = await _categoryService.GetCatehoryWithById(id);
        if (result.Ok) return Ok(result.Result);
        return BadRequest(result.Metadata?.Message);
    }

    /// <summary>
    /// Gets all active brands. Use minProductCount (e.g. 10) to only return brands with at least that many products (e.g. for homepage).
    /// Use inStockOnly=true to only return brands that have at least one product with stock &gt; 0.
    /// Homepage brands (minProductCount=10, with or without inStockOnly) are cached 10 min for fast load.
    /// </summary>
    [HttpGet("brands")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BrandElasticDto>))]
    public async Task<IActionResult> GetBrands([FromQuery] int? minProductCount = null, [FromQuery] bool inStockOnly = false)
    {
        if (minProductCount == 10)
        {
            var cacheKey = inStockOnly ? HomeBrandsInStockCacheKey : HomeBrandsCacheKey;
            if (_cache.TryGetValue(cacheKey, out List<BrandElasticDto>? cached) && cached != null)
                return Ok(cached);
            var result = await _brandService.GetAllAsync(10, inStockOnly);
            if (result.Ok)
            {
                _cache.Set(cacheKey, result.Result!, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = HomeBrandsCacheTtl });
                return Ok(result.Result);
            }
            return BadRequest(result.Metadata?.Message);
        }

        var uncached = await _brandService.GetAllAsync(minProductCount, inStockOnly);
        if (uncached.Ok) return Ok(uncached.Result);
        return BadRequest(uncached.Metadata?.Message);
    }

    /// <summary>
    /// Searches brands by keyword.
    /// </summary>
    [HttpGet("brands/search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<BrandElasticDto>))]
    public async Task<IActionResult> SearchBrands([FromQuery] string keyword)
    {
        var result = await _brandService.SearchAsync(keyword);
        if (result.Ok) return Ok(result.Result);
        return BadRequest(result.Metadata?.Message);
    }
}
