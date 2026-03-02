using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.EFCore.Context;
using ecommerce.Web.Domain.Dtos;
using Microsoft.EntityFrameworkCore;
using Nest;

namespace ecommerce.Web.Domain.Services;

public interface IManufacturerElasticService
{
    Task<IActionResult<List<ManufacturerElasticDto>>> GetAllAsync(int? vehicleType = null, int limit = 50);
    Task<IActionResult<ManufacturerElasticDto>> GetByIdAsync(int id);
    Task<IActionResult<ManufacturerElasticDto>> GetByNameAsync(string name);
    Task<IActionResult<List<BaseModelDto>>> GetModelsByManufacturerAsync(int manufacturerId, int? vehicleType = null);
}

public class ManufacturerElasticService : IManufacturerElasticService
{
    private readonly IElasticClient _elasticClient;
    private readonly ApplicationDbContext _dbContext;
    private const string IndexName = "manufacturer_index";

    public ManufacturerElasticService(IElasticClient elasticClient, ApplicationDbContext dbContext)
    {
        _elasticClient = elasticClient;
        _dbContext = dbContext;
    }

    public async Task<IActionResult<List<ManufacturerElasticDto>>> GetAllAsync(int? vehicleType = null, int limit = 50)
    {
        var result = OperationResult.CreateResult<List<ManufacturerElasticDto>>();
        
        try
        {
            // Try Elasticsearch first
            try
            {
                var searchDescriptor = new SearchDescriptor<ManufacturerElasticDto>()
                    .Index(IndexName)
                    .Size(limit)
                    .Sort(s => s
                        .Ascending(m => m.Order)
                        .Ascending(m => m.Name.Suffix("keyword")));

                if (vehicleType.HasValue)
                {
                    searchDescriptor = searchDescriptor.Query(q => q
                        .Term(t => t.Field(f => f.VehicleType).Value(vehicleType.Value)));
                }
                else
                {
                    searchDescriptor = searchDescriptor.Query(q => q.MatchAll());
                }

                var response = await _elasticClient.SearchAsync<ManufacturerElasticDto>(searchDescriptor);

                if (response.IsValid && response.Documents.Any())
                {
                    result.Result = response.Documents
                        .GroupBy(doc => doc.DatKey)
                        .Select(g => MergeManufacturers(g.ToList()))
                        .Select(CreateSummary)
                        .ToList();
                    return result;
                }
            }
            catch
            {
                // Elasticsearch failed, fallback to DB
            }

            // Fallback to DB
            var query = _dbContext.Set<DotManufacturer>()
                .AsNoTracking()
                .Where(x => x.IsActive && !string.IsNullOrEmpty(x.LogoUrl));

            if (vehicleType.HasValue)
            {
                query = query.Where(x => x.VehicleType == vehicleType.Value);
            }

            var manufacturers = await query
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Name)
                .Take(limit)
                .ToListAsync();

            result.Result = manufacturers.Select(m => new ManufacturerElasticDto
            {
                Id = m.Id,
                DatKey = m.DatKey,
                Name = m.Name,
                VehicleType = m.VehicleType,
                LogoUrl = m.LogoUrl,
                Order = m.Order,
                ModelCount = 0,
                Models = new()
            }).ToList();
        }
        catch (Exception ex)
        {
            result.AddSystemError($"Error fetching manufacturers: {ex.Message}");
        }

        return result;
    }

    public async Task<IActionResult<ManufacturerElasticDto>> GetByIdAsync(int id)
    {
        var result = OperationResult.CreateResult<ManufacturerElasticDto>();
        
        try
        {
            var response = await _elasticClient.GetAsync<ManufacturerElasticDto>(id, g => g.Index(IndexName));

            if (response.IsValid && response.Found && response.Source?.Models?.Any() == true)
            {
                result.Result = response.Source;
                return result;
            }
            
            result.AddError($"Manufacturer {id} not found in Elasticsearch");
        }
        catch (Exception ex)
        {
            result.AddSystemError($"Error fetching manufacturer: {ex.Message}");
        }

        return result;
    }

    public async Task<IActionResult<ManufacturerElasticDto>> GetByNameAsync(string name)
    {
        var result = OperationResult.CreateResult<ManufacturerElasticDto>();
        
        try
        {
            // Search for ALL documents with this manufacturer name
            var response = await _elasticClient.SearchAsync<ManufacturerElasticDto>(s => s
                .Index(IndexName)
                .Size(10)
                .Query(q => q.Match(m => m.Field(f => f.Name).Query(name))));

            if (response.IsValid && response.Documents.Any())
            {
                var allDocs = response.Documents.ToList();
                
                if (allDocs.Count == 1)
                {
                    // Single document - return as is
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

    public async Task<IActionResult<List<BaseModelDto>>> GetModelsByManufacturerAsync(int manufacturerId, int? vehicleType = null)
    {
        var result = OperationResult.CreateResult<List<BaseModelDto>>();
        
        try
        {
            var manufacturerResult = await GetByIdAsync(manufacturerId);
            
            if (!manufacturerResult.Ok || manufacturerResult.Result == null)
            {
                result.AddError("Manufacturer not found");
                return result;
            }

            var models = manufacturerResult.Result.Models;

            if (vehicleType.HasValue)
            {
                models = models.Where(m => m.VehicleType == vehicleType.Value).ToList();
            }

            result.Result = models.OrderBy(m => m.Name).ToList();
        }
        catch (Exception ex)
        {
            result.AddSystemError($"Error fetching models: {ex.Message}");
        }

        return result;
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

