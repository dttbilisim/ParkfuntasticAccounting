namespace ecommerce.Admin.Services.Dtos.VinDto;

/// <summary>
/// VIN decode işlemi sonuç DTO'su
/// </summary>
public class VinDecodeResultDto
{
    public string VinNumber { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    
    // VIN bileşenleri
    public string? Wmi { get; set; }  // World Manufacturer Identifier (ilk 3 karakter)
    public string? Vds { get; set; }  // Vehicle Descriptor Section (4-9 karakterler)
    
    // PostgreSQL'den gelen üretici bilgisi
    public string? ManufacturerName { get; set; }
    
    // Tüm araçlardan toplanan DatProcessNumber listesi (Elasticsearch araması için)
    public List<string> DatProcessNumbers { get; set; } = new();

    // Eşleşen araçlar
    public List<VinVehicleDto> MatchedVehicles { get; set; } = new();

    // Dış kaynaklardan gelen ek bilgiler (Scraping)
    public string? FactoryCode { get; set; }        // Araç Fabrika Kodu
    public string? ProductionYearScraped { get; set; } // Üretim Yılı (Dış kaynak)
    public string? ProductionPlace { get; set; }     // Üretim Yeri
    public string? FullVinDecoded { get; set; }      // Dış kaynakta görünen tam şasi
}
