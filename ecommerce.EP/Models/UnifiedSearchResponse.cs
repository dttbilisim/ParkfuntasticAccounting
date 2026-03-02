using ecommerce.Admin.Services.Dtos.VinDto;
using ecommerce.Core.Models;
using ecommerce.Domain.Shared.Dtos.Product;
using ecommerce.Domain.Shared.Dtos.Brand;
using ecommerce.Domain.Shared.Dtos.Category;

namespace ecommerce.EP.Models;

public class UnifiedSearchResponse
{
    public string SearchType { get; set; } = "Products";
    public string? Vin { get; set; }
    public VinDecodeResultDto? VinResult { get; set; }
    public Paging<List<SellerProductViewModel>>? Products { get; set; }
    public SearchFilterAggregations? Aggregations { get; set; }
    public List<BrandDto> Brands { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
}
