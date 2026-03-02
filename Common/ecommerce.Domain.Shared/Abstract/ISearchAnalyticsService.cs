using System.Threading.Tasks;
using ecommerce.Domain.Shared.Dtos.Search;

namespace ecommerce.Domain.Shared.Abstract
{
    public interface ISearchAnalyticsService
    {
        Task LogInteractionAsync(SearchInteractionDto interaction);
    }
}
