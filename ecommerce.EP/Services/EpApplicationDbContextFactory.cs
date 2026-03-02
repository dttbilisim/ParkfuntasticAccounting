using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ecommerce.EFCore.Context;
using ecommerce.Core.Interfaces;

namespace ecommerce.EP.Services;

/// <summary>
/// ApplicationDbContext ITenantProvider (scoped) istediği için her CreateDbContext'te yeni scope açıp
/// scope'u context ile birlikte dispose eden bir wrapper döndürür (CourierService "command already in progress" önlemi).
/// </summary>
public sealed class EpApplicationDbContextFactory : IDbContextFactory<ApplicationDbContext>
{
    private readonly IServiceProvider _rootProvider;

    public EpApplicationDbContextFactory(IServiceProvider rootProvider)
    {
        _rootProvider = rootProvider;
    }

    public ApplicationDbContext CreateDbContext()
    {
        var scope = _rootProvider.CreateScope();
        var tenantProvider = scope.ServiceProvider.GetRequiredService<ITenantProvider>();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
        return new EpScopedApplicationDbContext(options, tenantProvider, scope);
    }
}

/// <summary>
/// Dispose edildiğinde scope'u da dispose eder; böylece factory'den dönen context kullanıldıktan sonra scope leak olmaz.
/// </summary>
public sealed class EpScopedApplicationDbContext : ApplicationDbContext
{
    private readonly IServiceScope _scope;

    public EpScopedApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantProvider tenantProvider,
        IServiceScope scope)
        : base(options, tenantProvider)
    {
        _scope = scope;
    }

    public override void Dispose()
    {
        try
        {
            base.Dispose();
        }
        finally
        {
            _scope?.Dispose();
        }
    }
}
