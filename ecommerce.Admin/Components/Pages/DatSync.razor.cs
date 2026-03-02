using Dot.Integration.Services;
using Dot.Integration.Abstract;
using ecommerce.Admin.EFCore.UnitOfWork;
using ecommerce.EFCore.Context;
using Microsoft.AspNetCore.Components;
using Radzen;
using Microsoft.Extensions.DependencyInjection;
using ecommerce.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ecommerce.Admin.Components.Pages;

public partial class DatSync
{
    [Inject] private DatVehicleSyncService SyncService { get; set; } = null!;
    [Inject] private DatBulkSyncService BulkSyncService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private ILogger<DatSync> Logger { get; set; } = null!;
    [Inject] private IServiceScopeFactory _scopeFactory { get; set; } = null!;
    

    private bool isRunning = false;
    private bool isBulkSyncingConstructionPeriods = false;
    private bool isUpdatingYears = false;
    private string statusMessage = string.Empty;
    private string errorMessage = string.Empty;
    private SyncStatistics? syncStats = null;
    private readonly object _statsLock = new object();

    // Ayrı seçim için gerekli state'ler
    private List<DotVehicleType> vehicleTypes = new();
    private List<DotManufacturer> manufacturers = new();

    private int? selectedVehicleTypeId;
    private string? selectedManufacturerKey;

    protected override async Task OnInitializedAsync()
    {
        await LoadVehicleTypes();
    }

    private async Task LoadVehicleTypes()
    {
        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                vehicleTypes = await dbContext.Set<DotVehicleType>()
                    .Where(vt => vt.IsActive)
                    .OrderBy(vt => vt.Id)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Araç tipleri yüklenirken hata oluştu");
        }
    }

    private async Task SelectVehicleType(int vehicleTypeId)
    {
        selectedVehicleTypeId = vehicleTypeId;
        selectedManufacturerKey = null;
        manufacturers.Clear();

        await LoadManufacturers();
    }

    private async Task LoadManufacturers()
    {
        if (!selectedVehicleTypeId.HasValue) return;

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                manufacturers = await dbContext.Set<DotManufacturer>()
                    .Where(m => m.VehicleType == selectedVehicleTypeId.Value && m.IsActive)
                    .OrderBy(m => m.Name)
                    .ToListAsync();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Üreticiler yüklenirken hata oluştu");
        }
    }


    private async Task SyncSelectedManufacturer()
    {
        if (!selectedVehicleTypeId.HasValue || string.IsNullOrEmpty(selectedManufacturerKey))
        {
            errorMessage = "Lütfen önce araç tipi ve marka seçin!";
            return;
        }

        isRunning = true;
        statusMessage = string.Empty;
        errorMessage = string.Empty;
        syncStats = new SyncStatistics();
        StateHasChanged();

        try
        {
            var selectedVehicleType = vehicleTypes.First(vt => vt.Id == selectedVehicleTypeId.Value);
            var selectedManufacturer = manufacturers.First(m => m.DatKey == selectedManufacturerKey);

            statusMessage = $"🚀 BULK SENKRONIZASYON: Seçili marka için TÜM veriler aktarılıyor...\n";
            statusMessage += $"📋 Araç Tipi: {selectedVehicleType.Name} ({selectedVehicleType.Id})\n";
            statusMessage += $"🏭 Üretici: {selectedManufacturer.Name} ({selectedManufacturer.DatKey})\n";
            statusMessage += $"⚡ Paralel işlem, tam veri (CompiledCodes, Parts, Images dahil)\n\n";
            StateHasChanged();
            
            // BULK SERVİSİ KULLAN!
            var cancellationTokenSource = new CancellationTokenSource();
            var startTime = DateTime.Now;
            
            // Progress callback
            var progress = new Progress<BulkSyncResult>(async (partialResult) =>
            {
                await InvokeAsync(() =>
                {
                    // Ara sonuçları UI'ya yansıt
                    if (partialResult.BaseModelsCount > 0)
                        syncStats.BaseModelsCount = Math.Max(syncStats.BaseModelsCount, partialResult.BaseModelsCount);
                    if (partialResult.SubModelsCount > 0)
                        syncStats.SubModelsCount = Math.Max(syncStats.SubModelsCount, partialResult.SubModelsCount);
                    if (partialResult.OptionsCount > 0)
                        syncStats.OptionsCount = Math.Max(syncStats.OptionsCount, partialResult.OptionsCount);
                    if (partialResult.EngineOptionsCount > 0)
                        syncStats.EngineOptionsCount = Math.Max(syncStats.EngineOptionsCount, partialResult.EngineOptionsCount);
                    if (partialResult.CarBodyOptionsCount > 0)
                        syncStats.CarBodyOptionsCount = Math.Max(syncStats.CarBodyOptionsCount, partialResult.CarBodyOptionsCount);
                    // Kümülatif değil delta gelebilir: her zaman ekle
                    if (partialResult.PartsCount > 0)
                        syncStats.PartsCount += partialResult.PartsCount;
                    if (partialResult.ImagesCount > 0)
                        syncStats.ImagesCount += partialResult.ImagesCount;
                    if (partialResult.CompiledCodesCount > 0)
                        syncStats.CompiledCodesCount += partialResult.CompiledCodesCount;
                    if (partialResult.OptionsCount > 0)
                        syncStats.OptionsCount += partialResult.OptionsCount;

                    // Alt başlık: şu an işlenen bağlam
                    if (!string.IsNullOrWhiteSpace(partialResult.CurrentBaseModelName) || !string.IsNullOrWhiteSpace(partialResult.CurrentSubModelName))
                    {
                        var vt = partialResult.CurrentVehicleTypeName ?? selectedVehicleTypeId?.ToString();
                        var mk = partialResult.CurrentManufacturerName ?? selectedManufacturerKey;
                        var bm = partialResult.CurrentBaseModelName;
                        var sm = partialResult.CurrentSubModelName;
                        statusMessage = statusMessage.Replace("⏱️ Geçen süre:", $"⏱️ Geçen süre:  (Şu an: VT:{vt} · M:{mk} · B:{bm} · S:{sm})\n⏱️ Geçen süre:");
                    }
                    
                    StateHasChanged();
                });
            });
            
            // Tek marka için bulk sync çağır
            var result = await BulkSyncService.BulkSyncSingleManufacturerAsync(
                selectedVehicleTypeId.Value,
                selectedManufacturer.DatKey,
                cancellationToken: cancellationTokenSource.Token,
                progress: progress);
            
            var duration = DateTime.Now - startTime;
            
            // Sonuçları syncStats'a aktar
            syncStats.BaseModelsCount = result.BaseModelsCount;
            syncStats.SubModelsCount = result.SubModelsCount;
            syncStats.OptionsCount = result.OptionsCount;
            syncStats.EngineOptionsCount = result.EngineOptionsCount;
            syncStats.CarBodyOptionsCount = result.CarBodyOptionsCount;
            syncStats.PartsCount = result.PartsCount;
            syncStats.ImagesCount = result.ImagesCount;
            syncStats.CompiledCodesCount = result.SubModelsCount; // Her submodel için 1 compiled code varsayımı
            
            statusMessage += $"\n🎉 BULK SENKRONIZASYON TAMAMLANDI!\n";
            statusMessage += $"⏱️ Toplam süre: {duration:mm\\:ss}\n\n";
            statusMessage += $"📊 SONUÇLAR:\n";
            statusMessage += $"  🚗 {result.BaseModelsCount} ana model\n";
            statusMessage += $"  📦 {result.SubModelsCount} alt model\n";
            statusMessage += $"  ⚙️ {result.OptionsCount} classification option\n";
            statusMessage += $"  🔧 {result.EngineOptionsCount} motor opsiyonu\n";
            statusMessage += $"  🎨 {result.CarBodyOptionsCount} kasa opsiyonu\n";
            statusMessage += $"  📋 {result.PartsCount} parça\n";
            statusMessage += $"  🖼️ {result.ImagesCount} görsel\n";
            
            StateHasChanged();
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = result.HasErrors ? NotificationSeverity.Warning : NotificationSeverity.Success,
                Summary = "Bulk Senkronizasyon Tamamlandı",
                Detail = $"{result.SubModelsCount} alt model, {result.PartsCount} parça - Süre: {duration:mm\\:ss}",
                Duration = 10000
            });
            
            return; // ESKİ KODU ATLA

            // Bu marka için tüm base modelleri al
            statusMessage += $"📥 Ana modeller getiriliyor...\n";
            StateHasChanged();

            // constructionTime: TÜM modelleri getir (eski Tempra dahil)
            // Format belirsiz - farklı değerler deneyelim
            // Varsayılan 4040-4840 ise, daha geniş aralık: 0001-9912
            var baseModelsResponse = await SyncService._datService.GetBaseModelsNAsync(
                selectedVehicleTypeId.Value, 
                selectedManufacturer.DatKey,
                constructionTimeFrom: "0001",  // En eski (1900 Ocak?)
                constructionTimeTo: "9912");    // En yeni (2099 Aralık?)
            await SyncService._dataService.SaveBaseModelsAsync(baseModelsResponse.BaseModels.BaseModel, selectedVehicleTypeId.Value, selectedManufacturer.DatKey);
            syncStats.BaseModelsCount = baseModelsResponse.BaseModels.BaseModel.Count;

            statusMessage += $"✅ {baseModelsResponse.BaseModels.BaseModel.Count} ana model bulundu\n";
            statusMessage += $"📋 Model listesi:\n";
            foreach (var bm in baseModelsResponse.BaseModels.BaseModel.OrderBy(x => x.Value))
            {
                statusMessage += $"  • {bm.Value} (Key: {bm.Key})\n";
            }
            statusMessage += $"\n";
            StateHasChanged();

            // Her base model için sub models ve diğer verileri senkronize et (PARALEL)
            int processedModels = 0;
            int totalModels = baseModelsResponse.BaseModels.BaseModel.Count;
            int lastReportedProgress = 0;
            
            // Paralel işlem için SemaphoreSlim (aynı anda max 2 model - Apple Silicon stability)
            var semaphore = new SemaphoreSlim(2);
            var tasks = new List<Task>();

            foreach (var baseModel in baseModelsResponse.BaseModels.BaseModel)
            {
                var task = Task.Run(async () =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        // Her paralel işlem için AYRI SCOPE oluştur (DbContext thread-safe değil!)
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var scopedSyncService = scope.ServiceProvider.GetRequiredService<DatVehicleSyncService>();
                            
                            var currentIndex = Interlocked.Increment(ref processedModels);
                            
                            // Periyodik UI güncelle (her 10 model'de bir - performance)
                            if (currentIndex % 10 == 0 || currentIndex == totalModels)
                            {
                                await InvokeAsync(() =>
                                {
                                    statusMessage += $"📊 [{currentIndex}/{totalModels}] {baseModel.Value} işleniyor...\n";
                                    StateHasChanged();
                                });
                            }
                            
                            // Her 20 model'de GC ve delay (memory management)
                            if (currentIndex % 20 == 0)
                            {
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                await Task.Delay(500); // 500ms delay - API ve sistem rahatlar
                            }

                            try
                            {
                                var subModelsResponse = await scopedSyncService._datService.GetSubModelsAsync(
                                    selectedVehicleTypeId.Value, 
                                    selectedManufacturer.DatKey, 
                                    baseModel.Key,
                                    constructionTimeFrom: "0001",  // En eski
                                    constructionTimeTo: "9912");    // En yeni
                                
                                if (subModelsResponse.SubModels.SubModel.Any())
                                {
                                    await scopedSyncService._dataService.SaveSubModelsAsync(subModelsResponse.SubModels.SubModel, selectedVehicleTypeId.Value, selectedManufacturer.DatKey, baseModel.Key);
                                    
                                    lock (_statsLock)
                                    {
                                        syncStats.SubModelsCount += subModelsResponse.SubModels.SubModel.Count;
                                    }
                                    
                                    // Periyodik UI güncelleme (her 10 model'de bir)
                                    if (currentIndex % 10 == 0 || currentIndex == totalModels)
                                    {
                                        await InvokeAsync(() =>
                                        {
                                            statusMessage += $"  └─ ✅ {subModelsResponse.SubModels.SubModel.Count} alt model kaydedildi (toplam: {syncStats.SubModelsCount})\n";
                                            StateHasChanged();
                                        });
                                    }

                                    // Her sub model için detaylı senkronizasyon (PARALEL)
                                    var subModelTasks = subModelsResponse.SubModels.SubModel.Select(async subModel =>
                                    {
                                        try
                                        {
                                            // Her sub model için de AYRI SCOPE kullan
                                            using (var subScope = _scopeFactory.CreateScope())
                                            {
                                                var subScopedSyncService = subScope.ServiceProvider.GetRequiredService<DatVehicleSyncService>();
                                                await SyncSelectedSubModelWithScope(subScopedSyncService, selectedVehicleTypeId.Value, selectedManufacturer.DatKey, baseModel.Key, subModel.Key);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Logger.LogWarning(ex, "Sub model senkronizasyon hatası: {SubModel}", subModel.Value);
                                        }
                                    });

                                    await Task.WhenAll(subModelTasks);
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning(ex, "Base model senkronizasyon hatası: {BaseModel}", baseModel.Value);
                            }
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                tasks.Add(task);
            }

            // Tüm base modellerin tamamlanmasını bekle
            await Task.WhenAll(tasks);

            statusMessage += $"\n🎉 MARKA SENKRONİZASYONU TAMAMLANDI!\n";
            statusMessage += $"📊 Toplam {processedModels} ana model işlendi\n";
            statusMessage += $"📦 Toplam {syncStats.SubModelsCount} alt model işlendi\n";
            statusMessage += $"⚙️ Toplam {syncStats.OptionsCount} classification option kaydedildi\n";
            statusMessage += $"🔧 Toplam {syncStats.EngineOptionsCount} engine option kaydedildi\n";
            statusMessage += $"🎨 Toplam {syncStats.CarBodyOptionsCount} car body option kaydedildi\n";
            statusMessage += $"\n⚡ Performans: Sadece modeller ve options kaydedildi (DotCompiledCodes, DotParts, DotVehicleImages atlandı)\n";
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Senkronizasyon Tamamlandı",
                Detail = $"{selectedManufacturer.Name} markası için tüm modeller senkronize edildi",
                Duration = 8000
            });
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Logger.LogError(ex, "Marka senkronizasyon hatası");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Senkronizasyon sırasında kritik hata oluştu",
                Duration = 5000
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private async Task SyncSelectedSubModelWithScope(DatVehicleSyncService scopedService, int vehicleTypeId, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        try
        {
            // 1. Classification gruplarını al
            var classificationGroups = await scopedService._datService.GetClassificationGroupsAsync(vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);

            if (classificationGroups?.ClassificationGroups == null || !classificationGroups.ClassificationGroups.Any())
            {
                statusMessage += $"    ❌ Classification grubu bulunamadı\n";
                return;
            }

            // 2. Her classification için seçenekleri topla ve kaydet
            var allOptionsWithClassification = new List<(string Classification, string OptionKey, string OptionValue)>();

            foreach (var classification in classificationGroups.ClassificationGroups)
            {
                try
                {
                    var options = await scopedService._datService.GetOptionsByClassificationAsync(vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, classification);

                    if (options?.Options?.Option != null && options.Options.Option.Any())
                    {
                        // DB'ye kaydet
                        await scopedService._dataService.SaveOptionsAsync(options.Options.Option, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, classification);
                        
                        lock (_statsLock)
                        {
                            syncStats.OptionsCount += options.Options.Option.Count;
                        }
                        
                        // Her classification'dan ilk option'ı al (en geçerli olanı)
                        foreach (var option in options.Options.Option.Take(1))
                        {
                            allOptionsWithClassification.Add((classification.ToString(), option.Key, option.Value));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Classification {Classification} options alınamadı", classification);
                }
            }

            // 3. Engine ve CarBody options'ı da al ve kaydet (geniş tarih aralığı ile)
            try
            {
                var engineOptions = await scopedService._datService.GetEngineOptionsAsync(
                    vehicleTypeId, manufacturerKey, baseModelKey, subModelKey,
                    constructionTimeFrom: "0001",
                    constructionTimeTo: "9912");
                await scopedService._dataService.SaveEngineOptionsAsync(engineOptions.EngineOptions.EngineOption, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);
                
                lock (_statsLock)
                {
                    syncStats.EngineOptionsCount += engineOptions.EngineOptions.EngineOption.Count;
                }
            }
            catch { }

            try
            {
                var carBodyOptions = await scopedService._datService.GetCarBodyOptionsAsync(
                    vehicleTypeId, manufacturerKey, baseModelKey, subModelKey,
                    constructionTimeFrom: "0001",
                    constructionTimeTo: "9912");
                await scopedService._dataService.SaveCarBodyOptionsAsync(carBodyOptions.CarBodyOptions.CarBodyOption, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);
                
                lock (_statsLock)
                {
                    syncStats.CarBodyOptionsCount += carBodyOptions.CarBodyOptions.CarBodyOption.Count;
                }
            }
            catch { }

            // 4. CompiledCode, Parts ve Images aktarımı
            try
            {
                // Toplanan tüm option key'leri al
                var allOptionKeys = allOptionsWithClassification.Select(o => o.OptionKey).ToList();
                
                // DAT API genellikle 3-4 option ister ama boş liste veya daha az ile de deneyelim
                if (allOptionKeys.Count > 0 || true) // Her durumda dene (boş liste bile)
                {
                    // DatECode derle
                    var compileResult = await scopedService._datService.CompileDatECodeAsync(
                        vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, allOptionKeys);
                    
                    if (!string.IsNullOrWhiteSpace(compileResult.DatECode))
                    {
                        // CompiledCode'u kaydet
                        await scopedService._dataService.SaveCompiledCodeAsync(
                            compileResult.DatECode, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, 
                            allOptionKeys);
                        
                        lock (_statsLock)
                        {
                            syncStats.CompiledCodesCount++;
                        }
                        
                        await InvokeAsync(() =>
                        {
                            statusMessage += $"    🔍 DatECode: {compileResult.DatECode}\n";
                            StateHasChanged();
                        });
                        
                        // Parts getir ve kaydet
                        try
                        {
                            var parts = await scopedService._datService.SearchPartsAsync(
                                vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);
                            if (parts?.Parts?.Count > 0)
                            {
                                await scopedService._dataService.SavePartsAsync(parts.Parts, compileResult.DatECode);
                                
                                lock (_statsLock)
                                {
                                    syncStats.PartsCount += parts.Parts.Count;
                                }
                                
                                await InvokeAsync(() =>
                                {
                                    statusMessage += $"    ✅ {parts.Parts.Count} parça kaydedildi\n";
                                    StateHasChanged();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Parts hatası");
                        }
                        
                        // Images getir ve kaydet
                        try
                        {
                            var images = await scopedService._datService.GetVehicleImagesAsync(compileResult.DatECode);
                            if (images?.Images?.Count > 0)
                            {
                                await scopedService._dataService.SaveVehicleImagesAsync(compileResult.DatECode, images.Images);
                                
                                lock (_statsLock)
                                {
                                    syncStats.ImagesCount += images.Images.Count;
                                }
                                
                                await InvokeAsync(() =>
                                {
                                    statusMessage += $"    🖼️ {images.Images.Count} görsel kaydedildi\n";
                                    StateHasChanged();
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Images hatası");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "CompiledCode/Parts/Images hatası");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "SyncSelectedSubModelWithScope hatası");
        }
    }

    private async Task SyncSelectedSubModel(int vehicleTypeId, string manufacturerKey, string baseModelKey, string subModelKey)
    {
        try
        {
            // 1. Classification gruplarını al
            var classificationGroups = await SyncService._datService.GetClassificationGroupsAsync(vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);

            if (classificationGroups?.ClassificationGroups == null || !classificationGroups.ClassificationGroups.Any())
            {
                statusMessage += $"    ❌ Classification grubu bulunamadı\n";
                return;
            }

            // 2. Her classification için seçenekleri topla ve kaydet
            var allOptionsWithClassification = new List<(string Classification, string OptionKey, string OptionValue)>();

            foreach (var classification in classificationGroups.ClassificationGroups)
            {
                try
                {
                    var options = await SyncService._datService.GetOptionsByClassificationAsync(vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, classification);

                    if (options?.Options?.Option != null && options.Options.Option.Any())
                    {
                        // DB'ye kaydet
                        await SyncService._dataService.SaveOptionsAsync(options.Options.Option, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, classification);
                        
                        lock (_statsLock)
                        {
                            syncStats.OptionsCount += options.Options.Option.Count;
                        }
                        
                        // Her classification'dan ilk option'ı al (en geçerli olanı)
                        foreach (var option in options.Options.Option.Take(1))
                        {
                            allOptionsWithClassification.Add((classification.ToString(), option.Key, option.Value));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "Classification {Classification} options alınamadı", classification);
                }
            }

            // 3. Engine ve CarBody options'ı da al ve kaydet (geniş tarih aralığı ile)
            try
            {
                var engineOptions = await SyncService._datService.GetEngineOptionsAsync(
                    vehicleTypeId, manufacturerKey, baseModelKey, subModelKey,
                    constructionTimeFrom: "0001",
                    constructionTimeTo: "9912");
                await SyncService._dataService.SaveEngineOptionsAsync(engineOptions.EngineOptions.EngineOption, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);
                
                lock (_statsLock)
                {
                    syncStats.EngineOptionsCount += engineOptions.EngineOptions.EngineOption.Count;
                }
            }
            catch { }

            try
            {
                var carBodyOptions = await SyncService._datService.GetCarBodyOptionsAsync(
                    vehicleTypeId, manufacturerKey, baseModelKey, subModelKey,
                    constructionTimeFrom: "0001",
                    constructionTimeTo: "9912");
                await SyncService._dataService.SaveCarBodyOptionsAsync(carBodyOptions.CarBodyOptions.CarBodyOption, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);
                
                lock (_statsLock)
                {
                    syncStats.CarBodyOptionsCount += carBodyOptions.CarBodyOptions.CarBodyOption.Count;
                }
            }
            catch { }

            // 4. CompiledCode, Parts ve Images aktarımı
            try
            {
                // Toplanan tüm option key'leri al
                var allOptionKeys = allOptionsWithClassification.Select(o => o.OptionKey).ToList();
                
                // DAT API genellikle 3-4 option ister ama boş liste veya daha az ile de deneyelim
                if (allOptionKeys.Count > 0 || true) // Her durumda dene (boş liste bile)
                {
                    // DatECode derle
                    var compileResult = await SyncService._datService.CompileDatECodeAsync(
                        vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, allOptionKeys);
                    
                    if (!string.IsNullOrWhiteSpace(compileResult.DatECode))
                    {
                        // CompiledCode'u kaydet
                        await SyncService._dataService.SaveCompiledCodeAsync(
                            compileResult.DatECode, vehicleTypeId, manufacturerKey, baseModelKey, subModelKey, 
                            allOptionKeys);
                        
                        lock (_statsLock)
                        {
                            syncStats.CompiledCodesCount++;
                        }
                        
                        statusMessage += $"    🔍 DatECode: {compileResult.DatECode}\n";
                        StateHasChanged();
                        
                        // Parts getir ve kaydet
                        try
                        {
                            var parts = await SyncService._datService.SearchPartsAsync(
                                vehicleTypeId, manufacturerKey, baseModelKey, subModelKey);
                            if (parts?.Parts?.Count > 0)
                            {
                                await SyncService._dataService.SavePartsAsync(parts.Parts, compileResult.DatECode);
                                
                                lock (_statsLock)
                                {
                                    syncStats.PartsCount += parts.Parts.Count;
                                }
                                
                                statusMessage += $"    ✅ {parts.Parts.Count} parça kaydedildi\n";
                                StateHasChanged();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Parts hatası");
                        }
                        
                        // Images getir ve kaydet
                        try
                        {
                            var images = await SyncService._datService.GetVehicleImagesAsync(compileResult.DatECode);
                            if (images?.Images?.Count > 0)
                            {
                                await SyncService._dataService.SaveVehicleImagesAsync(compileResult.DatECode, images.Images);
                                
                                lock (_statsLock)
                                {
                                    syncStats.ImagesCount += images.Images.Count;
                                }
                                
                                statusMessage += $"    🖼️ {images.Images.Count} görsel kaydedildi\n";
                                StateHasChanged();
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning(ex, "Images hatası");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "CompiledCode/Parts/Images hatası");
            }
        }
        catch (Exception ex)
        {
            statusMessage += $"    ❌ Hata: {ex.Message}\n";
            Logger.LogError(ex, "SyncSelectedSubModel hatası");
        }
    }

    private async Task SyncAllVehicleTypes()
    {
        isRunning = true;
        statusMessage = string.Empty;
        errorMessage = string.Empty;
        syncStats = new SyncStatistics();
        StateHasChanged();

        try
        {
            statusMessage = "🚀 DAT API'den araç tipleri alınıyor...\n\n";
            StateHasChanged();

            // 1. DAT API'den araç tiplerini çek
            var vehicleTypesResponse = await SyncService._datService.GetVehicleTypesAsync();
            
            if (vehicleTypesResponse?.VehicleTypes?.VehicleType == null || !vehicleTypesResponse.VehicleTypes.VehicleType.Any())
            {
                errorMessage = "DAT API'den araç tipi alınamadı!";
                return;
            }

            // 2. Araç tiplerini database'e kaydet
            await SyncService._dataService.SaveVehicleTypesAsync(vehicleTypesResponse.VehicleTypes.VehicleType);
            syncStats.VehicleTypesCount = vehicleTypesResponse.VehicleTypes.VehicleType.Count;
            statusMessage += $"✅ {vehicleTypesResponse.VehicleTypes.VehicleType.Count} araç tipi database'e kaydedildi!\n\n";
            StateHasChanged();

            int totalSuccess = 0;
            int totalFailed = 0;

            // 3. Her araç tipi için cascade senkronizasyon yap
            foreach (var vehicleType in vehicleTypesResponse.VehicleTypes.VehicleType)
            {
                try
                {
                    statusMessage += $"📊 [{vehicleType.Key}] {vehicleType.Value} için tam senkronizasyon başlatılıyor...\n";
                    StateHasChanged();

                    // Manufacturers
                    var manufacturers = await SyncService._datService.GetManufacturersAsync(int.Parse(vehicleType.Key));
                    await SyncService._dataService.SaveManufacturersAsync(manufacturers.Manufacturers.Manufacturer, int.Parse(vehicleType.Key));
                    syncStats.ManufacturersCount += manufacturers.Manufacturers.Manufacturer.Count;
                    statusMessage += $"  └─ ✅ {manufacturers.Manufacturers.Manufacturer.Count} üretici kaydedildi\n";
                    StateHasChanged();

                    // Her üretici için base models ve alt veriler
                    foreach (var manufacturer in manufacturers.Manufacturers.Manufacturer)
                    {
                        try
                        {
                            var baseModels = await SyncService._datService.GetBaseModelsNAsync(int.Parse(vehicleType.Key), manufacturer.Key);
                            await SyncService._dataService.SaveBaseModelsAsync(baseModels.BaseModels.BaseModel, int.Parse(vehicleType.Key), manufacturer.Key);
                            syncStats.BaseModelsCount += baseModels.BaseModels.BaseModel.Count;
                            statusMessage += $"  └─ {manufacturer.Value}: {baseModels.BaseModels.BaseModel.Count} ana model\n";
                            StateHasChanged();

                            // Her base model için sub models
                            foreach (var baseModel in baseModels.BaseModels.BaseModel)
                            {
                                try
                                {
                                    var subModels = await SyncService._datService.GetSubModelsAsync(int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key);
                                    if (subModels.SubModels.SubModel.Any())
                                    {
                                        await SyncService._dataService.SaveSubModelsAsync(subModels.SubModels.SubModel, int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key);
                                        syncStats.SubModelsCount += subModels.SubModels.SubModel.Count;
                                        
                                        // Her sub model için options (Parts atlandı - performans)
                                        foreach (var subModel in subModels.SubModels.SubModel)
                                        {
                                            try
                                            {
                                                var selectedOptionKeys = new List<string>();
                                                
                                                var classificationGroups = await SyncService._datService.GetClassificationGroupsAsync(
                                                    int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key);
                                                
                                                foreach (var classification in classificationGroups.ClassificationGroups)
                                                {
                                                    try
                                                    {
                                                        var options = await SyncService._datService.GetOptionsByClassificationAsync(
                                                            int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key, classification);
                                                        await SyncService._dataService.SaveOptionsAsync(
                                                            options.Options.Option, int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key, classification);
                                                        syncStats.OptionsCount += options.Options.Option.Count;
                                                        
                                                        // İlk 4 opsiyonu topla (compileDatECode için)
                                                        if (selectedOptionKeys.Count < 4 && options.Options.Option.Any())
                                                        {
                                                            selectedOptionKeys.Add(options.Options.Option.First().Key);
                                                        }
                                                    }
                                                    catch { }
                                                }

                                                // Engine options
                                                try
                                                {
                                                    var engineOptions = await SyncService._datService.GetEngineOptionsAsync(
                                                        int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key,
                                                        constructionTimeFrom: "0001",
                                                        constructionTimeTo: "9912");
                                                    await SyncService._dataService.SaveEngineOptionsAsync(
                                                        engineOptions.EngineOptions.EngineOption, int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key);
                                                    syncStats.EngineOptionsCount += engineOptions.EngineOptions.EngineOption.Count;
                                                }
                                                catch { }

                                                // Car body options
                                                try
                                                {
                                                    var carBodyOptions = await SyncService._datService.GetCarBodyOptionsAsync(
                                                        int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key,
                                                        constructionTimeFrom: "0001",
                                                        constructionTimeTo: "9912");
                                                    await SyncService._dataService.SaveCarBodyOptionsAsync(
                                                        carBodyOptions.CarBodyOptions.CarBodyOption, int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key);
                                                    syncStats.CarBodyOptionsCount += carBodyOptions.CarBodyOptions.CarBodyOption.Count;
                                                }
                                                catch { }
                                                
                                                // CompiledCode, Parts ve Images aktarımı
                                                // DAT API genellikle 3-4 option ister ama daha az ile de deneyelim
                                                if (selectedOptionKeys.Count > 0 || true) // Her durumda dene
                                                {
                                                    try
                                                    {
                                                        var compileResult = await SyncService._datService.CompileDatECodeAsync(
                                                            int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key, selectedOptionKeys);
                                                        
                                                        if (!string.IsNullOrWhiteSpace(compileResult.DatECode))
                                                        {
                                                            // CompiledCode kaydet
                                                            await SyncService._dataService.SaveCompiledCodeAsync(
                                                                compileResult.DatECode, int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key,
                                                                selectedOptionKeys);
                                                            
                                                            // Parts kaydet
                                                            try
                                                            {
                                                                var parts = await SyncService._datService.SearchPartsAsync(
                                                                    int.Parse(vehicleType.Key), manufacturer.Key, baseModel.Key, subModel.Key);
                                                                if (parts?.Parts?.Count > 0)
                                                                {
                                                                    await SyncService._dataService.SavePartsAsync(parts.Parts, compileResult.DatECode);
                                                                }
                                                            }
                                                            catch { }
                                                            
                                                            // Images kaydet
                                                            try
                                                            {
                                                                var images = await SyncService._datService.GetVehicleImagesAsync(compileResult.DatECode);
                                                                if (images?.Images?.Count > 0)
                                                                {
                                                                    await SyncService._dataService.SaveVehicleImagesAsync(compileResult.DatECode, images.Images);
                                                                }
                                                            }
                                                            catch { }
                                                        }
                                                    }
                                                    catch { }
                                                }
                                            }
                                            catch { }
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                        catch { }
                        
                        StateHasChanged();
                    }

                    statusMessage += $"✅ [{vehicleType.Key}] {vehicleType.Value} başarıyla tamamlandı!\n\n";
                    totalSuccess++;
                }
                catch (Exception ex)
                {
                    statusMessage += $"❌ [{vehicleType.Key}] {vehicleType.Value} için hata: {ex.Message}\n\n";
                    Logger.LogError(ex, "Araç tipi {VehicleType} senkronizasyon hatası", vehicleType.Key);
                    totalFailed++;
                }
                
                StateHasChanged();
            }

            statusMessage += $"\n🎉 TÜM SENKRONIZASYON TAMAMLANDI!\n";
            statusMessage += $"✅ Başarılı: {totalSuccess} araç tipi\n";
            if (totalFailed > 0)
            {
                statusMessage += $"❌ Başarısız: {totalFailed} araç tipi\n";
            }
            statusMessage += $"\n📊 GENEL İSTATİSTİKLER:\n";
            statusMessage += $"  📋 {syncStats.VehicleTypesCount} araç tipi\n";
            statusMessage += $"  🏭 {syncStats.ManufacturersCount} üretici\n";
            statusMessage += $"  🚗 {syncStats.BaseModelsCount} ana model\n";
            statusMessage += $"  📦 {syncStats.SubModelsCount} alt model\n";
            statusMessage += $"  🚘 {syncStats.VehiclesCount} araç kaydı\n";
            statusMessage += $"  ⚙️ {syncStats.OptionsCount} opsiyon\n";
            statusMessage += $"  🔧 {syncStats.EngineOptionsCount} motor opsiyonu\n";
            statusMessage += $"  🎨 {syncStats.CarBodyOptionsCount} kasa opsiyonu\n";
            statusMessage += $"\n📊 PARÇA İSTATİSTİKLERİ:\n";
            statusMessage += $"  ⚡ Performans: Parts ve Images atlandı (ultra hızlı mod)\n";
            
            statusMessage += $"\n💡 BİLGİ:\n";
            statusMessage += $"  • Senkronizasyon sırasında bazı araçlar için DAT API'sinde parça bilgisi bulunmayabilir.\n";
            statusMessage += $"  • Bu durum özellikle eski model veya özel donanımlı araçlarda normaldir.\n";
            statusMessage += $"  • Parça bulunamayan araçlar için DAT farklı konfigürasyonlar denenmektedir.\n";

            NotificationService.Notify(new NotificationMessage
            {
                Severity = totalFailed == 0 ? NotificationSeverity.Success : NotificationSeverity.Warning,
                Summary = "Senkronizasyon Tamamlandı",
                Detail = $"{totalSuccess} araç tipi başarıyla senkronize edildi" + (totalFailed > 0 ? $", {totalFailed} başarısız" : ""),
                Duration = 8000
            });
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Logger.LogError(ex, "Genel senkronizasyon hatası");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Senkronizasyon sırasında kritik hata oluştu",
                Duration = 5000
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private async Task BulkSyncAllVehicleTypes()
    {
        isRunning = true;
        statusMessage = string.Empty;
        errorMessage = string.Empty;
        syncStats = new SyncStatistics();
        StateHasChanged();

        try
        {
            statusMessage = "🚀 BULK SENKRONIZASYON BAŞLATILIYOR...\n";
            statusMessage += "⚡ Bu yöntem çok daha hızlıdır - Paralel işlem ve bulk insert kullanır!\n\n";
            StateHasChanged();

            var startTime = DateTime.Now;
            var cancellationTokenSource = new CancellationTokenSource();
            
            // Progress tracking için timer
            var progressTimer = new Timer(async _ =>
            {
                var elapsed = DateTime.Now - startTime;
                await InvokeAsync(() =>
                {
                    // En sona yeni satır eklemek yerine mevcut satırı güncelle
                    // Son satırı bul ve değiştir
                    var lines = statusMessage.Split('\n').ToList();
                    var idx = lines.FindLastIndex(l => l.TrimStart().StartsWith("⏱️ Geçen süre:"));
                    if (idx >= 0)
                    {
                        lines[idx] = $"⏱️ Geçen süre: {elapsed:mm\\:ss}";
                        statusMessage = string.Join("\n", lines);
                    }
                    else
                    {
                        statusMessage += $"⏱️ Geçen süre: {elapsed:mm\\:ss}\n";
                    }
                    StateHasChanged();
                });
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));

            try
            {
                var progress = new Progress<BulkSyncResult>(async (partialResult) =>
                {
                    Console.WriteLine($"🔄 Progress callback called! VehicleTypes: {partialResult.VehicleTypesCount}, Manufacturers: {partialResult.ManufacturersCount}, BaseModels: {partialResult.BaseModelsCount}");
                    await InvokeAsync(() =>
                    {
                        // Ara sonuçları UI'ya yansıt - sadece 0'dan büyük olanları güncelle
                        if (partialResult.VehicleTypesCount > 0)
                            syncStats.VehicleTypesCount = partialResult.VehicleTypesCount;
                        if (partialResult.ManufacturersCount > 0)
                            syncStats.ManufacturersCount = Math.Max(syncStats.ManufacturersCount, partialResult.ManufacturersCount);
                        if (partialResult.BaseModelsCount > 0)
                            syncStats.BaseModelsCount = Math.Max(syncStats.BaseModelsCount, partialResult.BaseModelsCount);
                        if (partialResult.SubModelsCount > 0)
                            syncStats.SubModelsCount = Math.Max(syncStats.SubModelsCount, partialResult.SubModelsCount);
                        if (partialResult.VehiclesCount > 0)
                            syncStats.VehiclesCount = Math.Max(syncStats.VehiclesCount, partialResult.VehiclesCount);
                        if (partialResult.OptionsCount > 0)
                            syncStats.OptionsCount = Math.Max(syncStats.OptionsCount, partialResult.OptionsCount);
                        if (partialResult.EngineOptionsCount > 0)
                            syncStats.EngineOptionsCount = Math.Max(syncStats.EngineOptionsCount, partialResult.EngineOptionsCount);
                        if (partialResult.CarBodyOptionsCount > 0)
                            syncStats.CarBodyOptionsCount = Math.Max(syncStats.CarBodyOptionsCount, partialResult.CarBodyOptionsCount);
                        
                        Console.WriteLine($"🔄 UI Updated! VehicleTypes: {syncStats.VehicleTypesCount}, Manufacturers: {syncStats.ManufacturersCount}, BaseModels: {syncStats.BaseModelsCount}");
                        StateHasChanged();
                    });
                });

                var result = await BulkSyncService.BulkSyncAllVehicleTypesAsync(
                    cancellationToken: cancellationTokenSource.Token,
                    progress: progress);

                progressTimer.Dispose();

                // Sonuçları syncStats'a aktar
                syncStats.VehicleTypesCount = result.VehicleTypesCount;
                syncStats.ManufacturersCount = result.ManufacturersCount;
                syncStats.BaseModelsCount = result.BaseModelsCount;
                syncStats.SubModelsCount = result.SubModelsCount;
                syncStats.VehiclesCount = result.VehiclesCount;
                syncStats.OptionsCount = result.OptionsCount;
                syncStats.EngineOptionsCount = result.EngineOptionsCount;
                syncStats.CarBodyOptionsCount = result.CarBodyOptionsCount;

                statusMessage += $"\n🎉 BULK SENKRONIZASYON TAMAMLANDI!\n";
                statusMessage += $"⏱️ Toplam süre: {result.TotalDuration:mm\\:ss}\n";
                
                if (result.HasErrors)
                {
                    statusMessage += $"⚠️ Bazı işlemler sırasında hatalar oluştu\n";
                }

                statusMessage += $"\n📊 GENEL İSTATİSTİKLER:\n";
                statusMessage += $"  📋 {result.VehicleTypesCount} araç tipi\n";
                statusMessage += $"  🏭 {result.ManufacturersCount} üretici\n";
                statusMessage += $"  🚗 {result.BaseModelsCount} ana model\n";
                statusMessage += $"  📦 {result.SubModelsCount} alt model\n";
                statusMessage += $"  🚘 {result.VehiclesCount} araç kaydı\n";
                statusMessage += $"  ⚙️ {result.OptionsCount} opsiyon\n";
                statusMessage += $"  🔧 {result.EngineOptionsCount} motor opsiyonu\n";
                statusMessage += $"  🎨 {result.CarBodyOptionsCount} kasa opsiyonu\n";
                statusMessage += $"\n📊 PARÇA İSTATİSTİKLERİ:\n";
                statusMessage += $"  ✅ Toplam {result.PartsCount} parça kaydedildi\n";
                statusMessage += $"  ✅ {result.SubModelsWithParts} alt model için parça bulundu\n";
                statusMessage += $"  ⚠️ {result.SubModelsWithoutParts} alt model için parça bulunamadı (DAT'ta veri yok olabilir)\n";
                if (result.SubModelsWithErrors > 0)
                {
                    statusMessage += $"  ❌ {result.SubModelsWithErrors} alt model için hata oluştu\n";
                }
                
                statusMessage += $"\n💡 BULK SENKRONIZASYON AVANTAJLARI:\n";
                statusMessage += $"  • ⚡ Paralel API çağrıları (10x daha hızlı)\n";
                statusMessage += $"  • 🔄 Batch database operations\n";
                statusMessage += $"  • 🛡️ Rate limiting koruması\n";
                statusMessage += $"  • 📊 Gerçek zamanlı progress tracking\n";

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = result.HasErrors ? NotificationSeverity.Warning : NotificationSeverity.Success,
                    Summary = "Bulk Senkronizasyon Tamamlandı",
                    Detail = $"{result.PartsCount} parça, {result.SubModelsCount} alt model işlendi - Süre: {result.TotalDuration:mm\\:ss}",
                    Duration = 10000
                });
            }
            catch (OperationCanceledException)
            {
                progressTimer.Dispose();
                statusMessage += "\n⏹️ İşlem kullanıcı tarafından iptal edildi.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Logger.LogError(ex, "Bulk senkronizasyon hatası");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Bulk senkronizasyon sırasında kritik hata oluştu",
                Duration = 5000
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }




    private async Task SeedDatDataTable()
    {
        isRunning = true;
        statusMessage = "🌱 DatData tablosundan parçalar çekiliyor...\n\n";
        errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            // ÖNCELİKLE: Sadece UNIQUE VT-M-B kombinasyonlarını al (4M kayıt yerine sadece grupları)
            List<(int VehicleTypeKey, int ManufactureKey, int BaseModelKey)> uniqueGroups;
            using (var scope = _scopeFactory.CreateScope())
            {
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                uniqueGroups = await dataService.GetUniqueDatDataGroupsAsync();
            }
            
            if (!uniqueGroups.Any())
            {
                errorMessage = "⚠️ DatData tablosu boş! Önce veri eklemeniz gerekiyor.";
                isRunning = false;
                StateHasChanged();
                return;
            }

            statusMessage += $"📊 DatData tablosunda {uniqueGroups.Count} UNIQUE araç grubu bulundu (4M kayıt yerine sadece gruplar)\n\n";
            StateHasChanged();

            int totalPartsAdded = 0;

            foreach (var group in uniqueGroups)
            {
                var vt = group.VehicleTypeKey;
                var m = group.ManufactureKey;
                var b = group.BaseModelKey;
                
                // Bu gruptaki tüm DatProcessNo'ları al (sadece bu grup için)
                List<string> datProcessNos;
                using (var scope = _scopeFactory.CreateScope())
                {
                    var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                    var processNos = await dataService.GetDatProcessNumbersByVehicleAsync(vt, m, b);
                    datProcessNos = processNos.Select(p => p.ToString()).ToList();
                }

                statusMessage += $"🔍 Araç: VT:{vt} M:{m} B:{b}\n";
                statusMessage += $"   📋 {datProcessNos.Count} DatProcessNo bulundu\n";
                
                if (datProcessNos.Count == 0)
                {
                    statusMessage += $"   ⚠️ Hiç DatProcessNo yok, atlanıyor...\n";
                    StateHasChanged();
                    continue;
                }
                
                StateHasChanged();

                // DAT API LİMİTİ: Sadece 50 DatProcessNo kabul ediyor!
                // 50'şerli batch'lere böl
                const int batchSize = 50;
                var batches = datProcessNos
                    .Select((value, index) => new { value, index })
                    .GroupBy(x => x.index / batchSize)
                    .Select(g => g.Select(x => x.value).ToList())
                    .ToList();

                statusMessage += $"   📦 {batches.Count} batch'e bölündü (her batch max 50 DPN)\n";
                StateHasChanged();

                int groupPartsAdded = 0;

                // 🚀 PARALEL BATCH İŞLEME (max 2 paralel - API'yi yormayalım)
                var semaphore = new SemaphoreSlim(2, 2); // Max 2 batch aynı anda (API'yi yormayalım)
                var batchTasks = new List<Task>();
                var batchResults = new System.Collections.Concurrent.ConcurrentBag<(int batchIndex, int partsCount, string status)>();

                for (int i = 0; i < batches.Count; i++)
                {
                    var batchIndex = i;
                    var batch = batches[i];
                    
                    var task = Task.Run(async () =>
                    {
                        await semaphore.WaitAsync();
                        try
                        {
                            // 🔧 Her task kendi scope'unu oluşturmalı (paralel için)
                            using (var scope = _scopeFactory.CreateScope())
                            {
                                var datService = scope.ServiceProvider.GetRequiredService<IDatService>();
                                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                                
                                // DAT API'den parça ara (batch ile)
                                var partsResult = await datService.SearchPartsAsync(
                                    vt,
                                    m.ToString(),
                                    b.ToString(),
                                    "1", // SubModel - DatData'da yok, default kullan
                                    null, // selectedOptions
                                    batch); // 50'li batch

                                if (partsResult.Parts != null && partsResult.Parts.Any())
                                {
                                    // DotParts tablosuna kaydet (kendi scope'u ile)
                                    await dataService.SavePartsAsync(partsResult.Parts);
                                    
                                    // 🎯 Bu batch'teki DatProcessNo'ları IsTrans = true yap
                                    await dataService.MarkDatDataAsTransferredAsync(vt, m, b, batch);
                                    
                                    batchResults.Add((batchIndex, partsResult.Parts.Count, "✅ Success"));
                                    Logger.LogInformation("✅ Batch {Index}/{Total}: {Count} parça kaydedildi", 
                                        batchIndex + 1, batches.Count, partsResult.Parts.Count);
                                }
                                else
                                {
                                    batchResults.Add((batchIndex, 0, "⚠️ No parts"));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            batchResults.Add((batchIndex, 0, $"❌ Error: {ex.Message}"));
                            Logger.LogError(ex, "Batch parça çekme hatası VT:{VT} M:{M} B:{B} Batch:{BatchIndex}", 
                                vt, m, b, batchIndex + 1);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    });
                    
                    batchTasks.Add(task);
                    
                    // Batch'ler arası kısa delay ekle (API'yi rahatlatmak için)
                    if (i > 0 && i % 2 == 0) // Her 2 batch'te bir
                    {
                        await Task.Delay(2000); // 2 saniye delay (API'yi rahatlatır - 500 hatalarını azaltır)
                    }
                }

                // Tüm batch'lerin bitmesini bekle (max 20 dakika timeout - API yavaş olabilir)
                statusMessage += $"   ⏳ {batches.Count} batch işleniyor (max 20 dakika bekleniyor)...\n";
                StateHasChanged();
                
                var completedTask = await Task.WhenAny(
                    Task.WhenAll(batchTasks),
                    Task.Delay(TimeSpan.FromMinutes(20))
                );
                
                var completedCount = batchResults.Count;
                var totalCount = batches.Count;
                
                if (completedCount == totalCount)
                {
                    // Tüm batch'ler tamamlandı
                    statusMessage += $"✅ Tüm batch'ler başarıyla tamamlandı ({completedCount}/{totalCount})\n";
                    Logger.LogInformation("Tüm batch'ler başarıyla tamamlandı: {Completed}/{Total}", completedCount, totalCount);
                }
                else if (completedTask == Task.Delay(TimeSpan.FromMinutes(20)))
                {
                    // Gerçek timeout
                    statusMessage += $"⚠️ Timeout! {completedCount}/{totalCount} batch tamamlandı (20 dakika aşıldı)\n";
                    statusMessage += $"   📊 Tamamlanan: {completedCount}, Kalan: {totalCount - completedCount}\n";
                    Logger.LogWarning("Batch işleme timeout oldu - {Completed}/{Total} batch tamamlandı", completedCount, totalCount);
                }
                else
                {
                    // Task.WhenAll tamamlandı ama batchResults eksik (garip durum)
                    statusMessage += $"✅ Batch'ler tamamlandı ama sonuç sayısı eksik: {completedCount}/{totalCount}\n";
                    Logger.LogWarning("Batch'ler tamamlandı ama sonuç sayısı eksik: {Completed}/{Total}", completedCount, totalCount);
                }
                
                // Sonuçları topla ve göster
                var sortedResults = batchResults.OrderBy(r => r.batchIndex).ToList();
                foreach (var result in sortedResults)
                {
                    statusMessage += $"   Batch {result.batchIndex + 1}/{batches.Count}: {result.status}";
                    if (result.partsCount > 0)
                        statusMessage += $" ({result.partsCount} parça)\n";
                    else
                        statusMessage += "\n";
                    
                    groupPartsAdded += result.partsCount;
                }

                totalPartsAdded += groupPartsAdded;
                statusMessage += $"   🏁 Araç grubu tamamlandı: {groupPartsAdded} parça eklendi\n";
                statusMessage += "\n";
                StateHasChanged();
            }

            statusMessage += $"\n🎉 İŞLEM TAMAMLANDI!\n";
            statusMessage += $"📊 Toplam {totalPartsAdded} parça DotParts tablosuna eklendi\n";

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Seed Başarılı",
                Detail = $"{totalPartsAdded} parça kaydedildi",
                Duration = 5000
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"❌ HATA: {ex.Message}\n\n{ex.StackTrace}";
            Logger.LogError(ex, "DatData seed hatası");
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = ex.Message,
                Duration = 5000
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private class SyncStatistics
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
        public int ImagesCount { get; set; }
        public int CompiledCodesCount { get; set; }
    }

    private async Task SyncMissingImages()
    {
        isRunning = true;
        statusMessage = "🖼️ DotCompiledCodes tablosundan eksik image'lar çekiliyor...\n\n";
        errorMessage = string.Empty;
        StateHasChanged();

        try
        {
            // DotCompiledCodes tablosundan tüm DatECode'ları al
            List<string> allDatECodes;
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                allDatECodes = await dbContext.Set<DotCompiledCode>()
                    .Select(c => c.DatECode)
                    .Distinct()
                    .ToListAsync();
            }
            
            if (!allDatECodes.Any())
            {
                errorMessage = "⚠️ DotCompiledCodes tablosu boş!";
                isRunning = false;
                StateHasChanged();
                return;
            }

            statusMessage += $"📊 {allDatECodes.Count} DatECode bulundu\n\n";
            StateHasChanged();

            int totalImagesFetched = 0;
            int alreadyExists = 0;
            int errorCount = 0;

            foreach (var datECode in allDatECodes)
            {
                try
                {
                    // Bu DatECode için image var mı kontrol et
                    bool imageExists;
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        imageExists = await dbContext.Set<DotVehicleImage>()
                            .AnyAsync(i => i.DatECode == datECode);
                    }

                    if (imageExists)
                    {
                        alreadyExists++;
                        Logger.LogDebug("Image already exists for {DatECode}", datECode);
                        continue;
                    }

                    // Image yok, çek
                    statusMessage += $"🖼️ Fetching images for {datECode}...\n";
                    StateHasChanged();

                    var imagesResult = await SyncService._datService.GetVehicleImagesAsync(datECode);

                    if (imagesResult?.Images != null && imagesResult.Images.Any())
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();
                            await dataService.SaveVehicleImagesAsync(datECode, imagesResult.Images);
                        }

                        totalImagesFetched += imagesResult.Images.Count;
                        statusMessage += $"   ✅ {imagesResult.Images.Count} image kaydedildi\n";
                    }
                    else
                    {
                        statusMessage += $"   ℹ️ No images found\n";
                    }

                    StateHasChanged();
                    await Task.Delay(100); // Rate limiting
                }
                catch (Exception ex)
                {
                    errorCount++;
                    statusMessage += $"   ❌ Error: {ex.Message}\n";
                    Logger.LogError(ex, "Error fetching images for {DatECode}", datECode);
                    StateHasChanged();
                }
            }

            statusMessage += $"\n🎉 İŞLEM TAMAMLANDI!\n";
            statusMessage += $"📊 Toplam {totalImagesFetched} yeni image eklendi\n";
            statusMessage += $"✅ Zaten mevcut: {alreadyExists}\n";
            statusMessage += $"❌ Hata: {errorCount}\n";

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Image Sync Başarılı",
                Detail = $"{totalImagesFetched} yeni image eklendi!"
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Genel Hata: {ex.Message}";
            Logger.LogError(ex, "SyncMissingImages sırasında hata oluştu");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Image Sync Hatası",
                Detail = ex.Message
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private async Task BulkSyncConstructionPeriods()
    {
        isRunning = true;
        errorMessage = string.Empty;
        statusMessage = "🕐 BULK ConstructionPeriods Sync başlatılıyor...\n";
        StateHasChanged();

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Toplam kayıt sayısını al
                var totalCount = await dbContext.Set<DotCompiledCode>()
                    .Where(c => !string.IsNullOrEmpty(c.DatECode))
                    .CountAsync();

                statusMessage += $"📊 Toplam {totalCount} adet DatECode bulundu\n";
                statusMessage += $"⚙️  Batch boyutu: 1000 kayıt\n\n";
                StateHasChanged();

                int successCount = 0;
                int errorCount = 0;
                int skippedCount = 0;
                int batchNumber = 0;
                int batchSize = 1000;

                for (int skip = 0; skip < totalCount; skip += batchSize)
                {
                    batchNumber++;
                    statusMessage += $"\n━━━ BATCH {batchNumber} ━━━\n";
                    StateHasChanged();

                    var batch = await dbContext.Set<DotCompiledCode>()
                        .Where(c => !string.IsNullOrEmpty(c.DatECode))
                        .OrderBy(c => c.Id)
                        .Skip(skip)
                        .Take(batchSize)
                        .Select(c => c.DatECode)
                        .ToListAsync();

                    // Paralel akış: aynı anda 4 istek (DB connection contention önlemek için)
                    var throttler = new SemaphoreSlim(4);
                    var tasks = batch.Select(async datECode =>
                    {
                        await throttler.WaitAsync();
                        try
                        {
                            // Her görev kendi DbContext'ini kullanmalı
                            using var innerScope = _scopeFactory.CreateScope();
                            var innerDb = innerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                            var ct = await SyncService._datService.GetConstructionPeriodsInfoAsync(datECode);
                            if (ct == null)
                            {
                                Interlocked.Increment(ref errorCount);
                                Logger.LogWarning("ConstructionPeriods API null döndü: {DatECode}", datECode);
                                return;
                            }

                            // Mevcut kayıt var mı kontrol et
                            var existing = await innerDb.DotConstructionPeriods.FirstOrDefaultAsync(cp => cp.DatECode == datECode);
                            
                            if (existing != null)
                            {
                                // Güncelle (UPDATE)
                                existing.ConstructionTimeMin = ct.Min ?? ct.Current ?? string.Empty;
                                existing.ConstructionTimeMax = ct.Max ?? ct.Current ?? string.Empty;
                                existing.CurrentConstructionTime = ct.Current ?? ct.Max ?? ct.Min ?? string.Empty;
                                existing.YearMin = ct.YearMin;
                                existing.YearMax = ct.YearMax;
                                existing.LastUpdatedDate = DateTime.UtcNow;
                                innerDb.DotConstructionPeriods.Update(existing);
                            }
                            else
                            {
                                // Yeni ekle (INSERT)
                                var newPeriod = new DotConstructionPeriod
                                {
                                    DatECode = datECode,
                                    ConstructionTimeMin = ct.Min ?? ct.Current ?? string.Empty,
                                    ConstructionTimeMax = ct.Max ?? ct.Current ?? string.Empty,
                                    CurrentConstructionTime = ct.Current ?? ct.Max ?? ct.Min ?? string.Empty,
                                    YearMin = ct.YearMin,
                                    YearMax = ct.YearMax,
                                    CreatedDate = DateTime.UtcNow,
                                    LastUpdatedDate = DateTime.UtcNow
                                };
                                innerDb.DotConstructionPeriods.Add(newPeriod);
                            }
                            
                            var exec = innerDb.Database.CreateExecutionStrategy();
                            await exec.ExecuteAsync(async () => { await innerDb.SaveChangesAsync(); });
                            Interlocked.Increment(ref successCount);
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref errorCount);
                            Logger.LogError(ex, "ConstructionPeriods sync hatası: {DatECode}", datECode);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }).ToList();

                    await Task.WhenAll(tasks);

                    statusMessage += $"📊 Batch {batchNumber}: ✅ {successCount} | ❌ {errorCount} | ⏭️ {skippedCount}\n";
                    StateHasChanged();
                }

                statusMessage += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                statusMessage += $"🎉 CONSTRUCTION PERIODS SYNC TAMAMLANDI!\n";
                statusMessage += $"✅ Başarılı: {successCount}\n";
                statusMessage += $"❌ Hatalı: {errorCount}\n";
                statusMessage += $"⏭️ Atlandı (zaten var): {skippedCount}\n";

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "ConstructionPeriods Sync Tamamlandı",
                    Detail = $"{successCount} kayıt eklendi!"
                });
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "BulkSyncConstructionPeriods hatası");
            statusMessage += $"❌ HATA: {ex.Message}\n";
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = ex.Message
            });
        }
        finally
        {
            isBulkSyncingConstructionPeriods = false;
            StateHasChanged();
        }
    }
    
    private async Task UpdateConstructionPeriodYears()
    {
        try
        {
            isUpdatingYears = true;
            statusMessage = "🔄 YIL GÜNCELLEMESİ BAŞLIYOR...\n";
            StateHasChanged();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Yıl bilgisi null olan kayıtları bul (AsNoTracking: başka context'te kullanacağız için)
            var periodsWithoutYears = await db.DotConstructionPeriods
                .AsNoTracking()
                .Where(cp => cp.YearMin == null || cp.YearMax == null)
                .ToListAsync();

            statusMessage += $"📊 Güncellenecek kayıt sayısı: {periodsWithoutYears.Count}\n\n";
            StateHasChanged();

            var successCount = 0;
            var errorCount = 0;
            var batchSize = 100;
            var throttler = new SemaphoreSlim(4); // Düşürüldü: DB connection pool için

            for (int i = 0; i < periodsWithoutYears.Count; i += batchSize)
            {
                var batch = periodsWithoutYears.Skip(i).Take(batchSize).ToList();
                var batchNumber = (i / batchSize) + 1;

                statusMessage += $"🔄 Batch {batchNumber} işleniyor ({batch.Count} kayıt)...\n";
                StateHasChanged();

                // Paralel işlem yerine senkron yap (debug için)
                foreach (var period in batch)
                {
                    try
                    {
                        using var innerScope = _scopeFactory.CreateScope();
                        var innerDb = innerScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        int? yearMin = period.YearMin;
                        int? yearMax = period.YearMax;

                        statusMessage += $"  → {period.DatECode}: YearMin={yearMin}, YearMax={yearMax}\n";
                        StateHasChanged();

                        // Min tarihini çevir
                        if (yearMin == null && !string.IsNullOrEmpty(period.ConstructionTimeMin))
                        {
                            var minDate = await SyncService._datService.ConvertConstructionTimeToDateAsync(period.ConstructionTimeMin);
                            if (minDate.HasValue)
                            {
                                yearMin = minDate.Value.Year;
                                statusMessage += $"    ✅ YearMin from API: {yearMin}\n";
                                StateHasChanged();
                            }
                        }

                        // Max tarihini çevir
                        if (yearMax == null && !string.IsNullOrEmpty(period.ConstructionTimeMax))
                        {
                            var maxDate = await SyncService._datService.ConvertConstructionTimeToDateAsync(period.ConstructionTimeMax);
                            if (maxDate.HasValue)
                            {
                                yearMax = maxDate.Value.Year;
                                statusMessage += $"    ✅ YearMax from API: {yearMax}\n";
                                StateHasChanged();
                            }
                        }

                        // Güncelle
                        if (yearMin.HasValue || yearMax.HasValue)
                        {
                            // İlk sorguyu AsNoTracking ile yapıyoruz çünkü sadece ID'yi alacağız
                            var recordId = await innerDb.DotConstructionPeriods
                                .AsNoTracking()
                                .Where(cp => cp.DatECode == period.DatECode)
                                .Select(cp => cp.Id)
                                .FirstOrDefaultAsync();
                            
                            if (recordId > 0)
                            {
                                // Şimdi tracking ile yeniden oku
                                var existing = await innerDb.DotConstructionPeriods.FindAsync(recordId);
                                if (existing != null)
                                {
                                    var oldMin = existing.YearMin;
                                    var oldMax = existing.YearMax;
                                    
                                    existing.YearMin = yearMin ?? existing.YearMin;
                                    existing.YearMax = yearMax ?? existing.YearMax;
                                    existing.LastUpdatedDate = DateTime.UtcNow;
                                    
                                    // EF tracking sorunu için Update() kullan
                                    innerDb.DotConstructionPeriods.Update(existing);
                                    var changesCount = await innerDb.SaveChangesAsync();
                                    
                                    if (changesCount > 0)
                                    {
                                        statusMessage += $"    💾 Güncellendi: {oldMin}→{existing.YearMin}, {oldMax}→{existing.YearMax}\n";
                                        successCount++;
                                    }
                                    else
                                    {
                                        statusMessage += $"    ⚠️ SaveChanges 0 döndü\n";
                                    }
                                    StateHasChanged();
                                }
                                else
                                {
                                    statusMessage += $"    ⚠️ FindAsync null döndü (Id={recordId})\n";
                                    StateHasChanged();
                                }
                            }
                            else
                            {
                                statusMessage += $"    ⚠️ Kayıt DB'de bulunamadı\n";
                                StateHasChanged();
                            }
                        }
                        else
                        {
                            statusMessage += $"    ⚠️ API'den yıl gelmedi\n";
                            StateHasChanged();
                        }
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        statusMessage += $"    ❌ HATA: {ex.Message}\n";
                        Logger.LogError(ex, "Yıl güncelleme hatası: {DatECode}", period.DatECode);
                        StateHasChanged();
                    }
                }

                statusMessage += $"📊 Batch {batchNumber}: ✅ {successCount} | ❌ {errorCount}\n";
                StateHasChanged();
            }

            statusMessage += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
            statusMessage += $"🎉 YIL GÜNCELLEMESİ TAMAMLANDI!\n";
            statusMessage += $"✅ Güncellenen: {successCount}\n";
            statusMessage += $"❌ Hatalı: {errorCount}\n";

            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Yıl Güncellemesi Tamamlandı",
                Detail = $"{successCount} kayıt güncellendi!"
            });
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "UpdateConstructionPeriodYears hatası");
            statusMessage += $"❌ HATA: {ex.Message}\n";
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Sync Hatası",
                Detail = ex.Message
            });
        }
        finally
        {
            isUpdatingYears = false;
            StateHasChanged();
        }
    }

    private async Task BulkSyncAllVehicleData()
    {
        isRunning = true;
        errorMessage = string.Empty;
        statusMessage = "🚀 BULK VehicleData Sync başlatılıyor...\n";
        statusMessage += "⚠️ NOT: Bu işlem için önce 1️⃣ ConstructionPeriods sync tamamlanmalı!\n\n";
        StateHasChanged();

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();

                // Toplam kayıt sayısını al
                var totalCount = await dbContext.Set<DotCompiledCode>()
                    .Where(c => !string.IsNullOrEmpty(c.DatECode))
                    .CountAsync();

                // Kaç tanesi ConstructionPeriods'a sahip kontrol et
                var withConstructionTime = await dbContext.DotConstructionPeriods
                    .Where(cp => !string.IsNullOrWhiteSpace(cp.CurrentConstructionTime))
                    .CountAsync();

                // Eksik olanların VehicleType dağılımı
                var missingByType = await dbContext.Set<DotCompiledCode>()
                    .Where(c => !string.IsNullOrEmpty(c.DatECode))
                    .Where(c => !dbContext.DotConstructionPeriods.Any(cp => cp.DatECode == c.DatECode && !string.IsNullOrWhiteSpace(cp.CurrentConstructionTime)))
                    .GroupBy(c => c.VehicleType)
                    .Select(g => new { VehicleType = g.Key, Count = g.Count() })
                    .ToListAsync();

                statusMessage += $"📊 Toplam {totalCount} adet DatECode bulundu\n";
                statusMessage += $"✅ {withConstructionTime} tanesi ConstructionPeriods'a sahip\n";
                statusMessage += $"⚠️ {totalCount - withConstructionTime} tanesi atlanacak (ConstructionTime yok)\n";
                
                if (missingByType.Any())
                {
                    statusMessage += $"   Eksik kayıtların dağılımı:\n";
                    foreach (var item in missingByType.OrderBy(x => x.VehicleType))
                    {
                        statusMessage += $"   - Type {item.VehicleType}: {item.Count} adet\n";
                    }
                }
                
                statusMessage += $"⚙️  Batch boyutu: 100 kayıt\n";
                statusMessage += $"📦 Toplam batch sayısı: {Math.Ceiling(totalCount / 100.0)}\n\n";
                StateHasChanged();

                int successCount = 0;
                int errorCount = 0;
                int skippedCount = 0;
                int batchNumber = 0;
                int batchSize = 100;

                for (int skip = 0; skip < totalCount; skip += batchSize)
                {
                    batchNumber++;
                    statusMessage += $"\n━━━ BATCH {batchNumber} ({skip + 1}-{Math.Min(skip + batchSize, totalCount)}) ━━━\n";
                    StateHasChanged();

                    var batch = await dbContext.Set<DotCompiledCode>()
                        .Where(c => !string.IsNullOrEmpty(c.DatECode))
                        .OrderBy(c => c.Id)
                        .Skip(skip)
                        .Take(batchSize)
                        .Select(c => new { c.DatECode, c.VehicleType, c.ManufacturerKey, c.BaseModelKey, c.SubModelKey })
                        .ToListAsync();

                    foreach (var code in batch)
                    {
                        try
                        {
                            // Zaten kayıtlı mı kontrol et
                            var exists = await dbContext.Set<DotVehicleData>()
                                .AnyAsync(v => v.DatECode == code.DatECode);

                            if (exists)
                            {
                                skippedCount++;
                                continue;
                            }

                            // ConstructionTime'ı tablodan oku
                            var period = await dbContext.DotConstructionPeriods
                                .FirstOrDefaultAsync(cp => cp.DatECode == code.DatECode);
                            
                            if (period == null || string.IsNullOrWhiteSpace(period.CurrentConstructionTime))
                            {
                                skippedCount++;
                                if (skippedCount <= 10)
                                {
                                    statusMessage += $"⏭️ {code.DatECode} (Type:{code.VehicleType}): ConstructionTime yok (önce 1️⃣ butonu çalıştırın)\n";
                                    StateHasChanged();
                                }
                                continue; // ConstructionTime yoksa atla (önce ConstructionPeriods sync çalıştırılmalı)
                            }

                            var vehicleData = await SyncService._datService.GetVehicleDataAsync(
                                code.DatECode, 
                                null, 
                                period.CurrentConstructionTime);

                            if (!string.IsNullOrWhiteSpace(vehicleData.DatECode) && 
                                !string.IsNullOrWhiteSpace(vehicleData.ManufacturerName))
                            {
                                await dataService.SaveVehicleDataAsync(vehicleData);
                                successCount++;
                                
                                if (successCount % 10 == 0)
                                {
                                    statusMessage += $"✅ {successCount} kayıt başarılı | ❌ {errorCount} hata | ⏭️ {skippedCount} atlandı\n";
                                    StateHasChanged();
                                }
                            }
                            else
                            {
                                errorCount++;
                                if (errorCount <= 10)
                                {
                                    statusMessage += $"⚠️ {code.DatECode}: API'den boş yanıt\n";
                                    StateHasChanged();
                                }
                            }

                            await Task.Delay(200); // Rate limiting
                        }
                        catch (Exception ex)
                        {
                            errorCount++;
                            if (errorCount <= 10)
                            {
                                statusMessage += $"❌ {code.DatECode}: {ex.Message}\n";
                                StateHasChanged();
                            }
                            Logger.LogError(ex, "Bulk VehicleData sync hatası: {DatECode}", code.DatECode);
                        }
                    }

                    statusMessage += $"📊 Batch {batchNumber} tamamlandı: ✅ {successCount} | ❌ {errorCount} | ⏭️ {skippedCount}\n";
                    StateHasChanged();
                }

                statusMessage += $"\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                statusMessage += $"🎉 BULK SYNC TAMAMLANDI!\n";
                statusMessage += $"✅ Başarılı: {successCount}\n";
                statusMessage += $"❌ Hatalı: {errorCount}\n";
                statusMessage += $"⏭️ Atlandı (zaten var): {skippedCount}\n";
                statusMessage += $"📊 Toplam işlenen: {totalCount}\n";

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Bulk Sync Tamamlandı",
                    Detail = $"{successCount} yeni kayıt eklendi!"
                });
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Bulk Sync Hatası: {ex.Message}";
            Logger.LogError(ex, "BulkSyncAllVehicleData sırasında hata oluştu");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Bulk Sync Hatası",
                Detail = ex.Message
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private async Task TestVehicleDataSync()
    {
        isRunning = true;
        errorMessage = string.Empty;
        statusMessage = "🧪 Test VehicleData Sync başlatılıyor...\n";
        StateHasChanged();

        try
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var dataService = scope.ServiceProvider.GetRequiredService<DatDataService>();

                // Önce bilinen başarılı bir DatECode'u test et
                var knownWorkingECode = "015700460110001"; // Postman'de getConstructionPeriodsN için çalışan
                statusMessage += $"🧪 Bilinen başarılı DatECode test ediliyor: {knownWorkingECode}\n\n";
                StateHasChanged();

                try
                {
                    // Önce ConstructionTime'ı çek
                    var constructionTime = await SyncService._datService.GetConstructionPeriodsAsync(knownWorkingECode);
                    if (string.IsNullOrWhiteSpace(constructionTime))
                    {
                        statusMessage += $"❌ Test DatECode için ConstructionTime bulunamadı!\n";
                        return;
                    }
                    
                    statusMessage += $"🕐 Test ConstructionTime: {constructionTime}\n";
                    StateHasChanged();
                    
                    var vehicleData = await SyncService._datService.GetVehicleDataAsync(knownWorkingECode, null, constructionTime);
                    
                    if (!string.IsNullOrWhiteSpace(vehicleData.DatECode))
                    {
                        await dataService.SaveVehicleDataAsync(vehicleData);
                        statusMessage += $"✅ TEST BAŞARILI!\n";
                        statusMessage += $"   Marka: {vehicleData.ManufacturerName}\n";
                        statusMessage += $"   Model: {vehicleData.BaseModelName} {vehicleData.SubModelName}\n";
                        
                        if (vehicleData.TechInfo != null)
                        {
                            statusMessage += $"   Motor: {vehicleData.TechInfo.PowerHp} HP ({vehicleData.TechInfo.PowerKw} kW)\n";
                            statusMessage += $"   Yakıt: {vehicleData.TechInfo.FuelMethod}\n";
                            statusMessage += $"   Vites: {vehicleData.TechInfo.GearboxType}\n";
                        }
                        
                        statusMessage += $"\n✅ API çalışıyor! Şimdi tablodaki DatECode'ları test ediyoruz...\n\n";
                    }
                    else
                    {
                        statusMessage += $"❌ Bilinen DatECode bile çalışmadı! API sorunu var.\n";
                        NotificationService.Notify(new NotificationMessage
                        {
                            Severity = NotificationSeverity.Error,
                            Summary = "API Hatası",
                            Detail = "Bilinen çalışan DatECode bile veri döndürmedi!"
                        });
                        return;
                    }
                }
                catch (Exception ex)
                {
                    statusMessage += $"❌ Test DatECode hatası: {ex.Message}\n";
                    Logger.LogError(ex, "Test DatECode hatası");
                }
                
                StateHasChanged();
                await Task.Delay(1000);

                // DotCompiledCodes tablosundan DatECode'ları al (VehicleType=1 Otomobil/SUV)
                statusMessage += "📋 DotCompiledCodes tablosundan DatECode'lar çekiliyor (VehicleType=1)...\n";
                StateHasChanged();

                var compiledCodes = await dbContext.Set<DotCompiledCode>()
                    .Where(c => !string.IsNullOrEmpty(c.DatECode) && c.VehicleType == 1)
                    .OrderByDescending(c => c.CreatedDate)
                    .Take(50) // 50 kayıt test edelim, birkaçı mutlaka çalışır
                    .Select(c => new { c.DatECode, c.VehicleType, c.ManufacturerKey, c.BaseModelKey, c.SubModelKey })
                    .ToListAsync();

                if (!compiledCodes.Any())
                {
                    statusMessage += "⚠️ DotCompiledCodes tablosunda DatECode bulunamadı!\n";
                    NotificationService.Notify(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Warning,
                        Summary = "Veri Bulunamadı",
                        Detail = "Test için veri bulunamadı!"
                    });
                    return;
                }

                statusMessage += $"✅ {compiledCodes.Count} adet DatECode bulundu. VehicleData çekiliyor...\n\n";
                StateHasChanged();

                int successCount = 0;
                int errorCount = 0;

                foreach (var code in compiledCodes)
                {
                    try
                    {
                        statusMessage += $"🔍 DatECode: {code.DatECode}\n";
                        statusMessage += $"   📌 VT:{code.VehicleType} | M:{code.ManufacturerKey} | B:{code.BaseModelKey} | S:{code.SubModelKey}\n";
                        StateHasChanged();

                        // Önce ConstructionTime'ı çek
                        var constructionTime = await SyncService._datService.GetConstructionPeriodsAsync(code.DatECode);
                        if (string.IsNullOrWhiteSpace(constructionTime))
                        {
                            statusMessage += $"   ⚠️ ConstructionTime bulunamadı, atlaniyor\n\n";
                            errorCount++;
                            StateHasChanged();
                            await Task.Delay(200);
                            continue;
                        }
                        
                        statusMessage += $"   🕐 ConstructionTime: {constructionTime}\n";
                        
                        // ConstructionPeriod kaydet
                        var existingPeriod = await dbContext.DotConstructionPeriods
                            .FirstOrDefaultAsync(cp => cp.DatECode == code.DatECode);
                        
                        if (existingPeriod == null)
                        {
                            var newPeriod = new DotConstructionPeriod
                            {
                                DatECode = code.DatECode,
                                ConstructionTimeMin = constructionTime, // Şimdilik aynı
                                ConstructionTimeMax = constructionTime,
                                CurrentConstructionTime = constructionTime,
                                CreatedDate = DateTime.UtcNow
                            };
                            dbContext.DotConstructionPeriods.Add(newPeriod);
                            await dbContext.SaveChangesAsync();
                        }
                        
                        StateHasChanged();

                        // Sonra VehicleData'yı çek
                        var vehicleData = await SyncService._datService.GetVehicleDataAsync(
                            code.DatECode, 
                            null, 
                            constructionTime);

                        if (!string.IsNullOrWhiteSpace(vehicleData.DatECode))
                        {
                            await dataService.SaveVehicleDataAsync(vehicleData);
                            statusMessage += $"   ✅ Kaydedildi: {vehicleData.ManufacturerName} {vehicleData.BaseModelName} {vehicleData.SubModelName}\n";
                            
                            if (vehicleData.TechInfo != null)
                            {
                                statusMessage += $"   📊 Motor: {vehicleData.TechInfo.PowerHp} HP ({vehicleData.TechInfo.PowerKw} kW)\n";
                                statusMessage += $"   ⛽ Yakıt: {vehicleData.TechInfo.FuelMethod} | Vites: {vehicleData.TechInfo.GearboxType}\n";
                                statusMessage += $"   📏 Boyutlar: {vehicleData.TechInfo.Length}x{vehicleData.TechInfo.Width}x{vehicleData.TechInfo.Height} mm\n";
                            }
                            
                            successCount++;
                        }
                        else
                        {
                            statusMessage += $"   ⚠️ Boş vehicle data döndü (API'den veri gelmedi)\n";
                            errorCount++;
                        }

                        statusMessage += "\n";
                        StateHasChanged();
                        await Task.Delay(500); // Rate limiting
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        statusMessage += $"   ❌ Hata: {ex.Message}\n\n";
                        Logger.LogError(ex, "Test VehicleData sync hatası: {DatECode}", code.DatECode);
                        StateHasChanged();
                    }
                }

                statusMessage += $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n";
                statusMessage += $"🎉 TEST TAMAMLANDI!\n";
                statusMessage += $"✅ Başarılı: {successCount}\n";
                statusMessage += $"❌ Hatalı: {errorCount}\n";
                statusMessage += $"📊 Toplam: {compiledCodes.Count}\n";

                NotificationService.Notify(new NotificationMessage
                {
                    Severity = successCount > 0 ? NotificationSeverity.Success : NotificationSeverity.Warning,
                    Summary = "Test Tamamlandı",
                    Detail = $"{successCount} başarılı, {errorCount} hatalı"
                });
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Test Hatası: {ex.Message}";
            Logger.LogError(ex, "TestVehicleDataSync sırasında hata oluştu");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Test Hatası",
                Detail = ex.Message
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }

    private async Task SyncFromDatDatas()
    {
        isRunning = true;
        statusMessage = string.Empty;
        errorMessage = string.Empty;
        syncStats = new SyncStatistics();
        StateHasChanged();

        try
        {
            statusMessage = "🚀 DatDatas tablosundan senkronizasyon başlatılıyor...\n\n";
            StateHasChanged();

            // 1. DatDatas tablosundan unique grupları çek (IsTrans = false olanlar)
            var uniqueGroups = await SyncService._dataService.GetUniqueDatDataGroupsAsync();
            
            if (!uniqueGroups.Any())
            {
                statusMessage += "⚠️ İşlenecek kayıt bulunamadı (Tüm DatDatas kayıtları zaten aktarılmış).\n";
                isRunning = false;
                StateHasChanged();
                return;
            }

            statusMessage += $"📊 Toplam {uniqueGroups.Count} farklı araç grubu işlenecek.\n\n";
            StateHasChanged();

            int processedGroups = 0;
            int totalGroups = uniqueGroups.Count;

            foreach (var group in uniqueGroups)
            {
                processedGroups++;
                var vehicleTypeKey = group.VehicleTypeKey;
                var manufacturerKey = group.ManufactureKey.ToString();
                var baseModelKey = group.BaseModelKey.ToString();
                
                // SubModelKey bilinmiyor, "0" veya boş gönderilebilir. SearchPartsAsync metodunda subModelKey zorunlu değilse null geçilebilir.
                // Ancak metod imzasında string subModelKey zorunlu görünüyor. Genellikle "0" veya ilk submodel key kullanılır.
                // DatData tablosunda SubModel bilgisi yok, bu yüzden "0" varsayıyoruz.
                var subModelKey = "0"; 

                try
                {
                    // 2. Bu grup için DatProcessNo listesini çek
                    var datProcessNos = await SyncService._dataService.GetDatProcessNumbersByVehicleAsync(
                        vehicleTypeKey, group.ManufactureKey, group.BaseModelKey);

                    if (!datProcessNos.Any()) continue;

                    var allDatProcessNosString = datProcessNos.Select(p => p.ToString()).ToList();
                    
                    // Batching: Max 50 items per request
                    var batches = allDatProcessNosString.Chunk(50).ToList();
                    int currentBatch = 0;
                    int totalBatches = batches.Count;

                    foreach (var batch in batches)
                    {
                        currentBatch++;
                        var batchList = batch.ToList();
                        
                        statusMessage += $"🔄 [{processedGroups}/{totalGroups}] VT:{vehicleTypeKey} M:{manufacturerKey} B:{baseModelKey} - Batch {currentBatch}/{totalBatches} ({batchList.Count} items)...\n";
                        StateHasChanged();

                        try 
                        {
                            // 3. API'den parçaları çek
                            var partsResponse = await SyncService._datService.SearchPartsAsync(
                                vehicleTypeKey, manufacturerKey, baseModelKey, subModelKey, 
                                datProcessNos: batchList);

                            if (partsResponse?.Parts?.Count > 0)
                            {
                                // 4. Parçaları kaydet
                                await SyncService._dataService.SavePartsAsync(partsResponse.Parts, null);
                                
                                syncStats.PartsCount += partsResponse.Parts.Count;
                                statusMessage += $"  ✅ {partsResponse.Parts.Count} parça kaydedildi.\n";
                            }
                            else
                            {
                                statusMessage += $"  ⚠️ Parça bulunamadı.\n";
                            }

                            // 5. DatDatas tablosunu güncelle (IsTrans = true) - Sadece bu batch için
                            await SyncService._dataService.MarkDatDataAsTransferredAsync(
                                vehicleTypeKey, group.ManufactureKey, group.BaseModelKey, batchList);
                            
                            statusMessage += $"  💾 DatDatas güncellendi (Batch {currentBatch}).\n";
                            StateHasChanged();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError(ex, "Batch işlenirken hata: VT:{VT} M:{M} B:{B}", vehicleTypeKey, manufacturerKey, baseModelKey);
                            statusMessage += $"  ❌ Batch Hatası: {ex.Message}\n";
                        }
                        
                        // API rate limit için kısa bekleme
                        await Task.Delay(100);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Grup işlenirken hata: VT:{VT} M:{M} B:{B}", vehicleTypeKey, manufacturerKey, baseModelKey);
                    statusMessage += $"  ❌ Hata: {ex.Message}\n";
                }
                
                // UI donmasını engellemek için kısa bekleme
                await Task.Delay(50);
            }

            statusMessage += $"\n🎉 SENKRONİZASYON TAMAMLANDI!\n";
            statusMessage += $"📊 Toplam {syncStats.PartsCount} parça aktarıldı.\n";
            
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Senkronizasyon Tamamlandı",
                Detail = $"{syncStats.PartsCount} parça aktarıldı",
                Duration = 5000
            });

        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Logger.LogError(ex, "DatDatas senkronizasyon hatası");
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Error,
                Summary = "Hata",
                Detail = "Senkronizasyon sırasında hata oluştu",
                Duration = 5000
            });
        }
        finally
        {
            isRunning = false;
            StateHasChanged();
        }
    }
}

