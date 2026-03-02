using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nito.AsyncEx;

namespace ecommerce.Core.BackgroundJobs;

public class HangfireJobExecutionAdapter<TJob>
{
    private BackgroundJobOptions Options { get; }
    private IServiceScopeFactory ServiceScopeFactory { get; }
    private readonly ILogger<HangfireJobExecutionAdapter<TJob>> _logger;

    public HangfireJobExecutionAdapter(
        IOptions<BackgroundJobOptions> options,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<HangfireJobExecutionAdapter<TJob>> logger)
    {
        Options = options.Value;
        ServiceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public void Execute<TArgs>(TArgs? args = default)
    {
        if (!Options.IsJobExecutionEnabled)
        {
            throw new ArgumentException(
                "Background job execution is disabled. " +
                "This method should not be called! " +
                "If you want to enable the background job execution, " +
                $"set {nameof(BackgroundJobOptions)}.{nameof(BackgroundJobOptions.IsJobExecutionEnabled)} to true! "
                + "If you've intentionally disabled job execution and this seems a bug, please report it."
            );
        }

        var jobType = typeof(TJob);
        object? jobArgs = args;

        BackgroundJobArgsHelper.EnsureJobArgsType(jobType, args, out var jobArgsType);

        object? job = null;

        using var scope = ServiceScopeFactory.CreateScope();

        job = ActivatorUtilities.GetServiceOrCreateInstance(scope.ServiceProvider, jobType);

        if (job == null)
        {
            throw new ArgumentException("The job type is not registered to DI: " + jobType);
        }

        var jobExecuteMethod = jobType.GetMethod(nameof(IBackgroundJob.Execute)) ??
                               jobType.GetMethod(nameof(IAsyncBackgroundJob.ExecuteAsync));

        if (jobExecuteMethod == null)
        {
            throw new ArgumentException(
                $"Given job type does not implement {typeof(IBackgroundJob<>).Name} or {typeof(IAsyncBackgroundJob<>).Name}. " +
                "The job type was: " + jobType
            );
        }

        try
        {
            
            if (jobExecuteMethod.Name is nameof(IAsyncBackgroundJob.ExecuteAsync))
            {
                AsyncContext.Run(() => (Task?) jobExecuteMethod.Invoke(job, jobArgsType != null ? new[] { jobArgs } : null) ?? Task.CompletedTask);
            }
            else
            {
                jobExecuteMethod.Invoke(job, jobArgsType != null ? new[] { jobArgs } : null);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"{jobType.Name} background job execution is failed.");

            throw new BackgroundJobExecutionException($"A background job execution is failed. {ex.Message}", ex)
            {
                JobType = jobType.AssemblyQualifiedName ?? jobType.Name,
                JobArgs = args
            };
        }
    }
}