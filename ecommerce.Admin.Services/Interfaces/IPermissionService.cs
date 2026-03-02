using System.Security.Claims;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IPermissionService
    {
        Task<bool> HasPermission(string path, ClaimsPrincipal user);
        Task ClearCache();
    }
}
