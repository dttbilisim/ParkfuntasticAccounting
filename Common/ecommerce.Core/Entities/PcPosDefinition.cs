using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;

namespace ecommerce.Core.Entities;

public class PcPosDefinition : AuditableEntity<int>
{
    public string Name { get; set; } = null!;
    public int BranchId { get; set; }
    public int WarehouseId { get; set; }
    
    public int? PaymentTypeId { get; set; }
    [ForeignKey(nameof(PaymentTypeId))]
    public virtual ecommerce.Core.Entities.Accounting.PaymentType? PaymentType { get; set; }
}

