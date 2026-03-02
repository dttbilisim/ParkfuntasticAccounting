using System.Security.Claims;
using System.Security.Principal;

namespace ecommerce.Core.Identity;

public static class ClaimsIdentityExtensions
{
    public static int? FindUserId(this ClaimsPrincipal? principal)
    {
        var userIdOrNull = principal?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdOrNull == null || string.IsNullOrWhiteSpace(userIdOrNull.Value))
        {
            return null;
        }

        if (int.TryParse(userIdOrNull.Value, out var val))
        {
            return val;
        }

        return null;
    }

    public static int GetUserId(this ClaimsPrincipal? principal)
    {
        return FindUserId(principal) ?? throw new Exception("Yetkilendirme bilgileri bulunamadı.");
    }

    public static int? FindUserId(this IIdentity? identity)
    {
        var claimsIdentity = identity as ClaimsIdentity;

        var userIdOrNull = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
        if (userIdOrNull == null || string.IsNullOrWhiteSpace(userIdOrNull.Value))
        {
            return null;
        }

        if (int.TryParse(userIdOrNull.Value, out var val))
        {
            return val;
        }

        return null;
    }

    public static int GetUserId(this IIdentity? identity)
    {
        return FindUserId(identity) ?? throw new Exception("Yetkilendirme bilgileri bulunamadı.");
    }

    public static int? FindCompanyId(this ClaimsPrincipal? principal)
    {
        var companyIdOrNull = principal?.Claims.FirstOrDefault(c => c.Type == ecommerceClaimTypes.CompanyId);
        if (companyIdOrNull == null || string.IsNullOrWhiteSpace(companyIdOrNull.Value))
        {
            return null;
        }

        if (int.TryParse(companyIdOrNull.Value, out var val))
        {
            return val;
        }

        return null;
    }

    public static int GetCompanyId(this ClaimsPrincipal? principal)
    {
        return FindCompanyId(principal) ?? throw new Exception("Yetkilendirme bilgileri bulunamadı.");
    }

    public static int? FindCompanyId(this IIdentity? identity)
    {
        var claimsIdentity = identity as ClaimsIdentity;

        var companyIdOrNull = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ecommerceClaimTypes.CompanyId);
        if (companyIdOrNull == null || string.IsNullOrWhiteSpace(companyIdOrNull.Value))
        {
            return null;
        }

        if (int.TryParse(companyIdOrNull.Value, out var val))
        {
            return val;
        }

        return null;
    }

    public static int GetCompanyId(this IIdentity? identity)
    {
        return FindCompanyId(identity) ?? throw new Exception("Yetkilendirme bilgileri bulunamadı.");
    }

    public static string? FindFullName(this ClaimsPrincipal? principal)
    {
        var fullName = principal?.FindFirstValue(ecommerceClaimTypes.FullName);
        if (!string.IsNullOrWhiteSpace(fullName)) return fullName;

        var first = principal?.FindFirstValue(ecommerceClaimTypes.FirstName);
        var last = principal?.FindFirstValue(ecommerceClaimTypes.LastName);
        var combined = ($"{first} {last}").Trim();
        if (!string.IsNullOrWhiteSpace(combined)) return combined;

        // Son çare: CompanyName ya da e-posta kullanıcı adı kısmı
        var company = principal?.FindFirstValue(ecommerceClaimTypes.CompanyName);
        if (!string.IsNullOrWhiteSpace(company)) return company;

        var email = principal?.FindFirstValue(ClaimTypes.Email) ?? principal?.FindFirstValue("email");
        if (!string.IsNullOrWhiteSpace(email))
        {
            var atIdx = email.IndexOf('@');
            return atIdx > 0 ? email.Substring(0, atIdx) : email;
        }

        return null;
    }

    public static string? FindFullName(this IIdentity? identity)
    {
        var claimsIdentity = identity as ClaimsIdentity;

        return claimsIdentity?.FindFirst(ecommerceClaimTypes.FullName)?.Value;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal? principal)
    {
        return principal?.Identity?.IsAuthenticated ?? false;
    }

    public static string[] FindRoles(this ClaimsPrincipal? principal)
    {
        return principal?.Identities.SelectMany(i => i.FindAll(ClaimTypes.Role)).Select(c => c.Value).Distinct().ToArray() ?? Array.Empty<string>();
    }

    public static ClaimsIdentity AddIfNotContains(this ClaimsIdentity claimsIdentity, Claim claim)
    {
        if (!claimsIdentity.Claims.Any(x => string.Equals(x.Type, claim.Type, StringComparison.OrdinalIgnoreCase)))
        {
            claimsIdentity.AddClaim(claim);
        }

        return claimsIdentity;
    }

    public static ClaimsIdentity AddOrReplace(this ClaimsIdentity claimsIdentity, Claim claim)
    {
        foreach (var x in claimsIdentity.FindAll(claim.Type).ToList())
        {
            claimsIdentity.RemoveClaim(x);
        }

        claimsIdentity.AddClaim(claim);

        return claimsIdentity;
    }

    public static ClaimsPrincipal AddIdentityIfNotContains(this ClaimsPrincipal principal, ClaimsIdentity identity)
    {
        if (!principal.Identities.Any(x => string.Equals(x.AuthenticationType, identity.AuthenticationType, StringComparison.OrdinalIgnoreCase)))
        {
            principal.AddIdentity(identity);
        }

        return principal;
    }
}