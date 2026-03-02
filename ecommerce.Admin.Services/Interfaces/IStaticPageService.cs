using ecommerce.Admin.Domain.Dtos.StaticPageDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IStaticPageService
    {
        public Task<IActionResult<Paging<IQueryable<StaticPageListDto>>>> GetAboutUs(PageSetting pager);
        public Task<IActionResult<List<StaticPageListDto>>> GetAboutUs();
        Task<IActionResult<Empty>> UpsertAboutUs(AuditWrapDto<StaticPageUpsertDto> model);
        Task<IActionResult<Empty>> DeleteAboutUs(AuditWrapDto<StaticPageDeleteDto> model);
        Task<IActionResult<StaticPageUpsertDto>> GetAboutUsById(int Id);
        
        
    
    }
}
