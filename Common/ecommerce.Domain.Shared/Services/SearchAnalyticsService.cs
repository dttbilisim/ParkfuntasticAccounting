using System.Text.Json;
using System.Threading.Tasks;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Dtos.Search;

namespace ecommerce.Domain.Shared.Services
{
    public class SearchAnalyticsService : ISearchAnalyticsService
    {
        private readonly IRedisCacheService _redis;
        private readonly ILogger<SearchAnalyticsService> _logger;
        private const string RedisKey = "search_analytics:v1";

        public SearchAnalyticsService(IRedisCacheService redis, ILogger<SearchAnalyticsService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task LogInteractionAsync(SearchInteractionDto interaction)
        {
            if (interaction == null) return;
            
            // Eğer SearchTerm boşsa, biriken arama geçmişinden en sonuncusunu almayı dene (opsiyonel ama güvenli)
            if (string.IsNullOrWhiteSpace(interaction.SearchTerm)) 
            {
                _logger.LogWarning("Search interaction skipped: SearchTerm is empty.");
                return;
            }

            // Terminal'de anında görmek için:
            Console.WriteLine($">>>> ANALYTICS LOG: Term: {interaction.SearchTerm}, Product: {interaction.ProductId}, Type: {interaction.InteractionType}");

            try 
            {
                var json = JsonSerializer.Serialize(interaction);
                await _redis.AddToSortedSetAsync(RedisKey, json, interaction.Timestamp.Ticks);
                
                Console.WriteLine(">>>> ANALYTICS SUCCESS: Redis'e yazıldı.");

                // Temizlik: 2 aydan eski verileri temizle
                var twoMonthsAgo = DateTime.UtcNow.AddMonths(-2).Ticks;
                await _redis.RemoveRangeFromSortedSetByScoreAsync(RedisKey, 0, twoMonthsAgo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>>> ANALYTICS ERROR: {ex.Message}");
                _logger.LogError(ex, "Failed to log search interaction to Redis.");
            }
        }
    }
}
