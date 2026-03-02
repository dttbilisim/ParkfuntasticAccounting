using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatCarBodyOptionResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatCarBodyOptionBody Body { get; set; } = new();
}

public class DatCarBodyOptionBody
{
    [XmlElement("getCarBodyOptionsResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatCarBodyOptionGetResponse GetCarBodyOptionsResponse { get; set; } = new();
}

public class DatCarBodyOptionGetResponse
{
    [XmlElement("carBodyOption", Namespace = "")]
    public List<DatCarBodyOption> CarBodyOptions { get; set; } = new();
}

public class DatCarBodyOption
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}

public class DatCarBodyOptionReturn
{
    [XmlElement("carBodyOptions")]
    public DatCarBodyOptions CarBodyOptions { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatCarBodyOptions
{
    [XmlElement("carBodyOption")]
    public List<DatCarBodyOption> CarBodyOption { get; set; } = new();
}

