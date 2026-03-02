using ecommerce.Core.Entities.Base;
using ecommerce.Core.Interfaces;

namespace ecommerce.Core.Entities;

public class InvoiceTypeDefinition : AuditableEntity<int>, ITenantEntity
{
    public int BranchId { get; set; }
    
    public string Name { get; set; } = null!;
}

