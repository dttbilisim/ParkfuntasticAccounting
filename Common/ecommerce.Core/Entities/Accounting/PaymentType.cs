using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
}
