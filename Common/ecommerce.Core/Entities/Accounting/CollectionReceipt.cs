using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities.Accounting;

/// <summary>
/// Tahsilat makbuzu — cari + plasiyer bazlı, otomatik makbuz numarası ile saklanır.
/// </summary>
public class CollectionReceipt : AuditableEntity<int>
{
    /// <summary>
    /// Plasiyer bazlı otomatik tahsilat makbuz numarası (örn. TM-1-2026-00001).
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MakbuzNo { get; set; } = null!;

    /// <summary>
    /// Cari ID
    /// </summary>
    public int CustomerId { get; set; }
    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    /// <summary>
    /// Plasiyer (satış temsilcisi) ID — makbuz numarası bu plasiyere göre sıralanır.
    /// </summary>
    public int SalesPersonId { get; set; }
    [ForeignKey(nameof(SalesPersonId))]
    public virtual SalesPerson SalesPerson { get; set; } = null!;

    /// <summary>
    /// Bu makbuzun belgelediği cari hesap hareketi (tahsilat = Credit).
    /// </summary>
    public int CustomerAccountTransactionId { get; set; }
    [ForeignKey(nameof(CustomerAccountTransactionId))]
    public virtual CustomerAccountTransaction CustomerAccountTransaction { get; set; } = null!;

    /// <summary>
    /// Şube (multi-tenant)
    /// </summary>
    public int? BranchId { get; set; }
}
