using System.Xml.Serialization;

namespace Dot.Integration.Dtos;

[XmlRoot("Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
public class DatVehicleDetailsResponse
{
    [XmlElement("Body")]
    public DatVehicleDetailsBody Body { get; set; } = new();
}

public class DatVehicleDetailsBody
{
    [XmlElement("getVehicleDetailsResponse", Namespace = "http://sphinx.dat.de/services/VehicleSelectionService")]
    public DatVehicleDetailsGetResponse GetVehicleDetailsResponse { get; set; } = new();
}

public class DatVehicleDetailsGetResponse
{
    [XmlElement("return")]
    public DatVehicleDetailsReturn Return { get; set; } = new();
}

public class DatVehicleDetailsReturn
{
    [XmlElement("vehicles")]
    public DatVehicles Vehicles { get; set; } = new();
    
    [XmlElement("errorCode")]
    public string ErrorCode { get; set; } = string.Empty;
    
    [XmlElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;
}

public class DatVehicles
{
    [XmlElement("vehicle")]
    public List<DatVehicle> Vehicle { get; set; } = new();
}

public class DatVehicle
{
    [XmlElement("id")]
    public string Id { get; set; } = string.Empty;
    
    [XmlElement("make")]
    public string Make { get; set; } = string.Empty;
    
    [XmlElement("model")]
    public string Model { get; set; } = string.Empty;
    
    [XmlElement("year")]
    public string Year { get; set; } = string.Empty;
    
    [XmlElement("engine")]
    public string Engine { get; set; } = string.Empty;
    
    [XmlElement("fuelType")]
    public string FuelType { get; set; } = string.Empty;
}
