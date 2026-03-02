using ecommerce.Cargo.Sendeo.Jobs;
using ecommerce.Core.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ecommerce.Cargo.Sendeo;

public static class SendeoServiceCollectionExtensions
{
    public static IServiceCollection AddSendeoCargo(this IServiceCollection services, Action<SendeoOptions>? configure = null)
    {
        services.AddOptions<SendeoOptions>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddScoped<SendeoClient>();

        return services;
    }

    public static IHost UseSendeoCargo(this IHost app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IHangfireRecurringJobManager>();

            try
            {
                recurringJobManager.RecurAsync<SendeoReadyCheckJob>("0 * * * *").Wait();
                recurringJobManager.RecurAsync<SendeoDeliveredCheckJob>("0 0/3 * * *").Wait();
                recurringJobManager.RecurAsync<SendeoReturnReadyCheckJob>("0 * * * *").Wait();
                recurringJobManager.RecurAsync<SendeoReturnDeliveredCheckJob>("0 0/3 * * *").Wait();
            }
            catch (Exception ex)
            {
                // Convert to a safe logging or Console write if Logger is unavailable, 
                // but simpler to just swallow for startup resilience or use Console.
                Console.WriteLine($"WARNING: Failed to register Sendeo recurring jobs: {ex.Message}");
            }
        }

        return app;
    }
}