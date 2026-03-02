using System.Text.Json;
using ecommerce.Domain.Shared.Abstract;
using StackExchange.Redis;
namespace ecommerce.Domain.Shared.Concreate;
public class RedisCacheService:IRedisCacheService{
    private readonly IDatabase _db;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _db = redis.GetDatabase();
        _logger = logger;
    }

    public async Task SetAsync<T>(string key, T data, TimeSpan? expiry = null)
    {
        try
        {
            var json = JsonSerializer.Serialize(data);
            await _db.StringSetAsync(key, json, expiry);
            
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Redis SetAsync failed for key: {key}");
            
        }
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        try
        {
            var value = await _db.StringGetAsync(key);
            if (value.IsNullOrEmpty) return default;
            return JsonSerializer.Deserialize<T>(value!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Redis GetAsync failed for key: {key}");
            return default;
        }
    }

    public async Task<bool> RemoveAsync(string key)
    {
        return await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task AddToSortedSetAsync(string key, string member, double score)
    {
        await _db.SortedSetAddAsync(key, member, score);
    }

    public async Task<List<string>> GetRangeFromSortedSetByScoreAsync(string key, double min, double max)
    {
        // Get values with scores between min and max
        var results = await _db.SortedSetRangeByScoreAsync(key, min, max);
        return results.Select(x => x.ToString()).ToList();
    }

    public async Task RemoveRangeFromSortedSetByScoreAsync(string key, double min, double max)
    {
        await _db.SortedSetRemoveRangeByScoreAsync(key, min, max);
    }
    
    public async Task RemoveFromSortedSetAsync(string key, string member)
    {
        await _db.SortedSetRemoveAsync(key, member);
    }

    public async Task<List<string>> GetRevRangeFromSortedSetAsync(string key, long start, long stop)
    {
        var results = await _db.SortedSetRangeByRankAsync(key, start, stop, Order.Descending);
        return results.Select(x => x.ToString()).ToList();
    }

    public async Task RemoveRangeFromSortedSetByRankAsync(string key, long start, long stop)
    {
        await _db.SortedSetRemoveRangeByRankAsync(key, start, stop);
    }

    // List metotları — konum geçmişi için
    public async Task ListRightPushAsync(string key, string value)
    {
        try
        {
            await _db.ListRightPushAsync(key, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Redis ListRightPushAsync failed for key: {key}");
        }
    }

    public async Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1)
    {
        try
        {
            var results = await _db.ListRangeAsync(key, start, stop);
            return results.Select(x => x.ToString()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Redis ListRangeAsync failed for key: {key}");
            return new List<string>();
        }
    }

    public async Task SetKeyExpiryAsync(string key, TimeSpan expiry)
    {
        try
        {
            await _db.KeyExpireAsync(key, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Redis SetKeyExpiryAsync failed for key: {key}");
        }
    }
}
