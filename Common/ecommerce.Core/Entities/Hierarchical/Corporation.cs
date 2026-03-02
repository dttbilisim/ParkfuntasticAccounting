using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Hierarchical
{
    public class Corporation : AuditableEntity<int>
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [MaxLength(50)]
        public string? TaxNumber { get; set; }

        [MaxLength(100)]
        public string? TaxOffice { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(100)]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? LogoUrl { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<Branch> Branches { get; set; } = new List<Branch>();
    }
}
