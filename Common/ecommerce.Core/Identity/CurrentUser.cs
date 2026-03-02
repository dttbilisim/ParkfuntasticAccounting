using System.Security.Claims;
using ecommerce.Core.Utils.Threading;

namespace ecommerce.Core.Identity;

public class CurrentUser
{
    
    private readonly AsyncLocal<ClaimsPrincipal> _temporaryPrincipal = new();

    private ClaimsPrincipal _principal = new(new ClaimsIdentity());

    public ClaimsPrincipal Principal => _temporaryPrincipal.Value ?? _principal;

    public bool IsAuthenticated => Principal.IsAuthenticated();

    public int? Id => Principal.FindUserId();

    public int? UserId => Principal.FindCompanyId();

    public string? FullName => Principal.FindFullName();

    public string? Email => Principal.FindFirstValue(ClaimTypes.Email);

    public string[] Roles => Principal.FindRoles();

    public int GetId() => Principal.GetUserId();

    public int GetUserId() => Principal.GetUserId();

    public string? FindClaimValue(string claimType)
    {
        return Principal.FindFirstValue(claimType);
    }

    public virtual Claim? FindClaim(string claimType)
    {
        return Principal.FindFirst(claimType);
    }

    public virtual Claim[] FindClaims(string claimType)
    {
        return Principal.Claims.Where(c => c.Type == claimType).ToArray();
    }

    public virtual Claim[] GetAllClaims()
    {
        return Principal.Claims.ToArray();
    }

    public virtual bool IsInRole(string roleName)
    {
        return Principal.IsInRole(roleName);
    }

    public virtual IDisposable Change(ClaimsPrincipal principal)
    {
        return SetCurrent(principal);
    }

    private IDisposable SetCurrent(ClaimsPrincipal principal)
    {
        var parent = Principal;
        _temporaryPrincipal.Value = principal;

        return new DisposeAction<ValueTuple<AsyncLocal<ClaimsPrincipal>, ClaimsPrincipal>>(
            static (state) =>
            {
                var (currentPrincipal, parent) = state;
                currentPrincipal.Value = parent;
            },
            (_temporaryPrincipal, parent)
        );
    }

    public void SetUser(ClaimsPrincipal user)
    {
        _principal = user;
    }
}