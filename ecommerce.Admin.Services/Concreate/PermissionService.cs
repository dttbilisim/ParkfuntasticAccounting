using System.Security.Claims;
using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Entities.Authentication;
using ecommerce.EFCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace ecommerce.Admin.Services.Concreate
{
    public class PermissionService : IPermissionService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IMemoryCache _cache;

        public PermissionService(IServiceScopeFactory scopeFactory, IMemoryCache cache)
        {
            _scopeFactory = scopeFactory;
            _cache = cache;
        }

        public async Task<bool> HasPermission(string path, ClaimsPrincipal user)
        {
            if (user == null || !user.Identity.IsAuthenticated) return false;

            // 1. Check Super Admin Roles (God Mode)
            var roles = user.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
            
            if (user.IsInRole("SuperAdmin") || 
                user.IsInRole("MainRoot") || 
                user.IsInRole("Admin") ||
                user.IsInRole("B2BADMIN") ||
                user.IsInRole("B2B_ADMIN"))
            {
                return true;
            }

            // Sipariş onayı sayfası sepetteki "Siparişi Tamamla" ile erişilir, menü yetkisi gerektirmez
            var normalizedPathForBypass = NormalizePath(path);
            if (normalizedPathForBypass == "admin-checkout" || normalizedPathForBypass.StartsWith("admin-checkout/"))
            {
                return true;
            }

            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId)) return false;

            // 2. Get User Permissions and All Paths from Cache
            var cacheKey = $"User_Permissions_{userId}";
            if (!_cache.TryGetValue(cacheKey, out (HashSet<string> Allowed, HashSet<string> AllManaged) permissions))
            {
                permissions = await LoadPermissionsAsync(userId);
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10));
                _cache.Set(cacheKey, permissions, cacheEntryOptions);
            }

            // 3. Normalize path
            var normalizedPath = NormalizePath(path);
            if (string.IsNullOrEmpty(normalizedPath)) return true; 

            // 4. Exact match first
            Console.WriteLine($"[PermissionService] normalizedPath='{normalizedPath}', AllManaged contains={permissions.AllManaged.Contains(normalizedPath)}, Allowed contains={permissions.Allowed.Contains(normalizedPath)}");
            Console.WriteLine($"[PermissionService] AllManaged=[{string.Join(",", permissions.AllManaged.Take(10))}...]");
            Console.WriteLine($"[PermissionService] Allowed=[{string.Join(",", permissions.Allowed.Take(10))}...]");
            
            if (permissions.Allowed.Contains(normalizedPath)) return true;

            // 5. Check if this path is even managed by the system
            // If the path is NOT in any menu, we default to ALLOW (assuming it's a non-menu page)
            // UNLESS we find it's a child of a managed path
            
            bool isManaged = permissions.AllManaged.Contains(normalizedPath);
            Console.WriteLine($"[PermissionService] isManaged={isManaged} for path '{normalizedPath}'");
            if (!isManaged)
            {
                // Check if it's a child of a managed path
                // e.g. /categories/edit/5
                // If /categories is managed, then /categories/edit/5 should be managed?
                // Logic: If I have a menu /categories, and I go to /categories/edit/5,
                // I expect /categories permission to cover it.
                // If /categories is NOT allowed, then /categories/edit/5 is NOT allowed.
                
                foreach (var managed in permissions.AllManaged)
                {
                     if (normalizedPath.StartsWith(managed + "/"))
                     {
                         // Parent is managed. Is parent allowed?
                         if (permissions.Allowed.Contains(managed)) return true;
                         
                         // Parent is managed but NOT allowed.
                         // So this child path is effectively managed and denied.
                         return false; 
                     }
                }
                
                // If not child of any managed path, allowing.
                return true;
            }

            return false; // Managed (in AllManaged) but not in Allowed.
        }

        public async Task ClearCache()
        {
            await Task.CompletedTask;
        }

        private async Task<(HashSet<string> Allowed, HashSet<string> AllManaged)> LoadPermissionsAsync(int userId)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Get All Menu Paths (Managed Paths)
                var allPaths = await context.Menus
                    .Where(m => !string.IsNullOrEmpty(m.Path))
                    .Select(m => m.Path)
                    .ToListAsync();

                // Get User's Roles
                var userRoleIds = await context.UserRoles
                    .Where(ur => ur.UserId == userId)
                    .Select(ur => ur.RoleId)
                    .ToListAsync();

                // Get Allowed MenuIds (Roles)
                var roleMenuIds = await context.RoleMenus
                    .Where(rm => userRoleIds.Contains(rm.RoleId) && rm.CanView == true)
                    .Select(rm => rm.MenuId)
                    .ToListAsync();

                // Get Allowed MenuIds (Direct User Permissions)
                var userMenuIds = await context.UserMenus
                    .Where(um => um.UserId == userId && um.CanView == true)
                    .Select(um => um.MenuId)
                    .ToListAsync();

                var allAllowedMenuIds = roleMenuIds.Union(userMenuIds).Distinct().ToList();

                // Get Allowed Paths
                var allowedPaths = await context.Menus
                    .Where(m => allAllowedMenuIds.Contains(m.Id) && !string.IsNullOrEmpty(m.Path))
                    .Select(m => m.Path)
                    .ToListAsync();

                return (
                    allowedPaths.Select(p => NormalizePath(p)).ToHashSet(),
                    allPaths.Select(p => NormalizePath(p)).ToHashSet()
                );
            }
        }

        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            if (path.Contains("?")) path = path.Split('?')[0];
            return path.Trim('/').ToLowerInvariant();
        }
    }
}
