using AutoMapper;
using Azure;
using ecommerce.Admin.Domain.Dtos.MenuDto;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Models;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.EFCore.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using ecommerce.Core.Entities;
using System.Linq.Dynamic.Core;

namespace ecommerce.Admin.Services.Concreate
{
    public class MenuService : IMenuService
    {
        private readonly IUnitOfWork _uow; // Keep for write operations if needed, or fully switch? Write ops usually are triggered by user action, so less likely to clash? 
                                            // Write ops (Upsert/Delete) are usually single interactions. Read ops on load are the problem.
        private readonly IMapper _mapper;
        private readonly ILogger<MenuService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ecommerce.Admin.Domain.Services.IPermissionService _permissionService;
        private readonly IRadzenPagerService<MenuListDto> _radzenPagerService;
        private const string MENU_NAME = "menus";

        public MenuService(IUnitOfWork<ApplicationDbContext> uow, IMapper mapper, ILogger<MenuService> logger, 
            IHttpContextAccessor httpContextAccessor, IServiceScopeFactory serviceScopeFactory, ecommerce.Admin.Domain.Services.IPermissionService permissionService,
            IRadzenPagerService<MenuListDto> radzenPagerService)
        {
            _uow = uow;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
            _serviceScopeFactory = serviceScopeFactory;
            _permissionService = permissionService;
            _radzenPagerService = radzenPagerService;
        }

        public async Task<IActionResult<List<Menu>>> GetAllMenus()
        {
            var rs = new IActionResult<List<Menu>> { Result = new List<Menu>() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Menu>();
                    var menus = await repo.GetAllAsync(predicate: null);
                    rs.Result = menus.OrderBy(x => x.Order).ThenBy(x => x.Id).ToList();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetAllMenus error");
                rs.Result = new List<Menu>();
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<Menu>>> GetMenuHierarchy()
        {
            var rs = new IActionResult<List<Menu>> { Result = new List<Menu>() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Menu>();
                    
                    // Tüm menüleri çek
                    var allMenus = await repo.GetAllAsync(predicate: null);
                    var allMenuIds = new HashSet<int>(allMenus.Select(x => x.Id));
                    
                    _logger.LogInformation("GetMenuHierarchy: Toplam {ToplamMenu} menü bulundu. Menü ID'leri: {MenuIds}", 
                        allMenus.Count, string.Join(", ", allMenus.Select(x => $"{x.Id}:{x.Name}")));
                    
                    // Root menüler + orphan menüler (parent'ı silinmiş olanlar)
                    var rootMenus = allMenus.Where(x => 
                        x.ParentId == null || x.ParentId == 0 ||
                        !allMenuIds.Contains(x.ParentId.Value)
                    ).ToList();
                    
                    _logger.LogInformation("GetMenuHierarchy: {RootCount} root menü bulundu", rootMenus.Count);

                    foreach (var menu in rootMenus)
                    {
                        FillChildren(menu, allMenus);
                    }

                    rs.Result = rootMenus.OrderBy(x => x.Order).ThenBy(x => x.Id).ToList();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMenuHierarchy error");
                rs.Result = new List<Menu>();
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        private void FillChildren(Menu parent, IEnumerable<Menu> allMenus)
        {
            parent.InverseParent = allMenus.Where(x => x.ParentId == parent.Id).ToList();
            foreach (var child in parent.InverseParent)
            {
                FillChildren(child, allMenus);
            }
        }

        public async Task<IActionResult<Paging<List<MenuListDto>>>> GetPagedMenus(PageSetting pager)
        {
            IActionResult<Paging<List<MenuListDto>>> response = new() { 
                Result = new Paging<List<MenuListDto>> 
                { 
                    Data = new List<MenuListDto>(), 
                    DataCount = 0 
                } 
            };
            
            try
            {
                pager.Skip ??= 0;
                pager.Take ??= 10;
                
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Menu>();
                    
                    var menus = await repo.GetAllAsync(predicate: null, include: source => source.Include(x => x.Parent).Include(x => x.RoleMenus).ThenInclude(rm => rm.Role));
                    
                    if (menus == null || !menus.Any())
                        return response;
                    
                    var mapped = _mapper.Map<List<MenuListDto>>(menus);
                    
                    if (mapped == null || mapped.Count == 0)
                        return response;
                    
                    // RoleNames'i manuel doldur
                    foreach (var item in mapped)
                    {
                        var sourceMenu = menus.FirstOrDefault(m => m.Id == item.Id);
                        if (sourceMenu != null && sourceMenu.RoleMenus != null)
                        {
                            item.RoleNames = sourceMenu.RoleMenus
                                .Where(rm => rm.Role != null && rm.CanView)
                                .Select(rm => rm.Role!.Name!)
                                .ToList();
                        }
                    }

                    // In-memory paging
                    var ordered = mapped.OrderByDescending(x => x.Id).ToList();
                    var totalCount = ordered.Count;
                    var pagedData = ordered.Skip(pager.Skip.Value).Take(pager.Take.Value).ToList();
                    
                    response.Result = new Paging<List<MenuListDto>>
                    {
                        Data = pagedData,
                        DataCount = totalCount
                    };
                }
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetPagedMenus hatası");
                response.AddSystemError(ex.Message);
                return response;
            }
        }

        public async Task<IActionResult<MenuUpsertDto>> GetMenuById(int id)
        {
            var rs = new IActionResult<MenuUpsertDto> { Result = new() };
            try
            {
                // This is for Edit modal. 
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                     var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                     var repo = uow.GetRepository<Menu>();
                     var menu = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == id);
                
                     if (menu == null)
                     {
                         rs.AddError("Menü bulunamadı");
                         return rs;
                     }

                     rs.Result = _mapper.Map<MenuUpsertDto>(menu);
                     
                     // Helper: Load Selected Roles
                     var roleMenuRepo = uow.GetRepository<RoleMenu>();
                     var allowedRoleMenus = await roleMenuRepo.GetAllAsync(predicate: x => x.MenuId == id && x.CanView);
                     rs.Result.SelectedRoleIds = allowedRoleMenus.Select(x => x.RoleId).ToList();
                }
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMenuById error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> UpsertMenu(MenuUpsertDto dto)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Menu>();
                    var roleMenuRepo = uow.GetRepository<RoleMenu>();

                    Menu entity;
                    bool isNew = false;
                    
                    if (!dto.Id.HasValue || dto.Id == 0)
                    {
                        entity = _mapper.Map<Menu>(dto);
                        await repo.InsertAsync(entity);
                        isNew = true;
                    }
                    else
                    {
                        entity = await repo.GetFirstOrDefaultAsync(predicate: x => x.Id == dto.Id, disableTracking: false);
                        if (entity == null)
                        {
                            rs.AddError("Menü bulunamadı");
                            return rs;
                        }

                        _mapper.Map(dto, entity);
                        repo.Update(entity);
                    }

                    // Save to get ID for new items
                    await uow.SaveChangesAsync();
                    
                    // Check ID
                    var menuId = entity.Id;

                    // Handle RoleMenus
                    // 1. Get existing role menus
                    var existingRoleMenus = await roleMenuRepo.GetAllAsync(predicate: x => x.MenuId == menuId);
                    
                    // 2. Identify to delete (those NOT in selected list)
                    var selectedIds = dto.SelectedRoleIds ?? new List<int>();
                    
                    var toDelete = existingRoleMenus.Where(x => !selectedIds.Contains(x.RoleId)).ToList();
                    if(toDelete.Any()) roleMenuRepo.Delete(toDelete);
                    
                    // 3. Identify to add (those in selected list but NOT in existing)
                    var existingRoleIds = existingRoleMenus.Select(x => x.RoleId).ToList();
                    var toAddIds = selectedIds.Where(id => !existingRoleIds.Contains(id)).ToList();
                    
                    var newRoleMenus = toAddIds.Select(roleId => new RoleMenu
                    {
                        MenuId = menuId,
                        RoleId = roleId,
                        CanView = true, // Default permission
                        CanCreate = true,
                        CanEdit = true,
                        CanDelete = true
                    }).ToList();
                    
                    if(newRoleMenus.Any()) await roleMenuRepo.InsertAsync(newRoleMenus);

                    // Save changes for roles
                    await uow.SaveChangesAsync();
                    
                    rs.AddSuccess("Menü başarıyla kaydedildi");

                    return rs;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpsertMenu error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<Empty>> DeleteMenu(int id)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Menu>();
                    var menu = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == id,
                        include: source => source.Include(x => x.InverseParent),
                        disableTracking: false
                    );

                    if (menu == null)
                    {
                        rs.AddError("Menü bulunamadı");
                        return rs;
                    }

                    if (menu.InverseParent != null && menu.InverseParent.Count > 0)
                    {
                        rs.AddError("Bu menünün alt menüleri var, önce onları silmelisiniz");
                        return rs;
                    }

                    repo.Delete(menu);
                    await uow.SaveChangesAsync();
                    var lastResult = uow.LastSaveChangesResult;

                    if (lastResult.IsOk)
                    {
                        rs.AddSuccess("Menü başarıyla silindi");
                        return rs;
                    }
                    else
                    {
                        if (lastResult.Exception != null)
                            rs.AddError(lastResult.Exception.ToString());
                        return rs;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteMenu error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }

        public async Task<IActionResult<List<Menu>>> GetMenusForCurrentUser()
        {
            var response = OperationResult.CreateResult<List<Menu>>();
            
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    
                    // Get userId from claims (secure - backend only)
                    var userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                    {
                        _logger.LogWarning("User not authenticated or userId claim not found");
                        response.Result = new List<Menu>();
                        return response;
                    }
                    
                    // Get all menus - RE-USE LOGIC but inside this scope
                    var menuRepo = uow.GetRepository<Menu>();
                    var allMenusList = await menuRepo.GetAllAsync(predicate: null);
                    var allMenus = allMenusList.ToList(); // Materialize
                    
                    // Get role-based menu IDs from RoleMenu table
                    var roleMenuIds = new List<int>();
                    var roleMenuRepo = uow.GetRepository<ecommerce.Core.Entities.RoleMenu>();
                    
                    // Get user's roles first
                    var userRepo = uow.GetRepository<ecommerce.Core.Entities.Authentication.ApplicationUser>();
                    var user = await userRepo.GetFirstOrDefaultAsync(
                        predicate: u => u.Id == userId,
                        include: source => source.Include(u => u.Roles)
                    );
                    
                    if (user?.Roles != null)
                    {
                        var roleIds = user.Roles.Select(r => r.Id).ToList();
                        
                        // Get role menus for these roles
                        // Get role menus for these roles
                        var roleMenus = await roleMenuRepo.GetAllAsync(
                            predicate: rm => roleIds.Contains(rm.RoleId) && rm.CanView
                        );
                        
                        roleMenuIds = roleMenus.Select(rm => rm.MenuId).Distinct().ToList();
                    }
                    
                    // Get user-specific menu IDs directly from UserMenu repository
                    var userMenuIds = new List<int>();
                    var userMenuRepo = uow.GetRepository<ecommerce.Core.Entities.UserMenu>();
                    var userMenus = await userMenuRepo.GetAllAsync(
                        predicate: um => um.UserId == userId
                    );
                    userMenuIds = userMenus.Select(um => um.MenuId).ToList();
                    
                    // Combine both (union)
                    var allowedMenuIds = roleMenuIds.Union(userMenuIds).ToHashSet();
                    
                    _logger.LogInformation($"User {userId}: Role menus={roleMenuIds.Count}, User menus={userMenuIds.Count}, Total={allowedMenuIds.Count}");
                    
                    // Filter menus recursively (in-memory) relying on the fetched allMenus
                    // But we need the hierarchical structure. 
                    // Let's reconstruct hierarchy locally or reuse logic properly.
                    
                    // Helper logic to build hierarchy from flat list:
                    foreach(var m in allMenus) m.InverseParent = new List<Menu>(); // Clear first
                    var rootMenus = allMenus.Where(x => x.ParentId == null || x.ParentId == 0).ToList();
                    foreach(var m in allMenus.Where(x => x.ParentId.HasValue && x.ParentId > 0)) {
                        var p = allMenus.FirstOrDefault(x => x.Id == m.ParentId);
                        if(p != null) p.InverseParent.Add(m);
                    }

                    // Now filter
                    response.Result = FilterMenusByPermissions(rootMenus, allowedMenuIds);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetMenusForCurrentUser error");
                response.AddSystemError(ex.Message);
            }
            
            return response;
        }
        
        private List<Menu> FilterMenusByPermissions(List<Menu> allMenus, HashSet<int> allowedMenuIds)
        {
            var filtered = new List<Menu>();
            
            foreach (var menu in allMenus)
            {
              
                var filteredChildren = new List<Menu>();
                if (menu.InverseParent != null && menu.InverseParent.Count > 0)
                {
                    filteredChildren = FilterMenusByPermissions(menu.InverseParent.ToList(), allowedMenuIds);
                }


                bool isExplicitlyAllowed = allowedMenuIds.Contains(menu.Id);
                bool hasVisibleChildren = filteredChildren.Count > 0;

                if (isExplicitlyAllowed || hasVisibleChildren)
                {
                    var menuCopy = new Menu
                    {
                        Id = menu.Id,
                        Name = menu.Name,
                        Path = menu.Path,
                        Icon = menu.Icon,
                        ParentId = menu.ParentId,
                        InverseParent = filteredChildren,
                        Order = menu.Order
                    };
                    
                    filtered.Add(menuCopy);
                }
            }
            
            // Sort by Order
            return filtered.OrderBy(m => m.Order).ToList();
        }

        public async Task<IActionResult<Empty>> UpdateMenuLocation(int id, int? parentId, int order)
        {
            var rs = new IActionResult<Empty> { Result = new Empty() };
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
                    var repo = uow.GetRepository<Menu>();
                    
                    var menu = await repo.GetFirstOrDefaultAsync(
                        predicate: x => x.Id == id,
                        disableTracking: false
                    );

                    if (menu == null)
                    {
                        rs.AddError("Menü bulunamadı");
                        return rs;
                    }

                    // 1. If moving to a new parent or just reordering within same parent
                    var oldParentId = menu.ParentId;
                    
                    // Update the moved menu
                    menu.ParentId = parentId == 0 ? null : parentId;
                    // We don't set Order immediately, we will re-calculate all siblings
                    
                    // 2. Get all siblings in the TARGET parent
                    var siblings = await repo.GetAllAsync(predicate: x => x.ParentId == menu.ParentId && x.Id != id);
                    var siblingList = siblings.OrderBy(x => x.Order).ToList();
                    
                    // Adjust order if moving within same parent (compensate for the removed item's gap)
                    if (oldParentId == parentId && menu.Order < order)
                    {
                        order--;
                    }

                    // 3. Insert the menu at the desired position
                    if (order < 0) order = 0;
                    if (order > siblingList.Count) order = siblingList.Count;
                    
                    siblingList.Insert(order, menu);
                    
                    // 4. Update Order for all
                    for (int i = 0; i < siblingList.Count; i++)
                    {
                         siblingList[i].Order = i;
                         repo.Update(siblingList[i]);
                    }

                    await uow.SaveChangesAsync();
                }
                rs.AddSuccess("Menü güncellendi");
                return rs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateMenuLocation error");
                rs.AddSystemError(ex.Message);
                return rs;
            }
        }
    }
}