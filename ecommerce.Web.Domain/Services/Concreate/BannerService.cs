using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Dtos.Banners;
using ecommerce.Web.Domain.Services.Abstract;
using Nest;
namespace ecommerce.Web.Domain.Services.Concreate;
public class BannerService : IBannerService{
    private readonly IElasticSearchService _elasticSearchService;
    public BannerService(IElasticSearchService elasticSearchService){_elasticSearchService = elasticSearchService;}
    public async Task<IActionResult<List<BannerItemDto>>> GetAllAsync(BannerType bannerType){
        var rs = OperationResult.CreateResult<List<BannerItemDto>>();
        try{
            var result = await _elasticSearchService._client.SearchAsync<BannerItemDto>(s => s.Index(ElasticSearchIndexConstants.BannerItems).Size(1000)
                .Query(q => q.Term(t => t.BannerType, bannerType) && q.Term(t => t.Status, 1))
            );
            if(result.IsValid){
                rs.Result = result.Documents.ToList();
            } else{
                rs.AddError(result.OriginalException?.Message ?? result.ServerError?.Error?.Reason ?? "Elasticsearch search failed");
            }
        } catch(Exception e){
            Console.WriteLine(e);
            throw;
        }
        return rs;
    }
}
