using System.Security.Claims;
using ecommerce.Core.Entities.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ecommerce.Core.Identity;

public class UserClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>
{
    public UserClaimsPrincipalFactory(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (user.CompanyId.HasValue)
        {
            identity.AddIfNotContains(new Claim(ecommerceClaimTypes.CompanyId, user.CompanyId.Value.ToString()));
        }

        if (user.CustomerId.HasValue)
        {
            identity.AddIfNotContains(new Claim(ecommerceClaimTypes.CustomerId, user.CustomerId.Value.ToString()));
            if (user.Customer != null)
            {
                identity.AddIfNotContains(new Claim(ecommerceClaimTypes.CustomerName, user.Customer.Name));
            }
        }

        if (user.SalesPersonId.HasValue)
        {
            identity.AddIfNotContains(new Claim(ecommerceClaimTypes.SalesPersonId, user.SalesPersonId.Value.ToString()));
        }

        identity.AddIfNotContains(new Claim(ecommerceClaimTypes.FullName, user.FullName));

        return identity;
    }
}