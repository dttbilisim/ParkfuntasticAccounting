using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Hierarchical;

namespace ecommerce.Core.Entities
{
    public class SalesPersonBranch : AuditableEntity<int>
    {
        public int SalesPersonId { get; set; }
        [ForeignKey(nameof(SalesPersonId))]
        public virtual SalesPerson SalesPerson { get; set; } = null!;

        public int BranchId { get; set; }
        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; } = null!;

        public bool IsDefault { get; set; }
    }
}
