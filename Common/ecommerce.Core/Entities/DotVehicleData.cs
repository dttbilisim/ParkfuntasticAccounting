using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Core.Entities;

[Table("DotVehicleData")]
public class DotVehicleData
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string DatECode { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Container { get; set; }

    [StringLength(20)]
    public string? ConstructionTime { get; set; }

    [StringLength(300)]
    public string? ContainerName { get; set; }

    [StringLength(200)]
    public string? VehicleTypeName { get; set; }

    [StringLength(200)]
    public string? ManufacturerName { get; set; }

    [StringLength(200)]
    public string? BaseModelName { get; set; }

    [StringLength(200)]
    public string? SubModelName { get; set; }

    public int? VehicleType { get; set; }
    public int? Manufacturer { get; set; }
    public int? BaseModel { get; set; }
    public int? SubModel { get; set; }

    [StringLength(500)]
    public string? KbaNumbers { get; set; }

    public decimal? OriginalPriceNet { get; set; }
    public decimal? OriginalPriceGross { get; set; }
    public int? OriginalPriceVATRate { get; set; }
    public int? RentalCarClass { get; set; }

    [StringLength(50)]
    public string? StructureType { get; set; }

    [StringLength(200)]
    public string? StructureDescription { get; set; }

    public int? CountOfAxles { get; set; }
    public int? CountOfDrivedAxles { get; set; }
    public int? WheelBase { get; set; }
    public int? Length { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? RoofLoad { get; set; }
    public int? TrailerLoadBraked { get; set; }
    public int? TrailerLoadUnbraked { get; set; }
    public int? VehicleSeats { get; set; }
    public int? VehicleDoors { get; set; }
    public int? CountOfAirbags { get; set; }

    public decimal? Acceleration { get; set; }
    public int? SpeedMax { get; set; }
    public int? PowerHp { get; set; }
    public int? PowerKw { get; set; }
    public int? Capacity { get; set; }
    public int? Cylinder { get; set; }

    [StringLength(10)]
    public string? CylinderArrangement { get; set; }

    public int? RotationsOnMaxPower { get; set; }
    public int? RotationsOnMaxTorque { get; set; }
    public int? Torque { get; set; }

    [StringLength(50)]
    public string? GearboxType { get; set; }

    public int? NrOfGears { get; set; }

    [StringLength(50)]
    public string? OriginalTireSizeAxle1 { get; set; }

    [StringLength(50)]
    public string? OriginalTireSizeAxle2 { get; set; }

    public int? TankVolume { get; set; }
    public decimal? ConsumptionInTown { get; set; }
    public decimal? ConsumptionOutOfTown { get; set; }
    public decimal? Consumption { get; set; }
    public int? Co2Emission { get; set; }

    [StringLength(50)]
    public string? EmissionClass { get; set; }

    [StringLength(100)]
    public string? Drive { get; set; }

    [StringLength(10)]
    public string? DriveCode { get; set; }

    public int? EngineCycle { get; set; }

    [StringLength(50)]
    public string? FuelMethod { get; set; }

    [StringLength(10)]
    public string? FuelMethodCode { get; set; }

    [StringLength(50)]
    public string? FuelMethodType { get; set; }

    public int? UnloadedWeight { get; set; }
    public int? PermissableTotalWeight { get; set; }
    public int? LoadingSpace { get; set; }
    public int? LoadingSpaceMax { get; set; }

    public int? InsuranceTypeClassLiability { get; set; }
    public int? InsuranceTypeClassCascoPartial { get; set; }
    public int? InsuranceTypeClassCascoComplete { get; set; }

    [StringLength(100)]
    public string? ProductGroupName { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncDate { get; set; }
    public bool IsActive { get; set; } = true;
}


