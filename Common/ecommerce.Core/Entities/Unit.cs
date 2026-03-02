using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities
{
    public class Unit : AuditableEntity<int>
    {
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = null!;
        public int? BranchId { get; set; }

        // Navigation property
        public ICollection<ProductUnit> ProductUnits { get; set; } = new List<ProductUnit>();
    }
}
