using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ecommerce.EFCore.Context;

/// <summary>
/// Migration / design-time context factory (dotnet ef ...).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // dotnet ef: startup project (ecommerce.Admin) veya solution root'tan çalışır
        var currentDir = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(currentDir, "ecommerce.Admin"),
            Path.Combine(currentDir, "..", "ecommerce.Admin"),
            Path.Combine(currentDir, "ecommerce.EP"),
            Path.Combine(currentDir, "..", "ecommerce.EP"),
        };
        var basePath = candidates.FirstOrDefault(Directory.Exists) ?? currentDir;
        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();
        var connectionString = config.GetConnectionString("ApplicationDbContext")
            ?? "Host=localhost;Database=MarketPlace;Username=postgres;Password=postgres";
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, o => o.MigrationsAssembly("ecommerce.EFCore"));
        return new ApplicationDbContext(optionsBuilder.Options, new DesignTimeTenantProvider());
    }

    private sealed class DesignTimeTenantProvider : Core.Interfaces.ITenantProvider
    {
        public bool IsGlobalAdmin => false;
        public bool IsB2BAdmin => false;
        public bool IsPlasiyer => false;
        public bool IsCustomerB2B => false;
        public bool IsMultiTenantEnabled => false;
        public int? GetSalesPersonId() => null;
        public int? GetCustomerId() => null;
        public int GetCurrentBranchId() => 1;
        public int GetCurrentCorporationId() => 1;
        public void SetActiveBranchId(int branchId) { }
    }
}
