using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatECodeResponse
{
    [XmlElement("Body")]
    public DatECodeBody Body { get; set; } = new();
}

public class DatECodeBody
{
    [XmlElement("compileDatECodeResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatECodeCompileResponse CompileDatECodeResponse { get; set; } = new();
}

public class DatECodeCompileResponse
{
    [XmlElement("datECode", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public string DatECode { get; set; } = string.Empty;
    
    [XmlElement("datProcessNo", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public List<string>? DatProcessNos { get; set; }
}

public class DatECodeReturn
{
    public string DatECode { get; set; } = string.Empty;
    
    /// <summary>
    /// DAT Process Numbers - parça aramada kullanılır
    /// </summary>
    public List<string>? DatProcessNos { get; set; }
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Ham XML response - hata mesajlarını parse etmek için
    /// </summary>
    public string? RawXmlResponse { get; set; }
}

