using System.Text.Json.Serialization;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities {
    public class ProductCategories:IEntity<int>
	{
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int CategoryId { get; set; }

        [JsonIgnore]
        public Product Product { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public int BranchId { get; set; }

    }
}

