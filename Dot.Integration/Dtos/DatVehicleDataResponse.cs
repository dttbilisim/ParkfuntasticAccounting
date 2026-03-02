using System.Xml.Serialization;
using System.Collections.Generic;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatVehicleDataResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatVehicleDataBody Body { get; set; } = new();
}

public class DatVehicleDataBody
{
    [XmlElement("getVehicleDataResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatVehicleDataGetResponse GetVehicleDataResponse { get; set; } = new();
}

public class DatVehicleDataGetResponse
{
    [XmlElement("VXS", Namespace = "")]
    public DatVehicleDataVxs Vxs { get; set; } = new();
}

public class DatVehicleDataVxs
{
    [XmlElement(ElementName = "Country", Namespace = "http://www.dat.de/vxs")]
    public string? Country { get; set; }

    [XmlElement(ElementName = "Language", Namespace = "http://www.dat.de/vxs")]
    public string? Language { get; set; }

    [XmlElement(ElementName = "Vehicle", Namespace = "http://www.dat.de/vxs")]
    public DatVehicleDataVehicle Vehicle { get; set; } = new();
}

public class DatVehicleDataVehicle
{
    [XmlElement(ElementName = "DatECode", Namespace = "http://www.dat.de/vxs")]
    public string DatECode { get; set; } = string.Empty;

    [XmlElement(ElementName = "Container", Namespace = "http://www.dat.de/vxs")]
    public string? Container { get; set; }

    [XmlElement(ElementName = "ConstructionTime", Namespace = "http://www.dat.de/vxs")]
    public string? ConstructionTime { get; set; }

    [XmlElement(ElementName = "ContainerName", Namespace = "http://www.dat.de/vxs")]
    public string? ContainerName { get; set; }

    [XmlElement(ElementName = "ContainerNameN", Namespace = "http://www.dat.de/vxs")]
    public string? ContainerNameN { get; set; }

    [XmlElement(ElementName = "SalesDescription", Namespace = "http://www.dat.de/vxs")]
    public string? SalesDescription { get; set; }

    [XmlElement(ElementName = "VehicleTypeName", Namespace = "http://www.dat.de/vxs")]
    public string? VehicleTypeName { get; set; }

    [XmlElement(ElementName = "VehicleTypeNameN", Namespace = "http://www.dat.de/vxs")]
    public string? VehicleTypeNameN { get; set; }

    [XmlElement(ElementName = "ManufacturerName", Namespace = "http://www.dat.de/vxs")]
    public string? ManufacturerName { get; set; }

    [XmlElement(ElementName = "BaseModelName", Namespace = "http://www.dat.de/vxs")]
    public string? BaseModelName { get; set; }

    [XmlElement(ElementName = "SubModelName", Namespace = "http://www.dat.de/vxs")]
    public string? SubModelName { get; set; }

    [XmlElement(ElementName = "VehicleType", Namespace = "http://www.dat.de/vxs")]
    public int VehicleType { get; set; }

    [XmlElement(ElementName = "Manufacturer", Namespace = "http://www.dat.de/vxs")]
    public int Manufacturer { get; set; }

    [XmlElement(ElementName = "BaseModel", Namespace = "http://www.dat.de/vxs")]
    public int BaseModel { get; set; }

    [XmlElement(ElementName = "SubModel", Namespace = "http://www.dat.de/vxs")]
    public int SubModel { get; set; }

    [XmlElement(ElementName = "SubModelVariant", Namespace = "http://www.dat.de/vxs")]
    public int SubModelVariant { get; set; }

    [XmlElement(ElementName = "ReleaseIndicator", Namespace = "http://www.dat.de/vxs")]
    public string? ReleaseIndicator { get; set; }

    [XmlElement(ElementName = "kbaNumbers", Namespace = "")]
    public string? KbaNumbers { get; set; }

    [XmlElement(ElementName = "RentalCarClass", Namespace = "http://www.dat.de/vxs")]
    public int RentalCarClass { get; set; }

    [XmlElement(ElementName = "OriginalPriceNet", Namespace = "http://www.dat.de/vxs")]
    public decimal OriginalPriceNet { get; set; }

    [XmlElement(ElementName = "OriginalPriceGross", Namespace = "http://www.dat.de/vxs")]
    public decimal OriginalPriceGross { get; set; }

    [XmlElement(ElementName = "OriginalPriceInfo", Namespace = "http://www.dat.de/vxs")]
    public DatOriginalPriceInfo? OriginalPriceInfo { get; set; }

    [XmlElement(ElementName = "TechInfo", Namespace = "http://www.dat.de/vxs")]
    public DatTechInfo? TechInfo { get; set; }

    [XmlElement(ElementName = "PowerHp", Namespace = "http://www.dat.de/vxs")]
    public int PowerHp { get; set; }

    [XmlElement(ElementName = "PowerKw", Namespace = "http://www.dat.de/vxs")]
    public int PowerKw { get; set; }

    [XmlElement(ElementName = "Capacity", Namespace = "http://www.dat.de/vxs")]
    public int Capacity { get; set; }

    [XmlElement(ElementName = "FuelMethod", Namespace = "http://www.dat.de/vxs")]
    public string? FuelMethod { get; set; }

    [XmlElement(ElementName = "GearboxType", Namespace = "http://www.dat.de/vxs")]
    public string? GearboxType { get; set; }

    [XmlElement(ElementName = "Length", Namespace = "http://www.dat.de/vxs")]
    public int Length { get; set; }

    [XmlElement(ElementName = "Width", Namespace = "http://www.dat.de/vxs")]
    public int Width { get; set; }

    [XmlElement(ElementName = "Height", Namespace = "http://www.dat.de/vxs")]
    public int Height { get; set; }
}

public class DatOriginalPriceInfo
{
    [XmlElement(ElementName = "OriginalPriceNet", Namespace = "http://www.dat.de/vxs")]
    public decimal OriginalPriceNet { get; set; }

    [XmlElement(ElementName = "OriginalPriceVATRate", Namespace = "http://www.dat.de/vxs")]
    public int OriginalPriceVATRate { get; set; }

    [XmlElement(ElementName = "OriginalPriceGross", Namespace = "http://www.dat.de/vxs")]
    public decimal OriginalPriceGross { get; set; }
}

public class DatTechInfo
{
    [XmlElement(ElementName = "StructureType", Namespace = "http://www.dat.de/vxs")]
    public string? StructureType { get; set; }

    [XmlElement(ElementName = "StructureDescription", Namespace = "http://www.dat.de/vxs")]
    public string? StructureDescription { get; set; }

    [XmlElement(ElementName = "CountOfAxles", Namespace = "http://www.dat.de/vxs")]
    public int CountOfAxles { get; set; }

    [XmlElement(ElementName = "CountOfDrivedAxles", Namespace = "http://www.dat.de/vxs")]
    public int CountOfDrivedAxles { get; set; }

    [XmlElement(ElementName = "WheelBase", Namespace = "http://www.dat.de/vxs")]
    public int WheelBase { get; set; }

    [XmlElement(ElementName = "Length", Namespace = "http://www.dat.de/vxs")]
    public int Length { get; set; }

    [XmlElement(ElementName = "Width", Namespace = "http://www.dat.de/vxs")]
    public int Width { get; set; }

    [XmlElement(ElementName = "Height", Namespace = "http://www.dat.de/vxs")]
    public int Height { get; set; }

    [XmlElement(ElementName = "RoofLoad", Namespace = "http://www.dat.de/vxs")]
    public int RoofLoad { get; set; }

    [XmlElement(ElementName = "TrailerLoadBraked", Namespace = "http://www.dat.de/vxs")]
    public int TrailerLoadBraked { get; set; }

    [XmlElement(ElementName = "TrailerLoadUnbraked", Namespace = "http://www.dat.de/vxs")]
    public int TrailerLoadUnbraked { get; set; }

    [XmlElement(ElementName = "VehicleSeats", Namespace = "http://www.dat.de/vxs")]
    public int VehicleSeats { get; set; }

    [XmlElement(ElementName = "VehicleDoors", Namespace = "http://www.dat.de/vxs")]
    public int VehicleDoors { get; set; }

    [XmlElement(ElementName = "CountOfAirbags", Namespace = "http://www.dat.de/vxs")]
    public int CountOfAirbags { get; set; }

    [XmlElement(ElementName = "Acceleration", Namespace = "http://www.dat.de/vxs")]
    public decimal Acceleration { get; set; }

    [XmlElement(ElementName = "SpeedMax", Namespace = "http://www.dat.de/vxs")]
    public int SpeedMax { get; set; }

    [XmlElement(ElementName = "PowerHp", Namespace = "http://www.dat.de/vxs")]
    public int PowerHp { get; set; }

    [XmlElement(ElementName = "PowerKw", Namespace = "http://www.dat.de/vxs")]
    public int PowerKw { get; set; }

    [XmlElement(ElementName = "Capacity", Namespace = "http://www.dat.de/vxs")]
    public int Capacity { get; set; }

    [XmlElement(ElementName = "Cylinder", Namespace = "http://www.dat.de/vxs")]
    public int Cylinder { get; set; }

    [XmlElement(ElementName = "CylinderArrangement", Namespace = "http://www.dat.de/vxs")]
    public string? CylinderArrangement { get; set; }

    [XmlElement(ElementName = "RotationsOnMaxPower", Namespace = "http://www.dat.de/vxs")]
    public int RotationsOnMaxPower { get; set; }

    [XmlElement(ElementName = "RotationsOnMaxTorque", Namespace = "http://www.dat.de/vxs")]
    public int RotationsOnMaxTorque { get; set; }

    [XmlElement(ElementName = "Torque", Namespace = "http://www.dat.de/vxs")]
    public int Torque { get; set; }

    [XmlElement(ElementName = "GearboxType", Namespace = "http://www.dat.de/vxs")]
    public string? GearboxType { get; set; }

    [XmlElement(ElementName = "NrOfGears", Namespace = "http://www.dat.de/vxs")]
    public int NrOfGears { get; set; }

    [XmlElement(ElementName = "OriginalTireSizeAxle1", Namespace = "http://www.dat.de/vxs")]
    public string? OriginalTireSizeAxle1 { get; set; }

    [XmlElement(ElementName = "OriginalTireSizeAxle2", Namespace = "http://www.dat.de/vxs")]
    public string? OriginalTireSizeAxle2 { get; set; }

    [XmlElement(ElementName = "TankVolume", Namespace = "http://www.dat.de/vxs")]
    public int TankVolume { get; set; }

    [XmlElement(ElementName = "ConsumptionInTown", Namespace = "http://www.dat.de/vxs")]
    public decimal ConsumptionInTown { get; set; }

    [XmlElement(ElementName = "ConsumptionOutOfTown", Namespace = "http://www.dat.de/vxs")]
    public decimal ConsumptionOutOfTown { get; set; }

    [XmlElement(ElementName = "Consumption", Namespace = "http://www.dat.de/vxs")]
    public decimal Consumption { get; set; }

    [XmlElement(ElementName = "Co2Emission", Namespace = "http://www.dat.de/vxs")]
    public int Co2Emission { get; set; }

    [XmlElement(ElementName = "EmissionClass", Namespace = "http://www.dat.de/vxs")]
    public string? EmissionClass { get; set; }

    [XmlElement(ElementName = "Drive", Namespace = "http://www.dat.de/vxs")]
    public string? Drive { get; set; }

    [XmlElement(ElementName = "DriveCode", Namespace = "http://www.dat.de/vxs")]
    public string? DriveCode { get; set; }

    [XmlElement(ElementName = "EngineCycle", Namespace = "http://www.dat.de/vxs")]
    public int EngineCycle { get; set; }

    [XmlElement(ElementName = "FuelMethod", Namespace = "http://www.dat.de/vxs")]
    public string? FuelMethod { get; set; }

    [XmlElement(ElementName = "FuelMethodCode", Namespace = "http://www.dat.de/vxs")]
    public string? FuelMethodCode { get; set; }

    [XmlElement(ElementName = "FuelMethodType", Namespace = "http://www.dat.de/vxs")]
    public string? FuelMethodType { get; set; }

    [XmlElement(ElementName = "UnloadedWeight", Namespace = "http://www.dat.de/vxs")]
    public int UnloadedWeight { get; set; }

    [XmlElement(ElementName = "PermissableTotalWeight", Namespace = "http://www.dat.de/vxs")]
    public int PermissableTotalWeight { get; set; }

    [XmlElement(ElementName = "LoadingSpace", Namespace = "http://www.dat.de/vxs")]
    public int LoadingSpace { get; set; }

    [XmlElement(ElementName = "LoadingSpaceMax", Namespace = "http://www.dat.de/vxs")]
    public int LoadingSpaceMax { get; set; }

    [XmlElement(ElementName = "InsuranceTypeClassLiability", Namespace = "http://www.dat.de/vxs")]
    public int InsuranceTypeClassLiability { get; set; }

    [XmlElement(ElementName = "InsuranceTypeClassCascoPartial", Namespace = "http://www.dat.de/vxs")]
    public int InsuranceTypeClassCascoPartial { get; set; }

    [XmlElement(ElementName = "InsuranceTypeClassCascoComplete", Namespace = "http://www.dat.de/vxs")]
    public int InsuranceTypeClassCascoComplete { get; set; }

    [XmlElement(ElementName = "ProductGroupName", Namespace = "http://www.dat.de/vxs")]
    public string? ProductGroupName { get; set; }

    [XmlElement(ElementName = "ProductGroupCode", Namespace = "http://www.dat.de/vxs")]
    public string? ProductGroupCode { get; set; }
}


