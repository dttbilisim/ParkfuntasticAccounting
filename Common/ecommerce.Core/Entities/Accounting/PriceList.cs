using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Warehouse;
using ecommerce.Core.Entities;

namespace ecommerce.Core.Entities.Accounting
{
    public class PriceList : AuditableEntity<int>
    {
        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        public DateTime StartDate { get; set; } = DateTime.Now;

        public bool IsActive { get; set; } = true;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int? CorporationId { get; set; }
        public int? BranchId { get; set; }

        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public Customer? Customer { get; set; }

        public int? WarehouseId { get; set; }
        [ForeignKey(nameof(WarehouseId))]
        public Warehouse.Warehouse? Warehouse { get; set; }

        public int? CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public Currency? Currency { get; set; }

        [MaxLength(10)]
        public string? CurrencyCode { get; set; }

        // Navigation property for price list items
        public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
    }
}
