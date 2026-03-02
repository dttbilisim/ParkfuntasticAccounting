namespace ecommerce.Core.BackgroundJobs;

/// <summary>
/// Defines interface of a background job with a parameter.
/// </summary>
public interface IBackgroundJob<in TArgs>
{
    /// <summary>
    /// Executes the job with the <paramref name="args"/>.
    /// </summary>
    /// <param name="args">Job arguments.</param>
    void Execute(TArgs args);
}

/// <summary>
/// Defines interface of a background job.
/// </summary>
public interface IBackgroundJob
{
    /// <summary>
    /// Executes the job.
    /// </summary>
    void Execute();
}