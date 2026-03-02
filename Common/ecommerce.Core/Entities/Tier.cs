using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities
{
    public class Tier : AuditableEntity<int> {
        public int? BranchId { get; set; }
        public string Name { get; set; }
    }
}

