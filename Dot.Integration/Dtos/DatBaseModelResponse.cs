using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatBaseModelResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatBaseModelBody Body { get; set; } = new();
}

public class DatBaseModelBody
{
    [XmlElement("getBaseModelsNResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatBaseModelGetResponse GetBaseModelsNResponse { get; set; } = new();
}

public class DatBaseModelGetResponse
{
    [XmlElement("baseModelN", Namespace = "")]
    public List<DatBaseModel> BaseModels { get; set; } = new();
}

public class DatBaseModel
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
    
    [XmlAttribute("alternativeBaseType")]
    public string? AlternativeBaseType { get; set; }
    
    [XmlAttribute("repairIncomplete")]
    public bool RepairIncomplete { get; set; }
}

public class DatBaseModelReturn
{
    [XmlElement("baseModels")]
    public DatBaseModels BaseModels { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatBaseModels
{
    [XmlElement("baseModel")]
    public List<DatBaseModel> BaseModel { get; set; } = new();
}

