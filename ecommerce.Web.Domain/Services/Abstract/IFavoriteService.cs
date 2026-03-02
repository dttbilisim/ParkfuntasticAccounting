using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Dtos.Favorite;
using ecommerce.Domain.Shared.Dtos.Product;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IFavoriteService
{
    Task<IActionResult<Paging<List<ProductFavoriteDto>>>> GetAllFavoritesAsync(int page, int pageSize);

    Task<IActionResult<MyFavorites>> UpsertFavoriteForCurrentUserAsync(int productId);
    Task<IActionResult<bool>> DeleteFavoriteForCurrentUserAsync(int productId);
}