using System.Text.Json;
using ecommerce.Admin.Services.Interfaces;
using ecommerce.Domain.Shared.Dtos.Search;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Core.Identity;
using Microsoft.AspNetCore.Components.Authorization;

namespace ecommerce.Admin.Services.Concreate
{
    public class RecentSearchService : IRecentSearchService
    {
        private readonly IRedisCacheService _redis;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ecommerce.Admin.Services.AuthenticationService _authService;

        public RecentSearchService(IRedisCacheService redis, 
                                   AuthenticationStateProvider authStateProvider,
                                   ecommerce.Admin.Services.AuthenticationService authService)
        {
            _redis = redis;
            _authStateProvider = authStateProvider;
            _authService = authService;
        }

        private async Task<string?> GetCurrentUserIdKey()
        {
            try 
            {
               var user = _authService.User;
               if (user != null && user.Id > 0) return $"recent_searches:user:{user.Id}";
               
               // Fallback if generic user context isn't ready but AuthState is
               var state = await _authStateProvider.GetAuthenticationStateAsync();
               var u = state.User;
               if (u.Identity?.IsAuthenticated == true)
               {
                   var idClaim = u.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
                   if (idClaim != null) return $"recent_searches:user:{idClaim.Value}";
               }
            }
            catch {}
            return null;
        }

        public async Task AddSearchTermAsync(string term, int recordCount = 0)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            
            var key = await GetCurrentUserIdKey();
            if (key == null) return;

            var searchDto = new RecentSearchDto
            {
                Term = term.Trim(),
                SearchDate = DateTime.UtcNow,
                RecordCount = recordCount
            };

            // Serialize to JSON and store in sorted set
            var jsonValue = JsonSerializer.Serialize(searchDto);
            
            // Score = Current Ticks (Newest has highest score)
            // AddToSortedSet writes or updates (if exists, score is updated to now, bringing it to 'newest')
            await _redis.AddToSortedSetAsync(key, jsonValue, DateTime.UtcNow.Ticks);

            // Keep only top 50 (Highest Score = Newest)
            // Standard Redis Order: Lowest Score (Oldest) -> Highest Score (Newest)
            // We want to remove the Oldest.
            // Items are 0..N.
            // If we want to keep 50 items (N-50+1 to N), we remove 0 to N-50.
            // Using negative indices: -1 is Last (Newest). -50 is 50th from last.
            // So we want to remove 0 to -51.
            await _redis.RemoveRangeFromSortedSetByRankAsync(key, 0, -51);
        }

        public async Task<List<RecentSearchDto>> GetRecentSearchesAsync(int limit = 10)
        {
            var key = await GetCurrentUserIdKey();
            if (key == null) return new List<RecentSearchDto>();

            // Get top N descending (Newest first)
            var jsonValues = await _redis.GetRevRangeFromSortedSetAsync(key, 0, limit - 1);
            
            var results = new List<RecentSearchDto>();
            foreach (var jsonValue in jsonValues)
            {
                try
                {
                    // Try to deserialize as new format (with date and count)
                    var dto = JsonSerializer.Deserialize<RecentSearchDto>(jsonValue);
                    if (dto != null)
                    {
                        results.Add(dto);
                    }
                }
                catch
                {
                    // Fallback: If it's old format (just string), create DTO from it
                    results.Add(new RecentSearchDto
                    {
                        Term = jsonValue,
                        SearchDate = DateTime.UtcNow, // Unknown date for old entries
                        RecordCount = 0 // Unknown count for old entries
                    });
                }
            }
            
            return results;
        }

        public async Task ClearRecentSearchesAsync()
        {
            var key = await GetCurrentUserIdKey();
            if (key == null) return;

            await _redis.RemoveAsync(key);
        }

        public async Task RemoveSearchTermAsync(string term)
        {
            if (string.IsNullOrWhiteSpace(term)) return;
            
            var key = await GetCurrentUserIdKey();
            if (key == null) return;

            // Get all items to find the one matching the term
            var allSearches = await GetRecentSearchesAsync(50);
            var toRemove = allSearches.FirstOrDefault(s => s.Term.Equals(term.Trim(), StringComparison.OrdinalIgnoreCase));
            
            if (toRemove != null)
            {
                var jsonValue = JsonSerializer.Serialize(toRemove);
                await _redis.RemoveFromSortedSetAsync(key, jsonValue);
            }
            else
            {
                // Fallback: Try removing as plain string (for old format entries)
                await _redis.RemoveFromSortedSetAsync(key, term.Trim());
            }
        }
    }
}
