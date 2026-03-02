using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Dtos.Favorite;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace ecommerce.Web.Domain.Services.Concreate;

public class FavoriteService : IFavoriteService
{
    private readonly IElasticClient _elasticClient;
    private readonly IUnitOfWork<ApplicationDbContext> _context;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public FavoriteService(IElasticClient elasticClient, IUnitOfWork<ApplicationDbContext> context, IHttpContextAccessor httpContextAccessor)
    {
        _elasticClient = elasticClient;
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }



    public async Task<IActionResult<Paging<List<ProductFavoriteDto>>>> GetAllFavoritesAsync(int page, int pageSize)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            var fail = OperationResult.CreateResult<Paging<List<ProductFavoriteDto>>>();
            fail.AddError("Unauthorized");
            return fail;
        }

        var result = OperationResult.CreateResult<Paging<List<ProductFavoriteDto>>>();
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            // Total count is computed using a simple query to avoid reusing the projection query twice
            var totalCount = await _context.DbContext.MyFavorites.AsNoTracking()
                .Where(f => f.UserId == userId && f.Status == 1)
                .CountAsync();

            var itemsQuery =
                from fav in _context.DbContext.MyFavorites.AsNoTracking()
                where fav.UserId == userId && fav.Status == 1
                join prod in _context.DbContext.Product.AsNoTracking() on fav.ProductId equals prod.Id
                join brand in _context.DbContext.Brand.AsNoTracking() on prod.BrandId equals brand.Id
                select new ProductFavoriteDto
                {
                    Id = prod.Id,
                    SellerItemId = (
                        from si in _context.DbContext.SellerItems.AsNoTracking()
                        where si.ProductId == prod.Id && si.Stock > 0 && si.Status == 1
                        orderby si.SalePrice ascending
                        select (int?)si.Id
                    ).FirstOrDefault(),
                    Name = prod.Name,
                    Description = prod.Description,
                    BrandName = brand.Name,
                    CategoryName = (
                        from pc in _context.DbContext.ProductCategories.AsNoTracking()
                        where pc.ProductId == prod.Id
                        join c in _context.DbContext.Category.AsNoTracking() on pc.CategoryId equals c.Id
                        select c.Name
                    ).FirstOrDefault(),
                    UserId = fav.UserId,
                    FileName = (
                        from pi in _context.DbContext.ProductImages.AsNoTracking()
                        where pi.ProductId == prod.Id
                        orderby pi.Order
                        select pi.FileName
                    ).FirstOrDefault(),
                    Status = fav.Status,
                    DocumentUrl = prod.DocumentUrl ?? string.Empty
                };

            var items = await itemsQuery
                .OrderByDescending(p => p.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            result.Result = new Paging<List<ProductFavoriteDto>>
            {
                Data = items,
                DataCount = totalCount
            };
        }
        catch (Exception ex)
        {
            result.AddSystemError(ex.Message);
        }

        return result;
    }


    public async Task<IActionResult<MyFavorites>> UpsertFavoriteForCurrentUserAsync(int productId)
    {
        var rs = OperationResult.CreateResult<MyFavorites>();
        try
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                rs.AddError("Unauthorized");
                return rs;
            }

            var repo = _context.GetRepository<MyFavorites>();

            var existing = await repo.GetFirstOrDefaultAsync(predicate: x => x.UserId == userId && x.ProductId == productId);
            if (existing == null)
            {
                var entity = new MyFavorites
                {
                    UserId = userId,
                    ProductId = productId,
                    Status = 1,
                    CreatedDate = DateTime.UtcNow
                };
                await repo.InsertAsync(entity);
                await _context.SaveChangesAsync();
                rs.Result = entity;
                return rs;
            }

            if (existing.Status == 99)
            {
                existing.Status = 1;
                existing.DeletedDate = null;
                existing.ModifiedDate = DateTime.UtcNow;
                repo.Update(existing);
                await _context.SaveChangesAsync();
                rs.Result = existing;
                return rs;
            }

            rs.AddError("Bu ürün zaten favorilerde.");
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.Message);
            return rs;
        }
    }

 

    public async Task<IActionResult<bool>> DeleteFavoriteForCurrentUserAsync(int productId)
    {
        var principal = _httpContextAccessor.HttpContext?.User;
        var userIdClaim = principal?.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            var fail = OperationResult.CreateResult<bool>();
            fail.AddError("Unauthorized");
            return fail;
        }
        var rs = OperationResult.CreateResult<bool>();
        try
        {
            var repo = _context.GetRepository<MyFavorites>();
            var existing = await repo.GetFirstOrDefaultAsync(predicate: x => x.UserId == userId && x.ProductId == productId && x.Status == 1);
            if (existing == null)
            {
                rs.AddError("Favori bulunamadı.");
                return rs;
            }
            existing.Status = 99;
            existing.DeletedDate = DateTime.UtcNow;
            repo.Update(existing);
            await _context.SaveChangesAsync();
            rs.Result = true;
            return rs;
        }
        catch (Exception ex)
        {
            rs.AddSystemError(ex.Message);
            return rs;
        }
    }

    public async Task<MyFavorites?> GetFavoriteByUserAndProductIdAsync(int userId, int productId)
    {
        return await _context.DbContext.MyFavorites
            .FirstOrDefaultAsync(x => x.UserId == userId && x.ProductId == productId && x.Status == 1);
    }
}