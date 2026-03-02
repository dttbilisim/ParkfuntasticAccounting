using System.Text.Json;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Domain.Shared.Dtos.Search;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Core.Identity;

namespace ecommerce.Admin.Services.Concreate
{
    public class RecentSearchService : IRecentSearchService
    {
        private readonly IRedisCacheService _redis;
        private readonly CurrentUser _currentUser;

        public RecentSearchService(IRedisCacheService redis, CurrentUser currentUser)
        {
            _redis = redis;
            _currentUser = currentUser;
        }

        private string? GetCurrentUserIdKey()
        {
            if (_currentUser?.Id > 0)
            {
                return $"recent_searches:user:{_currentUser.Id}";
            }
            return null;
        }

        public async Task AddSearchTermAsync(string term, int recordCount = 0)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            
            var trimmed = term.Trim();
            
            // 3 karakterden kısa aramaları kaydetme
            if (trimmed.Length < 3) return;
            
            var key = GetCurrentUserIdKey();
            if (key == null) return;

            // Mevcut son aramaları al — aynı terimi veya bu terimin prefix'lerini temizle
            var existing = await GetRecentSearchesAsync(50);
            foreach (var old in existing)
            {
                // Aynı terim zaten varsa veya bu terimin prefix'i ise kaldır
                // Örn: "bo", "bos", "bosc" → "bosch" yazıldığında hepsi silinir
                if (string.Equals(old.Term, trimmed, StringComparison.OrdinalIgnoreCase) ||
                    trimmed.StartsWith(old.Term, StringComparison.OrdinalIgnoreCase))
                {
                    var oldJson = JsonSerializer.Serialize(old);
                    await _redis.RemoveFromSortedSetAsync(key, oldJson);
                }
            }

            var searchDto = new RecentSearchDto
            {
                Term = trimmed,
                SearchDate = DateTime.UtcNow,
                RecordCount = recordCount
            };

            var jsonValue = JsonSerializer.Serialize(searchDto);
            await _redis.AddToSortedSetAsync(key, jsonValue, DateTime.UtcNow.Ticks);
            await _redis.RemoveRangeFromSortedSetByRankAsync(key, 0, -51);
        }

        public async Task<List<RecentSearchDto>> GetRecentSearchesAsync(int limit = 10)
        {
            var key = GetCurrentUserIdKey();
            if (key == null) return new List<RecentSearchDto>();

            var jsonValues = await _redis.GetRevRangeFromSortedSetAsync(key, 0, limit - 1);
            
            var results = new List<RecentSearchDto>();
            foreach (var jsonValue in jsonValues)
            {
                try
                {
                    var dto = JsonSerializer.Deserialize<RecentSearchDto>(jsonValue);
                    if (dto != null) results.Add(dto);
                }
                catch
                {
                    results.Add(new RecentSearchDto
                    {
                        Term = jsonValue,
                        SearchDate = DateTime.UtcNow,
                        RecordCount = 0
                    });
                }
            }
            
            return results;
        }

        public async Task ClearRecentSearchesAsync()
        {
            var key = GetCurrentUserIdKey();
            if (key == null) return;
            await _redis.RemoveAsync(key);
        }

        public async Task RemoveSearchTermAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            
            var key = GetCurrentUserIdKey();
            if (key == null) return;

            var allSearches = await GetRecentSearchesAsync(50);
            var toRemove = allSearches.FirstOrDefault(s => s.Term.Equals(term.Trim(), StringComparison.OrdinalIgnoreCase));
            
            if (toRemove != null)
            {
                var jsonValue = JsonSerializer.Serialize(toRemove);
                await _redis.RemoveFromSortedSetAsync(key, jsonValue);
            }
        }
    }
}
