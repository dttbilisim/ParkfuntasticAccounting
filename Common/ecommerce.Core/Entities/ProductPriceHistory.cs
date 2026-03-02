using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities
{
    public class ProductPriceHistory : AuditableEntity<int>
    {
        public int CompanyId { get; set; }
        public int ProductId { get; set; }
        public decimal OldPrice { get; set; }
        public decimal NewPrice { get; set; }
        [ForeignKey("ProductId")]
        public Product Product { get; set; }

        [ForeignKey("CompanyId")]
        public Company Company { get; set; }
    }
}