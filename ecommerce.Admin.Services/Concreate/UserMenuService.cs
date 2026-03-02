using AutoMapper;
using ecommerce.Admin.Domain.Dtos.UserMenuDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ecommerce.Admin.Services.Concreate
{
    public class UserMenuService : IUserMenuService
    {
        private readonly IUnitOfWork<ApplicationDbContext> _uow;
        private readonly IMapper _mapper;
        private readonly ILogger<UserMenuService> _logger;

        public UserMenuService(IUnitOfWork<ApplicationDbContext> uow, IMapper mapper, ILogger<UserMenuService> logger)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult<List<UserMenuListDto>>> GetUserMenusByUserId(int userId)
        {
            var rs = new IActionResult<List<UserMenuListDto>> { Result = new List<UserMenuListDto>() };
            try
            {
                _logger.LogInformation($"GetUserMenusByUserId called for userId={userId}");
                
                var repo = _uow.GetRepository<UserMenu>();
                var userMenus = await repo.GetAllAsync(
                    predicate: x => x.UserId == userId,
                    include: source => source.Include(x => x.Menu)
                );

                _logger.LogInformation($"Found {userMenus.Count} UserMenu records for userId={userId}");
                
                var mapped = _mapper.Map<List<UserMenuListDto>>(userMenus);
                rs.Result = mapped ?? new List<UserMenuListDto>();
                
                _logger.LogInformation($"Mapped to {rs.Result.Count} UserMenuListDto records");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetUserMenusByUserId error for userId={userId}", userId);
                rs.AddError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertUserMenus(int userId, List<UserMenuUpsertDto> userMenus)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var repo = _uow.GetRepository<UserMenu>();
                
                // Delete existing user menus
                var existingMenus = await repo.GetAllAsync(predicate: x => x.UserId == userId);
                foreach (var menu in existingMenus)
                {
                    repo.Delete(menu);
                }

                // Insert new user menus with permissions
                foreach (var dto in userMenus)
                {
                    await repo.InsertAsync(new UserMenu
                    {
                        UserId = userId,
                        MenuId = dto.MenuId,
                        CanView = dto.CanView,
                        CanCreate = dto.CanCreate,
                        CanEdit = dto.CanEdit,
                        CanDelete = dto.CanDelete
                    });
                }

                await _uow.SaveChangesAsync();
                var lastResult = _uow.LastSaveChangesResult;
                
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Menü yetkileri başarıyla güncellendi");
                }
                else
                {
                    if (lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertUserMenus error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteUserMenu(int id)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                var repo = _uow.GetRepository<UserMenu>();
                var userMenu = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == id);
                
                if (userMenu == null)
                {
                    rs.AddError("Kullanıcı menü yetkisi bulunamadı");
                    return rs;
                }

                repo.Delete(userMenu);
                await _uow.SaveChangesAsync();
                var lastResult = _uow.LastSaveChangesResult;
                
                if (lastResult.IsOk)
                {
                    rs.AddSuccess("Menü yetkisi başarıyla silindi");
                    return rs;
                }
                else
                {
                    if (lastResult.Exception != null)
                        rs.AddError(lastResult.Exception.ToString());
                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteUserMenu error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }
        public async Task<bool> HasPermission(int userId, string menuPath, string permissionType)
        {
            if (string.IsNullOrEmpty(menuPath)) return false;

            try
            {
                var repo = _uow.GetRepository<UserMenu>();
                var userMenu = await repo.GetFirstOrDefaultAsync(
                    predicate: x => x.UserId == userId && x.Menu.Path == menuPath,
                    include: i => i.Include(m => m.Menu));

                if (userMenu != null)
                {
                    return permissionType.ToLower() switch
                    {
                        "view" => userMenu.CanView,
                        "create" => userMenu.CanCreate,
                        "edit" => userMenu.CanEdit,
                        "delete" => userMenu.CanDelete,
                        _ => false
                    };
                }

                // Fallback: Check Role Permissions
                // Since RoleMenu table doesn't have granular permissions (CanView, CanEdit etc.),
                // we assume if a Role has the Menu, it has FULL permission.
                
                var userRepo = _uow.GetRepository<ApplicationUser>();
                var user = await userRepo.GetFirstOrDefaultAsync(
                    predicate: u => u.Id == userId,
                    include: i => i.Include(u => u.Roles),
                    disableTracking: true
                );

                if (user == null || user.Roles == null || !user.Roles.Any()) return false;

                var roleIds = user.Roles.Select(r => r.Id).ToList();
                var roleMenuRepo = _uow.GetRepository<RoleMenu>();
                
                // Check if any of user's roles has this menu assigned with the required permission
                var type = permissionType.ToLower();
                var hasRoleAccess = await roleMenuRepo.GetAll(true).AnyAsync(rm => 
                    roleIds.Contains(rm.RoleId) && rm.Menu.Path == menuPath &&
                    (
                        (type == "view" && rm.CanView) ||
                        (type == "create" && rm.CanCreate) ||
                        (type == "edit" && rm.CanEdit) ||
                        (type == "delete" && rm.CanDelete)
                    )
                );

                return hasRoleAccess;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking permission: UserId={userId}, Path={menuPath}, Type={permissionType}");
                return false;
            }
        }
    }
}
