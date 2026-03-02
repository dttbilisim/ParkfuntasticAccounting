using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Admin;
using ecommerce.Core.Interfaces;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq.Expressions;

namespace ecommerce.Admin.Domain.Services
{
    public interface IPermissionService
    {
        Task<bool> CanView(string menuName);
        Task<bool> CanCreate(string menuName);
        Task<bool> CanEdit(string menuName);
        Task<bool> CanDelete(string menuName);
    }

    public class PermissionService : IPermissionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public PermissionService(IHttpContextAccessor httpContextAccessor, IServiceScopeFactory serviceScopeFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _serviceScopeFactory = serviceScopeFactory;
        }

        private async Task<bool> CheckPermission(string menuName, Func<RoleMenu, bool> rolePredicate, Func<UserMenu, bool> userPredicate)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return false;

            // Global Admin bypass
            if (user.IsInRole("Admin")) return true;

            var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return false;

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var menuNameLower = menuName.ToLower();
            var menuNameSlashLower = "/" + menuNameLower;

            // Check User Specific Permissions first
            var userPermission = await dbContext.UserMenus
                .AsNoTracking()
                .Where(um => um.UserId == userId)
                .Where(um => um.Menu.Path.ToLower() == menuNameLower || um.Menu.Path.ToLower() == menuNameSlashLower)
                .ToListAsync();

            if (userPermission.Any(userPredicate)) return true;

            // Check Role Permissions
            var userRoles = await dbContext.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToListAsync();

            if (!userRoles.Any()) return false;

            var rolePermissions = await dbContext.RoleMenus
                .AsNoTracking()
                .Where(rm => userRoles.Contains(rm.RoleId))
                .Where(rm => rm.Menu.Path.ToLower() == menuNameLower || rm.Menu.Path.ToLower() == menuNameSlashLower)
                .ToListAsync();

            return rolePermissions.Any(rolePredicate);
        }

        public async Task<bool> CanView(string menuName)
        {
            return await CheckPermission(menuName, x => x.CanView, x => x.CanView);
        }

        public async Task<bool> CanCreate(string menuName)
        {
             return await CheckPermission(menuName, x => x.CanCreate, x => x.CanCreate);
        }

        public async Task<bool> CanEdit(string menuName)
        {
             return await CheckPermission(menuName, x => x.CanEdit, x => x.CanEdit);
        }

        public async Task<bool> CanDelete(string menuName)
        {
             return await CheckPermission(menuName, x => x.CanDelete, x => x.CanDelete);
        }
    }
}
