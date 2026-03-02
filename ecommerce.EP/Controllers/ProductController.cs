using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Web.Domain.Services.Abstract;
using ecommerce.Admin.Services.Dtos.VinDto;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Web.Domain.Dtos.Order;
using System.Text.RegularExpressions;
using ecommerce.EP.Models;

namespace ecommerce.EP.Controllers
{
    /// <summary>
    /// Product Search and Filtering Controller
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("search")]
    public class ProductController : ControllerBase
    {
        private readonly IAdminProductSearchService _searchService;
        private readonly IVinService _vinService;
        private readonly ecommerce.Admin.Services.Interfaces.IRecentSearchService _recentSearchService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(
            IAdminProductSearchService searchService, 
            IVinService vinService,
            ecommerce.Admin.Services.Interfaces.IRecentSearchService recentSearchService,
            ILogger<ProductController> logger)
        {
            _searchService = searchService;
            _vinService = vinService;
            _recentSearchService = recentSearchService;
            _logger = logger;
        }

        /// <summary>
        /// Performs a unified search (VIN, OEM, or Text).
        /// </summary>
        /// <param name="keyword">Search term (e.g., product name, OEM, or 17-char VIN)</param>
        /// <param name="onlyInStock">If true, only products with stock will be returned</param>
        /// <param name="includeEquivalents">If true, includes equivalent/aftermarket parts (relevant for VIN/OEM searches)</param>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="pageSize">Items per page (default 20)</param>
        /// <returns>Unified search result state</returns>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnifiedSearchResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Search(
            [FromQuery] string keyword, 
            [FromQuery] bool onlyInStock = false, 
            [FromQuery] bool includeEquivalents = false,
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(keyword) || keyword.Length < 2)
            {
                return BadRequest("Arama terimi en az 2 karakter olmalıdır.");
            }

            var trimmedKeyword = keyword.Trim().ToUpperInvariant();
            
            // VIN Detection (17 chars, alphanumeric, no spaces/dashes)
            bool isVin = trimmedKeyword.Length == 17 && Regex.IsMatch(trimmedKeyword, @"^[A-Z0-9]{17}$") && !trimmedKeyword.Contains("-") && !trimmedKeyword.Contains(" ");

            if (isVin)
            {
                _logger.LogInformation("[API-VIN] VIN detected: {Vin}", trimmedKeyword);
                
                // Auto-correct common typos
                var correctedVin = trimmedKeyword.Replace('I', '1').Replace('O', '0').Replace('Q', '0');
                
                var decodeResult = await _vinService.DecodeVinAsync(correctedVin);
                if (decodeResult.Ok && decodeResult.Result != null && decodeResult.Result.IsSuccess)
                {
                    return Ok(new UnifiedSearchResponse
                    {
                        SearchType = "VIN",
                        Vin = correctedVin,
                        VinResult = decodeResult.Result
                    });
                }
                
                _logger.LogWarning("[API-VIN] VIN decode failed or no vehicles found for: {Vin}. Falling back to normal search.", correctedVin);
            }

            // Normal Product Search (Text or Code)
            var filter = new SearchFilterReguestDto
            {
                Search = keyword,
                OnlyInStock = onlyInStock,
                Page = page,
                PageSize = pageSize,
                ShouldGroupOems = !includeEquivalents // Admin handles grouping based on equivalents toggle
            };

            return await ExecuteSearchAndReturnResponse(filter, "Products");
        }

        /// <summary>
        /// Performs an advanced filtered search with paging.
        /// </summary>
        [HttpPost("filter")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnifiedSearchResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetByFilter([FromBody] SearchFilterReguestDto filter)
        {
            return await ExecuteSearchAndReturnResponse(filter, "Products");
        }

        /// <summary>
        /// VIN-specific search results.
        /// Hands searches by DAT Process Numbers or OEM Codes discovered via VIN lookup.
        /// </summary>
        [HttpPost("vin-search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UnifiedSearchResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VinSearch([FromBody] VinSearchRequest request)
        {
            var filter = new SearchFilterReguestDto
            {
                DatProcessNumbers = request.DatProcessNumbers,
                OemCodes = request.OemCodes,
                OnlyInStock = request.OnlyInStock,
                Page = request.Page ?? 1,
                PageSize = request.PageSize ?? 20,
                ShouldGroupOems = !request.IsEquivalent,
                // DotCompiledCodes eşleşmesi için araç bilgileri
                ManufacturerKey = request.ManufacturerKey,
                BaseModelKey = request.BaseModelKey,
            };

            return await ExecuteSearchAndReturnResponse(filter, "Products");
        }

        /// <summary>
        /// Internal helper to execute both product search and aggregation queries, 
        /// matching the logic in ProductSearch.razor.cs.
        /// </summary>
        private async Task<IActionResult> ExecuteSearchAndReturnResponse(SearchFilterReguestDto filter, string type)
        {
            var searchResult = await _searchService.GetByFilterPagingAsync(filter);
            var aggsResult = await _searchService.GetSearchAggregationsAsync(filter);

            if (searchResult.Ok)
            {
                // Arama geçmişine kaydet (sadece text search için)
                if (!string.IsNullOrWhiteSpace(filter.Search))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _recentSearchService.AddSearchTermAsync(
                                filter.Search, 
                                searchResult.Result?.DataCount ?? 0
                            );
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Arama geçmişi kaydedilemedi: {Search}", filter.Search);
                        }
                    });
                }

                var response = new UnifiedSearchResponse
                {
                    SearchType = type,
                    Products = searchResult.Result,
                    Aggregations = aggsResult.Ok ? aggsResult.Result : new SearchFilterAggregations()
                };

                // Extract filters manually from the data, mimicking 'ExtractFilters()' in ProductSearch.razor.cs
                if (searchResult.Result?.Data != null)
                {
                    // Extract unique brands
                    response.Brands = searchResult.Result.Data
                        .Where(p => p.Brand != null)
                        .Select(p => p.Brand!)
                        .GroupBy(b => b.Id)
                        .Select(g => g.First())
                        .ToList();

                    // Extract unique categories
                    response.Categories = searchResult.Result.Data
                        .Where(p => p.Categories != null && p.Categories.Any())
                        .SelectMany(p => p.Categories!)
                        .GroupBy(c => c.Id)
                        .Select(g => g.First())
                        .ToList();

                    // Aggregation'lara brands ve categories ekle (mobil filtre için)
                    if (response.Aggregations != null)
                    {
                        // Brands — Dictionary<int, string>
                        if (response.Aggregations.Brands == null || !response.Aggregations.Brands.Any())
                        {
                            response.Aggregations.Brands = response.Brands
                                .Where(b => b.Id.HasValue && !string.IsNullOrWhiteSpace(b.Name))
                                .GroupBy(b => b.Id!.Value)
                                .ToDictionary(g => g.Key, g => g.First().Name!);
                        }

                        // Categories — Dictionary<int, string>
                        if (response.Aggregations.Categories == null || !response.Aggregations.Categories.Any())
                        {
                            response.Aggregations.Categories = response.Categories
                                .Where(c => c.Id.HasValue && !string.IsNullOrWhiteSpace(c.Name))
                                .GroupBy(c => c.Id!.Value)
                                .ToDictionary(g => g.Key, g => g.First().Name!);
                        }

                        // DotPartNames — ürünlerden çıkar
                        if (response.Aggregations.DotPartNames == null || !response.Aggregations.DotPartNames.Any())
                        {
                            response.Aggregations.DotPartNames = searchResult.Result.Data
                                .Where(p => !string.IsNullOrWhiteSpace(p.DotPartName))
                                .Select(p => p.DotPartName!)
                                .Distinct()
                                .OrderBy(n => n)
                                .ToList();
                        }
                    }
                }

                return Ok(response);
            }

            return BadRequest(searchResult.Metadata?.Message ?? "Arama işlemi başarısız oldu.");
        }

        /// <summary>
        /// Kullanıcının son aramalarını getirir (en fazla 10 adet)
        /// </summary>
        [HttpGet("recent-searches")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetRecentSearches()
        {
            var searches = await _recentSearchService.GetRecentSearchesAsync(10);
            return Ok(searches);
        }

        /// <summary>
        /// Kullanıcının tüm arama geçmişini temizler
        /// </summary>
        [HttpPost("recent-searches/clear")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ClearRecentSearches()
        {
            await _recentSearchService.ClearRecentSearchesAsync();
            return Ok(new { message = "Arama geçmişi temizlendi" });
        }

        /// <summary>
        /// Belirli bir arama terimini geçmişten siler
        /// </summary>
        [HttpPost("recent-searches/{term}/delete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RemoveRecentSearch(string term)
        {
            await _recentSearchService.RemoveSearchTermAsync(term);
            return Ok(new { message = "Arama terimi silindi" });
        }

        /// <summary>
        /// Muadil ürünleri getirir — OEM kodlarına göre benzer ürünleri arar
        /// </summary>
        [HttpPost("similar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetSimilarProducts([FromBody] SimilarProductsRequest request)
        {
            if (request.OemCodes == null || !request.OemCodes.Any())
            {
                return BadRequest("OEM kodları gereklidir.");
            }

            var result = await _searchService.GetSimilarProductsAsync(request.OemCodes);
            if (result.Ok)
            {
                return Ok(result.Result ?? new List<SellerProductViewModel>());
            }

            return BadRequest(result.Metadata?.Message ?? "Muadil ürünler getirilemedi.");
        }
    }

    public class SimilarProductsRequest
    {
        public List<string> OemCodes { get; set; } = new();
        public int? ExcludeSellerItemId { get; set; }
    }

    public class VinSearchRequest
    {
        public List<string>? DatProcessNumbers { get; set; }
        public List<string>? OemCodes { get; set; }
        public bool OnlyInStock { get; set; }
        public bool IsEquivalent { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        // Araç bilgileri — DotCompiledCodes eşleşmesi için
        public string? ManufacturerKey { get; set; }
        public string? BaseModelKey { get; set; }
    }
}
