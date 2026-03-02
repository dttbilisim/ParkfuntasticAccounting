using System.Runtime.Serialization;

namespace ecommerce.Core.BackgroundJobs;

[Serializable]
public class BackgroundJobExecutionException : ArgumentException
{
    public string JobType { get; set; } = null!;

    public object? JobArgs { get; set; }

    public BackgroundJobExecutionException()
    {
    }

    /// <summary>
    /// Creates a new <see cref="BackgroundJobExecutionException"/> object.
    /// </summary>
    public BackgroundJobExecutionException(SerializationInfo serializationInfo, StreamingContext context)
        : base(serializationInfo, context)
    {
    }

    /// <summary>
    /// Creates a new <see cref="BackgroundJobExecutionException"/> object.
    /// </summary>
    /// <param name="message">Exception message</param>
    /// <param name="innerException">Inner exception</param>
    public BackgroundJobExecutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}