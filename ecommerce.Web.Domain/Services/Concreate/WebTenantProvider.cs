using System.Security.Claims;
using ecommerce.Core.Interfaces;
using Microsoft.AspNetCore.Http;

namespace ecommerce.Web.Domain.Services.Concreate
{
    public class WebTenantProvider : ITenantProvider
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public WebTenantProvider(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public int GetCurrentBranchId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("ActiveBranchId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }

        public int GetCurrentCorporationId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("ActiveCorporationId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : 0;
        }

        public bool IsMultiTenantEnabled => false;
        
        public bool IsGlobalAdmin => _httpContextAccessor.HttpContext?.User?.Identity != null && 
                                     (_httpContextAccessor.HttpContext.User.IsInRole("SuperAdmin") || 
                                      _httpContextAccessor.HttpContext.User.IsInRole("MainRoot") || 
                                      _httpContextAccessor.HttpContext.User.IsInRole("Admin"));

        public bool IsB2BAdmin
        {
            get
            {
                if (IsGlobalAdmin) return false;
                return _httpContextAccessor.HttpContext?.User?.IsInRole("B2BADMIN") ?? false;
            }
        }

        public bool IsPlasiyer => _httpContextAccessor.HttpContext?.User?.IsInRole("Plasiyer") ?? false;

        public bool IsCustomerB2B => _httpContextAccessor.HttpContext?.User?.IsInRole("CustomerB2B") ?? false;

        public int? GetSalesPersonId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("SalesPersonId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        public int? GetCustomerId()
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst("CustomerId");
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }
        
        public void SetActiveBranchId(int branchId)
        {
            // Web project doesn't support branch switching - no-op
        }
    }
}
