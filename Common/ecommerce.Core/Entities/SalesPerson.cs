using ecommerce.Core.Entities.Base;
using ecommerce.Core.Entities.Hierarchical;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

public class SalesPerson : AuditableEntity<int>
{
    public int? BranchId { get; set; }
    [ForeignKey(nameof(BranchId))]
    public virtual Branch? Branch { get; set; }

    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? Phone { get; set; }
    public string? MobilePhone { get; set; }
    public string? Email { get; set; }
    
    public int? CityId { get; set; }
    [ForeignKey(nameof(CityId))]
    public virtual City? City { get; set; }

    public int? TownId { get; set; }
    [ForeignKey(nameof(TownId))]
    public virtual Town? Town { get; set; }

    public virtual ICollection<SalesPersonBranch> SalesPersonBranches { get; set; } = new List<SalesPersonBranch>();

    public string? Address { get; set; }
    public bool SmsPermission { get; set; }
}