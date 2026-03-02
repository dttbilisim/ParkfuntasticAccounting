using Dot.Integration.Abstract;
using Microsoft.Extensions.Logging;

namespace Dot.Integration.Services;

public class DatVehicleSyncService
{
    public readonly IDatService _datService;
    public readonly DatDataService _dataService;
    private readonly ILogger<DatVehicleSyncService> _logger;

    public DatVehicleSyncService(
        IDatService datService, 
        DatDataService dataService, 
        ILogger<DatVehicleSyncService> logger)
    {
        _datService = datService;
        _dataService = dataService;
        _logger = logger;
    }

    public async Task SyncCompleteVehicleDataAsync(
        int vehicleType, 
        string? constructionTimeFrom = null, 
        string? constructionTimeTo = null)
    {
        try
        {
            _logger.LogInformation("Araç tipi {VehicleType} için tam senkronizasyon başlatılıyor", vehicleType);

            // 1. Bu araç tipi için üreticileri al ve kaydet
            _logger.LogInformation("Araç tipi {VehicleType} için üreticiler getiriliyor", vehicleType);
            var manufacturers = await _datService.GetManufacturersAsync(vehicleType, constructionTimeFrom, constructionTimeTo);
            await _dataService.SaveManufacturersAsync(manufacturers.Manufacturers.Manufacturer, vehicleType);
            _logger.LogInformation("{Count} üretici kaydedildi", manufacturers.Manufacturers.Manufacturer.Count);

            // 2. Her üretici için ana modelleri al
            foreach (var manufacturer in manufacturers.Manufacturers.Manufacturer)
            {
                _logger.LogInformation("Üretici işleniyor: {Manufacturer}", manufacturer.Value);
                
                var baseModels = await _datService.GetBaseModelsNAsync(vehicleType, manufacturer.Key, constructionTimeFrom, constructionTimeTo);
                await _dataService.SaveBaseModelsAsync(baseModels.BaseModels.BaseModel, vehicleType, manufacturer.Key);
                _logger.LogInformation("{Manufacturer} için {Count} ana model kaydedildi", 
                    manufacturer.Value, baseModels.BaseModels.BaseModel.Count);

                // 3. Her ana model için alt modelleri al
                foreach (var baseModel in baseModels.BaseModels.BaseModel)
                {
                    try
                    {
                        _logger.LogInformation("Ana model işleniyor: {BaseModel}", baseModel.Value);
                        
                        var subModels = await _datService.GetSubModelsAsync(vehicleType, manufacturer.Key, baseModel.Key, constructionTimeFrom, constructionTimeTo);
                        await _dataService.SaveSubModelsAsync(subModels.SubModels.SubModel, vehicleType, manufacturer.Key, baseModel.Key);
                        _logger.LogInformation("{BaseModel} için {Count} alt model kaydedildi", 
                            baseModel.Value, subModels.SubModels.SubModel.Count);

                    // 4. Her alt model için tüm opsiyonları al
                    foreach (var subModel in subModels.SubModels.SubModel)
                    {
                        _logger.LogInformation("Alt model işleniyor: {SubModel}", subModel.Value);
                        
                        // 4a. Sınıflandırma gruplarını al
                        var classificationGroups = await _datService.GetClassificationGroupsAsync(
                            vehicleType, manufacturer.Key, baseModel.Key, subModel.Key);
                        _logger.LogInformation("{SubModel} için {Count} sınıflandırma grubu bulundu", 
                            subModel.Value, classificationGroups.ClassificationGroups.Count);

                        // 4b. Her sınıflandırma için opsiyonları al
                        foreach (var classification in classificationGroups.ClassificationGroups)
                        {
                            try
                            {
                                var options = await _datService.GetOptionsByClassificationAsync(
                                    vehicleType, manufacturer.Key, baseModel.Key, subModel.Key, classification);
                                await _dataService.SaveOptionsAsync(
                                    options.Options.Option, vehicleType, manufacturer.Key, baseModel.Key, subModel.Key, classification);
                                _logger.LogInformation("Sınıflandırma {Classification} için {Count} opsiyon kaydedildi", 
                                    classification, options.Options.Option.Count);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Sınıflandırma {Classification} için opsiyon bulunamadı, atlanıyor...", classification);
                            }
                        }

                        // 4c. Motor opsiyonlarını al
                        try
                        {
                            var engineOptions = await _datService.GetEngineOptionsAsync(
                                vehicleType, manufacturer.Key, baseModel.Key, subModel.Key, constructionTimeFrom, constructionTimeTo);
                            await _dataService.SaveEngineOptionsAsync(
                                engineOptions.EngineOptions.EngineOption, vehicleType, manufacturer.Key, baseModel.Key, subModel.Key);
                            _logger.LogInformation("{SubModel} için {Count} motor opsiyonu kaydedildi", 
                                subModel.Value, engineOptions.EngineOptions.EngineOption.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "{SubModel} için motor opsiyonu bulunamadı, atlanıyor...", subModel.Value);
                        }

                        // 4d. Kasa opsiyonlarını al
                        try
                        {
                            var carBodyOptions = await _datService.GetCarBodyOptionsAsync(
                                vehicleType, manufacturer.Key, baseModel.Key, subModel.Key, constructionTimeFrom, constructionTimeTo);
                            await _dataService.SaveCarBodyOptionsAsync(
                                carBodyOptions.CarBodyOptions.CarBodyOption, vehicleType, manufacturer.Key, baseModel.Key, subModel.Key);
                            _logger.LogInformation("{SubModel} için {Count} kasa opsiyonu kaydedildi", 
                                subModel.Value, carBodyOptions.CarBodyOptions.CarBodyOption.Count);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "{SubModel} için kasa opsiyonu bulunamadı, atlanıyor...", subModel.Value);
                        }
                    }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Ana model {BaseModel} işlenirken hata oluştu, atlanıyor...", baseModel.Value);
                    }
                }
            }

            _logger.LogInformation("Araç tipi {VehicleType} için senkronizasyon başarıyla tamamlandı", vehicleType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Araç tipi {VehicleType} için tam senkronizasyon sırasında hata oluştu", vehicleType);
            throw;
        }
    }

    /// <summary>
    /// Belirli bir manufacturer için tüm bilgileri senkronize eder
    /// </summary>
    public async Task SyncManufacturerDataAsync(
        int vehicleType, 
        string manufacturerKey,
        string? constructionTimeFrom = null, 
        string? constructionTimeTo = null)
    {
        try
        {
            _logger.LogInformation("Araç tipi {VehicleType}, üretici {Manufacturer} için senkronizasyon başlatılıyor", 
                vehicleType, manufacturerKey);

            var baseModels = await _datService.GetBaseModelsNAsync(vehicleType, manufacturerKey, constructionTimeFrom, constructionTimeTo);
            await _dataService.SaveBaseModelsAsync(baseModels.BaseModels.BaseModel, vehicleType, manufacturerKey);

            foreach (var baseModel in baseModels.BaseModels.BaseModel)
            {
                await SyncBaseModelDataAsync(vehicleType, manufacturerKey, baseModel.Key, constructionTimeFrom, constructionTimeTo);
            }

            _logger.LogInformation("Üretici senkronizasyonu başarıyla tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üretici senkronizasyonu sırasında hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir base model için tüm bilgileri senkronize eder
    /// </summary>
    public async Task SyncBaseModelDataAsync(
        int vehicleType, 
        string manufacturerKey, 
        string baseModelKey,
        string? constructionTimeFrom = null, 
        string? constructionTimeTo = null)
    {
        try
        {
            _logger.LogInformation("Araç tipi {VehicleType}, üretici {Manufacturer}, ana model {BaseModel} için senkronizasyon başlatılıyor", 
                vehicleType, manufacturerKey, baseModelKey);

            var subModels = await _datService.GetSubModelsAsync(vehicleType, manufacturerKey, baseModelKey, constructionTimeFrom, constructionTimeTo);
            await _dataService.SaveSubModelsAsync(subModels.SubModels.SubModel, vehicleType, manufacturerKey, baseModelKey);

            foreach (var subModel in subModels.SubModels.SubModel)
            {
                await SyncSubModelDataAsync(vehicleType, manufacturerKey, baseModelKey, subModel.Key, constructionTimeFrom, constructionTimeTo);
            }

            _logger.LogInformation("Ana model senkronizasyonu başarıyla tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ana model senkronizasyonu sırasında hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir sub model için tüm opsiyonları senkronize eder
    /// </summary>
    public async Task SyncSubModelDataAsync(
        int vehicleType, 
        string manufacturerKey, 
        string baseModelKey, 
        string subModelKey,
        string? constructionTimeFrom = null, 
        string? constructionTimeTo = null)
    {
        try
        {
            _logger.LogInformation("Alt model {SubModel} için senkronizasyon başlatılıyor", subModelKey);

            // Sınıflandırma gruplarını al
            var classificationGroups = await _datService.GetClassificationGroupsAsync(
                vehicleType, manufacturerKey, baseModelKey, subModelKey);

            // Her sınıflandırma için opsiyonları al
            foreach (var classification in classificationGroups.ClassificationGroups)
            {
                var options = await _datService.GetOptionsByClassificationAsync(
                    vehicleType, manufacturerKey, baseModelKey, subModelKey, classification);
                await _dataService.SaveOptionsAsync(
                    options.Options.Option, vehicleType, manufacturerKey, baseModelKey, subModelKey, classification);
            }

            // Motor opsiyonlarını al
            var engineOptions = await _datService.GetEngineOptionsAsync(
                vehicleType, manufacturerKey, baseModelKey, subModelKey, constructionTimeFrom, constructionTimeTo);
            await _dataService.SaveEngineOptionsAsync(
                engineOptions.EngineOptions.EngineOption, vehicleType, manufacturerKey, baseModelKey, subModelKey);

            // Kasa opsiyonlarını al
            var carBodyOptions = await _datService.GetCarBodyOptionsAsync(
                vehicleType, manufacturerKey, baseModelKey, subModelKey, constructionTimeFrom, constructionTimeTo);
            await _dataService.SaveCarBodyOptionsAsync(
                carBodyOptions.CarBodyOptions.CarBodyOption, vehicleType, manufacturerKey, baseModelKey, subModelKey);

            _logger.LogInformation("Alt model senkronizasyonu başarıyla tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alt model senkronizasyonu sırasında hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Tüm araç türleri için tam senkronizasyon yapar (DİKKAT: Çok uzun sürebilir!)
    /// </summary>
    public async Task SyncAllVehicleTypesAsync(string? constructionTimeFrom = null, string? constructionTimeTo = null)
    {
        try
        {
            _logger.LogInformation("TÜM araç tipleri için senkronizasyon başlatılıyor");

            // Tüm araç tiplerini al
            var vehicleTypes = await _datService.GetVehicleTypesAsync();
            await _dataService.SaveVehicleTypesAsync(vehicleTypes.VehicleTypes.VehicleType);

            // Her araç tipini senkronize et
            foreach (var vehicleType in vehicleTypes.VehicleTypes.VehicleType)
            {
                if (int.TryParse(vehicleType.Key, out int vtId))
                {
                    await SyncCompleteVehicleDataAsync(vtId, constructionTimeFrom, constructionTimeTo);
                }
            }

            _logger.LogInformation("Tüm araç tipleri için senkronizasyon başarıyla tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tüm araç tipleri senkronizasyonu sırasında hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// DAT E-Code oluşturur ve database'e kaydeder
    /// </summary>
    public async Task<string> CompileAndSaveDatECodeAsync(
        int vehicleType, 
        string manufacturerKey, 
        string baseModelKey, 
        string subModelKey, 
        List<string>? selectedOptions = null)
    {
        try
        {
            var result = await _datService.CompileDatECodeAsync(
                vehicleType, manufacturerKey, baseModelKey, subModelKey, selectedOptions);
            
            await _dataService.SaveCompiledCodeAsync(
                result.DatECode, vehicleType, manufacturerKey, baseModelKey, subModelKey, selectedOptions);

            _logger.LogInformation("DAT E-Code derlendi ve kaydedildi: {DatECode}", result.DatECode);
            return result.DatECode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DAT E-Code derlenirken ve kaydedilirken hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir sub model için yedek parçaları senkronize eder
    /// </summary>
    public async Task SyncPartsForSubModelAsync(
        int vehicleType, 
        string manufacturerKey, 
        string baseModelKey, 
        string subModelKey, 
        List<string>? selectedOptions = null,
        List<string>? datProcessNos = null)
    {
        try
        {
            _logger.LogInformation("Parçalar getiriliyor - VehicleType: {VehicleType}, Manufacturer: {Manufacturer}, BaseModel: {BaseModel}, SubModel: {SubModel}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey);

            var partsResult = await _datService.SearchPartsAsync(
                vehicleType, manufacturerKey, baseModelKey, subModelKey, 
                selectedOptions, datProcessNos);

            if (partsResult.Parts.Any())
            {
                await _dataService.SavePartsAsync(partsResult.Parts);
                _logger.LogInformation("{Count} parça başarıyla kaydedildi", partsResult.Parts.Count);
            }
            else
            {
                _logger.LogWarning("Parça bulunamadı");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parçalar senkronize edilirken hata oluştu");
            throw;
        }
    }

    /// <summary>
    /// Belirli bir base model için tüm parçaları senkronize eder (tüm sub model opsiyonlarıyla)
    /// </summary>
    public async Task SyncAllPartsForBaseModelAsync(
        int vehicleType, 
        string manufacturerKey, 
        string baseModelKey,
        string? constructionTimeFrom = null, 
        string? constructionTimeTo = null)
    {
        try
        {
            _logger.LogInformation("Tüm parçalar getiriliyor - VehicleType: {VehicleType}, Manufacturer: {Manufacturer}, BaseModel: {BaseModel}", 
                vehicleType, manufacturerKey, baseModelKey);

            // Alt modelleri al
            var subModels = await _datService.GetSubModelsAsync(vehicleType, manufacturerKey, baseModelKey, constructionTimeFrom, constructionTimeTo);

            // Her alt model için parçaları senkronize et
            foreach (var subModel in subModels.SubModels.SubModel)
            {
                try
                {
                    _logger.LogInformation("Alt model {SubModel} için parçalar getiriliyor", subModel.Value);
                    await SyncPartsForSubModelAsync(vehicleType, manufacturerKey, baseModelKey, subModel.Key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Alt model {SubModel} için parçalar getirilirken hata oluştu, devam ediliyor...", subModel.Value);
                }
            }

            _logger.LogInformation("Base model için parça senkronizasyonu tamamlandı");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Base model parça senkronizasyonu sırasında hata oluştu");
            throw;
        }
    }
}

