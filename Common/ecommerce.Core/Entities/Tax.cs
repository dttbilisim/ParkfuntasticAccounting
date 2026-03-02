using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities
{
	public class Tax:AuditableEntity<int>
	{
        public string Name { get; set; } = null!;
        public int TaxRate { get; set; }
        public int? BranchId { get; set; }

       
    }
}

