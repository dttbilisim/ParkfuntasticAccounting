using ecommerce.Admin.Domain.Dtos.CargoDto;
using ecommerce.Admin.Domain.Dtos.CompanyDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces;
public interface IOnlineMeetService{
    public Task<IActionResult<Paging<IQueryable<OnlineMeetDto>>>> GetOnlineMeet(PageSetting pager);
    public Task<IActionResult<List<OnlineMeetDto>>> GetOnlineMeet();
    Task<IActionResult<int>> UpsertMeet(AuditWrapDto<OnlineMeetUpsertDto> model);
    Task<IActionResult<Empty>> DeleteMeet(AuditWrapDto<OnlineMeetDeleteDto> model);
    Task<IActionResult<OnlineMeetUpsertDto>> GetMeetById(int Id);
}
