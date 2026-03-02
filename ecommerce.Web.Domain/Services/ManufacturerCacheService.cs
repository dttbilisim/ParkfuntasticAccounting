using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Domain.Shared.Abstract;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Dtos;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace ecommerce.Web.Domain.Services;

public interface IManufacturerCacheService
{
    Task<IActionResult<List<ManufacturerElasticDto>>> GetAllAsync();
    Task<IActionResult<ManufacturerElasticDto>> GetByIdAsync(int id);
    Task<IActionResult<ManufacturerElasticDto>> GetByNameAsync(string name);
    Task WarmupCacheAsync();
}

public class ManufacturerCacheService : IManufacturerCacheService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IRedisCacheService _cache;
    private readonly IElasticClient _elasticClient;
    private const string CACHE_KEY_ALL = "Manufacturers:All";
    private const string CACHE_KEY_PREFIX = "Manufacturer:";
    private const string ES_INDEX = "manufacturer_index";
    private static readonly TimeSpan CacheTime = TimeSpan.FromHours(24);

    public ManufacturerCacheService(ApplicationDbContext dbContext, IRedisCacheService cache, IElasticClient elasticClient)
    {
        _dbContext = dbContext;
        _cache = cache;
        _elasticClient = elasticClient;
    }

    public async Task<IActionResult<List<ManufacturerElasticDto>>> GetAllAsync()
    {
        var result = OperationResult.CreateResult<List<ManufacturerElasticDto>>();
        
        try
        {
            // Try Redis cache first - FASTEST
            var cached = await _cache.GetAsync<List<ManufacturerElasticDto>>(CACHE_KEY_ALL);
            if (cached != null && cached.Any())
            {
                result.Result = cached;
                return result;
            }

            // Try Elasticsearch - FAST with all data
            try
            {
                var esResponse = await _elasticClient.SearchAsync<ManufacturerElasticDto>(s => s
                    .Index(ES_INDEX)
                    .Size(1000) // Increased from 100 to 1000 to catch all manufacturers
                    .Query(q => q
                        .Bool(b => b
                            .Must(
                                m => m.Range(r => r.Field(f => f.Order).LessThan(100))
                            )
                        )
                    )
                    .Sort(sort => sort.Ascending(m => m.Order)));

                if (esResponse.IsValid && esResponse.Documents.Any())
                {
                    var manufacturers = esResponse.Documents
                        // .Where(m => !string.IsNullOrEmpty(m.LogoUrl)) // REMOVED FILTER: Allow brands without logos
                        .GroupBy(m => m.Id) // Group by Id to prevent duplicates
                        .Select(g => g.First()) // Take first from each group
                        .OrderBy(m => m.Order)
                        // .Take(50) // REMOVED LIMIT: Client needs all manufacturers
                        .ToList();

                    var summaries = manufacturers
                        .Select(CreateSummary)
                        .DistinctBy(s => s.Id) // Ensure uniqueness by Id first
                        .DistinctBy(s => s.Name?.Trim().ToLowerInvariant()) // Then by Name (case-insensitive)
                        .ToList();

                    Console.WriteLine($"✅ Loaded {summaries.Count} manufacturers from Elasticsearch (deduplicated by Id)");
                    await _cache.SetAsync(CACHE_KEY_ALL, summaries, CacheTime);
                    result.Result = summaries;
                    return result;
                }
            }
            catch
            {
                // ES failed and DB fallback is disabled by request
            }

            result.AddError("Elasticsearch unavailable or empty for manufacturers. DB fallback disabled.");
            return result;
        }
        catch (Exception ex)
        {
            result.AddSystemError($"Error: {ex.Message}");
        }

        return result;
    }

    public async Task<IActionResult<ManufacturerElasticDto>> GetByIdAsync(int id)
    {
        var result = OperationResult.CreateResult<ManufacturerElasticDto>();
        
        try
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}{id}";
            
            // Try Redis cache - INSTANT
            var cached = await _cache.GetAsync<ManufacturerElasticDto>(cacheKey);
            if (cached != null && cached.Models?.Any() == true)
            {
                result.Result = cached;
                return result;
            }

            // Try Elasticsearch - FAST with images
            try
            {
                Console.WriteLine($"🔍 Loading manufacturer {id} from Elasticsearch...");
                var esResponse = await _elasticClient.GetAsync<ManufacturerElasticDto>(id, g => g.Index(ES_INDEX));
                if (esResponse.IsValid && esResponse.Found && esResponse.Source?.Models?.Any() == true)
                {
                    var withImages = esResponse.Source.Models.Count(m => !string.IsNullOrEmpty(m.ImageUrl));
                    Console.WriteLine($"✅ Loaded manufacturer {esResponse.Source.Name}: {esResponse.Source.Models.Count} models, {withImages} with images");
                    
                    await _cache.SetAsync(cacheKey, esResponse.Source, CacheTime);
                    result.Result = esResponse.Source;
                    return result;
                }
                else
                {
                    Console.WriteLine($"⚠️ Elasticsearch response for {id}: Valid={esResponse.IsValid}, Found={esResponse.Found}, HasModels={esResponse.Source?.Models?.Any()}");
                }
            }
            catch
            {
                // ES failed and DB fallback is disabled by request
            }
            result.AddError("Elasticsearch unavailable or empty for manufacturer. DB fallback disabled.");
            return result;
        }
        catch (Exception ex)
        {
            result.AddSystemError($"Error: {ex.Message}");
        }

        return result;
    }

    public async Task<IActionResult<ManufacturerElasticDto>> GetByNameAsync(string name)
    {
        var result = OperationResult.CreateResult<ManufacturerElasticDto>();
        
        try
        {
            
            var esResponse = await _elasticClient.SearchAsync<ManufacturerElasticDto>(s => s
                .Index(ES_INDEX)
                .Size(100)
                .Query(q => q.Match(m => m.Field(f => f.Name).Query(name))));

            if (esResponse.IsValid && esResponse.Documents.Any())
            {
                var allDocs = esResponse.Documents.ToList();
                
                if (allDocs.Count == 1)
                {
                    result.Result = allDocs.First();
                }
                else
                {
                    // Multiple documents (different VehicleTypes) - merge all models
                    var firstDoc = allDocs.First();
                    result.Result = MergeManufacturers(allDocs);
                    
                    Console.WriteLine($"✅ Merged {allDocs.Count} documents for {name}: {result.Result.ModelCount} total models (duplicates removed)");
                }
                
                return result;
            }
            
            result.AddError($"Manufacturer '{name}' not found in Elasticsearch");
        }
        catch (Exception ex)
        {
            result.AddSystemError($"Error fetching manufacturer by name: {ex.Message}");
        }

        return result;
    }

    public async Task WarmupCacheAsync()
    {
        try
        {
           
            
            await GetAllAsync();

          
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Cache warmup failed: {ex.Message}");
        }
    }

    private static ManufacturerElasticDto MergeManufacturers(IReadOnlyCollection<ManufacturerElasticDto> documents)
    {
        if (documents == null || documents.Count == 0)
        {
            return new ManufacturerElasticDto();
        }

        var first = documents.First();

        var mergedModels = documents
            .SelectMany(doc => doc.Models ?? new List<BaseModelDto>())
            .GroupBy(model => model.Id)
            .Select(MergeModelGroup)
            .OrderBy(model => model.Name)
            .ToList();

        return new ManufacturerElasticDto
        {
            Id = first.Id,
            DatKey = first.DatKey,
            Name = first.Name,
            VehicleType = first.VehicleType,
            LogoUrl = first.LogoUrl,
            Order = first.Order,
            ModelCount = mergedModels.Count,
            Models = mergedModels
        };
    }

    private static BaseModelDto MergeModelGroup(IGrouping<int, BaseModelDto> grouping)
    {
        var first = grouping.First();

        var mergedSubModels = grouping
            .SelectMany(model => model.SubModels ?? new List<SubModelDto>())
            .Where(subModel => subModel != null)
            .GroupBy(BuildSubModelKey)
            .Select(g =>
            {
                var seed = g.First();
                return new SubModelDto
                {
                    Id = seed.Id,
                    Name = seed.Name,
                    SubModelKey = seed.SubModelKey,
                    ImageUrl = seed.ImageUrl
                };
            })
            .OrderBy(subModel => subModel.Name)
            .ToList();

        return new BaseModelDto
        {
            Id = first.Id,
            Name = first.Name,
            VehicleType = first.VehicleType,
            ManufacturerKey = first.ManufacturerKey,
            BaseModelKey = first.BaseModelKey,
            ImageUrl = first.ImageUrl,
            SubModels = mergedSubModels
        };
    }

    private static string BuildSubModelKey(SubModelDto subModel)
    {
        if (!string.IsNullOrWhiteSpace(subModel.SubModelKey))
        {
            return subModel.SubModelKey.Trim().ToLowerInvariant();
        }

        return $"id:{subModel.Id}";
    }

    private static ManufacturerElasticDto CreateSummary(ManufacturerElasticDto source)
    {
        var summaryModels = (source.Models ?? new List<BaseModelDto>())
            .Select(model => new BaseModelDto
            {
                Id = model.Id,
                Name = model.Name,
                VehicleType = model.VehicleType,
                ManufacturerKey = model.ManufacturerKey,
                BaseModelKey = model.BaseModelKey,
                ImageUrl = model.ImageUrl,
                SubModels = model.SubModels ?? new List<SubModelDto>()
            })
            .ToList();

        return new ManufacturerElasticDto
        {
            Id = source.Id,
            DatKey = source.DatKey,
            Name = source.Name,
            VehicleType = source.VehicleType,
            LogoUrl = source.LogoUrl,
            Order = source.Order,
            ModelCount = summaryModels.Count,
            Models = summaryModels
        };
    }
}

