using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarEngine : AuditableEntity<int>
{
    public string Name { get; set; }

    public int CarModelId { get; set; }
    public CarModel Model { get; set; }

    public ICollection<CarVehicle> Vehicles { get; set; }
}