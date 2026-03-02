using Nest;
namespace ecommerce.Domain.Shared.Abstract;
public interface IElasticSearchService{
    IElasticClient _client { get; }

    Task CreateIndexIfNotExists(string indexName);
    Task DropIndexIfNotExists(string indexName);
    Task<bool> AddOrUpdateBulk<T>(string indexName, IEnumerable<T> documents) where T : class;
    Task<bool> AddOrUpdate<T>(string indexName, T document) where T : class;
    Task<T> Get<T>(string indexName, string key) where T : class;
    Task<List<T>?> GetAll<T>(string indexName) where T : class;
    Task<List<T>?> Query<T>(string indexName, QueryContainer predicate) where T : class;
    Task<List<T>?> Query<T>(string indexName, Func<QueryContainerDescriptor<T>, QueryContainer> predicate) where T : class;
    Task<List<T>> QueryAll<T>(string indexName, string predicate) where T : class;
    Task<bool> Remove<T>(string key, string indexName) where T : class;
    Task<long> RemoveAll<T>(string indexName) where T : class;
}
