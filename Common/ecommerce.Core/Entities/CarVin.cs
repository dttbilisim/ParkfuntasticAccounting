using System.ComponentModel.DataAnnotations;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarVin : AuditableEntity<int>
{
    
    public string Code { get; set; }

    public ICollection<CarSpec> Specs { get; set; } = new List<CarSpec>();
}
