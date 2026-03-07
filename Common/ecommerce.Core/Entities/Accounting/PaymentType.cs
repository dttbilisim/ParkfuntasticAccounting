using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities;
using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities.Accounting;

public class PaymentType : AuditableEntity<int>, ITenantEntity
{
    public int BranchId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = null!;

    public bool IsCash { get; set; }
    public bool IsCreditCard { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>PcPos transfer: Ödeme tipi kodu (1,2,3,11,12)</summary>
    public int? Type { get; set; }
    /// <summary>PcPos transfer: Döviz kuru FK</summary>
    public int? CurrencyId { get; set; }
    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency? Currency { get; set; }
    /// <summary>PcPos transfer: POS'ta kullanılabilir mi?</summary>
    public bool IsPcPos { get; set; }
    [MaxLength(50)]
    public string? CompanyCode { get; set; }
}
