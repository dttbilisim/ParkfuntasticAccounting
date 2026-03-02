using ecommerce.Admin.Domain.Dtos.UserMenuDto;
using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IUserMenuService
    {
        Task<IActionResult<List<UserMenuListDto>>> GetUserMenusByUserId(int userId);
        Task<IActionResult<Empty>> UpsertUserMenus(int userId, List<UserMenuUpsertDto> userMenus);
        Task<IActionResult<Empty>> DeleteUserMenu(int id);
        Task<bool> HasPermission(int userId, string menuPath, string permissionType);
    }
}
