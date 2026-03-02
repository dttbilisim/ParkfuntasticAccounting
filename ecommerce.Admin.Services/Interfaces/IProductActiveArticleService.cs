using ecommerce.Admin.Domain.Dtos.ProductActiveArcticleDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IProductActiveArticleService
    {
        public Task<IActionResult<List<ProductActiveArticleListDto>>> GetProductActiveArticles(int productId);
        Task<IActionResult<Empty>> UpsertProductActiveArticle(AuditWrapDto<ProductActiveArticleUpsertDto> model);
        Task<IActionResult<Empty>> DeleteProductActiveArticle(AuditWrapDto<ProductActiveArticleDeleteDto> model);
        Task<IActionResult<ProductActiveArticleUpsertDto>> GetProductActiveArticleById(int Id);
    }
}
