using ecommerce.Domain.Shared.Abstract;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;

namespace ecommerce.Domain.Shared.Concreate;
public class ElasticSearchService : IElasticSearchService
{
    public IElasticClient _client { get; }

    public ElasticSearchService(IElasticClient client)
    {
        _client = client;
    }
   
    public async Task CreateIndexIfNotExists(string indexName)
    {
        var exists = await _client.Indices.ExistsAsync(indexName);
        if (!exists.Exists)
            await _client.Indices.CreateAsync(indexName);
    }

    public async Task DropIndexIfNotExists(string indexName)
    {
        var exists = await _client.Indices.ExistsAsync(indexName);
        if (exists.Exists)
            await _client.Indices.DeleteAsync(indexName);
    }

    public async Task<bool> AddOrUpdateBulk<T>(string indexName, IEnumerable<T> documents) where T : class
    {
        var response = await _client.BulkAsync(b => b
            .Index(indexName)
            .IndexMany(documents)
        );
        return !response.Errors;
    }

    public async Task<bool> AddOrUpdate<T>(string indexName, T document) where T : class
    {
        var response = await _client.IndexAsync(document, i => i.Index(indexName));
        return response.IsValid;
    }

    public async Task<T> Get<T>(string indexName, string key) where T : class
    {
        var response = await _client.GetAsync<T>(key, g => g.Index(indexName));
        return response.Source;
    }

    public async Task<List<T>?> GetAll<T>(string indexName) where T : class
    {
        var response = await _client.SearchAsync<T>(s => s
            .Index(indexName)
            .MatchAll()
            .Size(10000) // isteğe göre artır
        );
        return response.Documents.ToList();
    }

    public async Task<List<T>?> Query<T>(string indexName, QueryContainer predicate) where T : class
    {
        var response = await _client.SearchAsync<T>(s => s
            .Index(indexName)
            .Query(q => predicate)
        );
        return response.Documents.ToList();
    }

    public async Task<List<T>?> Query<T>(string indexName, Func<QueryContainerDescriptor<T>, QueryContainer> predicate) where T : class
    {
        var response = await _client.SearchAsync<T>(s => s
            .Index(indexName)
            .Query(predicate)
        );
        return response.Documents.ToList();
    }

    public async Task<List<T>> QueryAll<T>(string indexName, string predicate) where T : class
    {
        var response = await _client.SearchAsync<T>(s => s
            .Index(indexName)
            .Query(q => q.QueryString(qs => qs.Query(predicate)))
        );
        return response.Documents.ToList();
    }

    public async Task<bool> Remove<T>(string key, string indexName) where T : class
    {
        var response = await _client.DeleteAsync<T>(key, d => d.Index(indexName));
        return response.IsValid;
    }

    public async Task<long> RemoveAll<T>(string indexName) where T : class
    {
        var response = await _client.DeleteByQueryAsync<T>(q => q
            .Index(indexName)
            .Query(rq => rq.MatchAll())
        );
        return response.Deleted;
    }
}