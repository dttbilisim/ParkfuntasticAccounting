using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatSubModelResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatSubModelBody Body { get; set; } = new();
}

public class DatSubModelBody
{
    [XmlElement("getSubModelsResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatSubModelGetResponse GetSubModelsResponse { get; set; } = new();
}

public class DatSubModelGetResponse
{
    [XmlElement("subModel", Namespace = "")]
    public List<DatSubModel> SubModels { get; set; } = new();
}

public class DatSubModel
{
    [XmlAttribute("key")]
    public string Key { get; set; } = string.Empty;
    
    [XmlAttribute("value")]
    public string Value { get; set; } = string.Empty;
    
    [XmlAttribute("constructionTimeFrom")]
    public string? ConstructionTimeFrom { get; set; }
    
    [XmlAttribute("constructionTimeTo")]
    public string? ConstructionTimeTo { get; set; }
}

public class DatSubModelReturn
{
    [XmlElement("subModels")]
    public DatSubModels SubModels { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatSubModels
{
    [XmlElement("subModel")]
    public List<DatSubModel> SubModel { get; set; } = new();
}

