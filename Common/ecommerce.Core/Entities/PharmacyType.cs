using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities {
    public class PharmacyType : AuditableEntity<int> {
        public string Name { get; set; } = null!;
    }
}

