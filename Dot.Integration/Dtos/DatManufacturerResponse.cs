using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatManufacturerResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatManufacturerBody Body { get; set; } = new();
}

public class DatManufacturerBody
{
    [XmlElement("getManufacturersResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatManufacturerGetResponse GetManufacturersResponse { get; set; } = new();
}

public class DatManufacturerGetResponse
{
    [XmlElement("manufacturer", Namespace = "")]
    public List<DatManufacturer> Manufacturers { get; set; } = new();
}

public class DatManufacturer
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}

public class DatManufacturerReturn
{
    [XmlElement("manufacturers")]
    public DatManufacturers Manufacturers { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatManufacturers
{
    [XmlElement("manufacturer")]
    public List<DatManufacturer> Manufacturer { get; set; } = new();
}

