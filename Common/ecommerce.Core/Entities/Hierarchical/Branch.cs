using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Hierarchical
{
    public class Branch : AuditableEntity<int>
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [MaxLength(50)]
        public string Code { get; set; } = null!;

        public int CorporationId { get; set; }
        [ForeignKey(nameof(CorporationId))]
        public virtual Corporation Corporation { get; set; } = null!;

        public int? CityId { get; set; }
        public int? TownId { get; set; }

        [MaxLength(20)]
        public string? PhoneNumber { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation
        public virtual ICollection<UserBranch> UserBranches { get; set; } = new List<UserBranch>();
    }
}
