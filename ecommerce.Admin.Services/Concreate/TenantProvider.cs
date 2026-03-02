using System.Security.Claims;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Identity;
using Microsoft.AspNetCore.Http;

namespace ecommerce.Admin.Services.Concreate
{
    public class TenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly CurrentUser _currentUser;

        public TenantProvider(IHttpContextAccessor httpContextAccessor, CurrentUser currentUser)
        {
            _httpContextAccessor = httpContextAccessor;
            _currentUser = currentUser;
        }

        public bool IsGlobalAdmin
        {
            get
            {
                var user = _currentUser.Principal ?? _httpContextAccessor.HttpContext?.User;
                if (user == null) return false;
                return user.IsInRole("Admin") || user.IsInRole("SuperAdmin") || 
                       user.IsInRole("MainRoot") || user.IsInRole("Administrator");
            }
        }

        public bool IsB2BAdmin
        {
            get
            {
                if (IsGlobalAdmin) return false; // Admin değilse
                var user = _currentUser.Principal ?? _httpContextAccessor.HttpContext?.User;
                return user != null && (user.IsInRole("B2BADMIN") || user.IsInRole("B2B_ADMIN"));
            }
        }

        public bool IsPlasiyer
        {
            get
            {
                var user = _currentUser.Principal ?? _httpContextAccessor.HttpContext?.User;
                return user != null && user.IsInRole("Plasiyer");
            }
        }

        public bool IsCustomerB2B
        {
            get
            {
                var user = _currentUser.Principal ?? _httpContextAccessor.HttpContext?.User;
                return user != null && user.IsInRole("CustomerB2B");
            }
        }

        public int GetCurrentBranchId()
        {
            // PRIORITY 1: Try to get from CurrentUser claims (Blazor Server context, from middleware)
            // This is the most authoritative as it is discovered from DB or updated via SetActiveBranchId
            var claim = _currentUser.Principal?.FindFirst("ActiveBranchId");
            if (claim != null && int.TryParse(claim.Value, out var branchId))
            {
                return branchId;
            }

            // PRIORITY 2: Check Cookie for user-selected branch (Fallback for API or when claim is missing)
            var cookieBranchId = _httpContextAccessor.HttpContext?.Request.Cookies["ActiveBranchId"];
            if (!string.IsNullOrEmpty(cookieBranchId) && int.TryParse(cookieBranchId, out var branchFromCookie) && branchFromCookie > 0)
            {
                return branchFromCookie;
            }
            
            // PRIORITY 3: Fallback to HttpContext for API/MVC scenarios
            var httpClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("ActiveBranchId");
            return httpClaim != null && int.TryParse(httpClaim.Value, out var id) ? id : 0;
        }

        public int GetCurrentCorporationId()
        {
            // First try to get from CurrentUser (Blazor Server context)
            var claim = _currentUser.Principal?.FindFirst("ActiveCorporationId");
            if (claim != null && int.TryParse(claim.Value, out var corpId))
            {
                return corpId;
            }
            
            // Fallback to HttpContext for API/MVC scenarios
            var httpClaim = _httpContextAccessor.HttpContext?.User?.FindFirst("ActiveCorporationId");
            return httpClaim != null && int.TryParse(httpClaim.Value, out var id) ? id : 0;
        }

        public void SetActiveBranchId(int branchId)
        {
            // 1. Save to Cookie (CRITICAL for persistence - works with Blazor Server forceLoad)
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            };
            _httpContextAccessor.HttpContext?.Response.Cookies.Append("ActiveBranchId", branchId.ToString(), cookieOptions);
            
            // 2. Update claim in CurrentUser principal if available (for current request)
            if (_currentUser.Principal?.Identity is ClaimsIdentity identity)
            {
                // Remove existing ActiveBranchId claim
                var existingClaim = identity.FindFirst("ActiveBranchId");
                if (existingClaim != null)
                {
                    identity.RemoveClaim(existingClaim);
                }
                
                // Add new ActiveBranchId claim
                identity.AddClaim(new Claim("ActiveBranchId", branchId.ToString()));
            }
            
            // 3. Also update HttpContext User claims (for API/MVC scenarios)
            if (_httpContextAccessor.HttpContext?.User?.Identity is ClaimsIdentity httpIdentity)
            {
                var existingHttpClaim = httpIdentity.FindFirst("ActiveBranchId");
                if (existingHttpClaim != null)
                {
                    httpIdentity.RemoveClaim(existingHttpClaim);
                }
                httpIdentity.AddClaim(new Claim("ActiveBranchId", branchId.ToString()));
            }
        }

        public int? GetSalesPersonId()
        {
            // Plasiyer için SalesPersonId claim'inden al
            var claim = _currentUser.Principal?.FindFirst("SalesPersonId") ?? 
                       _httpContextAccessor.HttpContext?.User?.FindFirst("SalesPersonId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        public int? GetCustomerId()
        {
            // CustomerB2B için CustomerId claim'inden al
            var claim = _currentUser.Principal?.FindFirst("CustomerId") ?? 
                       _httpContextAccessor.HttpContext?.User?.FindFirst("CustomerId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        public bool IsMultiTenantEnabled => true;
    }
}
