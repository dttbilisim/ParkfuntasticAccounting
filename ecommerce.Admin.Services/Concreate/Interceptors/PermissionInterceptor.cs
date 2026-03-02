using Castle.DynamicProxy;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using ecommerce.Admin.Domain.Interfaces;

namespace ecommerce.Admin.Helpers.Concretes
{
    public class PermissionInterceptor : IInterceptor
    {
        private readonly IServiceProvider _serviceProvider;

        public PermissionInterceptor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Intercept(IInvocation invocation)
        {
            var attribute = invocation.MethodInvocationTarget.GetCustomAttribute<PermissionAttribute>();
            if (attribute == null)
            {
                invocation.Proceed();
                return;
            }

            if (IsAsyncMethod(invocation.Method))
            {
                invocation.Proceed();
                var result = invocation.ReturnValue;
                if (result is Task task)
                {
                    invocation.ReturnValue = HandleAsync(task, attribute);
                }
            }
            else
            {
                // Sync method
                CheckPermissionSync(attribute);
                invocation.Proceed();
            }
        }

        private async Task HandleAsync(Task task, PermissionAttribute attribute)
        {
            await CheckPermissionAsync(attribute);
            await task;
        }

        private bool IsAsyncMethod(MethodInfo method)
        {
            return (method.ReturnType == typeof(Task) ||
                    (method.ReturnType.IsGenericType && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)));
        }

        private async Task CheckPermissionAsync(PermissionAttribute attribute)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var httpContextAccessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
                var userMenuService = scope.ServiceProvider.GetRequiredService<IUserMenuService>();

                var user = httpContextAccessor.HttpContext?.User;
                if (user == null || !user.Identity.IsAuthenticated)
                {
                    // If no user context (e.g. background job), we might skip or fail. 
                    // Fail safe:
                    throw new UnauthorizedAccessException("User is not authenticated.");
                }

                var userIdStr = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                     throw new UnauthorizedAccessException("Invalid User ID.");
                }

                bool hasPermission = await userMenuService.HasPermission(userId, attribute.MenuPath, attribute.PermissionType);
                if (!hasPermission)
                {
                     throw new UnauthorizedAccessException($"You do not have '{attribute.PermissionType}' permission for '{attribute.MenuPath}'.");
                }
            }
        }

        private void CheckPermissionSync(PermissionAttribute attribute)
        {
             CheckPermissionAsync(attribute).GetAwaiter().GetResult();
        }
    }
}
