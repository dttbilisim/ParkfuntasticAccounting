using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Utils;

namespace ecommerce.Core.Entities.Accounting
{
    /// <summary>
    /// Cari hesap hareketleri tablosu - Borç/Alacak takibi için
    /// </summary>
    public class CustomerAccountTransaction : AuditableEntity<int>
    {
        /// <summary>
        /// Cari ID (FK to Customer)
        /// </summary>
        public int CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public virtual Customer Customer { get; set; } = null!;

        /// <summary>
        /// Şube ID (Multi-branch filtering)
        /// </summary>
        public int? BranchId { get; set; }


        /// <summary>
        /// Sipariş ID (opsiyonel - siparişten kaynaklanıyorsa)
        /// </summary>
        public int? OrderId { get; set; }
        [ForeignKey(nameof(OrderId))]
        public virtual Orders? Order { get; set; }

        /// <summary>
        /// Fatura ID (opsiyonel - faturadan kaynaklanıyorsa)
        /// </summary>
        public int? InvoiceId { get; set; }
        [ForeignKey(nameof(InvoiceId))]
        public virtual Invoice? Invoice { get; set; }

        /// <summary>
        /// İşlem Tipi: Debit (Borç) veya Credit (Alacak)
        /// </summary>
        public CustomerAccountTransactionType TransactionType { get; set; }

        /// <summary>
        /// İşlem Tutarı
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// İşlem Tarihi
        /// </summary>
        public DateTime TransactionDate { get; set; } = DateTime.Now;

        /// <summary>
        /// Açıklama
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Ödeme Tipi (CreditCart, CustomerBalance) - Enum olarak tutulur
        /// </summary>
        [Column("PaymentTypeId", TypeName = "integer")]
        public ecommerce.Core.Utils.PaymentType? PaymentTypeId { get; set; }

        /// <summary>
        /// Kasa ID (nakit ödemeler için)
        /// </summary>
        public int? CashRegisterId { get; set; }
        [ForeignKey(nameof(CashRegisterId))]
        public virtual CashRegister? CashRegister { get; set; }

        /// <summary>
        /// Referans No (sipariş no, fatura no vb.)
        /// </summary>
        [MaxLength(50)]
        public string? ReferenceNo { get; set; }

        /// <summary>
        /// Çek ID (çek kaynaklı cari hareket için)
        /// </summary>
        public int? CheckId { get; set; }
        [ForeignKey(nameof(CheckId))]
        public virtual Check? Check { get; set; }

        /// <summary>
        /// Bakiye (işlem sonrası cari bakiyesi - hesaplanabilir veya saklanabilir)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal BalanceAfterTransaction { get; set; }
    }
}
