using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Warehouse
{
    public class WarehouseShelf : AuditableEntity<int>
    {
        public int WarehouseId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Code { get; set; } // e.g. A-01-R5

        public string? Description { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; }

        public ICollection<ProductStock> ProductStocks { get; set; } = new List<ProductStock>();
    }
}
