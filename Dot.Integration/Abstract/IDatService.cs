using Dot.Integration.Dtos;

namespace Dot.Integration.Abstract;

public interface IDatService
{
    Task<DatTokenReturn> GetTokenAsync();
    Task<DatVehicleTypeReturn> GetVehicleTypesAsync();
    Task<DatManufacturerReturn> GetManufacturersAsync(int vehicleType, string? constructionTimeFrom = null, string? constructionTimeTo = null);
    Task<DatBaseModelReturn> GetBaseModelsNAsync(int vehicleType, string manufacturerKey, string? constructionTimeFrom = null, string? constructionTimeTo = null, bool withRepairIncomplete = true);
    Task<DatSubModelReturn> GetSubModelsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string? constructionTimeFrom = null, string? constructionTimeTo = null);
    Task<DatECodeReturn> CompileDatECodeAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, List<string>? selectedOptions = null);
    Task<DatClassificationGroupReturn> GetClassificationGroupsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey);
    Task<DatOptionReturn> GetOptionsByClassificationAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, int classification);
    Task<DatEngineOptionReturn> GetEngineOptionsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, string? constructionTimeFrom = null, string? constructionTimeTo = null);
    Task<DatCarBodyOptionReturn> GetCarBodyOptionsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, string? constructionTimeFrom = null, string? constructionTimeTo = null);
    Task<DatVehicleDataVehicle> GetVehicleDataAsync(string datECode, string? container = null, string? constructionTime = null, string restriction = "ALL");
    Task<string?> GetConstructionPeriodsAsync(string datECode, string? container = null);
    Task<Dot.Integration.Dtos.DatConstructionPeriodsInfo?> GetConstructionPeriodsInfoAsync(string datECode, string? container = null);
    Task<DateTime?> ConvertConstructionTimeToDateAsync(string constructionTime);
    Task<DatPartsReturn> SearchPartsAsync(int vehicleType, string manufacturerKey, string baseModelKey, string subModelKey, List<string>? selectedOptions = null, List<string>? datProcessNos = null, string? partNumber = null, string? description = null);
    Task<DatPartsReturn> GetPartDetailsAsync(string partNumber);
    Task<DatVehicleImageReturn> GetVehicleImagesAsync(string datECode, List<string>? aspects = null, string imageType = "PICTURE");
}