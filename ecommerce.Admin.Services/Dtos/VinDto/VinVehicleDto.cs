namespace ecommerce.Admin.Services.Dtos.VinDto;

/// <summary>
/// VIN decode sonucunda dönen araç bilgisi
/// PostgreSQL vin_get_models() fonksiyonundan gelen veri
/// </summary>
public class VinVehicleDto
{
    public string ManufacturerKey { get; set; } = string.Empty;
    public string ManufacturerName { get; set; } = string.Empty;
    public string BaseModelKey { get; set; } = string.Empty;
    public string BaseModelName { get; set; } = string.Empty;
    public int ModelYear { get; set; }
    public string Wmi { get; set; } = string.Empty;
    public string VdsCode { get; set; } = string.Empty;
    
    // Eşleşme yöntemi (Örneğin: UNIVERSAL_V11, FALLBACK_BRAND)
    public string MatchMethod { get; set; } = string.Empty;
    
    // OEM parça listesi
    public List<VinOemPartDto> OemParts { get; set; } = new();
    
    // DatProcessNumber listesi (Elasticsearch eşleştirmesi için)
    public List<string> DatProcessNumbers { get; set; } = new();

    // Dış kaynak eşleştirmesi için ek bilgi
    public string? FactoryCode { get; set; }
    
    // Araç fotoğrafı — DotCompiledCodes → DatECode → DotVehicleImages zinciri ile çekilir
    public string? VehicleImageBase64 { get; set; }
    public string? VehicleImageFormat { get; set; }  // JPG, PNG
    public string? VehicleImageAspect { get; set; }  // SIDEVIEW, ANGULARFRONT
}
