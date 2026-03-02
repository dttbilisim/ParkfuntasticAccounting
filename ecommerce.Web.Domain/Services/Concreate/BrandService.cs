using ecommerce.Admin.Domain.Interfaces;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Dtos.Brand;
using Nest;
using IBrandService = ecommerce.Web.Domain.Services.Abstract.IBrandService;

namespace ecommerce.Web.Domain.Services.Concreate;

public class BrandService : IBrandService
{
    private readonly IElasticSearchService _elasticSearchService;

    public BrandService(IElasticSearchService elasticSearchService)
    {
        _elasticSearchService = elasticSearchService;
    }

    public async Task<IActionResult<List<BrandElasticDto>>> GetAllAsync(int? minProductCount = null, bool inStockOnly = false)
    {
        var rs = OperationResult.CreateResult<List<BrandElasticDto>>();
        try
        {
            // Önce product index'inden hangi brand_id'lerin ürünü olduğunu bul (ve isteğe göre en az N ürün; isteğe göre sadece stokta olanlar)
            var mustQueries = new List<QueryContainer>
            {
                new TermQuery { Field = Infer.Field<ecommerce.Domain.Shared.Dtos.Product.SellerProductElasticDto>(f => f.ProductStatus), Value = 1 },
                new ExistsQuery { Field = Infer.Field<ecommerce.Domain.Shared.Dtos.Product.SellerProductElasticDto>(f => f.BrandId) }
            };
            if (inStockOnly)
                mustQueries.Add(new NumericRangeQuery { Field = Infer.Field<ecommerce.Domain.Shared.Dtos.Product.SellerProductElasticDto>(f => f.Stock), GreaterThan = 0 });

            var productAggregation = await _elasticSearchService._client.SearchAsync<ecommerce.Domain.Shared.Dtos.Product.SellerProductElasticDto>(s => s
                .Index(ElasticSearchIndexConstants.SellerProducts)
                .Size(0) // Döküman döndürme, sadece aggregation
                .Query(_ => new QueryContainer(new BoolQuery { Must = mustQueries }))
                .Aggregations(a => a
                    .Terms("brands_with_products", t => t
                        .Field(f => f.BrandId)
                        .Size(10000) // Tüm brand'leri al
                    )
                )
            );

            if (!productAggregation.IsValid)
            {
                rs.AddSystemError(productAggregation.OriginalException?.Message ?? productAggregation.ServerError?.Error?.Reason ?? "Product aggregation failed");
                return rs;
            }

            // Ürünü olan brand_id'leri al; minProductCount varsa en az o kadar ürünü olanları filtrele
            var buckets = productAggregation.Aggregations.Terms("brands_with_products").Buckets;
            var brandsWithProducts = buckets
                .Where(b => !minProductCount.HasValue || b.DocCount >= minProductCount.Value)
                .Select(b => Convert.ToInt32(b.Key))
                .ToList();

            if (!brandsWithProducts.Any())
            {
                rs.Result = new List<BrandElasticDto>();
                return rs;
            }

            // Şimdi sadece ürünü olan brand'leri getir
            var result = await _elasticSearchService._client.SearchAsync<BrandElasticDto>(x => x
                .Index(ElasticSearchIndexConstants.Brands)
                .Query(q => q
                    .Bool(b => b
                        .Must(
                            m => m.Term(t => t.Status, 1), // Aktif markalar
                            m => m.Terms(t => t.Field(f => f.Id).Terms(brandsWithProducts)) // Ürünü olan markalar
                        )
                    )
                )
                .Size(1000)
            );
            
            if (result.IsValid)
            {
                rs.Result = result.Documents.ToList();
                Console.WriteLine($"🔍 Found {rs.Result.Count} brands with products (out of {brandsWithProducts.Count} brand IDs)");
            }
            else
            {
                rs.AddSystemError(result.OriginalException?.Message ?? result.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }

        return rs;
    }

    public async Task<IActionResult<List<BrandElasticDto>>> SearchAsync(string keyword)
    {
        var rs = OperationResult.CreateResult<List<BrandElasticDto>>();
        try
        {
            var response = await _elasticSearchService._client.SearchAsync<BrandElasticDto>(s => s
                .Index(ElasticSearchIndexConstants.Brands)
                .Query(q => q
                    .Bool(b => b
                        .Must(m => m.Term(t => t.Status, 1))
                        .Should(
                            sh => sh.Match(mt => mt.Field(f => f.Name).Query(keyword).Fuzziness(Fuzziness.Auto))
                        )
                        .MinimumShouldMatch(1)
                    )
                )
                .Size(1000)
            );

            if (response.IsValid)
            {
                rs.Result = response.Documents.ToList();
            }
            else
            {
                rs.AddSystemError(response.OriginalException?.Message ?? response.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        }
        catch (Exception e)
        {
            rs.AddSystemError(e.Message);
        }

        return rs;
    }
}