using System.Security.Claims;
using ecommerce.Core.Entities.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace ecommerce.Core.Identity;

public class WebUserClaimsPrincipalFactory : UserClaimsPrincipalFactory<User, ApplicationRole>
{
    public WebUserClaimsPrincipalFactory(
        UserManager<User> userManager,
        RoleManager<ApplicationRole> roleManager,
        IOptions<IdentityOptions> options) : base(userManager, roleManager, options)
    {
    }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(User user)
    {
        var identity = await base.GenerateClaimsAsync(user);

        if (!string.IsNullOrWhiteSpace(user.FirstName))
            identity.AddIfNotContains(new Claim(ecommerceClaimTypes.FirstName, user.FirstName));

        if (!string.IsNullOrWhiteSpace(user.LastName))
            identity.AddIfNotContains(new Claim(ecommerceClaimTypes.LastName, user.LastName));

        if (!string.IsNullOrWhiteSpace(user.CompanyName))
            identity.AddIfNotContains(new Claim(ecommerceClaimTypes.CompanyName, user.CompanyName));

        identity.AddIfNotContains(new Claim(ecommerceClaimTypes.WebUserType, ((int)user.WebUserType).ToString()));
        identity.AddIfNotContains(new Claim(ecommerceClaimTypes.UserId, user.Id.ToString()));

        var fullName = ((user.FirstName ?? string.Empty).Trim() + " " + (user.LastName ?? string.Empty).Trim()).Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            if (!string.IsNullOrWhiteSpace(user.CompanyName))
            {
                fullName = user.CompanyName!.Trim();
            }
            else
            {
                var source = (user.Email ?? user.UserName ?? string.Empty).Trim();
                var atIdx = source.IndexOf('@');
                fullName = atIdx > 0 ? source.Substring(0, atIdx) : source;
            }
        }
        identity.AddOrReplace(new Claim(ecommerceClaimTypes.FullName, fullName));

        return identity;
    }
}


