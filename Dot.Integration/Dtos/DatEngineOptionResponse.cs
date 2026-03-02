using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatEngineOptionResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatEngineOptionBody Body { get; set; } = new();
}

public class DatEngineOptionBody
{
    [XmlElement("getEngineOptionsResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatEngineOptionGetResponse GetEngineOptionsResponse { get; set; } = new();
}

public class DatEngineOptionGetResponse
{
    [XmlElement("engineOption", Namespace = "")]
    public List<DatEngineOption> EngineOptions { get; set; } = new();
}

public class DatEngineOption
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}

public class DatEngineOptionReturn
{
    [XmlElement("engineOptions")]
    public DatEngineOptions EngineOptions { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatEngineOptions
{
    [XmlElement("engineOption")]
    public List<DatEngineOption> EngineOption { get; set; } = new();
}

