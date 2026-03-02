using Dot.Integration.Abstract;
using Dot.Integration.Concreate;
using Dot.Integration.Options;
using Dot.Integration.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Dot.Integration.Extensions;

public static class DatServiceExtensions
{
    public static IServiceCollection AddDatIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        // Options configuration
        services.Configure<DatServiceOptions>(configuration.GetSection("DatService"));
        
        // HttpClient configuration
        services.AddHttpClient<IDatService, DatService>(client =>
        {
            var options = configuration.GetSection("DatService").Get<DatServiceOptions>();
            client.Timeout = TimeSpan.FromSeconds(options?.TimeoutSeconds ?? 30);
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        });
        
        // Services registration
        services.AddSingleton<DatTokenCache>();
        services.AddScoped<IDatService, DatService>();
        services.AddScoped<DatDataService>(sp => 
        {
            var datService = sp.GetRequiredService<IDatService>();
            var dbContext = sp.GetRequiredService<DbContext>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatDataService>>();
            return new DatDataService(datService, dbContext, logger);
        });
        services.AddScoped<DatVehicleSyncService>();
        
        return services;
    }
}
