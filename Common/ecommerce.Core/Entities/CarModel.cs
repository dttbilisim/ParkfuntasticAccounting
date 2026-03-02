using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarModel : AuditableEntity<int>
{
    public string Name { get; set; }

    public int CarBrandId { get; set; }
    public CarBrand Brand { get; set; }

    public ICollection<CarEngine> Engines { get; set; }
}