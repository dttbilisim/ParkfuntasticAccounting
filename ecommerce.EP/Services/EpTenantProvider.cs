using ecommerce.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ecommerce.EP.Services;

/// <summary>
/// Mobil API (EP) için tenant sağlayıcı. Claim/cookie'de ActiveBranchId yoksa (0 dönerse)
/// appsettings'teki Branch:BranchId değerini kullanır; böylece global query filter her zaman
/// tek şube verisini döndürür (farklı şirketlerin indirimi/ürünü karışmaz).
/// </summary>
public class EpTenantProvider : ITenantProvider
{
    private readonly ecommerce.Admin.Services.Concreate.TenantProvider _inner;
    private readonly int _defaultBranchId;

    public EpTenantProvider(
        ecommerce.Admin.Services.Concreate.TenantProvider inner,
        IOptions<Configuration.EpBranchOptions> branchOptions)
    {
        _inner = inner;
        _defaultBranchId = branchOptions?.Value?.BranchId ?? 1;
    }

    public bool IsGlobalAdmin => _inner.IsGlobalAdmin;
    public bool IsB2BAdmin => _inner.IsB2BAdmin;
    public bool IsPlasiyer => _inner.IsPlasiyer;
    public bool IsCustomerB2B => _inner.IsCustomerB2B;
    public bool IsMultiTenantEnabled => true;

    public int GetCurrentBranchId()
    {
        var fromClaimOrCookie = _inner.GetCurrentBranchId();
        return fromClaimOrCookie != 0 ? fromClaimOrCookie : _defaultBranchId;
    }

    public int GetCurrentCorporationId() => _inner.GetCurrentCorporationId();
    public int? GetSalesPersonId() => _inner.GetSalesPersonId();
    public int? GetCustomerId() => _inner.GetCustomerId();
    public void SetActiveBranchId(int branchId) => _inner.SetActiveBranchId(branchId);
}
