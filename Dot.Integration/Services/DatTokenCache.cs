using Dot.Integration.Dtos;
using Microsoft.Extensions.Logging;

namespace Dot.Integration.Services;

public class DatTokenCache
{
    private DatTokenReturn? _cachedToken;
    private DateTime _tokenExpiry;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly ILogger<DatTokenCache> _logger;

    public DatTokenCache(ILogger<DatTokenCache> logger)
    {
        _logger = logger;
    }

    public async Task<DatTokenReturn> GetValidTokenAsync(Func<Task<DatTokenReturn>> tokenProvider)
    {
        await _semaphore.WaitAsync();
        try
        {
            // Verbose log removed

            // Token hala geçerli mi kontrol et
            if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                // Verbose log removed
                return _cachedToken;
            }

            _logger.LogInformation("Fetching new token...");
            // Yeni token al
            var newToken = await tokenProvider();
            
            // Verbose log removed
            
            _cachedToken = newToken;
            
            // Token süresini parse et (ISO 8601 format)
            if (DateTime.TryParse(newToken.Expires, out var expiry))
            {
                _tokenExpiry = expiry.AddMinutes(-5); // 5 dakika önceden yenile
                // Verbose log removed
            }
            else
            {
                _tokenExpiry = DateTime.UtcNow.AddHours(1); // Varsayılan 1 saat
                _logger.LogWarning("Could not parse token expiry '{Expires}', using default 1 hour", newToken.Expires);
            }

            // Verbose log removed
            return _cachedToken;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void ClearCache()
    {
        _cachedToken = null;
        _tokenExpiry = DateTime.MinValue;
    }
}
