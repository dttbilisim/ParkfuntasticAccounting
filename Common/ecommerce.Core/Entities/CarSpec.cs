using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ecommerce.Core.Entities.Base;
namespace ecommerce.Core.Entities;
public class CarSpec : AuditableEntity<int>
{
    
    public int CarBrandId { get; set; }

    [ForeignKey(nameof(CarBrandId))]
    public CarBrand Brand { get; set; }


    public int ModelId { get; set; }

    [ForeignKey(nameof(ModelId))]
    public CarModel Model { get; set; }

    public int? CarEngineId { get; set; }
    [ForeignKey(nameof(CarEngineId))]
    public CarEngine Engine { get; set; }

    public int? CarFuelId { get; set; }
    [ForeignKey(nameof(CarFuelId))]
    public CarFuelType Fuel { get; set; }

    public int? CarGearboxId { get; set; }
    [ForeignKey(nameof(CarGearboxId))]
    public CarGearbox Gearbox { get; set; }

    public int? CarYearId { get; set; }
    [ForeignKey(nameof(CarYearId))]
    public CarYear Year { get; set; }

    public int? CarVinId { get; set; }
    [ForeignKey(nameof(CarVinId))]
    public CarVin Vin { get; set; }

  
    public string OEM { get; set; }

    
    public string SourceUrl { get; set; }

    public ICollection<CarSpecOriginalNumber> OriginalNumbers { get; set; } = new List<CarSpecOriginalNumber>();
}
