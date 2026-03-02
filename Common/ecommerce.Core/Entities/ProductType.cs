using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities {
    public class ProductType:AuditableEntity<int>
	{
        public int? BranchId { get; set; }
        public string Name { get; set; } = null!;


    }
}

