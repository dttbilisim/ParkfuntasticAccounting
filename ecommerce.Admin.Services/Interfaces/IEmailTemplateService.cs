using ecommerce.Admin.Domain.Dtos.EmailDto;
using ecommerce.Admin.Domain.Dtos.StaticPageDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IEmailTemplateService{
   
    
    public Task<IActionResult<Paging<IQueryable<EmailTemplatesDto>>>> GetAllPaging(PageSetting pager);
    public Task<IActionResult<List<EmailTemplatesDto>>> Get();
    Task<IActionResult<Empty>> Upsert(AuditWrapDto<EmailTemplatesDto> model);
    Task<IActionResult<Empty>> Delete(AuditWrapDto<EmailTemplatesDto> model);
    Task<IActionResult<EmailTemplatesDto>> GetById(int Id);
}
