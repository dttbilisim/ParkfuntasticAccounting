using System.Text.Json;
using ecommerce.Core.Models;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Dtos.Filters;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Web.Domain.Services.Abstract;
using Nest;
namespace ecommerce.Web.Domain.Services.Concreate;
public class ProductService : IProductService{
    private readonly IElasticSearchService _elasticSearchService;
    public ProductService(IElasticSearchService elasticSearchService){_elasticSearchService = elasticSearchService;}
    public async Task<IActionResult<List<ProductElasticDto>>> GetAllAsync(){
        var rs = OperationResult.CreateResult<List<ProductElasticDto>>();
        try{
            var result = await _elasticSearchService._client.SearchAsync<ProductElasticDto>(x => x.Index(ElasticSearchIndexConstants.Products)
                .Query(q => q.Term(t => t.Status, 1)).Size(50));
            if(result.IsValid){
                rs.Result = result.Documents.ToList();
            } else{
                rs.AddError(result.OriginalException?.Message ?? result.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        } catch(Exception e){
            rs.AddError(e.Message);
        }
        return rs;
    }
    public async Task<IActionResult<ProductElasticDto>> GetByIdAsync(int id){
        var rs = OperationResult.CreateResult<ProductElasticDto>();
        try{
            var response = await _elasticSearchService._client.SearchAsync<ProductElasticDto>(s => s.Index(ElasticSearchIndexConstants.Products).Query(q => q.Bool(b => b.Must(m => m.Term(t => t.Status, 1), m => m.Term(t => t.Id, id)))));
            if(response.IsValid){
                rs.Result = response.Documents.FirstOrDefault();
            } else{
                rs.AddError(response.OriginalException?.Message ?? response.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        } catch(Exception e){
            rs.AddError(e.Message);
        }
        return rs;
    }
    public async Task<IActionResult<List<ProductElasticDto>>> GetByCategoryIdAsync(int categoryId){
        var rs = OperationResult.CreateResult<List<ProductElasticDto>>();
        try{
            var result = await _elasticSearchService._client.SearchAsync<ProductElasticDto>(s => s.Index(ElasticSearchIndexConstants.Products).Query(q => categoryId > 0 ? q.Nested(n => n.Path(p => p.Categories).Query(nq => nq.Term(t => t.Field("Categories.Id").Value(categoryId)))) : q.Nested(n => n.Path(p => p.Categories).Query(nq => nq.Term(t => t.Field("Categories.ParentId").Value(-categoryId))))).Size(1000));
            if(result.IsValid){
                rs.Result = result.Documents.ToList();
            } else{
                rs.AddError(result.OriginalException?.Message ?? result.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        } catch(Exception e){
            rs.AddError(e.Message);
        }
        return rs;
    }
    public async Task<IActionResult<Paging<List<ProductElasticDto>>>> GetByFilterPagingAsync(SearchFilterReguestDto filter){
        var rs = OperationResult.CreateResult<Paging<List<ProductElasticDto>>>();
        try{
            ISearchResponse<ProductElasticDto> result;
            var boolQuery = new BoolQuery{Must = new List<QueryContainer>(), Should = new List<QueryContainer>()};

            // Single CategoryId filter (positive: Categories.Id, negative: Categories.ParentId)
            if(filter.CategoryId.HasValue){
                var categoryField = filter.CategoryId > 0 ? "Categories.Id" : "Categories.ParentId";
                var categoryValue = filter.CategoryId > 0 ? filter.CategoryId.Value : -filter.CategoryId.Value;
                ((List<QueryContainer>) boolQuery.Must).Add(new NestedQuery{Path = "Categories", Query = new TermQuery{Field = categoryField, Value = categoryValue}});
            }

            // Single BrandId filter
            if(filter.BrandId.HasValue){
                ((List<QueryContainer>) boolQuery.Must).Add(new TermQuery{Field = "Brand.Id", Value = filter.BrandId.Value});
            }

            // Category filter (supports multiple with OR logic)
            if(filter.CategoryIds?.Any() == true){
                var categoryShould = new List<QueryContainer>();
                foreach(var catId in filter.CategoryIds){
                    categoryShould.Add(new NestedQuery {
                        Path = "Categories",
                        Query = new TermQuery {
                            Field = "Categories.Id",
                            Value = catId
                        }
                    });
                }

                ((List<QueryContainer>) boolQuery.Must).Add(new BoolQuery {
                    Should = categoryShould,
                    MinimumShouldMatch = 1
                });
            }

            // Brand filter (supports multiple with OR logic)
            if(filter.BrandIds?.Any() == true){
                var brandShould = new List<QueryContainer>();
                foreach(var brandId in filter.BrandIds){
                    brandShould.Add(new TermQuery {
                        Field = "BrandId",
                        Value = brandId
                    });
                }

                ((List<QueryContainer>) boolQuery.Must).Add(new BoolQuery {
                    Should = brandShould,
                    MinimumShouldMatch = 1
                });
            }

            // Status = 1 (Aktif)
            ((List<QueryContainer>)boolQuery.Must).Add(new TermQuery { Field = "Status", Value = 1 });

            // SellerItems filtreleri (SalePrice > 0 ve Stock > 0)
            ((List<QueryContainer>)boolQuery.Must).Add(new NestedQuery {
                Path = "SellerItems",
                Query = new BoolQuery {
                    Must = new List<QueryContainer> {
                        new LongRangeQuery {
                            Field = "SellerItems.SalePrice",
                            GreaterThan = 0
                        },
                        new LongRangeQuery {
                            Field = "SellerItems.Stock",
                            GreaterThan = 0
                        }
                    }
                }
            });

            // Name alanı boş olmayanlar
            ((List<QueryContainer>)boolQuery.Must).Add(new ExistsQuery {
                Field = "Name"
            });

            // Search query
            if(!string.IsNullOrWhiteSpace(filter.Search)){
                boolQuery.Should = new List<QueryContainer>{
                    new MatchQuery{Field = "Name", Query = filter.Search, Fuzziness = Fuzziness.Auto}, 
                    new MatchQuery{Field = "Description", Query = filter.Search, Fuzziness = Fuzziness.Auto}, 
                    new TermQuery{Field = "GroupCodes.OemCode.keyword", Value = filter.Search, Boost = 2.0}, // Exact match with higher boost
                    new TermQuery{Field = "Parts.Oem.keyword", Value = filter.Search, Boost = 2.0}, // Exact match for Parts OEM
                    new MatchQuery{Field = "Brand.Name", Query = filter.Search, Fuzziness = Fuzziness.Auto}
                };
                boolQuery.MinimumShouldMatch = 1;
            }

            // Sort - Default to ByStockDesc if not specified
            if(filter.Sort == null || filter.Sort == 0){
                filter.Sort = ProductFilter.ByStockDesc;
            }
            
            Func<SortDescriptor<ProductElasticDto>, IPromise<IList<ISort>>> sortFunc = null;
            if(filter.Sort != null){
                switch(filter.Sort){
                    case ProductFilter.ByPriceAsc:
                        sortFunc = sort => sort.Field(f => f.Field("SellerItems.SalePrice").Order(SortOrder.Ascending).Nested(n => n.Path("SellerItems")));
                        break;
                    case ProductFilter.ByPriceDesc:
                        sortFunc = sort => sort.Field(f => f.Field("SellerItems.SalePrice").Order(SortOrder.Descending).Nested(n => n.Path("SellerItems")));
                        break;
                    case ProductFilter.ByStockAsc:
                        sortFunc = sort => sort.Field(f => f.Field("SellerItems.Stock").Order(SortOrder.Ascending).Nested(n => n.Path("SellerItems")));
                        break;
                    case ProductFilter.ByStockDesc:
                        sortFunc = sort => sort.Field(f => f.Field("SellerItems.Stock").Order(SortOrder.Descending).Nested(n => n.Path("SellerItems")));
                        break;
                    case ProductFilter.ByCreationDateAsc:
                        sortFunc = sort => sort.Descending("CreatedDate");
                        break;
                    case ProductFilter.productNameAsc:
                        sortFunc = sort => sort.Field(f => f.Field(p => p.Name.Suffix("keyword")).Order(SortOrder.Ascending));
                        break;
                    case ProductFilter.productNamedesc:
                        sortFunc = sort => sort.Field(f => f.Field(p => p.Name.Suffix("keyword")).Order(SortOrder.Descending));
                        break;
                    default:
                        break;
                }
            }
            result = await _elasticSearchService._client.SearchAsync<ProductElasticDto>(s => s.Index(ElasticSearchIndexConstants.Products).Query(q => boolQuery).Sort(sortFunc).From((filter.Page - 1) * filter.PageSize).Size(filter.PageSize));
            var pagingResult = new Paging<List<ProductElasticDto>>{Data = result.Documents.ToList(), DataCount = (int) result.Total};
            rs.Result = pagingResult;
        } catch(Exception e){
            rs.AddError(e.Message);
        }
        return rs;
    }
    public async Task<IActionResult<List<ProductElasticDto>>> SearchAsync(string keyword){
        var rs = OperationResult.CreateResult<List<ProductElasticDto>>();
        try{
            var response = await _elasticSearchService._client.SearchAsync<ProductElasticDto>(s => s.Index(ElasticSearchIndexConstants.Products).Query(q => q.Bool(b => b.Must(m => m.Term(t => t.Status, 1)).Should(sh => sh.Match(mt => mt.Field(f => f.Name).Query(keyword).Fuzziness(Fuzziness.Auto)), sh => sh.Match(mt => mt.Field(f => f.Description).Query(keyword).Fuzziness(Fuzziness.Auto)), sh => sh.Match(mt => mt.Field("GroupCodes.OemCode").Query(keyword).Fuzziness(Fuzziness.Auto))).MinimumShouldMatch(1))).Size(100));
            if(response.IsValid){
                rs.Result = response.Documents.ToList();
            } else{
                rs.AddError(response.OriginalException?.Message ?? response.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        } catch(Exception e){
            rs.AddError(e.Message);
        }
        return rs;
    }
    
    public async Task<IActionResult<List<ProductElasticDto>>> GetByBrandIdAsync(int brandId)
    {
        var rs = OperationResult.CreateResult<List<ProductElasticDto>>();
        try
        {
            var result = await _elasticSearchService._client.SearchAsync<ProductElasticDto>(s => s
                .Index(ElasticSearchIndexConstants.Products)
                .Size(50)
                .Query(q => q.Bool(b => b.Must(
                    m => m.Term(t => t.Status, 1),
                    m => m.Term(t => t.Field("Brand.Id").Value(brandId))
                )))
            );

            if (result.IsValid)
            {
                rs.Result = result.Documents.ToList();
            }
            else
            {
                rs.AddError(result.OriginalException?.Message ?? "Elasticsearch error");
            }
        }
        catch (Exception e)
        {
            rs.AddError(e.Message);
        }

        return rs;
    }
}
