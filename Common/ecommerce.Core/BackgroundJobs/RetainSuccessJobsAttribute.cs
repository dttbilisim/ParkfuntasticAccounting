using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;

namespace ecommerce.Core.BackgroundJobs;

public class RetainSuccessJobsAttribute : JobFilterAttribute, IApplyStateFilter
{
    private readonly int _days;

    public RetainSuccessJobsAttribute(int days = 100)
    {
        _days = days;
    }

    public void OnStateApplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
        if (context.NewState is SucceededState)
        {
            context.JobExpirationTimeout = TimeSpan.FromDays(_days);
        }
    }

    public void OnStateUnapplied(ApplyStateContext context, IWriteOnlyTransaction transaction)
    {
    }
}
