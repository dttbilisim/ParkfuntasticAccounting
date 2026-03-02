using ecommerce.Domain.Shared.ElasticSearch.Abstract;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
namespace ecommerce.Domain.Shared.ElasticSearch.Concreate;
 public class ElasticSearchManager : IElasticSearchService{
        public IElasticClient _client{get;set;}
        private readonly IElasticSearchConfigration _elasticSearchConfigration;
        public ElasticSearchManager(IElasticSearchConfigration elasticSearchConfigration){
            _elasticSearchConfigration = elasticSearchConfigration;
            _client = GetClient();
        }
        private ElasticClient GetClient(){
            var pool = new SingleNodeConnectionPool(new Uri(_elasticSearchConfigration.ConnectionString));
            var connectionSettings = new ConnectionSettings(pool, sourceSerializer:JsonNetSerializer.Default);
            return new ElasticClient(connectionSettings.DisablePing().SniffOnStartup(false).SniffOnConnectionFault(false).DefaultFieldNameInferrer(p => p).BasicAuthentication(_elasticSearchConfigration.AuthUserName, _elasticSearchConfigration.AuthPassWord));
        }
        public async Task CreateIndexIfNotExists(string indexName){
            if(!(await _client.Indices.ExistsAsync(indexName)).Exists){
                await _client.Indices.CreateAsync(indexName, c => c.Map<dynamic>(m => m.AutoMap()));
            }
        }
        public async Task DropIndexIfNotExists(string indexName){
            if((await _client.Indices.ExistsAsync(indexName)).Exists){
                await _client.Indices.DeleteAsync(indexName);
            }
        }
        public async Task<bool> AddOrUpdateBulk<T>(string indexName, IEnumerable<T> documents) where T : class{
            var indexResponse = await _client.BulkAsync(b => b.Index(indexName).UpdateMany(documents, (ud, d) => ud.Doc(d).DocAsUpsert()));
            return indexResponse.IsValid;
        }
        public async Task<bool> AddOrUpdate<T>(string indexName, T document) where T : class{
            var indexResponse = await _client.IndexAsync(document, idx => idx.Index(indexName).OpType(OpType.Index));
            return indexResponse.IsValid;
        }
        public async Task<T> Get<T>(string indexName, string key) where T : class{
            var response = await _client.GetAsync<T>(key, g => g.Index(indexName));
            return response.Source;
        }
        public async Task<List<T> ?> GetAll<T>(string indexName) where T : class{
            var searchResponse = await _client.SearchAsync<T>(s => s.Index(indexName).From(0).Size(2000).Query(q => q.MatchAll()));
            return searchResponse.IsValid ? searchResponse.Documents.ToList() : new List<T>();
        }
        public async Task<List<T> ?> Query<T>(string indexName, QueryContainer predicate) where T : class{
            var searchResponse = await _client.SearchAsync<T>(s => s.Index(indexName).From(0).Size(2000).Query(q => predicate));
            return searchResponse.IsValid ? searchResponse.Documents.ToList() : new List<T>();
        }
        public async Task<List<T> ?> Query<T>(string indexName, Func<QueryContainerDescriptor<T>, QueryContainer> predicate) where T : class{
            var searchResponse = await _client.SearchAsync<T>(s => s.Index(indexName).Query(predicate));
            return searchResponse.IsValid ? searchResponse.Documents.ToList() : new List<T>();
        }
        public async Task<List<T>> QueryAll<T>(string indexName, string predicate) where T : class{
            var searchResponse = await _client.SearchAsync<T>(s => s.Index(indexName).Query(q => q.MatchPhrasePrefix(x => x.Field("Product.Name").Query(predicate)) || q.MatchPhrasePrefix(x => x.Field("Brand.Name").Query(predicate)) || q.Match(x => x.Field("Product.Barcode").Query(predicate))));
            return searchResponse.IsValid ? searchResponse.Documents.ToList() : new List<T>();
        }
        public async Task<bool> Remove<T>(string key, string indexName) where T : class{
            var response = await _client.DeleteAsync<T>(key, d => d.Index(indexName));
            return response.IsValid;
        }
        public async Task<long> RemoveAll<T>(string indexName) where T : class{
            var response = await _client.DeleteByQueryAsync<T>(d => d.Index(indexName).Query(q => q.MatchAll()));
            return response.Deleted;
        }
    }
