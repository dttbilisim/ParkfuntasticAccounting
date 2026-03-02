using System.Net.Sockets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
namespace ecommerce.Admin.Services;
public class DatabaseHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        // Simulate checking database connectivity.
        var siteCheck = WebSiteHttpCheck();
        var dbCheck = DbCheck();

        return siteCheck ? Task.FromResult(HealthCheckResult.Healthy("web site is running")) : Task.FromResult(dbCheck ? HealthCheckResult.Healthy("db is running") : HealthCheckResult.Unhealthy("web site is down"));
    }
    
    public static bool WebSiteHttpCheck() {
        try {
            var client = new TcpClient("eczapro.net", 80);
            client.Close();
            return true;
        } catch (Exception) {
            return false;
        }
    }

    public static bool DbCheck() {
        using (var client = new TcpClient()) {
            try {
                client.Connect("195.142.137.22", 5432);
            } catch (SocketException) {
                return false;
            }
            client.Close();
            return true;
        }
    }
}
