using ecommerce.Cargo.Mng.Jobs;
using ecommerce.Core.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ecommerce.Cargo.Mng;

public static class MngServiceCollectionExtensions
{
    public static IServiceCollection AddMngCargo(this IServiceCollection services, Action<MngOptions>? configure = null)
    {
        services.AddOptions<MngOptions>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddScoped<MngClient>();

        return services;
    }

    public static IHost UseMngCargo(this IHost app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var recurringJobManager = scope.ServiceProvider.GetRequiredService<IHangfireRecurringJobManager>();

            recurringJobManager.RecurAsync<MngReadyCheckJob>("0 * * * *").Wait();
            recurringJobManager.RecurAsync<MngDeliveredCheckJob>("0 0/3 * * *").Wait();
            recurringJobManager.RecurAsync<MngReturnReadyCheckJob>("0 * * * *").Wait();
            recurringJobManager.RecurAsync<MngReturnDeliveredCheckJob>("0 0/3 * * *").Wait();
        }

        return app;
    }
}