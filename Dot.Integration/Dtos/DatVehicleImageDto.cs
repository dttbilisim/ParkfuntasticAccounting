using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

// vehicleImagesN wrapper
public class DatVehicleImageResponse
{
    [XmlElement("images")]
    public List<DatVehicleImageItem> ImagesList { get; set; } = new();
}

// Her bir images elementi
public class DatVehicleImageItem
{
    [XmlElement("aspect")]
    public string Aspect { get; set; } = string.Empty;
    
    [XmlElement("imageType")]
    public string ImageType { get; set; } = string.Empty;
    
    [XmlElement("imageFormat")]
    public string ImageFormat { get; set; } = string.Empty;
    
    [XmlElement("imageBase64")]
    public string ImageBase64 { get; set; } = string.Empty;
}

// For return type compatibility
public class DatVehicleImageDto
{
    public string Aspect { get; set; } = string.Empty;
    public string ImageType { get; set; } = string.Empty;
    public string ImageFormat { get; set; } = string.Empty;
    public string ImageBase64 { get; set; } = string.Empty;
}

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatVehicleImageSoapResponse
{
    [XmlElement("Body")]
    public DatVehicleImageSoapBody Body { get; set; } = new();
}

public class DatVehicleImageSoapBody
{
    [XmlElement("getVehicleImagesNResponse", Namespace = "http://sphinx.dat.de/services/VehicleImagery")]
    public DatVehicleImageResponseWrapper GetVehicleImagesNResponse { get; set; } = new();
}

public class DatVehicleImageResponseWrapper
{
    [XmlElement("vehicleImagesN")]
    public DatVehicleImageResponse VehicleImagesN { get; set; } = new();
}

public class DatVehicleImageReturn
{
    public List<DatVehicleImageDto> Images { get; set; } = new();
}

