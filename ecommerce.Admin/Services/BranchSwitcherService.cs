using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Security.Claims;

namespace ecommerce.Admin.Services.Concreate
{
    /// <summary>
    /// Service for managing branch switching in multi-tenant environment
    /// </summary>
    public class BranchSwitcherService
    {
        private readonly ITenantProvider _tenantProvider;
        private readonly IBranchService _branchService;
        private readonly IUserBranchService _userBranchService;
        private readonly IIdentityUserService _identityUserService;
        private readonly ISalesPersonService _salesPersonService;
        private readonly NavigationManager _navigationManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJSRuntime _jsRuntime;

#pragma warning disable CS0067 // Event is part of public API for branch change notifications
        public event Action? OnBranchChanged;
#pragma warning restore CS0067

        public BranchSwitcherService(
            ITenantProvider tenantProvider, 
            IBranchService branchService,
            IUserBranchService userBranchService,
            IIdentityUserService identityUserService,
            ISalesPersonService salesPersonService,
            NavigationManager navigationManager,
            IHttpContextAccessor httpContextAccessor,
            IJSRuntime jsRuntime)
        {
            _tenantProvider = tenantProvider;
            _branchService = branchService;
            _userBranchService = userBranchService;
            _identityUserService = identityUserService;
            _salesPersonService = salesPersonService;
            _navigationManager = navigationManager;
            _httpContextAccessor = httpContextAccessor;
            _jsRuntime = jsRuntime;
        }

        /// <summary>
        /// Get current active branch ID
        /// </summary>
        public int GetCurrentBranchId()
        {
            return _tenantProvider.GetCurrentBranchId();
        }

        /// <summary>
        /// Get current branch details
        /// </summary>
        public async Task<(int Id, string Name)?> GetCurrentBranchAsync()
        {
            if (!_tenantProvider.IsMultiTenantEnabled)
                return null;

            var branchId = _tenantProvider.GetCurrentBranchId();
            var result = await _branchService.GetBranchById(branchId);
            
            if (result?.Ok == true && result.Result != null && result.Result.Id.HasValue)
            {
                return (result.Result.Id.Value, result.Result.Name ?? string.Empty);
            }

            return null;
        }

        /// <summary>
        /// Get all branches accessible to current user
        /// </summary>
        public async Task<List<(int Id, string Name)>> GetUserBranchesAsync()
        {
            var branches = new List<(int Id, string Name)>();
            
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null || !user.Identity.IsAuthenticated)
                return branches;

            // Check if Super User - they see all active branches
            if (user.IsInRole("MainRoot") || user.IsInRole("SuperAdmin"))
            {
                var result = await _branchService.GetAllActiveBranches();
                
                if (result?.Ok == true && result.Result != null)
                {
                    branches = result.Result
                        .Select(b => (Id: b.Id, Name: b.Name ?? string.Empty))
                        .OrderBy(b => b.Name)
                        .ToList();
                }
            }
            else
            {
                var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int userId))
                {
                    // Check if user is a SalesPerson
                    var userDetailsResponse = await _identityUserService.GetAsync(userId);
                    
                    if (userDetailsResponse.Ok && userDetailsResponse.Result != null && userDetailsResponse.Result.SalesPersonId.HasValue)
                    {
                        // User is a SalesPerson, get branches from SalesPersonService
                        var salesPersonResponse = await _salesPersonService.GetSalesPersonById(userDetailsResponse.Result.SalesPersonId.Value);
                        if (salesPersonResponse.Ok && salesPersonResponse.Result != null && salesPersonResponse.Result.Branches != null)
                        {
                            branches = salesPersonResponse.Result.Branches
                                .Where(b => b.BranchId > 0)
                                .Select(b => (Id: b.BranchId, Name: b.BranchName ?? "Unknown Branch"))
                                .OrderBy(b => b.Name)
                                .ToList();
                        }
                    }
                    else
                    {
                        // Normal user - only their assigned branches from UserBranches
                        var result = await _userBranchService.GetUserBranches(userId);
                        if (result?.Ok == true && result.Result != null)
                        {
                            branches = result.Result
                                .Select(ub => (Id: ub.BranchId, Name: ub.BranchName ?? "Unknown Branch"))
                                .OrderBy(b => b.Name)
                                .ToList();
                        }
                    }
                }
            }

            return branches;
        }

        /// <summary>
        /// Switch to a different branch
        /// </summary>
        public async Task<bool> SwitchBranchAsync(int newBranchId)
        {
            if (!_tenantProvider.IsMultiTenantEnabled)
                return false;

            var currentBranchId = _tenantProvider.GetCurrentBranchId();
            if (currentBranchId == newBranchId)
                return true; // Already on this branch

            // Verify user has access to this branch
            var userBranches = await GetUserBranchesAsync();
            if (!userBranches.Any(b => b.Id == newBranchId))
                return false; // User doesn't have access

            // CRITICAL: Set branch ID via JavaScript (client-side)
            // This completely avoids server-side cookie/session timing issues
            // JavaScript sets both localStorage AND cookie, then reloads
            await _jsRuntime.InvokeVoidAsync("eval", $@"
                localStorage.setItem('ActiveBranchId', '{newBranchId}');
                document.cookie = 'ActiveBranchId={newBranchId}; path=/; max-age=2592000; SameSite=Lax';
                location.reload();
            ");

            return true;
        }
    }
}

