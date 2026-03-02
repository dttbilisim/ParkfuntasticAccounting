using ecommerce.Admin.Domain.Dtos.Identity;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces;

public interface IIdentityUserService
{
    Task<IActionResult<Paging<List<IdentityUserListDto>>>> GetPagedListAsync(PageSetting pager);

    Task<IActionResult<List<IdentityRoleListDto>>> GetRoleListAsync();

    Task<IActionResult<IdentityUserUpsertDto>> GetAsync(int id);

    Task<IActionResult<int>> UpsertAsync(IdentityUserUpsertDto dto);

    Task<IActionResult<Empty>> DeleteAsync(int id);
    Task<IActionResult<Empty>> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}