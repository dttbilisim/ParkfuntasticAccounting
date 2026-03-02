using System.Linq.Expressions;
using Hangfire.Common;
using Hangfire.Dashboard;

namespace ecommerce.Core.BackgroundJobs;

public static class BackgroundJobArgsHelper
{
    public static Type? GetJobArgsType(Type jobType)
    {
        foreach (var @interface in jobType.GetInterfaces())
        {
            if (!@interface.IsGenericType)
            {
                continue;
            }

            if (@interface.GetGenericTypeDefinition() != typeof(IBackgroundJob<>) &&
                @interface.GetGenericTypeDefinition() != typeof(IAsyncBackgroundJob<>))
            {
                continue;
            }

            var genericArgs = @interface.GetGenericArguments();
            if (genericArgs.Length != 1)
            {
                continue;
            }

            return genericArgs[0];
        }

        return null;
    }

    public static void EnsureJobArgsType(Type jobType, object? args, out Type? jobArgsType)
    {
        jobArgsType = GetJobArgsType(jobType);

        if (jobArgsType == null)
        {
            if (args != null)
            {
                throw new ArgumentException(
                    $"Job args should be null since the job type ({jobType.AssemblyQualifiedName}) does not require any argument!"
                );
            }

            return;
        }

        if (args == null)
        {
            throw new ArgumentException(
                $"Job args should not be null since the job type ({jobType.AssemblyQualifiedName}) requires an argument of type {jobArgsType.AssemblyQualifiedName}!"
            );
        }

        if (!jobArgsType.IsInstanceOfType(args))
        {
            throw new ArgumentException(
                $"Job args is not assignable to the required type ({jobType.AssemblyQualifiedName})! " +
                $"Required type: {jobArgsType.AssemblyQualifiedName}, " +
                $"given type: {args.GetType().AssemblyQualifiedName}"
            );
        }
    }

    public static Expression<Action<HangfireJobExecutionAdapter<TJob>>> CreateJobMethodExpression<TJob>(object? jobArgs, Type? jobArgsType)
    {
        var parameterExpression = Expression.Parameter(typeof(HangfireJobExecutionAdapter<TJob>), "job");
        var methodCall = Expression.Lambda<Action<HangfireJobExecutionAdapter<TJob>>>(
            Expression.Call(
                parameterExpression,
                typeof(HangfireJobExecutionAdapter<TJob>).GetMethod(nameof(HangfireJobExecutionAdapter<TJob>.Execute))!.MakeGenericMethod(jobArgsType ?? typeof(object)),
                Expression.Constant(jobArgs, jobArgsType ?? typeof(object))
            ),
            parameterExpression
        );

        return methodCall;
    }

    public static Func<DashboardContext, Job, string> GetDashboardDisplayNameProvider()
    {
        return (_, job) =>
        {
            var jobType = job.Type;

            if (jobType.IsGenericType && jobType.GetGenericTypeDefinition() == typeof(HangfireJobExecutionAdapter<>))
            {
                jobType = jobType.GetGenericArguments()[0];
            }

            return jobType.Name;
        };
    }
}