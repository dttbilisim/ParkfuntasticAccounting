using System.Collections.Generic;

namespace ecommerce.Domain.Shared.Dtos.Product
{
    public class SearchFilterAggregations
    {
        public List<string> Manufacturers { get; set; } = new();
        public List<string> BaseModels { get; set; } = new();
        public List<string> SubModels { get; set; } = new();
        public List<string> DotPartNames { get; set; } = new();
        public Dictionary<int, string> Brands { get; set; } = new(); // Id, Name
        public Dictionary<int, string> Categories { get; set; } = new(); // Id, Name
    }
}
