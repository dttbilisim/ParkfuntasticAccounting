using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities {
    public class Brand: AuditableEntity<int> {
        public int? BranchId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }

    }
}

