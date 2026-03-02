using ecommerce.Admin.Domain.Dtos.MenuDto;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IMenuService
    {
        Task<IActionResult<List<Menu>>> GetAllMenus();
        Task<IActionResult<List<Menu>>> GetMenuHierarchy();
        Task<IActionResult<List<Menu>>> GetMenusForCurrentUser(); // Gets menus for authenticated user (role + user-specific)
        Task<IActionResult<Paging<List<MenuListDto>>>> GetPagedMenus(PageSetting pager);
        Task<IActionResult<MenuUpsertDto>> GetMenuById(int id);
        Task<IActionResult<Empty>> UpsertMenu(MenuUpsertDto dto);
        Task<IActionResult<Empty>> DeleteMenu(int id);
        Task<IActionResult<Empty>> UpdateMenuLocation(int id, int? parentId, int order);
    }
}