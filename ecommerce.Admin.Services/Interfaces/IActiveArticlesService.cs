using ecommerce.Admin.Domain.Dtos.ActiveArticleDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IActiveArticlesService
    {
        public Task<IActionResult<Paging<IQueryable<ActiveArticleListDto>>>> GetActiveArticles(PageSetting pager);
        public Task<IActionResult<List<ActiveArticleListDto>>> GetActiveArticles();
        Task<IActionResult<Empty>> UpsertActiveArticle(AuditWrapDto<ActiveArticleUpsertDto> model);
        Task<IActionResult<Empty>> DeleteActiveArticle(AuditWrapDto<ActiveArticleDeleteDto> model);
        Task<IActionResult<ActiveArticleUpsertDto>> GetActiveArticleById(int Id);
    }
}
