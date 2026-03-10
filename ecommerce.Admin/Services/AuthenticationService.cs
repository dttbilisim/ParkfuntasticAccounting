using System.Security.Claims;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Identity;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ecommerce.Admin.Helpers;

namespace ecommerce.Admin.Services;

public class AuthenticationService
{
    private readonly NavigationManager _navigationManager;

    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CurrentUser _currentUser;
    private readonly IMemoryCache _cache;

    public ApplicationUser User { get; private set; } = new ApplicationUser { UserName = "Anonymous" };

    public ClaimsPrincipal Principal { get; private set; }

    public int? SelectedCustomerId { get; private set; }
    public string? SelectedCustomerName { get; private set; }
    public event Action OnSelectedCustomerChanged;

    public async Task SetSelectedCustomer(int? customerId, string? customerName = null)
    {
        SelectedCustomerId = customerId;
        SelectedCustomerName = customerName;

        // Persist to Server-Side Cache
        if (User.Id > 0)
        {
            var cacheKey = $"{SessionKeys.SelectedCustomerId}_{User.Id}";
            var nameCacheKey = $"{SessionKeys.SelectedCustomerName}_{User.Id}";
            
            if (customerId.HasValue)
            {
                // Security check: Only allow if Plasiyer has access (optional but recommended)
                // For now we assume the UI only allows valid customers
                
                _cache.Set(cacheKey, customerId.Value, TimeSpan.FromHours(8));
                if (!string.IsNullOrEmpty(customerName))
                {
                    _cache.Set(nameCacheKey, customerName, TimeSpan.FromHours(8));
                }
            }
            else
            {
                _cache.Remove(cacheKey);
                _cache.Remove(nameCacheKey);
            }
        }

        if (customerId.HasValue)
        {
            var targetUserId = await FindUserIdByCustomerId(customerId.Value);
            if (targetUserId.HasValue)
            {
                var identity = new ClaimsIdentity(Principal.Identity);
                
                // Impersonate the customer's user for cart and order operations
                identity.AddOrReplace(new Claim(ClaimTypes.NameIdentifier, targetUserId.Value.ToString()));
                identity.AddOrReplace(new Claim("CustomerId", customerId.Value.ToString()));
                
                // Plasiyer yetkisi korunsun — sipariş onay/iptal için SalesPersonId gerekli
                if (User.SalesPersonId.HasValue)
                {
                    identity.AddOrReplace(new Claim("SalesPersonId", User.SalesPersonId.Value.ToString()));
                }
                
                var newPrincipal = new ClaimsPrincipal(identity);
                _currentUser.SetUser(newPrincipal);
            }
        }
        else
        {
            // Reset to original logged-in user context
            _currentUser.SetUser(Principal);
        }

        OnSelectedCustomerChanged?.Invoke();
    }

    private async Task<int?> FindUserIdByCustomerId(int customerId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Find the first user associated with this customer
        var user = await context.AspNetUsers
            .Where(u => u.CustomerId == customerId)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync();
            
        return user?.Id;
    }

    public AuthenticationService(NavigationManager navigationManager, IServiceScopeFactory serviceScopeFactory, CurrentUser currentUser, IMemoryCache cache)
    {
        _navigationManager = navigationManager;
        _serviceScopeFactory = serviceScopeFactory;
        _currentUser = currentUser;
        _cache = cache;
    }

    public bool IsAuthenticated()
    {
        return Principal.IsAuthenticated();
    }

    public async Task<bool> InitializeAsync(AuthenticationState result)
    {
        Principal = result.User;
        _currentUser.SetUser(Principal);

        var idClaim = Principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!string.IsNullOrEmpty(idClaim) && int.TryParse(idClaim, out var userId))
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IUnitOfWork<ApplicationDbContext>>();
            var userRepository = context.GetRepository<ApplicationUser>();

            var applicationUser = await userRepository.GetFirstOrDefaultAsync(
                predicate: u => u.Id == userId,
                include: q => q.Include(u => u.Roles)
            );

            User = applicationUser ?? new ApplicationUser { Id = userId, UserName = "Anonymous" };
            
            // NOTE: ActiveBranchId and ActiveCorporationId claims are now added in CurrentUserMiddleware
            // This ensures claims are available before any DbContext queries or component initialization
            
            // Restore selected customer from Server-Side Cache (only for Plasiyers)
            if (User.SalesPersonId.HasValue)
            {
                var cacheKey = $"{SessionKeys.SelectedCustomerId}_{User.Id}";
                var nameCacheKey = $"{SessionKeys.SelectedCustomerName}_{User.Id}";
                
                if (_cache.TryGetValue(cacheKey, out int customerId))
                {
                    _cache.TryGetValue(nameCacheKey, out string? customerName);
                    await SetSelectedCustomer(customerId, customerName);
                }
            }
        }
        else
        {
            User = new ApplicationUser { UserName = "Anonymous" };
        }

        return IsAuthenticated();
    }

    public async Task Logout()
    {
        // Clear selected customer from Cache
        var cacheKey = $"{SessionKeys.SelectedCustomerId}_{User.Id}";
        var nameCacheKey = $"{SessionKeys.SelectedCustomerName}_{User.Id}";
        _cache.Remove(cacheKey);
        _cache.Remove(nameCacheKey);
        
        _navigationManager.NavigateTo("Account/Logout", true);
    }

    public void Login()
    {
        _navigationManager.NavigateTo("Login", true);
    }
}