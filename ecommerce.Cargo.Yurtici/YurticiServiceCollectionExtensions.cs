using ecommerce.Cargo.Yurtici.Jobs;
using ecommerce.Core.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ecommerce.Cargo.Yurtici;

public static class YurticiServiceCollectionExtensions
{
    public static IServiceCollection AddYurticiCargo(this IServiceCollection services, Action<YurticiOptions>? configure = null)
    {
        services.AddOptions<YurticiOptions>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddScoped<YurticiClient>();

        return services;
    }

    public static IHost UseYurticiCargo(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IHangfireRecurringJobManager>();

        recurringJobManager.RecurAsync<YurticiReadyCheckJob>("0 * * * *").Wait();
        recurringJobManager.RecurAsync<YurticiDeliveredCheckJob>("0 0/3 * * *").Wait();
        recurringJobManager.RecurAsync<YurticiReturnReadyCheckJob>("0 * * * *").Wait();
        recurringJobManager.RecurAsync<YurticiReturnDeliveredCheckJob>("0 0/3 * * *").Wait();
        return app;
    }
}