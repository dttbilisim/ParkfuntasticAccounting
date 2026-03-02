using Hangfire.Common;

namespace ecommerce.Core.BackgroundJobs;

public class HangfireJobFilterAttributeFilterProvider : JobFilterAttributeFilterProvider
{
    protected override IEnumerable<JobFilterAttribute> GetTypeAttributes(Job job)
    {
        job = UnwrapJob(job);

        return base.GetTypeAttributes(job);
    }

    protected override IEnumerable<JobFilterAttribute> GetMethodAttributes(Job job)
    {
        job = UnwrapJob(job);

        return base.GetMethodAttributes(job);
    }

    private Job UnwrapJob(Job job)
    {
        if (!job.Type.IsGenericType)
        {
            return job;
        }

        var genericType = job.Type.GetGenericTypeDefinition();

        if (genericType != typeof(HangfireJobExecutionAdapter<>))
        {
            return job;
        }

        var jobType = job.Type.GetGenericArguments()[0];
        var jobArgsType = BackgroundJobArgsHelper.GetJobArgsType(jobType);

        var jobExecuteMethod = jobType.GetMethod(nameof(IBackgroundJob.Execute)) ??
                               jobType.GetMethod(nameof(IAsyncBackgroundJob.ExecuteAsync));

        job = jobArgsType != null
            ? new Job(jobType, jobExecuteMethod, job.Args)
            : new Job(jobType, jobExecuteMethod);

        return job;
    }
}