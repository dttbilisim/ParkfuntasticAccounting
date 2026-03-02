using AutoMapper;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.Core.Utils;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.Domain.Shared.Conts;
using ecommerce.Domain.Shared.Dtos.Banners;
using ecommerce.Domain.Shared.Dtos.Category;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Services.Abstract;

namespace ecommerce.Web.Domain.Services.Concreate;

public class CategoryService: ICategoryService
{
    private readonly IUnitOfWork<ApplicationDbContext> context;
    private readonly IMapper mapper;
    private readonly IElasticSearchService _elasticSearchService;


    public CategoryService(IUnitOfWork<ApplicationDbContext> _context, IMapper _mapper, IElasticSearchService elasticSearchService)
    {
        context = _context;
         mapper = _mapper;
         _elasticSearchService = elasticSearchService;

    }
    public async Task<IActionResult<List<CategoryElasticDto>>> GetAllAsync()
    {
        var rs = OperationResult.CreateResult<List<CategoryElasticDto>>();

        try
        {
            var result = await _elasticSearchService._client.SearchAsync<CategoryElasticDto>(s => s
                    .Index(ElasticSearchIndexConstants.Categories)
                    .Size(1000)
                    .Query(q => q.Term(t => t.Status, 1))
                   

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
            rs.AddError("Exception: " + e.Message);
        }

        return rs;
    }
    public async Task<IActionResult<List<CategoryElasticDto>>> GetAllWithIsMainPageAsync(){
        var rs = OperationResult.CreateResult<List<CategoryElasticDto>>();

        try
        {
            var result = await _elasticSearchService._client.SearchAsync<CategoryElasticDto>(s => s
                .Index(ElasticSearchIndexConstants.Categories)
                .Size(1000)
                .Query(q => q.Term(t => t.Status, 1))
                .Query(q => q.Term(t => t.IsMainPage, true))
                   

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
            rs.AddError("Exception: " + e.Message);
        }

        return rs;
    }
    public async Task<IActionResult<List<CategoryElasticDto>>> GetCatehoryWithById(int categoryId){
        var rs = OperationResult.CreateResult<List<CategoryElasticDto>>();
        try
        {
            var result = await _elasticSearchService._client.SearchAsync<CategoryElasticDto>(s => s.Index(ElasticSearchIndexConstants.Categories)
                
                .Query(q => q.Term(t => t.Status, 1)).Query(q => q.Term(t => t.ParentId, categoryId))
                .Size(1000));
            if(result.Documents != null){
                rs.Result = result.Documents.ToList();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            rs.AddError("Exception: " + e.Message);
        }
        return rs;
    }
    public async Task<IActionResult<List<CategoryElasticDto>>> GetAllCategoryFooter(){
        var rs = OperationResult.CreateResult<List<CategoryElasticDto>>();

        try
        {
            var result = await _elasticSearchService._client.SearchAsync<CategoryElasticDto>(s => s
                .Index(ElasticSearchIndexConstants.Categories)
                .Size(5)
                .Query(q => q.Term(t => t.Status, 1))
                .Query(q => q.Term(t => t.IsMainPage, true))
                   

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
            rs.AddError("Exception: " + e.Message);
        }

        return rs;
    }
}