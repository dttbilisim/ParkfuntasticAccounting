using ecommerce.Core.Utils;
namespace ecommerce.Domain.Shared.Dtos.Filters;
public class SearchFilterReguestDto{

    public int? CategoryId{get;set;}
    public int? BrandId{get;set;}
    public List<int>? CategoryIds { get; set; }

    public List<int>? BrandIds { get; set; }

    public string? Search { get; set; }
    
    public ProductFilter Sort { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; } = 50;

    public string? ManufacturerKey { get; set; }

    public string? BaseModelKey { get; set; }

    public int? ModelId { get; set; }

    public string? PartNumber { get; set; }

    public List<string>? DotPartNames { get; set; }

    public List<string>? ManufacturerNames { get; set; }
    public List<string>? ManufacturerKeys { get; set; }

    public List<string>? BaseModelNames { get; set; }

    public List<string>? SubModelNames { get; set; }

    public List<string>? SubModelKeys { get; set; }
    
    // Fallback names for Backend Logic
    public string? SingleManufacturerName { get; set; }
    public string? SingleModelName { get; set; }
    public string? SingleSubModelName { get; set; }

    public bool OnlyPerfectCompatibility { get; set; }
    public bool OnlyInStock { get; set; }
    
    public double? MinPrice { get; set; }
    public double? MaxPrice { get; set; }
    public bool OnlyWithImage { get; set; }
    public List<string>? DatProcessNumbers { get; set; }
    public List<int>? ProductIds { get; set; }
    public bool ShouldGroupOems { get; set; }
    
    /// <summary>
    /// VIN decode sonrası OEM kodları ile direkt arama için kullanılır
    /// </summary>
    public List<string>? OemCodes { get; set; }
}
