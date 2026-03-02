using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarFuelType : AuditableEntity<int>
{
    public string Name { get; set; }
    public ICollection<CarVehicle> Vehicles { get; set; }
}