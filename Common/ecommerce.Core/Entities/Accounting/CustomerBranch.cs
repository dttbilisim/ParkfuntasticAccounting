using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Hierarchical;

namespace ecommerce.Core.Entities.Accounting
{
    public class CustomerBranch : AuditableEntity<int>
    {
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; } = null!;

        public int BranchId { get; set; }
        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; } = null!;

        public bool IsDefault { get; set; }
    }
}
