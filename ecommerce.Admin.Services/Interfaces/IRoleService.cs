using ecommerce.Admin.Domain.Dtos.Role;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces;

public interface IRoleService
{
    Task<IActionResult<List<RoleListDto>>> GetAllRoles();
    Task<IActionResult<RoleUpsertDto>> GetRoleById(int id);
    Task<IActionResult<Empty>> UpsertRole(RoleUpsertDto roleDto);
    Task<IActionResult<Empty>> DeleteRole(int id);
}
