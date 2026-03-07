using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Hierarchical;

namespace ecommerce.Core.Entities
{
    /// <summary>PcPos transfer: tProductBranch - Ürün-Şube eşlemesi</summary>
    public class ProductBranch
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;
        public int BranchId { get; set; }
        [ForeignKey(nameof(BranchId))]
        public virtual Branch Branch { get; set; } = null!;
    }
}
