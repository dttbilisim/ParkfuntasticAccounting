using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatImageResponseEnvelope
{
    [XmlElement(ElementName = "Body")]
    public DatImageResponseBody Body { get; set; } = new();
}

public class DatImageResponseBody
{
    [XmlElement(ElementName = "getImagesResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatImageResponse? GetImagesResponse { get; set; }
}

public class DatImageResponse
{
    [XmlElement(ElementName = "images", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatImages? Images { get; set; }
}

public class DatImages
{
    [XmlElement(ElementName = "image", Namespace = "http://www.dat.de/vxs")]
    public List<DatImage> Image { get; set; } = new();
}

public class DatImage
{
    [XmlElement(ElementName = "imageType", Namespace = "http://www.dat.de/vxs")]
    public string? ImageType { get; set; }
    
    [XmlElement(ElementName = "imageFormat", Namespace = "http://www.dat.de/vxs")]
    public string? ImageFormat { get; set; }
    
    [XmlElement(ElementName = "imageBase64", Namespace = "http://www.dat.de/vxs")]
    public string? ImageBase64 { get; set; }
    
    [XmlElement(ElementName = "aspect", Namespace = "http://www.dat.de/vxs")]
    public string? Aspect { get; set; }
}

public class DatImageReturn
{
    public DatImages? Images { get; set; }
    public string RawXmlResponse { get; set; } = string.Empty;
}

