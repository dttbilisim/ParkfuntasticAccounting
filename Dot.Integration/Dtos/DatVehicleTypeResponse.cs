using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatVehicleTypeResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatVehicleTypeBody Body { get; set; } = new();
}

public class DatVehicleTypeBody
{
    [XmlElement("getVehicleTypesResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatVehicleTypeGetResponse GetVehicleTypesResponse { get; set; } = new();
}

public class DatVehicleTypeGetResponse
{
    [XmlElement("vehicleType", Namespace = "")]
    public List<DatVehicleType> VehicleTypes { get; set; } = new();
}

public class DatVehicleType
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}

public class DatVehicleTypeReturn
{
    [XmlElement("vehicleTypes")]
    public DatVehicleTypes VehicleTypes { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatVehicleTypes
{
    [XmlElement("vehicleType")]
    public List<DatVehicleType> VehicleType { get; set; } = new();
}
