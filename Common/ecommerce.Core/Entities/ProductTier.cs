using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    public class ProductTier : AuditableEntity<int>
    {
        public int ProductId { get; set; }
        public int TierId { get; set; }

        [ForeignKey("ProductId ")]
        public Product Product { get; set; }

        [ForeignKey("TierId ")]
        public Tier Tier { get; set; }
    }
}

