using ecommerce.Core.Entities;
using ecommerce.Core.Utils.ResultSet;
using ecommerce.Web.Domain.Dtos;

namespace ecommerce.Web.Domain.Services.Abstract;

public interface IDotIntegrationService
{
    // 1. Araç Türleri
    Task<IActionResult<List<DotVehicleType>>> GetVehicleTypesAsync();
    
    // 2. Markalar
    Task<IActionResult<List<DotManufacturer>>> GetManufacturersAsync();
    Task<IActionResult<List<DotManufacturer>>> GetManufacturersByVehicleTypeAsync(int vehicleType);
    
    // 3. Seriler (BaseModels)
    Task<IActionResult<List<DotBaseModel>>> GetBaseModelsAsync();
    Task<IActionResult<List<DotBaseModel>>> GetBaseModelsByManufacturerAsync(string manufacturerKey, int vehicleType);
    
    // 4. Modeller (SubModels)
    Task<IActionResult<List<DotSubModel>>> GetSubModelsAsync();
    Task<IActionResult<List<DotSubModel>>> GetSubModelsByBaseModelAsync(string manufacturerKey, string baseModelKey, int vehicleType);
    
    // 5. Kasa Tipleri (CarBodyOptions)
    Task<IActionResult<List<DotCarBodyOption>>> GetCarBodyOptionsAsync();
    Task<IActionResult<List<DotCarBodyOption>>> GetCarBodyOptionsBySubModelAsync(string manufacturerKey, string baseModelKey, string subModelKey, int vehicleType);
    
    // 6. Motor Seçenekleri
    Task<IActionResult<List<DotEngineOption>>> GetEngineOptionsAsync();
    Task<IActionResult<List<DotEngineOption>>> GetEngineOptionsBySubModelAsync(string manufacturerKey, string baseModelKey, string subModelKey, int vehicleType);
    
    // 7. Ek Özellikler (Options)
    Task<IActionResult<List<DotOption>>> GetOptionsAsync();
    Task<IActionResult<List<DotOption>>> GetOptionsBySubModelAsync(string manufacturerKey, string baseModelKey, string subModelKey, int vehicleType);
    
    // 8. Araç Fotoğrafları
    Task<IActionResult<List<DotVehicleImage>>> GetVehicleImagesAsync();
    Task<IActionResult<List<VehicleImageWithCode>>> GetVehicleImagesByCodesAsync(string vehicleType, string manufacturerKey, string baseModelKey, string subModelKey);
    Task<IActionResult<List<DotVehicleImage>>> GetVehicleImagesByDatECodeAsync(string datECode);
    Task<IActionResult<List<DotVehicleImage>>> GetVehicleImagesByVehicleInfoAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey);
    
    // 9. Derlenmiş Kodlar
    Task<IActionResult<List<DotCompiledCode>>> GetCompiledCodesAsync();
    Task<IActionResult<DotCompiledCode?>> GetCompiledCodeByDatECodeAsync(string datECode);
    
    // 10. Tam Araç Seçimi için yardımcı metodlar
    Task<IActionResult<string?>> GetVehicleImageBase64Async(string datECode, string aspect = "SIDEVIEW");
}
