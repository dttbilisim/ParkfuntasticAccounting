using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Dot.Integration.Abstract;
using Dot.Integration.Dtos;
namespace Dot.Integration.Services;

/// <summary>
/// DAT API ile bulk senkronizasyon işlemlerini yönetir
/// Paralel API çağrıları ve batch işlemler ile performansı optimize eder
/// </summary>
public class DatBulkSyncService : IDisposable
{
    private readonly IDatService _datService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DatBulkSyncService> _logger;
    private readonly SemaphoreSlim _apiSemaphore;
    private readonly SemaphoreSlim _dbSemaphore = new(1, 1);
    private readonly SemaphoreSlim _optionsFetchSemaphore = new(5, 5); // Limit concurrent options/classification fetching (reduced for stability)
    private readonly int _maxConcurrency = 2; // Max concurrent API calls (Apple Silicon stability)

    public DatBulkSyncService(
        IDatService datService, 
        IServiceScopeFactory scopeFactory,
        ILogger<DatBulkSyncService> logger)
    {
        _datService = datService;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _apiSemaphore = new SemaphoreSlim(_maxConcurrency, _maxConcurrency);
    }

    /// <summary>
    /// Tüm vehicle type'ları bulk olarak senkronize eder
    /// </summary>
    public async Task<BulkSyncResult> BulkSyncAllVehicleTypesAsync(
        CancellationToken cancellationToken = default,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("🚀 Bulk senkronizasyon başlatılıyor...");

            // 1. Vehicle Types'ları al
            var vehicleTypes = await _datService.GetVehicleTypesAsync();
            result.VehicleTypesCount = vehicleTypes.VehicleTypes.VehicleType.Count;
            _logger.LogInformation($"🔄 VehicleTypes fetched: {result.VehicleTypesCount}");
            progress?.Report(result);

            // 2. Her vehicle type için paralel işlem
            var vehicleTypeTasks = vehicleTypes.VehicleTypes.VehicleType.Select(vehicleType =>
                ProcessVehicleTypeBulkAsync(int.Parse(vehicleType.Key), cancellationToken, progress));

            BulkSyncResult[] vehicleTypeResults = await Task.WhenAll(vehicleTypeTasks);

            // 3. Sonuçları birleştir ve progress güncelle
            foreach (var vehicleTypeResult in vehicleTypeResults)
            {
                result.Add(vehicleTypeResult);
                // Her vehicle type tamamlandığında progress güncelle
                _logger.LogInformation($"🔄 Vehicle Type tamamlandı - Toplam Manufacturers: {result.ManufacturersCount}");
                progress?.Report(result);
            }

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;

            // Final progress güncelleme
            progress?.Report(result);

            _logger.LogInformation("✅ Bulk senkronizasyon tamamlandı! Süre: {Duration}ms, Toplam: {Total}",
                result.TotalDuration.TotalMilliseconds, result.GetTotalCount());

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Bulk senkronizasyon sırasında hata oluştu");
            result.HasErrors = true;
            return result;
        }
    }

    /// <summary>
    /// Tek bir manufacturer için tüm modelleri bulk olarak senkronize eder
    /// </summary>
    public async Task<BulkSyncResult> BulkSyncSingleManufacturerAsync(
        int vehicleType,
        string manufacturerKey,
        CancellationToken cancellationToken = default,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("🚀 Bulk senkronizasyon başlatılıyor - VT:{VehicleType} M:{Manufacturer}", 
                vehicleType, manufacturerKey);

            // Base modelleri al
            var baseModels = await _datService.GetBaseModelsNAsync(
                vehicleType, 
                manufacturerKey,
                constructionTimeFrom: "0001",
                constructionTimeTo: "9912");
            
            result.BaseModelsCount = baseModels.BaseModels.BaseModel.Count;
            _logger.LogInformation($"🔄 BaseModels fetched: {result.BaseModelsCount}");
            progress?.Report(result);

            // Her base model için paralel işlem (SemaphoreSlim ile kontrollü)
            var tasks = new List<Task<BulkSyncResult>>();
            
            foreach (var baseModel in baseModels.BaseModels.BaseModel)
            {
                await _apiSemaphore.WaitAsync(cancellationToken);
                
                var task = Task.Run(async () =>
                {
                    try
                    {
                        // Progress: şu an işlenen bağlamı bildir
                        progress?.Report(new BulkSyncResult
                        {
                            // UI'da alt başlık için
                            CurrentVehicleTypeName = vehicleType.ToString(),
                            CurrentManufacturerName = manufacturerKey,
                            CurrentBaseModelName = baseModel.Value,
                            CurrentSubModelName = null
                        });
                        return await ProcessBaseModelBulkAsync(
                            vehicleType, 
                            manufacturerKey, 
                            baseModel.Key, 
                            cancellationToken, 
                            progress);
                    }
                    finally
                    {
                        _apiSemaphore.Release();
                    }
                }, cancellationToken);
                
                tasks.Add(task);
            }

            var baseModelResults = await Task.WhenAll(tasks);

            // Sonuçları birleştir
            foreach (var bmResult in baseModelResults)
            {
                result.Add(bmResult);
                progress?.Report(result);
            }

            stopwatch.Stop();
            result.TotalDuration = stopwatch.Elapsed;
            progress?.Report(result);

            _logger.LogInformation("✅ Bulk senkronizasyon tamamlandı! VT:{VT} M:{M} - Süre: {Duration}ms",
                vehicleType, manufacturerKey, result.TotalDuration.TotalMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Bulk senkronizasyon hatası - VT:{VT} M:{M}", vehicleType, manufacturerKey);
            result.HasErrors = true;
            return result;
        }
    }

    /// <summary>
    /// Tek bir base model için tüm sub modelleri işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessBaseModelBulkAsync(
        int vehicleType,
        string manufacturerKey,
        string baseModelKey,
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();

        try
        {
            // Sub modelleri al
            var subModels = await _datService.GetSubModelsAsync(
                vehicleType, 
                manufacturerKey, 
                baseModelKey,
                constructionTimeFrom: "0001",
                constructionTimeTo: "9912");

            if (!subModels.SubModels.SubModel.Any())
                return result;

            // Sub modelleri kaydet
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                await dataService.SaveSubModelsAsync(
                    subModels.SubModels.SubModel, 
                    vehicleType, 
                    manufacturerKey, 
                    baseModelKey);
            }

            result.SubModelsCount = subModels.SubModels.SubModel.Count;

            // Her sub model için detaylı işlem (options, parts, images)
            var subModelTasks = subModels.SubModels.SubModel.Select(subModel =>
            {
                // Progress: submodel bağlamını bildir
                progress?.Report(new BulkSyncResult
                {
                    CurrentVehicleTypeName = vehicleType.ToString(),
                    CurrentManufacturerName = manufacturerKey,
                    CurrentBaseModelName = baseModelKey,
                    CurrentSubModelName = subModel.Value
                });

                return ProcessSubModelBulkAsync(
                    vehicleType,
                    manufacturerKey,
                    baseModelKey,
                    subModel.Key,
                    cancellationToken,
                    progress);
            });

            var subModelResults = await Task.WhenAll(subModelTasks);

            // Alt sonuçları topla
            foreach (var smResult in subModelResults)
            {
                result.Add(smResult);
            }

            progress?.Report(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Base model işleme hatası - VT:{VT} M:{M} B:{B}", 
                vehicleType, manufacturerKey, baseModelKey);
            result.HasErrors = true;
            return result;
        }
    }

    /// <summary>
    /// HIZLI TEST: Sadece belirli bir araç için işlem yapar
    /// </summary>
    private async Task<BulkSyncResult> ProcessSingleVehicleTestAsync(
        int vehicleType,
        string manufacturerKey,
        string baseModelKey,
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();
        
        try
        {
            _logger.LogInformation("⚡ TEST: Processing VT:{VT} M:{M} B:{B}", vehicleType, manufacturerKey, baseModelKey);
            
            // 1. Bu manufacturer için base model'i al
            var baseModels = await _datService.GetBaseModelsNAsync(vehicleType, manufacturerKey);
            
            // Sadece belirtilen base model'i bul
            var targetBaseModel = baseModels.BaseModels.BaseModel.FirstOrDefault(b => b.Key == baseModelKey);
            if (targetBaseModel == null)
            {
                _logger.LogError("❌ BaseModel {Key} bulunamadı!", baseModelKey);
                return result;
            }
            
         
            
            // 2. Bu base model için sub models al
            var subModels = await _datService.GetSubModelsAsync(
                vehicleType, manufacturerKey, baseModelKey, 
                null, null); // Construction time'ı null geç
            
            if (subModels?.SubModels?.SubModel == null || !subModels.SubModels.SubModel.Any())
            {
                _logger.LogError("❌ SubModel bulunamadı!");
                return result;
            }
            
          
            
            // 3. İlk 2 sub model'i işle
            // Önce manufacturer object'i oluştur
            var manufacturerObj = new { Key = manufacturerKey, Value = "Test Manufacturer" };
            
            foreach (var subModel in subModels.SubModels.SubModel.Take(2))
            {
                var subModelResult = await ProcessSubModelBulkAsync(
                    vehicleType, 
                    manufacturerObj,   // DOĞRU: Manufacturer object
                    targetBaseModel,   // DOĞRU: BaseModel object
                    subModel, 
                    cancellationToken, 
                    progress);
                
                result.Add(subModelResult);
                progress?.Report(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ TEST HATASI!");
        }
        
        return result;
    }

    /// <summary>
    /// Tek bir vehicle type'ı bulk olarak işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessVehicleTypeBulkAsync(
        int vehicleType, 
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();

        try
        {
            // 1. Manufacturers (tek seferde) - API call için semaphore kullan
            await _apiSemaphore.WaitAsync(cancellationToken);
            DatManufacturerReturn manufacturers;
            try
            {
                manufacturers = await _datService.GetManufacturersAsync(vehicleType);
                await Task.Delay(100, cancellationToken); // Rate limiting
            }
            finally
            {
                _apiSemaphore.Release(); // Hemen release et
            }
            
            // DB write - semaphore dışında
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                await _dbSemaphore.WaitAsync(CancellationToken.None);
                try
                {
                    await dataService.SaveManufacturersAsync(manufacturers.Manufacturers.Manufacturer, vehicleType);
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            }
            
            result.ManufacturersCount = manufacturers.Manufacturers.Manufacturer.Count;
            progress?.Report(result);

            // SIRAYLA İŞLE: Her marka bitmeden diğerine geçme
            _logger.LogInformation("🚀 Processing {Count} manufacturers SEQUENTIALLY for VT {VehicleType}", 
                manufacturers.Manufacturers.Manufacturer.Count, vehicleType);

            foreach (var manufacturer in manufacturers.Manufacturers.Manufacturer)
            {
                var manufacturerResult = await ProcessManufacturerBulkAsync(vehicleType, manufacturer, cancellationToken, progress);
                result.Add(manufacturerResult);
                _logger.LogInformation("✅ Manufacturer completed for VT {VehicleType}", vehicleType);
            }
            
            _logger.LogInformation("🎉 All manufacturers completed for VT {VehicleType}", vehicleType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vehicle type {VehicleType} işlenirken hata oluştu", vehicleType);
            result.HasErrors = true;
        }

        return result;
    }

    /// <summary>
    /// Tek bir manufacturer'ı bulk olarak işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessManufacturerBulkAsync(
        int vehicleType, 
        object manufacturer,
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();

        var manufacturerKey = GetKey(manufacturer);
        
        try
        {
            // 1. Base Models çek - semaphore kullan API call için
            await _apiSemaphore.WaitAsync(cancellationToken);
            
            DatBaseModelReturn baseModels;
            try
            {
                baseModels = await _datService.GetBaseModelsNAsync(vehicleType, manufacturerKey, null, null);
                _logger.LogInformation("✅ BaseModels fetched: {Count} for VT:{VT} M:{M}", 
                    baseModels.BaseModels.BaseModel.Count, vehicleType, manufacturerKey);
                
                // API rate limiting için delay
                await Task.Delay(100, cancellationToken);
            }
            finally
            {
                // API call bitti, semaphore'u hemen serbest bırak
                _apiSemaphore.Release();
            }
            
            // Kendi scope'unda kaydet - semaphore DIŞINDA
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                await _dbSemaphore.WaitAsync(CancellationToken.None);
                try
                {
                    await dataService.SaveBaseModelsAsync(baseModels.BaseModels.BaseModel, vehicleType, manufacturerKey);
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            }
            
            result.BaseModelsCount = baseModels.BaseModels.BaseModel.Count;
            // Base models kaydedildiğinde progress gönder
            progress?.Report(result);
            
            // 2. Her base model için SIRAYLA işlem
            _logger.LogInformation("🚀 Processing {Count} BaseModels SEQUENTIALLY for VT:{VT} M:{M}", 
                baseModels.BaseModels.BaseModel.Count, vehicleType, manufacturerKey);

            foreach (var baseModel in baseModels.BaseModels.BaseModel)
            {
                var baseModelResult = await ProcessBaseModelBulkAsync(vehicleType, manufacturer, baseModel, cancellationToken, progress);
                result.Add(baseModelResult);
                _logger.LogInformation("✅ BaseModel completed for VT:{VT} M:{M}", vehicleType, manufacturerKey);
            }
            
            _logger.LogInformation("🎉 All BaseModels completed for VT:{VT} M:{M}", vehicleType, manufacturerKey);
        }
        catch (Exception ex)
        {
            var manufacturerValue = GetValue(manufacturer);
            _logger.LogWarning(ex, "Manufacturer {Manufacturer} işlenirken hata oluştu", manufacturerValue);
            result.HasErrors = true;
        }

        return result;
    }

    /// <summary>
    /// Tek bir base model'i bulk olarak işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessBaseModelBulkAsync(
        int vehicleType, 
        object manufacturer,
        object baseModel,
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();
        var manufacturerKey = GetKey(manufacturer);
        var baseModelKey = GetKey(baseModel);
        var manufacturerValue = GetValue(manufacturer);
        var baseModelValue = GetValue(baseModel);

        try
        {
            // 1. Sub Models çek - semaphore kullan API call için
            await _apiSemaphore.WaitAsync(cancellationToken);
            
            DatSubModelReturn subModels;
            try
            {
                subModels = await _datService.GetSubModelsAsync(vehicleType, manufacturerKey, baseModelKey, null, null);
                _logger.LogInformation("✅ SubModels fetched: {Count} for VT:{VehicleType} M:{Manufacturer} B:{BaseModel}", 
                    subModels.SubModels.SubModel.Count, vehicleType, manufacturerKey, baseModelKey);
                
                // API rate limiting için delay
                await Task.Delay(100, cancellationToken);
            }
            finally
            {
                // API call bitti, semaphore'u hemen serbest bırak
                _apiSemaphore.Release();
            }
            
            // Kendi scope'unda kaydet - semaphore DIŞINDA
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                await _dbSemaphore.WaitAsync(CancellationToken.None);
                try
                {
                    await dataService.SaveSubModelsAsync(subModels.SubModels.SubModel, vehicleType, manufacturerKey, baseModelKey);
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            }
            
            result.SubModelsCount = subModels.SubModels.SubModel.Count;
            // Sub models kaydedildiğinde progress gönder
            progress?.Report(result);

            // 2. Vehicles kayıtları oluştur (bulk insert)
            var vehicles = subModels.SubModels.SubModel.Select(sm => new
            {
                VehicleType = vehicleType,
                Manufacturer = manufacturerValue,
                BaseModel = baseModelValue,
                SubModel = GetValue(sm)
            }).ToList();

            // Bulk vehicle insert (her vehicle için ayrı scope)
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                await _dbSemaphore.WaitAsync(CancellationToken.None);
                try
                {
                    foreach (var vehicle in vehicles)
                    {
                        await dataService.SaveVehicleAsync(vehicle.VehicleType, vehicle.Manufacturer, vehicle.BaseModel, vehicle.SubModel);
                        result.VehiclesCount++;
                    }
                }
                finally
                {
                    _dbSemaphore.Release();
                }
            }

            // 3. Her sub model için SIRAYLA işlem (opsiyonlar ve parçalar)
            _logger.LogInformation("🚀 Processing {Count} SubModels SEQUENTIALLY (VT:{VehicleType} M:{Manufacturer} B:{BaseModel})", 
                subModels.SubModels.SubModel.Count, vehicleType, manufacturerKey, baseModelKey);

            foreach (var subModel in subModels.SubModels.SubModel)
            {
                var subModelResult = await ProcessSubModelBulkAsync(vehicleType, manufacturer, baseModel, subModel, cancellationToken, progress);
                result.Add(subModelResult);
                _logger.LogInformation("✅ SubModel completed");
            }
            
            _logger.LogInformation("🎉 All SubModels completed for VT:{VehicleType} M:{Manufacturer} B:{BaseModel}", 
                vehicleType, manufacturerKey, baseModelKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Base model {BaseModelKey} işlenirken hata oluştu", baseModelKey);
            result.HasErrors = true;
        }

        return result;
    }

    /// <summary>
    /// Tek bir sub model'i bulk olarak işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessSubModelBulkAsync(
        int vehicleType, 
        object manufacturer,
        object baseModel,
        object subModel,
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();
        var manufacturerKey = GetKey(manufacturer);
        var baseModelKey = GetKey(baseModel);
        var subModelKey = GetKey(subModel);

        try
        {
            // Check if cancellation requested
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("⚠️ SubModel processing cancelled before starting for VT:{VehicleType} M:{Manufacturer} B:{BaseModel} S:{SubModel}", 
                    vehicleType, GetKey(manufacturer), GetKey(baseModel), GetKey(subModel));
                return result;
            }

            // SEMAPHORE KALDIRILDI - DEADLOCK SORUNU
            BulkSyncResult optionsResult;
            try
            {
                optionsResult = await ProcessOptionsBulkAsync(vehicleType, manufacturer, baseModel, subModel, CancellationToken.None, progress);
                result.Add(optionsResult);
            }
            catch (Exception optEx)
            {
                _logger.LogError(optEx, "❌ ERROR in ProcessOptionsBulkAsync for VT:{VehicleType} M:{Manufacturer} B:{BaseModel} S:{SubModel}", 
                    vehicleType, GetKey(manufacturer), GetKey(baseModel), GetKey(subModel));
                optionsResult = new BulkSyncResult(); // Boş result, devam edebilsin
            }
            
            // API rate limiting için delay
            await Task.Delay(100);
            
            // PARTS & VEHICLE DATA AKTARIMI - DatECode compile et (image ProcessPartsBulkAsync içinde)
            try
            {
                var partsImageResult = await ProcessPartsBulkAsync(
                    vehicleType, manufacturer, baseModel, subModel, 
                    CancellationToken.None, 
                    optionsResult.CollectedOptions,
                    progress);
                result.Add(partsImageResult);
                _logger.LogInformation("✅ Parts & VehicleData processing completed");
            }
            catch (Exception partsEx)
            {
                _logger.LogError(partsEx, "❌ ERROR in ProcessPartsBulkAsync for VT:{VehicleType} M:{Manufacturer} B:{BaseModel} S:{SubModel}", 
                    vehicleType, GetKey(manufacturer), GetKey(baseModel), GetKey(subModel));
            }
            
            // API rate limiting için delay
            await Task.Delay(100);
            
            // IMAGE AKTARIMI - ProcessPartsBulkAsync içinde zaten yapılıyor
            // DatECode olmadan image alınamaz (DAT API kısıtlaması)
            _logger.LogDebug("⏭️ Image aktarımı ProcessPartsBulkAsync içinde yapıldı (DatECode ile)");
            
            // API rate limiting için delay
            await Task.Delay(100);
        }
        catch (Exception ex)
        {
            var subModelValue = GetValue(subModel);
            _logger.LogWarning(ex, "❌ Sub model {SubModel} işlenirken hata: {ErrorMessage}", subModelValue, ex.Message);
            result.HasErrors = true;
        }

        return result;
    }

    /// <summary>
    /// Sub model için tüm opsiyonları paralel olarak işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessOptionsBulkAsync(
        int vehicleType, 
        object manufacturer,
        object baseModel,
        object subModel,
        CancellationToken cancellationToken,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();

        try
        {
            // Classification groups'ları al
            var manufacturerKey = GetKey(manufacturer);
            var baseModelKey = GetKey(baseModel);
            var subModelKey = GetKey(subModel);
            
            
            var classificationGroups = await _datService.GetClassificationGroupsAsync(
                vehicleType, manufacturerKey, baseModelKey, subModelKey);
            
            _logger.LogInformation("✅ ClassificationGroups fetched: {Count} groups", 
                classificationGroups?.ClassificationGroups?.Count ?? 0);

            // Eğer classification group yoksa, boş sonuç döndür
            if (classificationGroups == null || classificationGroups.ClassificationGroups == null || !classificationGroups.ClassificationGroups.Any())
            {
                _logger.LogWarning("⚠️⚠️⚠️ ATLANIYOR: No classification groups found for VT:{VT} M:{M} B:{B} S:{S} - ProcessPartsBulkAsync ÇAĞRILMAYACAK!", 
                    vehicleType, manufacturerKey, baseModelKey, subModelKey);
                return result; // BOŞ RESULT DÖNÜYOR - CollectedOptions = NULL!
            }
            
            _logger.LogInformation("✅✅✅ BAŞARILI: {Count} classification groups found for VT:{VT} M:{M} B:{B} S:{S} - DEVAM EDİYOR!", 
                classificationGroups.ClassificationGroups.Count, vehicleType, manufacturerKey, baseModelKey, subModelKey);

                // BATCH ŞEKLİNDE PARALEL İŞLE (performans + kararlılık)
            var collectedOptions = new System.Collections.Concurrent.ConcurrentBag<string>();
            var totalOptions = 0;
            var allOptionsToSave = new System.Collections.Concurrent.ConcurrentBag<(List<DatOption> options, int classification)>();
            
            _logger.LogDebug("⏳ Processing {Count} classifications in BATCHES...", classificationGroups.ClassificationGroups.Count);
            
            // Her 3 classification'ı bir batch olarak işle
            var batchSize = 3;
            for (int i = 0; i < classificationGroups.ClassificationGroups.Count; i += batchSize)
            {
                var batch = classificationGroups.ClassificationGroups.Skip(i).Take(batchSize).ToList();
                _logger.LogDebug("📦 Processing batch {BatchNo}/{Total} ({Count} classifications)", 
                    (i / batchSize) + 1, 
                    (classificationGroups.ClassificationGroups.Count + batchSize - 1) / batchSize,
                    batch.Count);
                
                var batchTasks = batch.Select(async classification =>
                {
                    try
                    {
                        var options = await _datService.GetOptionsByClassificationAsync(
                            vehicleType, manufacturerKey, baseModelKey, subModelKey, classification);
                        
                        // Opsiyonları topla (her classification'dan SADECE 1 option al - uyumluluk için)
                        if (options.Options?.Option != null && options.Options.Option.Any())
                        {
                            // Her classification'dan sadece ilk option'ı al (uyumluluk garantisi)
                            var firstOption = options.Options.Option.First();
                            collectedOptions.Add(GetKey(firstOption));
                            Interlocked.Add(ref totalOptions, options.Options.Option.Count);
                            // Progress: Options delta (sadece 1 artır)
                            progress?.Report(new BulkSyncResult { OptionsCount = 1 });
                            
                            // Kayıt için tüm options'ı sakla
                            allOptionsToSave.Add((options.Options.Option, classification));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error fetching options for classification {C}", classification);
                    }
                }).ToList();
                
                // Bu batch'i tamamla
                await Task.WhenAll(batchTasks);
            }
            
            _logger.LogInformation("✅ All classifications fetched. Total options: {Total}, Collected for eCode: {Collected}", 
                totalOptions, collectedOptions.Count);
            
            // Şimdi toplu kaydet (DB lock süresini minimize et)
            if (allOptionsToSave.Any())
            {
                _logger.LogInformation("💾 Saving {Count} option groups to DB...", allOptionsToSave.Count);
                
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                    await _dbSemaphore.WaitAsync(CancellationToken.None);
                    try
                    {
                        foreach (var (options, classification) in allOptionsToSave)
                        {
                            await dataService.SaveOptionsAsync(
                                options, vehicleType, manufacturerKey, baseModelKey, subModelKey, classification);
                        }
                        _logger.LogInformation("✅ All options saved to DB");
                    }
                    finally
                    {
                        _dbSemaphore.Release();
                    }
                }
            }
            
            result.OptionsCount = totalOptions;
            result.CollectedOptions = collectedOptions.ToList(); // Tüm toplanan options'ı sakla
            
            if (collectedOptions.Any())
            {
                _logger.LogInformation($"📋 Collected Options: {string.Join(", ", collectedOptions.Take(10))}");
            }
            else
            {
                _logger.LogWarning("⚠️ No options collected for eCode compilation");
            }

            // Engine options (paralel)
            var engineOptionsTask = Task.Run(async () =>
            {
                try
                {
                    var engineOptions = await _datService.GetEngineOptionsAsync(
                        vehicleType, manufacturerKey, baseModelKey, subModelKey,
                        constructionTimeFrom: "0001",
                        constructionTimeTo: "9912");
                    
                    // Kendi scope'unda kaydet
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                        await _dbSemaphore.WaitAsync(CancellationToken.None);
                        try
                        {
                            await dataService.SaveEngineOptionsAsync(
                                engineOptions.EngineOptions.EngineOption, vehicleType, manufacturerKey, baseModelKey, subModelKey);
                        }
                        finally
                        {
                            _dbSemaphore.Release();
                        }
                    }
                    // Progress: engine options delta
                    progress?.Report(new BulkSyncResult { EngineOptionsCount = engineOptions.EngineOptions.EngineOption.Count });

                    // Fallback: classification 3'ten azsa ilk engine option'ı da derlemeye ekle
                    var firstEngine = engineOptions.EngineOptions.EngineOption.FirstOrDefault();
                    if (firstEngine != null && collectedOptions.Count < 4)
                    {
                        collectedOptions.Add(GetKey(firstEngine));
                    }
                    return engineOptions.EngineOptions.EngineOption.Count;
                }
                catch
                {
                    return 0;
                }
            });

            // Car body options (paralel)
            var carBodyOptionsTask = Task.Run(async () =>
            {
                try
                {
                    var carBodyOptions = await _datService.GetCarBodyOptionsAsync(
                        vehicleType, manufacturerKey, baseModelKey, subModelKey,
                        constructionTimeFrom: "0001",
                        constructionTimeTo: "9912");
                    
                    // Kendi scope'unda kaydet
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                        await _dbSemaphore.WaitAsync(CancellationToken.None);
                        try
                        {
                            await dataService.SaveCarBodyOptionsAsync(
                                carBodyOptions.CarBodyOptions.CarBodyOption, vehicleType, manufacturerKey, baseModelKey, subModelKey);
                        }
                        finally
                        {
                            _dbSemaphore.Release();
                        }
                    }
                    // Progress: car body options delta
                    progress?.Report(new BulkSyncResult { CarBodyOptionsCount = carBodyOptions.CarBodyOptions.CarBodyOption.Count });

                    // Fallback: classification 3'ten azsa ilk car body option'ı da derlemeye ekle
                    var firstBody = carBodyOptions.CarBodyOptions.CarBodyOption.FirstOrDefault();
                    if (firstBody != null && collectedOptions.Count < 4)
                    {
                        collectedOptions.Add(GetKey(firstBody));
                    }
                    return carBodyOptions.CarBodyOptions.CarBodyOption.Count;
                }
                catch
                {
                    return 0;
                }
            });

            await Task.WhenAll(engineOptionsTask, carBodyOptionsTask);
            
            result.EngineOptionsCount = await engineOptionsTask;
            result.CarBodyOptionsCount = await carBodyOptionsTask;

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌❌❌ KRITIK HATA: ProcessOptionsBulkAsync FAILED! Exception: {Message}", ex.Message);
            _logger.LogError("❌ Stack Trace: {StackTrace}", ex.StackTrace);
            result.HasErrors = true;
        }

        _logger.LogDebug("🏁 ProcessOptionsBulkAsync TAMAMLANDI - Collected Options: {Count}", result.CollectedOptions?.Count ?? 0);
        return result;
    }

    /// <summary>
    /// Sub model için parçaları işler
    /// </summary>
    private async Task<BulkSyncResult> ProcessPartsBulkAsync(
        int vehicleType, 
        object manufacturer,
        object baseModel,
        object subModel,
        CancellationToken cancellationToken,
        List<string>? selectedOptions = null,
        IProgress<BulkSyncResult>? progress = null)
    {
        var result = new BulkSyncResult();
        var manufacturerKey = GetKey(manufacturer);
        var baseModelKey = GetKey(baseModel);
        var subModelKey = GetKey(subModel);

        try
        {
            // ✅ DotOptions tablosundan classification bazında options al
            // Her classification'dan 1 option seç → Option sayısı = Classification sayısı
            List<string> allOptions;
            Dictionary<int, List<string>> optionsByClassification;
            
            try
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                    optionsByClassification = await dataService.GetOptionsGroupedByClassificationAsync(
                        vehicleType, manufacturerKey, baseModelKey, subModelKey);
                }
                
                // Her classification'dan BİR option al (FIRST), max 8 classification
                allOptions = optionsByClassification
                    .OrderBy(kvp => kvp.Key) // Classification numarasına göre sırala
                    .Take(8) // MAX 8 classification!
                    .Select(kvp => kvp.Value.FirstOrDefault()) // Her classification'dan BİR option
                    .Where(opt => !string.IsNullOrWhiteSpace(opt))
                    .ToList()!;
                
                _logger.LogInformation($"📊 {optionsByClassification.Count} classification, {allOptions.Count} option (her classification'dan 1)");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ DotOptions'dan option alınamadı, boş liste kullanılacak");
                allOptions = (selectedOptions ?? new List<string>()).ToList();
                optionsByClassification = new Dictionary<int, List<string>>();
            }
            
            {
                // Hiç opsiyon yoksa bile BOŞ LİSTE ile DatECode derlemeyi dene
                if (allOptions.Count == 0)
                {
                    _logger.LogWarning($"⚠️ Hiç classification/option yok. BOŞ LİSTE ile DatECode denleniyor.");
                }
                else if (allOptions.Count < 3)
                {
                    _logger.LogWarning($"⚠️ {allOptions.Count} classification var. DAT API genellikle minimum 3 gerektiriyor, deneyeceğiz.");
                }
                
                // ✅ CLASSIFICATION BAZLI DENEME
                // Her classification'dan 1 option → Option sayısı = Classification sayısı
                // Örnek: 7 classification var → 7 option gönder (her birinden 1)
                
                string? compiledECode = null;
                List<string>? usedOptions = null;
                
            // ÖNCELİK: Tüm classification'lardan birer option ile dene (direkt gönder!)
            _logger.LogInformation($"🔍 {optionsByClassification.Count} classification için CompileDatECode deneniyor...");
            
            // Fallback için candidates listesi (başlangıç: mevcut allOptions)
            var candidates = allOptions.Take(4).ToList();
            
            DatECodeReturn eCodeResult;
            if (allOptions.Count >= 3)
            {
                eCodeResult = await _datService.CompileDatECodeAsync(
                    vehicleType, manufacturerKey, baseModelKey, subModelKey, allOptions.Take(4).ToList());
            }
            else
            {
                // Seçmeli yapının fallback'i: elimizdeki 1-2 option + engine/carbody ilkleriyle 3-4'e tamamla
                while (candidates.Count() < 3 && allOptions.Any())
                {
                    var next = allOptions.Where(o => !candidates.Contains(o)).FirstOrDefault();
                    if (next == null) break;
                    candidates.Add(next);
                }
                eCodeResult = await _datService.CompileDatECodeAsync(
                    vehicleType, manufacturerKey, baseModelKey, subModelKey, candidates);
            }
                
                if (!string.IsNullOrWhiteSpace(eCodeResult.DatECode))
                {
                    compiledECode = eCodeResult.DatECode;
                    usedOptions = allOptions;
                    _logger.LogInformation($"✅ eCode derlendi ({allOptions.Count} classification ile): {compiledECode}");
                }
                else
                {
                    // Başarısız olduysa, hata mesajını kontrol et ve classification sayısını azaltarak dene
                    if (!string.IsNullOrWhiteSpace(eCodeResult.RawXmlResponse))
                    {
                        var xmlResp = eCodeResult.RawXmlResponse;
                        
                        // "between X and Y options are required" mesajını parse et
                        var betweenMatch = System.Text.RegularExpressions.Regex.Match(
                            xmlResp, @"between (\d+) and (\d+) options are required", 
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        
                        if (betweenMatch.Success)
                        {
                            var minClassifications = int.Parse(betweenMatch.Groups[1].Value);
                            var maxClassifications = int.Parse(betweenMatch.Groups[2].Value);
                            _logger.LogInformation($"📊 API {minClassifications}-{maxClassifications} classification istiyor, {optionsByClassification.Count} mevcut");
                            
                            // Classification sayısını kontrol et ve retry yap
                            if (optionsByClassification.Count < minClassifications)
                            {
                                _logger.LogWarning($"❌ Yetersiz classification! {optionsByClassification.Count} var, {minClassifications}-{maxClassifications} gerekli");
                                result.SubModelsWithoutParts = 1;
                            }
                            else if (optionsByClassification.Count >= minClassifications && optionsByClassification.Count <= maxClassifications)
                            {
                                // Doğru sayıda classification var ama "Wrong options" hatası aldık
                                // Her classification'dan 2. option'ı dene (belki farklı kombinasyon gerekli)
                                var retryOptions = optionsByClassification
                                    .OrderBy(x => x.Key)
                                    .Select(kvp => kvp.Value.Skip(1).FirstOrDefault() ?? kvp.Value.FirstOrDefault()) // 2. option yoksa 1.yi al
                                    .Where(opt => !string.IsNullOrWhiteSpace(opt))
                                    .Take(8)
                                    .ToList()!;
                                
                                if (retryOptions.Any() && !retryOptions.SequenceEqual(allOptions))
                                {
                                    _logger.LogInformation($"🔄 Retry: Farklı option kombinasyonu ile ({retryOptions.Count} option)...");
                                    
                                    var retryResult = await _datService.CompileDatECodeAsync(
                                        vehicleType, manufacturerKey, baseModelKey, subModelKey, retryOptions);
                                    
                                    if (!string.IsNullOrWhiteSpace(retryResult.DatECode))
                                    {
                                        compiledECode = retryResult.DatECode;
                                        usedOptions = retryOptions;
                                        _logger.LogInformation($"✅ eCode derlendi (alternatif kombinasyon ile): {compiledECode}");
                                    }
                                }
                            }
                            else if (optionsByClassification.Count > maxClassifications)
                            {
                                // Fazla classification var, azalt
                                var selectedClassifications = optionsByClassification
                                    .OrderBy(x => x.Key)
                                    .Take(maxClassifications)
                                    .ToList();
                                
                                var retryOptions = selectedClassifications
                                    .Select(kvp => kvp.Value.FirstOrDefault())
                                    .Where(opt => !string.IsNullOrWhiteSpace(opt))
                                    .ToList()!;
                                
                                _logger.LogInformation($"🔄 Retry: {maxClassifications} classification ile...");
                                
                                var retryResult = await _datService.CompileDatECodeAsync(
                                    vehicleType, manufacturerKey, baseModelKey, subModelKey, retryOptions);
                                
                                if (!string.IsNullOrWhiteSpace(retryResult.DatECode))
                                {
                                    compiledECode = retryResult.DatECode;
                                    usedOptions = retryOptions;
                                    _logger.LogInformation($"✅ eCode derlendi ({retryOptions.Count} classification ile): {compiledECode}");
                                }
                            }
                        }
                        else if (xmlResp.Contains("Wrong options"))
                        {
                            // "Wrong options" → Option kombinasyonu yanlış, alternatif dene
                            _logger.LogInformation($"⚠️ Wrong options! Alternatif kombinasyon deneniyor...");
                            
                            // Her classification'dan 2. option'ı dene
                            var retryOptions = optionsByClassification
                                .OrderBy(x => x.Key)
                                .Take(8)
                                .Select(kvp => kvp.Value.Skip(1).FirstOrDefault() ?? kvp.Value.FirstOrDefault())
                                .Where(opt => !string.IsNullOrWhiteSpace(opt))
                                .ToList()!;
                            
                            if (retryOptions.Any() && !retryOptions.SequenceEqual(allOptions))
                            {
                                _logger.LogInformation($"🔄 Retry: 2. option kombinasyonu ({retryOptions.Count} option)...");
                                
                                var retryResult = await _datService.CompileDatECodeAsync(
                                    vehicleType, manufacturerKey, baseModelKey, subModelKey, retryOptions);
                                
                                if (!string.IsNullOrWhiteSpace(retryResult.DatECode))
                                {
                                    compiledECode = retryResult.DatECode;
                                    usedOptions = retryOptions;
                                    _logger.LogInformation($"✅ eCode derlendi (2. kombinasyon ile): {compiledECode}");
                                }
                            }
                        }
                        else
                        {
                            _logger.LogWarning($"❌ CompileDatECode başarısız: {xmlResp.Substring(0, Math.Min(200, xmlResp.Length))}");
                        }
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(compiledECode) && usedOptions != null)
                {
                    _logger.LogInformation($"🔍 DatECode: {compiledECode} (VT:{vehicleType} M:{manufacturerKey} B:{baseModelKey} S:{subModelKey})");
                    
                    // derlenen eCode'u hemen kaydet
                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                            await _dbSemaphore.WaitAsync(CancellationToken.None);
                            try
                            {
                                await dataService.SaveCompiledCodeAsync(
                                    compiledECode,
                                    vehicleType,
                                    manufacturerKey,
                                    baseModelKey,
                                    subModelKey,
                                    usedOptions);
                                _logger.LogInformation("✅ CompiledCode saved: {DatECode}", compiledECode);
                                // Progress: compiled code delta
                                progress?.Report(new BulkSyncResult { SubModelsCount = 0, CompiledCodesCount = 1 });
                            }
                            finally
                            {
                                _dbSemaphore.Release();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Error saving CompiledCode for {DatECode}", compiledECode);
                    }

                    // ÖNCELİKLE: DatData tablosundan DatProcessNo listesini al
                    List<string>? datProcessNosFromDb = null;
                    try
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                            var dbProcessNos = await dataService.GetDatProcessNumbersByVehicleAsync(
                                vehicleType, 
                                int.Parse(manufacturerKey), 
                                int.Parse(baseModelKey));
                            
                            if (dbProcessNos.Any())
                            {
                                datProcessNosFromDb = dbProcessNos.Select(n => n.ToString()).ToList();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ DatData error");
                    }
                    
                    // datProcessNo listesini belirle (öncelik sırası: DatData > datECode)
                    var datProcessNos = datProcessNosFromDb != null && datProcessNosFromDb.Any()
                        ? datProcessNosFromDb  // 1. DatData'dan gelen (en güvenilir)
                        : new List<string> { compiledECode };  // 2. Fallback: datECode
                        
                        
                    // PARTS AKTARIMI ATLANACAK - DatData tablosundan çekilecek
                    
                    // IMAGE AKTARIMI - DatECode ile
                    try
                    {
                        var imagesResult = await _datService.GetVehicleImagesAsync(compiledECode);
                            
                        if (imagesResult?.Images != null && imagesResult.Images.Any())
                        {
                            _logger.LogInformation("✅ {Count} images for: {DatECode}", 
                                imagesResult.Images.Count, compiledECode);
                            
                            // Images'ları veritabanına kaydet
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                                await _dbSemaphore.WaitAsync(CancellationToken.None);
                                try
                                {
                                    await dataService.SaveVehicleImagesAsync(compiledECode, imagesResult.Images);
                                    result.ImagesCount = imagesResult.Images.Count;
                                    // Progress: images delta
                                    progress?.Report(new BulkSyncResult { ImagesCount = imagesResult.Images.Count });
                                }
                                finally
                                {
                                    _dbSemaphore.Release();
                                }
                            }
                        }
                    }
                    catch (Exception imgEx)
                    {
                        _logger.LogWarning(imgEx, "❌ Image error: {Msg}", imgEx.Message);
                    }
                        
                    // Araç verilerini çek ve kaydet
                    try
                    {
                        // SubModel'den constructionTimeFrom al (yoksa default kullanılır)
                        var constructionTime = GetConstructionTimeFrom(subModel);
                        
                        var vehicleData = await _datService.GetVehicleDataAsync(compiledECode, null, constructionTime);
                        
                        // Eğer sadece boş vehicle döndüyse (API 500 hatası), kaydetme
                        if (string.IsNullOrWhiteSpace(vehicleData.Container) && 
                            string.IsNullOrWhiteSpace(vehicleData.ManufacturerName) &&
                            string.IsNullOrWhiteSpace(vehicleData.BaseModelName))
                        {
                            _logger.LogDebug("VehicleData empty");
                        }
                        else
                        {
                            // DatECode'u manuel set et (API'den gelmeyebilir)
                            if (string.IsNullOrWhiteSpace(vehicleData.DatECode))
                            {
                                vehicleData.DatECode = compiledECode;
                            }
                            
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                                await _dbSemaphore.WaitAsync(CancellationToken.None);
                                try
                                {
                                    await dataService.SaveVehicleDataAsync(vehicleData);
                                    _logger.LogInformation("✅ VehicleData saved");
                                }
                                finally
                                {
                                    _dbSemaphore.Release();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "VehicleData not available");
                    }

                }
                else
                {
                    _logger.LogWarning("⚠️ eCode derlenemedi (3-8 option denendi) VT:{VehicleType} M:{Manufacturer} B:{BaseModel} S:{SubModel}", vehicleType, manufacturerKey, baseModelKey, subModelKey);
                    result.SubModelsWithoutParts = 1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "❌ Parts error for VehicleType: {VehicleType}, Manufacturer: {ManufacturerKey}, BaseModel: {BaseModelKey}, SubModel: {SubModelKey}: {ErrorMessage}", 
                vehicleType, manufacturerKey, baseModelKey, subModelKey, ex.Message);
            result.SubModelsWithErrors = 1;
            result.HasErrors = true;
        }

        return result;
    }

    /// <summary>
    /// Object'ten Key property'sini alır (reflection ile)
    /// </summary>
    private static string GetKey(object obj)
    {
        var keyProperty = obj.GetType().GetProperty("Key");
        return keyProperty?.GetValue(obj)?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Object'ten Value property'sini alır (reflection ile)
    /// </summary>
    private static string GetValue(object obj)
    {
        var valueProperty = obj.GetType().GetProperty("Value");
        return valueProperty?.GetValue(obj)?.ToString() ?? string.Empty;
    }

    public void Dispose()
    {
        _apiSemaphore?.Dispose();
        _dbSemaphore?.Dispose();
        _optionsFetchSemaphore?.Dispose();
    }
    
    private string? GetConstructionTimeFrom(object obj)
    {
        if (obj == null) return "4040"; // Default: 2040/April
        
        var type = obj.GetType();
        var constructionTimeFromProp = type.GetProperty("ConstructionTimeFrom");
        
        if (constructionTimeFromProp != null)
        {
            var value = constructionTimeFromProp.GetValue(obj);
            var constructionTime = value?.ToString();
            
            // Eğer boş değilse, döndür
            if (!string.IsNullOrWhiteSpace(constructionTime))
            {
                return constructionTime;
            }
        }
        
        // SubModel'de constructionTime yoksa veya boşsa, default değer kullan
        _logger.LogDebug("ConstructionTimeFrom bulunamadı veya boş, default değer kullanılıyor: 4040");
        return "4040"; // Default: 2040/April (YYMM formatı)
    }

}

/// <summary>
/// Bulk senkronizasyon sonuçlarını tutar
/// </summary>
public class BulkSyncResult
{
    public int VehicleTypesCount { get; set; }
    public int ManufacturersCount { get; set; }
    public int BaseModelsCount { get; set; }
    public int SubModelsCount { get; set; }
    public int VehiclesCount { get; set; }
    public int OptionsCount { get; set; }
    public int EngineOptionsCount { get; set; }
    public int CarBodyOptionsCount { get; set; }
    public int PartsCount { get; set; }
    public int SubModelsWithParts { get; set; }
    public int SubModelsWithoutParts { get; set; }
    public int SubModelsWithErrors { get; set; }
    public int ImagesCount { get; set; }
    public int CompiledCodesCount { get; set; }
    // UI alt başlık için anlık bağlam
    public string? CurrentVehicleTypeName { get; set; }
    public string? CurrentManufacturerName { get; set; }
    public string? CurrentBaseModelName { get; set; }
    public string? CurrentSubModelName { get; set; }
    public bool HasErrors { get; set; }
    public TimeSpan TotalDuration { get; set; }
    

    public List<string> CollectedOptions { get; set; } = new List<string>();

    public void Add(BulkSyncResult other)
    {
        VehicleTypesCount += other.VehicleTypesCount;
        ManufacturersCount += other.ManufacturersCount;
        BaseModelsCount += other.BaseModelsCount;
        SubModelsCount += other.SubModelsCount;
        VehiclesCount += other.VehiclesCount;
        OptionsCount += other.OptionsCount;
        EngineOptionsCount += other.EngineOptionsCount;
        CarBodyOptionsCount += other.CarBodyOptionsCount;
        PartsCount += other.PartsCount;
        SubModelsWithParts += other.SubModelsWithParts;
        SubModelsWithoutParts += other.SubModelsWithoutParts;
        SubModelsWithErrors += other.SubModelsWithErrors;
        ImagesCount += other.ImagesCount;
        HasErrors = HasErrors || other.HasErrors;
        
    
        if (other.CollectedOptions != null && other.CollectedOptions.Any())
        {
            CollectedOptions = other.CollectedOptions;
        }
    }

    public int GetTotalCount()
    {
        return VehicleTypesCount + ManufacturersCount + BaseModelsCount + SubModelsCount + 
               VehiclesCount + OptionsCount + EngineOptionsCount + CarBodyOptionsCount + PartsCount + ImagesCount;
    }
}