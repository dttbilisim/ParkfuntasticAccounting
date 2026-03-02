using ecommerce.Domain.Shared.Dtos.Search;

namespace ecommerce.Admin.Services.Interfaces
{
    public interface IRecentSearchService
    {
        Task AddSearchTermAsync(string term, int recordCount = 0);
        Task<List<RecentSearchDto>> GetRecentSearchesAsync(int limit = 10);
        Task ClearRecentSearchesAsync();
        Task RemoveSearchTermAsync(string term);
    }
}
