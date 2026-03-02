using ecommerce.Admin.Domain.Dtos.Scheduler;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IEducationCalendarService{
   
    public Task<IActionResult<Paging<List<EducationCalendarListDto>>>> GetAll(PageSetting pager);
    public Task<IActionResult<List<EducationCalendarListDto>>> Get();

    Task<IActionResult<Empty>> Upsert(AuditWrapDto<EducationCalendarUpsertDto> model);
    Task<IActionResult<Empty>> Delete(AuditWrapDto<EducationCalendarDeleteDto> model);
    Task<IActionResult<EducationCalendarUpsertDto>> GetById(int Id);
}
