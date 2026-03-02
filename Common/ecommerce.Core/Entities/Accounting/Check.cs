using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities.Accounting
{
    /// <summary>
    /// Çek kaydı — cari, banka, şube, vade, tutar
    /// </summary>
    public class Check : AuditableEntity<int>, ITenantEntity
    {
        /// <summary>Cari (müşteri)</summary>
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; } = null!;

        public int BankId { get; set; }
        [ForeignKey(nameof(BankId))]
        public virtual Bank Bank { get; set; } = null!;

        /// <summary>Banka şubesi (il/ilçe bilgisi buradan gelir)</summary>
        public int? BankBranchId { get; set; }
        [ForeignKey(nameof(BankBranchId))]
        public virtual BankBranch? BankBranch { get; set; }

        [Required]
        [MaxLength(50)]
        public string CheckNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime DueDate { get; set; }

        public int CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public virtual Currency Currency { get; set; } = null!;

        public CheckStatus CheckStatus { get; set; } = CheckStatus.InPortfolio;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Şube (multi-tenant). Zorunlu — listeleme/erişim bu alana göre filtrelenir.</summary>
        public int BranchId { get; set; }

        /// <summary>Çek giriş tarihi</summary>
        public DateTime? ReceivedDate { get; set; }

        /// <summary>Tahsil / red / iade tarihi</summary>
        public DateTime? SettlementDate { get; set; }
    }
}
