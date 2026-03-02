using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities.Accounting
{
    /// <summary>
    /// Banka şubesi — il/ilçe ile konum bilgisi (master data, tenant bağımsız)
    /// </summary>
    public class BankBranch
    {
        [Key]
        public int Id { get; set; }

        public int BankId { get; set; }
        [ForeignKey(nameof(BankId))]
        public virtual Bank Bank { get; set; } = null!;

        /// <summary>İl (City)</summary>
        public int CityId { get; set; }
        [ForeignKey(nameof(CityId))]
        public virtual City City { get; set; } = null!;

        /// <summary>İlçe (Town) — opsiyonel</summary>
        public int? TownId { get; set; }
        [ForeignKey(nameof(TownId))]
        public virtual Town? Town { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        public bool Active { get; set; } = true;
    }
}
