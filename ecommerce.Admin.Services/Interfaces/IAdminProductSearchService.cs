using System.Collections.Generic;
using System.Threading.Tasks;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IAdminProductSearchService
    {
        Task<IActionResult<List<SellerProductViewModel>>> SearchAsync(string keyword, bool onlyInStock = false);
        Task<IActionResult<Paging<List<SellerProductViewModel>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter);
        Task<IActionResult<List<SellerProductViewModel>>> GetSimilarProductsAsync(List<string> oemCodes);
        Task<IActionResult<List<SellerProductViewModel>>> GetSimilarProductsAsync(string oemCode);
        Task<IActionResult<SearchFilterAggregations>> GetSearchAggregationsAsync(SearchFilterReguestDto filter);
    }
}
