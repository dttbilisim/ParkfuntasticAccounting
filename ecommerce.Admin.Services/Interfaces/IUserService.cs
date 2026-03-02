using ecommerce.Admin.Domain.Dtos.UserDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IUserService
    {
        Task<IActionResult<Paging<List<UserListDto>>>> GetUsers(PageSetting pager);
        Task<IActionResult<UserUpsertDto>> GetUserById(int id);
        Task<IActionResult<int>> UpsertUser(AuditWrapDto<UserUpsertDto> model);
        Task<IActionResult<Empty>> DeleteUser(AuditWrapDto<UserDeleteDto> model);
        Task<IActionResult<int>> GetUserCount();
    }
}


