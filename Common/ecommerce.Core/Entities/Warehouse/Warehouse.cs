using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Hierarchical;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities.Warehouse
{
    public class Warehouse : AuditableEntity<int>, ITenantEntity
    {
        public int BranchId { get; set; }

        public int? CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public virtual City? City { get; set; }

        public int? TownId { get; set; }
        [ForeignKey(nameof(TownId))]
        public virtual Town? Town { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; }

        public string? Address { get; set; }
        
        // Navigation Properties
        public virtual ICollection<WarehouseShelf> Shelves { get; set; } = new List<WarehouseShelf>();
    }
}
