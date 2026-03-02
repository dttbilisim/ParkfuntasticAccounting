using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities.Accounting
{
    /// <summary>
    /// Kasa hareketleri — kasa girişi / kasa çıkışı, çoklu döviz destekli
    /// </summary>
    public class CashRegisterMovement : AuditableEntity<int>, ITenantEntity
    {
        /// <summary>Hangi kasa</summary>
        public int CashRegisterId { get; set; }
        [ForeignKey(nameof(CashRegisterId))]
        public virtual CashRegister CashRegister { get; set; } = null!;

        /// <summary>Kasa girişi veya kasa çıkışı</summary>
        public CashRegisterMovementType MovementType { get; set; }

        /// <summary>Cari (opsiyonel — giriş/çıkışta cari seçilebilir)</summary>
        public int? CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer? Customer { get; set; }

        /// <summary>Ödeme tipi (tablo: PaymentType)</summary>
        public int? PaymentTypeId { get; set; }
        [ForeignKey(nameof(PaymentTypeId))]
        public virtual PaymentType? PaymentType { get; set; }

        /// <summary>Döviz — çoklu döviz girişi</summary>
        public int CurrencyId { get; set; }
        [ForeignKey(nameof(CurrencyId))]
        public virtual Currency Currency { get; set; } = null!;

        /// <summary>İşlem tutarı (seçilen döviz cinsinden)</summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>İşlem tarihi</summary>
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>Şube (multi-tenant)</summary>
        public int BranchId { get; set; }
    }
}
