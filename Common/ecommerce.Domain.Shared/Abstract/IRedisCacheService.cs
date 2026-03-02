namespace ecommerce.Domain.Shared.Abstract;
public interface IRedisCacheService{
    Task SetAsync<T>(string key, T data, TimeSpan? expiry = null);
    Task<T?> GetAsync<T>(string key);
    Task<bool> RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    
    // Sorted Set Methods for Active User Tracking
    Task AddToSortedSetAsync(string key, string member, double score);
    Task<List<string>> GetRangeFromSortedSetByScoreAsync(string key, double min, double max);
    Task RemoveRangeFromSortedSetByScoreAsync(string key, double min, double max);
    Task RemoveFromSortedSetAsync(string key, string member);
    
    // Recent Search Helpers
    Task<List<string>> GetRevRangeFromSortedSetAsync(string key, long start, long stop);
    Task RemoveRangeFromSortedSetByRankAsync(string key, long start, long stop);
    
    // List Methods — Konum geçmişi için
    Task ListRightPushAsync(string key, string value);
    Task<List<string>> ListRangeAsync(string key, long start = 0, long stop = -1);
    Task SetKeyExpiryAsync(string key, TimeSpan expiry);
}
