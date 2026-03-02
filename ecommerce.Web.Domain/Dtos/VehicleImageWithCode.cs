namespace ecommerce.Web.Domain.Dtos;

public class VehicleImageWithCode
{
    public string ImageBase64 { get; set; } // Deprecated: Use Url instead for better performance
    public string Url { get; set; } // CDN URL for the image
    public string DatECode { get; set; }
    public string VehicleType { get; set; }
    public string ManufacturerKey { get; set; }
    public string BaseModelKey { get; set; }
    public string SubModelKey { get; set; }
}
