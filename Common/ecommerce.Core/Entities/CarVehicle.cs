using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarVehicle : AuditableEntity<int>
{
    public int CarEngineId { get; set; }
    public CarEngine Engine { get; set; }

    public int CarFuelTypeId { get; set; }
    public CarFuelType FuelType { get; set; }

    public int CarGearboxId { get; set; }
    public CarGearbox Gearbox { get; set; }

    public string Years { get; set; }
    public string VIN { get; set; }
    public string OriginalNumbers { get; set; }
}