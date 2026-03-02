using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatTokenResponse
{
    [XmlElement("Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public DatTokenBody Body { get; set; } = new();
}

public class DatTokenBody
{
    [XmlElement("generateTokenResponse", Namespace = "http://sphinx.dat.de/services/Authentication")]
    public DatTokenGenerateResponse GenerateTokenResponse { get; set; } = new();
}

public class DatTokenGenerateResponse
{
    [XmlElement("token", Namespace = "")]
    public string Token { get; set; } = string.Empty;
    
    [XmlElement("expires", Namespace = "")]
    public string Expires { get; set; } = string.Empty;
    
    [XmlElement("errorCode", Namespace = "")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage", Namespace = "")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatTokenReturn
{
    public string Token { get; set; } = string.Empty;
    public string Expires { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}
