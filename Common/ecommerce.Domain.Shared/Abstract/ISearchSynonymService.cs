using ecommerce.Core.Entities;
using ecommerce.Domain.Shared.Models;

namespace ecommerce.Domain.Shared.Abstract
{
    public interface ISearchSynonymService
    {
        Task<List<SearchSynonym>> GetAllSynonymsAsync();
        Task<Dictionary<string, List<string>>> GetSynonymDictionaryAsync();
        Task<SearchMetadataContainer> GetSearchMetadataAsync();
        Task SaveGeneralSettingsAsync(SearchGeneralSettings settings);
        Task ClearCacheAsync();
    }
}
