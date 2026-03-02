using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatClassificationGroupResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatClassificationGroupBody Body { get; set; } = new();
}

public class DatClassificationGroupBody
{
    [XmlElement("getClassificationGroupsResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatClassificationGroupGetResponse GetClassificationGroupsResponse { get; set; } = new();
}

public class DatClassificationGroupGetResponse
{
    [XmlElement("classificationGroup", Namespace = "")]
    public List<int> ClassificationGroups { get; set; } = new();
}

public class DatClassificationGroupReturn
{
    public List<int> ClassificationGroups { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

