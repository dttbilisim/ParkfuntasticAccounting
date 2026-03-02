using ecommerce.Core.Entities.Accounting;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities
{
    public class CustomerPlasiyer
    {
        public int Id { get; set; }

        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer Customer { get; set; } = null!;

        public int SalesPersonId { get; set; }
        [ForeignKey(nameof(SalesPersonId))]
        public SalesPerson SalesPerson { get; set; } = null!;

        public int RegionId { get; set; }
        [ForeignKey(nameof(RegionId))]
        public Region Region { get; set; } = null!;

        public bool IsDefault { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }
}

