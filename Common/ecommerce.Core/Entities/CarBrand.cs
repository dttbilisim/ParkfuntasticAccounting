using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarBrand : AuditableEntity<int>
{
    public string Name { get; set; }
    public ICollection<CarModel> Models { get; set; }
}
