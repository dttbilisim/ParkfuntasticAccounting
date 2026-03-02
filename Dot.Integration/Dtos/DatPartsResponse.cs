using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatPartsResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatPartsBody Body { get; set; } = new();
}

public class DatPartsBody
{
    [XmlElement("getSparePartsDetailsForDPNResponse", Namespace = "http://www.dat.eu/myClaim/soap/v2/VehicleRepairService")]
    public DatSparePartsDpnResponse? GetSparePartsDetailsForDPNResponse { get; set; }
    
    // Alternatif namespace (eski API)
    [XmlElement("getSparePartsDetailsForDPNResponse", Namespace = "http://sphinx.dat.de/services/VehicleRepairService")]
    public DatSparePartsDpnResponse? GetSparePartsDetailsForDPNResponseAlternate { get; set; }
}

public class DatSparePartsDpnResponse
{
    [XmlElement("SparePartsDetailsForDPNResponse", Namespace = "")]
    public SparePartsDetailsForDPNResponse SparePartsDetailsForDPNResponse { get; set; } = new();
}

public class SparePartsDetailsForDPNResponse
{
    [XmlElement("sparePartsResultPerDPN", Namespace = "")]
    public List<SparePartsResultPerDPN> Results { get; set; } = new();
}

public class SparePartsResultPerDPN
{
    [XmlElement("datProcessNumber", Namespace = "")]
    public string DatProcessNumber { get; set; } = string.Empty;

    [XmlElement("sparePartsInformations", Namespace = "")]
    public SparePartsInformations SparePartsInformations { get; set; } = new();
    
    [XmlElement("sparePartsVehicles", Namespace = "")]
    public SparePartsVehicles? SparePartsVehicles { get; set; }

    [XmlElement("vehiclesFound", Namespace = "")]
    public int VehiclesFound { get; set; }

    [XmlElement("vehiclesReturned", Namespace = "")]
    public int VehiclesReturned { get; set; }
}

public class SparePartsInformations
{
    [XmlElement("sparePartsInformation", Namespace = "")]
    public List<SparePartsInformation> Items { get; set; } = new();
}

public class SparePartsInformation
{
    [XmlElement("name", Namespace = "")]
    public string Name { get; set; } = string.Empty;
    
    [XmlElement("partNumber", Namespace = "")]
    public string PartNumber { get; set; } = string.Empty;

    [XmlElement("price", Namespace = "")]
    public decimal Price { get; set; }
    
    [XmlElement("priceDate", Namespace = "")]
    public string? PriceDate { get; set; }
    
    [XmlElement("workTimeMin", Namespace = "")]
    public decimal? WorkTimeMin { get; set; }
    
    [XmlElement("workTimeMax", Namespace = "")]
    public decimal? WorkTimeMax { get; set; }

    [XmlElement("orderable", Namespace = "")]
    public string? Orderable { get; set; }

    [XmlElement("finisNumber", Namespace = "")]
    public string? FinisNumber { get; set; }

    [XmlElement("amount", Namespace = "")]
    public int? Amount { get; set; }

    [XmlElement("possibleNames", Namespace = "")]
    public List<PossibleName> PossibleNames { get; set; } = new();

    [XmlElement("repairSet", Namespace = "")]
    public RepairSet? RepairSet { get; set; }
}

public class RepairSet
{
    [XmlElement("sparePartsInformation", Namespace = "")]
    public List<SparePartsInformation> Items { get; set; } = new();
}

public class PossibleName
{
    [XmlElement("identifier", Namespace = "")]
    public string Identifier { get; set; } = string.Empty;

    [XmlElement("name", Namespace = "")]
    public string Name { get; set; } = string.Empty;
}

public class SparePartsVehicles
{
    [XmlElement("sparePartsVehicle", Namespace = "")]
    public List<SparePartsVehicle> Vehicles { get; set; } = new();
}

public class SparePartsVehicle
{
    [XmlElement("datProcessNumber", Namespace = "")]
    public string DatProcessNumber { get; set; } = string.Empty;
    
    [XmlElement("partNumber", Namespace = "")]
    public string PartNumber { get; set; } = string.Empty;
    
    [XmlElement("vehicleType", Namespace = "")]
    public int VehicleType { get; set; }
    
    [XmlElement("vehicleTypeName", Namespace = "")]
    public string? VehicleTypeName { get; set; }
    
    [XmlElement("manufacturer", Namespace = "")]
    public string Manufacturer { get; set; } = string.Empty;
    
    [XmlElement("manufacturerName", Namespace = "")]
    public string? ManufacturerName { get; set; }
    
    [XmlElement("baseModel", Namespace = "")]
    public string BaseModel { get; set; } = string.Empty;
    
    [XmlElement("baseModelName", Namespace = "")]
    public string? BaseModelName { get; set; }
    
    [XmlElement("descriptionIdentifier", Namespace = "")]
    public string? DescriptionIdentifier { get; set; }
    
    [XmlElement("sparePartsSubModels", Namespace = "")]
    public SparePartsSubModels? SparePartsSubModels { get; set; }
}

public class SparePartsSubModels
{
    [XmlElement("sparePartsSubModel", Namespace = "")]
    public List<SparePartsSubModel> SubModels { get; set; } = new();
}

public class SparePartsSubModel
{
    [XmlElement("subModel", Namespace = "")]
    public string SubModel { get; set; } = string.Empty;
    
    [XmlElement("subModelName", Namespace = "")]
    public string? SubModelName { get; set; }
}

// Unified return type used by services
public class DatPartsReturn
{
    public List<DatPartSimple> Parts { get; set; } = new();
}

public class DatPartSimple
{
    public string PartNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Name { get; set; }
    public decimal? NetPrice { get; set; }
    public string? Availability { get; set; }
    public DateTime? PriceDate { get; set; }
    public decimal? WorkTimeMin { get; set; }
    public decimal? WorkTimeMax { get; set; }
    public string? DatProcessNumber { get; set; }
    public int? VehicleType { get; set; }
    public string? VehicleTypeName { get; set; }
    public string? ManufacturerKey { get; set; }
    public string? ManufacturerName { get; set; }
    public string? BaseModelKey { get; set; }
    public string? BaseModelName { get; set; }
    public string? DescriptionIdentifier { get; set; }
    public string? SubModelsJson { get; set; }
    public string? PreviousPricesJson { get; set; }
    public string? PreviousPartNumbersJson { get; set; }
}
