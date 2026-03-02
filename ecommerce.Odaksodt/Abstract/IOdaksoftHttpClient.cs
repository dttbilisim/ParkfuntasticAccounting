namespace ecommerce.Odaksodt.Abstract;

/// <summary>
/// Odaksoft HTTP client interface
/// </summary>
public interface IOdaksoftHttpClient
{
    /// <summary>
    /// GET request gönderir
    /// </summary>
    Task<TResponse?> GetAsync<TResponse>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// POST request gönderir
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// POST request gönderir (token ile)
    /// </summary>
    Task<TResponse?> PostWithAuthAsync<TRequest, TResponse>(string endpoint, TRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Binary data indirir
    /// </summary>
    Task<byte[]> DownloadBinaryAsync(string endpoint, CancellationToken cancellationToken = default);
}
