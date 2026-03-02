using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Authentication;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Hierarchical
{
    public class UserBranch : AuditableEntity<int>
    {
        public int UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual ApplicationUser User { get; set; } = null!;

        public int BranchId { get; set; }
        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; } = null!;

        public bool IsDefault { get; set; }
    }
}
