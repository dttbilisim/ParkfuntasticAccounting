using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatOptionResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatOptionBody Body { get; set; } = new();
}

public class DatOptionBody
{
    [XmlElement("getOptionsbyClassificationResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatOptionGetResponse GetOptionsbyClassificationResponse { get; set; } = new();
}

public class DatOptionGetResponse
{
    [XmlElement("options", Namespace = "")]
    public List<DatOption> Options { get; set; } = new();
    
    [XmlElement("option", Namespace = "")]
    public List<DatOption> OptionsAlternate { get; set; } = new();
}

public class DatOption
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
}

public class DatOptionReturn
{
    [XmlElement("options")]
    public DatOptions Options { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatOptions
{
    [XmlElement("option")]
    public List<DatOption> Option { get; set; } = new();
}

