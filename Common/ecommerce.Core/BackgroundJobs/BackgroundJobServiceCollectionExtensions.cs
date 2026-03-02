using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ecommerce.Core.BackgroundJobs;

public static class BackgroundJobServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, Action<BackgroundJobOptions>? configure = null)
    {
        services.AddOptions<BackgroundJobOptions>();

        if (configure != null)
        {
            services.Configure(configure);
        }

        services.AddSingleton<IHangfireJobManager, HangfireJobManager>();
        services.AddSingleton<IHangfireRecurringJobManager, HangfireRecurringJobManager>();

        return services;
    }

    public static IServiceCollection AddNullBackgroundJobs(this IServiceCollection services)
    {
        services.AddSingleton<IHangfireJobManager, NullHangfireJobManager>();
        services.AddSingleton<IHangfireRecurringJobManager, NullHangfireRecurringJobManager>();

        return services;
    }

    public static IHost UseBackgroundJobs(this IHost app)
    {
        // initializing hangfire server
        try 
        {
            var storage = app.Services.GetService<JobStorage>();
        }
        catch 
        {
            // Ignore if not registered
        }

        return app;
    }
}