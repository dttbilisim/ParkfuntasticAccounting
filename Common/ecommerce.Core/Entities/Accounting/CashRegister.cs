using ecommerce.Core.Entities.Base;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities.Accounting
{
    public class CashRegister : AuditableEntity<int>, ITenantEntity
    {
        public int BranchId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public int CurrencyId { get; set; }
        public virtual Currency Currency { get; set; } = null!;

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal OpeningBalance { get; set; }

        public bool IsDefault { get; set; }
        
        public int? PaymentTypeId { get; set; }
        [ForeignKey(nameof(PaymentTypeId))]
        public virtual PaymentType? PaymentType { get; set; }

        /// <summary>Ödeme tipi 3 veya 4 (Havale/EFT, Çek) seçildiğinde bağlanacak banka hesabı.</summary>
        public int? BankAccountId { get; set; }
        [ForeignKey(nameof(BankAccountId))]
        public virtual ecommerce.Core.Entities.BankAccount? BankAccount { get; set; }
    }
}
