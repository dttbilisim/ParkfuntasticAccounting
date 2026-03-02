using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Accounting
{
    public class PriceListItem : AuditableEntity<int>
    {
        public int PriceListId { get; set; }
        [ForeignKey(nameof(PriceListId))]
        public PriceList PriceList { get; set; } = null!;

        public int? ProductId { get; set; }
        [ForeignKey(nameof(ProductId))]
        public Product? Product { get; set; }

        [MaxLength(200)]
        public string? ProductName { get; set; }

        public decimal CostPrice { get; set; }

        public decimal SalePrice { get; set; }

        public int Order { get; set; }
    }
}
