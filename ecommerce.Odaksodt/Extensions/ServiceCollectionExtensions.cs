using ecommerce.Odaksodt.Abstract;
using ecommerce.Odaksodt.Concreate;
using ecommerce.Odaksodt.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace ecommerce.Odaksodt.Extensions;

/// <summary>
/// Odaksoft servislerini DI container'a ekleyen extension metodları
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Odaksoft E-Fatura servislerini ekler
    /// </summary>
    public static IServiceCollection AddOdaksoftServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Options pattern ile konfigürasyon
        services.Configure<OdaksoftOptions>(configuration.GetSection(OdaksoftOptions.SectionName));

        // Auth service - kendi HttpClient'ı ile (döngüsel bağımlılığı önlemek için)
        services.AddHttpClient<IOdaksoftAuthService, OdaksoftAuthService>()
            .AddPolicyHandler(GetRetryPolicy());

        // HttpClient ile Polly retry policy
        services.AddHttpClient<IOdaksoftHttpClient, OdaksoftHttpClient>()
            .AddPolicyHandler(GetRetryPolicy())
            .AddPolicyHandler(GetCircuitBreakerPolicy());

        // Business services
        services.AddScoped<IOdaksoftInvoiceService, OdaksoftInvoiceService>();

        return services;
    }

    /// <summary>
    /// Retry policy - sadece geçici ağ hatalarında yeniden dener (500 hariç — iş mantığı hatası olabilir)
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.RequestTimeout)
            .WaitAndRetryAsync(
                retryCount: 2,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    // NOT: Response body burada okunmamalı - stream tükenir ve asıl handler'da okunamaz
                    Console.WriteLine($"Retry {retryCount} after {timespan.TotalSeconds}s - Status: {outcome.Result?.StatusCode}");
                });
    }

    /// <summary>
    /// Circuit breaker policy - ardışık hatalardan sonra devre kesici devreye girer
    /// </summary>
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 10,
                durationOfBreak: TimeSpan.FromSeconds(15));
    }
}
