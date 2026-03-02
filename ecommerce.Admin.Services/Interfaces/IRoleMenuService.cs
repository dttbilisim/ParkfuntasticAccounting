using ecommerce.Admin.Domain.Dtos.RoleMenuDto;
using ecommerce.Core.Helpers;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
namespace ecommerce.Admin.Domain.Interfaces
{
    public interface IRoleMenuService
    {
        public Task<IActionResult<Paging<IQueryable<RoleMenuListDto>>>> GetRoleMenus(PageSetting pager);
        public Task<IActionResult<List<RoleMenuListDto>>> GetRoleMenus(List<int> RoleIds);
        Task<IActionResult<List<RoleMenuListDto>>> GetRoleMenus(int roleId);
        Task<IActionResult<Empty>> UpsertRoleMenus(int roleId, List<RoleMenuUpsertDto> roleMenus);
        Task<IActionResult<Empty>> UpsertRoleMenu(AuditWrapDto<RoleMenuUpsertDto> model);
        Task<IActionResult<Empty>> DeleteRoleMenu(AuditWrapDto<RoleMenuDeleteDto> model);
        Task<IActionResult<RoleMenuUpsertDto>> GetRoleMenuById(int Id);
    }
}
